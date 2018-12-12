using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VetMedData.NET.Model;
using VetMedData.NET.Util;

namespace VetMedData.NET.ProductMatching.Optimisation
{
    internal class CorrectPercentageFitness : IFitness
    {
        public double Evaluate(IChromosome chromosome)
        {
            var correctCount = 0;
            var pid = VMDPIDFactory.GetVmdPid(
                PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                PidFactoryOptions.PersistentPid
            ).Result;

            var cfg = (FloatingPointChromosome)chromosome;
            var threshold = cfg.ToFloatingPoints()[3];
            var pmr = new ProductMatchRunner(cfg.GetMatchConfig());
            var toMatch = new ConcurrentDictionary<string, string[]>(TruthFactory.GetTruth());
            Parallel.ForEach(toMatch, tm =>
            {
                var ap = new ActionedProduct { Product = new Product { Name = tm.Key }, TargetSpecies = TargetSpecies.Find("cattle").ToArray() };
                ReferenceProduct[] rp;
                lock (pid)
                {
                    rp = pid.RealProducts.ToArray();
                }
                var res = pmr.GetMatch(ap, rp);
                if ((tm.Value.Contains(res.ReferenceProduct.VMNo) && res.ProductNameSimilarity > threshold)
                    || (tm.Value.All(string.IsNullOrEmpty)&& res.ProductNameSimilarity < threshold )
                )
                {
                    Interlocked.Increment(ref correctCount);
                }
            });

            return (double)correctCount / toMatch.Count;
        }
    }
}
