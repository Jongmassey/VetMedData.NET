using System;
using System.Collections.Generic;
using System.Linq;
using VetMedData.NET.Model;
using VetMedData.NET.Util;

namespace VetMedData.CLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "match":
                        MatchRunner.Match(args);
                        break;
                    case "optimise":
                        Optimisation.Optimise(args);
                        break;
                    case "print":
                        PrintPIDProperty(args[1]);
                        break;
                    default:
                        PrintUsage();
                        break;
                }
            }
            PrintUsage();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("keywords: match|optimise|print");
            Console.ReadLine();
        }

        internal static void PrintPIDProperty(string propName)
        {
            var pidProperties = typeof(VMDPID).GetProperties();
            try
            {
                var prop = pidProperties.Single(
                        p => p.Name.Equals(propName, StringComparison.InvariantCultureIgnoreCase));

                var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                                  PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                                  PidFactoryOptions.PersistentPid).Result;

                var values = (IEnumerable<string>)prop.GetValue(pid);
                foreach (var value in values.Distinct().OrderBy(s => s))
                {
                    Console.WriteLine(value);
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Property {propName} not found in VMD PID");
            }
        }
    }
}
