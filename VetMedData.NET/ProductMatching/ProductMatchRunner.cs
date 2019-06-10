using System.Collections.Generic;
using System.Linq;
using VetMedData.NET.Model;

namespace VetMedData.NET.ProductMatching
{
    public class ProductMatchRunner
    {
        private readonly ProductMatchConfig _cfg;

        public ProductMatchRunner(ProductMatchConfig cfg)
        {
            _cfg = cfg;
        }

        public IEnumerable<ProductSimilarityResult> GetMatchResults(ActionedProduct p, IEnumerable<ReferenceProduct> referenceProductSet)
        {
            return referenceProductSet.Select(rp => p.GetMatchingResult(rp, _cfg));
        }

        public IEnumerable<ProductSimilarityResult> GetDisambiguationCandidates(IEnumerable<ProductSimilarityResult> matchResults)
        {
            return _cfg.DisambiguationCandidateFilter.FilterResults(matchResults);
        }

        public ProductSimilarityResult GetMatch(ActionedProduct p, IEnumerable<ReferenceProduct> referenceProductSet)
        {
            return _cfg.Disambiguator.DisambiguateMatchResults(GetDisambiguationCandidates(GetMatchResults(p, referenceProductSet)));
        }
    }
}
