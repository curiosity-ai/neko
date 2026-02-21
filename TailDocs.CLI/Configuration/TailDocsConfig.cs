using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace TailDocs.CLI.Configuration
{
    public class TailDocsConfig
    {
        [YamlMember(Alias = "input")]
        public string Input { get; set; } = ".";

        [YamlMember(Alias = "output")]
        public string Output { get; set; } = ".taildocs";

        [YamlMember(Alias = "url")]
        public string Url { get; set; } = "localhost";

        [YamlMember(Alias = "branding")]
        public BrandingConfig Branding { get; set; } = new BrandingConfig();

        [YamlMember(Alias = "links")]
        public List<LinkConfig> Links { get; set; } = new List<LinkConfig>();

        [YamlMember(Alias = "meta")]
        public MetaConfig Meta { get; set; } = new MetaConfig();

        [YamlMember(Alias = "theme")]
        public ThemeConfig Theme { get; set; } = new ThemeConfig();
    }

    public class BrandingConfig
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; } = "TailDocs";

        [YamlMember(Alias = "label")]
        public string Label { get; set; }

        [YamlMember(Alias = "logo")]
        public string Logo { get; set; }

        [YamlMember(Alias = "logoDark")]
        public string LogoDark { get; set; }
    }

    public class LinkConfig
    {
        [YamlMember(Alias = "text")]
        public string Text { get; set; }

        [YamlMember(Alias = "link")]
        public string Link { get; set; }

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "target")]
        public string Target { get; set; }

        [YamlMember(Alias = "items")]
        public List<LinkConfig> Items { get; set; } = new List<LinkConfig>();
    }

    public class MetaConfig
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }
    }

    public class ThemeConfig
    {
        [YamlMember(Alias = "base")]
        public Dictionary<string, string> Base { get; set; } = new Dictionary<string, string>();

        [YamlMember(Alias = "dark")]
        public Dictionary<string, string> Dark { get; set; } = new Dictionary<string, string>();

        [YamlMember(Alias = "highlight")]
        public HighlightConfig Highlight { get; set; } = new HighlightConfig();
    }

    public class HighlightConfig
    {
        [YamlMember(Alias = "light")]
        public string Light { get; set; } = "tokyo-night-light";

        [YamlMember(Alias = "dark")]
        public string Dark { get; set; } = "tokyo-night-dark";
    }
}
