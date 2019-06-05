using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VetMedData.NET.Model;
using VetMedData.NET.ProductMatching;
using VetMedData.NET.Util;

namespace VetMedData.Tests
{
    [TestClass]
    public class TestNameMatching
    {
        [TestMethod]
        public void TestIdenticalStrings()
        {
            var pnm = new PositionalNameMetric();
            string[][] testsStrings = {
                new[] {"TestString", "TestString"},
                new[] {"Test String", "Test String"}
            };
            foreach (var testStringPair in testsStrings)
            {
                var sim = pnm.GetSimilarity(testStringPair[0], testStringPair[1]);
                Assert.IsTrue(Math.Abs(1 - sim) < 0.0001d
                              ,$"Strings: {testStringPair[0]},{testStringPair[1]} returned {sim} instead of 1");
            }
            
        }

        [TestMethod]
        public void TestSingleStringWithKnownSimilarity()
        {
            var pnm = new PositionalNameMetric();
            string[] testStringPair = {"teststring", "teststrang"};
            var sim = pnm.GetSimilarity(testStringPair[0], testStringPair[1]);
            Assert.IsTrue(0.9d == sim ,
                $"Strings: {testStringPair[0]},{testStringPair[1]} returned {sim} instead of 1");
        }

        [TestMethod]
        public void TestProductMatchRunner()
        {
            var cfg = new DefaultProductMatchConfig();
            var pmr = new ProductMatchRunner(cfg);
            var pid = VMDPIDFactory.GetVmdPid(PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct |
                                              PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct |
                                              PidFactoryOptions.PersistentPid).Result;
            var ap = new SoldProduct {
                TargetSpecies = new[] {"cattle"},
                Product = new Product {Name = "metacam"} ,
                ActionDate = DateTime.Now
            };
            var res = pmr.GetMatch(ap, pid.RealProducts);
            Assert.IsNotNull(res);
            Assert.IsTrue(res.ReferenceProduct.Name.StartsWith("metacam",StringComparison.InvariantCultureIgnoreCase));
            Assert.IsTrue(res.ReferenceProduct.TargetSpecies.Any(ts=>ts.Equals("cattle", StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
