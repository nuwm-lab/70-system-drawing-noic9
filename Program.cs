using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace GraphDrawing
{
    public class GraphForm : Form
    {
        private const double X_START = 0.0;
        private const double X_END = 0.5;
        private const double DELTA_X = 0.1;
        
        private List<PointF> dataPoints;
        private double minY, maxY;

        public GraphForm()
        {
            InitializeForm();
            CalculateDataPoints();
        }

        private void InitializeForm()
        {
            this.Text = "Графік функції y = (2.5x³) / (e^(2x) + 2)";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(400, 300);
            this.BackColor = Color.White;
            
            // Встановлюємо подвійну буферизацію для плавного малювання
            this.DoubleBuffered = true;
            
            // Підписуємось на події
            this.Paint += GraphForm_Paint;
            this.Resize += GraphForm_Resize;
        }

        private void CalculateDataPoints()
        {
            dataPoints = new List<PointF>();
            minY = double.MaxValue;
            maxY = double.MinValue;

            // Обчислюємо точки графіка
            for (double x = X_START; x <= X_END; x += DELTA_X)
            {
                double y = CalculateFunction(x);
                dataPoints.Add(new PointF((float)x, (float)y));
                
                // Знаходимо мінімум та максимум для масштабування
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        private double CalculateFunction(double x)
        {
            // y = (2.5x³) / (e^(2x) + 2)
            double numerator = 2.5 * Math.Pow(x, 3);
            double denominator = Math.Exp(2 * x) + 2;
            return numerator / denominator;
        }

        private void GraphForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Визначаємо відступи
            int leftMargin = 60;
            int rightMargin = 40;
            int topMargin = 40;
            int bottomMargin = 60;

            int graphWidth = this.ClientSize.Width - leftMargin - rightMargin;
            int graphHeight = this.ClientSize.Height - topMargin - bottomMargin;

            // Малюємо координатні осі
            DrawAxes(g, leftMargin, topMargin, graphWidth, graphHeight);

            // Малюємо сітку
            DrawGrid(g, leftMargin, topMargin, graphWidth, graphHeight);

            // Малюємо підписи осей
            DrawLabels(g, leftMargin, topMargin, graphWidth, graphHeight);

            // Малюємо графік
            DrawGraph(g, leftMargin, topMargin, graphWidth, graphHeight);

            // Виводимо формулу
            DrawFormula(g);
        }

        private void DrawAxes(Graphics g, int left, int top, int width, int height)
        {
            Pen axisPen = new Pen(Color.Black, 2);

            // Вертикальна вісь (Y)
            g.DrawLine(axisPen, left, top, left, top + height);

            // Горизонтальна вісь (X)
            g.DrawLine(axisPen, left, top + height, left + width, top + height);

            // Стрілки
            g.DrawLine(axisPen, left - 5, top + 10, left, top);
            g.DrawLine(axisPen, left + 5, top + 10, left, top);
            g.DrawLine(axisPen, left + width - 10, top + height - 5, left + width, top + height);
            g.DrawLine(axisPen, left + width - 10, top + height + 5, left + width, top + height);
        }

        private void DrawGrid(Graphics g, int left, int top, int width, int height)
        {
            Pen gridPen = new Pen(Color.LightGray, 1);
            gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

            // Вертикальні лінії
            for (int i = 1; i < 10; i++)
            {
                int x = left + (width * i) / 10;
                g.DrawLine(gridPen, x, top, x, top + height);
            }

            // Горизонтальні лінії
            for (int i = 1; i < 10; i++)
            {
                int y = top + (height * i) / 10;
                g.DrawLine(gridPen, left, y, left + width, y);
            }
        }

        private void DrawLabels(Graphics g, int left, int top, int width, int height)
        {
            Font labelFont = new Font("Arial", 8);
            Brush labelBrush = Brushes.Black;

            // Підписи осі X
            for (int i = 0; i <= 5; i++)
            {
                double xValue = X_START + (X_END - X_START) * i / 5.0;
                int xPos = left + (width * i) / 5;
                string label = xValue.ToString("F2");
                SizeF size = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, labelBrush, 
                    xPos - size.Width / 2, top + height + 5);
            }

            // Підписи осі Y
            for (int i = 0; i <= 5; i++)
            {
                double yValue = maxY - (maxY - minY) * i / 5.0;
                int yPos = top + (height * i) / 5;
                string label = yValue.ToString("F4");
                SizeF size = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, labelBrush, 
                    left - size.Width - 5, yPos - size.Height / 2);
            }

            // Назви осей
            Font axisFont = new Font("Arial", 10, FontStyle.Bold);
            g.DrawString("X", axisFont, labelBrush, left + width + 5, top + height - 10);
            g.DrawString("Y", axisFont, labelBrush, left - 15, top - 20);
        }

        private void DrawGraph(Graphics g, int left, int top, int width, int height)
        {
            if (dataPoints.Count < 2) return;

            Pen graphPen = new Pen(Color.Blue, 2);
            Brush pointBrush = new SolidBrush(Color.Red);

            // Перетворюємо точки даних у координати екрану
            PointF[] screenPoints = new PointF[dataPoints.Count];
            
            for (int i = 0; i < dataPoints.Count; i++)
            {
                float screenX = left + (dataPoints[i].X - (float)X_START) / 
                    ((float)X_END - (float)X_START) * width;
                float screenY = top + height - (dataPoints[i].Y - (float)minY) / 
                    ((float)maxY - (float)minY) * height;
                
                screenPoints[i] = new PointF(screenX, screenY);
            }

            // Малюємо лінії графіка
            g.DrawLines(graphPen, screenPoints);

            // Малюємо точки
            foreach (PointF point in screenPoints)
            {
                g.FillEllipse(pointBrush, point.X - 3, point.Y - 3, 6, 6);
            }
        }

        private void DrawFormula(Graphics g)
        {
            Font formulaFont = new Font("Arial", 11, FontStyle.Bold);
            string formula = "y = (2.5x³) / (e^(2x) + 2)";
            string range = $"0 ≤ x ≤ 0.5, Δx = 0.1";
            
            g.DrawString(formula, formulaFont, Brushes.DarkBlue, 10, 10);
            g.DrawString(range, new Font("Arial", 9), Brushes.DarkGreen, 10, 30);
        }

        private void GraphForm_Resize(object sender, EventArgs e)
        {
            // Перемальовуємо графік при зміні розміру вікна
            this.Invalidate();
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
