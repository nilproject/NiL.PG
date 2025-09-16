using System.Collections.Generic;

namespace NiL.PG
{
    public partial class Parser
    {
        private class ConstantElement : Element
        {
            public string Value { get; set; }

            public override string ToString() => Value;

            public override TreeNode[]? Parse(string text, int position, ref int maxAchievedPosition, Dictionary<(Fragment Fragment, int Position), TreeNode[]?> processedFragments)
            {
                if (maxAchievedPosition < position)
                    maxAchievedPosition = position;

                if (position + Value.Length > text.Length)
                    return null;

                while ((text.Length > position) && char.IsWhiteSpace(text[position]))
                    position++;

                if (position + Value.Length > text.Length)
                    return null;

                var index = text.IndexOf(Value, position, Value.Length);

                if (index == position)
                {
                    if (maxAchievedPosition < position + Value.Length)
                        maxAchievedPosition = position + Value.Length;

                    return [new StrictEqualTreeNode(Value) { Value = Value, Position = position }];
                }

                return null;
            }
        }
    }
}