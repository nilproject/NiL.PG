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

            public override TreeNode Parse(string text, int position, out int maxAchievedPosition, Dictionary<(Fragment Fragment, int Position), TreeNode> processedFragments)
            {
                var t = Fragment.Parse(text, position, out maxAchievedPosition, processedFragments);
                if (t != null)
                    t.Name = FieldName;
                return t;
            }
        }
    }
}