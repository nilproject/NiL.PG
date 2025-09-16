using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

            public override string ToString() => Name;

            public virtual TreeNode[]? Parse(string text, int position, ref int maxAchievedPosition, Dictionary<(Fragment Fragment, int Position), TreeNode[]?> processedFragments)
            {
                if (processedFragments.TryGetValue((this, position), out var existedResult))
                {
                    var t = position + (existedResult?.Max(x => x.Value.Length) ?? 0);
                    if (t > maxAchievedPosition)
                        maxAchievedPosition = t;

                    if (existedResult is null)
                        return null;

                    return [.. existedResult.Select(x => x.Clone() as TreeNode)!];
                }

                processedFragments[(this, position)] = null;

                List<FragmentTreeNode>? res = null;
                if (maxAchievedPosition < position)
                    maxAchievedPosition = position;
                for (int i = 0; i < Variants.Count; i++)
                {
                    var parsedSubVariants = Variants[i].Parse(text, position, ref maxAchievedPosition, processedFragments);

                    if (parsedSubVariants != null)
                    {
                        foreach (var variant in parsedSubVariants)
                        {
                            variant.VariantIndex = i;
                            (res ??= []).Add(variant);
                        }
                    }
                }

                return processedFragments[(this, position)] = res?.ToArray();
            }
        }
    }
}