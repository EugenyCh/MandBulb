using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;

namespace MandBulb
{
    class EuclidVector : ICloneable
    {
        public double X { set; get; }
        public double Y { set; get; }
        public double Z { set; get; }

        public object Clone() => new EuclidVector
        {
            X = X,
            Y = Y,
            Z = Z
        };

        public double Square() => X * X + Y * Y + Z * Z;
    }

    class RowSetting
    {
        public int Y0;
        public int Y1;
    }

    class PolarVector : ICloneable
    {
        public double R { set; get; }
        public double Phi { set; get; }
        public double Theta { set; get; }

        public object Clone()
        {
            return new PolarVector { R = R, Phi = Phi, Theta = Theta };
        }
    }

    class Mandelbulb
    {
        public Mandelbulb(uint power, uint side)
        {
            Power = power;
            Side = side;
        }

        public Mandelbulb() { }

        public uint? Power { set; get; }
        public uint? Side { set; get; }
        public Color[,] Pixels { set; get; }
        public double AngleXY { set; get; } = 0.0;
        public double AngleXZ { set; get; } = 0.0;
        public double AngleYZ { set; get; } = 0.0;
        public double Bailout => Math.Pow(2.0, 1.0 / (Power.Value - 1.0));

        private object locker = new object();
        private int maxIter;
        private int lines;


        public void SaveTo(string filename)
        {
            int side = (int)Side.Value;
            using (var bitmap = new Bitmap(side, side))
            {
                for (int y = 0; y < side; ++y)
                    for (int x = 0; x < side; ++x)
                        bitmap.SetPixel(x, y, Pixels[y, x]);
                bitmap.Save(filename, ImageFormat.Png);
            }
        }

        private void RenderRow(object s)
        {
            var setting = (RowSetting)s;
            int y0 = setting.Y0;
            int y1 = setting.Y1;
            int side = (int)Side.Value;
            int halfside = side >> 1;
            double bailout2 = Math.Pow(Bailout, 2.0);
            for (int y = y0; y < y1; ++y)
            {
                for (int x = 0; x < side; ++x)
                {
                    double? sqrVec = null;
                    for (int z = side - 1; z >= halfside - 1; --z)
                    {
                        var vec = PointToEuclidVector(x, y, z);
                        var cVec = (EuclidVector)vec.Clone();
                        for (int i = 0; i < maxIter; ++i)
                            IterateVector(ref vec, cVec);
                        double sqr = vec.Square();
                        if (sqr <= bailout2)
                        {
                            sqrVec = cVec.Square();
                            break;
                        }
                    }
                    if (sqrVec.HasValue)
                    {
                        double k = sqrVec.Value / bailout2;
                        lock (locker)
                            Pixels[y, x] = Color.FromArgb(
                                (int)(k * 127),
                                (int)(Math.Pow(k < 0.5 ? (1.0 - k) : k - 0.5, 2.0) * 255),
                                (int)(k * 255));
                    }
                }

                lock (locker)
                    ++lines;
            }
        }

        public void Render(int maxiter, int threadsCount = 1)
        {
            bool ready = true;
            if (Power == null)
            {
                Console.WriteLine("Set the Power");
                ready = false;
            }
            if (Side == null)
            {
                Console.WriteLine("Set the value of side in pixels");
                ready = false;
            }
            if (threadsCount < 1)
            {
                Console.WriteLine("Threads must be one at least");
                ready = false;
            }
            if (!ready)
                return;
            int side = (int)Side.Value;
            double power = Power.Value;
            Pixels = new Color[side, side];
            int barw = 20;
            int rowHeight = side / threadsCount + 1;
            var threads = new Thread[threadsCount];
            maxIter = maxiter;
            lines = 0;
            for (int i = 0; i < threadsCount; ++i)
            {
                int y0 = rowHeight * i;
                int y1 = Math.Min(y0 + rowHeight, side);
                threads[i] = new Thread(RenderRow);
                threads[i].Start(new RowSetting
                {
                    Y0 = y0,
                    Y1 = y1
                });
            }
            Console.WriteLine($"Rendering Mandelbulb | N = {power} | Side = {side} | Iterations = {maxIter} ...");
            bool go = true;
            var time_before = DateTime.Now;
            while (go)
            {
                if (lines == side)
                    go = false;
                double t = (double)lines / side;
                int ti = (int)(barw * t);
                string bar;
                if (t < 1.0)
                {
                    bar = ">".PadLeft(ti, '=');
                    bar = bar.PadRight(barw, '.');
                    Console.Write($"\r[{bar}] {(int)(t * 100)}.{(int)(t * 1000) % 10}% ...  ");
                }
                else
                {
                    bar = "".PadLeft(barw, '=');
                    Console.Write($"\r[{bar}] {(int)(t * 100)}.{(int)(t * 1000) % 10}%   ");
                }
                Thread.Sleep(100);
            }
            var time_after = DateTime.Now;
            var delta_ms = (int)(time_after - time_before).TotalMilliseconds;
            var delta_s = (delta_ms / 1000) % 60;
            var delta_m = delta_ms / 60000;
            delta_ms %= 1000;
            Console.WriteLine($"\nIt tooks {delta_m} m {delta_s}" +
                $".{delta_ms.ToString().PadLeft(3, '0')} s");
        }

        public EuclidVector PointToEuclidVector(int x, int y, int z)
        {
            double halfside = (int)Side.Value >> 1;
            double xf = (x / halfside - 1.0) * Bailout;
            double yf = (y / halfside - 1.0) * Bailout;
            double zf = (z / halfside - 1.0) * Bailout;
            double a = xf, b = yf;
            xf = a * Math.Cos(AngleXY) - b * Math.Sin(AngleXY);
            yf = a * Math.Sin(AngleXY) + b * Math.Cos(AngleXY);
            a = xf; b = zf;
            xf = a * Math.Cos(AngleXZ) - b * Math.Sin(AngleXZ);
            zf = a * Math.Sin(AngleXZ) + b * Math.Cos(AngleXZ);
            a = yf; b = zf;
            yf = a * Math.Cos(AngleYZ) - b * Math.Sin(AngleYZ);
            zf = a * Math.Sin(AngleYZ) + b * Math.Cos(AngleYZ);
            return new EuclidVector { X = xf, Y = yf, Z = zf };
        }

        public PolarVector EuclidVectorToPolar(EuclidVector ev)
        {
            double x = ev.X, y = ev.Y, z = ev.Z;
            double r = Math.Sqrt(x * x + y * y + z * z);
            double phi = x != 0 ? Math.Atan(y / x) : 0.0;
            double theta = r != 0 ? Math.Acos(z / r) : 0.0;
            return new PolarVector { R = r, Phi = phi, Theta = theta };
        }

        public void IterateVector(ref EuclidVector ev, EuclidVector cv)
        {
            var pv = EuclidVectorToPolar(ev);
            double n = Power.Value;
            double rn = Math.Pow(pv.R, n);
            double phi = pv.Phi;
            double theta = pv.Theta;
            ev.X = rn * Math.Sin(n * theta) * Math.Sin(n * phi) + cv.X;
            ev.Y = rn * Math.Sin(n * theta) * Math.Cos(n * phi) + cv.Y;
            ev.Z = rn * Math.Cos(n * theta) + cv.Z;
        }
    }
}
