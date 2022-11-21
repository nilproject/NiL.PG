namespace NiL.PG
{
    public partial class Parser
    {
        private abstract class Element
        {
            public bool Repeated { get; set; }

            public string FieldName { get; set; }

            public abstract TreeNode Parse(string text, int position, out int maxAchievedPosition);
        }
    }
}