using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;

// TODO:
// -suwaki do współcznników kd, ks, m
// -źródło światła wspolrzedna z
// -domyślny kolor obiektu
// -modyfikacja wektora normalnego na podstawie wczytanej mapy wektorów normalnych


namespace Zadanie2GK
{
    public partial class Zadanie2 : Form
    {
        readonly Drawer drawer;
        readonly Calculator calculator;
        int frame = 0;
        Vector3D lightPos = new Vector3D(1.5, 0.5, 5);
        readonly double lightRadius = 1.0;
        readonly Stopwatch stopWatch = new Stopwatch();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        static bool drawingOnA = true;
        double lastFPS = 0;
        public Zadanie2()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            InitializeComponent();

            drawer = new Drawer(Canvas.Width, Canvas.Height);
            calculator = new Calculator();
            calculator.SetPrecision(5);


            timer.Interval = 40; // 25 FPS
            timer.Tick += new EventHandler(RenderFrame);
            timer.Start();
        }
        private void RenderFrame(object sender, EventArgs e)
        {
            UpdateLight();
            
            stopWatch.Restart();
            //List<(double r, double g, double b)> colors = calculator.CalculateColors(lightPos);
            Bitmap bmp = new Bitmap(Canvas.Width, Canvas.Height);
            using (var gfx = Graphics.FromImage(bmp))
            {
                drawer.DrawPolygons(gfx, calculator, lightPos);
            }
            Canvas.Image = bmp;
            Canvas.Invalidate();
            //drawer.DrawMultiThread(e, calculator, lightPos);
            stopWatch.Stop();
            lastFPS += 1000.0 / stopWatch.ElapsedMilliseconds;
            lastFPS /= 2;
            FPSLabel.Text = $"FPS: {lastFPS.ToString("0.00")} ({stopWatch.ElapsedMilliseconds} ms)";
            frame++;
        }
        private void UpdateLight()
        {
            lightPos.X = (lightRadius * Math.Cos(frame * 40f / 5000f * 2f * Math.PI)) + 0.5;
            lightPos.Y = (lightRadius * Math.Sin(frame * 40f / 5000f * 2f * Math.PI)) + 0.5;
        }
        private void ksBar_ValueChanged(object sender, EventArgs e)
        {
            calculator.SetKdSlider(kdBar.Value);
        }
        private void MBox_ValueChanged(object sender, EventArgs e)
        {
            calculator.SetM((int)MBox.Value);
        }
    }
    public class Canvas : Panel
    {
        public Canvas()
        {
            this.DoubleBuffered = true;
        }
    }
    public class Calculator
    {
        double[,] Z;
        public int numberSegments;
        double kd = 0.2;
        int m = 30;
        (double r, double g, double b) lightColor = (1, 1, 1);
        (double r, double g, double b) objectColor = (1, 0, 0);
        Vector3D[,] cachedPoints;
        Vector3D[,] cachedNormals;
        List<Vector3D[]> cachedTraingles;

        void clearCache()
        {
            cachedPoints = null;
            cachedNormals = null;
            cachedTraingles = null;
        }
        public Calculator()
        {
            InitZ();
            //CachePowers();
        }
        public void SetPrecision(int number)
        {
            this.numberSegments = number;
            clearCache();
        }
        public void SetM(int number)
        {
            this.m = number;
        }
        public void SetKdSlider(int number)
        {
            this.kd = number / 10.0;
        }
        public void InitZ()
        {
            /// kola (x-1/2)^2 + (y-1/2)^2 + z^2 = 1
            Z = new double[4, 4];
            double dist = 1 / 3.0;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    Z[i, j] = Math.Sqrt(1 - (((i * dist) - (1 / 2f)) * ((i * dist) - (1 / 2f))) - (((j * dist) - (1 / 2f)) * ((j * dist) - (1 / 2f))));
                    //Z[i, j] = 0;
                }
        }
        double B(int i, double t)
        {
            int[] choose = { 1, 3, 3, 1 };
            return choose[i] * Math.Pow(t, i) * Math.Pow(1 - t, 3 - i);
        }
        double z(double x, double y)
        {
            double sum = 0;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    sum += Z[i, j] * B(i, x) * B(j, y);
            return sum;
        }
        public double[,] GetValues()
        {
            double[,] values = new double[numberSegments + 1, numberSegments + 1];
            double distance = 1.0 / (numberSegments + 1);
            Parallel.For(0, (numberSegments + 1) * (numberSegments + 1), (int i) =>
            {
                int ix = i % (numberSegments + 1);
                int iy = i / (numberSegments + 1);
                double x = ix * distance;
                double y = iy * distance;
                values[ix, iy] = z(x, y);
            });
            return values;
        }
        public List<Vector3D[]> GetTriangles()
        {
            if (cachedTraingles != null)
                return cachedTraingles;

            Vector3D[,] points = GetPoints3();
            List<Vector3D[]> triangles = new List<Vector3D[]>(numberSegments * numberSegments * 2);
            for (int i = 0; i < numberSegments; i++)
                for (int j = 0; j < numberSegments; j++)
                {
                    // (i,j)    (i+1,j)
                    // (i,j+1)  (i+1,j+1)
                    triangles.Add(new[] { points[i, j], points[i + 1, j], points[i + 1, j + 1] });
                    triangles.Add(new[] { points[i, j], points[i, j + 1], points[i + 1, j + 1] });
                }
            cachedTraingles = triangles;
            return triangles;
        }
        public Vector3D[,] GetPoints3()
        {
            if (cachedPoints != null)
                return cachedPoints;
            Vector3D[,] points3 = new Vector3D[numberSegments + 1, numberSegments + 1];
            double distH = 1.0 / numberSegments;
            double distW = 1.0 / numberSegments;
            for (int i = 0; i <= numberSegments; i++)
                for (int j = 0; j <= numberSegments; j++)
                {
                    points3[i, j].X = i * distH;
                    points3[i, j].Y = j * distW;
                    points3[i, j].Z = z(points3[i, j].X, points3[i, j].Y);
                }
            cachedPoints = points3;
            return points3;
        }
        public Vector3D[,] GetNormalVectors(Vector3D[,] points3 = null)
        {
            if (cachedNormals != null)
                return cachedNormals;
            if (points3 == null)
                points3 = GetPoints3();
            Vector3D[,] normals = new Vector3D[numberSegments + 1, numberSegments + 1];

            for (int i = 0; i <= numberSegments; i++)
                for (int j = 0; j <= numberSegments; j++)
                {
                    //Vector3D Px;
                    //if (i == numberSegments)
                    //    Px = points3[i, j] - points3[i - 1, j];
                    //else
                    //    Px = points3[i + 1, j] - points3[i, j];
                    //Vector3D Py;
                    //if (j == numberSegments)
                    //    Py = points3[i, j] - points3[i, j - 1];
                    //else
                    //    Py = points3[i, j + 1] - points3[i, j];

                    //normals[i, j] = Vector3D.CrossProduct(Px, Py);
                    //double h = 0.01;
                    //double zdu = (z(points3[i, j].X + h, points3[i, j].Y) - z(points3[i, j].X, points3[i, j].Y)) / h;
                    //Vector3D Pu = new Vector3D(1, 0, zdu);
                    //double zdu =
                    // sphere normal
                    normals[i, j] = points3[i , j] - new Vector3D(0.5, 0.5, 0);
                }
            cachedNormals = normals;
            return normals;

        }
        public Vector3D[,] GetLightVectors(Vector3D lightPos, Vector3D[,] points3 = null)
        {
            if (points3 == null)
                points3 = GetPoints3();

            Vector3D[,] lightvectors = new Vector3D[numberSegments + 1, numberSegments + 1];
            for (int i = 0; i <= numberSegments; i++)
                for (int j = 0; j <= numberSegments; j++)
                    lightvectors[i, j] = lightPos - points3[i, j];
            return lightvectors;
        }
        public List<(double r, double g, double b)> CalculateColors(Vector3D lightPos)
        {
            Vector3D[,] points3 = GetPoints3();
            Vector3D[,] normals = GetNormalVectors(points3);
            Vector3D[,] lightvectores = GetLightVectors(lightPos);

            List<(double r, double g, double b)> colors = new List<(double r, double g, double b)>(numberSegments * numberSegments * 2);

            for (int i = 0; i < numberSegments; i++)
                for (int j = 0; j < numberSegments; j++)
                {
                    Vector3D normal1 = 1 / 3.0 * (normals[i, j] + normals[i + 1, j] + normals[i + 1, j + 1]);
                    Vector3D light1 = 1 / 3.0 * (lightvectores[i, j] + lightvectores[i + 1, j] + lightvectores[i + 1, j + 1]);
                    colors.Add(GetColor(normal1, light1));

                    Vector3D normal2 = 1 / 3.0 * (normals[i, j] + normals[i, j + 1] + normals[i + 1, j + 1]);
                    Vector3D light2 = 1 / 3.0 * (lightvectores[i, j] + lightvectores[i, j + 1] + lightvectores[i + 1, j + 1]);
                    colors.Add(GetColor(normal2, light2));
                }

            return colors;
        }
        public (double r, double g, double b) GetColor(Vector3D normal, Vector3D light)
        {
            normal.Normalize();
            light.Normalize();
            Vector3D V = new Vector3D(0, 0, 1);
            Vector3D R = (2 * Vector3D.DotProduct(normal, light) * normal) - light;

            double red = (kd * lightColor.r * objectColor.r * PositiveCos(normal, light)) + ((1 - kd) * lightColor.r * objectColor.r * Math.Pow(PositiveCos(V, R), m));

            double green = (kd * lightColor.g * objectColor.g * PositiveCos(normal, light)) + ((1 - kd) * lightColor.g * objectColor.g * Math.Pow(PositiveCos(V, R), m));

            double blue = (kd * lightColor.b * objectColor.b * PositiveCos(normal, light)) + ((1 - kd) * lightColor.b * objectColor.b * Math.Pow(PositiveCos(V, R), m));

            return (red, green, blue);
        }
        double PositiveCos(Vector3D a, Vector3D b)
        {
            return Math.Max(Vector3D.DotProduct(a, b) / a.Length / b.Length, 0);
        }
    }
    public partial class Drawer
    {
        static int height, width;
        static Calculator calc;
        //Pen pen = new Pen(Color.Black);
        static SolidBrush brush = new SolidBrush(Color.Black);
        System.Drawing.Color[,] colors; 
        public Drawer(int width, int height)
        {
            Drawer.height = height;
            Drawer.width = width;
            colors = new Color[width + 1, height + 1];
        }
        public void DrawPolygons(Graphics gfx, Calculator calculator, Vector3D lightPos)
        {
            Vector3D[,] points = calculator.GetPoints3();
            Vector3D[,] normalV = calculator.GetNormalVectors();
            Vector3D[,] lightV = calculator.GetLightVectors(lightPos);
            calc = calculator;

            for (int i = 0; i < calculator.numberSegments; i++)
                for (int j = 0; j < calculator.numberSegments; j++)
                {
                    // (i,j)    (i+1,j)
                    // (i,j+1)  (i+1,j+1)
                    //DrawPolygon(e,
                    //    new[] { points[i, j], points[i + 1, j], points[i + 1, j + 1] },
                    //    new[] { normalV[i, j], normalV[i + 1, j], normalV[i + 1, j + 1] },
                    //    new[] { lightV[i, j], lightV[i + 1, j], lightV[i + 1, j + 1] });
                    //DrawPolygon(e,
                    // new[] { points[i, j], points[i, j + 1], points[i + 1, j + 1] },
                    // new[] { normalV[i, j], normalV[i, j + 1], normalV[i + 1, j + 1] },
                    // new[] { lightV[i, j], lightV[i, j + 1], lightV[i + 1, j + 1] }
                    //);
                    ComputeColorsLineScan(gfx,
                        new[] { points[i, j], points[i + 1, j], points[i + 1, j + 1] },
                        new[] { normalV[i, j], normalV[i + 1, j], normalV[i + 1, j + 1] },
                        new[] { lightV[i, j], lightV[i + 1, j], lightV[i + 1, j + 1] });
                    ComputeColorsLineScan(gfx,
                             new[] { points[i, j], points[i, j + 1], points[i + 1, j + 1] },
                             new[] { normalV[i, j], normalV[i, j + 1], normalV[i + 1, j + 1] },
                             new[] { lightV[i, j], lightV[i, j + 1], lightV[i + 1, j + 1] }
                                );
                }
            for (int i = 0; i < width + 1; i++)
                for (int j = 0; j < height + 1; j++)
                {
                    brush.Color = colors[i, j];
                    gfx.FillRectangle(brush, i, j, 1, 1);
                }

        }
        void DrawPolygon(Graphics gfx, Vector3D[] Points, Vector3D[] normalVectors, Vector3D[] lightVectors)
        {
            Point[] P = Points.Select(v => new Point((int)(v.X * width), (int)(v.Y * height))).ToArray();
            int N = P.Length;
            HashSet<(Point a, Point b)> AET = new HashSet<(Point a, Point b)>();
            int[] ind = Enumerable.Range(0, N).OrderBy((int i) => P[i].Y).ToArray();
            int ymin = P[ind[0]].Y;
            int ymax = P[ind[N - 1]].Y;
            int k = 0; // indeks w ind[] wierzchołka z poprzedniej scanlini

            for (int i = 0; i < N; i++)
                if (P[i].Y == P[(i + 1) % N].Y)
                    for (int x = Math.Min(P[i].X, P[(i + 1) % N].X); x <= Math.Max(P[i].X, P[(i + 1) % N].X); x++)
                    {
                        DrawPixel(gfx, x, P[i].Y, P, normalVectors, lightVectors);
                    }
            //e.Graphics.DrawLine(pen, P[i], P[(i + 1) % N]);

            for (int y = ymin; y <= ymax; y++)
            {
                while (P[ind[k]].Y == y - 1)
                {
                    Point curr = P[ind[k]];

                    Point prev = P[(ind[k] - 1 + N) % N];
                    if (prev.Y > curr.Y)
                        AET.Add((prev, curr));
                    else
                        AET.Remove((prev, curr));

                    Point next = P[(ind[k] + 1) % N];
                    if (next.Y > curr.Y)
                        AET.Add((curr, next));
                    else
                        AET.Remove((curr, next));
                    k++;
                }

                IOrderedEnumerable<double> sortedXs = AET.Select(s => XAt(s.a, s.b, y)).OrderBy(x => x);

                int i = 0;
                double x1 = 0.0;
                double x2 = 0.0;
                using (IEnumerator<double> iter = sortedXs.GetEnumerator())
                {
                    while (iter.MoveNext())
                    {
                        if (i % 2 == 0)
                            x1 = iter.Current;
                        else if (i % 2 == 1)
                        {
                            x2 = iter.Current;
                            //e.Graphics.DrawLine(pen, (int)x1, y, (int)x2, y);
                            for(int x= (int)x1; x<= (int)x2; x++)
                            {
                                DrawPixel(gfx, x, y, P, normalVectors, lightVectors);
                            }
                        }
                        i++;
                    }
                }

            }
        }

        void ComputeColorsLineScan(Graphics gfx, Vector3D[] Points, Vector3D[] normalVectors, Vector3D[] lightVectors)
        {
            Point[] P = Points.Select(v => new Point((int)(v.X * width), (int)(v.Y * height))).ToArray();
            int N = P.Length;
            HashSet<(Point a, Point b)> AET = new HashSet<(Point a, Point b)>();
            int[] ind = Enumerable.Range(0, N).OrderBy((int i) => P[i].Y).ToArray();
            int ymin = P[ind[0]].Y;
            int ymax = P[ind[N - 1]].Y;
            int k = 0; // indeks w ind[] wierzchołka z poprzedniej scanlini

            for (int i = 0; i < N; i++)
                if (P[i].Y == P[(i + 1) % N].Y)
                    for (int x = Math.Min(P[i].X, P[(i + 1) % N].X); x <= Math.Max(P[i].X, P[(i + 1) % N].X); x++)
                    {
                        DrawPixel(gfx, x, P[i].Y, P, normalVectors, lightVectors);
                    }
            //e.Graphics.DrawLine(pen, P[i], P[(i + 1) % N]);

            for (int y = ymin; y <= ymax; y++)
            {
                while (P[ind[k]].Y == y - 1)
                {
                    Point curr = P[ind[k]];

                    Point prev = P[(ind[k] - 1 + N) % N];
                    if (prev.Y > curr.Y)
                        AET.Add((prev, curr));
                    else
                        AET.Remove((prev, curr));

                    Point next = P[(ind[k] + 1) % N];
                    if (next.Y > curr.Y)
                        AET.Add((curr, next));
                    else
                        AET.Remove((curr, next));
                    k++;
                }

                IOrderedEnumerable<double> sortedXs = AET.Select(s => XAt(s.a, s.b, y)).OrderBy(x => x);

                int i = 0;
                double x1 = 0.0;
                double x2 = 0.0;
                using (IEnumerator<double> iter = sortedXs.GetEnumerator())
                {
                    while (iter.MoveNext())
                    {
                        if (i % 2 == 0)
                            x1 = iter.Current;
                        else if (i % 2 == 1)
                        {
                            x2 = iter.Current;
                            //e.Graphics.DrawLine(pen, (int)x1, y, (int)x2, y);
                            //Parallel.For((int)x1, (int)x2 + 1, x => colors[x, y] = CompColor(x, y, P, normalVectors, lightVectors));
                            for (int x = (int)x1; x <= (int)x2; x++)
                            {
                                colors[x, y] = CompColor(x, y, P, normalVectors, lightVectors);
                            }
                        }
                        i++;
                    }
                }

            }
        }
        public static double XAt(Point a, Point b, int y)
        {
            double mx = (b.X - a.X) / ((double)(b.Y - a.Y));
            return a.X + (mx * (y - a.Y));
        }
        static void DrawPixel(Graphics gfx, int x, int y, Point[] P, Vector3D[] normalVectors, Vector3D[] lightVectors)
        {
            double a = (double)((P[1].Y * P[2].X - P[2].Y * P[1].X) + (P[2].Y * x - y * P[2].X) + (y * P[1].X - P[1].Y * x)) / (double)((P[1].Y * P[2].X - P[2].Y * P[1].X) + (P[2].Y * P[0].X - P[0].Y * P[2].X) + (P[0].Y * P[1].X - P[1].Y * P[0].X));
            double b = (double)((P[2].Y * P[0].X - P[0].Y * P[2].X) + (P[0].Y * x - y * P[0].X) + (y * P[2].X - P[2].Y * x)) / (double)((P[1].Y * P[2].X - P[2].Y * P[1].X) + (P[2].Y * P[0].X - P[0].Y * P[2].X) + (P[0].Y * P[1].X - P[1].Y * P[0].X));
            double c = 1 - a - b;
            Vector3D aproxN = a * normalVectors[0] + b * normalVectors[1] + c * normalVectors[2];
            Vector3D aproxL = a * lightVectors[0] + b * lightVectors[1] + c * lightVectors[2];
            (double r, double g, double b) color = calc.GetColor(aproxN, aproxL);
            brush.Color = Color.FromArgb(255, (int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255));
            gfx.FillRectangle(brush, x, y, 1, 1);
        }
    
    //Multithreading

        static BlockingCollection<(int x, int y, Color c)> linesQ;
        static object __lockObj = new object();
        static int polygonsNumber;

        public void DrawMultiThread(PaintEventArgs e, Calculator calculator, Vector3D lightPos)
        {
            calc = calculator;
            polygonsNumber = calc.numberSegments * calc.numberSegments * 2;
            linesQ = new BlockingCollection<(int x, int y, Color c)>();

            Vector3D[,] points = calculator.GetPoints3();
            Vector3D[,] normalV = calculator.GetNormalVectors();
            Vector3D[,] lightV = calculator.GetLightVectors(lightPos);

            Thread drawer = new Thread(() => DrawFromQ(e));
            drawer.Start();
            for (int i = 0; i < calculator.numberSegments; i++)
                for (int j = 0; j < calculator.numberSegments; j++)
                {
                    //ThreadPool.QueueUserWorkItem(new WaitCallback((state) => AddToQ(e,
                    //    new[] { points[i, j], points[i + 1, j], points[i + 1, j + 1] },
                    //    new[] { normalV[i, j], normalV[i + 1, j], normalV[i + 1, j + 1] },
                    //    new[] { lightV[i, j], lightV[i + 1, j], lightV[i + 1, j + 1] })));
                    //ThreadPool.QueueUserWorkItem(new WaitCallback((state) => AddToQ(e,
                    // new[] { points[i, j], points[i, j + 1], points[i + 1, j + 1] },
                    // new[] { normalV[i, j], normalV[i, j + 1], normalV[i + 1, j + 1] },
                    // new[] { lightV[i, j], lightV[i, j + 1], lightV[i + 1, j + 1] }
                    //    )));

                    AddToQ(e,
                        new[] { points[i, j], points[i + 1, j], points[i + 1, j + 1] },
                        new[] { normalV[i, j], normalV[i + 1, j], normalV[i + 1, j + 1] },
                        new[] { lightV[i, j], lightV[i + 1, j], lightV[i + 1, j + 1] });
                    AddToQ(e,
                     new[] { points[i, j], points[i, j + 1], points[i + 1, j + 1] },
                     new[] { normalV[i, j], normalV[i, j + 1], normalV[i + 1, j + 1] },
                     new[] { lightV[i, j], lightV[i, j + 1], lightV[i + 1, j + 1] }
                        );
                }

            drawer.Join();
        }
        static void AddToQ(PaintEventArgs e, Vector3D[] Points, Vector3D[] normalVectors, Vector3D[] lightVectors)
        {
            Point[] P = Points.Select(v => new Point((int)(v.X * width), (int)(v.Y * height))).ToArray();
            int N = P.Length;
            HashSet<(Point a, Point b)> AET = new HashSet<(Point a, Point b)>();
            int[] ind = Enumerable.Range(0, N).OrderBy((int i) => P[i].Y).ToArray();
            int ymin = P[ind[0]].Y;
            int ymax = P[ind[N - 1]].Y;
            int k = 0; // indeks w ind[] wierzchołka z poprzedniej scanlini

            for (int i = 0; i < N; i++)
                if (P[i].Y == P[(i + 1) % N].Y)
                    for (int x = Math.Min(P[i].X, P[(i + 1) % N].X); x <= Math.Max(P[i].X, P[(i + 1) % N].X); x++)
                    {
                        linesQ.Add((x, P[1].Y, CompColor(x, P[1].Y, P,normalVectors, lightVectors)));
                    }
 

            for (int y = ymin; y <= ymax; y++)
            {
                while (P[ind[k]].Y == y - 1)
                {
                    Point curr = P[ind[k]];

                    Point prev = P[(ind[k] - 1 + N) % N];
                    if (prev.Y > curr.Y)
                        AET.Add((prev, curr));
                    else
                        AET.Remove((prev, curr));

                    Point next = P[(ind[k] + 1) % N];
                    if (next.Y > curr.Y)
                        AET.Add((curr, next));
                    else
                        AET.Remove((curr, next));
                    k++;
                }

                IOrderedEnumerable<double> sortedXs = AET.Select(s => XAt(s.a, s.b, y)).OrderBy(x => x);

                int i = 0;
                double x1 = 0.0;
                double x2 = 0.0;
                using (IEnumerator<double> iter = sortedXs.GetEnumerator())
                {
                    while (iter.MoveNext())
                    {
                        if (i % 2 == 0)
                            x1 = iter.Current;
                        else if (i % 2 == 1)
                        {
                            x2 = iter.Current;
                            //e.Graphics.DrawLine(pen, (int)x1, y, (int)x2, y);
                            for (int x = (int)x1; x <= (int)x2; x++)
                            {
                                linesQ.Add((x, y, CompColor(x, y, P, normalVectors, lightVectors)));
                            }
                        }
                        i++;
                    }
                }

            }
            lock (__lockObj)
            {
                polygonsNumber--;
                if (polygonsNumber == 0) linesQ.CompleteAdding();
            }

        }
        static void DrawFromQ(PaintEventArgs e)
        {
            while (linesQ.TryTake(out (int x, int y, Color c) data))
            {
                SolidBrush sb = new SolidBrush(data.c);
                e.Graphics.FillRectangle(sb, data.x, data.y, 1, 1);
                sb.Dispose();
            }
        }
        static Color CompColor(int x, int y, Point[] P, Vector3D[] normalVectors, Vector3D[] lightVectors)
        {
            double a = (double)((P[1].Y * P[2].X - P[2].Y * P[1].X) + (P[2].Y * x - y * P[2].X) + (y * P[1].X - P[1].Y * x)) / (double)((P[1].Y * P[2].X - P[2].Y * P[1].X) + (P[2].Y * P[0].X - P[0].Y * P[2].X) + (P[0].Y * P[1].X - P[1].Y * P[0].X));
            double b = (double)((P[2].Y * P[0].X - P[0].Y * P[2].X) + (P[0].Y * x - y * P[0].X) + (y * P[2].X - P[2].Y * x)) / (double)((P[1].Y * P[2].X - P[2].Y * P[1].X) + (P[2].Y * P[0].X - P[0].Y * P[2].X) + (P[0].Y * P[1].X - P[1].Y * P[0].X));
            double c = 1 - a - b;
            Vector3D aproxN = a * normalVectors[0] + b * normalVectors[1] + c * normalVectors[2];
            Vector3D aproxL = a * lightVectors[0] + b * lightVectors[1] + c * lightVectors[2];
            (double r, double g, double b) color = calc.GetColor(aproxN, aproxL);
            return Color.FromArgb((int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255));
        }
    }

}
