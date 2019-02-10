using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NiL.PG
{
    public class Parser
    {
        public class TreeNode
        {
            public List<TreeNode> NextNodes { get; private set; }
            public string Name { get; set; }
            public string Value { get; set; }

            public TreeNode()
            {
                NextNodes = new List<TreeNode>();
                Name = "";
                Value = "";
            }

            public TreeNode this[string name]
            {
                get
                {
                    string[] path = name.Split('\\');
                    TreeNode c = this;
                    for (int i = 0; i < path.Length; i++)
                    {
                        bool cont = false;
                        foreach (var n in c.NextNodes)
                            if (n.Name == path[i])
                            {
                                c = n;
                                cont = true;
                                break;
                            }
                        if (cont)
                            continue;
                        return null;
                    }
                    return c;
                }
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private class Rule
        {
            public string Name { get; set; }
            public Regex RegExp { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private class FragmentVariant
        {
            public string Name { get; set; }
            public List<Element> Elements { get; private set; }

            public FragmentVariant()
            {
                Elements = new List<Element>();
                Name = "";
            }

            public FragmentVariant(string name)
                : this()
            {
                Name = name;
            }

            public override string ToString()
            {
                string res = "";
                for (int i = 0; i < Elements.Count; i++)
                    res += Elements[i].ToString() + (i + 1 < Elements.Count ? " " : "");
                return res;
            }

            public TreeNode Parse(string text, int pos, out int parsedLen)
            {
                TreeNode result = null;
                int spos = pos;
                int ruleIndex = 0;
                int tokenLen = 0;
                parsedLen = 0;
                for (int i = 0; i < Elements.Count; i++)
                {
                    while ((text.Length > pos) && char.IsWhiteSpace(text[pos])) pos++;
                    if (pos == text.Length)
                    {
                        if (Elements[i].Repeated)
                            break;
                        return null;
                    }

                    var t = Elements[i].Parse(text, pos, out tokenLen);
                    if (tokenLen > 0) parsedLen += tokenLen;
                    if (t == null)
                    {
                        if (!Elements[i].Repeated)
                            return null;
                    }
                    else
                    {
                        if (Elements[i].Repeated)
                        {
                            i--;
                            t.Name += ruleIndex.ToString();
                            ruleIndex++;
                        }
                        else
                            ruleIndex = 0;
                    }

                    if (t != null)
                    {
                        if (result == null)
                            result = new TreeNode();

                        result.NextNodes.Add(t);
                        pos += t.Value.Length;
                    }
                }

                if (result == null)
                    result = new TreeNode();

                result.Value = text.Substring(spos, pos - spos);
                result.Name = Name;

                return result;
            }
        }

        private class Fragment
        {
            public string Name { get; set; }
            public List<FragmentVariant> Variants { get; private set; }

            public Fragment(string name)
            {
                this.Name = name;
                Variants = new List<FragmentVariant>();
            }

            public override string ToString()
            {
                return Name;
            }

            public virtual TreeNode Parse(string text, int pos, out int parsedLen)
            {
                TreeNode res = null;
                int tlen = 0;
                parsedLen = 0;
                for (int i = 0; (i < Variants.Count) && (res == null); i++)
                {
                    res = Variants[i].Parse(text, pos, out tlen);
                    if (tlen > parsedLen)
                        parsedLen = tlen;
                }
                if (res != null)
                    res.Name = this.Name;
                return res;
            }
        }

        private abstract class Element
        {
            public bool Repeated { get; set; }
            public string FieldName { get; set; }

            public abstract TreeNode Parse(string text, int startpos, out int parsedLen);
        }

        private class RuleElement : Element
        {
            public List<Rule> Rules { get; set; }

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
                for (int i = 0; i < Rules.Count; i++)
                {
                    match = Rules[i].RegExp.Match(text, pos);
                    if (match.Index != pos || !match.Success)
                    {
                        match = null;
                    }
                    else
                    {
                        break;
                    }
                }

                if (match == null || !match.Success)
                {
                    parsedLen = 0;
                    return null;
                }

                parsedLen = match.Length;
                return new TreeNode
                {
                    Name = FieldName,
                    Value = match.Value,
                };
            }
        }

        private class FragmentElement : Element
        {
            public Fragment Fragment { get; set; }

            public override string ToString()
            {
                return "*" + FieldName + "(" + Fragment.ToString() + ")" + (Repeated ? "*" : "");
            }

            public override TreeNode Parse(string text, int pos, out int parsedLen)
            {
                var t = Fragment.Parse(text, pos, out parsedLen);
                if (t != null)
                    t.Name = FieldName;
                return t;
            }
        }

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
                    return new TreeNode() { Value = Value, Name = Value };
                }

                return null;
            }
        }

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

        public Parser(string pattern)
        {
            Dictionary<string, Rule> rules = new Dictionary<string, Rule>();
            Dictionary<string, Fragment> fragments = new Dictionary<string, Fragment>();
            string[] input = pattern.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
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
                string tok = getToken(input[line], ref position);
                switch (tok)
                {
                    case "rule":
                    {
                        lineFeed();
                        tok = getToken(input[line], ref position);

                        if (!isValidName(tok))
                            throw new ArgumentException("Invalid rule name " + tok + " (" + line + ", " + position + ")");

                        if (rules.ContainsKey(tok))
                            throw new ArgumentException("Try to redefine rule " + tok + " (" + line + ", " + position + ")");

                        if (fragments.ContainsKey(tok))
                            throw new ArgumentException("Try to redefine fragment " + tok + " (" + line + ", " + position + ")");

                        Rule rule = new Rule();
                        rule.Name = tok;
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
                        tok = getToken(input[line], ref position);

                        if (!isValidName(tok))
                            throw new ArgumentException("Invalid fragment name " + tok + " (" + line + ", " + position + ")");

                        if (rules.ContainsKey(tok))
                            throw new ArgumentException("Try to redefine rule " + tok + " (" + line + ", " + position + ")");

                        Fragment frag;
                        if (fragments.TryGetValue(tok, out frag))
                        {
                            if (frag.Variants.Count != 0)
                                throw new ArgumentException("Try to redefine fragment " + tok + " (" + line + ", " + position + ")");
                        }
                        else
                        {
                            frag = new Fragment(tok);
                            fragments.Add(tok, frag);
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
                            var variant = new FragmentVariant(tok + i.ToString());
                            frag.Variants.Add(variant);
                            string trimcode = code[i].Trim();
                            for (int j = 0; j < trimcode.Length;)
                            {
                                string t = getToken(trimcode, ref j);
                                switch (t)
                                {
                                    case "\\":
                                    {
                                        t = getToken(trimcode, ref j);
                                        goto default;
                                    }
                                    case "*":
                                    {
                                        string fname = getToken(trimcode, ref j);
                                        if (!isValidName(fname))
                                            throw new ArgumentException("Invalid field name " + fname + " (" + (line - input.Length + i) + ", " + position + ")");
                                        List<Rule> frules = new List<Rule>();
                                        List<Fragment> ffrags = new List<Fragment>();
                                        bool work = true;
                                        bool mrule = false;
                                        bool Repeated = false;
                                        while (work)
                                        {
                                            switch (trimcode[j])
                                            {
                                                case '*':
                                                {
                                                    Repeated = true;
                                                    j++;
                                                    break;
                                                }
                                                case ' ':
                                                {
                                                    work = mrule;
                                                    j++;
                                                    break;
                                                }
                                                case ',':
                                                {
                                                    if (mrule)
                                                    {
                                                        while (char.IsWhiteSpace(trimcode[++j])) ;
                                                        j--;
                                                        goto case '(';
                                                    }
                                                    else
                                                        goto default;
                                                }
                                                case '(':
                                                {
                                                    string rulname = "";
                                                    j++;
                                                    while ((trimcode[j] != ')') && (trimcode[j] != ','))
                                                        rulname += trimcode[j++];
                                                    if (!isValidName(rulname) || !(rules.ContainsKey(rulname) || fragments.ContainsKey(rulname)))
                                                        throw new ArgumentException("Invalid element define \"" + rulname + "\"");
                                                    if (rules.ContainsKey(rulname))
                                                        frules.Add(rules[rulname]);
                                                    if (fragments.ContainsKey(rulname))
                                                        ffrags.Add(fragments[rulname]);
                                                    mrule = trimcode[j] == ',';
                                                    if (!mrule) j++;
                                                    break;
                                                }
                                                default:
                                                {
                                                    work = false;
                                                    break;
                                                }
                                            }
                                            if (trimcode.Length <= j)
                                                work = false;
                                        }

                                        if ((frules.Count != 0) && (ffrags.Count != 0))
                                        {
                                            throw new ArgumentException("Field define can't contains rules and fragment together");
                                        }

                                        if (ffrags.Count > 1)
                                            throw new ArgumentException("Field define can't contain more than one fragment");

                                        if ((ffrags.Count == 0) && (frules.Count == 0))
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
                                            variant.Elements.Add(new RuleElement()
                                            {
                                                Repeated = Repeated,
                                                Rules = frules,
                                                FieldName = fname
                                            });
                                        }
                                        break;
                                    }
                                    default:
                                    {
                                        EqualElement eq = new EqualElement() { Value = t };
                                        variant.Elements.Add(eq);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    default:
                    {
                        throw new ArgumentException("Invalid token " + tok + " (" + line + ", " + position + ")");
                    }
                }
            }
            root = fragments["root"];
        }

        public TreeNode Parse(string text)
        {
            int parsedLength = 0;
            var t = root.Parse(text, 0, out parsedLength);
            if (t.Value != text)
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