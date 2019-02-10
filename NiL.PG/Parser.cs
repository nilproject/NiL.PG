using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NiL.PG
{
    public partial class Parser
    {
        private static string getToken(string text, ref int pos)
        {
            if (pos == text.Length)
                return "";

            while (char.IsWhiteSpace(text[pos]))
                pos++;

            if (char.IsLetterOrDigit(text[pos]))
            {
                int s = pos;
                while ((s > 0) && char.IsLetterOrDigit(text[s - 1]))
                    if (--s <= 0)
                    {
                        s = 0;
                        break;
                    }
                while (char.IsLetterOrDigit(text[pos + 1]))
                    if (++pos >= text.Length - 1)
                    {
                        pos = text.Length - 1;
                        break;
                    }
                return text.Substring(s, ++pos - s);
            }

            return text[pos++].ToString();
        }

        private static bool isValidName(string name)
        {
            if (!char.IsLetter(name[0]) && (name[0] != '_'))
                return false;
            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && (name[0] != '_'))
                    return false;
            }
            return true;
        }

        private Fragment root;

        public Parser(string definition)
        {
            var rules = new Dictionary<string, Rule>();
            var fragments = new Dictionary<string, Fragment>();
            string[] input = definition.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int line = 0;
            int position = 0;
            Func<bool> lineFeed = () =>
            {
                if (line >= input.Length)
                    return false;
                if (position >= input[line].Length)
                {
                    position -= input[line].Length;
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
                        line++;
                        string code = "";
                        while (input[line].Trim() != "")
                        {
                            code += input[line].Trim();
                            line++;
                        }
                        position = 0;

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
                        line++;
                        while ((line < input.Length) && (input[line].Trim() != ""))
                        {
                            code.Add(input[line]);
                            line++;
                        }

                        position = 0;
                        for (int i = 0; i < code.Count; i++)
                        {
                            var variant = new FragmentVariant(token, i);
                            fragment.Variants.Add(variant);
                            var trimcode = code[i].Trim();
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
                                            throw new ArgumentException("Invalid field name " + fname + " (" + (line - input.Length + i) + ", " + position + ")");

                                        List<Rule> paramsRules = new List<Rule>();
                                        List<Fragment> ffrags = new List<Fragment>();
                                        bool work = true;
                                        bool mrule = false;
                                        bool Repeated = false;
                                        while (work)
                                        {
                                            switch (trimcode[fragmentPosition])
                                            {
                                                case '*':
                                                {
                                                    Repeated = true;
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
                                                    while ((trimcode[fragmentPosition] != ')') && (trimcode[fragmentPosition] != ','))
                                                        rulname += trimcode[fragmentPosition++];
                                                    if (!isValidName(rulname) || !(rules.ContainsKey(rulname) || fragments.ContainsKey(rulname)))
                                                        throw new ArgumentException("Invalid element define \"" + rulname + "\"");
                                                    if (rules.ContainsKey(rulname))
                                                        paramsRules.Add(rules[rulname]);
                                                    if (fragments.ContainsKey(rulname))
                                                        ffrags.Add(fragments[rulname]);
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

                                        if ((paramsRules.Count != 0) && (ffrags.Count != 0))
                                        {
                                            throw new ArgumentException("Field define can't contains rules and fragment together");
                                        }

                                        if (ffrags.Count > 1)
                                            throw new ArgumentException("Field define can't contain more than one fragment");

                                        if ((ffrags.Count == 0) && (paramsRules.Count == 0))
                                        {
                                            throw new ArgumentException("Rule shall be declared");
                                        }

                                        if (ffrags.Count != 0)
                                        {
                                            variant.Elements.Add(new FragmentElement()
                                            {
                                                Repeated = Repeated,
                                                Fragment = ffrags[0],
                                                FieldName = fname
                                            });
                                        }
                                        else
                                        {
                                            var ruleElement = new RuleElement()
                                            {
                                                Repeated = Repeated,
                                                FieldName = fname
                                            };
                                            ruleElement.Rules.AddRange(paramsRules);
                                            variant.Elements.Add(ruleElement);

                                        }
                                        break;
                                    }

                                    default:
                                    {
                                        if (prevPosition == fragmentPosition - t.Length
                                            && variant.Elements.Count > 0
                                            && variant.Elements[variant.Elements.Count - 1] is EqualElement equalElement)
                                        {
                                            equalElement.Value += t;
                                        }
                                        else
                                        {
                                            var eq = new EqualElement() { Value = t };
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
            int parsedLength = 0;
            var t = root.Parse(text, 0, out parsedLength);

            if (t == null)
            {
                var src = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int line = 0;
                while (src[line].Length < parsedLength)
                    parsedLength -= src[line++].Length;
                line++;
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