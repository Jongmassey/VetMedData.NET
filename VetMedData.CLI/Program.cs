using GeneticSharp.Domain.Chromosomes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimMetrics.Net.Metric;
using VetMedData.NET.Model;
using VetMedData.NET.ProductMatching;
using VetMedData.NET.ProductMatching.Optimisation;
using VetMedData.NET.Util;

namespace VetMedData.CLI
{
    internal class Program
    {
        //TODO: yml parsing
        //  What's needed in config?
        //  What's userful output for GA Optimisation - persistent failures?

        //TODO: GA tweaking
        // pop size
        // xover 
        // mutation?
        private static void Main(string[] args)
        {
            if (args.Length == 2 && args[0].Equals("--config"))
            {
                try
                {
                    var conf = Configuration.Parse(args[1]);

                    switch (conf.Method)
                    {
                        case Configuration.Methods.MATCH:
                            RunMatch(conf.ConfigDictionary);
                            break;
                        case Configuration.Methods.EXPLAIN:
                            ExplainMatch(conf.ConfigDictionary);
                            break;
                        case Configuration.Methods.OPTIMISE:
                            Optimise(conf.ConfigDictionary);
                            break;
                        default:
                            throw new Exception("unknown method");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }
                
            }

            if (args.Length ==1 && File.Exists(args[0]))
            {
                var sb = new StringBuilder("\"Input Name\",\"Matched Name\",\"VM Number\",\"Similarity Score\"" + Environment.NewLine);
                var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                                  PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                                  PidFactoryOptions.PersistentPid).Result;
                var cfg = new DefaultProductMatchConfig();
                var pmr = new ProductMatchRunner(cfg);
                var sw = Stopwatch.StartNew();

                var inputStrings = new BlockingCollection<string>();

                using (var fs = File.OpenText(args[0]))
                {
                    while (!fs.EndOfStream)
                    {

                        inputStrings.Add(fs.ReadLine()?.ToLowerInvariant().Trim());
                    }
                }
                sw.Restart();
                Parallel.ForEach(inputStrings.Where(s => !string.IsNullOrEmpty(s)), inputString =>
                  {
                      var ap = new SoldProduct
                      {
                          TargetSpecies = new[] { "cattle" },
                          Product = new Product { Name = inputString },
                          ActionDate = DateTime.Now
                      };

                      var res = pmr.GetMatch(ap, pid.RealProducts);
                      lock (sb)
                      {
                          sb.AppendJoin(',',
                              $"\"{res.InputProduct.Product.Name}\"",
                              $"\"{res.ReferenceProduct.Name}\"",
                              $"\"{res.ReferenceProduct.VMNo}\"",
                              res.ProductNameSimilarity.ToString(CultureInfo.InvariantCulture),
                              Environment.NewLine);
                      }
                  });
                Console.WriteLine(sb.ToString());
            }

            else

            if (args.Length > 0 && args[0].Equals("print", StringComparison.InvariantCultureIgnoreCase))
            {
                var propName = args[1];
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
                    Console.WriteLine($"Property {propName} not found in VMDPID");
                }
            }
            else
            if (args.Length > 1 && args[0].Equals("explainmatch"))
            {
                var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                                  PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                                  PidFactoryOptions.PersistentPid).Result;

                var cfg = new DefaultProductMatchConfig();
                var pmr = new ProductMatchRunner(cfg);
                var name = args[1];

                var ap = new ActionedProduct
                {
                    Product = new Product { Name = name },
                };

                var refprod = pid.AllProducts.Single(p => p.VMNo.Equals(args[2]));
                var foo = ap.GetMatchingResult(refprod, cfg);
                Console.WriteLine(foo);
            }
            else
            if (args.Length > 2 && args[0].Equals("explain"))
            {
                var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                                  PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                                  PidFactoryOptions.PersistentPid).Result;

                var cfg = new DefaultProductMatchConfig();
                var pmr = new ProductMatchRunner(cfg);

                var species = args[2].Split(',');
                var name = args[1];

                var ap = new ActionedProduct
                {
                    Product = new Product { Name = name },
                    TargetSpecies = species
                };

                var mr = pmr.GetMatchResults(ap, pid.RealProducts).ToArray();
                var dc = pmr.GetDisambiguationCandidates(mr).ToArray();
                var res = pmr.GetMatch(ap, pid.RealProducts);
                Console.WriteLine("Matched product:");
                Console.WriteLine(string.Join('\t', res.ReferenceProduct.Name, res.ReferenceProduct.VMNo, res.ProductNameSimilarity.ToString(CultureInfo.InvariantCulture)));
                Console.WriteLine();
                Console.WriteLine("All products:");
                foreach (var matchResult in mr)
                {
                    Console.WriteLine(string.Join('\t', matchResult.ReferenceProduct.Name, matchResult.ReferenceProduct.VMNo, matchResult.ProductNameSimilarity.ToString(CultureInfo.InvariantCulture)));
                }
                Console.WriteLine();
                Console.WriteLine("Disambiguation Candidates:");
                foreach (var productSimilarityResult in dc)
                {
                    Console.WriteLine(string.Join('\t', productSimilarityResult.ReferenceProduct.Name, productSimilarityResult.ReferenceProduct.VMNo, productSimilarityResult.ProductNameSimilarity.ToString(CultureInfo.InvariantCulture)));
                }

