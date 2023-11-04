using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
        Drawer drawer;
        Calculator calculator;
        List<Point[]> triangles;
        List<(double r, double g, double b)> colors;
        (double r, double g, double b) objectColor = (1,0,0);
        (double r, double g, double b) lightColor = (1,1,1);
        double kd = 1;
        double ks = 0;
        int frame = 0;
        Vector3D lightPos = new Vector3D(1.5,0.5,5);
        double lightRadius = 1.0;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        public Zadanie2()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            InitializeComponent();

            drawer = new Drawer(Canvas.Width, Canvas.Height);
            calculator = new Calculator(Canvas.Width, Canvas.Height);
            calculator.SetPrecision(100);
            triangles = calculator.GetTriangles();

            //Canvas.Invalidate();
            timer.Interval = (40); // 25 FPS
            timer.Tick += new EventHandler(RenderFrame);
            timer.Start();
        }
        private void RenderFrame(object sender, EventArgs e)
        {
            UpdateLight();
            
            Canvas.Invalidate();
            frame++;
        }
        private void UpdateLight()
        {
            lightPos.X = lightRadius * Math.Cos(frame * 40f / 5000f * 2f*Math.PI) + 0.5;               
            lightPos.Y = lightRadius * Math.Sin(frame * 40f / 5000f * 2f * Math.PI) + 0.5;
            
        }
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.Black), (int)lightPos.X, (int)lightPos.Y, 10, 10);
            List<(double r, double g, double b)> colors = calculator.CalculateColors(lightPos);
            //drawer.DrawPolygons(e, triangles, colors);
            drawer.DrawMultiThread(e, triangles, colors);

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
        int numberSegments;
        int width;
        int height;
        double kd = 0.6;
        int m = 30;
        (double r, double g, double b) lightColor = (1,1,1);
        (double r, double g, double b) objectColor = (0.8,0.2,0.3);
        double[,] cachedIndPow;
        Vector3D[,] cachedPoints;

        void CachePowers()
        {
            cachedIndPow = new double[numberSegments + 1, 4];
            double dist = 1.0 / numberSegments;
            for(int i=0; i < numberSegments; i++)
            {
                cachedIndPow[i, 0] = 1;
                for (int j = 1; j <= 3; j++)
                    cachedIndPow[i, j] = cachedIndPow[i, j-1] * dist * i;
            }
        }
        public Calculator(double[,] Z, int width, int height)
        {
            if (!(Z.GetLength(0) == 4 && Z.GetLength(1) == 4))
                throw new Exception("Zły rozmiar Z!");
            this.Z = Z;
            this.width = width;
            this.height = height;
            CachePowers();
        }
        public Calculator(int width, int height)
        {
            InitZ();
            this.width = width;
            this.height = height; 
            CachePowers();
        }
        public void SetPrecision(int number)
        {
            this.numberSegments = number;
            CachePowers();
            cachedPoints = null;
        }
        public void SetM(int number)
        {
            this.m = number;
        }
        public void SetKdSlider(int number)
        {
            this.kd = number/10.0;
        }
        public void InitZ()
        {
            /// kola (x-1/2)^2 + (y-1/2)^2 + z^2 = 1
            Z = new double[4, 4];
            double dist = 1 / 3.0;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    Z[i, j] = Math.Sqrt(1 - (i * dist - 1 / 2f) * (i * dist - 1 / 2f) - (j * dist - 1 / 2f) * (j * dist - 1 / 2f));
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
        //double ZInd(int inx, int iny)
        //{
        //    double sum = 0;
        //    for (int i = 0; i < 4; i++)
        //        for (int j = 0; j < 4; j++)
        //            sum += Z[i, j] * BInd(i, inx) * BInd(j, iny);
        //    return sum;
        //}
        //double BInd(int i, int indt)
        //{
        //    int[] choose = { 1, 3, 3, 1 };
        //    return choose[i] * cachedIndPow[indt,i] * cachedIndPow[numberSegments - indt, i];
        //}
        public double[,] GetValues()
        {
            double[,] values = new double[numberSegments+1, numberSegments+1];
            double distance = 1.0 / (numberSegments+1);
            Parallel.For(0, (numberSegments+1) * (numberSegments + 1), (int i) =>
            {
                int ix = i % (numberSegments + 1);
                int iy = i / (numberSegments + 1);
                double x = ix * distance;
                double y = iy * distance;
                values[ix, iy] = z(x, y);
            });
            return values;
        }
        public List<Point[]> GetTriangles()
        {
            Point[,] points = new Point[numberSegments+1, numberSegments+1];
            double distH = height / numberSegments;
            double distW = width / numberSegments;
            for (int i = 0; i <= numberSegments; i++)
                for (int j = 0; j <= numberSegments; j++)
                {
                    points[i, j].X = (int)(i * distH);
                    points[i, j].Y = (int)(j * distW);
                }
            List<Point[]> triangles = new List<Point[]>(numberSegments * numberSegments * 2);
            for (int i = 0; i < numberSegments; i++)
                for (int j = 0; j < numberSegments; j++)
                {
                    // (i,j)    (i+1,j)
                    // (i,j+1)  (i+1,j+1)
                    triangles.Add(new[] { points[i, j], points[i+1, j], points[i+1, j+1]});
                    triangles.Add(new[] { points[i, j], points[i, j+1], points[i+1, j+1]});
                }
            return triangles;
        }
        public Vector3D[,] GetPonts3()
        {
            //double[,] values = GetValues();
            if(cachedPoints != null) return cachedPoints;
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
        public Vector3D[,] GetNormalVectors()
        {
            Vector3D[,] points3 = GetPonts3();
            Vector3D[,] normals = new Vector3D[numberSegments + 1, numberSegments + 1];

            for(int i = 0; i <= numberSegments; i++)
                for(int  j = 0; j <= numberSegments; j++)
                {
                    Vector3D Px;
                    if (i == numberSegments)
                        Px = points3[i, j] - points3[i - 1, j];
                    else
                        Px = points3[i + 1, j] - points3[i, j];
                    Vector3D Py;
                    if (j == numberSegments)
                        Py = points3[i, j] - points3[i, j - 1];
                    else
                        Py = points3[i, j + 1] - points3[i, j];

                    normals[i, j] = Vector3D.CrossProduct(Px, Py);

                    // sphere normal
                    //normals[i, j] = points3[i , j] - new Vector3D(0.5, 0.5, 0);
                }

                return normals;

        }
        public Vector3D[,] GetLightVectors(Vector3D[,] points3, Vector3D lightPos)
        {
            Vector3D[,] lightvectors = new Vector3D[numberSegments+1, numberSegments+1];
            for (int i = 0; i <= numberSegments; i++)
                for (int j = 0; j <= numberSegments; j++)
                    lightvectors[i,j] = lightPos - points3[i, j];
            return lightvectors;
        }
        public List<(double r, double g, double b)> CalculateColors( Vector3D lightPos)
        {
            Vector3D[,] points3 = GetPonts3();
            Vector3D[,] normals = GetNormalVectors();
            Vector3D[,] lightvectores = GetLightVectors(points3, lightPos);
            List<(double r, double g, double b)> colors = new List<(double r, double g, double b)>(numberSegments * numberSegments * 2);

            for (int i = 0; i < numberSegments; i++)
                for (int j = 0; j < numberSegments; j++)
                {                   
                    Vector3D normal1 = 1 / 3.0 * (normals[i, j] + normals[i + 1, j] + normals[i + 1, j + 1]);
                    Vector3D light1 = 1 / 3.0 * (lightvectores[i, j] + lightvectores[i + 1, j] + lightvectores[i + 1, j + 1]);
                    colors.Add(GetColor(normal1, light1));

                    Vector3D normal2 = 1 / 3.0 * (normals[i, j] + normals[i, j+1] + normals[i + 1, j + 1]);
                    Vector3D light2 = 1 / 3.0 * (lightvectores[i, j] + lightvectores[i, j + 1] + lightvectores[i + 1, j + 1]);
                    colors.Add(GetColor(normal2, light2));
                }

            return colors;
        }
        public (double r, double g, double b) GetColor(Vector3D normal, Vector3D light)
        {
            normal.Normalize();
            light.Normalize();
            Vector3D V = new Vector3D(0,0,1);
            Vector3D R = 2 * Vector3D.DotProduct(normal, light) * normal - light;

            double red = kd * lightColor.r * objectColor.r * PositiveCos(normal, light) + (1-kd)*lightColor.r * objectColor.r*Math.Pow(PositiveCos(V, R), m);

            double green = kd * lightColor.g * objectColor.g * PositiveCos(normal, light) + (1 - kd) * lightColor.g * objectColor.g * Math.Pow(PositiveCos(V, R), m);

            double blue = kd * lightColor.b * objectColor.b * PositiveCos(normal, light) + (1 - kd) * lightColor.b * objectColor.b * Math.Pow(PositiveCos(V, R), m);

            return (red, green, blue);
        }

        double PositiveCos(Vector3D a, Vector3D b)
        {
            return Math.Max(Vector3D.DotProduct(a, b) / a.Length / b.Length, 0);
        }
    }
    public class Drawer
    {
        int height, width;

        static BlockingCollection<(int x1, int x2, int y, Color)> linesQ = new BlockingCollection<(int x1, int x2, int y, Color)>(); 
        static object __lockObj = new object();
        static int polygonsNumber;
        public Drawer(int width, int height)
        {
            this.height = height;
            this.width = width;
        }
        public void DrawPolygons(PaintEventArgs e, IList<Point[]> polygons, List<(double r, double g, double b)> colors)
        {
            for(int i=0; i < polygons.Count; i++)
                DrawPolygon(e, polygons[i], colors[i]);

        }
        
        public void DrawPolygon(PaintEventArgs e, Point[] P, (double r, double g, double b) color)
        {
            int N = P.Length;
            HashSet<(Point a, Point b)> AET = new HashSet<(Point a, Point b)>();
            Pen pen = new Pen(Color.FromArgb(255, (int)(color.r*255), (int)(color.g *255), (int)(color.b *255)));
            int[] ind = Enumerable.Range(0, N).OrderBy((int i) => P[i].Y).ToArray();
            int ymin = P[ind[0]].Y;
            int ymax = P[ind[N - 1]].Y;
            int k = 0; // indeks w ind[] wierzchołka z poprzedniej scanlini

            for (int i = 0; i < N; i++)
                if (P[i].Y == P[(i + 1) % N].Y)
                    e.Graphics.DrawLine(pen, P[i], P[(i + 1) % N]);

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
                using (var iter = sortedXs.GetEnumerator())
                {
                    while (iter.MoveNext())
                    {
                        if (i % 2 == 0)
                            x1 = iter.Current;
                        else if (i % 2 == 1)
                        {
                            x2 = iter.Current;
                            e.Graphics.DrawLine(pen, (int)x1, y, (int)x2, y);
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

        public void DrawMultiThread(PaintEventArgs e, IList<Point[]> polygons, List<(double r, double g, double b)> colors)
        {
            polygonsNumber = polygons.Count;
            Thread drawer = new Thread(() => DrawLines(e));
            drawer.Start();
            for (int i = 0; i < polygons.Count; i++)
            {
                //Thread t = new Thread(() => AddLinesToQ(e, polygons[i], colors[i]));
                //t.Start();
                //ThreadPool.QueueUserWorkItem(new WaitCallback( (object state) => AddLinesToQ(e, polygons[i], colors[i])) );
                AddLinesToQ(e, polygons[i], colors[i]);
            }            
            drawer.Join();
            linesQ = new BlockingCollection<(int x1, int x2, int y, Color)>();
        }
        static void AddLinesToQ(PaintEventArgs e, Point[] P, (double r, double g, double b) color)
        {
            int N = P.Length;
            HashSet<(Point a, Point b)> AET = new HashSet<(Point a, Point b)>();
            Color col = Color.FromArgb(255, (int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255));
            int[] ind = Enumerable.Range(0, N).OrderBy((int i) => P[i].Y).ToArray();
            int ymin = P[ind[0]].Y;
            int ymax = P[ind[N - 1]].Y;
            int k = 0;

            for (int i = 0; i < N; i++)
                if (P[i].Y == P[(i + 1) % N].Y)
                    linesQ.Add(((int)P[i].X, (int)P[(i + 1) % N].X, P[i].Y, col));

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
                using (var iter = sortedXs.GetEnumerator())
                {
                    while (iter.MoveNext())
                    {
                        if (i % 2 == 0)
                            x1 = iter.Current;
                        else if (i % 2 == 1)
                        {
                            x2 = iter.Current;
                            linesQ.Add(((int)x1, (int)x2, y, col));
                        }
                        i++;
                    }
                }

            }
            lock(__lockObj)
            {
                polygonsNumber--;
                if(polygonsNumber == 0) linesQ.CompleteAdding();
            }
        }

        static void DrawLines(PaintEventArgs e)
        {
            while (linesQ.TryTake(out (int x1, int x2, int y, Color c) line))
            {
                Pen pen = new Pen(line.c);
                e.Graphics.DrawLine(pen, line.x1, line.y, line.x2, line.y);
            }           
        }
        // wzór z ASD2
        public static bool ChckInstersect(Point a, Point b, Point c, Point d)
        {
            double d1 = CrossProduct(a, b, c);
            double d2 = CrossProduct(a, b, d);
            double d3 = CrossProduct(c, d, a);
            double d4 = CrossProduct(c, d, b);

            return d1 * d2 < 0 && d3 * d4 < 0;
        }
        private static double CrossProduct(Point p1, Point p2, Point p3)
        {
            return ((p2.X - p1.X) * (p3.Y - p1.Y)) - ((p2.Y - p1.Y) * (p3.X - p1.X));
        }


    }

}
