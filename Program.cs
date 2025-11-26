using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GraphDrawing
{
    /// <summary>
    /// Клас для рендерингу графіка функції
    /// </summary>
    public class ChartRenderer
    {
        private const double XMin = 0.0;
        private const double XMax = 0.5;
        private const double DeltaX = 0.1;

        private readonly double _yMin;
        private readonly double _yMax;

        // Кешовані ресурси для малювання
        private readonly Pen _axisPen;
        private readonly Pen _gridPen;
        private readonly Pen _graphPen;
        private readonly Brush _pointBrush;
        private readonly Brush _textBrush;
        private readonly Brush _arrowBrush;
        private readonly Font _labelFont;
        private readonly Font _axisFont;
        private readonly Font _titleFont;

        // Кешовані точки графіка
        private Point[] _cachedScreenPoints;
        private Point[] _cachedDeltaPoints;
        private Rectangle _lastDrawArea;

        public enum DrawMode { LineAndPoints, LineOnly, PointsOnly }

        public ChartRenderer()
        {
            // Обчислення діапазону Y
            CalculateFunctionRange(out _yMin, out _yMax);

            // Ініціалізація ресурсів
            _axisPen = new Pen(Color.Black, 2);
            _gridPen = new Pen(Color.LightGray, 1) { DashStyle = DashStyle. Dot };
            _graphPen = new Pen(Color.Blue, 3);
            _pointBrush = new SolidBrush(Color.Red);
            _textBrush = new SolidBrush(Color.Black);
            _arrowBrush = new SolidBrush(Color. Black);
            _labelFont = new Font("Arial", 10);
            _axisFont = new Font("Arial", 12, FontStyle.Bold);
            _titleFont = new Font("Arial", 11, FontStyle. Italic);

            _lastDrawArea = Rectangle.Empty;
        }

        /// <summary>
        /// Обчислення діапазону значень функції
        /// </summary>
        private void CalculateFunctionRange(out double yMin, out double yMax)
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
            if (Math.Abs(yRange) < 1e-10)
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
        /// Обчислення значення функції y = (2. 5x³) / (e^(2x) + 2)
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
            if (drawArea. Width <= 0 || drawArea.Height <= 0)
                return new Point(drawArea.Left, drawArea.Bottom);

            double xRange = XMax - XMin;
            double yRange = _yMax - _yMin;

            if (Math.Abs(xRange) < 1e-10) xRange = 1.0;
            if (Math.Abs(yRange) < 1e-10) yRange = 1. 0;

            int screenX = (int)(drawArea.Left + (x - XMin) / xRange * drawArea.Width);
            int screenY = (int)(drawArea.Bottom - (y - _yMin) / yRange * drawArea.Height);

            return new Point(screenX, screenY);
        }

        /// <summary>
        /// Отримання координати Y для осі X (де y = 0)
        /// </summary>
        private int GetXAxisScreenY(Rectangle drawArea)
        {
            if (_yMin <= 0 && _yMax >= 0)
            {
                Point p = ConvertToScreenCoordinates(XMin, 0, drawArea);
                return p.Y;
            }
            return drawArea.Bottom;
        }

        /// <summary>
        /// Отримання координати X для осі Y (де x = 0)
        /// </summary>
        private int GetYAxisScreenX(Rectangle drawArea)
        {
            if (XMin <= 0 && XMax >= 0)
            {
                Point p = ConvertToScreenCoordinates(0, _yMin, drawArea);
                return p.X;
            }
            return drawArea.Left;
        }

        /// <summary>
        /// Перерахунок кешованих точок графіка
        /// </summary>
        private void RecalculateScreenPoints(Rectangle drawArea)
        {
            if (drawArea.Width <= 0 || drawArea.Height <= 0)
                return;

            // Динамічний розрахунок кількості точок
            int pixelsPerPoint = 3;
            int targetPointCount = Math.Max(drawArea.Width / pixelsPerPoint, 20);

            double step = (XMax - XMin) / (targetPointCount - 1);
            step = Math.Max(step, DeltaX / 10);

            int pointCount = (int)((XMax - XMin) / step) + 1;
            _cachedScreenPoints = new Point[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                double x = XMin + i * step;
                if (x > XMax) x = XMax;

                double y = CalculateY(x);
                _cachedScreenPoints[i] = ConvertToScreenCoordinates(x, y, drawArea);
            }

            // Точки за кроком DeltaX
            int deltaPointCount = (int)((XMax - XMin) / DeltaX) + 1;
            _cachedDeltaPoints = new Point[deltaPointCount];

            for (int i = 0; i < deltaPointCount; i++)
            {
                double x = XMin + i * DeltaX;
                double y = CalculateY(x);
                _cachedDeltaPoints[i] = ConvertToScreenCoordinates(x, y, drawArea);
            }

            _lastDrawArea = drawArea;
        }

        /// <summary>
        /// Головний метод рендерингу
        /// </summary>
        public void Render(Graphics g, Rectangle clientArea, DrawMode mode)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            // Визначення області для малювання
            int margin = 70;
            int topMargin = 50;
            Rectangle drawArea = new Rectangle(
                margin,
                topMargin,
                clientArea.Width - 2 * margin,
                clientArea.Height - margin - topMargin
            );

            // Перевірка коректності розмірів
            if (drawArea.Width <= 0 || drawArea.Height <= 0)
                return;

            // Перерахунок точок при зміні розміру
            if (_lastDrawArea != drawArea)
            {
                RecalculateScreenPoints(drawArea);
            }

            DrawGrid(g, drawArea);
            DrawAxes(g, drawArea);
            DrawGraph(g, drawArea, mode);
            DrawLabels(g, drawArea);
            DrawTitle(g, clientArea);
        }

        /// <summary>
        /// Малювання сітки
        /// </summary>
        private void DrawGrid(Graphics g, Rectangle drawArea)
        {
            // Вертикальні лінії
            for (double x = XMin; x <= XMax + DeltaX / 2; x += DeltaX)
            {
                Point p = ConvertToScreenCoordinates(x, _yMin, drawArea);
                g.DrawLine(_gridPen, p. X, drawArea.Top, p.X, drawArea. Bottom);
            }

            // Горизонтальні лінії
            int gridLinesCount = 10;
            for (int i = 0; i <= gridLinesCount; i++)
            {
                double y = _yMin + (_yMax - _yMin) * i / gridLinesCount;
                Point p = ConvertToScreenCoordinates(XMin, y, drawArea);
                g. DrawLine(_gridPen, drawArea.Left, p.Y, drawArea.Right, p.Y);
            }
        }

        /// <summary>
        /// Малювання осей координат
        /// </summary>
        private void DrawAxes(Graphics g, Rectangle drawArea)
        {
            int xAxisY = GetXAxisScreenY(drawArea);
            int yAxisX = GetYAxisScreenX(drawArea);

            // Осі
            g.DrawLine(_axisPen, drawArea.Left, xAxisY, drawArea.Right, xAxisY);
            g.DrawLine(_axisPen, yAxisX, drawArea.Top, yAxisX, drawArea.Bottom);

            // Стрілки
            DrawArrow(g, drawArea. Right, xAxisY, 0);
            DrawArrow(g, yAxisX, drawArea.Top, 90);
        }

        /// <summary>
        /// Малювання стрілки
        /// </summary>
        private void DrawArrow(Graphics g, int x, int y, int angle)
        {
            int arrowSize = 10;
            PointF[] arrowPoints;

            if (angle == 0)
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x - arrowSize, y - arrowSize / 2f),
                    new PointF(x - arrowSize, y + arrowSize / 2f)
                };
            }
            else
            {
                arrowPoints = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x - arrowSize / 2f, y + arrowSize),
                    new PointF(x + arrowSize / 2f, y + arrowSize)
                };
            }

            g. FillPolygon(_arrowBrush, arrowPoints);
        }

        /// <summary>
        /// Малювання графіка
        /// </summary>
        private void DrawGraph(Graphics g, Rectangle drawArea, DrawMode mode)
        {
            if (_cachedScreenPoints == null || _cachedScreenPoints.Length == 0)
                return;

            // Малювання ліній
            if ((mode == DrawMode.LineAndPoints || mode == DrawMode.LineOnly) && _cachedScreenPoints. Length > 1)
            {
                g.DrawLines(_graphPen, _cachedScreenPoints);
            }

            // Малювання точок
            if ((mode == DrawMode.LineAndPoints || mode == DrawMode.PointsOnly) && _cachedDeltaPoints != null)
            {
                foreach (Point p in _cachedDeltaPoints)
                {
                    g.FillEllipse(_pointBrush, p.X - 4, p.Y - 4, 8, 8);
                }
            }
        }

        /// <summary>
        /// Малювання підписів осей
        /// </summary>
        private void DrawLabels(Graphics g, Rectangle drawArea)
        {
            // Підписи осі X
            for (double x = XMin; x <= XMax + DeltaX / 2; x += DeltaX)
            {
                Point p = ConvertToScreenCoordinates(x, _yMin, drawArea);
                string label = x.ToString("F1");
                SizeF labelSize = g.MeasureString(label, _labelFont);
                g.DrawString(label, _labelFont, _textBrush,
                    p.X - labelSize.Width / 2,
                    drawArea.Bottom + 5);
            }

            // Підписи осі Y
            int gridLinesCount = 10;
            for (int i = 0; i <= gridLinesCount; i++)
            {
                double y = _yMin + (_yMax - _yMin) * i / gridLinesCount;
                Point p = ConvertToScreenCoordinates(XMin, y, drawArea);
                string label = y.ToString("F4");
                SizeF labelSize = g.MeasureString(label, _labelFont);
                g.DrawString(label, _labelFont, _textBrush,
                    drawArea.Left - labelSize.Width - 5,
                    p. Y - labelSize.Height / 2);
            }

            // Назви осей
            g.DrawString("X", _axisFont, _textBrush, drawArea.Right + 5, drawArea.Bottom - 10);
            g.DrawString("Y", _axisFont, _textBrush, drawArea. Left - 10, drawArea.Top - 20);
        }

        /// <summary>
        /// Малювання заголовка
        /// </summary>
        private void DrawTitle(Graphics g, Rectangle clientArea)
        {
            string formula = "y = (2.5x³) / (e^(2x) + 2)";
            SizeF formulaSize = g.MeasureString(formula, _titleFont);
            g.DrawString(formula, _titleFont, _textBrush,
                (clientArea.Width - formulaSize.Width) / 2, 10);
        }

        /// <summary>
        /// Звільнення ресурсів
        /// </summary>
        public void Dispose()
        {
            _axisPen?.Dispose();
            _gridPen?. Dispose();
            _graphPen?.Dispose();
            _pointBrush?.Dispose();
            _textBrush?. Dispose();
            _arrowBrush?.Dispose();
            _labelFont?.Dispose();
            _axisFont?. Dispose();
            _titleFont?. Dispose();
        }
    }

    /// <summary>
    /// Форма для відображення графіка
    /// </summary>
    public class GraphForm : Form
    {
        private readonly ChartRenderer _renderer;
        private ComboBox _drawModeComboBox;
        private Label _modeLabel;
        private ChartRenderer.DrawMode _currentDrawMode = ChartRenderer.DrawMode.LineAndPoints;

        public GraphForm()
        {
            this.Text = "Графік функції y = (2.5x³) / (e^(2x) + 2)";
            this.Size = new Size(900, 700);
            this.MinimumSize = new Size(500, 400);

            // Увімкнення подвійного буферування
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);

            _renderer = new ChartRenderer();
            InitializeControls();
        }

        /// <summary>
        /// Ініціалізація UI елементів
        /// </summary>
        private void InitializeControls()
        {
            _modeLabel = new Label
            {
                Text = "Режим:",
                Location = new Point(10, 10),
                Size = new Size(60, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(_modeLabel);

            _drawModeComboBox = new ComboBox
            {
                Location = new Point(70, 10),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _drawModeComboBox.Items.AddRange(new string[]
            {
                "Лінії та точки",
                "Тільки лінії",
                "Тільки точки"
            });
            _drawModeComboBox.SelectedIndex = 0;
            _drawModeComboBox. SelectedIndexChanged += DrawModeComboBox_SelectedIndexChanged;
            this.Controls.Add(_drawModeComboBox);
        }

        /// <summary>
        /// Обробник зміни режиму малювання
        /// </summary>
        private void DrawModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentDrawMode = (ChartRenderer.DrawMode)_drawModeComboBox. SelectedIndex;
            this.Invalidate();
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
            _renderer.Render(e.Graphics, this.ClientRectangle, _currentDrawMode);
        }

        /// <summary>
        /// Звільнення ресурсів
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderer?.Dispose();
                _drawModeComboBox?.Dispose();
                _modeLabel?.Dispose();
            }
            base. Dispose(disposing);
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
