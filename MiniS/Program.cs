using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace MiniS_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch swatch = new Stopwatch();

            MiniS.Engine minis = new MiniS.Engine();
            minis.OPEN_STORAGE("db1");

            minis.SET("root/subroot1/subroot2/subroot3/key", "123");


            Console.WriteLine("elapsed ticks: {0}", swatch.ElapsedTicks);
            Console.ReadKey();
        }
    }
}
