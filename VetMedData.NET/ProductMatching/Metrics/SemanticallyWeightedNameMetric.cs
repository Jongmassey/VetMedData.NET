using SimMetrics.Net.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VetMedData.NET.ProductMatching
{
    public class SemanticallyWeightedNameMetric : SimilarityMetric
    {
        private SemanticallyWeightedNameMetricConfig SemanticConfig => (SemanticallyWeightedNameMetricConfig) _config;
        public SemanticallyWeightedNameMetric(SemanticallyWeightedNameMetricConfig config) : base(config)
        {
        }

        public SemanticallyWeightedNameMetric() : base(new DefaultSemanticallyWeightedNameMetricConfig())
        {
        }

        public override double GetSimilarity(string firstWord, string secondWord)
        {
            var vec = GetVectorSimilarity(firstWord, secondWord);
            return vec.Select(v => v.Item1 * v.Item2).Sum() / vec.Sum(v => v.Item2);
        }

        public IEnumerable<Tuple<double, double>> GetVectorSimilarity(string firstWord, string secondWord)
        {
            var outList = new List<Tuple<double, double>>();

            var bTokens = SemanticConfig.Tokeniser.Tokenize(secondWord);

            var aTags = SemanticConfig.TagDictionary[firstWord];

            foreach (var bToken in bTokens)
            {
                var maxSim = 0d;
                var weight = 0d;
                foreach (var aToken in aTags)
                {
                    var sim = SemanticConfig.InnerMetric.GetSimilarity(aToken.Item2.ToLowerInvariant(), bToken.ToLowerInvariant());

                    if (!(sim > maxSim)) continue;
                    maxSim = sim;
                    weight = SemanticConfig.TagWeights[aToken.Item1];

                }
                outList.Add(new Tuple<double, double>(maxSim, weight));
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
