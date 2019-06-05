using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SimMetrics.Net.API;
using SimMetrics.Net.Metric;
using SimMetrics.Net.Utilities;
using VetMedData.NET.Util;

namespace VetMedData.NET.ProductMatching
{
    public abstract class SemanticallyWeightedNameMetricConfig :MetricConfig
    {
        /// <summary>
        /// String similarity metric to use when comparing names
        /// </summary>
        public AbstractStringMetric InnerMetric { get; set; }
        /// <summary>
        /// Tags to include and their weights
        /// </summary>
        public Dictionary<string,double> TagWeights { get; set; }
        /// <summary>
        /// Threshold of similarity of string pairs to be included
        /// </summary>
        public double InnerSimilarityThreshold { get; set; }
        /// <summary>
        /// Tokeniser for pairwise token similarity measurement.
        /// If left null then whole name strings will be compared.
        /// </summary>
        public ITokeniser Tokeniser { get; set; }
        /// <summary>
        /// Tokens present in reference set and their weights
        /// </summary>
        public Dictionary<string,List<Tuple<string,string>>> TagDictionary { get; set; }

        public string AnnotationConfigPath { get; set; }
        public string AnnotatedTextFilePath { get; set; }


        protected static Dictionary<string,double> InitialiseWeightDictionary(string annotationConfigPath)
        {
            return StandoffImport.GetEntitiesFromConfig(annotationConfigPath).ToDictionary(s => s,s=> 0d);
        }

        protected static Dictionary<string, List<Tuple<string, string>>> InitialiseTagDictionary(string annotatedTextFilePath)
        {
            return StandoffImport.ParseBrat(annotatedTextFilePath);

        }

    }

    public class DefaultSemanticallyWeightedNameMetricConfig : SemanticallyWeightedNameMetricConfig
    {
        public DefaultSemanticallyWeightedNameMetricConfig()
        {
            InnerMetric = new Levenstein();
            TagWeights = new Dictionary<string, double>{{"foo",1d}};
            Tokeniser = new TokeniserWhitespace();
            InnerSimilarityThreshold = 0.8d;
            AnnotatedTextFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"PIDAnnotations\vmdPIDantimicrobials.txt");
            AnnotatedTextFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"PIDAnnotations\annotation.conf");
            
            TagWeights = InitialiseWeightDictionary(AnnotationConfigPath);
            TagDictionary = InitialiseTagDictionary(AnnotatedTextFilePath);

        }

        public DefaultSemanticallyWeightedNameMetricConfig(Dictionary<string,double> weightDictionary,Dictionary<string,List<Tuple<string,string>>> tagDictionary)
        {
            InnerMetric = new Levenstein();
            TagWeights = new Dictionary<string, double> { { "foo", 1d } };
            Tokeniser = new TokeniserWhitespace();
            InnerSimilarityThreshold = 0.8d;

            TagWeights = weightDictionary;
            TagDictionary = tagDictionary;

        }

    }
}
