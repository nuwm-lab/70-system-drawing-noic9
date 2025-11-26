using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace GraphDrawing
{
    public class GraphForm : Form
    {
        // Константи для функції
        private const double X_START = 0.0;
        private const double X_END = 0.5;
        private const double DELTA_X = 0.1;
        
        // Константи для відступів та UI
        private const int LEFT_MARGIN = 60;
        private const int RIGHT_MARGIN = 40;
        private const int TOP_MARGIN = 40;
        private const int BOTTOM_MARGIN = 60;
        private const int GRID_DIVISIONS = 10;
        private const int AXIS_LABEL_COUNT = 5;
        private const float POINT_SIZE = 6f;
        private const double MIN_Y_RANGE = 0.0001;
        private const double Y_PADDING_FACTOR = 0.1;
        
        // Дані графіка
        private List<PointF> _dataPoints;
        private double _minY;
        private double _maxY;
        
        // GDI+ об'єкти для повторного використання
        private Pen _axisPen;
        private Pen _gridPen;
        private Pen _graphPen;
        private Brush _pointBrush;
        private Brush _textBrush;
        private Font _labelFont;
        private Font _axisFont;
        private Font _formulaFont;
        private Font _rangeFont;
        
        // Кешування фону
        private Bitmap _backgroundCache;
        private bool _needsBackgroundRedraw = true;

        public GraphForm()
        {
            InitializeForm();
            InitializeGdiObjects();
            CalculateDataPoints();
        }

        private void InitializeForm()
        {
            this.Text = "Графік функції y = (2.5x³) / (e^(2x) + 2)";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(400, 300);
            this.BackColor = Color.White;
            
            // Оптимізована подвійна буферизація
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer | 
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.UserPaint, 
                true);
            
            // Підписуємось на події
            this.Paint += GraphForm_Paint;
            this.Resize += GraphForm_Resize;
        }
        
        private void InitializeGdiObjects()
        {
            _axisPen = new Pen(Color.Black, 2);
            _gridPen = new Pen(Color.LightGray, 1) { DashStyle = DashStyle.Dot };
            _graphPen = new Pen(Color.Blue, 2);
            _pointBrush = new SolidBrush(Color.Red);
            _textBrush = new SolidBrush(Color.Black);
            _labelFont = new Font("Arial", 8);
            _axisFont = new Font("Arial", 10, FontStyle.Bold);
            _formulaFont = new Font("Arial", 11, FontStyle.Bold);
            _rangeFont = new Font("Arial", 9);
        }

        private void CalculateDataPoints()
        {
            _dataPoints = new List<PointF>();
            _minY = double.MaxValue;
            _maxY = double.MinValue;

            // Обчислюємо точки графіка
            for (double x = X_START; x <= X_END + DELTA_X / 2; x += DELTA_X)
            {
                double y = CalculateFunction(x);
                _dataPoints.Add(new PointF((float)x, (float)y));
                
                // Знаходимо мінімум та максимум для масштабування
                if (y < _minY) _minY = y;
                if (y > _maxY) _maxY = y;
            }
            
            // Додаємо padding для кращого відображення
            double yRange = _maxY - _minY;
            if (yRange < MIN_Y_RANGE)
            {
                // Якщо діапазон занадто малий, встановлюємо мінімальний
                double center = (_maxY + _minY) / 2;
                _minY = center - MIN_Y_RANGE / 2;
                _maxY = center + MIN_Y_RANGE / 2;
            }
            else
            {
                double padding = yRange * Y_PADDING_FACTOR;
                _minY -= padding;
                _maxY += padding;
            }
        }

        private double CalculateFunction(double x)
        {
            // y = (2.5x³) / (e^(2x) + 2)
            double numerator = 2.5 * Math.Pow(x, 3);
            double denominator = Math.Exp(2 * x) + 2;
            
            // Захист від ділення на нуль (хоча denominator завжди >= 2)
            if (Math.Abs(denominator) < double.Epsilon)
                return 0;
                
            return numerator / denominator;
        }

        private void GraphForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int graphWidth = this.ClientSize.Width - LEFT_MARGIN - RIGHT_MARGIN;
            int graphHeight = this.ClientSize.Height - TOP_MARGIN - BOTTOM_MARGIN;

            // Перевірка мінімальних розмірів
            if (graphWidth < 100 || graphHeight < 100)
            {
                g.DrawString("Вікно занадто мале", _labelFont, _textBrush, 10, 10);
                return;
            }

            // Малюємо або кешований фон, або створюємо новий
            if (_needsBackgroundRedraw || _backgroundCache == null || 
                _backgroundCache.Width != this.ClientSize.Width || 
                _backgroundCache.Height != this.ClientSize.Height)
            {
                RenderBackground();
                _needsBackgroundRedraw = false;
            }

            // Малюємо кешований фон
            if (_backgroundCache != null)
            {
                g.DrawImageUnscaled(_backgroundCache, 0, 0);
            }

            // Малюємо динамічний графік поверх фону
            DrawGraph(g, LEFT_MARGIN, TOP_MARGIN, graphWidth, graphHeight);
        }
        
        private void RenderBackground()
        {
            // Звільняємо попередній кеш
            _backgroundCache?.Dispose();
            
            // Створюємо новий bitmap для фону
            _backgroundCache = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            
            using (Graphics g = Graphics.FromImage(_backgroundCache))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(this.BackColor);

                int graphWidth = this.ClientSize.Width - LEFT_MARGIN - RIGHT_MARGIN;
                int graphHeight = this.ClientSize.Height - TOP_MARGIN - BOTTOM_MARGIN;

                // Малюємо статичні елементи
                DrawAxes(g, LEFT_MARGIN, TOP_MARGIN, graphWidth, graphHeight);
                DrawGrid(g, LEFT_MARGIN, TOP_MARGIN, graphWidth, graphHeight);
                DrawLabels(g, LEFT_MARGIN, TOP_MARGIN, graphWidth, graphHeight);
                DrawFormula(g);
            }
        }

        private void DrawAxes(Graphics g, int left, int top, int width, int height)
        {
            // Вертикальна вісь (Y)
            g.DrawLine(_axisPen, left, top, left, top + height);

            // Горизонтальна вісь (X)
            g.DrawLine(_axisPen, left, top + height, left + width, top + height);

            // Стрілки
            g.DrawLine(_axisPen, left - 5, top + 10, left, top);
            g.DrawLine(_axisPen, left + 5, top + 10, left, top);
            g.DrawLine(_axisPen, left + width - 10, top + height - 5, left + width, top + height);
            g.DrawLine(_axisPen, left + width - 10, top + height + 5, left + width, top + height);
        }

        private void DrawGrid(Graphics g, int left, int top, int width, int height)
        {
            // Вертикальні лінії
            for (int i = 1; i < GRID_DIVISIONS; i++)
            {
                int x = left + (width * i) / GRID_DIVISIONS;
                g.DrawLine(_gridPen, x, top, x, top + height);
            }

            // Горизонтальні лінії
            for (int i = 1; i < GRID_DIVISIONS; i++)
            {
                int y = top + (height * i) / GRID_DIVISIONS;
                g.DrawLine(_gridPen, left, y, left + width, y);
            }
        }

        private void DrawLabels(Graphics g, int left, int top, int width, int height)
        {
            // Підписи осі X
            for (int i = 0; i <= AXIS_LABEL_COUNT; i++)
            {
                double xValue = X_START + (X_END - X_START) * i / (double)AXIS_LABEL_COUNT;
                int xPos = left + (width * i) / AXIS_LABEL_COUNT;
                string label = xValue.ToString("F2");
                SizeF size = g.MeasureString(label, _labelFont);
                g.DrawString(label, _labelFont, _textBrush, 
                    xPos - size.Width / 2, top + height + 5);
            }

            // Підписи осі Y
            double yRange = _maxY - _minY;
            if (yRange > MIN_Y_RANGE)
            {
                for (int i = 0; i <= AXIS_LABEL_COUNT; i++)
                {
                    double yValue = _maxY - yRange * i / (double)AXIS_LABEL_COUNT;
                    int yPos = top + (height * i) / AXIS_LABEL_COUNT;
                    string label = yValue.ToString("F4");
                    SizeF size = g.MeasureString(label, _labelFont);
                    g.DrawString(label, _labelFont, _textBrush, 
                        left - size.Width - 5, yPos - size.Height / 2);
                }
            }

            // Назви осей
            g.DrawString("X", _axisFont, _textBrush, left + width + 5, top + height - 10);
            g.DrawString("Y", _axisFont, _textBrush, left - 15, top - 20);
        }

        private void DrawGraph(Graphics g, int left, int top, int width, int height)
        {
            if (_dataPoints == null || _dataPoints.Count < 2) return;

            double yRange = _maxY - _minY;
            double xRange = X_END - X_START;
            
            // Захист від ділення на нуль
            if (yRange < MIN_Y_RANGE || xRange < double.Epsilon)
                return;

            // Перетворюємо точки даних у координати екрану
            PointF[] screenPoints = new PointF[_dataPoints.Count];
            
            for (int i = 0; i < _dataPoints.Count; i++)
            {
                float screenX = left + (float)((_dataPoints[i].X - X_START) / xRange * width);
                float screenY = top + (float)((_maxY - _dataPoints[i].Y) / yRange * height);
                
                screenPoints[i] = new PointF(screenX, screenY);
            }

            // Малюємо лінії графіка
            g.DrawLines(_graphPen, screenPoints);

            // Малюємо точки
            float halfPointSize = POINT_SIZE / 2;
            foreach (PointF point in screenPoints)
            {
                g.FillEllipse(_pointBrush, 
                    point.X - halfPointSize, 
                    point.Y - halfPointSize, 
                    POINT_SIZE, 
                    POINT_SIZE);
            }
        }

        private void DrawFormula(Graphics g)
        {
            string formula = "y = (2.5x³) / (e^(2x) + 2)";
            string range = $"0 ≤ x ≤ 0.5, Δx = 0.1";
            
            g.DrawString(formula, _formulaFont, Brushes.DarkBlue, 10, 10);
            g.DrawString(range, _rangeFont, Brushes.DarkGreen, 10, 30);
        }

        private void GraphForm_Resize(object sender, EventArgs e)
        {
            // Позначаємо, що потрібно перемалювати фон
            _needsBackgroundRedraw = true;
            
            // Перемальовуємо графік при зміні розміру вікна
            this.Invalidate();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Звільняємо всі GDI+ об'єкти
                _axisPen?.Dispose();
                _gridPen?.Dispose();
                _graphPen?.Dispose();
                _pointBrush?.Dispose();
                _textBrush?.Dispose();
                _labelFont?.Dispose();
                _axisFont?.Dispose();
                _formulaFont?.Dispose();
                _rangeFont?.Dispose();
                _backgroundCache?.Dispose();
            }
            base.Dispose(disposing);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GraphForm());
        }
    }
}
