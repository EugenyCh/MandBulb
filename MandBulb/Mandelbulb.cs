using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

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

        public void Render(int maxiter)
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
            if (!ready)
                return;
            int side = (int)Side.Value;
            double power = Power.Value;
            Pixels = new Color[side, side];
            double bailout = Bailout;
            double bailout2 = Math.Pow(Bailout, 2.0);
            int halfside = side >> 1;
            Console.WriteLine($"Rendering Mandelbulb - N = {power}, Side = {side}...");
            int barw = 20;
            var time_before = DateTime.Now;
            for (int y = 0; y < side; ++y)
            {
                for (int x = 0; x < side; ++x)
                {
                    double? sqrVec = null;
                    for (int z = side - 1; z >= halfside - 1; --z)
                    {
                        var vec = PointToEuclidVector(x, y, z);
                        var cVec = (EuclidVector)vec.Clone();
                        for (int i = 0; i < maxiter; ++i)
                            IterateVector(ref vec, cVec);
                        double sqr = vec.Square();
                        if (sqr <= bailout2)
                        {
                            sqrVec = sqr;
                            break;
                        }
                    }
                    if (sqrVec.HasValue)
                    {
                        double k = 1.0 - sqrVec.Value / bailout2;
                        Pixels[y, x] = Color.FromArgb(
                            (int)(k * 255),  // red
                            (int)(k * 255),  // green
                            (int)(k * 255)); // blue
                    }
                }
                double t = (y + 1.0) / side;
                int ti = (int)(barw * t);
                string bar = "".PadLeft(ti, '=');
                bar = bar.PadRight(barw, '.');
                Console.Write($"\r[{bar}] {(int)(t * 100)}%  ");
            }
            var time_after = DateTime.Now;
            var delta_ms = (int)(time_after - time_before).TotalMilliseconds;
            var delta_s = (delta_ms / 1000) % 60;
            var delta_m = delta_ms / 60000;
            delta_ms %= 1000;
            Console.WriteLine($"\nIt tooks {delta_m} m {delta_s.ToString().PadLeft(2, '0')}" +
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
