using System;
using System.Collections.Generic;

namespace Neko.Configuration
{
    public static class ThemeDefinitions
    {
        public static readonly Dictionary<string, Dictionary<string, string>> Themes = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "blue", new Dictionary<string, string>
                {
                    { "50", "#eff6ff" },
                    { "100", "#dbeafe" },
                    { "200", "#bfdbfe" },
                    { "300", "#93c5fd" },
                    { "400", "#60a5fa" },
                    { "500", "#3b82f6" },
                    { "600", "#2563eb" },
                    { "700", "#1d4ed8" },
                    { "800", "#1e40af" },
                    { "900", "#1e3a8a" },
                    { "950", "#172554" }
                }
            },
            {
                "violet", new Dictionary<string, string>
                {
                    { "50", "#f5f3ff" },
                    { "100", "#ede9fe" },
                    { "200", "#ddd6fe" },
                    { "300", "#c4b5fd" },
                    { "400", "#a78bfa" },
                    { "500", "#8b5cf6" },
                    { "600", "#7c3aed" },
                    { "700", "#6d28d9" },
                    { "800", "#5b21b6" },
                    { "900", "#4c1d95" },
                    { "950", "#2e1065" }
                }
            },
            {
                "emerald", new Dictionary<string, string>
                {
                    { "50", "#ecfdf5" },
                    { "100", "#d1fae5" },
                    { "200", "#a7f3d0" },
                    { "300", "#6ee7b7" },
                    { "400", "#34d399" },
                    { "500", "#10b981" },
                    { "600", "#059669" },
                    { "700", "#047857" },
                    { "800", "#065f46" },
                    { "900", "#064e3b" },
                    { "950", "#022c22" }
                }
            },
            {
                "rose", new Dictionary<string, string>
                {
                    { "50", "#fff1f2" },
                    { "100", "#ffe4e6" },
                    { "200", "#fecdd3" },
                    { "300", "#fda4af" },
                    { "400", "#fb7185" },
                    { "500", "#f43f5e" },
                    { "600", "#e11d48" },
                    { "700", "#be123c" },
                    { "800", "#9f1239" },
                    { "900", "#881337" },
                    { "950", "#4c0519" }
                }
            },
            {
                "amber", new Dictionary<string, string>
                {
                    { "50", "#fffbeb" },
                    { "100", "#fef3c7" },
                    { "200", "#fde68a" },
                    { "300", "#fcd34d" },
                    { "400", "#fbbf24" },
                    { "500", "#f59e0b" },
                    { "600", "#d97706" },
                    { "700", "#b45309" },
                    { "800", "#92400e" },
                    { "900", "#78350f" },
                    { "950", "#451a03" }
                }
            },
            {
                "sky", new Dictionary<string, string>
                {
                    { "50", "#f0f9ff" },
                    { "100", "#e0f2fe" },
                    { "200", "#bae6fd" },
                    { "300", "#7dd3fc" },
                    { "400", "#38bdf8" },
                    { "500", "#0ea5e9" },
                    { "600", "#0284c7" },
                    { "700", "#0369a1" },
                    { "800", "#075985" },
                    { "900", "#0c4a6e" },
                    { "950", "#082f49" }
                }
            },
            {
                "fuchsia", new Dictionary<string, string>
                {
                    { "50", "#fdf4ff" },
                    { "100", "#fae8ff" },
                    { "200", "#f5d0fe" },
                    { "300", "#f0abfc" },
                    { "400", "#e879f9" },
                    { "500", "#d946ef" },
                    { "600", "#c026d3" },
                    { "700", "#a21caf" },
                    { "800", "#86198f" },
                    { "900", "#701a75" },
                    { "950", "#4a044e" }
                }
            }
        };

        public static Dictionary<string, string> GetTheme(string name)
        {
            if (string.IsNullOrEmpty(name)) return Themes["blue"];
            return Themes.TryGetValue(name, out var theme) ? theme : Themes["blue"];
        }
    }
}