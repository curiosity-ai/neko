using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Neko.Configuration
{
    public class NekoConfig
    {
        [YamlMember(Alias = "input")]
        public string Input { get; set; } = ".";

        [YamlMember(Alias = "output")]
        public string Output { get; set; } = ".neko";

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

        [YamlMember(Alias = "banner")]
        public BannerConfig Banner { get; set; } = new BannerConfig();

        [YamlMember(Alias = "snippets")]
        public SnippetsConfig Snippets { get; set; } = new SnippetsConfig();
    }

    public class SnippetsConfig
    {
        [YamlMember(Alias = "lineNumbers")]
        public List<string> LineNumbers { get; set; } = new List<string>();
    }

    public class BannerConfig
    {
        [YamlMember(Alias = "text")]
        public string Text { get; set; }

        [YamlMember(Alias = "link")]
        public string Link { get; set; }

        [YamlMember(Alias = "linkText")]
        public string LinkText { get; set; }

        [YamlMember(Alias = "visible")]
        public bool Visible { get; set; } = true;

        [YamlMember(Alias = "background")]
        public string Background { get; set; } = "bg-indigo-600";

        [YamlMember(Alias = "color")]
        public string Color { get; set; } = "text-white";

        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "global-banner";

        [YamlMember(Alias = "dismissible")]
        public bool Dismissible { get; set; } = true;
    }

    public class BrandingConfig
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; } = "Neko";

        [YamlMember(Alias = "label")]
        public string Label { get; set; }

        [YamlMember(Alias = "logo")]
        public string Logo { get; set; }

        [YamlMember(Alias = "logoDark")]
        public string LogoDark { get; set; }

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "favicon")]
        public string Favicon { get; set; }

        [YamlMember(Alias = "repository")]
        public string Repository { get; set; }
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

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "items")]
        public List<LinkConfig> Items { get; set; } = new List<LinkConfig>();

        [YamlMember(Alias = "footerItems")]
        public List<LinkConfig> FooterItems { get; set; } = new List<LinkConfig>();
    }

    public class MetaConfig
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "keywords")]
        public string Keywords { get; set; }

        [YamlMember(Alias = "author")]
        public string Author { get; set; }

        [YamlMember(Alias = "image")]
        public string Image { get; set; }

        [YamlMember(Alias = "url")]
        public string Url { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "twitterCard")]
        public string TwitterCard { get; set; }

        [YamlMember(Alias = "twitterSite")]
        public string TwitterSite { get; set; }

        [YamlMember(Alias = "twitterCreator")]
        public string TwitterCreator { get; set; }
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
