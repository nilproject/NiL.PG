using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NiL.PG
{
    public partial class Parser
    {
        private static string getToken(string text, ref int pos)
        {
            while (pos < text.Length && char.IsWhiteSpace(text[pos]))
                pos++;

            if (pos >= text.Length)
                return "";

            pos = skipComment(text, pos, true);

            if (char.IsLetterOrDigit(text[pos]) || text[pos] == '_')
            {
                int s = pos;
                while ((s > 0) && (char.IsLetterOrDigit(text[s - 1]) || text[s - 1] == '_'))
                {
                    if (--s <= 0)
                    {
                        s = 0;
                        break;
                    }
                }

                while (pos + 1 < text.Length && (char.IsLetterOrDigit(text[pos + 1]) || text[pos + 1] == '_'))
                {
                    if (++pos >= text.Length - 1)
                    {
                        pos = text.Length - 1;
                        break;
                    }
                }

                return text.Substring(s, ++pos - s);
            }

            return text[pos++].ToString();
        }

        private static bool isValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !char.IsLetter(name[0]) && (name[0] != '_'))
                return false;

            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && (name[i] != '_'))
                    return false;
            }

            return true;
        }

        private Fragment root;

        public Parser(string definition)
        {
            var rules = new Dictionary<string, Rule>();
            var fragments = new Dictionary<string, Fragment>();
            var input = definition.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int line = 0;
            int position = 0;
            Func<bool> lineFeed = () =>
            {
                if (line >= input.Length)
                    return false;

                if (string.IsNullOrWhiteSpace(input[line]) || position >= input[line].Length)
                {
                    position = 0;
                    line++;
                    return true;
                }

                return false;
            };

            while (line < input.Length)
            {
                while (lineFeed()) ;

                if (line == input.Length)
                    break;

                string token = getToken(input[line], ref position);

                if (token is "")
                    continue;

                switch (token)
                {
                    case "rule":
                    {
                        lineFeed();
                        token = getToken(input[line], ref position);

                        if (!isValidName(token))
                            throw new ArgumentException("Invalid rule name " + token + " (" + line + ", " + position + ")");

                        if (rules.ContainsKey(token))
                            throw new ArgumentException("Try to redefine rule " + token + " (" + line + ", " + position + ")");

                        if (fragments.ContainsKey(token))
                            throw new ArgumentException("Try to redefine fragment " + token + " (" + line + ", " + position + ")");

                        Rule rule = new Rule();
                        rule.Name = token;
                        rules.Add(rule.Name, rule);
                        string code = "";
                        while (!string.IsNullOrWhiteSpace(input[line + 1]))
                        {
                            line++;
                            code += input[line].Trim();
                        }

                        position = int.MaxValue;

                        try
                        {
                            rule.RegExp = new Regex(code);
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException("Invalid rule define " + rule.Name + " (" + line + ", " + position + ")", e);
                        }

                        break;
                    }
                    case "fragment":
                    {
                        token = getToken(input[line], ref position);

                        if (!isValidName(token))
                            throw new ArgumentException("Invalid fragment name " + token + " (" + line + ", " + position + ")");

                        if (rules.ContainsKey(token))
                            throw new ArgumentException("Try to redefine rule " + token + " (" + line + ", " + position + ")");

                        Fragment fragment;
                        if (fragments.TryGetValue(token, out fragment))
                        {
                            if (fragment.Variants.Count != 0)
                                throw new ArgumentException("Try to redefine fragment " + token + " (" + line + ", " + position + ")");
                        }
                        else
                        {
                            fragment = new Fragment(token);
                            fragments.Add(token, fragment);
                        }

                        List<string> code = new List<string>();
                        while ((line + 1 < input.Length) && !string.IsNullOrWhiteSpace(input[line + 1]) && char.IsWhiteSpace(input[line + 1][0]))
                        {
                            line++;
                            code.Add(input[line]);
                        }

                        position = int.MaxValue;

                        for (int i = 0; i < code.Count; i++)
                        {
                            var variant = new FragmentVariant(token, i);
                            fragment.Variants.Add(variant);
                            var trimcode = code[i].Trim();
                            var absCodeLineIndex = line - code.Count + i + 2;
                            var prevPosition = 0;
                            var fragmentPosition = 0;
                            while (fragmentPosition < trimcode.Length)
                            {
                                prevPosition = fragmentPosition;
                                var t = getToken(trimcode, ref fragmentPosition);
                                switch (t)
                                {
                                    case "\\":
                                    {
                                        t = getToken(trimcode, ref fragmentPosition);
                                        goto default;
                                    }

                                    case "*":
                                    {
                                        string fname = getToken(trimcode, ref fragmentPosition);
                                        if (!isValidName(fname))
                                            throw new ArgumentException("Invalid field name " + fname + " (" + absCodeLineIndex + ", " + fragmentPosition + ")");

                                        List<Rule>? paramsRules = null;
                                        List<Fragment>? ffrags = null;
                                        bool work = true;
                                        bool mrule = false;
                                        bool repeated = false;
                                        bool optional = false;
                                        while (work)
                                        {
                                            switch (trimcode[fragmentPosition])
                                            {
                                                case '?':
                                                {
                                                    optional = true;
                                                    fragmentPosition++;
                                                    break;
                                                }
                                                case '*':
                                                {
                                                    repeated = true;
                                                    fragmentPosition++;
                                                    break;
                                                }
                                                case ' ':
                                                {
                                                    work = mrule;
                                                    fragmentPosition++;
                                                    break;
                                                }
                                                case ',':
                                                {
                                                    if (mrule)
                                                    {
                                                        while (char.IsWhiteSpace(trimcode[++fragmentPosition])) ;
                                                        fragmentPosition--;
                                                        goto case '(';
                                                    }
                                                    else
                                                        goto default;
                                                }
                                                case '(':
                                                {
                                                    string rulname = "";
                                                    fragmentPosition++;
                                                    var ruleNameStart = fragmentPosition;

                                                    while ((trimcode[fragmentPosition] != ')') && (trimcode[fragmentPosition] != ','))
                                                        fragmentPosition++;

                                                    rulname = trimcode[ruleNameStart..fragmentPosition];

                                                    if (!isValidName(rulname))
                                                        throw new ArgumentException("Invalid element definition \"" + rulname + "\" at line " + absCodeLineIndex);

                                                    if (!(rules.ContainsKey(rulname) || fragments.ContainsKey(rulname)))
                                                        throw new ArgumentException("Undefined element \"" + rulname + "\" at line " + absCodeLineIndex);

                                                    if (rules.ContainsKey(rulname))
                                                        (paramsRules ??= []).Add(rules[rulname]);

                                                    if (fragments.TryGetValue(rulname, out var frag))
                                                    {
                                                        if (frag == fragment && variant.Elements.Count == 0)
                                                            throw new InvalidOperationException("Immediate recursive call is not allowed");

                                                        (ffrags ??= []).Add(frag);
                                                    }

                                                    mrule = trimcode[fragmentPosition] == ',';

                                                    if (!mrule) fragmentPosition++;

                                                    break;
                                                }
                                                default:
                                                {
                                                    work = false;
                                                    break;
                                                }
                                            }
                                            if (trimcode.Length <= fragmentPosition)
                                                work = false;
                                        }

                                        if (paramsRules is not null && ffrags is not null)
                                            throw new ArgumentException("Field define can't contains rules and fragment together at line " + absCodeLineIndex);

                                        if (ffrags is { Count: > 1 })
                                            throw new ArgumentException("Field define can't contain more than one fragment at line " + absCodeLineIndex);

                                        if ((ffrags is null) && (paramsRules is null))
                                            throw new ArgumentException("Rule must be declared at line " + absCodeLineIndex);

                                        if (ffrags is not null)
                                        {
                                            variant.Elements.Add(new FragmentElement()
                                            {
                                                Repeated = repeated,
                                                Fragment = ffrags[0],
                                                FieldName = fname,
                                                Optional = optional,
                                            });
                                        }
                                        else
                                        {
                                            var ruleElement = new RuleElement()
                                            {
                                                Repeated = repeated,
                                                FieldName = fname,
                                                Optional = optional,
                                            };
                                            ruleElement.Rules.AddRange(paramsRules!);
                                            variant.Elements.Add(ruleElement);
                                        }
                                        break;
                                    }

                                    default:
                                    {
                                        if (prevPosition == fragmentPosition - t.Length
                                            && variant.Elements.Count > 0
                                            && variant.Elements[variant.Elements.Count - 1] is ConstantElement equalElement)
                                        {
                                            equalElement.Value += t;
                                        }
                                        else
                                        {
                                            var eq = new ConstantElement() { Value = t, FieldName = t };
                                            variant.Elements.Add(eq);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    default:
                    {
                        throw new ArgumentException("Invalid token " + token + " (" + line + ", " + position + ")");
                    }
                }
            }

            root = fragments["root"];
        }

        public TreeNode Parse(string text)
        {
            var t = root.Parse(text, 0, out int parsedLength, []);

            while (parsedLength < text.Length && char.IsWhiteSpace(text[parsedLength]))
                parsedLength++;

            if (parsedLength < text.Length)
            {
                int line = 1;
                var lineStart = 0;

                for (var i = 0; i < parsedLength; i++)
                {
                    if (text[i] == '\n')
                    {
                        line++;
                        lineStart = i;
                    }

                    if (text[i] == '\r')
                        lineStart = i;
                }

                parsedLength -= lineStart;

                throw new ArgumentException("Syntax error at " + line + ":" + parsedLength);
            }

            return t;
        }

        private static int skipComment(string code, int index, bool skipSpaces = false)
        {
            bool work;
            do
            {
                if (code.Length <= index)
                    return index;

                work = false;
                if (code[index] == '/' && index + 1 < code.Length)
                {
                    switch (code[index + 1])
                    {
                        case '/':
                        {
                            index += 2;
                            while (index < code.Length && !IsLineTerminator(code[index]))
                                index++;

                            work = true;
                            break;
                        }
                        case '*':
                        {
                            index += 2;
                            while (index + 1 < code.Length && (code[index] != '*' || code[index + 1] != '/'))
                                index++;
                            if (index + 1 >= code.Length)
                                throw new InvalidOperationException("Unexpected end of source.");

                            index += 2;
                            work = true;
                            break;
                        }
                    }
                }
            }
            while (work);

            if (skipSpaces)
            {
                while ((index < code.Length) && (char.IsWhiteSpace(code[index])))
                    index++;
            }

            return index;
        }

        internal static bool IsLineTerminator(char c)
        {
            return (c == '\u000A') || (c == '\u000D') || (c == '\u2028') || (c == '\u2029');
        }
    }
}