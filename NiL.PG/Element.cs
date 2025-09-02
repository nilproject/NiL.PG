using System.Collections.Generic;

namespace NiL.PG
{
    public partial class Parser
    {
        private abstract class Element
        {
            public bool Repeated { get; set; }

            public bool Optional { get; set; }

            public string FieldName { get; set; }

            public abstract TreeNode Parse(string text, int position, out int maxAchievedPosition, Dictionary<(Fragment Fragment, int Position), TreeNode> processedFragments);
        }
    }
}