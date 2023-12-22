using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using static Zadanie2GK.Zadanie2.Drawer;

namespace Zadanie2GK
{
    public partial class Zadanie2 : Form
    {
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly Stopwatch stopWatch = new Stopwatch();
        readonly Drawer drawer;
        readonly Calculator calculator;

        Action<byte[], int, Calculator, Vector3D> drawFunction;
        uint frame = 0;
        double lastFPS = 0;
        static double lightRadius = 2.0;
        static Vector3D lightPos = new Vector3D(0.5 + lightRadius, 0.5, 5);

        byte[] buffer;
        Bitmap bmpA, bmpB;
        int depth;
        bool isAUsed = false;

        public bool rysujSfere { set { calculator.rysujSfere = value; drawer.cachedNormals = null; } }
        public int numXSegments { set { calculator.SetPrecision( value, calculator.numberYSegments); } }
        public int numYSegments { set { calculator.SetPrecision(calculator.numberXSegments, value); } }
        public int m { set { calculator.m = value; } }
        public double kd { set { calculator.kd = value; } }
        public double ks { set { calculator.ks = value; } }
        public string normalMap { set { drawer.LoadBitmap(value); } }
        public Color lightColor { set { calculator.lightColor = (value.R/255.0, value.G/255.0, value.B / 255.0); } }
        public Color objectColor { set { calculator.objectColor = (value.R / 255.0, value.G / 255.0, value.B / 255.0); drawer.LoadColorMap(null); } }
        public Drawer.NormalMap mapMode { set { drawer.SetMapState(value); } }       
        public double lightR { set { lightRadius = value; } }
        public double lightH { set { lightPos.Z = value; } }
        public double[,] Z { set { calculator.SetZ(value); }  get { return calculator.Z; } }
        public bool drawOnlyTraingles { set {
                if (!value)
                    drawFunction = (buffer, depth, calculator, lightPos)
                        => drawer.DrawPolygons(buffer, depth, calculator, lightPos);
                else
                    drawFunction = (buffer, depth, calculator, lightPos)
                        =>{Array.Clear(buffer, 0, buffer.Length);
                            drawer.DrawBorders(buffer, depth, calculator, lightPos);};
                if (buffer != null) Array.Clear(buffer, 0, buffer.Length);
            } }
        public string colorMap { set { drawer.LoadColorMap(value); } }
        public Zadanie2(int numXSegments, int numYSegments, int m, double kd, double ks, Color lightColor, Color objectColor)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            InitializeComponent();

            //this.Width = 1000;
            //this.Height = 1000;
            //Canvas.Width = 1000;
            //Canvas.Height = 1000;

            drawOnlyTraingles = false;

            bmpA = new Bitmap(Canvas.Width, Canvas.Height);
            bmpB = new Bitmap(Canvas.Width, Canvas.Height);
            if (bmpA.PixelFormat != bmpB.PixelFormat)
                throw new Exception("formaty inne?");
            depth = Bitmap.GetPixelFormatSize(bmpA.PixelFormat) / 8;
            buffer = new byte[Canvas.Width * Canvas.Height * depth];
            Canvas.Image = bmpA;
            isAUsed = true;

            drawer = new Drawer(Canvas.Width, Canvas.Height);
            calculator = new Calculator();
            this.numXSegments = numXSegments;
            this.numYSegments = numYSegments;
            this.m = m;
            this.kd = kd;
            this.ks = ks;
            this.lightColor = lightColor;
            this.objectColor = objectColor;

            //drawer.LoadBitmap("C:\\Users\\marci\\Downloads\\NormalMap.png");
            //drawer.LoadBitmap("C:\\Users\\marci\\OneDrive - Politechnika Warszawska\\Pulpit\\brick_normalmap.png");
            //drawer.LoadBitmap("C:\\Users\\marci\\source\\repos\\Zadanie2GK\\Zadanie2GK\\bin\\Debug\\normal_map.jpg");

