namespace NiL.PG
{
    public partial class Parser
    {
        private class FragmentElement : Element
        {
            public Fragment Fragment { get; set; }

            public override string ToString()
            {
                return "*" + FieldName + "(" + Fragment.ToString() + ")" + (Repeated ? "*" : "");
            }

            public override TreeNode Parse(string text, int pos, out int maxAchievedPosition)
            {
                var t = Fragment.Parse(text, pos, out maxAchievedPosition);
                if (t != null)
                    t.Name = FieldName;
                return t;
            }
        }
    }
}