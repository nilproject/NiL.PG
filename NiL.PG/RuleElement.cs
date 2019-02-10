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
                Rules = new List<Rule>();
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
                return "*" + FieldName + "(" + r + ")";
            }

            public override TreeNode Parse(string text, int pos, out int parsedLen)
            {
                Match match = null;
                var ruleName = "";
                for (int i = 0; i < Rules.Count; i++)
                {
                    match = Rules[i].RegExp.Match(text, pos);
                    if (match.Index != pos || !match.Success)
                    {
                        match = null;
                    }
                    else
                    {
                        ruleName = Rules[i].Name;
                        break;
                    }
                }

                if (match == null || !match.Success)
                {
                    parsedLen = 0;
                    return null;
                }

                parsedLen = match.Length;
                return new TreeNode(ruleName)
                {
                    Name = FieldName,
                    Value = match.Value,
                };
            }
        }
    }
}