using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiL.PG;

namespace CLikeParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(File.ReadAllText("SyntaxDescription.npg"));

            var tree = parser.Parse(File.ReadAllText("code.c"));

            for (var i = 0; i < tree.Childs.Count; i++)
            {
                var func = tree.Childs[i];
                Console.Write(func["type"].Value + " " + func["name"].Value);
            }
        }
    }
}
