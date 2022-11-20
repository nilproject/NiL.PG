using System.Collections.Generic;

namespace NiL.PG
{
    public partial class Parser
    {
        private class FragmentVariant
        {
            public string FragmentName { get; }
            public int VariantIndex { get; }
            public List<Element> Elements { get; }

            public FragmentVariant(string fragmentName, int variantIndex)
            {
                Elements = new List<Element>();
                FragmentName = "";
                FragmentName = fragmentName;
                VariantIndex = variantIndex;
            }

            public override string ToString()
            {
                string res = "";
                for (int i = 0; i < Elements.Count; i++)
                    res += Elements[i].ToString() + (i + 1 < Elements.Count ? " " : "");
                return res;
            }

            public FragmentTreeNode Parse(string text, int pos, out int parsedLen)
            {
                FragmentTreeNode result = null;
                int spos = pos;
                int instanceIndex = 0;
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

                    var parsedFragment = Elements[i].Parse(text, pos, out int tokenLen);
                    if (tokenLen > 0) parsedLen += tokenLen;
                    if (parsedFragment == null)
                    {
                        if (!Elements[i].Repeated)
                            return null;
                    }
                    else
                    {
                        if (Elements[i].Repeated)
                        {
                            i--;
                            parsedFragment.Name += instanceIndex.ToString();
                            instanceIndex++;
                        }
                        else
                            instanceIndex = 0;
                    }

                    if (parsedFragment != null)
                    {
                        if (result == null)
                            result = new FragmentTreeNode(FragmentName);

                        result.Children.Add(parsedFragment);
                        pos += parsedFragment.Value.Length;
                    }
                }

                if (result == null)
                    result = new FragmentTreeNode(FragmentName);

                result.Value = text.Substring(spos, pos - spos);
                return result;
            }
        }
    }
}