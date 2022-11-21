namespace NiL.PG
{
    public partial class Parser
    {
        private class EqualElement : Element
        {
            public string Value { get; set; }

            public override string ToString()
            {
                return Value;
            }

            public override TreeNode Parse(string text, int pos, out int maxAchievedPosition)
            {
                maxAchievedPosition = pos;

                if (pos + Value.Length > text.Length)
                    return null;

                var index = text.IndexOf(Value, pos, Value.Length);
                if (index == pos)
                {
                    maxAchievedPosition = pos + Value.Length;
                    return new StrictEqualTreeNode(Value) { Value = Value };
                }

                return null;
            }
        }
    }
}