using System.Linq;
using System.Text.RegularExpressions;
using VetMedData.NET.Model;

namespace VetMedData.NET.ProductMatching
{
    /// <summary>
    /// Extension methods for Product class
    /// </summary>
    public static class ProductExtensions
    {
        private const string BracketedTermPattern = @"[\[\(] *\w* *[\]\)]";
        private const string LeadingMultiplierPattern = @"^[0-9]+ *[xX] *(?=\w*)";

        /// <summary>
        /// Extension method to provide "cleaned" version of name according
        /// to rules defined in configuration class
        /// </summary>
        /// <param name="product">Product from which cleaned name will be extracted</param>
        /// <param name="cfg">Configuration for cleaning rules.
        /// Defaults to DefaultMatchNameCleaningConfig</param>
        /// <returns>Name field from product, cleaned according to configured rules</returns>
        public static string GetCleanedName(this Product product, NameCleaningConfig cfg = null)
        {
            cfg = cfg ?? new DefaultMatchNameCleaningConfig();
            var outstr = product.Name;

            outstr = cfg.RemoveBracketedTerms ?
                Regex.Replace(outstr, BracketedTermPattern, "",
                    RegexOptions.Compiled) :
                outstr;

            outstr = cfg.RemoveLeadingMultiplierTerms ? 
                Regex.Replace(outstr,LeadingMultiplierPattern,"",
                    RegexOptions.Compiled) : 
                outstr;

            outstr = cfg.PatternsToRemove != null && cfg.PatternsToRemove.Length > 0 ?
                cfg.PatternsToRemove.Aggregate(outstr, (current, pat) =>
                    Regex.Replace(current, pat, "",
                        RegexOptions.Compiled)) :
                outstr;

            outstr = cfg.CharsToRemove != null && cfg.CharsToRemove.Length > 0 ?
                cfg.CharsToRemove.Aggregate(outstr, (current, rmchar) =>
                     current.Replace(rmchar.ToString(), "")) :
                outstr;

            outstr = cfg.TermsToRemove != null && cfg.TermsToRemove.Length > 0 ?
                cfg.TermsToRemove.Aggregate(outstr, (current, rmstr) =>
                     Regex.Replace(current, $"\\b{rmstr}\\b", "",
                     RegexOptions.IgnoreCase & RegexOptions.Compiled)) :
                outstr;

            return outstr;
        }

        public static ProductSimilarityResult GetMatchingResult(this ActionedProduct product, ReferenceProduct referenceProduct, ProductMatchConfig cfg) => new ProductSimilarityResult
        {
            InputProduct = product,
            ReferenceProduct = referenceProduct,
            ProductNameSimilarity =
                    //new PositionalNameMetric(cfg.NameMetricConfig)
                    cfg.Metric
                        .GetSimilarity(product.Product.Name, referenceProduct.Name)
        };
    }
}
