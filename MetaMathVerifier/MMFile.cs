using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MetaMathVerifier
{
    public class MMFile
    {
        class TokenReader
        {
            StreamReader lines;
            public TokenReader(StreamReader lines)
            {
                this.lines = lines;
                buffer = new Queue<string>();
            }
            Queue<string> buffer;
            public string ReadToken()
            {
                while (buffer.Count == 0)
                {
                    string line = lines.ReadLine();
                    if (line == null) return null;
                    foreach (string s in line.Split()) buffer.Enqueue(s);
                }
                return buffer.Dequeue();
            }
        }
        public static MMFile FromFile(string file)
        {
            using (StreamReader r = new StreamReader(file))
                return FromStreamReader(r);
        }
        public static MMFile FromStream(Stream stream)
        {
            return FromStreamReader(new StreamReader(stream));
        }
        public static MMFile FromStreamReader(StreamReader streamReader)
        {
            TokenReader r = new TokenReader(streamReader);
            MMFile f = new MMFile();
            f.Read(r);
            return f;
        }
        string ReadC(TokenReader r)
        {
            while (true)
            {
                string tok = r.ReadToken();
                if (tok == null) return null;
                if (tok == "$(")
                {
                    do
                    {
                        tok = r.ReadToken();
                    } while (tok != "$)");
                }
                else if (tok != "")
                    return tok;
            }
        }
        SymbolString ReadStat(TokenReader r)
        {
            SymbolString ret = new SymbolString();
            while (true)
            {
                string tok = ReadC(r);
                if (tok == null)
                    throw new Exception("EOF before $.");
                else if (tok == "$.")
                    break;
                ret.Add(tok);
            }
            return ret;
        }
        FrameStack fs = new FrameStack();
        //int lineNbr, colNbr;
        void Read(TokenReader r)
        {
            fs.Push();
            string label = null;
            SymbolString stat = null;
            while (true)
            {
                string tok = ReadC(r);
                if (tok == null || tok == "$}") break;
                else if (tok == "$c")
                    ReadStat(r).ForEach(fs.AddC);
                else if (tok == "$v")
                    ReadStat(r).ForEach(fs.AddV);
                else if (tok == "$f")
                {
                    if (label == null)
                        throw new Exception("$f must have label");
                    stat = ReadStat(r);
                    if (stat.Count != 2)
                        throw new Exception("$f must have 2 symbols");
                    fs.AddF(label, stat);
                    label = null;
                }
                else if (tok == "$a")
                {
                    if (label == null)
                        throw new Exception("$a must have label");
                    stat = ReadStat(r);
                    fs.AddA(label, stat);
                    label = null;
                }
                else if (tok == "$e")
                {
                    if (label == null)
                        throw new Exception("$e must have label");
                    stat = ReadStat(r);
                    fs.AddE(label, stat);
                    label = null;
                }
                else if (tok == "$p")
                {
                    if (label == null)
                        throw new Exception("$p must have label");
                    stat = ReadStat(r);
                    int i = stat.IndexOf("$=");
                    if (i == -1)
                        throw new Exception("$p must contain proof after $=");
                    var result = stat.Take(i);
                    var proofString = stat.Skip(i + 1);
                    Theorem t = new Theorem { Name = label, Result = new SymbolString(result) };
                    fs.AddP(label, t);
                    if (proofString.First() == "(")
                    { //compressed proof
                        t.Proof = UncompressProof(fs, t, proofString).ToList();
                    }
                    else
                    {
                        t.Proof = GetStatements(fs, proofString).ToList();
                    }
                    if (t.Proof.Count(n => n is Axiom && ((Axiom)n).Hypotheses == null) > 0)
                        throw new Exception();
                    Statements.Add(t);
                    label = null;
                }
                else if (tok == "$d")
                {
                    stat = ReadStat(r);
                    fs.AddD(stat);
                }
                else if (tok == "${") Read(r);
                else if (tok[0] != '$')
                {
                    label = tok;
                }
                else
                    throw new Exception("Unexpected token " + tok);

            }
            fs.Pop();
        }

        private IEnumerable<MMStatement> GetStatements(FrameStack fs, IEnumerable<string> proofString)
        {
            List<MMStatement> stats = new List<MMStatement>();
            foreach (string s in proofString)
                stats.Add(fs.LookupStatement(s));
            return stats;
        }
        IEnumerable<MMStatement> UncompressProof(FrameStack fs, Theorem th, IEnumerable<string> proof)
        {
            var ints = UncompressProofNumbers(proof);

            int hypCount = th.Hypotheses.Count;

            int labelCount = proof.Skip(1).TakeWhile(n => n != ")").Count();

            int zCount = ints.Count(i => i == 0);

            List<MMStatement> ret = new List<MMStatement>();
            MMStatement stat = null;
            foreach (int j in ints)
            {
                int i = j;
                if (i == 0)
                    stat = new ZIStatement();
                else
                {
                    i -= 1;
                    if (i < hypCount)
                        stat = th.Hypotheses[i];
                    else
                    {
                        i -= hypCount;
                        if (i < labelCount)
                            stat = fs.LookupStatement(proof.ElementAt(i + 1));
                        else
                        {
                            i -= labelCount;
                            if (i < zCount)
                            {
                                stat = new ZRStatement { Num = i };
                            }
                            else
                                throw new Exception("Couldn't uncompress proof.");
                        }
                    }
                }
                ret.Add(stat);
            }
            return ret;
        }
        List<int> UncompressProofNumbers(IEnumerable<string> proof)
        {
            var it = proof.GetEnumerator();
            it.MoveNext();
            while (it.Current != ")")
            {
                it.MoveNext();
            }
            string comp = "";
            while (it.MoveNext())
            {
                comp += it.Current;
            }
            List<int> ret = new List<int>();
            int numStart = 0;
            while (numStart < comp.Length)
            {
                int numNext = numStart;
                while (comp[numNext] > 'T')
                {
                    numNext++;
                }
                int val = 0;
                for (int i = numStart; i < numNext; i++)
                {
                    val += 4 * (int)Math.Pow(5, (numNext - i)) * ((int)comp[i] - (int)'T');
                }
                val += (int)comp[numNext++] - (int)'A' + 1;
                ret.Add(val);
                if (comp.Length > numNext && comp[numNext] == 'Z')
                {
                    numNext++;
                    ret.Add(0);
                }
                numStart = numNext;
            }
            return ret;
        }

        public List<MMStatement> Statements = new List<MMStatement>();
    }

}
