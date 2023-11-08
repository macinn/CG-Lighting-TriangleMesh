using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

// TODO:
// -źródło światła wspolrzedna z, radius, kolor obiaktu, swiatla do wybrania

// -modyfikacja wektora normalnego na podstawie wczytanej mapy wektorów normalnych


namespace Zadanie2GK
{
    public partial class Zadanie2 : Form
    {
        readonly Drawer drawer;
        readonly Calculator calculator;
        uint frame = 0;
        Vector3D lightPos = new Vector3D(1.5, 0.5, 5);
        readonly double lightRadius = 10.0;
        readonly Stopwatch stopWatch = new Stopwatch();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        double lastFPS = 0;
        byte[] buffer;
        public Zadanie2()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            InitializeComponent();

            drawer = new Drawer(Canvas.Width, Canvas.Height);
            calculator = new Calculator();
            calculator.SetPrecision(30);

            timer.Interval = 40; // 25 FPS
            timer.Tick += new EventHandler(RenderFrame);
            timer.Start();
        }        
        private void RenderFrame(object sender, EventArgs e)
        {
            UpdateLight();
            
            stopWatch.Restart();

            //// https://stackoverflow.com/questions/21497537/allow-an-image-to-be-accessed-by-several-threads
            Bitmap bmp = new Bitmap(Canvas.Width, Canvas.Height);
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            var depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8; //bytes per pixel 
            if(buffer == null)
                buffer = new byte[Canvas.Width * data.Height * depth];
            drawer.DrawPolygons(buffer, depth, calculator, lightPos);
            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            bmp.UnlockBits(data);
            Canvas.Image = bmp;
            //Canvas.Invalidate();

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
        static Vector3D V = new Vector3D(0, 0, 1);
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

            Vector3D R = (2 * Vector3D.DotProduct(normal, light) * normal) - light;
            R.Normalize();

            double cosNL = PositiveCos(normal, light);
            double cosVRM = Math.Pow(PositiveCos(V, R), m);
            double loR = lightColor.r * objectColor.r;
            double loG = lightColor.g * objectColor.g;
            double loB = lightColor.b * objectColor.b;

            double red = (kd * loR * cosNL) + ((1 - kd) * loR * cosVRM);
            double green = (kd * loG * cosNL) + ((1 - kd) * loG * cosVRM);
            double blue = (kd * loB * cosNL) + ((1 - kd) * loB * cosVRM);

            return (red, green, blue);
        }
        double PositiveCos(Vector3D a, Vector3D b)
        {
            // a and b are versors
            return Math.Max(Vector3D.DotProduct(a, b), 0);
        }
    }
    public partial class Drawer
    {
        static int height, width;
        static Calculator calc;

        int numberOfSegmentsCache = -1;

        bool saveCache = false;
        (double a, double b, double c)[,] cacheCoefs = null;
        Vector3D[,] lastLightV;

        public Drawer(int width, int height)
        {
            Drawer.height = height;
            Drawer.width = width;
            lastLightV = new Vector3D[width, height];
        }
        public void DrawPolygons(byte[] buffor, int depth, Calculator calculator, Vector3D lightPos)
        {
            Vector3D[,] points = calculator.GetPoints3();
            Vector3D[,] normalV = calculator.GetNormalVectors();
            Vector3D[,] lightV = calculator.GetLightVectors(lightPos);
            calc = calculator;
            if(numberOfSegmentsCache != calc.numberSegments)
            {
                numberOfSegmentsCache = calc.numberSegments;
                cacheCoefs = new (double a, double b, double c)[width, height];
                saveCache = true;
            }

                Parallel.For(0, calculator.numberSegments* calculator.numberSegments, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 }, k =>
                {
                    int i = k % calc.numberSegments;
                    int j = k / calc.numberSegments;
                    //for (int j = 0; j < calculator.numberSegments; j++)
                    {
                        DrawPolygon(buffor, depth,
                            new[] { points[i, j], points[i + 1, j], points[i + 1, j + 1] },
                            new[] { normalV[i, j], normalV[i + 1, j], normalV[i + 1, j + 1] },
                            new[] { lightV[i, j], lightV[i + 1, j], lightV[i + 1, j + 1] });
                        DrawPolygon(buffor, depth,
                         new[] { points[i, j], points[i, j + 1], points[i + 1, j + 1] },
                         new[] { normalV[i, j], normalV[i, j + 1], normalV[i + 1, j + 1] },
                         new[] { lightV[i, j], lightV[i, j + 1], lightV[i + 1, j + 1] }
                        );
                    }
                });

            
            saveCache = false;
        }
        void DrawPolygon(byte[] buffor, int depth, Vector3D[] Points, Vector3D[] normalVectors, Vector3D[] lightVectors)
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
                        DrawPixel(buffor, depth, x, P[i].Y, P, normalVectors, lightVectors);
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

                            if (calc.numberSegments < 5)
                            {
                                Parallel.For((int)x1, (int)x2 + 1, x =>
                                    {
                                        DrawPixel(buffor, depth, x, y, P, normalVectors, lightVectors);
                                    });
                            }
                            else
                            {
                                for (int x = (int)x1; x <= (int)x2; x++)
                                {
                                    DrawPixel(buffor, depth, x, y, P, normalVectors, lightVectors);
                                }
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
        void DrawPixel(byte[] buffor, int depth, int x, int y, Point[] P, Vector3D[] normalVectors, Vector3D[] lightVectors)
        {
            if (x >= width || y >= height) 
                return;
            double a, b, c;
            if (!saveCache)
            {
                (a, b, c) = cacheCoefs[x, y];
            }
            else
            {
                double denom = ((P[1].Y * P[2].X - P[2].Y * P[1].X) + (P[2].Y * P[0].X - P[0].Y * P[2].X) + (P[0].Y * P[1].X - P[1].Y * P[0].X));
                a = ((P[1].Y * P[2].X - P[2].Y * P[1].X) + (P[2].Y * x - y * P[2].X) + (y * P[1].X - P[1].Y * x)) / denom;
                b = ((P[2].Y * P[0].X - P[0].Y * P[2].X) + (P[0].Y * x - y * P[0].X) + (y * P[2].X - P[2].Y * x)) / denom;
                c = 1 - a - b;
                cacheCoefs[x, y] = (a, b, c);
            }
            Vector3D aproxL = a * lightVectors[0] + b * lightVectors[1] + c * lightVectors[2];
            //if (vectorDist(aproxL, lastLightV[x, y]) < 0.01) return;
            lastLightV[x,y] = aproxL;
            Vector3D aproxN = a * normalVectors[0] + b * normalVectors[1] + c * normalVectors[2];
            (double r, double g, double b) color = calc.GetColor(aproxN, aproxL);
            int offset = ((y * width) + x) * depth;
            buffor[offset + 3] = 255;
            buffor[offset + 2] = (byte)(color.r * 255);
            buffor[offset + 1] = (byte)(color.g * 255);
            buffor[offset + 0] = (byte)(color.b * 255);
        }
        double vectorDist(Vector3D a, Vector3D b)
        {
            return (a.Z - b.Z) * (a.Z - b.Z) + (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }
    }

}
