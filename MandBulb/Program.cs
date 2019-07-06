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
            var mandelbulb = new Mandelbulb(8, 320);
            mandelbulb.Render(5, 10);
            mandelbulb.SaveTo("output.png");
        }
    }
}
