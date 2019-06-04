using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VetMedData.NET.Util;

namespace VetMedData.Tests
{
    [TestClass]
    public class TestStandoffImport
    {

        private const string PathToTxt = @"TestFiles\TestStandoffImport\vmdPIDantimicrobials1.txt";
        private const string PathToConf = @"TestFiles\TestStandoffImport\annotation.conf";
        private const int ExpectedRowsInTxt = 139;
        private const int ExpectedEntitesInConf = 9;
        private const int ExpectedTotalRows = 559;

        private Dictionary<string, List<Tuple<string, string>>> FullDictionary;
        private IEnumerable<string> FullEntityList;


        [TestMethod, DeploymentItem(@"TestFiles\TestStandoffImport\", @"TestFiles\TestStandoffImport\")]
        public void TestImportBrat()
        {
            var outDic = StandoffImport.ParseBrat(PathToTxt);
            Assert.IsFalse(outDic == null,"null Dictionary returned");
            Assert.IsFalse(outDic.Count==0, "empty Dictionary returned");
            Assert.IsTrue(outDic.Count== ExpectedRowsInTxt, "fewer than expected items in Dictionary");

        }

        [TestMethod, DeploymentItem(@"TestFiles\TestStandoffImport\", @"TestFiles\TestStandoffImport\")]
        public void TestGetEntitiesFromConfig()
        {
            var outList = GetAllEntities();
            Assert.IsFalse(outList == null, "null list returned");
            Assert.IsFalse(!outList.Any(),"empty list returned");
            Assert.IsTrue(outList.All(s=>!string.IsNullOrWhiteSpace(s)),"empty entities returned");
            Assert.IsTrue(outList.Count()==ExpectedEntitesInConf,$"unexpected number of entities returned:{outList.Count()}");
        }

        [TestMethod, DeploymentItem(@"TestFiles\TestStandoffImport\", @"TestFiles\TestStandoffImport\")]
        public void LoadAllAnnotations()
        {
            var outDictionary = ParseAll();
            Assert.IsTrue(outDictionary.Count == ExpectedTotalRows, $"Expected {ExpectedTotalRows}, got {outDictionary.Count}");
        }

        internal static IEnumerable<string> GetAllEntities() => StandoffImport.GetEntitiesFromConfig(PathToConf);

        internal static Dictionary<string, List<Tuple<string, string>>> ParseAll()
        {

            var outDict = new Dictionary<string, List<Tuple<string, string>>>();

            foreach (var (key, value) in Directory.GetFiles(@"TestFiles\TestStandoffImport\", "*.txt")
                .SelectMany(StandoffImport.ParseBrat)) outDict[key] = value;

            return outDict;
            //return Directory.GetFiles(@"TestFiles\TestStandoffImport\", "*.txt")
            //    .SelectMany(StandoffImport.ParseBrat)
            //    .Distinct()
            //    .ToDictionary(innerDict => innerDict.Key,
            //        innerDict => innerDict.Value);
        }
    }
}
