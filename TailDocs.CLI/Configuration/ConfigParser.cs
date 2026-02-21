using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TailDocs.CLI.Configuration
{
    public static class ConfigParser
    {
        public static TailDocsConfig Parse(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return new TailDocsConfig(); // Return default if no config file
            }

            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<TailDocsConfig>(yaml);
        }
    }
}
