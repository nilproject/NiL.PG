using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace NiL.PG
{
    public partial class Parser
    {
        public sealed class StrictEqualTreeNode : TreeNode
        {
            public StrictEqualTreeNode(string name) : base("StrictEqual")
            {
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        public sealed class FragmentTreeNode : TreeNode
        {
            public FragmentTreeNode(string fragmentName) : base(fragmentName)
            {
            }
        }

        public class TreeNode : ICloneable
        {
            private static readonly char[] _splitChars = ['\\', '/'];
            public TreeNode[] Children { get; internal set; } = [];
            public string Name { get; internal set; }
            public string Value { get; set; }
            public string FragmentName { get; internal set; }
            public int Position { get; internal set; }
            public int VariantIndex { get; internal set; }
            public TreeNode[]? AlternativeInterpretations { get; internal set; }

            public TreeNode(string fragmentName)
            {
                Value = "";
                Name = "";
                FragmentName = fragmentName;
            }

            public TreeNode? this[string name]
            {
                get
                {
                    string[] path = name.Split(_splitChars);
                    TreeNode c = this;
                    for (int i = 0; i < path.Length; i++)
                    {
                        bool cont = false;
                        var byFragmentName = false;
                        var pathItem = path[i];
                        if (pathItem.StartsWith("[") && pathItem.EndsWith("]"))
                        {
                            pathItem = pathItem[1..^1];
                            byFragmentName = true;
                        }

                        foreach (var n in c.Children)
                        {
                            if ((byFragmentName ? n.FragmentName : n.Name) == pathItem)
                            {
                                c = n;
                                cont = true;
                                break;
                            }
                        }

                        if (cont)
                            continue;
                        return null;
                    }

                    return c;
                }
            }

            public IEnumerable<TreeNode> Enumerate(string path, params string[]? alternativePaths)
            {
                static IEnumerable<TreeNode> enumerateWithPathPart(IEnumerable<TreeNode> nodes, string pathPart)
                {
                    var byFragmentName = false;
                    var init = false;

                    foreach (var nextNode in nodes)
                    {
                        if (!init)
                        {
                            if (pathPart.StartsWith("[") && pathPart.EndsWith("]"))
                            {
                                pathPart = pathPart[1..^1];
                                byFragmentName = true;
                            }

                            init = true;
                        }

                        if (byFragmentName && nextNode.FragmentName == pathPart)
                        {
                            yield return nextNode;
                        }
                        else if (!byFragmentName && nextNode.Name == pathPart)
                        {
                            yield return nextNode;
                            yield break;
                        }
                    }
                }

                var enumerator = Children as IEnumerable<TreeNode>;

                var pathParts = path.Split(_splitChars);
                for (int i = 0; i < pathParts.Length; i++)
                    enumerator = enumerateWithPathPart(i > 0
                        ? enumerator.SelectMany(x => x.Children)
                        : enumerator, pathParts[i]);

                if (alternativePaths?.Length > 0)
                {
                    for (var pi = 0; pi < alternativePaths.Length; pi++)
                    {
                        pathParts = alternativePaths[pi].Split(_splitChars);
                        var nextEnumerator = Children as IEnumerable<TreeNode>;
                        for (int i = 0; i < pathParts.Length; i++)
                            nextEnumerator = enumerateWithPathPart(i > 0
                                ? nextEnumerator.SelectMany(x => x.Children)
                                : nextEnumerator, pathParts[i]);

                        enumerator = enumerator.Concat(nextEnumerator);
                    }
                }

                return enumerator;
            }

            public override string ToString() => Name + ": " + Value;

            public object Clone() => MemberwiseClone();
        }
    }
}