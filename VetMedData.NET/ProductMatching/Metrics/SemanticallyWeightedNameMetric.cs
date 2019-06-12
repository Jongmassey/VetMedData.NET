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
            var vec = GetVectorSimilarity(secondWord, firstWord);
            return vec.Select(v => v.Item1 * v.Item2).Sum() / vec.Sum(v => v.Item2);
        }

        public IEnumerable<Tuple<double, double>> GetVectorSimilarity(string referenceWord, string testWord)
        {
            var outList = new List<Tuple<double, double>>();

            var inputTokens = SemanticConfig.Tokeniser.Tokenize(testWord);

            var referenceTags = SemanticConfig.TagDictionary.SingleOrDefault(k =>
                k.Key.Equals(referenceWord, StringComparison.InvariantCultureIgnoreCase)).Value; //SemanticConfig.TagDictionary[referenceWord];

            foreach (var inputToken in inputTokens)
            {
                //TODO: max inner similarity thresholding?
                var maxSim = 0d;
                var weight = 0d;
                if (referenceTags != null)
                    foreach (var (tag, referenceToken) in referenceTags)
                    {
                        var sim = SemanticConfig.InnerMetric.GetSimilarity(referenceToken.ToLowerInvariant(),
                            inputToken.ToLowerInvariant());

                        if (!(sim > maxSim)) continue;
                        maxSim = sim;
                        weight = SemanticConfig.TagWeights[tag];
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
