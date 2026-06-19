using System;
using System.Collections.Generic;
using System.Globalization;
using Neko.Configuration;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// The subset of Tailwind v3's default theme that Neko's class vocabulary
    /// uses, overlaid with the per-site <c>primary</c>/<c>accent</c> palettes from
    /// <see cref="ThemeDefinitions.ResolvePalettes"/>. Pure data — the registry
    /// reads these scales to resolve utility values.
    /// </summary>
    internal sealed class TailwindTheme
    {
        /// <summary>color family → (shade → hex). Plus single-value colors keyed by name with shade "".</summary>
        public Dictionary<string, Dictionary<string, string>> Colors { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> Spacing { get; }
        public Dictionary<string, (string Size, string LineHeight)> FontSize { get; }
        public Dictionary<string, string> FontWeight { get; }
        public Dictionary<string, string> LineHeight { get; }
        public Dictionary<string, string> LetterSpacing { get; }
        public Dictionary<string, string> BorderRadius { get; }
        public Dictionary<string, string> BorderWidth { get; }
        public Dictionary<string, string> Opacity { get; }
        public Dictionary<string, string> ZIndex { get; }
        public Dictionary<string, string> Screens { get; }
        public Dictionary<string, string> Blur { get; }
        public Dictionary<string, string> Duration { get; }
        public Dictionary<string, string> MaxWidth { get; }

        public TailwindTheme(NekoConfig config)
        {
            BuildColors(config);
            Spacing = BuildSpacing();
            FontSize = BuildFontSize();
            FontWeight = BuildFontWeight();
            LineHeight = BuildLineHeight();
            LetterSpacing = BuildLetterSpacing();
            BorderRadius = BuildBorderRadius();
            BorderWidth = BuildBorderWidth();
            Opacity = BuildOpacity();
            ZIndex = BuildZIndex();
            Screens = BuildScreens();
            Blur = BuildBlur();
            Duration = BuildDuration();
            MaxWidth = BuildMaxWidth();
        }

        /// <summary>Resolves a color token (e.g. <c>primary-600</c>, <c>white</c>, <c>black</c>) to a hex string, or null.</summary>
        public string ResolveColorHex(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            int dash = token.LastIndexOf('-');
            if (dash > 0)
            {
                var name = token.Substring(0, dash);
                var shade = token.Substring(dash + 1);
                if (Colors.TryGetValue(name, out var ramp) && ramp.TryGetValue(shade, out var hex))
                    return hex;
            }
            // Single-keyword colors (white/black/...) stored under "".
            if (Colors.TryGetValue(token, out var single) && single.TryGetValue("", out var v))
                return v;
            return null;
        }

        private void BuildColors(NekoConfig config)
        {
            foreach (var kvp in DefaultColors)
                Colors[kvp.Key] = new Dictionary<string, string>(kvp.Value, StringComparer.Ordinal);

            var (primary, accent) = ThemeDefinitions.ResolvePalettes(config);
            Colors["primary"] = new Dictionary<string, string>(primary, StringComparer.Ordinal);
            Colors["accent"] = new Dictionary<string, string>(accent, StringComparer.Ordinal);

            // Keyword colors live under the empty-string shade.
            Add("white", "#fff");
            Add("black", "#000");
            Add("transparent", "transparent");
            Add("current", "currentColor");
            Add("inherit", "inherit");

            void Add(string name, string val)
            {
                Colors[name] = new Dictionary<string, string>(StringComparer.Ordinal) { [""] = val };
            }
        }

        /// <summary>Converts a hex color to its space-separated RGB channels, e.g. <c>#2563eb</c> → <c>37 99 235</c>.</summary>
        public static string HexToRgbChannels(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex[0] != '#') return null;
            var h = hex.Substring(1);
            if (h.Length == 3)
                h = string.Concat(h[0], h[0], h[1], h[1], h[2], h[2]);
            if (h.Length != 6) return null;
            int r = int.Parse(h.Substring(0, 2), NumberStyles.HexNumber);
            int g = int.Parse(h.Substring(2, 2), NumberStyles.HexNumber);
            int b = int.Parse(h.Substring(4, 2), NumberStyles.HexNumber);
            return $"{r} {g} {b}";
        }

        private static Dictionary<string, string> BuildSpacing()
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["0"] = "0px",
                ["px"] = "1px",
                ["0.5"] = "0.125rem",
                ["1"] = "0.25rem",
                ["1.5"] = "0.375rem",
                ["2"] = "0.5rem",
                ["2.5"] = "0.625rem",
                ["3"] = "0.75rem",
                ["3.5"] = "0.875rem",
                ["4"] = "1rem",
                ["5"] = "1.25rem",
                ["6"] = "1.5rem",
                ["7"] = "1.75rem",
                ["8"] = "2rem",
                ["9"] = "2.25rem",
                ["10"] = "2.5rem",
                ["11"] = "2.75rem",
                ["12"] = "3rem",
                ["14"] = "3.5rem",
                ["16"] = "4rem",
                ["20"] = "5rem",
                ["24"] = "6rem",
                ["28"] = "7rem",
                ["32"] = "8rem",
                ["36"] = "9rem",
                ["40"] = "10rem",
                ["44"] = "11rem",
                ["48"] = "12rem",
                ["52"] = "13rem",
                ["56"] = "14rem",
                ["60"] = "15rem",
                ["64"] = "16rem",
                ["72"] = "18rem",
                ["80"] = "20rem",
                ["96"] = "24rem",
            };
            return d;
        }

        private static Dictionary<string, (string, string)> BuildFontSize()
        {
            return new Dictionary<string, (string, string)>(StringComparer.Ordinal)
            {
                ["xs"] = ("0.75rem", "1rem"),
                ["sm"] = ("0.875rem", "1.25rem"),
                ["base"] = ("1rem", "1.5rem"),
                ["lg"] = ("1.125rem", "1.75rem"),
                ["xl"] = ("1.25rem", "1.75rem"),
                ["2xl"] = ("1.5rem", "2rem"),
                ["3xl"] = ("1.875rem", "2.25rem"),
                ["4xl"] = ("2.25rem", "2.5rem"),
                ["5xl"] = ("3rem", "1"),
                ["6xl"] = ("3.75rem", "1"),
                ["7xl"] = ("4.5rem", "1"),
                ["8xl"] = ("6rem", "1"),
                ["9xl"] = ("8rem", "1"),
            };
        }

        private static Dictionary<string, string> BuildFontWeight() => new(StringComparer.Ordinal)
        {
            ["thin"] = "100",
            ["extralight"] = "200",
            ["light"] = "300",
            ["normal"] = "400",
            ["medium"] = "500",
            ["semibold"] = "600",
            ["bold"] = "700",
            ["extrabold"] = "800",
            ["black"] = "900",
        };

        private static Dictionary<string, string> BuildLineHeight() => new(StringComparer.Ordinal)
        {
            ["none"] = "1",
            ["tight"] = "1.25",
            ["snug"] = "1.375",
            ["normal"] = "1.5",
            ["relaxed"] = "1.625",
            ["loose"] = "2",
            ["3"] = ".75rem",
            ["4"] = "1rem",
            ["5"] = "1.25rem",
            ["6"] = "1.5rem",
            ["7"] = "1.75rem",
            ["8"] = "2rem",
            ["9"] = "2.25rem",
            ["10"] = "2.5rem",
        };

        private static Dictionary<string, string> BuildLetterSpacing() => new(StringComparer.Ordinal)
        {
            ["tighter"] = "-0.05em",
            ["tight"] = "-0.025em",
            ["normal"] = "0em",
            ["wide"] = "0.025em",
            ["wider"] = "0.05em",
            ["widest"] = "0.1em",
        };

        private static Dictionary<string, string> BuildBorderRadius() => new(StringComparer.Ordinal)
        {
            ["none"] = "0px",
            ["sm"] = "0.125rem",
            [""] = "0.25rem",
            ["md"] = "0.375rem",
            ["lg"] = "0.5rem",
            ["xl"] = "0.75rem",
            ["2xl"] = "1rem",
            ["3xl"] = "1.5rem",
            ["full"] = "9999px",
        };

        private static Dictionary<string, string> BuildBorderWidth() => new(StringComparer.Ordinal)
        {
            [""] = "1px",
            ["0"] = "0px",
            ["2"] = "2px",
            ["4"] = "4px",
            ["8"] = "8px",
        };

        private static Dictionary<string, string> BuildOpacity()
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var n in new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 })
                d[n.ToString()] = (n / 100.0).ToString(CultureInfo.InvariantCulture);
            return d;
        }

        private static Dictionary<string, string> BuildZIndex() => new(StringComparer.Ordinal)
        {
            ["0"] = "0",
            ["10"] = "10",
            ["20"] = "20",
            ["30"] = "30",
            ["40"] = "40",
            ["50"] = "50",
            ["auto"] = "auto",
        };

        private static Dictionary<string, string> BuildScreens() => new(StringComparer.Ordinal)
        {
            ["sm"] = "640px",
            ["md"] = "768px",
            ["lg"] = "1024px",
            ["xl"] = "1280px",
            ["2xl"] = "1536px",
        };

        private static Dictionary<string, string> BuildBlur() => new(StringComparer.Ordinal)
        {
            ["none"] = "0",
            [""] = "8px",
            ["sm"] = "4px",
            ["md"] = "12px",
            ["lg"] = "16px",
            ["xl"] = "24px",
            ["2xl"] = "40px",
            ["3xl"] = "64px",
        };

        private static Dictionary<string, string> BuildDuration() => new(StringComparer.Ordinal)
        {
            ["0"] = "0s",
            ["75"] = "75ms",
            ["100"] = "100ms",
            ["150"] = "150ms",
            ["200"] = "200ms",
            ["300"] = "300ms",
            ["500"] = "500ms",
            ["700"] = "700ms",
            ["1000"] = "1000ms",
        };

        private static Dictionary<string, string> BuildMaxWidth() => new(StringComparer.Ordinal)
        {
            ["0"] = "0rem",
            ["none"] = "none",
            ["xs"] = "20rem",
            ["sm"] = "24rem",
            ["md"] = "28rem",
            ["lg"] = "32rem",
            ["xl"] = "36rem",
            ["2xl"] = "42rem",
            ["3xl"] = "48rem",
            ["4xl"] = "56rem",
            ["5xl"] = "64rem",
            ["6xl"] = "72rem",
            ["7xl"] = "80rem",
            ["full"] = "100%",
            ["min"] = "min-content",
            ["max"] = "max-content",
            ["fit"] = "fit-content",
            ["prose"] = "65ch",
            ["screen-sm"] = "640px",
            ["screen-md"] = "768px",
            ["screen-lg"] = "1024px",
            ["screen-xl"] = "1280px",
            ["screen-2xl"] = "1536px",
        };

        // The full Tailwind v3 default color palette (extracted/verified against
        // the standalone CLI output). primary/accent are layered in at runtime.
        private static readonly Dictionary<string, Dictionary<string, string>> DefaultColors = new(StringComparer.Ordinal)
        {
            ["slate"] = new() { ["50"]="#f8fafc",["100"]="#f1f5f9",["200"]="#e2e8f0",["300"]="#cbd5e1",["400"]="#94a3b8",["500"]="#64748b",["600"]="#475569",["700"]="#334155",["800"]="#1e293b",["900"]="#0f172a",["950"]="#020617" },
            ["gray"] = new() { ["50"]="#f9fafb",["100"]="#f3f4f6",["200"]="#e5e7eb",["300"]="#d1d5db",["400"]="#9ca3af",["500"]="#6b7280",["600"]="#4b5563",["700"]="#374151",["800"]="#1f2937",["900"]="#111827",["950"]="#030712" },
            ["zinc"] = new() { ["50"]="#fafafa",["100"]="#f4f4f5",["200"]="#e4e4e7",["300"]="#d4d4d8",["400"]="#a1a1aa",["500"]="#71717a",["600"]="#52525b",["700"]="#3f3f46",["800"]="#27272a",["900"]="#18181b",["950"]="#09090b" },
            ["neutral"] = new() { ["50"]="#fafafa",["100"]="#f5f5f5",["200"]="#e5e5e5",["300"]="#d4d4d4",["400"]="#a3a3a3",["500"]="#737373",["600"]="#525252",["700"]="#404040",["800"]="#262626",["900"]="#171717",["950"]="#0a0a0a" },
            ["stone"] = new() { ["50"]="#fafaf9",["100"]="#f5f5f4",["200"]="#e7e5e4",["300"]="#d6d3d1",["400"]="#a8a29e",["500"]="#78716c",["600"]="#57534e",["700"]="#44403c",["800"]="#292524",["900"]="#1c1917",["950"]="#0c0a09" },
            ["red"] = new() { ["50"]="#fef2f2",["100"]="#fee2e2",["200"]="#fecaca",["300"]="#fca5a5",["400"]="#f87171",["500"]="#ef4444",["600"]="#dc2626",["700"]="#b91c1c",["800"]="#991b1b",["900"]="#7f1d1d",["950"]="#450a0a" },
            ["orange"] = new() { ["50"]="#fff7ed",["100"]="#ffedd5",["200"]="#fed7aa",["300"]="#fdba74",["400"]="#fb923c",["500"]="#f97316",["600"]="#ea580c",["700"]="#c2410c",["800"]="#9a3412",["900"]="#7c2d12",["950"]="#431407" },
            ["amber"] = new() { ["50"]="#fffbeb",["100"]="#fef3c7",["200"]="#fde68a",["300"]="#fcd34d",["400"]="#fbbf24",["500"]="#f59e0b",["600"]="#d97706",["700"]="#b45309",["800"]="#92400e",["900"]="#78350f",["950"]="#451a03" },
            ["yellow"] = new() { ["50"]="#fefce8",["100"]="#fef9c3",["200"]="#fef08a",["300"]="#fde047",["400"]="#facc15",["500"]="#eab308",["600"]="#ca8a04",["700"]="#a16207",["800"]="#854d0e",["900"]="#713f12",["950"]="#422006" },
            ["lime"] = new() { ["50"]="#f7fee7",["100"]="#ecfccb",["200"]="#d9f99d",["300"]="#bef264",["400"]="#a3e635",["500"]="#84cc16",["600"]="#65a30d",["700"]="#4d7c0f",["800"]="#3f6212",["900"]="#365314",["950"]="#1a2e05" },
            ["green"] = new() { ["50"]="#f0fdf4",["100"]="#dcfce7",["200"]="#bbf7d0",["300"]="#86efac",["400"]="#4ade80",["500"]="#22c55e",["600"]="#16a34a",["700"]="#15803d",["800"]="#166534",["900"]="#14532d",["950"]="#052e16" },
            ["emerald"] = new() { ["50"]="#ecfdf5",["100"]="#d1fae5",["200"]="#a7f3d0",["300"]="#6ee7b7",["400"]="#34d399",["500"]="#10b981",["600"]="#059669",["700"]="#047857",["800"]="#065f46",["900"]="#064e3b",["950"]="#022c22" },
            ["teal"] = new() { ["50"]="#f0fdfa",["100"]="#ccfbf1",["200"]="#99f6e4",["300"]="#5eead4",["400"]="#2dd4bf",["500"]="#14b8a6",["600"]="#0d9488",["700"]="#0f766e",["800"]="#115e59",["900"]="#134e4a",["950"]="#042f2e" },
            ["cyan"] = new() { ["50"]="#ecfeff",["100"]="#cffafe",["200"]="#a5f3fc",["300"]="#67e8f9",["400"]="#22d3ee",["500"]="#06b6d4",["600"]="#0891b2",["700"]="#0e7490",["800"]="#155e75",["900"]="#164e63",["950"]="#083344" },
            ["sky"] = new() { ["50"]="#f0f9ff",["100"]="#e0f2fe",["200"]="#bae6fd",["300"]="#7dd3fc",["400"]="#38bdf8",["500"]="#0ea5e9",["600"]="#0284c7",["700"]="#0369a1",["800"]="#075985",["900"]="#0c4a6e",["950"]="#082f49" },
            ["blue"] = new() { ["50"]="#eff6ff",["100"]="#dbeafe",["200"]="#bfdbfe",["300"]="#93c5fd",["400"]="#60a5fa",["500"]="#3b82f6",["600"]="#2563eb",["700"]="#1d4ed8",["800"]="#1e40af",["900"]="#1e3a8a",["950"]="#172554" },
            ["indigo"] = new() { ["50"]="#eef2ff",["100"]="#e0e7ff",["200"]="#c7d2fe",["300"]="#a5b4fc",["400"]="#818cf8",["500"]="#6366f1",["600"]="#4f46e5",["700"]="#4338ca",["800"]="#3730a3",["900"]="#312e81",["950"]="#1e1b4b" },
            ["violet"] = new() { ["50"]="#f5f3ff",["100"]="#ede9fe",["200"]="#ddd6fe",["300"]="#c4b5fd",["400"]="#a78bfa",["500"]="#8b5cf6",["600"]="#7c3aed",["700"]="#6d28d9",["800"]="#5b21b6",["900"]="#4c1d95",["950"]="#2e1065" },
            ["purple"] = new() { ["50"]="#faf5ff",["100"]="#f3e8ff",["200"]="#e9d5ff",["300"]="#d8b4fe",["400"]="#c084fc",["500"]="#a855f7",["600"]="#9333ea",["700"]="#7e22ce",["800"]="#6b21a8",["900"]="#581c87",["950"]="#3b0764" },
            ["fuchsia"] = new() { ["50"]="#fdf4ff",["100"]="#fae8ff",["200"]="#f5d0fe",["300"]="#f0abfc",["400"]="#e879f9",["500"]="#d946ef",["600"]="#c026d3",["700"]="#a21caf",["800"]="#86198f",["900"]="#701a75",["950"]="#4a044e" },
            ["pink"] = new() { ["50"]="#fdf2f8",["100"]="#fce7f3",["200"]="#fbcfe8",["300"]="#f9a8d4",["400"]="#f472b6",["500"]="#ec4899",["600"]="#db2777",["700"]="#be185d",["800"]="#9d174d",["900"]="#831843",["950"]="#500724" },
            ["rose"] = new() { ["50"]="#fff1f2",["100"]="#ffe4e6",["200"]="#fecdd3",["300"]="#fda4af",["400"]="#fb7185",["500"]="#f43f5e",["600"]="#e11d48",["700"]="#be123c",["800"]="#9f1239",["900"]="#881337",["950"]="#4c0519" },
        };
    }
}
