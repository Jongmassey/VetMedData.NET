﻿using GeneticSharp.Domain;
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

            ICrossover crossover;
            switch (configDictionary["crossover"])
            {
                case "uniformCrossover":
                    var mixProbability = float.Parse(configDictionary["mixProbability"]);
                    crossover = new UniformCrossover(mixProbability);
                    break;
                default:
                    crossover = new UniformCrossover(0.5f);
                    break;
            }
            IPopulation population = new Population(int.Parse(configDictionary["populationMinSize"]),
                int.Parse(configDictionary["populationMaxSize"]), chromosome);

            var fitness = new CorrectPercentageFitness();

            ISelection selection;
            switch (configDictionary["selection"])
            {
                case "EliteSelection":
                    selection = new EliteSelection();
                    break;
                default:
                    selection = new EliteSelection();
                    break;
            }

            IMutation mutation;
            switch (configDictionary["mutation"])
            {
                case "FlipBitMutation":
                    mutation = new FlipBitMutation();
                    break;
                default:
                    mutation = new FlipBitMutation();
                    break;
            }

            ITermination termination;
            switch (configDictionary["termination"])
            {
                case "FitnessStagnationTermination":
                    var expectedStagnantGenerationsNumber =
                        int.Parse(configDictionary["expectedStagnantGenerationsNumber"]);
                    termination = new FitnessStagnationTermination(expectedStagnantGenerationsNumber);
                    break;
                default:
                    termination = new FitnessStagnationTermination(100);
                    break;
            }
            var ga = new GeneticAlgorithm(
                    population,
                    fitness,
                    selection,
                    crossover,
                    mutation)
            { Termination = termination };
            return ga;
        }

        public static GeneticAlgorithm GetGeneticAlgorithm()
        {
            var chromosome = new ConfigurationChromosome();
            var population = new Population(50, 100, chromosome);
            var fitness = new CorrectPercentageFitness();
            var selection = new EliteSelection();
            var crossover = new UniformCrossover(0.5f);
            var mutation = new FlipBitMutation();
            var termination = new FitnessStagnationTermination(100);

            var ga = new GeneticAlgorithm(
                    population,
                    fitness,
                    selection,
                    crossover,
                    mutation)
            { Termination = termination };
            return ga;
        }
    }
}
