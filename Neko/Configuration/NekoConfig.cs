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

        // Site personality. `docs` (default) renders the documentation chrome —
        // sidebar header with a border/shadow, the dark-mode toggle, and the
        // logo paired with the branding title. `blog` switches to the marketing
        // look used by curiosity.ai: a light, borderless header, the logo used
        // on its own as a wordmark (no duplicated title), pill-shaped search and
        // call-to-action buttons (see `actions:`), and a light page background.
        [YamlMember(Alias = "mode")]
        public string Mode { get; set; } = "docs";

        [YamlMember(Alias = "cname")]
        public string Cname { get; set; }

        [YamlMember(Alias = "password")]
        public string Password { get; set; }

        [YamlMember(Alias = "sitemap")]
        public bool Sitemap { get; set; } = true;

        [YamlMember(Alias = "branding")]
        public BrandingConfig Branding { get; set; } = new BrandingConfig();

        [YamlMember(Alias = "breadcrumb")]
        public BreadcrumbConfig Breadcrumb { get; set; } = new BreadcrumbConfig();

        [YamlMember(Alias = "links")]
        public List<LinkConfig> Links { get; set; } = new List<LinkConfig>();

        // Header call-to-action buttons, rendered on the right of the navbar as
        // pills (e.g. "Book a Demo", "Talk to Sales"). Each entry takes a
        // `text`, `link`, optional `icon`/`target`, and a `variant`
        // (`primary` = solid, `outline` = bordered). Available in both modes but
        // most at home in `blog` mode.
        [YamlMember(Alias = "actions")]
        public List<ActionConfig> Actions { get; set; } = new List<ActionConfig>();

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

        [YamlMember(Alias = "footer")]
        public FooterConfig Footer { get; set; } = new FooterConfig();

        [YamlMember(Alias = "layout")]
        public LayoutConfig Layout { get; set; } = new LayoutConfig();

        [YamlMember(Alias = "nav")]
        public NavConfig Nav { get; set; } = new NavConfig();

        [YamlMember(Alias = "poweredByNeko")]
        public bool PoweredByNeko { get; set; } = true;

        [YamlMember(Alias = "ignore")]
        public string[] Ignore { get; set; } = Array.Empty<string>();

        [YamlMember(Alias = "imageGen")]
        public ImageGenConfig ImageGen { get; set; } = new ImageGenConfig();

        [YamlMember(Alias = "tesserae")]
        public TesseraeConfig Tesserae { get; set; } = new TesseraeConfig();

        [YamlMember(Alias = "apiDocs")]
        public ApiDocsConfig ApiDocs { get; set; } = new ApiDocsConfig();

        [YamlMember(Alias = "blog")]
        public BlogConfig Blog { get; set; } = new BlogConfig();

        public void NormalizeLinks()
        {
            if (Banner != null)
            {
                Banner.Link = LinkNormalizer.Normalize(Banner.Link);
            }
            NormalizeLinkList(Links);
            if (Actions != null)
            {
                foreach (var action in Actions)
                    action.Link = LinkNormalizer.Normalize(action.Link);
            }
            if (Footer?.Columns != null)
            {
                foreach (var column in Footer.Columns)
                    NormalizeLinkList(column.Links);
            }
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

            // Inherit the site mode when the child left it at the default ("docs").
            if ((string.IsNullOrEmpty(Mode) || Mode.Equals("docs", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrEmpty(parent.Mode))
            {
                Mode = parent.Mode;
            }

            // Inherit header action buttons: only when the child defined none.
            if ((Actions == null || Actions.Count == 0) && parent.Actions != null && parent.Actions.Count > 0)
            {
                Actions = new List<ActionConfig>(parent.Actions);
            }

            // Inherit footer settings per-field when the child left them unset.
            if (Footer == null) Footer = new FooterConfig();
            if (parent.Footer != null)
            {
                if (string.IsNullOrEmpty(Footer.Copyright)) Footer.Copyright = parent.Footer.Copyright;
                if (string.IsNullOrEmpty(Footer.Logo)) Footer.Logo = parent.Footer.Logo;
                if (string.IsNullOrEmpty(Footer.Tagline)) Footer.Tagline = parent.Footer.Tagline;
                if (string.IsNullOrEmpty(Footer.CopyrightIcon)) Footer.CopyrightIcon = parent.Footer.CopyrightIcon;
                if ((Footer.Columns == null || Footer.Columns.Count == 0) && parent.Footer.Columns != null && parent.Footer.Columns.Count > 0)
                    Footer.Columns = new List<FooterColumnConfig>(parent.Footer.Columns);
                if ((Footer.Social == null || Footer.Social.Count == 0) && parent.Footer.Social != null && parent.Footer.Social.Count > 0)
                    Footer.Social = new List<FooterSocialConfig>(parent.Footer.Social);
                if ((Footer.Badges == null || Footer.Badges.Count == 0) && parent.Footer.Badges != null && parent.Footer.Badges.Count > 0)
                    Footer.Badges = new List<FooterBadgeConfig>(parent.Footer.Badges);
            }

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

            if (Theme.Highlight.Light == "github" && parent.Theme.Highlight.Light != "github")
                Theme.Highlight.Light = parent.Theme.Highlight.Light;
            if (Theme.Highlight.Dark == "tokyo-night-dark" && parent.Theme.Highlight.Dark != "tokyo-night-dark")
                Theme.Highlight.Dark = parent.Theme.Highlight.Dark;

            if (Theme.Font != null && parent.Theme.Font != null)
            {
                if (string.IsNullOrEmpty(Theme.Font.Family)) Theme.Font.Family = parent.Theme.Font.Family;
                if (string.IsNullOrEmpty(Theme.Font.Url)) Theme.Font.Url = parent.Theme.Font.Url;
            }

            foreach (var kvp in parent.Theme.Accent)
            {
                if (!Theme.Accent.ContainsKey(kvp.Key)) Theme.Accent[kvp.Key] = kvp.Value;
            }

            // Inherit Nav settings (only when the child left them unset/default)
            if (Nav == null) Nav = new NavConfig();
            if (Nav.Icons == null) Nav.Icons = new NavIconsConfig();
            if (parent.Nav != null)
            {
                // Top-nav icon toggles (header / dropdown / pivot) inherit per-flag.
                Nav.HeaderIcons ??= parent.Nav.HeaderIcons;
                Nav.DropdownIcons ??= parent.Nav.DropdownIcons;
                Nav.PivotIcons ??= parent.Nav.PivotIcons;

                // Sidebar icon mode inherits only when the child left the default ("none").
                if (parent.Nav.Icons != null
                    && string.Equals(Nav.Icons.Mode, "none", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(parent.Nav.Icons.Mode, "none", StringComparison.OrdinalIgnoreCase))
                {
                    Nav.Icons.Mode = parent.Nav.Icons.Mode;
                }
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

            // Inherit Tesserae settings (per-field, only when the child left the default)
            if (Tesserae == null) Tesserae = new TesseraeConfig();
            if (parent.Tesserae != null)
            {
                if (string.IsNullOrEmpty(Tesserae.Version)) Tesserae.Version = parent.Tesserae.Version;
                if (Tesserae.MaxParallelism == 0)           Tesserae.MaxParallelism = parent.Tesserae.MaxParallelism;
                if (Tesserae.MeasureWidth == 0)             Tesserae.MeasureWidth = parent.Tesserae.MeasureWidth;
            }
        }
    }

    public class TesseraeConfig
    {
        // Pin the Tesserae NuGet version used to compile live `tesserae` samples.
        // When empty, Neko resolves the latest stable version (cached on disk, see
        // TesseraeCompiler). Pinning makes the sample cache deterministic — a new
        // Tesserae release no longer invalidates every cached sample at once.
        [YamlMember(Alias = "version")]
        public string Version { get; set; }

        // Maximum number of Tesserae samples compiled in parallel during the
        // build's cache-warming pre-pass. 0 (default) means Environment.ProcessorCount.
        [YamlMember(Alias = "maxParallelism")]
        public int MaxParallelism { get; set; }

        // Viewport width (in CSS px) used by the `gen-tesserae-heights` command
        // when measuring sample heights with a headless browser. Should approximate
        // the rendered width of the live-preview iframe in the docs content column.
        // 0 (default) means use the built-in default. Normal builds never measure.
        [YamlMember(Alias = "measureWidth")]
        public int MeasureWidth { get; set; }
    }

    // Configures `neko sync-api-docs` (also run by default before build/watch).
    // Maps a source-root name used in `<!-- api:source repo="…" -->` markers to a
    // local checkout. Only the root neko.yml is consulted; paths are resolved
    // relative to it. A missing root is skipped (the committed block is left
    // intact). There is no CLI override, environment-variable, or hard-coded path
    // fallback.
    public class ApiDocsConfig
    {
        [YamlMember(Alias = "roots")]
        public Dictionary<string, string> Roots { get; set; }
    }

    // Hero shown above the post grid on the blog index page (blog mode): a small
    // rounded pill, the large page title, and an optional lead paragraph. When
    // `title` is set it replaces the plain label H1 that the index would
    // otherwise render. Ignored outside blog mode.
    public class BlogConfig
    {
        [YamlMember(Alias = "pill")]
        public string Pill { get; set; }

        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        // Optional lead paragraph under the title. `lead` and `description` are
        // accepted as aliases for the same slot; `lead` wins when both are set.
        [YamlMember(Alias = "lead")]
        public string Lead { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }
    }

    public class NavConfig
    {
        // Sidebar icon mode (none/all/folders/pages/top). Distinct from the
        // top-nav icon toggles below.
        [YamlMember(Alias = "icons")]
        public NavIconsConfig Icons { get; set; } = new NavIconsConfig();

        // Show icons on the top-level header links and the dropdown trigger buttons.
        // Null/false hides them; set to true in neko.yml to opt back in. Named
        // `headerIcons` to avoid colliding with the sidebar `icons` object above.
        [YamlMember(Alias = "headerIcons")]
        public bool? HeaderIcons { get; set; }

        // Show icons on the items inside dropdown flyout menus (and footer items).
        [YamlMember(Alias = "dropdownIcons")]
        public bool? DropdownIcons { get; set; }

        // Show icons on the contextual pivot tab bar.
        [YamlMember(Alias = "pivotIcons")]
        public bool? PivotIcons { get; set; }
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

        // Caps the width of the header content, pivot tabs, and the
        // sidebar + content + TOC row, centring them on wide screens so the
        // layout stops expanding past a comfortable reading width. Accepts a
        // Tailwind max-width token (e.g. "screen-2xl", "7xl"), a full
        // "max-w-…" class, or a raw CSS length ("1600px", "90rem"). Use
        // "full" or "none" to disable the cap and span the full viewport.
        [YamlMember(Alias = "maxWidth")]
        public string MaxWidth { get; set; } = "screen-2xl";
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

    public class BreadcrumbConfig
    {
        // Friendly name for this (sub-)project in the cross-project search
        // breadcrumb trail — the first crumb a result shows. Falls back to
        // `branding.label`, then `branding.title`, then a title-cased route
        // prefix when unset. Project-local: not inherited from a parent config.
        [YamlMember(Alias = "label")]
        public string Label { get; set; }

        // Reserved for the on-page breadcrumb home marker (icon/text). Parsed so
        // a `breadcrumb: { home: … }` block doesn't trip up deserialization.
        [YamlMember(Alias = "home")]
        public string Home { get; set; }
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

    // Footer configuration. `copyright` works in both modes (docs renders it on
    // the slim in-content footer). The richer `columns` / `social` / `badges` /
    // `logo` fields drive the full marketing-style footer used in `blog` mode —
    // when none are set, blog mode falls back to a slim centred footer.
    public class FooterConfig
    {
        [YamlMember(Alias = "copyright")]
        public string Copyright { get; set; }

        // Optional footer logo. Defaults to branding.logoDark / branding.logo.
        [YamlMember(Alias = "logo")]
        public string Logo { get; set; }

        // Optional short blurb under the footer logo.
        [YamlMember(Alias = "tagline")]
        public string Tagline { get; set; }

        // Optional icon shown before the copyright line (blog-mode mega footer).
        // A UIcon name (e.g. "cookie") or an image path (e.g. "/assets/img/cookie.svg").
        // Defaults to the built-in cookie glyph when unset.
        [YamlMember(Alias = "copyrightIcon")]
        public string CopyrightIcon { get; set; }

        [YamlMember(Alias = "columns")]
        public List<FooterColumnConfig> Columns { get; set; } = new List<FooterColumnConfig>();

        [YamlMember(Alias = "social")]
        public List<FooterSocialConfig> Social { get; set; } = new List<FooterSocialConfig>();

        [YamlMember(Alias = "badges")]
        public List<FooterBadgeConfig> Badges { get; set; } = new List<FooterBadgeConfig>();

        // True when any of the rich (marketing) footer fields are populated.
        [YamlIgnore]
        public bool HasRichContent =>
            (Columns != null && Columns.Count > 0)
            || (Social != null && Social.Count > 0)
            || (Badges != null && Badges.Count > 0);
    }

    public class FooterColumnConfig
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "links")]
        public List<LinkConfig> Links { get; set; } = new List<LinkConfig>();
    }

    public class FooterSocialConfig
    {
        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "link")]
        public string Link { get; set; }

        [YamlMember(Alias = "label")]
        public string Label { get; set; }
    }

    public class FooterBadgeConfig
    {
        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }
    }

    public class ActionConfig
    {
        [YamlMember(Alias = "text")]
        public string Text { get; set; }

        [YamlMember(Alias = "link")]
        public string Link { get; set; }

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "target")]
        public string Target { get; set; }

        // `primary` (solid, filled) or `outline` (bordered). Defaults to primary.
        [YamlMember(Alias = "variant")]
        public string Variant { get; set; } = "primary";
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

        // Optional body font override. When set, Neko loads this font (instead of
        // its bundled Inter) and uses it as the site's base `font-family`.
        [YamlMember(Alias = "font")]
        public FontConfig Font { get; set; } = new FontConfig();
    }

    // Configures the site's base font. Leave `family` empty to keep Neko's
    // default (Inter). Set `family` to a CSS font-family name and, optionally,
    // `url` to a stylesheet that provides it (e.g. a Google Fonts link).
    public class FontConfig
    {
        // The CSS font-family name, e.g. "Plus Jakarta Sans". Neko appends a
        // `, sans-serif` fallback automatically.
        [YamlMember(Alias = "family")]
        public string Family { get; set; }

        // Optional stylesheet URL that provides the font (Google Fonts, a CDN,
        // or a self-hosted `/assets/….css`). Omit when the font is already
        // available (system font, or loaded by an include).
        [YamlMember(Alias = "url")]
        public string Url { get; set; }
    }

    public class HighlightConfig
    {
        [YamlMember(Alias = "light")]
        public string Light { get; set; } = "github";

        [YamlMember(Alias = "dark")]
        public string Dark { get; set; } = "tokyo-night-dark";
    }
}
