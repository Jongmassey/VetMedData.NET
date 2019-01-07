using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
namespace VetMedData.NET.ProductMatching.Optimisation
{

    internal class GeneticSharpHelpers
    {
        internal static ICrossover GetCrossoverByNameFromConfig(IDictionary<string, string> configDictionary)
        {
            return (ICrossover)GetInstanceFromConfig(configDictionary, "Crossover");
        }

        internal static IChromosome GetChromosomeByNameFromConfig(IDictionary<string, string> configDictionary)
        {
            return (IChromosome)GetInstanceFromConfig(configDictionary, "Chromosome");
        }

        internal static IMutation GetMutationByNameFromConfig(IDictionary<string, string> configDictionary){
            return (IMutation)GetInstanceFromConfig(configDictionary,"Mutation");
        }

        internal static IPopulation GetPopulationByNameFromConfig(IDictionary<string, string> configDictionary){
            return (IPopulation)GetInstanceFromConfig(configDictionary,"Population");
        }

        internal static ISelection GetSelectionByNameFromConfig(IDictionary<string, string> configDictionary){
            return (ISelection)GetInstanceFromConfig(configDictionary,"Selection");
        }

        internal static ITermination GetTerminationByNameFromConfig(IDictionary<string, string> configDictionary){
            return (ITermination)GetInstanceFromConfig(configDictionary,"Termination");
        }

        private static object GetInstanceFromConfig(IDictionary<string, string> configDictionary, string objectType, string parentAssemblyName = "GeneticSharp.Domain")
        {
            var asm = Assembly.Load($"{parentAssemblyName}.{objectType}s");
            ConstructorInfo[] ctors;
            try
            {
                string objectTypeName = configDictionary[objectType.ToLowerInvariant()];

                try
                {
                    ctors = asm.GetExportedTypes().Single(t => t.Name.Equals(objectTypeName)).GetType().GetConstructors();
                }
                catch (Exception)
                {
                    throw new Exception($"Invalid {objectType} {objectTypeName}");
                }
            }
            catch (Exception)
            {
                throw new Exception($"Type config for  {objectType} not found");
            }

            ConstructorInfo defaultctor = ctors.DefaultIfEmpty(null).SingleOrDefault(c => !c.GetParameters().Any());

            foreach (var ctor in ctors.Where(c => c.GetParameters().Any()))
            {
                var paramnames = ctor.GetParameters().Select(p => p.Name);
                if (paramnames.Except(configDictionary.Keys).Count() == 0)
                {
                    var parameters = new List<object>();
                    foreach (var param in ctor.GetParameters())
                    {
                        parameters.Add(configDictionary[param.Name]);
                    }

                    return ctor.Invoke(parameters.ToArray());
                }
            }

            if (defaultctor != null)
            {
                return defaultctor.Invoke(new object[] { });
            }
            throw new Exception($"Inadequate constructor parameters and no default ctor for {configDictionary[objectType]}");
        }
    }
}