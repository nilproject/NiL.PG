using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NiL.PG
{
    public partial class Parser
    {
        private class RuleElement : Element
        {
            public List<Rule> Rules { get; }

            public RuleElement()
            {
                Rules = [];
            }

            public override string ToString()
            {
                string r = "";
                for (int i = 0; i < Rules.Count; i++)
                {
                    r += Rules[i].Name;
                    if (i + 1 < Rules.Count)
                        r += ", ";
                }
                return "*" + FieldName + "(" + r + ")" + (Repeated ? "*" : "") + (Optional ? "?" : "");
            }

            public override TreeNode[]? Parse(string text, int pos, ref int maxAchievedPosition, Dictionary<(Fragment Fragment, int Position), TreeNode[]?> processedFragments)
            {
                Match? match = null;
                var ruleName = "";
                for (int i = 0; i < Rules.Count; i++)
                {
                    match = Rules[i].RegExp.Match(text, pos);
                    if (!match.Success)
                    {
                        match = null;
                    }
                    else if (match.Index > pos)
                    {
                        var checkPos = pos;
                        while (match.Index > checkPos && char.IsWhiteSpace(text[checkPos]))
                            checkPos++;

                        if (match.Index != checkPos)
                        {
                            match = null;
                            continue;
                        }

                        pos = checkPos;
                        ruleName = Rules[i].Name;
                        break;
                    }
                    else
                    {
                        ruleName = Rules[i].Name;
                        break;
                    }
                }

                maxAchievedPosition = pos;

                if (match is not { Success: true })
                    return null;

                maxAchievedPosition += match.Length;

                return [new TreeNode(ruleName)
                {
                    Name = FieldName,
                    Value = match.Value,
                    Position = pos,
                }];
            }
        }
    }
}