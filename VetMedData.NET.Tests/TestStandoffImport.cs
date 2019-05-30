using Microsoft.VisualStudio.TestTools.UnitTesting;
using VetMedData.NET.Util;

namespace VetMedData.Tests
{
    [TestClass]
    public class TestStandoffImport
    {
        [TestMethod, DeploymentItem(@"TestFiles\TestStandoffImport\", @"TestFiles\TestStandoffImport\")]
        public void TestImportBrat()
        {
            const string pathToTxt = @"TestFiles\TestStandoffImport\vmdPIDantimicrobials1.txt";
            //139
            var outDic = StandoffImport.ParseBrat(pathToTxt);
            Assert.IsFalse(outDic == null,"null Dictionary returned");
            Assert.IsFalse(outDic.Count==0, "empty Dictionary returned");
            Assert.IsTrue(outDic.Count==139,"fewer than expected items in Dictionary");

        }
    }
}
