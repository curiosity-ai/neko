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
            return Parse<NekoConfig>(configPath) ?? new NekoConfig();
        }

        public static T Parse<T>(string configPath) where T : new()
        {
            if (!File.Exists(configPath))
            {
                return new T(); // Return default if no config file
            }

            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<T>(yaml);
        }
    }
}
