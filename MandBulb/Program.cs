using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MandBulb
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() >= 3)
            {
                try
                {
                    int n = int.Parse(args[0]);
                    if (n < 2)
                        throw new Exception("N must be 2 or greater");
                    int side = int.Parse(args[1]);
                    if (side == 0)
                        throw new Exception("Side must be positive");
                    int iter = int.Parse(args[2]);
                    if (iter < 0)
                        throw new Exception("Iterations must be positive or only zero");
                    var mandelbulb = new Mandelbulb((uint)n, (uint)side);
                    if (args.Count() == 6)
                    {
                        mandelbulb.AngleXY = double.Parse(args[3]) * Math.PI / 180;
                        mandelbulb.AngleXZ = double.Parse(args[4]) * Math.PI / 180;
                        mandelbulb.AngleYZ = double.Parse(args[5]) * Math.PI / 180;
                    }
                    mandelbulb.Render(iter, 10);
                    mandelbulb.SaveTo($"mandelbulb-n{n}-s{side}-i{iter}" +
                        $"-xy{(int)(mandelbulb.AngleXY * 180 / Math.PI)}" +
                        $"-xz{(int)(mandelbulb.AngleXZ * 180 / Math.PI)}" +
                        $"-yz{(int)(mandelbulb.AngleYZ * 180 / Math.PI)}.png");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Execute MandBulb with next arguments:\n" +
                    "MandBulb <N> <Side> <Iteration> [<AngleXY> <AngleXZ> <AngleYZ>]");
            }
        }
    }
}