                var disambiguationConfig = ((HierarchicalFilterWithRandomFinalSelect)cfg.Disambiguator)._cfg;

                foreach (var disambiguationFilter in disambiguationConfig.Filters)
                {
                    Console.WriteLine($"Filter: {disambiguationFilter.GetType().Name}");
                    foreach (var filterResult in disambiguationFilter.FilterResults(dc))
                    {
                        Console.WriteLine(string.Join('\t', filterResult.ReferenceProduct.Name, filterResult.ReferenceProduct.VMNo, filterResult.ProductNameSimilarity.ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }

            else if (args.Length > 1 && args[0].Equals("optimise", StringComparison.InvariantCultureIgnoreCase))
            {
                TruthFactory.SetPath(args[1]);
                var pid = VMDPIDFactory.GetVmdPid(
                    PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                    PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                    PidFactoryOptions.PersistentPid
                    ).Result;

                var ga = GaRunner.GetGeneticAlgorithm();
                Console.WriteLine("Generation, ABWeightRatio, AWeight, BWeight, Threshold, SuccessRate");

                var latestFitness = 0.0;

                ga.GenerationRan += (sender, e) =>
                {
                    var bestChromosome = ga.BestChromosome as FloatingPointChromosome;
                    var bestFitness = bestChromosome.Fitness.Value;

                    if (bestFitness != latestFitness)
                    {
                        latestFitness = bestFitness;
                        var phenotype = bestChromosome.ToFloatingPoints();

                        Console.WriteLine(
                            "{0,2},{1},{2},{3},{4},{5}",
                            ga.GenerationsNumber,
                            phenotype[0],
                            phenotype[1],
                            phenotype[2],
                            phenotype[3],
                            bestFitness
                        );
                    }
                };
                ga.Start();
            }

            else
            {
                Console.WriteLine("Requires path to file to process as first argument.");
                Console.ReadLine();
            }
        }

        private static void RunMatch(Dictionary<string,string> configDictionary)
        {
            try
            {
                
            

            var sb = new StringBuilder("\"Input Name\",\"Matched Name\",\"VM Number\",\"Similarity Score\"" + Environment.NewLine);
            var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                              PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                              PidFactoryOptions.PersistentPid).Result;
                var cfg = new DefaultProductMatchConfig
                {
                    NameMetricConfig =
                    {
                        ABCompoundPositionalWeightRatio = double.Parse(configDictionary["abWeightRatio"]),
                        APositionalWeightingCoefficientPower = double.Parse(configDictionary["aWeightingParameter"]),
                        BPositionalWeightingCoefficientPower = double.Parse(configDictionary["bWeightingParameter"]),
                        InnerMetric = new Levenstein()
                    }
                };
                
                var pmr = new ProductMatchRunner(cfg);
            

            var inputStrings = new BlockingCollection<string>();

                using (var fs = File.OpenText(configDictionary["input"]))
                {
                    while (!fs.EndOfStream)
                    {
                        inputStrings.Add(fs.ReadLine()?.ToLowerInvariant().Trim());
                    }
                }
            
            Parallel.ForEach(inputStrings.Where(s => !string.IsNullOrEmpty(s)), inputString =>
            {
                var ap = new SoldProduct
                {
                    TargetSpecies = configDictionary["targetSpecies"].Split(',') ,
                    Product = new Product { Name = inputString },
                    ActionDate = DateTime.Now
                };

                var res = pmr.GetMatch(ap, pid.RealProducts);
                lock (sb)
                {
                    sb.AppendJoin(',',
                        $"\"{res.InputProduct.Product.Name}\"",
                        $"\"{res.ReferenceProduct.Name}\"",
                        $"\"{res.ReferenceProduct.VMNo}\"",
                        res.ProductNameSimilarity.ToString(CultureInfo.InvariantCulture),
                        Environment.NewLine);
                }
            });
            Console.WriteLine(sb.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ExplainMatch(Dictionary<string, string> configDictionary)
        {
        }

        private static void Optimise(Dictionary<string, string> configDictionary)
        {
            TruthFactory.SetPath(configDictionary["input"]);
            var pid = VMDPIDFactory.GetVmdPid(
                PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                PidFactoryOptions.PersistentPid
            ).Result;

            var ga = GaRunner.GetGeneticAlgorithm(configDictionary);
            Console.WriteLine("Generation, ABWeightRatio, AWeight, BWeight, Threshold, SuccessRate");

            var latestFitness = 0.0;

            ga.GenerationRan += (sender, e) =>
            {
                var bestChromosome = ga.BestChromosome as FloatingPointChromosome;
                var bestFitness = bestChromosome.Fitness.Value;

                if (bestFitness != latestFitness)
                {
                    latestFitness = bestFitness;
                    var phenotype = bestChromosome.ToFloatingPoints();

                    Console.WriteLine(
                        "{0,2},{1},{2},{3},{4},{5}",
                        ga.GenerationsNumber,
                        phenotype[0],
                        phenotype[1],
                        phenotype[2],
                        phenotype[3],
                        bestFitness
                    );
                }
            };
            ga.Start();
        }
    }
}
