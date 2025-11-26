using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GraphDrawing
{
    public class GraphForm : Form
    {
        // Константи для функції
        private const double XMin = 0. 0;
        private const double XMax = 0.5;
        private const double DeltaX = 0.1;
        
        // Діапазон значень Y
        private double yMin;
        private double yMax;
        
        // Кешовані ресурси для малювання
        private Pen axisPen;
        private Pen gridPen;
        private Pen graphPen;
        private Brush pointBrush;
        private Brush textBrush;
        private Font labelFont;
        private Font axisFont;
        private Font titleFont;
        
        // UI елементи
        private ComboBox drawModeComboBox;
        private Label modeLabel;
        private enum DrawMode { LineAndPoints, LineOnly, PointsOnly }
        private DrawMode currentDrawMode = DrawMode.LineAndPoints;

        public GraphForm()
        {
            this.Text = "Графік функції y = (2.5x³) / (e^(2x) + 2)";
            this.Size = new Size(900, 700);
            this.MinimumSize = new Size(500, 400);
            
            // Увімкнення подвійного буферування
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint, true);
            
            // Ініціалізація ресурсів для малювання
            InitializeDrawingResources();
            
            // Обчислення діапазону значень функції
            CalculateFunctionRange();
            
            // Створення UI елементів
            InitializeControls();
        }

        /// <summary>
        /// Ініціалізація UI елементів
        /// </summary>
        private void InitializeControls()
        {
            // Label для режиму малювання
            modeLabel = new Label
            {
                Text = "Режим:",
                Location = new Point(10, 10),
                Size = new Size(60, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(modeLabel);

            // ComboBox для вибору режиму малювання
            drawModeComboBox = new ComboBox
            {
                Location = new Point(70, 10),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle. DropDownList
            };
            drawModeComboBox.Items. AddRange(new string[] 
            { 
                "Лінії та точки", 
                "Тільки лінії", 
                "Тільки точки" 
            });
            drawModeComboBox.SelectedIndex = 0;
            drawModeComboBox. SelectedIndexChanged += DrawModeComboBox_SelectedIndexChanged;
            this.Controls.Add(drawModeComboBox);
        }

        /// <summary>
        /// Обробник зміни режиму малювання
        /// </summary>
        private void DrawModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentDrawMode = (DrawMode)drawModeComboBox.SelectedIndex;
            this.Invalidate();
        }

        /// <summary>
        /// Ініціалізація ресурсів для малювання
        /// </summary>
        private void InitializeDrawingResources()
        {
            axisPen = new Pen(Color.Black, 2);
            
            gridPen = new Pen(Color.LightGray, 1)
            {
                DashStyle = DashStyle. Dot
            };
            
            graphPen = new Pen(Color.Blue, 3);
            
            pointBrush = new SolidBrush(Color.Red);
            textBrush = new SolidBrush(Color.Black);
            
            labelFont = new Font("Arial", 10);
            axisFont = new Font("Arial", 12, FontStyle.Bold);
            titleFont = new Font("Arial", 11, FontStyle. Italic);
        }

        /// <summary>
        /// Обчислення діапазону значень функції
        /// </summary>
        private void CalculateFunctionRange()
        {
            yMin = double.MaxValue;
            yMax = double.MinValue;
            
            for (double x = XMin; x <= XMax + DeltaX / 2; x += DeltaX)
            {
                double y = CalculateY(x);
                if (y < yMin) yMin = y;
                if (y > yMax) yMax = y;
            }
            
            // Додаємо запас для осей
            double yRange = yMax - yMin;
            if (Math.Abs(yRange) < 1e-10) // Перевірка на випадок yMax == yMin
            {
                yRange = 1. 0;
                yMin -= 0.5;
                yMax += 0.5;
            }
            else
            {
                yMin -= yRange * 0.1;
                yMax += yRange * 0.1;
            }
        }

        /// <summary>
        /// Обчислення значення функції y = (2.5x³) / (e^(2x) + 2)
        /// </summary>
        private double CalculateY(double x)
        {
            double numerator = 2.5 * Math.Pow(x, 3);
            double denominator = Math.Exp(2 * x) + 2;
            return numerator / denominator;
        }

        /// <summary>
        /// Перетворення координат графіка в координати екрану
        /// </summary>
        private Point ConvertToScreenCoordinates(double x, double y, Rectangle drawArea)
        {
            if (drawArea.Width <= 0 || drawArea.Height <= 0)
                return new Point(drawArea.Left, drawArea.Bottom);

            double xRange = XMax - XMin;
            double yRange = yMax - yMin;
            
            if (Math.Abs(xRange) < 1e-10) xRange = 1.0;
            if (Math.Abs(yRange) < 1e-10) yRange = 1. 0;

            int screenX = (int)(drawArea.Left + (x - XMin) / xRange * drawArea.Width);
            int screenY = (int)(drawArea.Bottom - (y - yMin) / yRange * drawArea.Height);
            
            return new Point(screenX, screenY);
        }

        /// <summary>
        /// Отримання координати Y для осі X (де y = 0)
        /// </summary>
        private int GetXAxisScreenY(Rectangle drawArea)
        {
            if (yMin <= 0 && yMax >= 0)
            {
                // Вісь X проходить через y = 0
                Point p = ConvertToScreenCoordinates(XMin, 0, drawArea);
                return p.Y;
            }
            // Якщо 0 не входить в діапазон, малюємо внизу
            return drawArea.Bottom;
        }

        /// <summary>
        /// Отримання координати X для осі Y (де x = 0)
        /// </summary>
        private int GetYAxisScreenX(Rectangle drawArea)
        {
            if (XMin <= 0 && XMax >= 0)
            {
                // Вісь Y проходить через x = 0
                Point p = ConvertToScreenCoordinates(0, yMin, drawArea);
                return p.X;
            }
            // Якщо 0 не входить в діапазон, малюємо зліва
            return drawArea.Left;
        }

        /// <summary>
        /// Перевизначення OnResize
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate();
        }

        /// <summary>
        /// Перевизначення OnPaint
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            
            // Визначення області для малювання
            int margin = 70;
            int topMargin = 50;
            Rectangle drawArea = new Rectangle(
                margin,
                topMargin,
                this.ClientSize.Width - 2 * margin,
                this.ClientSize.Height - margin - topMargin
            );
            
            // Перевірка на коректність розмірів
            if (drawArea.Width <= 0 || drawArea.Height <= 0)
                return;
            
            DrawGrid(g, drawArea);
            DrawAxes(g, drawArea);
            DrawGraph(g, drawArea);
            DrawLabels(g, drawArea);
            DrawTitle(g);
        }

        /// <summary>
        /// Малювання сітки
        /// </summary>
        private void DrawGrid(Graphics g, Rectangle drawArea)
        {
            // Вертикальні лінії сітки
            for (double x = XMin; x <= XMax + DeltaX / 2; x += DeltaX)
            {
                Point p = ConvertToScreenCoordinates(x, yMin, drawArea);
                g.DrawLine(gridPen, p. X, drawArea.Top, p.X, drawArea.Bottom);
            }
            
            // Горизонтальні лінії сітки
            int gridLinesCount = 10;
            for (int i = 0; i <= gridLinesCount; i++)
            {
                double y = yMin + (yMax - yMin) * i / gridLinesCount;
                Point p = ConvertToScreenCoordinates(XMin, y, drawArea);
                g.DrawLine(gridPen, drawArea.Left, p.Y, drawArea.Right, p.Y);
            }
        }

        /// <summary>
        /// Малювання осей координат
        /// </summary>
        private void DrawAxes(Graphics g, Rectangle drawArea)
        {
            int xAxisY = GetXAxisScreenY(drawArea);
            int yAxisX = GetYAxisScreenX(drawArea);
            
            // Вісь X
            g.DrawLine(axisPen, drawArea.Left, xAxisY, drawArea.Right, xAxisY);
            
            // Вісь Y
            g.DrawLine(axisPen, yAxisX, drawArea.Top, yAxisX, drawArea.Bottom);
            
            // Стрілки
            DrawArrow(g, axisPen, drawArea.Right, xAxisY, 0);
            DrawArrow(g, axisPen, yAxisX, drawArea.Top, 90);
        }

        /// <summary>
        /// Малювання стрілки
        /// </summary>
        private void DrawArrow(Graphics g, Pen pen, int x, int y, int angle)
        {
            int arrowSize = 10;
            PointF[] arrowPoints;
            
            if (angle == 0) // Стрілка вправо
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x - arrowSize, y - arrowSize / 2f),
                    new PointF(x - arrowSize, y + arrowSize / 2f)
                };
            }
            else // Стрілка вгору
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x - arrowSize / 2f, y + arrowSize),
                    new PointF(x + arrowSize / 2f, y + arrowSize)
                };
            }
            
            g.FillPolygon(new SolidBrush(pen.Color), arrowPoints);
        }

        /// <summary>
        /// Малювання графіка функції з динамічною деталізацією
        /// </summary>
        private void DrawGraph(Graphics g, Rectangle drawArea)
        {
            // Динамічний розрахунок кількості точок залежно від ширини
            int pixelsPerPoint = 5;
            int targetPointCount = Math.Max(drawArea.Width / pixelsPerPoint, 10);
            
            double step = (XMax - XMin) / (targetPointCount - 1);
            step = Math.Max(step, DeltaX / 10); // Мінімальний крок
            
            int pointCount = (int)((XMax - XMin) / step) + 1;
            Point[] screenPoints = new Point[pointCount];
            
            // Обчислення точок
            for (int i = 0; i < pointCount; i++)
            {
                double x = XMin + i * step;
                if (x > XMax) x = XMax;
                
                double y = CalculateY(x);
                screenPoints[i] = ConvertToScreenCoordinates(x, y, drawArea);
            }
            
            // Малювання ліній
            if ((currentDrawMode == DrawMode.LineAndPoints || currentDrawMode == DrawMode.LineOnly) 
                && screenPoints.Length > 1)
            {
                g.DrawLines(graphPen, screenPoints);
            }
            
            // Малювання точок за кроком DeltaX
            if (currentDrawMode == DrawMode. LineAndPoints || currentDrawMode == DrawMode. PointsOnly)
            {
                for (double x = XMin; x <= XMax + DeltaX / 2; x += DeltaX)
                {
                    double y = CalculateY(x);
                    Point p = ConvertToScreenCoordinates(x, y, drawArea);
                    g.FillEllipse(pointBrush, p.X - 4, p.Y - 4, 8, 8);
                }
            }
        }

        /// <summary>
        /// Малювання підписів
        /// </summary>
        private void DrawLabels(Graphics g, Rectangle drawArea)
        {
            // Підписи на осі X
            for (double x = XMin; x <= XMax + DeltaX / 2; x += DeltaX)
            {
                Point p = ConvertToScreenCoordinates(x, yMin, drawArea);
                string label = x.ToString("F1");
                SizeF labelSize = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, textBrush,
                    p.X - labelSize.Width / 2,
                    drawArea.Bottom + 5);
            }
            
            // Підписи на осі Y
            int gridLinesCount = 10;
            for (int i = 0; i <= gridLinesCount; i++)
            {
                double y = yMin + (yMax - yMin) * i / gridLinesCount;
                Point p = ConvertToScreenCoordinates(XMin, y, drawArea);
                string label = y.ToString("F4");
                SizeF labelSize = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, textBrush,
                    drawArea.Left - labelSize.Width - 5,
                    p. Y - labelSize.Height / 2);
            }
            
            // Назви осей
            g.DrawString("X", axisFont, textBrush, drawArea.Right + 5, drawArea.Bottom - 10);
            g.DrawString("Y", axisFont, textBrush, drawArea.Left - 10, drawArea.Top - 20);
        }

        /// <summary>
        /// Малювання заголовка
        /// </summary>
        private void DrawTitle(Graphics g)
        {
            string formula = "y = (2.5x³) / (e^(2x) + 2)";
            SizeF formulaSize = g.MeasureString(formula, titleFont);
            g.DrawString(formula, titleFont, textBrush,
                (this.ClientSize. Width - formulaSize.Width) / 2, 10);
        }

        /// <summary>
        /// Звільнення ресурсів
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                axisPen?.Dispose();
                gridPen?.Dispose();
                graphPen?.Dispose();
                pointBrush?. Dispose();
                textBrush?.Dispose();
                labelFont?.Dispose();
                axisFont?.Dispose();
                titleFont?.Dispose();
                
                drawModeComboBox?.Dispose();
                modeLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    static class Program
    {
        /// <summary>
        /// Головна точка входу для програми.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GraphForm());
        }
    }
}
