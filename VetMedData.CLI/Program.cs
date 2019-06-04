using System;
using System.IO;

namespace VetMedData.CLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && File.Exists(args[0]))
            {
                MatchRunner.MatchFile(args[0]);
            }
            else if (args.Length > 0 && args[0].Equals("print", StringComparison.InvariantCultureIgnoreCase))
            {
                MatchRunner.PrintPIDProperty(args[1]);
            }
            else if (args.Length > 1 && args[0].Equals("explainmatch"))
            {
                MatchRunner.ExplainMatch(args[1], args[2]);
            }
            else if (args.Length > 2 && args[0].Equals("explain"))
            {
                MatchRunner.Explain(args[1], args[2]);
            }
            else if (args.Length > 1 && args[0].Equals("optimise", StringComparison.InvariantCultureIgnoreCase))
            {
                Optimisation.GALearnWeights(args[1]);
            }
            else
            {
                Console.WriteLine("Requires path to file to process as first argument.");
                Console.ReadLine();
            }
        }
    }
}
