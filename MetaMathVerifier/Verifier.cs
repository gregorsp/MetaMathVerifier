using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaMathVerifier
{
    public class Verifier
    {
        Dictionary<string, MMStatement> labels = new Dictionary<string, MMStatement>();
        string[] labelRequired = new string[] { "$a", "$e", "$f", "$p" };
        List<string> ApplySubst(IEnumerable<string> stat, Dictionary<string, List<string>> subst)
        {
            var res = new List<string>();
            foreach (string tok in stat)
            {
                if (subst.ContainsKey(tok))
                    res.AddRange(subst[tok]);
                else
                    res.Add(tok);
            }
            return res;
        }
        List<string> FindVars(List<string> stat, Theorem th)
        {
            List<string> vars = new List<string>();
            foreach (string x in stat)
                if (!vars.Contains(x) && th.Hypotheses.Where(n => n is FHyp && n.Statement[1] == x).Count() == 1)
                    vars.Add(x);
            return vars;
        }

        Stack<List<string>> Stack;
        Dictionary<string, List<string>> Subst;

        public void Verify(Theorem th)
        {
            Stack = new Stack<List<string>>();
            List<List<string>> stored = new List<List<string>>();
            foreach (MMStatement step in th.Proof)
            {
                if (step is FHyp)
                {
                    FHyp f = (FHyp)step;
                    Stack.Push(new List<string>(new string[] { f.Statement[0], f.Statement[1] }));
                }
                else if (step is EHyp)
                {
                    Stack.Push(((EHyp)step).Statement);
                }
                else if (step is ZIStatement)
                {
                    stored.Add(Stack.Peek());
                }
                else if (step is ZRStatement)
                {
                    Stack.Push(stored[((ZRStatement)step).Num]);
                }
                else if (step is Axiom)
                {
                    Axiom s = (Axiom)step;
                    int popCount = s.Hypotheses.Count;
                    if (popCount > Stack.Count)
                        throw new Exception("Stack underflow");
                    var hyps = new Stack<List<string>>();
                    foreach (var hyp in Stack.Take(popCount))
                        hyps.Push(hyp);
                    var subst = Subst = new Dictionary<string, List<string>>();
                    foreach (Hypothesis f in s.Hypotheses.OfType<FHyp>())
                    {
                        var entry = hyps.Pop().ToList();
                        if (entry[0] != f.Statement[0])
                        {
                            throw new Exception("Stack entry doesn't match mandatory var hyp");
                        }
                        entry.RemoveAt(0);
                        subst[f.Statement[1]] = entry;
                    }

                    foreach (Distinct d in s.Distinct)
                    {
                        var xVars = FindVars(subst[d.X], th);
                        var yVars = FindVars(subst[d.Y], th);
                        foreach (string gam in xVars)
                        foreach (string delt in yVars)
                            if (gam == delt)
                                throw new Exception("Disjoint violation for " + d.X + ", " + d.Y + "(" + gam + ")");
                    }
                    foreach (Hypothesis h in s.Hypotheses.OfType<EHyp>())
                    {
                        var entry = hyps.Pop();
                        var substH = ApplySubst(h.Statement, subst);
                        if (!entry.SequenceEqual(substH))
                        {
                            throw new Exception("$e hypothesis doesn't match stack.");
                        }
                    }
                    for (int i = 0; i < popCount; i++) Stack.Pop();
                    Stack.Push(ApplySubst(s.Result, subst));
                    Subst = null;
                }
            }
            if (Stack.Count == 0)
                throw new Exception("Stack is empty at end of proof.");
            else if (Stack.Count > 1)
                throw new Exception("Stack has more than one item at end of proof.");
            if (!Stack.Peek().SequenceEqual(th.Result))
                throw new Exception("Assertion proved doesn't match.");
        }
    }
}