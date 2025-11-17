using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace NiL.PG
{
    public partial class Parser
    {
        private class FragmentVariant
        {
            private record ParseVariantListNode(
                ParseVariantListNode? Parent,
                TreeNode TreeNode,
                Element? Element)
            {
                public bool Valid = true;
                public int Count = (Parent?.Count ?? 0) + 1;
            }

            public string FragmentName { get; }
            public int VariantIndex { get; }
            public List<Element> Elements { get; }

            public FragmentVariant(string fragmentName, int variantIndex)
            {
                Elements = new List<Element>();
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

            private void parse(
                ParseVariantListNode? parentNode,
                int elementIndex,
                ref List<ParseVariantListNode>? parseVariantLeafs,
                string text,
                int position,
                ref int maxAchievedPosition,
                Dictionary<(Fragment Fragment, int Position), TreeNode[]?> processedFragments)
            {
                if (elementIndex == Elements.Count)
                {
                    (parseVariantLeafs ??= []).Add(parentNode!);
                    return;
                }

                var parsedFragment = Elements[elementIndex].Parse(text, position, ref maxAchievedPosition, processedFragments);
                if (parsedFragment == null)
                {
                    if (Elements[elementIndex].Optional
                        || (Elements[elementIndex].Repeated && parentNode?.Element != Elements[elementIndex]))
                    {
                        parse(
                            parentNode,
                            elementIndex + 1,
                            ref parseVariantLeafs,
                            text,
                            position,
                            ref maxAchievedPosition,
                            processedFragments);
                    }
                }
                else
                {
                    for (int i = 0; i < parsedFragment.Length; i++)
                    {
                        var node = new ParseVariantListNode(parentNode, parsedFragment[i], Elements[elementIndex]);

                        if (Elements[elementIndex].Repeated)
                        {
                            parse(
                                node,
                                elementIndex,
                                ref parseVariantLeafs,
                                text,
                                parsedFragment[i].Position + parsedFragment[i].Value.Length,
                                ref maxAchievedPosition,
                                processedFragments);
                        }

                        parse(
                            node,
                            elementIndex + 1,
                            ref parseVariantLeafs,
                            text,
                            parsedFragment[i].Position + parsedFragment[i].Value.Length,
                            ref maxAchievedPosition,
                            processedFragments);
                    }

                    if (Elements[elementIndex].Optional)
                    {
                        parse(
                            parentNode,
                            elementIndex + 1,
                            ref parseVariantLeafs,
                            text,
                            position,
                            ref maxAchievedPosition,
                            processedFragments);
                    }
                }
            }

            public FragmentTreeNode[]? Parse(string text, int position, ref int maxAchievedPosition, Dictionary<(Fragment Fragment, int Position), TreeNode[]?> processedFragments)
            {
                if (position > maxAchievedPosition)
                    maxAchievedPosition = position;

                List<ParseVariantListNode>? parseVariantLeafs = null;
                parse(null, 0, ref parseVariantLeafs, text, position, ref maxAchievedPosition, processedFragments);

                if (parseVariantLeafs != null)
                {
                    var result = new FragmentTreeNode[parseVariantLeafs.Count];
                    for (var i = 0; i < parseVariantLeafs.Count; i++)
                    {
                        var node = parseVariantLeafs[i];
                        var variant = new FragmentTreeNode(FragmentName)
                        {
                            Children = new TreeNode[node.Count],
                            FragmentName = FragmentName,
                        };

                        result[i] = variant;

                        var endPos = node.TreeNode.Position + node.TreeNode.Value.Length;

                        while (true)
                        {
                            variant.Children[node.Count - 1] = node.TreeNode;
                            if (node.Parent == null)
                                break;

                            node = node.Parent;
                        }

                        variant.Position = node.TreeNode.Position;
                        variant.Value = text[variant.Position..endPos];
                    }

                    if (result.Length > 1)
                    {
                        if (result.Length == 2)
                        {
                            if (result[0].Value.Length == result[1].Value.Length
                                && result[0].Position == result[1].Position)
                            {
                                result[0].Value = result[1].Value;
                                result[0].AlternativeInterpretations = [result[1]];
                                return [result[0]];
                            }
                        }
                        else
                        {
                            var keys = new int[result.Length];
                            for (var i = 0; i < result.Length; i++)
                                keys[i] = result[i].Value.Length;

                            Array.Sort(keys, result);
                            List<FragmentTreeNode>? buffer = null;
                            for (var i = 0; i < result.Length; i++)
                            {
                                if (result[i] == null)
                                    continue;

                                for (var j = i + 1; j < result.Length; j++)
                                {
                                    if (result[j] == null)
                                        continue;

                                    if (result[j].Value.Length > result[i].Value.Length)
                                        break;

                                    if (result[i].Position == result[j].Position
                                        && result[i].Value.Length == result[j].Value.Length)
                                    {
                                        result[j].Value = result[i].Value;
                                        (buffer ??= []).Add(result[j]);
                                        result[j] = null!;
                                    }
                                }

                                if (buffer is { Count: > 0 })
                                {
                                    result[i].AlternativeInterpretations = [.. buffer];
                                    buffer.Clear();
                                }
                            }

                            if (buffer != null)
                            {
                                for (var i = 0; i < result.Length; i++)
                                {
                                    if (result[i] != null)
                                        buffer.Add(result[i]);
                                }

                                result = [.. buffer];
                            }
                        }
                    }

                    return result!;
                }

                return null;
            }
        }
    }
}