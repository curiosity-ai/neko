using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Neko.Configuration
{
    public static class ConfigParser
    {
        public static NekoConfig Parse(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return new NekoConfig(); // Return default if no config file
            }

            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<NekoConfig>(yaml);
        }
    }
}
