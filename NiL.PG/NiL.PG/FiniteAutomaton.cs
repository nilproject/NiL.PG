using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NiL.PG
{
    /// <summary>
    /// Конечный строковый автомат
    /// </summary>
    /// <typeparam name="T">Тип значения в конечных состояниях</typeparam>
    public class FiniteAutomaton<T>
    {
        protected class _State
        {
            public bool Final { get; set; }
            public char Value { get; set; }
            public List<_State> Nexts { get; private set; }
            public bool Fictive { get; set; }

            public _State()
            {
                Nexts = new List<_State>();
            }

            public override string ToString()
            {
                return Value + (Final ? " final" : "");
            }
        }

        protected _State start;
        protected _State final;
        public T Value { get; protected set; }

        public T this[string x]
        {
            get 
            {
                return default (T);
            }
        }

        private static FiniteAutomaton<T> compilePattern(string pattern, T value)
        {
            FiniteAutomaton<T> auto = new FiniteAutomaton<T>(value);
            List<List<FiniteAutomaton<T>._State>> prew = new List<List<FiniteAutomaton<T>._State>>();
            //Stack<int> charscount = new Stack<int>();
            prew.Add(new List<FiniteAutomaton<T>._State>() { auto.start });
            //charscount.Push(0);
            char tok = '\0';
            bool setFinal = true;
            bool orSec = false;
            for (int i = 0; i < pattern.Length; i++)
            {
                switch (tok = pattern[i])
                {
                    case '\\':
                        {
                            i++;
                            tok = pattern[i];
                            goto default;
                        }
                    case ' ':
                        {
                            throw new ArgumentException("Space");
                        }
                    case '}':
                        {
                            throw new ArgumentException("Invalid char");
                        }
                    case '|':
                        {
                            if ((pattern[i - 1] == ']') || (i == 0))
                                throw new ArgumentException("Invalid \"or\" term");
                            orSec = true;
                            break;
                        }
                    case '{':
                        {
                            bool repeated = false;
                            for (int k = i; k < pattern.Length; k++)
                            {
                                if (pattern[k] == '}')
                                {
                                    if (pattern.Length > k + 1)
                                        repeated = pattern[k + 1] == '*';
                                    break;
                                }
                            }
                            bool work = true;
                            while (work)
                            {
                                i++;
                                char b = char.MinValue;
                                char e = char.MaxValue;
                                while ((pattern[i] != ',') && (pattern[i] != '}'))
                                    switch (pattern[i])
                                    {
                                        case ' ':
                                            {
                                                i++;
                                                break;
                                            }
                                        case ',':
                                            {
                                                throw new ArgumentException("Invalid char");
                                            }
                                        case '\\':
                                            {
                                                i++;
                                                goto default;
                                            }
                                        case '.':
                                            {
                                                i++;
                                                if (pattern[i] != '.')
                                                    throw new ArgumentException("Invalid range define");
                                                i++;
                                                switch (pattern[i])
                                                {
                                                    case ',':
                                                        {
                                                            throw new ArgumentException("Invalid range define");
                                                        }
                                                    case '\\':
                                                        {
                                                            i++;
                                                            goto default;
                                                        }
                                                    default:
                                                        {
                                                            e = pattern[i];
                                                            break;
                                                        }
                                                }
                                                i++;
                                                break;
                                            }
                                        case '{':
                                            {
                                                throw new ArgumentException("Invalid char in range define");
                                            }
                                        default:
                                            {
                                                b = pattern[i];
                                                if (pattern[i + 1] != '.')
                                                {
                                                    if ((pattern[i + 1] != ',') && (pattern[i + 1] != '}'))
                                                        throw new ArgumentException("Invalid range define");
                                                    e = pattern[i];
                                                }
                                                i++;
                                                break;
                                            }
                                    }
                                for (char ci = b; ci <= e; ci++)
                                {
                                    FiniteAutomaton<T>._State state = new FiniteAutomaton<T>._State() { Value = ci, Final = false };

                                    if (repeated)
                                    {
                                        state.Nexts.Add(state);
                                    }
                                    if (orSec)
                                    {
                                        foreach (var s in prew[prew.Count - 2])
                                            s.Nexts.Add(state);
                                        prew[prew.Count - 1].Add(state);
                                    }
                                    else
                                    {
                                        foreach (var s in prew[prew.Count - 1])
                                            s.Nexts.Add(state);
                                        prew.Add(new List<FiniteAutomaton<T>._State>() { state });
                                    }
                                    orSec = true;
                                }
                                if (pattern[i] == '}')
                                    work = false;
                            }
                            orSec = false;
                            if ((i + 1 < pattern.Length) && (repeated))
                                i++;
                            break;
                        }
                    case '(':
                    case '[':
                        {
                            if ((tok == '[') && (orSec))
                                throw new ArgumentException("Invalid \"or\" term");
                            Stack<char> bracket = new Stack<char>();
                            bracket.Push(tok);
                            char ttok = '\0';
                            int t = i + 1;
                            for (; (t < pattern.Length) && (bracket.Count > 0); t++)
                            {
                                switch (ttok = pattern[t])
                                {
                                    case ' ':
                                        {
                                            break;
                                        }
                                    case '[':
                                    case '(':
                                        {
                                            bracket.Push(ttok);
                                            break;
                                        }
                                    case ']':
                                        {
                                            if (bracket.Peek() != '[')
                                                throw new ArgumentException("Invalid braket");
                                            bracket.Pop();
                                            break;
                                        }
                                    case ')':
                                        {
                                            if (bracket.Peek() != '(')
                                                throw new ArgumentException("Invalid braket");
                                            bracket.Pop();
                                            break;
                                        }
                                }
                            }
                            var tempa = compilePattern(pattern.Substring(i + 1, t - i - 2), value);

                            Action<List<_State>, List<_State>> concatNexts = null;
                            concatNexts = (x, y) =>
                            {
                                for (int j = 0; j < y.Count; j++)
                                {
                                    var index = x.FindIndex((f) => { return f.Value == y[j].Value; });
                                    if (index != -1)
                                    {
                                        x[index].Final |= y[j].Final;
                                        concatNexts(x[index].Nexts, y[j].Nexts);
                                    }
                                    else
                                    {
                                        x.Add(y[j]);
                                    }
                                }
                            };

                            if (pattern.Length != t)
                            {
                                tempa.final.Final = false;
                                if (pattern[t] == '*')
                                {
                                    if (tempa.start.Fictive)
                                        tempa.final.Nexts.AddRange(tempa.start.Nexts);
                                        //concatNexts(tempa.final.Nexts, tempa.start.Nexts);
                                    else
                                        tempa.final.Nexts.Add(tempa.start);
                                    t++;
                                }
                            }

                            if (orSec)
                            {
                                foreach (var s in prew[prew.Count - 2])
                                    if (tempa.start.Fictive)
                                        concatNexts(s.Nexts, tempa.start.Nexts);
                                    else
                                    {
                                        var index = s.Nexts.FindIndex((x) => { return x.Value == tempa.start.Value; });
                                        if (index != -1)
                                        {
                                            s.Nexts[index].Final |= tempa.start.Final;
                                            concatNexts(s.Nexts[index].Nexts, tempa.start.Nexts);
                                        }
                                        else
                                            s.Nexts.Add(tempa.start);
                                    }
                                tempa.final.Final = false;
                                prew[prew.Count - 1].Add(tempa.final);
                            }
                            else
                            {
                                foreach (var s in prew[prew.Count - 1])
                                    if (tempa.start.Fictive)
                                        s.Nexts.AddRange(tempa.start.Nexts);
                                    //concatNexts(s.Nexts, tempa.start.Nexts);
                                    else
                                        s.Nexts.Add(tempa.start);
                                if (tok == '(')
                                {
                                    if ((pattern.Length >= t) && (pattern[t - 1] == '*'))
                                    {
                                        prew[prew.Count - 1].Add(tempa.final);
                                    }
                                    else
                                        prew.Add(new List<FiniteAutomaton<T>._State>() { tempa.final });
                                }
                                else
                                {
                                    prew[prew.Count - 1].Add(tempa.final);
                                }
                            }
                            i = t - 1;
                            break;
                        }
                    default:
                        {
                            FiniteAutomaton<T>._State state = new FiniteAutomaton<T>._State() { Value = tok, Final = pattern.Length == i + 1 };
                            
                            if (!state.Final)
                            {
                                if (pattern[i + 1] == '*')
                                {
                                    i++;
                                    state.Nexts.Add(state);
                                }
                            }
                            else 
                            {
                                if (!orSec)
                                {
                                    setFinal = false;
                                    auto.final = state;
                                }
                                else
                                    state.Final = false;
                            }
                            if (orSec)
                            {
                                foreach (var s in prew[prew.Count - 2])
                                    s.Nexts.Add(state);
                                prew[prew.Count - 1].Add(state);
                                orSec = false;
                            }
                            else
                            {
                                foreach (var l in prew[prew.Count - 1])
                                    l.Nexts.Add(state);
                                if (pattern[i] == '*')
                                    prew[prew.Count - 1].Add(state);
                                else
                                    prew.Add(new List<FiniteAutomaton<T>._State>() { state });
                            }
                            break;
                        }
                }
            };
            if (setFinal)
                foreach (var l in prew[prew.Count - 1])
                {
                    l.Nexts.Add(auto.final);
                    l.Final = true;
                }
            if (auto.start.Nexts.Count == 1)
                auto.start = auto.start.Nexts[0];
            return auto;
        }

        private FiniteAutomaton(T value)
        {
            final = new _State() { Fictive = true, Final = true };
            start = new _State() { Fictive = true };
            Value = value; 
        }

        public FiniteAutomaton(string pattern, T value)
        {
            var t = compilePattern(pattern, value);
            start = t.start;
            final = t.final;
            Value = value;
        }

        public virtual T Get(string text)
        {
            var s = start;
            int i = 0;
            if (!s.Fictive)
            {
                if (s.Value != text[0])
                    return default (T);
                i++;
            }
            while (i < text.Length)
            {
                int j = 0;
                bool f = false;
                for (; j < s.Nexts.Count; j++)
                {
                    if (s.Nexts[j].Fictive)
                    {
                        var t = s.Nexts[j].Nexts;
                        s.Nexts.RemoveAt(j);
                        s.Nexts.InsertRange(j, t);
                        j--;
                    }
                    else
                    {
                        if (s.Nexts[j].Value == text[i])
                        {
                            s = s.Nexts[j];
                            i++;
                            f = true;
                            break;
                        }
                    }
                }
                if ((s == null) || (!f))
                    return default (T);
            }
            if (s.Final)
                return Value;
            else
                return default (T);
        }

        public string MaxSatisfying(string text)
        {
            var s = start;
            int i = 0;
            int r = 0;
            if (!s.Fictive)
            {
                if (s.Value != text[0])
                    return "";
                i++;
            }
            while (i < text.Length)
            {
                int j = 0;
                bool f = false;
                for (; j < s.Nexts.Count; j++)
                {
                    if (s.Nexts[j].Fictive)
                    {
                        var t = s.Nexts[j].Nexts;
                        s.Nexts.RemoveAt(j);
                        s.Nexts.InsertRange(j, t);
                        if (t.Count != 0)
                            j--;
                    }
                    else
                    {
                        if (s.Nexts[j].Value == text[i])
                        {
                            s = s.Nexts[j];
                            i++;
                            r = i;
                            f = true;
                            break;
                        }
                    }
                }
                if ((s == null) || (!f))
                    return text.Substring(0, r);
            }
            if (s.Final)
                return text;
            else
                return text.Substring(0, r);
        }
    }

    public class FiniteAutomaton : FiniteAutomaton<bool>
    {
        public FiniteAutomaton(string pattern)
            : base(pattern, true)
        {
 
        }

        public override bool Get(string text)
        {
            return base.Get(text);
        }
    }
}
