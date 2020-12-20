using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetaMathVerifier
{
    public class SymbolString : List<string>
    {
        public SymbolString(IEnumerable<string> items) : base(items) { }
        public SymbolString() { }
        public override string ToString()
        {
            return this.Aggregate(new StringBuilder(), (sb, s) => sb.Append(s + " "),
                sb =>
                {
                    sb.Length -= 1;
                    return sb.ToString();
                });
        }
    }
}