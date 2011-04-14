using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuildPPRB;

namespace BuildPPConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var preprocessedOutput = BuildTool.Build(args[0]);
            Console.WriteLine(preprocessedOutput);
        }
    }
}
