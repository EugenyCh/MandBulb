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
            var mandelbulb = new Mandelbulb(8, 240);
            mandelbulb.Render(3);
            mandelbulb.SaveTo("output.png");
        }
    }
}
