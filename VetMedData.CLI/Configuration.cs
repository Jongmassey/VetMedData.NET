using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace VetMedData.CLI
{
    internal class Configuration
    {
        public Methods Method { get; set; }
        public Dictionary<string,string> ConfigDictionary { get; set; }

        internal enum Methods
        {
            MATCH,
            EXPLAIN,
            OPTIMISE
        }

        internal static Configuration Parse(string pathToYaml)
        {
            if (!File.Exists(pathToYaml)) { throw new FileNotFoundException("unable to find YAML config file"); }
            using (var sr = new StringReader(pathToYaml))
            {
                var yaml = new YamlStream();
                yaml.Load(sr);

                var mapping = (YamlMappingNode) yaml.Documents[0].RootNode;

                if (!mapping.Children.ContainsKey(new YamlScalarNode("method")))
                {
                    throw new KeyNotFoundException("missing method");
                }

                var method = ((YamlScalarNode)mapping.Children[new YamlScalarNode("method")]).Value;
                if (!(new[] {"", "", ""}).Contains(method))
                {
                    throw new KeyNotFoundException($"unknown method name{method}");
                }

                if (!mapping.Children.ContainsKey(new YamlScalarNode($"{method}Config")))
                {
                    throw new KeyNotFoundException("missing method configuration block");
                }

                var configNode = (YamlMappingNode)mapping.Children[new YamlScalarNode($"{method}Config")];
                

                if (method.Contains("match"))
                {
                    return new Tuple<string, Dictionary<string, string>>(method,
                        ParseMatchConfig(configNode));
                }

                return new Tuple<string, Dictionary<string, string>>(method,
                    ParseOptimiseConfig(configNode));

            }
        }

        private static Dictionary<string, string> ParseMatchConfig(YamlMappingNode configNode)
        {
            return configNode.Children.ToDictionary(c => c.Key.ToString(), c => c.Value.ToString());
        }

        private static Dictionary<string, string> ParseOptimiseConfig(YamlMappingNode configNode)
        {
            return configNode.Children.ToDictionary(c => c.Key.ToString(), c => c.Value.ToString());
        }


    }
}
