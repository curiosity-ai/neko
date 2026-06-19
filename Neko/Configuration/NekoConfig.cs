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

        [YamlMember(Alias = "password")]
        public string Password { get; set; }

        [YamlMember(Alias = "sitemap")]
        public bool Sitemap { get; set; } = true;

        [YamlMember(Alias = "branding")]
        public BrandingConfig Branding { get; set; } = new BrandingConfig();

        [YamlMember(Alias = "links")]
        public List<LinkConfig> Links { get; set; } = new List<LinkConfig>();

        [YamlMember(Alias = "pageLinks")]
        public List<PageLinkConfig> PageLinks { get; set; } = new List<PageLinkConfig>();

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

        [YamlMember(Alias = "imageGen")]
        public ImageGenConfig ImageGen { get; set; } = new ImageGenConfig();

        [YamlMember(Alias = "nav")]
        public NavConfig Nav { get; set; } = new NavConfig();

        public void NormalizeLinks()
        {
            if (Banner != null)
            {
                Banner.Link = LinkNormalizer.Normalize(Banner.Link);
            }
            NormalizeLinkList(Links);
        }

        private static void NormalizeLinkList(List<LinkConfig> links)
        {
            if (links == null) return;
            foreach (var link in links)
            {
                link.Link = LinkNormalizer.Normalize(link.Link);
                NormalizeLinkList(link.Items);
                NormalizeLinkList(link.FooterItems);
            }
        }

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

            if (string.IsNullOrEmpty(Password)) Password = parent.Password;

            // Inherit Theme settings
            if (Theme.Name == ThemeDefinitions.DefaultThemeName && parent.Theme.Name != ThemeDefinitions.DefaultThemeName) Theme.Name = parent.Theme.Name;

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

            foreach (var kvp in parent.Theme.Accent)
            {
                if (!Theme.Accent.ContainsKey(kvp.Key)) Theme.Accent[kvp.Key] = kvp.Value;
            }

            // Inherit Snippets settings
            if (Snippets.LineNumbers.Count == 0 && parent.Snippets.LineNumbers.Count > 0)
            {
                Snippets.LineNumbers = new List<string>(parent.Snippets.LineNumbers);
            }

            // Inherit PageLinks: only when the child defined none of its own.
            if ((PageLinks == null || PageLinks.Count == 0) && parent.PageLinks != null && parent.PageLinks.Count > 0)
            {
                PageLinks = new List<PageLinkConfig>(parent.PageLinks);
            }

            // Inherit Ignore settings
            if ((Ignore == null || Ignore.Length == 0) && parent.Ignore != null && parent.Ignore.Length > 0)
            {
                Ignore = (string[])parent.Ignore.Clone();
            }

            // Inherit Nav settings (only when the child left the default)
            if (Nav == null) Nav = new NavConfig();
            if (Nav.Icons == null) Nav.Icons = new NavIconsConfig();
            if (parent.Nav?.Icons != null
                && string.Equals(Nav.Icons.Mode, "none", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(parent.Nav.Icons.Mode, "none", StringComparison.OrdinalIgnoreCase))
            {
                Nav.Icons.Mode = parent.Nav.Icons.Mode;
            }

            // Inherit ImageGen settings (per-field, only when the child left the default)
            if (ImageGen == null) ImageGen = new ImageGenConfig();
            if (parent.ImageGen != null)
            {
                if (string.IsNullOrEmpty(ImageGen.SystemPrompt))   ImageGen.SystemPrompt   = parent.ImageGen.SystemPrompt;
                if (string.IsNullOrEmpty(ImageGen.LightModePrompt) || ImageGen.LightModePrompt == ImageGenConfig.DefaultLightModePrompt)
                    ImageGen.LightModePrompt = parent.ImageGen.LightModePrompt;
                if (string.IsNullOrEmpty(ImageGen.DarkModePrompt)  || ImageGen.DarkModePrompt  == ImageGenConfig.DefaultDarkModePrompt)
                    ImageGen.DarkModePrompt  = parent.ImageGen.DarkModePrompt;
                if (string.IsNullOrEmpty(ImageGen.Size) || ImageGen.Size == ImageGenConfig.DefaultSize)
                    ImageGen.Size = parent.ImageGen.Size;
            }
        }
    }

    public class NavConfig
    {
        [YamlMember(Alias = "icons")]
        public NavIconsConfig Icons { get; set; } = new NavIconsConfig();
    }

    public class NavIconsConfig
    {
        /// <summary>
        /// Controls which sidebar navigation items render an icon.
        /// One of: <c>none</c> (default), <c>all</c>, <c>folders</c>,
        /// <c>pages</c>, <c>top</c>. Icons are hidden by default and must be
        /// opted into explicitly.
        /// </summary>
        [YamlMember(Alias = "mode")]
        public string Mode { get; set; } = "none";
    }

    public class ImageGenConfig
    {
        public const string DefaultSize = "1536x1024";
        public const string DefaultLightModePrompt = "Render this in light mode: use a clean white or very light background and colors suitable for display on a light/white theme.";
        public const string DefaultDarkModePrompt = "Recreate this exact same image, preserving the composition, subjects, layout and details, but adapted for dark mode: use a dark background (near-black or deep neutral) and adjust foreground colors so the image reads clearly on a dark/black theme.";

        [YamlMember(Alias = "systemPrompt")]
        public string SystemPrompt { get; set; }

        [YamlMember(Alias = "size")]
        public string Size { get; set; } = DefaultSize;

        [YamlMember(Alias = "lightMode")]
        public bool LightMode { get; set; } = true;

        [YamlMember(Alias = "darkMode")]
        public bool DarkMode { get; set; } = true;

        [YamlMember(Alias = "lightModePrompt")]
        public string LightModePrompt { get; set; } = DefaultLightModePrompt;

        [YamlMember(Alias = "darkModePrompt")]
        public string DarkModePrompt { get; set; } = DefaultDarkModePrompt;
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

        [YamlIgnore]
        public string Password { get; set; }
    }

    public class PageLinkConfig
    {
        [YamlMember(Alias = "label")]
        public string Label { get; set; }

        [YamlMember(Alias = "url")]
        public string Url { get; set; }

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "target")]
        public string Target { get; set; }
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
        public string Name { get; set; } = ThemeDefinitions.DefaultThemeName;

        [YamlMember(Alias = "colors")]
        public Dictionary<string, string> Colors { get; set; } = new Dictionary<string, string>();

        [YamlMember(Alias = "base")]
        public Dictionary<string, string> Base { get; set; } = new Dictionary<string, string>();

        [YamlMember(Alias = "dark")]
        public Dictionary<string, string> Dark { get; set; } = new Dictionary<string, string>();

        [YamlMember(Alias = "highlight")]
        public HighlightConfig Highlight { get; set; } = new HighlightConfig();

        [YamlMember(Alias = "accent")]
        public Dictionary<string, string> Accent { get; set; } = new Dictionary<string, string>();
    }

    public class HighlightConfig
    {
        [YamlMember(Alias = "light")]
        public string Light { get; set; } = "tokyo-night-light";

        [YamlMember(Alias = "dark")]
        public string Dark { get; set; } = "tokyo-night-dark";
    }
}
