using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VetMedData.NET.Model;
using VetMedData.NET.ProductMatching;
using VetMedData.NET.Util;

namespace VetMedData.CLI
{
    internal class MatchRunner
    {
        internal static void MatchFile(string pathToInputFile)
        {
            var sb = new StringBuilder("\"Input Name\",\"Matched Name\",\"VM Number\",\"Similarity Score\"" + Environment.NewLine);
            var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                              PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                              PidFactoryOptions.PersistentPid).Result;
            var cfg = new DefaultProductMatchConfig();
            var pmr = new ProductMatchRunner(cfg);
            var sw = Stopwatch.StartNew();

            var inputStrings = new BlockingCollection<string>();

            using (var fs = File.OpenText(pathToInputFile))
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

        internal static void ExplainMatch(string inputString, string refVMNo)
        {
            var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                              PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                              PidFactoryOptions.PersistentPid).Result;

            var cfg = new DefaultProductMatchConfig();
            var pmr = new ProductMatchRunner(cfg);
            var name = inputString;

            var ap = new ActionedProduct
            {
                Product = new Product { Name = name },
            };

            var refprod = pid.AllProducts.Single(p => p.VMNo.Equals(refVMNo));
            var foo = ap.GetMatchingResult(refprod, cfg);
            Console.WriteLine(foo);
        }


        internal static void Explain(string productName, string commaSepSpeciesList)
        {
            var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                                 PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                                 PidFactoryOptions.PersistentPid).Result;

            var cfg = new DefaultProductMatchConfig();
            var pmr = new ProductMatchRunner(cfg);

            var species = commaSepSpeciesList.Split(',');
            var name = productName;

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
                Console.WriteLine($"Property {propName} not found in VMDPID");
            }
        }
    }
}
