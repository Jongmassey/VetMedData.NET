using System;

namespace VetMedData.NET.ProductMatching
{
    public abstract class ProductMatchConfig
    {
        public SimilarityMetric Metric { get; set; }
        public IProductMatchDisambiguator Disambiguator { get; set; }
        public IProductMatchResultFilter DisambiguationCandidateFilter { get; set; }
    }

    public class DefaultProductMatchConfig : ProductMatchConfig
    {
        public DefaultProductMatchConfig(MetricConfig metricConfig = null)
        {
            Metric =
                (metricConfig ?? new DefaultPositionalNameMetricConfig()) is SemanticallyWeightedNameMetricConfig ?
               (SimilarityMetric)new SemanticallyWeightedNameMetric((SemanticallyWeightedNameMetricConfig)metricConfig) :
                 new PositionalNameMetric((DefaultPositionalNameMetricConfig)metricConfig ?? new DefaultPositionalNameMetricConfig());
            Disambiguator = new HierarchicalFilterWithRandomFinalSelect(
                new OrderedFilterBasedDisambiguatorConfig
                {
                    Filters = new IProductMatchResultFilter[]
                    {
                        new CommonTargetSpeciesFilter() ,
                        new RandomSelectFilter()
                    }
                });
            DisambiguationCandidateFilter = new MaximalSimilarityResultFilter();
        }
    }

}
