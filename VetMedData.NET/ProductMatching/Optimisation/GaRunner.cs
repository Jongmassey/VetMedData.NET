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
using System.Reflection;
using System;
//using Microsoft.Extensions.DependencyModel;


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

//TODO:ctor params
         private static ICrossover GetCrossoverByNameFromConfig(IDictionary<string, string> configDictionary)
         {
             var xovers = Assembly.Load("GeneticSharp.Domain.Crossovers");
             ConstructorInfo[] ctors;
             try {
             ctors= xovers.GetExportedTypes().Single(t=>t.Name.Equals(configDictionary["crossover"])).GetType().GetConstructors();
             }
             catch (Exception e){
                 throw new Exception($"Invalid crossover {configDictionary["crossover"]}");
             }
             
            ConstructorInfo defaultctor = null;
            if(ctors.Any(c=>!c.GetParameters().Any())){
                defaultctor = ctors.Single(c=>!c.GetParameters().Any());
            }           

            foreach(var ctor in ctors.Where(c=>c.GetParameters().Any()))
            {
                var paramnames = ctor.GetParameters().Select(p=>p.Name);
                if(paramnames.Except(configDictionary.Keys).Count()==0)
                {
                    var parameters = new List<object>();
                    foreach (var param in ctor.GetParameters())
                    {
                        parameters.Add(configDictionary[param.Name]);
                    }

                    return  (ICrossover)  ctor.Invoke(new object[]{});
                }
                
            }
            
            if(defaultctor!=null)
            {
                return (ICrossover) defaultctor.Invoke(new object[]{});
            }
            throw new Exception($"Inadequate constructor parameters and no default ctor for {configDictionary["crossover"]}");
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
