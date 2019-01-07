using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace VetMedData.NET.ProductMatching.Optimisation
{
    public static class GaRunner
    {
        public static double[] Run()
        {
            var ga = GetGeneticAlgorithm();
            ga.Start();

            return ((FloatingPointChromosome)ga.BestChromosome).ToFloatingPoints()
                .Union(new[] { ga.BestChromosome.Fitness.Value }).ToArray();
        }


        public static ObservableCollection<double[]> RunWithGenerationalResults()
        {
            var obc = new ObservableCollection<double[]>();
            var ga = GetGeneticAlgorithm();
            var latestFitness = 0.0;

            ga.GenerationRan += (sender, e) =>
            {
                var bestChromosome = ga.BestChromosome as FloatingPointChromosome;
                var bestFitness = bestChromosome.Fitness.Value;

                if (bestFitness != latestFitness)
                {
                    latestFitness = bestFitness;
                    var phenotype = bestChromosome.ToFloatingPoints();
                    obc.Add(new[]
                    {
                            ga.GenerationsNumber,
                            phenotype[0],
                            phenotype[1],
                            phenotype[2],
                            phenotype[3],
                            bestFitness
                    });
                }
            };
            return obc;
        }
              
        public static GeneticAlgorithm GetGeneticAlgorithm(IDictionary<string, string> configDictionary)
        {
            var chromosome = new ConfigurationChromosome();
            var fitness = new CorrectPercentageFitness();
             IPopulation population = new Population(int.Parse(configDictionary["populationMinSize"]),
                 int.Parse(configDictionary["populationMaxSize"]), chromosome);
            
            var ga = new GeneticAlgorithm(
                    population,
                    fitness,
                    GeneticSharpHelpers.GetSelectionByNameFromConfig(configDictionary),
                    GeneticSharpHelpers.GetCrossoverByNameFromConfig(configDictionary),
                    GeneticSharpHelpers.GetMutationByNameFromConfig(configDictionary))
            { Termination = GeneticSharpHelpers.GetTerminationByNameFromConfig(configDictionary) };

            if (configDictionary.ContainsKey("crossoverProbability")){
                var xrfp = float.Parse(configDictionary["crossoverProbability"]);
                ga.CrossoverProbability  = xrfp;
            }

            if (configDictionary.ContainsKey("mutationProbability")){
                var mp = float.Parse(configDictionary["mutationProbability"]);
                ga.MutationProbability = mp;
            }

            return ga;
        }

        public static GeneticAlgorithm GetGeneticAlgorithm()
        {
            
            var configDictionary = new Dictionary<string, string>()
            {
                {"crossover", "UniformCrossover"}
                ,{"mixProbability", "0.5"}
                ,{"selection", "EliteSelection"}
                ,{"mutation","FlipBitMutation"}
                ,{"termination","FitnessStagnationTermination"}
                ,{"expectedStagnantGenerationsNumber","100"}
                ,{"populationMinSize","50"}
                ,{"populationMaxSize","100"}
            };

            return GetGeneticAlgorithm(configDictionary);
        }
    }
}
