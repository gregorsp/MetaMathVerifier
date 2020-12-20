using System.Collections.Generic;

namespace MetaMathVerifier
{
    public class Axiom : MMStatement
    {
        public SymbolString Result = new SymbolString();
        public List<Hypothesis> Hypotheses = new List<Hypothesis>();
        public HashSet<Distinct> Distinct;
    }
}