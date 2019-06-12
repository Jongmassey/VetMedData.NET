using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
                    if (ln == null || ln.Length == 0 || ln.All(string.IsNullOrWhiteSpace)) continue;
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

        /// <summary>
        /// Parse all *txt and *ann files in the given directory
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static Dictionary<string, List<Tuple<string, string>>> ParseAll(string directoryPath)
        {
            var outDict = new Dictionary<string, List<Tuple<string, string>>>();

            foreach (var (key, value) in Directory.GetFiles(directoryPath, "*.txt")
                .SelectMany(ParseBrat)) outDict[key] = value;

            return outDict;
        }

        /// <summary>
        /// Merge .txt and .ann files in alphabetical order, updating annotation IDs to be contiguous
        /// </summary>
        /// <param name="directoryPath"></param>
        public static void CombineFiles(string directoryPath)
        {
            var inputDir = new DirectoryInfo(directoryPath);
            var outputFilePath = Path.Combine(directoryPath, inputDir.Name);

            var charOffset = 0;
            var tagOffsets = new Dictionary<char, Tuple<int, int>>
            {
                {'T', new Tuple<int,int>(0,0)},
                {'E', new Tuple<int,int>(0,0)},
                {'R', new Tuple<int,int>(0,0)},
            };

            foreach (var textFile in inputDir.GetFiles("*.txt"))
            {

                //copy annotations to output, updating offsets
                using (var inputAnn = File.OpenText(textFile.FullName.Replace(".txt", ".ann")))
                using (var outputAnn = new StreamWriter(outputFilePath + ".ann", true))
                {
                    while (!inputAnn.EndOfStream)
                    {
                        var ln = inputAnn.ReadLine();
                        var tagType = ln[0];
                        var previousTagOffset = tagOffsets[tagType];
                        var outerSplit = ln.Split('\t');
                        var currentTagNumber = outerSplit[0].Remove(0, 1);
                        var newTagNumber = int.Parse(currentTagNumber) + previousTagOffset.Item1;

                        //update max tag number if greater
                        if (previousTagOffset.Item2 < newTagNumber)
                        {
                            previousTagOffset = new Tuple<int, int>(previousTagOffset.Item1, newTagNumber);
                            tagOffsets[tagType] = previousTagOffset;
                        }

                        outerSplit[0] = $"{tagType}{newTagNumber}";


                        var innerSplit = outerSplit[1].Split(' ');

                        //update character positions for entities
                        if (tagType == 'T')
                        {
                            for (int i = 1; i < innerSplit.Length; i++)
                            {
                                if (innerSplit[i].Contains(';'))
                                {
                                    var innerInnerSplit = innerSplit[i].Split(';');
                                    innerSplit[i] =
                                        $"{charOffset + int.Parse(innerInnerSplit[0])};{charOffset + int.Parse(innerInnerSplit[1])}";

                                }
                                else
                                {
                                    innerSplit[i] = $"{int.Parse(charOffset + innerSplit[i])}";
                                }
                            }
                        }

                        //update relationship/event references
                        if (tagType == 'R' || tagType == 'E')
                        {
                            var tOffset = tagOffsets['T'].Item1;
                            for (int i = (tagType == 'R' ? 1 : 0); i < innerSplit.Length; i++)
                            {
                                var innerInnerSplit = innerSplit[i].Split(':');
                                innerSplit[i] = $"{innerInnerSplit[0]}:T{tOffset + int.Parse(innerInnerSplit[1].Remove(0,1))}";
                            }
                        }

                        outerSplit[1] = string.Join(' ', innerSplit);

                        outputAnn.WriteLine(string.Join('\t',outerSplit));
                    }
                }


                //copy txt file to output, counting characters and updating char offset
                using (var inputText = textFile.OpenText())
                using (var outputText = new StreamWriter(outputFilePath + ".txt", true))
                {
                    while (!inputText.EndOfStream)
                    {
                        var ln = inputText.ReadLine();
                        charOffset += ln.Length + 1;
                        outputText.WriteLine(ln);
                    }
                }

                //update tag offsets with max new offsets
                foreach (var tag in tagOffsets.Keys.ToArray())
                {
                    var (_, newOffset) = tagOffsets[tag];
                    tagOffsets[tag] = new Tuple<int, int>(newOffset, newOffset);
                }
            }
        }
    }
}
