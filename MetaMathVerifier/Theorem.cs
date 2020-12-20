using System.Collections.Generic;

namespace MetaMathVerifier
{
    public class Theorem : Axiom
    {
        public List<MMStatement> Proof = new List<MMStatement>();
    }
}