using System.Text.RegularExpressions;

namespace NiL.PG
{
    public partial class Parser
    {
        private class Rule
        {
            public string Name { get; set; }
            public Regex RegExp { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}