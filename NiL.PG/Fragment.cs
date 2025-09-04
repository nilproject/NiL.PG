using System;
using System.Collections.Generic;
using System.Diagnostics;

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
                Variants = [];
            }

            public override string ToString()
            {
                return Name;
            }

            public virtual TreeNode? Parse(string text, int position, out int maxAchievedPosition, Dictionary<(Fragment Fragment, int Position), TreeNode?> processedFragments)
            {
                if (processedFragments.TryGetValue((this, position), out var existedResult))
                {
                    maxAchievedPosition = position + (existedResult?.Value.Length ?? 0);
                    return existedResult;
                }

                processedFragments[(this, position)] = null;

                FragmentTreeNode? res = null;
                maxAchievedPosition = position;
                for (int i = 0; (i < Variants.Count) && (res == null); i++)
                {
                    res = Variants[i].Parse(text, position, out var tlen, processedFragments);
                    if (tlen > maxAchievedPosition)
                        maxAchievedPosition = tlen;

                    if (res != null)
                        res.VariantIndex = i;
                }

                if (res != null)
                    res.Name = Name;

                processedFragments[(this, position)] = res;
                return res;
            }
        }
    }
}