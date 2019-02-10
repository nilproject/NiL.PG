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

            public override TreeNode Parse(string text, int pos, out int parsedLen)
            {
                parsedLen = 0;
                if (pos + Value.Length > text.Length)
                    return null;

                var index = text.IndexOf(Value, pos, Value.Length);
                if (index == pos)
                {
                    parsedLen = Value.Length;
                    return new StrictEqualTreeNode(Value) { Value = Value };
                }

                return null;
            }
        }
    }
}