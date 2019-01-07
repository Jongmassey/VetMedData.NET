using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VetMedData.NET.ProductMatching.Optimisation;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
namespace VetMedData.Tests
{
    [TestClass]
    public class TestGAOptimisation
    {
        [TestMethod]
        public void TestGetCrossoverFromConfig()
        {
            var configDictionary = new Dictionary<string,string>()
            {
                {"crossover", "UniformCrossover"}
                ,{"mixProbability", "0.5"}
            };
            
            var res = GeneticSharpHelpers.GetCrossoverByNameFromConfig(configDictionary);
            Assert.IsInstanceOfType(res,typeof(UniformCrossover));
            Assert.IsTrue(((UniformCrossover)res).MixProbability == 0.5f);

        }
    }
}