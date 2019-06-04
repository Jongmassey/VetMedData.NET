using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using VetMedData.NET.ProductMatching;

namespace VetMedData.Tests
{
    [TestClass]
    public class TestSemanticallyWeightedNameMatching
    {
        [TestMethod]
        public void TestLoadDefaultConfig()
        {
            var swmc = new DefaultSemanticallyWeightedNameMetricConfig();
            Assert.IsNotNull(swmc, "Null config");
            Assert.IsNotNull(swmc.TagWeights, "Null weight dictionary");
            Assert.IsFalse(swmc.TagWeights.Count == 0, "Empty weight dictionary");
            Assert.IsNotNull(swmc.TagDictionary, "Null tag dictionary");
            Assert.IsFalse(swmc.TagDictionary.Count == 0, "Empty tag dictionary");
        }


        [TestMethod]
        public void TestIdenticalStrings()
        {
            var swm = new SemanticallyWeightedNameMetric(GetRunningConfig());
            var result = swm.GetSimilarity("Terramycin/LA 200 mg/ml Solution for Injection", "Terramycin LA 200 mg/ml Solution For Injection");
            Assert.IsTrue(result == 1d, $"similarity returned {result}, expected 1");
        }

        [TestMethod]
        public void TestHalfIdenticalStrings()
        {
            var swm = new SemanticallyWeightedNameMetric(GetRunningConfig());
            var result = swm.GetSimilarity("Antirobe Capsules 150 mg", "Antixxxx Capsxxxx");
            Assert.IsTrue(Math.Abs(result - 0.5d) < 0.05, $"similarity returned {result}, expected 0.5");

        }




        [TestMethod]
        public void TestInnerMetric()
        {
            var res = GetRunningConfig().InnerMetric.GetSimilarity("capsules", "irrelevant");
            Assert.IsTrue(Math.Abs(res - 0.1d) < 0.05,$"should be 0.1, got {res}");
        }

        private SemanticallyWeightedNameMetricConfig GetRunningConfig()
        {
            var td = TestStandoffImport.ParseAll();
            var tw = TestStandoffImport.GetAllEntities().ToDictionary(e => e, e => 1.0d);

            return new DefaultSemanticallyWeightedNameMetricConfig(tw, td);
            
        }
    }
}
