using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaMathVerifier
{
    public class FrameStack
    {
        public Stack<Frame> stack = new Stack<Frame>();
        HashSet<string> usedVariables = new HashSet<string>();
        public void Push()
        {
            stack.Push(new Frame());
        }
        public void Pop()
        {
            stack.Pop();
        }
        public void AddC(string tok)
        {
            if (stack.Count > 1)
                throw new Exception("Constants must be declared in the outermost block.");
            var frame = stack.Peek();
            if (usedVariables.Contains(tok))
                throw new Exception("A name defined as constant was already used as a variable.");
            if (LookupToken(tok))
                throw new Exception("Name already active. Can't define constant " + tok);
            frame.C.Add(tok);
        }
        public void AddV(string tok)
        {
            var frame = stack.Peek();
            if (LookupToken(tok))
                throw new Exception("Name already active. Can't define variable " + tok);
            usedVariables.Add(tok);
            frame.V.Add(tok);
        }
        public void AddF(string label, SymbolString symbols)
        {
            if (symbols.Count != 2)
                throw new Exception("$f statement must have two symbols.");
            string var = symbols[1], kind = symbols[0];
            if (!LookupV(var))
                throw new Exception("var in $f not defined: " + var);
            if (!LookupC(kind))
                throw new Exception("const in $f not defined: " + kind);
            var frame = stack.Peek();
            foreach (var f in frame.Hyps.OfType<FHyp>())
                if (f.Statement[1] == var)
                    throw new Exception("var in $f already defined in scope");
            FHyp val = new FHyp { Statement = symbols, Name = label };
            frame.Hyps.Add(val);
            AddStatement(label, val);
        }
        public void AddE(string label, SymbolString stat)
        {
            if (!LookupC(stat[0]))
                throw new Exception("A constant in an $e statement was not defined: " + stat[0]);
            for (int i = 1; i < stat.Count; i++)
            {
                if (LookupF(stat[i]) == null && !LookupC(stat[i]))
                    throw new Exception("A symbol used in an $e statement was not defined: " + stat[i]);
            }
            EHyp eh = new EHyp { Statement = stat, Name = label };
            stack.Peek().Hyps.Add(eh);
            AddStatement(label, eh);
        }
        public void AddD(List<string> stat)
        {
            var frame = stack.Peek();
            for (int i = 0; i < stat.Count; i++)
            {
                for (int j = i + 1; j < stat.Count; j++)
                {
                    Distinct d = new Distinct { X = stat[i], Y = stat[j] };
                    if (d.X.CompareTo(d.Y) == 1)
                    {
                        string t = d.X;
                        d.X = d.Y;
                        d.Y = t;
                    }
                    if (d.X != d.Y)
                        frame.D.Add(d);
                }
            }
        }
        public void AddA(string label, SymbolString stat)
        {
            Axiom a = new Axiom { Result = stat };
            MakeAssertion(stat, a);
            AddStatement(label, a);
        }
        public void AddP(string label, Theorem th)
        {
            MakeAssertion(th.Result, th);
            AddStatement(label, th);
        }
        void AddStatement(string label, MMStatement st)
        {
            var dict = stack.Peek().Statements;
            st.Name = label;
            if (st is Axiom)
                dict = stack.Last().Statements;
            if (LookupToken(label))
                throw new Exception("Label " + label + " is already declared.");
            dict[label] = st;
        }
        public bool LookupC(string tok)
        {
            foreach (Frame f in stack)
                if (f.C.Contains(tok))
                    return true;
            return false;
        }
        public bool LookupV(string tok)
        {
            foreach (Frame f in stack)
                if (f.V.Contains(tok))
                    return true;
            return false;
        }
        public bool LookupToken(string varOrConst)
        {
            return LookupC(varOrConst) || LookupV(varOrConst) || LookupStatement(varOrConst) != null;
        }
        public string LookupF(string var)
        {
            foreach (Frame frame in stack)
            foreach (FHyp f in frame.Hyps.OfType<FHyp>())
                if (f.Statement[1] == var)
                    return f.Statement[0];
            return null;
        }
        public bool LookupD(string x, string y)
        {
            if (x.CompareTo(y) == 1)
            {
                string t = x;
                x = y; y = t;
            }
            foreach (Frame f in stack)
                if (f.D.Contains(new Distinct { X = x, Y = y }))
                    return true;
            return false;
        }
        public MMStatement LookupStatement(string label)
        {
            foreach (Frame f in stack)
                if (f.Statements.ContainsKey(label))
                    return f.Statements[label];
            return null;
        }
        void MakeAssertion(IEnumerable<string> stat, Axiom item)
        {
            if (!LookupC(stat.First()))
                throw new Exception("First symbol in assertion must be a constant, not " + stat.First());
            HashSet<string> vars = new HashSet<string>();
            foreach (string s in stat.Skip(1))
            {
                var f = LookupF(s);
                if (!LookupC(s) && f == null && !(LookupStatement(s) is EHyp))
                    throw new Exception("Undeclared symbol in assertion: " + s);
                if (f != null) vars.Add(s);
            }
            var hyps = new List<Hypothesis>();
            var mandVars = new HashSet<string>();
            foreach (Frame f in stack.Reverse())
            foreach (Hypothesis hyp in f.Hyps)
                if ((hyp is FHyp) && vars.Contains(hyp.Statement[1]) || (hyp is EHyp))
                    hyps.Add(hyp);
            Frame frame = stack.Last();
            var visible = new List<IEnumerable<string>>(hyps.Select(h => h.Statement.UC()));
            visible.Add(stat);
            foreach (IEnumerable<string> hyp in visible)
            foreach (string tok in hyp)
                if (LookupV(tok))
                    mandVars.Add(tok);
            var dm = new HashSet<Distinct>();
            foreach (Frame f in stack)
            foreach (Distinct d in f.D)
                if (mandVars.Contains(d.X) && mandVars.Contains(d.Y))
                    dm.Add(d);
            var mandHyps = new List<Hypothesis>();
            foreach (Frame fr in stack.Reverse())
            foreach (var h in fr.Hyps)
            {
                if (h is FHyp)
                {
                    if (mandVars.Contains(h.Statement[1]))
                    {
                        mandHyps.Add(h);
                        mandVars.Remove(h.Statement[1]);
                    }
                }
                else
                    mandHyps.Add(h);
            }
            item.Distinct = dm;
            item.Hypotheses = mandHyps;
        }
    }
}