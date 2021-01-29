using System.Collections.Generic;

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
            public int VariantIndex { get; internal set; }

            public FragmentTreeNode(string fragmentName) : base(fragmentName)
            {
            }
        }

        public class TreeNode
        {
            public List<TreeNode> Children { get; private set; }
            public string Name { get; internal set; }
            public string Value { get; set; }
            public string FragmentName { get; internal set; }

            public TreeNode(string fragmentName)
            {
                Children = new List<TreeNode>();
                Value = "";
                Name = "";
                FragmentName = fragmentName;
            }

            public TreeNode this[string name]
            {
                get
                {
                    string[] path = name.Split('\\');
                    TreeNode c = this;
                    for (int i = 0; i < path.Length; i++)
                    {
                        bool cont = false;
                        foreach (var n in c.Children)
                        {
                            if (n.Name == path[i])
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

            public override string ToString()
            {
                return Name + ": " + Value;
            }
        }
    }
}