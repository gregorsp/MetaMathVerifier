using System;
using System.Threading.Tasks;
using MetaMathVerifier;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = MMFile.FromFile(@"C:\mmj2\set.mm");
            var verifier = new Verifier();
            for (var i = 0; i < file.Statements.Count; i++)
            {
                var theorem = (Theorem) file.Statements[i];
                verifier.Verify(theorem);
                Console.WriteLine($"{theorem.Result} is true");
            }

            Console.ReadLine();
        }
    }
}
