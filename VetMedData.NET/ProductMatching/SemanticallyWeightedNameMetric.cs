using SimMetrics.Net.API;
using System;
using System.Collections.Generic;
using System.Linq;
using SimMetrics.Net.Metric;

namespace VetMedData.NET.ProductMatching
{
    public class SemanticallyWeightedNameMetric : AbstractStringMetric
    {
        private readonly SemanticallyWeightedNameMetricConfig _config;
        public SemanticallyWeightedNameMetric(SemanticallyWeightedNameMetricConfig conf = null)
        {
            _config = conf ?? new DefaultSemanticallyWeightedNameMetricConfig();
        }
        public override double GetSimilarity(string firstWord, string secondWord)
        {
            var vec = GetVectorSimilarity(firstWord, secondWord);
            return vec.Select(v => v.Item1 * v.Item2).Sum() / vec.Sum(v => v.Item2);
        }

        public IEnumerable<Tuple<double, double>> GetVectorSimilarity(string firstWord, string secondWord)
        {
            var outList = new List<Tuple<double, double>>();

            var bTokens = _config.Tokeniser.Tokenize(secondWord);

            var totalSim = 0d;
            var totalDivisor = 0d;

            var aTags = _config.TagDictionary[firstWord];

            foreach (var bToken in bTokens)
            {
                var maxSim = 0d;
                var weight = 0d;
                foreach (var aToken in aTags)
                {
                    var sim = _config.InnerMetric.GetSimilarity(aToken.Item2.ToLowerInvariant(), bToken.ToLowerInvariant());

                    if (!(sim > maxSim)) continue;
                    maxSim = sim;
                    weight = _config.TagWeights[aToken.Item1];

                }

                //if (maxSim > 0)
                //{
                    outList.Add(new Tuple<double, double>(maxSim, weight));
                //}
            }

            return outList;
        }


        public override string GetSimilarityExplained(string firstWord, string secondWord)
        {
            throw new NotImplementedException();
        }

        public override double GetSimilarityTimingEstimated(string firstWord, string secondWord)
        {
            throw new NotImplementedException();
        }

        public override double GetUnnormalisedSimilarity(string firstWord, string secondWord)
        {
            throw new NotImplementedException();
        }

        public override string LongDescriptionString { get; }
        public override string ShortDescriptionString { get; }
    }
}