            timer.Interval = 25; // 40 FPS
            timer.Tick += new EventHandler(RenderFrame);
            timer.Start();
        }
        public void StopDrawing()
        {
            timer.Stop();
        }
        public void StartDrawing()
        {
            timer.Start();
        }
        private void RenderFrame(object sender, EventArgs e)
        {
            UpdateLight();

            stopWatch.Restart();

            //// https://stackoverflow.com/questions/21497537/allow-an-image-to-be-accessed-by-several-threads
            // podwojne buforowanie
            Rectangle rect = new Rectangle(0, 0, Canvas.Width, Canvas.Height);
            if (isAUsed)
            {              
                BitmapData data = bmpB.LockBits(rect, ImageLockMode.ReadWrite, bmpB.PixelFormat);
                drawFunction(buffer, depth, calculator, lightPos);
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bmpB.UnlockBits(data);
                Canvas.Image = bmpB;
            }
            else
            {
                BitmapData data = bmpA.LockBits(rect, ImageLockMode.ReadWrite, bmpA.PixelFormat);
                drawFunction(buffer, depth, calculator, lightPos);
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bmpA.UnlockBits(data);
                Canvas.Image = bmpA;
            }
            isAUsed = !isAUsed;

            stopWatch.Stop();
            lastFPS += 1000.0 / stopWatch.ElapsedMilliseconds;
            lastFPS /= 2;
            this.Text = $"Display FPS: {lastFPS.ToString("0.00")} ({stopWatch.ElapsedMilliseconds} ms)";
            frame++;
        }
        private void UpdateLight()
        {
            lightPos.X = (lightRadius * Math.Cos(frame * 40f / 5000f * 2f * Math.PI)) + 0.5;
            lightPos.Y = (lightRadius * Math.Sin(frame * 40f / 5000f * 2f * Math.PI)) + 0.5;
        }
        public class Calculator
        {
            public double[,] Z;
            public int numberXSegments;
            public int numberYSegments;
            public double kd = 0.2;
            public double ks = 0.8;
            public int m = 30;
            public (double r, double g, double b) lightColor = (1, 1, 1);
            public (double r, double g, double b) objectColor = (1, 0, 0);
            bool labySfera = false;
            public bool rysujSfere { set { labySfera = value; clearCache(); } }
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
            }
            public void SetPrecision(int numberX, int numberY)
            {
                this.numberXSegments = numberX;
                this.numberYSegments = numberY;
                clearCache();
            }
            public void SetZ(double[,] Z)
            {
                this.Z = Z;
                clearCache();
            }
            public void InitZ()
            {
                /// kola (x-1/2)^2 + (y-1/2)^2 + z^2 = 1
                Z = new double[4, 4];
                Random rand = new Random();
                double dist = 1 / 3.0;
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 4; j++)
                    {
                        //Z[i, j] = Math.Sqrt(1 - (((i * dist) - (1 / 2f)) * ((i * dist) - (1 / 2f))) - (((j * dist) - (1 / 2f)) * ((j * dist) - (1 / 2f))));
                        Z[i, j] = 0;
                        //Z[i, j] = rand.NextDouble()*10;
                    }
            }
            double B(int i, double t)
            {
                int[] choose = { 1, 3, 3, 1 };
                return choose[i] * Math.Pow(t, i) * Math.Pow(1 - t, 3 - i);
            }
            double z(double x, double y)
            {
                if (!labySfera)
                {
                    double sum = 0;
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 4; j++)
                            sum += Z[i, j] * B(i, x) * B(j, y);
                    return sum;
                }
                else
                {
                    Vector3D srodekKuli = new Vector3D(0.5, 0.5, 0.0);
                    if (0.25 - (x - srodekKuli.X) * (x - srodekKuli.X) - (y - srodekKuli.Y) * (y - srodekKuli.Y) < 0)
                        return 0.0;

                    double z = Math.Sqrt(0.25 - (x - srodekKuli.X) * (x - srodekKuli.X) - (y - srodekKuli.Y) * (y - srodekKuli.Y));

                    return Math.Max(0, z);
                }
                //if(z < 0)
                //    return 0;
                //else
                //    {

                //    }
            }
            public double[,] GetValues()
            {
                double[,] values = new double[numberXSegments + 1, numberYSegments + 1];
                double distanceX = 1.0 / (numberXSegments + 1);
                double distanceY = 1.0 / (numberYSegments + 1);
                Parallel.For(0, (numberXSegments + 1) * (numberYSegments + 1), (int i) =>
                {
                    int ix = i % (numberXSegments + 1);
                    int iy = i / (numberXSegments + 1);
                    double x = ix * distanceX;
                    double y = iy * distanceY;
                    values[ix, iy] = z(x, y);
                });
                return values;
            }
            public List<Vector3D[]> GetTriangles()
            {
                if (cachedTraingles != null)
                    return cachedTraingles;

                Vector3D[,] points = GetPoints3();
                List<Vector3D[]> triangles = new List<Vector3D[]>(numberXSegments * numberYSegments * 2);
                for (int i = 0; i < numberXSegments; i++)
                    for (int j = 0; j < numberYSegments; j++)
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
                Vector3D[,] points3 = new Vector3D[numberXSegments + 1, numberYSegments + 1];
                double distX = 1.0 / numberXSegments;
                double distY = 1.0 / numberYSegments;
                for (int i = 0; i <= numberXSegments; i++)
                    for (int j = 0; j <= numberYSegments; j++)
                    {
                        points3[i, j].X = i * distX;
                        points3[i, j].Y = j * distY;
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
                Vector3D[,] normals = new Vector3D[numberXSegments + 1, numberYSegments + 1];

                for (int i = 0; i <= numberXSegments; i++)
                    for (int j = 0; j <= numberYSegments; j++)
                    {

                        //double z = this.z(points3[i, j].X, points3[i, j].Y);
                        //if (z == 0)
                        //    normals[i, j] = new Vector3D(0, 0, 1);
                        //else
                        //{
                        //    Vector3D v = new Vector3D(points3[i, j].X, points3[i, j].Y, z);
                        //    Vector3D srodekKuli = new Vector3D(0.5, 0.5, 0.0);
                        //    normals[i, j] = v - srodekKuli; normals[i, j].Normalize();
                        //}
                        //if (true)
                        //{
                            double h = 0.01;
                            double zdu = (z(points3[i, j].X + h, points3[i, j].Y) - z(points3[i, j].X, points3[i, j].Y)) / h;
                            double zdv = (z(points3[i, j].X, points3[i, j].Y + h) - z(points3[i, j].X, points3[i, j].Y)) / h;
                            Vector3D Pu = new Vector3D(1, 0, zdu);
                            Vector3D Pv = new Vector3D(0, 1, zdv);
                            normals[i, j] = Vector3D.CrossProduct(Pu, Pv);
                            normals[i, j].Normalize();
                        //}
                        //else
                        //{
                        //    double z = this.z(points3[i, j].X, points3[i, j].Y);
                        //    if (z == 0)
                        //        normals[i, j] = new Vector3D(0, 0, 1);
                        //    else
                        //    {
                        //        Vector3D v = new Vector3D(points3[i, j].X, points3[i, j].Y, z);
                        //        Vector3D srodekKuli = new Vector3D(0.5, 0.5, 0.0);
                        //        normals[i, j] = v - srodekKuli; normals[i, j].Normalize();
                        //    }
                        //}
                    }
                cachedNormals = normals;
                return normals;

            }
            public Vector3D[,] GetLightVectors(Vector3D lightPos, Vector3D[,] points3 = null)
            {
                if (points3 == null)
                    points3 = GetPoints3();

                Vector3D[,] lightvectors = new Vector3D[numberXSegments + 1, numberYSegments + 1];
                for (int i = 0; i <= numberXSegments; i++)
                    for (int j = 0; j <= numberYSegments; j++)
                    {
                        lightvectors[i, j] = lightPos - points3[i, j];
                        lightvectors[i, j].Normalize();
                    }
                return lightvectors;
            }
            //public List<(double r, double g, double b)> CalculateColors(Vector3D lightPos)
            //{
            //    Vector3D[,] points3 = GetPoints3();
            //    Vector3D[,] normals = GetNormalVectors(points3);
            //    Vector3D[,] lightvectores = GetLightVectors(lightPos);

            //    List<(double r, double g, double b)> colors = new List<(double r, double g, double b)>(numberXSegments * numberYSegments * 2);

            //    for (int i = 0; i < numberXSegments; i++)
            //        for (int j = 0; j < numberYSegments; j++)
            //        {
            //            Vector3D normal1 = 1 / 3.0 * (normals[i, j] + normals[i + 1, j] + normals[i + 1, j + 1]);
            //            Vector3D light1 = 1 / 3.0 * (lightvectores[i, j] + lightvectores[i + 1, j] + lightvectores[i + 1, j + 1]);
            //            colors.Add(GetColor(normal1, light1));

            //            Vector3D normal2 = 1 / 3.0 * (normals[i, j] + normals[i, j + 1] + normals[i + 1, j + 1]);
            //            Vector3D light2 = 1 / 3.0 * (lightvectores[i, j] + lightvectores[i, j + 1] + lightvectores[i + 1, j + 1]);
            //            colors.Add(GetColor(normal2, light2));
            //        }

            //    return colors;
            //}
            public (double r, double g, double b) GetColor(Vector3D normal, Vector3D light)
            {
                // asumming normal and light are versors!!!!

                //normal.Normalize();
                //light.Normalize();
                Vector3D R = (2 * Vector3D.DotProduct(normal, light) * normal) - light;
                //R.Normalize();

                double cosNL = PositiveCos(normal, light);
                double cosVRM = Math.Pow(PositiveCos(V, R), m);
                double loR = lightColor.r * objectColor.r;
                double loG = lightColor.g * objectColor.g;
                double loB = lightColor.b * objectColor.b;

                double red = (kd * loR * cosNL) + (ks * loR * cosVRM);
                double green = (kd * loG * cosNL) + (ks * loG * cosVRM);
                double blue = (kd * loB * cosNL) + (ks * loB * cosVRM);

                return (Math.Min(red, 1), Math.Min(green, 1), Math.Min(blue, 1));
            }
            public (double r, double g, double b) GetColor(Vector3D normal, Vector3D light, Color customColor)
            {
                // asumming normal and light are versors!!!!

                //normal.Normalize();
                //light.Normalize();
                Vector3D R = (2 * Vector3D.DotProduct(normal, light) * normal) - light;
                //R.Normalize();

                double cosNL = PositiveCos(normal, light);
                double cosVRM = Math.Pow(PositiveCos(V, R), m);
                double loR = lightColor.r * customColor.R / 255.0;
                double loG = lightColor.g * customColor.G / 255.0;
                double loB = lightColor.b * customColor.B / 255.0;

                double red = (kd * loR * cosNL) + (ks * loR * cosVRM);
                double green = (kd * loG * cosNL) + (ks * loG * cosVRM);
                double blue = (kd * loB * cosNL) + (ks * loB * cosVRM);

                return (Math.Min(red,1), Math.Min(green, 1), Math.Min(blue, 1));
            }
            double PositiveCos(Vector3D a, Vector3D b)
            {
                // a and b are versors
                return Math.Max(Vector3D.DotProduct(a, b), 0);
            }
        }
        public class Drawer
        {
            static int height, width;
            static Calculator calc;
            double error = 1e-6;
            double bmpWidth;
            double bmpHeight;
            Vector3D[,] bmpNormals;
            NormalMap mapState = NormalMap.None;

            bool readFromCache = false;
            (int,int) lastCachedSegments = (-1, -1);
            public Vector3D[,] cachedNormals;

            Color[,] loadedColors;
            public enum NormalMap
            { None, Multiply, Zastąp }
            public Drawer(int width, int height)
            {
                Drawer.height = height;
                Drawer.width = width;
            }
            public void DrawPolygons(byte[] buffor, int depth, Calculator calculator, Vector3D lightPos)
            {
                Vector3D[,] points = calculator.GetPoints3();
                Vector3D[,] normalV = calculator.GetNormalVectors();
                Vector3D[,] lightV = calculator.GetLightVectors(lightPos);
                calc = calculator;

                if (lastCachedSegments == (-1, -1))
                {
                    cachedNormals = new Vector3D[width, height];
                    lastCachedSegments = (calc.numberXSegments, calc.numberYSegments);
                }
                else if (lastCachedSegments == (calc.numberXSegments, calc.numberYSegments))
                {                    
                    if (cachedNormals != null)
                    {
                        readFromCache = true;
                    }
                    else
                    {
                        cachedNormals = new Vector3D[width, height];
                    }
                }

                Parallel.For(0, calculator.numberXSegments * calculator.numberYSegments, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount - 2 }, k =>
                {
                    int i = k % calc.numberXSegments;
                    int j = k / calc.numberXSegments;
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

                readFromCache = false;
            }        
            public void DrawBorders(byte[] buffor, int depth, Calculator calculator, Vector3D lightPos)
            {
                Vector3D[,] points = calculator.GetPoints3();
                Vector3D[,] normalV = calculator.GetNormalVectors();
                Vector3D[,] lightV = calculator.GetLightVectors(lightPos);
                calc = calculator;

                if (lastCachedSegments == (-1, -1))
                {
                    cachedNormals = new Vector3D[width, height];
                    lastCachedSegments = (calc.numberXSegments, calc.numberYSegments);
                }
                else if (lastCachedSegments == (calc.numberXSegments, calc.numberYSegments))
                {
                    if (cachedNormals != null)
                    {
                        readFromCache = true;
                    }
                    else
                    {
                        cachedNormals = new Vector3D[width, height];
                    }
                }

                Parallel.For(0, calculator.numberXSegments * calculator.numberYSegments, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 }, k =>
                {
                    int i = k % calc.numberXSegments;
                    int j = k / calc.numberXSegments;
                    {
                        DrawBorder(buffor, depth,
                            new[] { points[i, j], points[i + 1, j], points[i + 1, j + 1] },
                            new[] { normalV[i, j], normalV[i + 1, j], normalV[i + 1, j + 1] },
                            new[] { lightV[i, j], lightV[i + 1, j], lightV[i + 1, j + 1] });
                        DrawBorder(buffor, depth,
                         new[] { points[i, j], points[i, j + 1], points[i + 1, j + 1] },
                         new[] { normalV[i, j], normalV[i, j + 1], normalV[i + 1, j + 1] },
                         new[] { lightV[i, j], lightV[i, j + 1], lightV[i + 1, j + 1] }
                        );
                    }
                });

                readFromCache = false;
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
            void DrawBorder(byte[] buffor, int depth, Vector3D[] Points, Vector3D[] normalVectors, Vector3D[] lightVectors)
            {
                Point[] P = Points.Select(v => new Point((int)(v.X * width), (int)(v.Y * height))).ToArray();
                int N = P.Length;
                for(int i=0; i < N; i++)
                {
                    DrawLine(buffor, depth, P[i], P[(i+1) % N],P, normalVectors, lightVectors);
                }
                
            }
            void DrawLine(byte[] buffor, int depth, Point p1, Point p2, Point[] P, Vector3D[] normalVectors, Vector3D[] lightVectors)
            {
                //https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
                int DY = p2.Y - p1.Y;
                int DX = p2.X - p1.X;
                if (Math.Abs(DY) < Math.Abs(DX))
                {
                    if (p1.X > p2.X)
                        plotLineLow(p2, p1);
                    else
                        plotLineLow(p1, p2);
                }
                else
                {
                    if (p1.Y > p2.Y)
                        plotLineHigh(p2, p1);
                    else
                        plotLineHigh(p1, p2);
                }

                void plotLineHigh(Point point1, Point point2)
                {
                    int dx = point2.X - point1.X;
                    int dy = point2.Y - point1.Y;
                    int xi = 1;
                    if (dx < 0)
                    {
                        xi = -1;
                        dx = -dx;
                    }
                    int D = 2 * dx - dy;
                    int x1 = point1.X;
                    for (int y1 = point1.Y; y1 <= point2.Y; y1++)
                    {
                        DrawPixel(buffor, depth, x1, y1, P, normalVectors, lightVectors);
    
                    if (D > 0)
                        {
                            x1 += xi;
                            D -= 2 * dy;
                        }
                        D += 2 * dx;
                    }
                }
                void plotLineLow(Point point0, Point point1)
                {
                    int dx = point1.X - point0.X;
                    int dy = point1.Y - point0.Y;
                    int yi = 1;
                    if (dy < 0)
                    {
                        yi = -1;
                        dy = -dy;
                    }
                    int D = 2 * dy - dx;
                    int y1 = point0.Y;
                    for (int x1 = point0.X; x1 <= point1.X; x1++)
                    {
                        DrawPixel(buffor, depth, x1, y1, P, normalVectors, lightVectors);

                        if (D >= 0)
                        {
                            y1 += yi;
                            D -= 2 * dx;
                        }
                        D += 2 * dy;
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
                double denom = (P[1].Y * P[2].X) - (P[2].Y * P[1].X) + ((P[2].Y * P[0].X) - (P[0].Y * P[2].X)) + ((P[0].Y * P[1].X) - (P[1].Y * P[0].X));
                a = ((P[1].Y * P[2].X) - (P[2].Y * P[1].X) + ((P[2].Y * x) - (y * P[2].X)) + ((y * P[1].X) - (P[1].Y * x))) / denom;
                b = ((P[2].Y * P[0].X) - (P[0].Y * P[2].X) + ((P[0].Y * x) - (y * P[0].X)) + ((y * P[2].X) - (P[2].Y * x))) / denom;
                c = 1 - a - b;

                Vector3D aproxL = (a * lightVectors[0]) + (b * lightVectors[1]) + (c * lightVectors[2]);
                aproxL.Normalize();
                Vector3D aproxN;
                if (readFromCache)
                    aproxN = cachedNormals[x, y];
                else
                {
                    aproxN = (a * normalVectors[0]) + (b * normalVectors[1]) + (c * normalVectors[2]);
                    if (mapState != NormalMap.None && bmpNormals != null)
                    {
                        (int x, int y) value = Ind2Bitmap(x, y);
                        switch (mapState)
                        {
                            case NormalMap.Zastąp:
                                aproxN = bmpNormals[value.x, value.y];
                                aproxN.Normalize();
                                break;
                            case NormalMap.Multiply:
                                Vector3D B;
                                if (Math.Abs(aproxN.X) < error && Math.Abs(aproxN.Y) < error && Math.Abs(aproxN.Z - 1) < error)
                                {
                                    B = new Vector3D(0, 1, 0);
                                }
                                else
                                {
                                    B = Vector3D.CrossProduct(aproxN, new Vector3D(0, 0, 1));
                                }
                                Vector3D T = Vector3D.CrossProduct(B, aproxN);
                                Matrix3D M = new Matrix3D(T.X, B.X, aproxN.X, 0,
                                T.Y, B.Y, aproxN.Y, 0,
                                T.Z, B.Z, aproxN.Z, 0,
                                0, 0, 0, 0);
                                aproxN = M.Transform(bmpNormals[value.x, value.y]);
                                aproxN.Normalize();
                                break;
                        }
                    }
                    cachedNormals[x, y] = aproxN;
                }

                (double r, double g, double b) color;
                if (loadedColors == null)
                    color = calc.GetColor(aproxN, aproxL);
                else
                    color = calc.GetColor(aproxN, aproxL, 
                        loadedColors[(int)(x * (loadedColors.GetLength(0) - 1) / width), (int)(y * (loadedColors.GetLength(0) - 1) / height)]);

                int offset = ((y * width) + x) * depth;
                buffor[offset + 3] = 255;
                buffor[offset + 2] = (byte)(color.r * 255);
                buffor[offset + 1] = (byte)(color.g * 255);
                buffor[offset + 0] = (byte)(color.b * 255);
            }
            public void LoadBitmap(string filename)
            {
                if (String.IsNullOrEmpty(filename))
                {
                    mapState = NormalMap.None;
                    return;
                }
                Bitmap bmp = new Bitmap(filename);
                bmpWidth = bmp.Width;
                bmpHeight = bmp.Height;
                bmpNormals = new Vector3D[bmp.Width, bmp.Height];
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                for (int x = 0; x < bmp.Width; x++)
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        var color = bmp.GetPixel(x, y);
                        bmpNormals[x, y].X = (color.R - 128) / 127.0;
                        bmpNormals[x, y].Y = (color.G - 128) / 127.0;
                        bmpNormals[x, y].Z = color.B / 255.0;

                        bmpNormals[x, y].Normalize();
                    }
                mapState = NormalMap.Multiply;
                cachedNormals = null;
            }
            public void SetMapState(NormalMap map)
            {
                mapState = map;
                cachedNormals = null;
            }
            (int x, int y) Ind2Bitmap(int x, int y)
            {
                return ((int)(x * (bmpWidth - 1) / width), (int)(y * (bmpHeight - 1) / height));
            }

            internal void LoadColorMap(string filename)
            {
                if (String.IsNullOrEmpty(filename))
                {
                    loadedColors = null;
                    return;
                }
                Bitmap bmp = new Bitmap(filename);
                loadedColors = new Color[bmp.Width, bmp.Height];
                for (int x = 0; x < bmp.Width; x++)
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        loadedColors[x, y] = bmp.GetPixel(x, y);
                    }
            }
        }
    }
    public class Canvas : Panel
    {
        public Canvas()
        {
            this.DoubleBuffered = true;
        }
    }
    
}
