using System;
using System.Collections.Generic;
using System.Text;
using SimMetrics.Net.API;

namespace VetMedData.NET.ProductMatching
{
    public abstract class SimilarityMetric :AbstractStringMetric
    {
        protected readonly MetricConfig _config;
        protected SimilarityMetric(MetricConfig config)
        {
            _config = config;
        }
    }
}
