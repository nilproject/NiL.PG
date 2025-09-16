using System.Collections.Generic;

namespace NiL.PG
{
    public partial class Parser
    {
        private class FragmentElement : Element
        {
            public Fragment Fragment { get; set; }

            public override string ToString()
            {
                return "*" + FieldName + "(" + Fragment.ToString() + ")" + (Repeated ? "*" : "") + (Optional ? "?" : "");
            }

            public override TreeNode[]? Parse(string text, int position, ref int maxAchievedPosition, Dictionary<(Fragment Fragment, int Position), TreeNode[]?> processedFragments)
            {
                var t = Fragment.Parse(text, position, ref maxAchievedPosition, processedFragments);
                if (t != null)
                {
                    foreach (var fragment in t)
                    {
                        fragment.Name = FieldName;
                    }
                }

                return t;
            }
        }
    }
}