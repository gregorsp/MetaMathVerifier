using System.Linq;

namespace MetaMathVerifier
{
    public abstract class Hypothesis : MMStatement
    {
        public SymbolString Statement;
        public override bool Equals(object obj)
        {
            return obj is EHyp && Statement.SequenceEqual(((EHyp)obj).Statement);
        }
        public override int GetHashCode()
        {
            return Statement.GetHashCode();
        }
    }
}