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
        
        [YamlMember(Alias = "cname")]
        public string Cname { get; set; }

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

        [YamlMember(Alias = "layout")]
        public LayoutConfig Layout { get; set; } = new LayoutConfig();

        [YamlMember(Alias = "poweredByNeko")]
        public bool PoweredByNeko { get; set; } = true;

        [YamlMember(Alias = "ignore")]
        public string[] Ignore { get; set; } = Array.Empty<string>();

        public void MergeWith(NekoConfig parent)
        {
            if (parent == null) return;

            // Inherit Branding properties if not set
            if (string.IsNullOrEmpty(Branding.Title)) Branding.Title = parent.Branding.Title;
            if (string.IsNullOrEmpty(Branding.Label)) Branding.Label = parent.Branding.Label;
            if (string.IsNullOrEmpty(Branding.Logo)) Branding.Logo = parent.Branding.Logo;
            if (string.IsNullOrEmpty(Branding.LogoDark)) Branding.LogoDark = parent.Branding.LogoDark;
            if (string.IsNullOrEmpty(Branding.Icon)) Branding.Icon = parent.Branding.Icon;
            if (string.IsNullOrEmpty(Branding.Favicon)) Branding.Favicon = parent.Branding.Favicon;
            if (string.IsNullOrEmpty(Branding.Repository)) Branding.Repository = parent.Branding.Repository;

            // Inherit Theme settings
            if (Theme.Name == "blue" && parent.Theme.Name != "blue") Theme.Name = parent.Theme.Name;

            foreach (var kvp in parent.Theme.Colors)
            {
                if (!Theme.Colors.ContainsKey(kvp.Key)) Theme.Colors[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in parent.Theme.Base)
            {
                if (!Theme.Base.ContainsKey(kvp.Key)) Theme.Base[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in parent.Theme.Dark)
            {
                if (!Theme.Dark.ContainsKey(kvp.Key)) Theme.Dark[kvp.Key] = kvp.Value;
            }

            if (Theme.Highlight.Light == "tokyo-night-light" && parent.Theme.Highlight.Light != "tokyo-night-light")
                Theme.Highlight.Light = parent.Theme.Highlight.Light;
            if (Theme.Highlight.Dark == "tokyo-night-dark" && parent.Theme.Highlight.Dark != "tokyo-night-dark")
                Theme.Highlight.Dark = parent.Theme.Highlight.Dark;

            // Inherit Snippets settings
            if (Snippets.LineNumbers.Count == 0 && parent.Snippets.LineNumbers.Count > 0)
            {
                Snippets.LineNumbers = new List<string>(parent.Snippets.LineNumbers);
            }

            // Inherit Ignore settings
            if ((Ignore == null || Ignore.Length == 0) && parent.Ignore != null && parent.Ignore.Length > 0)
            {
                Ignore = (string[])parent.Ignore.Clone();
            }
        }
    }

    public class LayoutConfig
    {
        [YamlMember(Alias = "sidebar")]
        public bool Sidebar { get; set; } = true;

        [YamlMember(Alias = "toc")]
        public bool Toc { get; set; } = true;
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

        [YamlIgnore]
        public string FolderPath { get; set; }
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
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = "blue";

        [YamlMember(Alias = "colors")]
        public Dictionary<string, string> Colors { get; set; } = new Dictionary<string, string>();

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
