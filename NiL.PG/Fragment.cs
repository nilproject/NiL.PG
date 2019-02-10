using System.Collections.Generic;

namespace NiL.PG
{
    public partial class Parser
    {
        private class Fragment
        {
            public string Name { get; set; }
            public List<FragmentVariant> Variants { get; private set; }

            public Fragment(string name)
            {
                Name = name;
                Variants = new List<FragmentVariant>();
            }

            public override string ToString()
            {
                return Name;
            }

            public virtual TreeNode Parse(string text, int pos, out int parsedLen)
            {
                FragmentTreeNode res = null;
                int tlen = 0;
                parsedLen = 0;
                for (int i = 0; (i < Variants.Count) && (res == null); i++)
                {
                    res = Variants[i].Parse(text, pos, out tlen);
                    if (tlen > parsedLen)
                        parsedLen = tlen;
                    if (res != null)
                        res.VariantIndex = i;
                }

                if (res != null)
                    res.Name = Name;

                return res;
            }
        }
    }
}