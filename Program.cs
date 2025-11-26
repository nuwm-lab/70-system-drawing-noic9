using System;
using System.Drawing;
using System.Windows.Forms;

namespace GraphDrawing
{
    public class GraphForm : Form
    {
        private const double X_MIN = 0. 0;
        private const double X_MAX = 0.5;
        private const double DELTA_X = 0.1;
        
        private double yMin;
        private double yMax;

        public GraphForm()
        {
            this.Text = "Графік функції y = (2.5x³) / (e^(2x) + 2)";
            this.Size = new Size(800, 600);
            this. MinimumSize = new Size(400, 300);
            this. DoubleBuffered = true; // Запобігає миготінню при перерисуванні
            
            // Обчислення значень функції
            CalculateFunctionValues();
            
            // Підписка на події
            this.Paint += GraphForm_Paint;
            this. Resize += GraphForm_Resize;
        }

        /// <summary>
        /// Обчислення значень функції для визначення діапазону Y
        /// </summary>
        private void CalculateFunctionValues()
        {
            yMin = double.MaxValue;
            yMax = double.MinValue;
            
            // Обчислення значень y та знаходження мінімуму/максимуму
            for (double x = X_MIN; x <= X_MAX; x += DELTA_X)
            {
                double y = CalculateY(x);
                
                if (y < yMin) yMin = y;
                if (y > yMax) yMax = y;
            }
            
            // Додаємо невеликий запас для осей
            double yRange = yMax - yMin;
            yMin -= yRange * 0.1;
            yMax += yRange * 0.1;
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
            int screenX = (int)(drawArea.Left + (x - X_MIN) / (X_MAX - X_MIN) * drawArea.Width);
            int screenY = (int)(drawArea. Bottom - (y - yMin) / (yMax - yMin) * drawArea.Height);
            return new Point(screenX, screenY);
        }

        /// <summary>
        /// Обробка події зміни розміру вікна
        /// </summary>
        private void GraphForm_Resize(object sender, EventArgs e)
        {
            this.Invalidate(); // Перерисовуємо форму
        }

        /// <summary>
        /// Обробка події малювання
        /// </summary>
        private void GraphForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Очищення фону
            g.Clear(Color. White);
            
            // Визначення області для малювання (з відступами для підписів)
            int margin = 60;
            Rectangle drawArea = new Rectangle(
                margin, 
                margin, 
                this.ClientSize.Width - 2 * margin, 
                this.ClientSize.Height - 2 * margin
            );
            
            // Малювання осей координат
            DrawAxes(g, drawArea);
            
            // Малювання сітки
            DrawGrid(g, drawArea);
            
            // Малювання графіка
            DrawGraph(g, drawArea);
            
            // Малювання підписів
            DrawLabels(g, drawArea);
        }

        /// <summary>
        /// Малювання осей координат
        /// </summary>
        private void DrawAxes(Graphics g, Rectangle drawArea)
        {
            Pen axisPen = new Pen(Color.Black, 2);
            
            // Вісь X
            g.DrawLine(axisPen, drawArea.Left, drawArea.Bottom, drawArea.Right, drawArea. Bottom);
            
            // Вісь Y
            g. DrawLine(axisPen, drawArea.Left, drawArea.Top, drawArea.Left, drawArea.Bottom);
            
            // Стрілки на осях
            DrawArrow(g, axisPen, drawArea.Right, drawArea.Bottom, 0); // Стрілка X
            DrawArrow(g, axisPen, drawArea.Left, drawArea.Top, 90);     // Стрілка Y
            
            axisPen.Dispose();
        }

