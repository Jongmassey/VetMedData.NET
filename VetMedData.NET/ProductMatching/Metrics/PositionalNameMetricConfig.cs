using SimMetrics.Net.API;
using SimMetrics.Net.Metric;
using SimMetrics.Net.Utilities;

namespace VetMedData.NET.ProductMatching
{
    /// <summary>
    /// Configures medicine similarity metric behaviour
    /// </summary>
    public abstract class PositionalNameMetricConfig: MetricConfig
    {
        /// <summary>
        /// String similarity metric to use when comparing names
        /// </summary>
        public AbstractStringMetric InnerMetric { get; set; }
        /// <summary>
        /// Name pre-cleaning configuration
        /// </summary>
        public NameCleaningConfig NameCleaningConfig { get; set; }
        /// <summary>
        /// Tokeniser for pairwise token similarity measurement.
        /// If left null then whole name strings will be compared.
        /// </summary>
        public ITokeniser Tokeniser { get; set; }
        /// <summary>
        /// Power to which the token index will be raised to form
        /// inverse weighting coefficient for A string
        /// </summary>
        public double APositionalWeightingCoefficientPower { get; set; }
        /// <summary>
        /// Power to which the token index will be raised to form
        /// inverse weighting coefficient for B string
        /// </summary>
        public double BPositionalWeightingCoefficientPower { get; set; }
        /// <summary>
        /// Ratio of A-token-index-weighting (0) to
        /// B-token-index-weighting (1) to be applied.
        /// </summary>
        public double ABCompoundPositionalWeightRatio { get; set; }
    }

    public class DefaultPositionalNameMetricConfig : PositionalNameMetricConfig
    {
        public DefaultPositionalNameMetricConfig()
        {
            InnerMetric = new Levenstein();
            NameCleaningConfig = new DefaultMatchNameCleaningConfig();
            Tokeniser = new TokeniserWhitespace();
            APositionalWeightingCoefficientPower = 1.5d;
            BPositionalWeightingCoefficientPower = 0d;
            ABCompoundPositionalWeightRatio = 0d;
            }
    }
}
