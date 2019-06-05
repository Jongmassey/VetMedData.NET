namespace VetMedData.NET.ProductMatching
{
    public abstract class ProductMatchConfig
    {
        public SimilarityMetric Metric { get; set; }
       // public MetricConfig NameMetricConfig { get; set; }
        public IProductMatchDisambiguator Disambiguator { get; set; }
        public IProductMatchResultFilter DisambiguationCandidiateFilter { get; set; }
    }

    public class DefaultProductMatchConfig : ProductMatchConfig
    {
        public DefaultProductMatchConfig()
        {
            Metric = new PositionalNameMetric(new DefaultPositionalNameMetricConfig());
            //NameMetricConfig = new DefaultPositionalNameMetricConfig();
            Disambiguator = new HierarchicalFilterWithRandomFinalSelect(
                new OrderedFilterBasedDisambiguatorConfig
                {
                    Filters = new IProductMatchResultFilter[]
                    {
                        new CommonTargetSpeciesFilter() ,
                        new RandomSelectFilter()
                    }
                });
            DisambiguationCandidiateFilter = new MaximalSimilarityResultFilter();
        }
    }

}
