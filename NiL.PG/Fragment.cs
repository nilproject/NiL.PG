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

            public virtual TreeNode Parse(string text, int pos, out int maxAchievedPosition)
            {
                FragmentTreeNode res = null;
                maxAchievedPosition = pos;
                for (int i = 0; (i < Variants.Count) && (res == null); i++)
                {
                    res = Variants[i].Parse(text, pos, out var tlen);
                    if (tlen > maxAchievedPosition)
                        maxAchievedPosition = tlen;

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