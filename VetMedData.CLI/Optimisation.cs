using System;
using GeneticSharp.Domain.Chromosomes;
using VetMedData.NET.ProductMatching.Optimisation;
using VetMedData.NET.Util;

namespace VetMedData.CLI
{
    internal class Optimisation
    {
        internal static void GALearnWeights(string pathToTruthFile)
        {
            TruthFactory.SetPath(pathToTruthFile);
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
    }
}
