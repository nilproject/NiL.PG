using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiL.PG;

namespace HtmlParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(File.ReadAllText("SyntaxDescription.npg"));

            var tree = parser.Parse(File.ReadAllText("page.html"));

            printNode(tree["html"]);
        }

        private static void printNode(Parser.TreeNode tree)
        {
            if (tree["tag"]?.Value == "head")
                return;

            foreach (var node in tree.Childs)
            {
                switch (node.FragmentName)
                {
                    case "block":
                    {
                        var fragmentNode = node as Parser.FragmentTreeNode;
                        switch (fragmentNode.VariantIndex)
                        {
                            case 2:
                            {
                                printNode(fragmentNode["inner"]);
                                break;
                            }
                        }

                        break;
                    }

                    case "blockContent":
                    {
                        var fragmentNode = node as Parser.FragmentTreeNode;
                        switch (fragmentNode.VariantIndex)
                        {
                            case 0:
                            {
                                printNode(fragmentNode["node"]);
                                break;
                            }

                            case 1:
                            {
                                Console.WriteLine(fragmentNode.Value.Trim());
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
