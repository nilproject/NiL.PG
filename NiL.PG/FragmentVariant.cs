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

            public FragmentTreeNode Parse(string text, int position, out int maxAchievedPosition)
            {
                FragmentTreeNode result = null;
                int startPos = position;
                int instanceIndex = 0;
                maxAchievedPosition = position;
                for (int i = 0; i < Elements.Count; i++)
                {
                    while ((text.Length > position) && char.IsWhiteSpace(text[position])) position++;
                    if (position == text.Length)
                    {
                        if (Elements[i].Repeated)
                            break;

                        return null;
                    }

                    var parsedFragment = Elements[i].Parse(text, position, out var maxPos);

                    if (maxPos > maxAchievedPosition)
                        maxAchievedPosition = maxPos;

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
                        position += parsedFragment.Value.Length;
                    }
                }

                if (result == null)
                    result = new FragmentTreeNode(FragmentName);

                result.Value = text.Substring(startPos, position - startPos);
                return result;
            }
        }
    }
}