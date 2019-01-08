using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("VetMedData.Tests")]
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

        internal static IMutation GetMutationByNameFromConfig(IDictionary<string, string> configDictionary)
        {
            return (IMutation)GetInstanceFromConfig(configDictionary, "Mutation");
        }

        // internal static IPopulation GetPopulationByNameFromConfig(IDictionary<string, string> configDictionary)
        // {
        //     return (IPopulation)GetInstanceFromConfig(configDictionary, "Population");
        // }

        internal static ISelection GetSelectionByNameFromConfig(IDictionary<string, string> configDictionary)
        {
            return (ISelection)GetInstanceFromConfig(configDictionary, "Selection");
        }

        internal static ITermination GetTerminationByNameFromConfig(IDictionary<string, string> configDictionary)
        {
            return (ITermination)GetInstanceFromConfig(configDictionary, "Termination");
        }

        private static object GetInstanceFromConfig(IDictionary<string, string> configDictionary, string objectType, string parentAssemblyName = "GeneticSharp.Domain")
        {
            //Load "Parent" Geneticsharp assembly
            var asm = typeof(IGeneticAlgorithm).Assembly;
            
            //Get members of sub-namespace of interest
            var classes = asm.GetExportedTypes().Where(t => t.Namespace.Equals($"{parentAssemblyName}.{objectType}s"));
            
            //get the configured class's constructors
            ConstructorInfo[] ctors;
            try
            {
                string objectTypeName = configDictionary[objectType.ToLowerInvariant()];

                try
                {
                    var matchedClass = classes.Single(t => t.Name.Equals(objectTypeName));
                    ctors = matchedClass.GetConstructors();
                }
                catch (Exception)
                {
                    throw new Exception($"Invalid {objectType} {objectTypeName}");
                }
            }
            catch (Exception)
            {
                throw new Exception($"Type config for {objectType} not found");
            }

            //try to match each constructor by its parameters to configured parameters
            foreach (var ctor in ctors.Where(c => c.GetParameters().Any()))
            {
                var ctorParams = ctor.GetParameters();
                if (ctorParams.Select(p => p.Name).Except(configDictionary.Keys).Count() == 0)
                {
                    var parameters = new List<object>();
                    foreach (var param in ctor.GetParameters())
                    {
                        try
                        {
                            dynamic pv;
                            Type t = param.ParameterType;

                            //if ctor parameter type has parse from string method, use it.
                            var parseMethod = t.GetMethod("Parse", new[] { typeof(String) });
                            if (parseMethod != null)
                            {
                                pv = parseMethod.Invoke(null, new object[] { configDictionary[param.Name] });
                            }
                            else
                            {
                                pv = configDictionary[param.Name];
                            }
                            parameters.Add(pv);
                        }
                        catch (Exception e)
                        {
                            throw new AggregateException($"Unable to parse parameter {param.Name} value {configDictionary[param.Name]} to required type", new[] { e });
                        }
                    }

                    return ctor.Invoke(parameters.ToArray());
                }
            }
            
            //try default constructor
            ConstructorInfo defaultctor = ctors.DefaultIfEmpty(null).SingleOrDefault(c => !c.GetParameters().Any());
            if (defaultctor != null)
            {
                return defaultctor.Invoke(new object[] { });
            }

            throw new Exception($"Inadequate constructor parameters and no default ctor for {configDictionary[objectType]}");
        }
    }
}