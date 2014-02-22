using System;
using System.Collections.Generic;
using System.Text;

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
            public FiniteAutomaton Format { get; set; }

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
                TreeNode res = new TreeNode();
                int spos = pos;
                int rindex = 0;
                int tlen = 0;
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
                    var t = Elements[i].Parse(text, pos, out tlen);
                    if (tlen > 0) parsedLen += tlen;
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
                            t.Name += rindex.ToString();
                            rindex++;
                        }
                        else
                            rindex = 0;
                    }
                    if (t != null)
                    {
                        res.NextNodes.Add(t);
                        pos += t.Value.Length;
                    }
                }
                res.Value = text.Substring(spos, pos - spos);
                res.Name = Name;
                return res;
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
                TreeNode res = new TreeNode() { Name = this.FieldName };
                string stext = text.Substring(pos);
                for (int i = 0; i < Rules.Count; i++)
                {
                    string s = Rules[i].Format.MaxSatisfying(stext);
                    if (s.Length > res.Value.Length)
                        res.Value = s;
                }
                parsedLen = 0;
                if (res.Value == "")
                    return null;
                parsedLen = res.Value.Length;
                return res;
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
                if (pos + Value.Length >= text.Length)
                    return null;
                var t = text.Substring(pos, Value.Length);
                if (t == Value)
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
            string[] acode = pattern.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            int line = 0;
            int chari = 0;
            Func<bool> lineFeed = () =>
            {
                if (line >= acode.Length)
                    return false;
                if (chari >= acode[line].Length)
                {
                    chari -= acode[line].Length;
                    line++;
                    return true;
                }
                return false;
            };
            while (line < acode.Length)
            {
                while (lineFeed()) ;
                if (line == acode.Length)
                    break;
                string tok = getToken(acode[line], ref chari);
                switch (tok)
                {
                    case "rule":
                        {
                            lineFeed();
                            tok = getToken(acode[line], ref chari);
                            if (!isValidName(tok))
                                throw new ArgumentException("Invalid rule name " + tok + " (" + line + ", " + chari + ")");
                            if (rules.ContainsKey(tok))
                                throw new ArgumentException("Try to redefine rule " + tok + " (" + line + ", " + chari + ")");
                            if (fragments.ContainsKey(tok))
                                throw new ArgumentException("Try to redefine fragment " + tok + " (" + line + ", " + chari + ")");
                            Rule rule = new Rule();
                            rule.Name = tok;
                            rules.Add(rule.Name, rule);
                            line++;
                            string code = "";
                            while (acode[line].Trim() != "")
                            {
                                code += acode[line].Trim();
                                line++;
                            }
                            chari = 0;
                            try
                            {
                                rule.Format = new FiniteAutomaton(code);
                            }
                            catch
                            {
                                throw new ArgumentException("Invalid rule define " + rule.Name + " (" + line + ", " + chari + ")"); 
                            }
                            break;
                        }
                    case "fragment":
                        {
                            tok = getToken(acode[line], ref chari);
                            if (!isValidName(tok))
                                throw new ArgumentException("Invalid fragment name " + tok + " (" + line + ", " + chari + ")");
                            if (rules.ContainsKey(tok))
                                throw new ArgumentException("Try to redefine rule " + tok + " (" + line + ", " + chari + ")");
                            if (fragments.ContainsKey(tok))
                                throw new ArgumentException("Try to redefine fragment " + tok + " (" + line + ", " + chari + ")");
                            Fragment frag = new Fragment(tok);
                            fragments.Add(tok, frag);
                            List<string> code = new List<string>();
                            line++;
                            while ((line < acode.Length) && (acode[line].Trim() != ""))
                            {
                                code.Add(acode[line]);
                                line++;
                            }
                            chari = 0;
                            for (int i = 0; i < code.Count; i++)
                            {
                                FragmentVariant variant = new FragmentVariant(tok + i.ToString());
                                frag.Variants.Add(variant);
                                string trimcode = code[i].Trim();
                                for (int j = 0; j < trimcode.Length; )
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
                                                    throw new ArgumentException("Invalid field name " + fname + " (" + (line - acode.Length + i) + ", " + chari + ")");
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
                            throw new ArgumentException("Invalid token " + tok + " (" + line + ", " + chari + ")");
                        }
                }
            }
            root = fragments["root"];
        }

        public TreeNode CreateTree(string text)
        {
            int pl = 0;
            var t = root.Parse(text, 0, out pl);
            if (t.Value != text)
            {
                var src = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int l = 0;
                while (src[l].Length < pl)
                    pl -= src[l++].Length;
                l++;
                throw new ArgumentException("Syntaxis error at " + l + ":" + pl);
            }
            return t;
        }
    }
}
