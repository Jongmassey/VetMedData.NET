using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VetMedData.NET.ProductMatching.Optimisation;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
namespace VetMedData.Tests
{
    [TestClass]
    public class TestGAOptimisation
    {
        Dictionary<string, string> configDictionary = new Dictionary<string, string>()
            {
                {"crossover", "UniformCrossover"}
                ,{"mixProbability", "0.5"}
                ,{"selection", "EliteSelection"}
                ,{"mutation","FlipBitMutation"}
                ,{"termination","FitnessStagnationTermination"}
                ,{"expectedStagnantGenerationsNumber","100"}
            };


        [TestMethod]
        public void TestGetCrossoverFromConfig()
        {

            var res = GeneticSharpHelpers.GetCrossoverByNameFromConfig(configDictionary);
            Assert.IsInstanceOfType(res, typeof(UniformCrossover));
            Assert.IsTrue(((UniformCrossover)res).MixProbability == 0.5f);

        }

        [TestMethod]
        public void TestGetSelectionFromConfig()
        {
            var res = GeneticSharpHelpers.GetSelectionByNameFromConfig(configDictionary);
            Assert.IsInstanceOfType(res, typeof(EliteSelection));

        }

        [TestMethod]
        public void TestGetTerminationFromConfig()
        {
            var res = GeneticSharpHelpers.GetTerminationByNameFromConfig(configDictionary);
            Assert.IsInstanceOfType(res, typeof(FitnessStagnationTermination));
            Assert.IsTrue(((FitnessStagnationTermination)res).ExpectedStagnantGenerationsNumber == 100);
        }

        [TestMethod]
        public void TestGetMutationFromConfig()
        {
            var res = GeneticSharpHelpers.GetMutationByNameFromConfig(configDictionary);
            Assert.IsInstanceOfType(res,typeof(FlipBitMutation));
        }
    }
}