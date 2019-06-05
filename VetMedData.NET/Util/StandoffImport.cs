using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VetMedData.NET.Util
{
    public static class StandoffImport
    {
        /// <summary>
        /// Look for "Type" entities from brat by 'T' prefix
        /// </summary>
        private const char EntityPrefix = 'T';

        /// <summary>
        /// Parses brat annotation.conf file and extracts the entities section
        /// </summary>
        /// <param name="pathToAnnotationConfig">path to annotation.conf file</param>
        /// <returns>Entities defined within file</returns>
        public static IEnumerable<string> GetEntitiesFromConfig(string pathToAnnotationConfig)
        {
            var outList = new List<string>();

            var confLines = File.ReadAllLines(pathToAnnotationConfig);
            var entitySection = false;
            foreach (var confLine in confLines)
            {
                var ln = confLine.Trim();

                var entitySectionStart =
                    ln.Equals("[entities]", StringComparison.InvariantCultureIgnoreCase);
                var otherSectionStart = !entitySectionStart && ln.StartsWith('[') && ln.EndsWith(']');

                entitySection = !otherSectionStart && (entitySection || entitySectionStart);

                if (!entitySectionStart && entitySection && !ln.StartsWith('#') && !string.IsNullOrWhiteSpace(ln))
                {
                    outList.Add(ln);
                }
            }

            return outList;
        }


        /// <summary>
        /// Parses Brat standoff <see href="http://brat.nlplab.org/standoff.html"/> format files.
        /// Expects pair of identically-named .txt and .ann files
        /// </summary>
        /// <param name="pathToTxtFile"></param>
        /// <returns>Dictionary of medicine names and their tagged tokens</returns>
        public static Dictionary<string, List<Tuple<string, string>>> ParseBrat(string pathToTxtFile)
        {
            var outDictionary = new Dictionary<string, List<Tuple<string, string>>>();
            var stringBounds = new Dictionary<string, Tuple<int, int>>();

            using (var fs = File.OpenText(pathToTxtFile))
            {
                var strPos = 0;
                while (!fs.EndOfStream)
                {
                    var ln = fs.ReadLine();
                    if (string.IsNullOrWhiteSpace(ln)) continue;
                    stringBounds.Add(ln, new Tuple<int, int>(strPos, strPos + ln.Length));
                    outDictionary.Add(ln, new List<Tuple<string, string>>());
                    strPos += ln.Length + 1;
                }
            }

            var pathToAnnFile = pathToTxtFile.Replace(".txt", ".ann");
            using (var fs = File.OpenText(pathToAnnFile))
            {
                while (!fs.EndOfStream)
                {
                    var ln = fs.ReadLine()?.Split('\t');
                    if (ln == null || ln.Length==0 || ln.All(string.IsNullOrWhiteSpace)) continue;
                    if (!ln[0].StartsWith(EntityPrefix)) continue;

                    var inner = ln[1].Split(' ');

                    var tag = inner[0];
                    var taggedTokens = ln[2].Split(' ');
                    var startPos = int.Parse(inner[1]);

                    //take final bound for discontinuous annotations
                    var endPos = int.Parse(inner.Length > 3 ? inner[3] : inner[2]);

                    var productName = stringBounds
                        .Single(s => startPos >= s.Value.Item1 && endPos <= s.Value.Item2)
                        .Key;

                    outDictionary[productName].AddRange(
                        taggedTokens
                            .Select(s => new Tuple<string, string>(tag, s))
                        );
                }
            }

            return outDictionary;
        }

        public static Dictionary<string, List<Tuple<string, string>>> ParseAll(string directoryPath)
        {
            var outDict = new Dictionary<string, List<Tuple<string, string>>>();

            foreach (var (key, value) in Directory.GetFiles(directoryPath)
                .SelectMany(ParseBrat)) outDict[key] = value;

            return outDict;
        }

    }
}