        /// <summary>
        /// Малювання стрілки
        /// </summary>
        private void DrawArrow(Graphics g, Pen pen, int x, int y, int angle)
        {
            int arrowSize = 10;
            PointF[] arrowPoints;
            
            if (angle == 0) // Стрілка вправо (вісь X)
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x - arrowSize, y - arrowSize / 2),
                    new PointF(x - arrowSize, y + arrowSize / 2)
                };
            }
            else // Стрілка вгору (вісь Y)
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x - arrowSize / 2, y + arrowSize),
                    new PointF(x + arrowSize / 2, y + arrowSize)
                };
            }
            
            using (SolidBrush brush = new SolidBrush(pen.Color))
            {
                g.FillPolygon(brush, arrowPoints);
            }
        }

        /// <summary>
        /// Малювання сітки
        /// </summary>
        private void DrawGrid(Graphics g, Rectangle drawArea)
        {
            using (Pen gridPen = new Pen(Color.LightGray, 1))
            {
                gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle. Dot;
                
                // Вертикальні лінії сітки (по X)
                for (double x = X_MIN; x <= X_MAX; x += DELTA_X)
                {
                    Point p = ConvertToScreenCoordinates(x, yMin, drawArea);
                    g.DrawLine(gridPen, p. X, drawArea.Top, p.X, drawArea.Bottom);
                }
                
                // Горизонтальні лінії сітки (по Y)
                int gridLinesCount = 10;
                for (int i = 0; i <= gridLinesCount; i++)
                {
                    double y = yMin + (yMax - yMin) * i / gridLinesCount;
                    Point p = ConvertToScreenCoordinates(X_MIN, y, drawArea);
                    g.DrawLine(gridPen, drawArea.Left, p.Y, drawArea.Right, p.Y);
                }
            }
        }

        /// <summary>
        /// Малювання графіка функції
        /// </summary>
        private void DrawGraph(Graphics g, Rectangle drawArea)
        {
            using (Pen graphPen = new Pen(Color.Blue, 3))
            {
                // Обчислення точок графіка в координатах екрану
                int pointCount = (int)((X_MAX - X_MIN) / DELTA_X) + 1;
                Point[] screenPoints = new Point[pointCount];
                
                int index = 0;
                for (double x = X_MIN; x <= X_MAX; x += DELTA_X)
                {
                    double y = CalculateY(x);
                    screenPoints[index] = ConvertToScreenCoordinates(x, y, drawArea);
                    index++;
                }
                
                // Малювання ліній між точками
                if (screenPoints.Length > 1)
                {
                    g.DrawLines(graphPen, screenPoints);
                }
                
                // Малювання точок на графіку
                using (Brush pointBrush = new SolidBrush(Color.Red))
                {
                    foreach (Point p in screenPoints)
                    {
                        g.FillEllipse(pointBrush, p.X - 4, p.Y - 4, 8, 8);
                    }
                }
            }
        }

        /// <summary>
        /// Малювання підписів осей та значень
        /// </summary>
        private void DrawLabels(Graphics g, Rectangle drawArea)
        {
            using (Font font = new Font("Arial", 10))
            using (Font axisFont = new Font("Arial", 12, FontStyle.Bold))
            using (Font titleFont = new Font("Arial", 11, FontStyle. Italic))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                // Підписи на осі X
                for (double x = X_MIN; x <= X_MAX; x += DELTA_X)
                {
                    Point p = ConvertToScreenCoordinates(x, yMin, drawArea);
                    string label = x.ToString("F1");
                    SizeF labelSize = g.MeasureString(label, font);
                    g.DrawString(label, font, textBrush, 
                        p.X - labelSize.Width / 2, 
                        drawArea.Bottom + 5);
                }
                
                // Підписи на осі Y
                int gridLinesCount = 10;
                for (int i = 0; i <= gridLinesCount; i++)
                {
                    double y = yMin + (yMax - yMin) * i / gridLinesCount;
                    Point p = ConvertToScreenCoordinates(X_MIN, y, drawArea);
                    string label = y.ToString("F4");
                    SizeF labelSize = g.MeasureString(label, font);
                    g.DrawString(label, font, textBrush, 
                        drawArea.Left - labelSize.Width - 5, 
                        p.Y - labelSize.Height / 2);
                }
                
                // Назва осі X
                g.DrawString("X", axisFont, textBrush, drawArea.Right + 5, drawArea.Bottom - 10);
                
                // Назва осі Y
                g.DrawString("Y", axisFont, textBrush, drawArea.Left - 10, drawArea.Top - 20);
                
                // Формула функції
                string formula = "y = (2. 5x³) / (e^(2x) + 2)";
                SizeF formulaSize = g.MeasureString(formula, titleFont);
                g.DrawString(formula, titleFont, textBrush, 
                    (this.ClientSize.Width - formulaSize.Width) / 2, 10);
            }
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
