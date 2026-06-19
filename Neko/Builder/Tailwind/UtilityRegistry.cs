using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Stage 3: maps a parsed <see cref="Candidate"/> to its CSS declarations,
    /// matching the Tailwind v3 standalone CLI's output for the utility vocabulary
    /// Neko emits. Organised family-by-family; <see cref="Resolve"/> tries each
    /// family and returns the first match (null when the token is not a utility).
    /// </summary>
    internal sealed class UtilityRegistry
    {
        private readonly TailwindTheme _t;

        // Shared Tailwind v3 templates.
        private const string Transform =
            "translate(var(--tw-translate-x), var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))";
        private const string FilterChain =
            "var(--tw-blur) var(--tw-brightness) var(--tw-contrast) var(--tw-grayscale) var(--tw-hue-rotate) var(--tw-invert) var(--tw-saturate) var(--tw-sepia) var(--tw-drop-shadow)";
        private const string BackdropChain =
            "var(--tw-backdrop-blur) var(--tw-backdrop-brightness) var(--tw-backdrop-contrast) var(--tw-backdrop-grayscale) var(--tw-backdrop-hue-rotate) var(--tw-backdrop-invert) var(--tw-backdrop-opacity) var(--tw-backdrop-saturate) var(--tw-backdrop-sepia)";
        private const string DivideSpaceSuffix = " > :not([hidden]) ~ :not([hidden])";

        public UtilityRegistry(TailwindTheme theme)
        {
            _t = theme;
        }

        public UtilityResult Resolve(Candidate c)
        {
            var v = c.Core;
            var neg = c.Negative;

            // Arbitrary properties: [property:value] → property: value.
            if (v.Length >= 2 && v[0] == '[' && v[v.Length - 1] == ']')
            {
                var inner = v.Substring(1, v.Length - 2);
                int colon = inner.IndexOf(':');
                if (colon > 0)
                {
                    var prop = Kebab(inner.Substring(0, colon));
                    var val = inner.Substring(colon + 1).Replace('_', ' ').Trim();
                    return UtilityResult.Of((prop, val));
                }
                return null;
            }

            // Exact, value-less utilities first.
            if (!neg && _static.TryGetValue(v, out var stat))
            {
                var r = new UtilityResult();
                var p = new UtilityPart();
                p.Declarations.AddRange(stat);
                r.Parts.Add(p);
                return r;
            }

            // Family dispatch, ordered to avoid prefix collisions.
            return TrySpacing(v, neg)
                ?? TrySizing(v)
                ?? TryInset(v, neg)
                ?? TryColorFamilies(v)
                ?? TryGradient(v)
                ?? TryBorderRadius(v)
                ?? TryBorderWidth(v)
                ?? TryRing(v)
                ?? TryShadow(v)
                ?? TryFlexGrid(v)
                ?? TryGap(v)
                ?? TryTypography(v)
                ?? TryTransform(v, neg)
                ?? TryTransition(v)
                ?? TryFilter(v)
                ?? TryEffects(v, neg)
                ?? TryLayoutValue(v)
                ?? TryAspect(v);
        }

        // ---- helpers -------------------------------------------------------

        private static bool IsArbitrary(string v, out string inner)
        {
            inner = null;
            if (v != null && v.Length >= 2 && v[0] == '[' && v[v.Length - 1] == ']')
            {
                inner = NormalizeArbitrary(v.Substring(1, v.Length - 2));
                return true;
            }
            return false;
        }

        // Underscores become spaces, and Tailwind adds spaces around '+' inside
        // math expressions (e.g. calc(2rem+1px) → calc(2rem + 1px)).
        private static string NormalizeArbitrary(string inner)
        {
            inner = inner.Replace('_', ' ');
            if (inner.Contains("+"))
            {
                var sb = new System.Text.StringBuilder(inner.Length + 4);
                foreach (var ch in inner)
                {
                    if (ch == '+') { TrimEndSpace(sb); sb.Append(" + "); }
                    else sb.Append(ch);
                }
                inner = sb.ToString();
                while (inner.Contains("  ")) inner = inner.Replace("  ", " ");
            }
            return inner;
        }
        private static void TrimEndSpace(System.Text.StringBuilder sb)
        {
            while (sb.Length > 0 && sb[sb.Length - 1] == ' ') sb.Length--;
        }

        // Resolves a length-like value: arbitrary, theme spacing, or null.
        private string Spacing(string v, bool neg)
        {
            string val = null;
            if (IsArbitrary(v, out var inner)) val = inner;
            else if (_t.Spacing.TryGetValue(v, out var s)) val = s;
            if (val == null) return null;
            return neg ? Negate(val) : val;
        }

        // Length value for sizing/inset: spacing + fractions + keywords + arbitrary.
        private string Length(string v, bool neg, bool allowFractions, params (string Key, string Val)[] keywords)
        {
            foreach (var (k, val) in keywords)
                if (v == k) return neg ? Negate(val) : val;

            if (IsArbitrary(v, out var inner))
                return neg ? Negate(inner) : inner;

            if (allowFractions && TryFraction(v, out var pct))
                return neg ? Negate(pct) : pct;

            if (_t.Spacing.TryGetValue(v, out var s))
                return neg ? Negate(s) : s;

            return null;
        }

        private static bool TryFraction(string v, out string pct)
        {
            pct = null;
            int slash = v.IndexOf('/');
            if (slash <= 0 || slash >= v.Length - 1) return false;
            if (!int.TryParse(v.Substring(0, slash), out var num)) return false;
            if (!int.TryParse(v.Substring(slash + 1), out var den) || den == 0) return false;
            double p = Math.Round(num * 100.0 / den, 6);
            pct = p.ToString(CultureInfo.InvariantCulture) + "%";
            return true;
        }

        private static string Negate(string val)
        {
            if (string.IsNullOrEmpty(val) || val == "auto") return val;
            return val.StartsWith("-") ? val.Substring(1) : "-" + val;
        }

        private static string AlphaFromModifier(string mod)
        {
            if (IsArbitrary(mod, out var inner)) return inner;
            if (int.TryParse(mod, out var n))
                return (n / 100.0).ToString(CultureInfo.InvariantCulture);
            return mod;
        }

        private struct ColorValue
        {
            public bool Ok;
            public string Keyword;      // transparent/current/inherit, or null
            public string Hex;          // #rrggbb or #rgb
            public string Channels;     // "r g b"
            public string Alpha;        // resolved alpha, or null
        }

        // Parses "<color>[/<alpha>]" where color is a theme color, keyword, or
        // arbitrary [#hex]/[value].
        private ColorValue ParseColor(string value)
        {
            var cv = new ColorValue();
            string colorPart = value;
            string alphaPart = null;

            int slash = LastTopLevelSlash(value);
            if (slash > 0)
            {
                colorPart = value.Substring(0, slash);
                alphaPart = value.Substring(slash + 1);
            }

            string hex = null;
            if (IsArbitrary(colorPart, out var inner))
            {
                if (inner.StartsWith("#")) hex = inner;
                else if (inner == "transparent" || inner == "currentColor" || inner == "inherit") { cv.Keyword = inner; }
                else hex = null; // unsupported arbitrary color form
                if (hex == null && cv.Keyword == null) return cv;
            }
            else if (colorPart == "transparent") { cv.Keyword = "transparent"; }
            else if (colorPart == "current") { cv.Keyword = "currentColor"; }
            else if (colorPart == "inherit") { cv.Keyword = "inherit"; }
            else
            {
                hex = _t.ResolveColorHex(colorPart);
                if (hex == null) return cv;
                if (hex == "transparent" || hex == "currentColor" || hex == "inherit") { cv.Keyword = hex; hex = null; }
            }

            cv.Hex = hex;
            cv.Channels = hex != null ? TailwindTheme.HexToRgbChannels(hex) : null;
            cv.Alpha = alphaPart != null ? AlphaFromModifier(alphaPart) : null;
            cv.Ok = cv.Keyword != null || cv.Channels != null;
            return cv;
        }

        private static int LastTopLevelSlash(string v)
        {
            int depth = 0;
            for (int i = v.Length - 1; i >= 0; i--)
            {
                char ch = v[i];
                if (ch == ']' || ch == ')') depth++;
                else if (ch == '[' || ch == '(') { if (depth > 0) depth--; }
                else if (ch == '/' && depth == 0) return i;
            }
            return -1;
        }

        // Emits the opacity-dance declarations for a color property.
        private void EmitColor(UtilityPart part, ColorValue cv, string prop, string opacityVar)
        {
            if (cv.Keyword != null)
            {
                part.Declarations.Add((prop, cv.Keyword));
                return;
            }
            if (cv.Alpha != null)
            {
                part.Declarations.Add((prop, $"rgb({cv.Channels} / {cv.Alpha})"));
                return;
            }
            part.Declarations.Add(($"--tw-{opacityVar}-opacity", "1"));
            part.Declarations.Add((prop, $"rgb({cv.Channels} / var(--tw-{opacityVar}-opacity, 1))"));
        }

        // ---- families ------------------------------------------------------

        private UtilityResult TrySpacing(string v, bool neg)
        {
            var map = new (string Prefix, string[] Props)[]
            {
                ("p", new[] { "padding" }),
                ("px", new[] { "padding-left", "padding-right" }),
                ("py", new[] { "padding-top", "padding-bottom" }),
                ("pt", new[] { "padding-top" }),
                ("pr", new[] { "padding-right" }),
                ("pb", new[] { "padding-bottom" }),
                ("pl", new[] { "padding-left" }),
                ("m", new[] { "margin" }),
                ("mx", new[] { "margin-left", "margin-right" }),
                ("my", new[] { "margin-top", "margin-bottom" }),
                ("mt", new[] { "margin-top" }),
                ("mr", new[] { "margin-right" }),
                ("mb", new[] { "margin-bottom" }),
                ("ml", new[] { "margin-left" }),
                ("scroll-mt", new[] { "scroll-margin-top" }),
                ("scroll-mb", new[] { "scroll-margin-bottom" }),
            };
            foreach (var (prefix, props) in map)
            {
                if (!TryValue(v, prefix, out var val)) continue;
                bool isMargin = prefix[0] == 'm' || prefix.StartsWith("scroll");
                string resolved;
                if (isMargin && val == "auto") resolved = "auto";
                else resolved = Spacing(val, neg);
                if (resolved == null) return null;
                var p = new UtilityPart();
                foreach (var prop in props) p.Declarations.Add((prop, resolved));
                return new UtilityResult().Add(p);
            }

            // space-x / space-y (child combinator)
            foreach (var (prefix, axis) in new[] { ("space-x", "x"), ("space-y", "y") })
            {
                if (!TryValue(v, prefix, out var val)) continue;
                if (val == "reverse")
                    return WithSuffix(DivideSpaceSuffix, ($"--tw-space-{axis}-reverse", "1"));
                var resolved = Spacing(val, neg);
                if (resolved == null) return null;
                var p = new UtilityPart { SelectorSuffix = DivideSpaceSuffix };
                p.Declarations.Add(($"--tw-space-{axis}-reverse", "0"));
                if (axis == "x")
                {
                    p.Declarations.Add(("margin-right", $"calc({resolved} * var(--tw-space-x-reverse))"));
                    p.Declarations.Add(("margin-left", $"calc({resolved} * calc(1 - var(--tw-space-x-reverse)))"));
                }
                else
                {
                    p.Declarations.Add(("margin-top", $"calc({resolved} * calc(1 - var(--tw-space-y-reverse)))"));
                    p.Declarations.Add(("margin-bottom", $"calc({resolved} * var(--tw-space-y-reverse))"));
                }
                return new UtilityResult().Add(p);
            }
            return null;
        }

        private UtilityResult TrySizing(string v)
        {
            var widthKw = new[] { ("full", "100%"), ("screen", "100vw"), ("auto", "auto"), ("min", "min-content"), ("max", "max-content"), ("fit", "fit-content"), ("px", "1px") };
            var heightKw = new[] { ("full", "100%"), ("screen", "100vh"), ("auto", "auto"), ("min", "min-content"), ("max", "max-content"), ("fit", "fit-content"), ("px", "1px") };

            if (TryValue(v, "w", out var wv))
            {
                var val = Length(wv, false, true, widthKw);
                return val == null ? null : SizeResult("width", val);
            }
            if (TryValue(v, "h", out var hv))
            {
                var val = Length(hv, false, true, heightKw);
                return val == null ? null : SizeResult("height", val);
            }
            if (TryValue(v, "min-w", out var mwv))
            {
                var val = MaxMinValue(mwv, widthKw);
                return val == null ? null : SizeResult("min-width", val);
            }
            if (TryValue(v, "min-h", out var mhv))
            {
                var val = Length(mhv, false, false, new[] { ("full", "100%"), ("screen", "100vh"), ("min", "min-content"), ("max", "max-content"), ("fit", "fit-content"), ("0", "0px") });
                return val == null ? null : UtilityResult.Of(("min-height", val));
            }
            if (TryValue(v, "max-w", out var xwv))
            {
                if (IsArbitrary(xwv, out var inner)) return UtilityResult.Of(("max-width", inner));
                if (_t.MaxWidth.TryGetValue(xwv, out var mw)) return UtilityResult.Of(("max-width", mw));
                return null;
            }
            if (TryValue(v, "max-h", out var xhv))
            {
                var val = Length(xhv, false, false, new[] { ("full", "100%"), ("screen", "100vh"), ("min", "min-content"), ("max", "max-content"), ("fit", "fit-content"), ("none", "none"), ("px", "1px") });
                return val == null ? null : UtilityResult.Of(("max-height", val));
            }
            return null;
        }

        private string MaxMinValue(string v, (string, string)[] kw)
        {
            return Length(v, false, false, kw);
        }

        // Emits a sizing declaration, expanding the intrinsic-size keywords to the
        // vendor-prefixed pairs Tailwind produces (e.g. -moz-max-content).
        private static UtilityResult SizeResult(string prop, string val)
        {
            switch (val)
            {
                case "max-content": return UtilityResult.Of((prop, "-moz-max-content"), (prop, "max-content"));
                case "min-content": return UtilityResult.Of((prop, "-webkit-min-content"), (prop, "-moz-min-content"), (prop, "min-content"));
                case "fit-content": return UtilityResult.Of((prop, "-moz-fit-content"), (prop, "fit-content"));
                default: return UtilityResult.Of((prop, val));
            }
        }

        private UtilityResult TryInset(string v, bool neg)
        {
            var insetKw = new[] { ("full", "100%"), ("auto", "auto"), ("px", "1px") };
            var map = new (string Prefix, string[] Props)[]
            {
                ("inset-x", new[] { "left", "right" }),
                ("inset-y", new[] { "top", "bottom" }),
                ("inset", new[] { "inset" }),
                ("top", new[] { "top" }),
                ("right", new[] { "right" }),
                ("bottom", new[] { "bottom" }),
                ("left", new[] { "left" }),
            };
            foreach (var (prefix, props) in map)
            {
                if (!TryValue(v, prefix, out var val)) continue;
                var resolved = Length(val, neg, true, insetKw);
                if (resolved == null) return null;
                var p = new UtilityPart();
                foreach (var prop in props) p.Declarations.Add((prop, resolved));
                return new UtilityResult().Add(p);
            }
            return null;
        }

        private UtilityResult TryColorFamilies(string v)
        {
            // bg- and text- also carry non-color utilities (bg-cover, text-center,
            // …); those are filtered out here and resolved by later families.
            if (TryValue(v, "bg-opacity", out var bgo))
            {
                return UtilityResult.Of(("--tw-bg-opacity", AlphaFromModifier(bgo)));
            }
            if (TryValue(v, "bg", out var bgv))
            {
                if (bgv == "cover" || bgv == "contain" || bgv == "auto") return UtilityResult.Of(("background-size", bgv));
                if (bgv == "center" || bgv == "top" || bgv == "bottom" || bgv == "left" || bgv == "right") return UtilityResult.Of(("background-position", bgv));
                if (bgv == "no-repeat") return UtilityResult.Of(("background-repeat", "no-repeat"));
                if (bgv == "fixed" || bgv == "local" || bgv == "scroll") return UtilityResult.Of(("background-attachment", bgv));
                if (bgv == "clip-text") return UtilityResult.Of(("-webkit-background-clip", "text"), ("background-clip", "text"));
                if (bgv.StartsWith("gradient-to-")) return null; // handled in TryGradient
                var cv = ParseColor(bgv);
                if (cv.Ok)
                {
                    var p = new UtilityPart();
                    EmitColor(p, cv, "background-color", "bg");
                    return new UtilityResult().Add(p);
                }
                return null;
            }

            if (TryValue(v, "text", out var tv))
            {
                // text-align / wrap handled in typography; here only colors.
                if (tv == "left" || tv == "center" || tv == "right" || tv == "justify" || tv == "start" || tv == "end") return null;
                if (tv == "wrap" || tv == "nowrap" || tv == "balance" || tv == "pretty") return null;
                if (IsFontSizeKey(tv)) return null;
                if (IsArbitrary(tv, out var arb) && IsLengthLike(arb)) return null; // arbitrary font-size
                var cv = ParseColor(tv);
                if (cv.Ok)
                {
                    var p = new UtilityPart();
                    EmitColor(p, cv, "color", "text");
                    return new UtilityResult().Add(p);
                }
                return null;
            }

            if (TryValue(v, "border", out var bv))
            {
                // styles / collapse / widths handled elsewhere; only colors here.
                var cv = ParseColor(bv);
                if (cv.Ok)
                {
                    var p = new UtilityPart();
                    EmitColor(p, cv, "border-color", "border");
                    return new UtilityResult().Add(p);
                }
                return null;
            }

            if (TryValue(v, "divide", out var dv))
            {
                // Widths (divide-x / divide-y[-N] / divide-{reverse}) first.
                if (dv == "x" || dv == "y" || dv.StartsWith("x-") || dv.StartsWith("y-"))
                    return TryDivideWidth(dv);
                var cv = ParseColor(dv);
                if (cv.Ok)
                {
                    var p = new UtilityPart { SelectorSuffix = DivideSpaceSuffix };
                    EmitColor(p, cv, "border-color", "divide");
                    return new UtilityResult().Add(p);
                }
                return null;
            }

            if (TryValue(v, "ring-offset", out var rov))
            {
                if (_t.BorderWidth.TryGetValue(rov, out _) || int.TryParse(rov, out _) || IsArbitrary(rov, out _))
                {
                    var w = IsArbitrary(rov, out var ai) ? ai : (rov + "px");
                    return UtilityResult.Of(("--tw-ring-offset-width", w));
                }
                var cv = ParseColor(rov);
                if (cv.Ok)
                {
                    var val = cv.Keyword ?? cv.Hex;
                    return UtilityResult.Of(("--tw-ring-offset-color", val));
                }
                return null;
            }

            if (TryValue(v, "ring", out var rv))
            {
                // ring widths handled in TryRing; here only ring colors.
                var cv = ParseColor(rv);
                if (cv.Ok)
                {
                    var p = new UtilityPart();
                    EmitColor(p, cv, "--tw-ring-color", "ring");
                    return new UtilityResult().Add(p);
                }
                return null;
            }

            if (TryValue(v, "placeholder", out var pv))
            {
                var cv = ParseColor(pv);
                if (!cv.Ok) return null;
                var r = new UtilityResult();
                foreach (var pseudo in new[] { "::-moz-placeholder", "::placeholder" })
                {
                    var p = new UtilityPart { SelectorSuffix = pseudo };
                    EmitColor(p, cv, "color", "placeholder");
                    r.Parts.Add(p);
                }
                return r;
            }

            if (TryValue(v, "outline", out var ov))
            {
                var cv = ParseColor(ov);
                if (cv.Ok)
                {
                    var val = cv.Keyword ?? cv.Hex;
                    return UtilityResult.Of(("outline-color", val));
                }
                return null;
            }

            if (TryValue(v, "accent", out var av))
            {
                if (av == "auto") return UtilityResult.Of(("accent-color", "auto"));
                var cv = ParseColor(av);
                if (cv.Ok) { var p = new UtilityPart(); EmitColor(p, cv, "accent-color", "accent"); return new UtilityResult().Add(p); }
                return null;
            }

            if (TryValue(v, "caret", out var cv2))
            {
                var c = ParseColor(cv2);
                if (c.Ok) { var p = new UtilityPart(); EmitColor(p, c, "caret-color", "caret"); return new UtilityResult().Add(p); }
                return null;
            }

            if (TryValue(v, "fill", out var fv))
            {
                var c = ParseColor(fv);
                if (c.Keyword != null) return UtilityResult.Of(("fill", c.Keyword));
                if (c.Hex != null) return UtilityResult.Of(("fill", c.Hex));
                return null;
            }
            if (TryValue(v, "stroke", out var sv))
            {
                var c = ParseColor(sv);
                if (c.Keyword != null) return UtilityResult.Of(("stroke", c.Keyword));
                if (c.Hex != null) return UtilityResult.Of(("stroke", c.Hex));
                if (int.TryParse(sv, out _) || IsArbitrary(sv, out _)) return UtilityResult.Of(("stroke-width", IsArbitrary(sv, out var si) ? si : sv));
                return null;
            }

            return null;
        }

        private UtilityResult TryDivideWidth(string v)
        {
            string axis = null;
            if (v == "x" || v.StartsWith("x-")) axis = "x";
            else if (v == "y" || v.StartsWith("y-")) axis = "y";
            if (axis == null) return null;
            var rest = v.Length > 1 ? v.Substring(2) : "";
            if (rest == "reverse")
                return WithSuffix(DivideSpaceSuffix, ($"--tw-divide-{axis}-reverse", "1"));
            string w = rest == "" ? "1px" : (_t.BorderWidth.TryGetValue(rest, out var bw) ? bw : (IsArbitrary(rest, out var ai) ? ai : null));
            if (w == null) return null;
            var p = new UtilityPart { SelectorSuffix = DivideSpaceSuffix };
            p.Declarations.Add(($"--tw-divide-{axis}-reverse", "0"));
            if (axis == "x")
            {
                p.Declarations.Add(("border-right-width", $"calc({w} * var(--tw-divide-x-reverse))"));
                p.Declarations.Add(("border-left-width", $"calc({w} * calc(1 - var(--tw-divide-x-reverse)))"));
            }
            else
            {
                p.Declarations.Add(("border-top-width", $"calc({w} * calc(1 - var(--tw-divide-y-reverse)))"));
                p.Declarations.Add(("border-bottom-width", $"calc({w} * var(--tw-divide-y-reverse))"));
            }
            return new UtilityResult().Add(p);
        }

        private UtilityResult TryGradient(string v)
        {
            string dir = null;
            if (TryValue(v, "bg-gradient-to", out var d))
            {
                dir = d switch
                {
                    "t" => "to top", "tr" => "to top right", "r" => "to right", "br" => "to bottom right",
                    "b" => "to bottom", "bl" => "to bottom left", "l" => "to left", "tl" => "to top left",
                    _ => null
                };
                if (dir == null) return null;
                return UtilityResult.Of(("background-image", $"linear-gradient({dir}, var(--tw-gradient-stops))"));
            }

            if (TryValue(v, "from", out var fv))
            {
                var cv = ParseColor(fv);
                if (!cv.Ok || cv.Hex == null) return null;
                return UtilityResult.Of(
                    ("--tw-gradient-from", $"{cv.Hex} var(--tw-gradient-from-position)"),
                    ("--tw-gradient-to", $"rgb({cv.Channels} / 0) var(--tw-gradient-to-position)"),
                    ("--tw-gradient-stops", "var(--tw-gradient-from), var(--tw-gradient-to)"));
            }
            if (TryValue(v, "via", out var vv))
            {
                var cv = ParseColor(vv);
                if (!cv.Ok || cv.Hex == null) return null;
                return UtilityResult.Of(
                    ("--tw-gradient-to", $"rgb({cv.Channels} / 0) var(--tw-gradient-to-position)"),
                    ("--tw-gradient-stops", $"var(--tw-gradient-from), {cv.Hex} var(--tw-gradient-via-position), var(--tw-gradient-to)"));
            }
            if (TryValue(v, "to", out var tv))
            {
                var cv = ParseColor(tv);
                if (!cv.Ok || cv.Hex == null) return null;
                return UtilityResult.Of(("--tw-gradient-to", $"{cv.Hex} var(--tw-gradient-to-position)"));
            }
            return null;
        }

        private UtilityResult TryBorderRadius(string v)
        {
            var map = new (string Prefix, string[] Props)[]
            {
                ("rounded-tl", new[] { "border-top-left-radius" }),
                ("rounded-tr", new[] { "border-top-right-radius" }),
                ("rounded-br", new[] { "border-bottom-right-radius" }),
                ("rounded-bl", new[] { "border-bottom-left-radius" }),
                ("rounded-t", new[] { "border-top-left-radius", "border-top-right-radius" }),
                ("rounded-r", new[] { "border-top-right-radius", "border-bottom-right-radius" }),
                ("rounded-b", new[] { "border-bottom-right-radius", "border-bottom-left-radius" }),
                ("rounded-l", new[] { "border-top-left-radius", "border-bottom-left-radius" }),
                ("rounded", new[] { "border-radius" }),
            };
            foreach (var (prefix, props) in map)
            {
                if (v != prefix && !v.StartsWith(prefix + "-")) continue;
                var key = v == prefix ? "" : v.Substring(prefix.Length + 1);
                string val;
                if (IsArbitrary(key, out var inner)) val = ResolveThemeFn(inner);
                else if (!_t.BorderRadius.TryGetValue(key, out val)) return null;
                var p = new UtilityPart();
                foreach (var prop in props) p.Declarations.Add((prop, val));
                return new UtilityResult().Add(p);
            }
            return null;
        }

        private UtilityResult TryBorderWidth(string v)
        {
            if (v == "border-collapse") return UtilityResult.Of(("border-collapse", "collapse"));
            if (v == "border-separate") return UtilityResult.Of(("border-collapse", "separate"));
            foreach (var style in new[] { "solid", "dashed", "dotted", "double", "hidden", "none" })
                if (v == "border-" + style) return UtilityResult.Of(("border-style", style));

            var map = new (string Prefix, string Prop)[]
            {
                ("border-x", null), ("border-y", null),
                ("border-t", "border-top-width"), ("border-r", "border-right-width"),
                ("border-b", "border-bottom-width"), ("border-l", "border-left-width"),
                ("border", "border-width"),
            };
            foreach (var (prefix, prop) in map)
            {
                if (v != prefix && !v.StartsWith(prefix + "-")) continue;
                var key = v == prefix ? "" : v.Substring(prefix.Length + 1);
                string w;
                if (IsArbitrary(key, out var inner)) w = inner;
                else if (!_t.BorderWidth.TryGetValue(key, out w)) return null;
                var p = new UtilityPart();
                if (prefix == "border-x") { p.Declarations.Add(("border-left-width", w)); p.Declarations.Add(("border-right-width", w)); }
                else if (prefix == "border-y") { p.Declarations.Add(("border-top-width", w)); p.Declarations.Add(("border-bottom-width", w)); }
                else p.Declarations.Add((prop, w));
                return new UtilityResult().Add(p);
            }
            return null;
        }

        private UtilityResult TryRing(string v)
        {
            if (v == "ring-inset") return UtilityResult.Of(("--tw-ring-inset", "inset"));
            if (v == "ring" || (v.StartsWith("ring-") && IsRingWidth(v.Substring(5), out _)))
            {
                string w = v == "ring" ? "3px" : RingWidth(v.Substring(5));
                if (w == null) return null;
                return UtilityResult.Of(
                    ("--tw-ring-offset-shadow", "var(--tw-ring-inset) 0 0 0 var(--tw-ring-offset-width) var(--tw-ring-offset-color)"),
                    ("--tw-ring-shadow", $"var(--tw-ring-inset) 0 0 0 calc({w} + var(--tw-ring-offset-width)) var(--tw-ring-color)"),
                    ("box-shadow", "var(--tw-ring-offset-shadow), var(--tw-ring-shadow), var(--tw-shadow, 0 0 #0000)"));
            }
            return null;
        }

        private static bool IsRingWidth(string s, out string w)
        {
            w = null;
            if (s == "0" || s == "1" || s == "2" || s == "4" || s == "8") { w = s + "px"; return true; }
            if (s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']') { w = s.Substring(1, s.Length - 2); return true; }
            return false;
        }
        private static string RingWidth(string s) { IsRingWidth(s, out var w); return w; }

        private UtilityResult TryShadow(string v)
        {
            if (v != "shadow" && !v.StartsWith("shadow-")) return null;
            var key = v == "shadow" ? "" : v.Substring("shadow-".Length);
            if (IsArbitrary(key, out var arb))
            {
                // Arbitrary box-shadow: --tw-shadow uses the literal value, the
                // colored variant swaps the trailing color for var(--tw-shadow-color).
                var colored = ColorizeShadow(arb);
                return UtilityResult.Of(
                    ("--tw-shadow", arb),
                    ("--tw-shadow-colored", colored),
                    ("box-shadow", "var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)"));
            }
            if (_shadows.TryGetValue(key, out var sh))
            {
                if (key == "none")
                    return UtilityResult.Of(("box-shadow", "0 0 #0000"));
                return UtilityResult.Of(
                    ("--tw-shadow", sh.Shadow),
                    ("--tw-shadow-colored", sh.Colored),
                    ("box-shadow", "var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)"));
            }
            return null;
        }

        private UtilityResult TryFlexGrid(string v)
        {
            switch (v)
            {
                case "flex-row": return UtilityResult.Of(("flex-direction", "row"));
                case "flex-row-reverse": return UtilityResult.Of(("flex-direction", "row-reverse"));
                case "flex-col": return UtilityResult.Of(("flex-direction", "column"));
                case "flex-col-reverse": return UtilityResult.Of(("flex-direction", "column-reverse"));
                case "flex-wrap": return UtilityResult.Of(("flex-wrap", "wrap"));
                case "flex-wrap-reverse": return UtilityResult.Of(("flex-wrap", "wrap-reverse"));
                case "flex-nowrap": return UtilityResult.Of(("flex-wrap", "nowrap"));
                case "flex-1": return UtilityResult.Of(("flex", "1 1 0%"));
                case "flex-auto": return UtilityResult.Of(("flex", "1 1 auto"));
                case "flex-initial": return UtilityResult.Of(("flex", "0 1 auto"));
                case "flex-none": return UtilityResult.Of(("flex", "none"));
                case "flex-grow": case "grow": return UtilityResult.Of(("flex-grow", "1"));
                case "flex-grow-0": case "grow-0": return UtilityResult.Of(("flex-grow", "0"));
                case "flex-shrink": case "shrink": return UtilityResult.Of(("flex-shrink", "1"));
                case "flex-shrink-0": case "shrink-0": return UtilityResult.Of(("flex-shrink", "0"));
            }
            if (TryValue(v, "grid-cols", out var gc))
            {
                if (gc == "none") return UtilityResult.Of(("grid-template-columns", "none"));
                if (IsArbitrary(gc, out var inner)) return UtilityResult.Of(("grid-template-columns", inner));
                if (int.TryParse(gc, out var n)) return UtilityResult.Of(("grid-template-columns", $"repeat({n}, minmax(0, 1fr))"));
                return null;
            }
            if (TryValue(v, "grid-rows", out var gr))
            {
                if (gr == "none") return UtilityResult.Of(("grid-template-rows", "none"));
                if (IsArbitrary(gr, out var inner)) return UtilityResult.Of(("grid-template-rows", inner));
                if (int.TryParse(gr, out var n)) return UtilityResult.Of(("grid-template-rows", $"repeat({n}, minmax(0, 1fr))"));
                return null;
            }
            if (TryValue(v, "col-span", out var cs))
            {
                if (cs == "full") return UtilityResult.Of(("grid-column", "1 / -1"));
                if (int.TryParse(cs, out var n)) return UtilityResult.Of(("grid-column", $"span {n} / span {n}"));
                return null;
            }
            if (TryValue(v, "row-span", out var rs))
            {
                if (rs == "full") return UtilityResult.Of(("grid-row", "1 / -1"));
                if (int.TryParse(rs, out var n)) return UtilityResult.Of(("grid-row", $"span {n} / span {n}"));
                return null;
            }
            if (TryValue(v, "col-start", out var cst) && int.TryParse(cst, out var csn)) return UtilityResult.Of(("grid-column-start", csn.ToString()));
            if (TryValue(v, "col-end", out var cse) && int.TryParse(cse, out var cen)) return UtilityResult.Of(("grid-column-end", cen.ToString()));
            if (TryValue(v, "order", out var ov))
            {
                if (ov == "first") return UtilityResult.Of(("order", "-9999"));
                if (ov == "last") return UtilityResult.Of(("order", "9999"));
                if (ov == "none") return UtilityResult.Of(("order", "0"));
                if (int.TryParse(ov, out var n)) return UtilityResult.Of(("order", n.ToString()));
                return null;
            }
            if (TryValue(v, "columns", out var cols))
            {
                if (int.TryParse(cols, out var n)) return UtilityResult.Of(("-moz-columns", n.ToString()), ("columns", n.ToString()));
                return null;
            }
            if (TryValue(v, "basis", out var basis))
            {
                var val = Length(basis, false, true, new[] { ("full", "100%"), ("auto", "auto"), ("px", "1px") });
                return val == null ? null : UtilityResult.Of(("flex-basis", val));
            }
            return null;
        }

        private UtilityResult TryGap(string v)
        {
            if (TryValue(v, "gap-x", out var gx)) { var val = Spacing(gx, false); return val == null ? null : UtilityResult.Of(("-moz-column-gap", val), ("column-gap", val)); }
            if (TryValue(v, "gap-y", out var gy)) { var val = Spacing(gy, false); return val == null ? null : UtilityResult.Of(("row-gap", val)); }
            if (TryValue(v, "gap", out var g)) { var val = Spacing(g, false); return val == null ? null : UtilityResult.Of(("gap", val)); }
            return null;
        }

        private UtilityResult TryTypography(string v)
        {
            // text alignment / wrap / size
            if (TryValue(v, "text", out var tv))
            {
                switch (tv)
                {
                    case "left": case "center": case "right": case "justify": case "start": case "end":
                        return UtilityResult.Of(("text-align", tv));
                    case "wrap": return UtilityResult.Of(("text-wrap", "wrap"));
                    case "nowrap": return UtilityResult.Of(("text-wrap", "nowrap"));
                    case "balance": return UtilityResult.Of(("text-wrap", "balance"));
                    case "pretty": return UtilityResult.Of(("text-wrap", "pretty"));
                }
                // font-size, optionally size/lineheight
                string sizeKey = tv, lhKey = null;
                int slash = tv.IndexOf('/');
                if (slash > 0) { sizeKey = tv.Substring(0, slash); lhKey = tv.Substring(slash + 1); }
                if (IsArbitrary(sizeKey, out var arb) && IsLengthLike(arb))
                {
                    var p0 = new UtilityPart(); p0.Declarations.Add(("font-size", arb));
                    if (lhKey != null) p0.Declarations.Add(("line-height", ResolveLineHeight(lhKey)));
                    return new UtilityResult().Add(p0);
                }
                if (_t.FontSize.TryGetValue(sizeKey, out var fs))
                {
                    var p = new UtilityPart();
                    p.Declarations.Add(("font-size", fs.Size));
                    p.Declarations.Add(("line-height", lhKey != null ? ResolveLineHeight(lhKey) : fs.LineHeight));
                    return new UtilityResult().Add(p);
                }
                return null;
            }
            if (TryValue(v, "font", out var fv))
            {
                if (_t.FontWeight.TryGetValue(fv, out var w)) return UtilityResult.Of(("font-weight", w));
                if (fv == "sans") return UtilityResult.Of(("font-family", "ui-sans-serif, system-ui, sans-serif, \"Apple Color Emoji\", \"Segoe UI Emoji\", \"Segoe UI Symbol\", \"Noto Color Emoji\""));
                if (fv == "serif") return UtilityResult.Of(("font-family", "ui-serif, Georgia, Cambria, \"Times New Roman\", Times, serif"));
                if (fv == "mono") return UtilityResult.Of(("font-family", "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace"));
                return null;
            }
            if (TryValue(v, "leading", out var lv))
                return UtilityResult.Of(("line-height", ResolveLineHeight(lv)));
            if (TryValue(v, "tracking", out var trv))
            {
                if (IsArbitrary(trv, out var inner)) return UtilityResult.Of(("letter-spacing", inner));
                if (_t.LetterSpacing.TryGetValue(trv, out var ls)) return UtilityResult.Of(("letter-spacing", ls));
                return null;
            }
            switch (v)
            {
                case "italic": return UtilityResult.Of(("font-style", "italic"));
                case "not-italic": return UtilityResult.Of(("font-style", "normal"));
                case "uppercase": return UtilityResult.Of(("text-transform", "uppercase"));
                case "lowercase": return UtilityResult.Of(("text-transform", "lowercase"));
                case "capitalize": return UtilityResult.Of(("text-transform", "capitalize"));
                case "normal-case": return UtilityResult.Of(("text-transform", "none"));
                case "underline": return UtilityResult.Of(("text-decoration-line", "underline"));
                case "overline": return UtilityResult.Of(("text-decoration-line", "overline"));
                case "line-through": return UtilityResult.Of(("text-decoration-line", "line-through"));
                case "no-underline": return UtilityResult.Of(("text-decoration-line", "none"));
                case "truncate": return UtilityResult.Of(("overflow", "hidden"), ("text-overflow", "ellipsis"), ("white-space", "nowrap"));
                case "text-ellipsis": return UtilityResult.Of(("text-overflow", "ellipsis"));
                case "list-none": return UtilityResult.Of(("list-style-type", "none"));
                case "list-disc": return UtilityResult.Of(("list-style-type", "disc"));
                case "list-decimal": return UtilityResult.Of(("list-style-type", "decimal"));
                case "tabular-nums": return UtilityResult.Of(("--tw-numeric-spacing", "tabular-nums"), ("font-variant-numeric", "var(--tw-ordinal) var(--tw-slashed-zero) var(--tw-numeric-figure) var(--tw-numeric-spacing) var(--tw-numeric-fraction)"));
                case "align-middle": return UtilityResult.Of(("vertical-align", "middle"));
                case "align-top": return UtilityResult.Of(("vertical-align", "top"));
                case "align-bottom": return UtilityResult.Of(("vertical-align", "bottom"));
                case "align-baseline": return UtilityResult.Of(("vertical-align", "baseline"));
            }
            if (TryValue(v, "line-clamp", out var lc) && int.TryParse(lc, out var lcn))
                return UtilityResult.Of(("overflow", "hidden"), ("display", "-webkit-box"), ("-webkit-box-orient", "vertical"), ("-webkit-line-clamp", lcn.ToString()));
            if (TryValue(v, "whitespace", out var ws)) return UtilityResult.Of(("white-space", ws));
            return null;
        }

        private string ResolveLineHeight(string key)
        {
            if (IsArbitrary(key, out var inner)) return inner;
            if (_t.LineHeight.TryGetValue(key, out var lh)) return lh;
            return key;
        }

        private UtilityResult TryTransform(string v, bool neg)
        {
            if (v == "transform") return UtilityResult.Of(("transform", Transform));
            if (v == "transform-none") return UtilityResult.Of(("transform", "none"));
            if (v == "transform-gpu") return UtilityResult.Of(("transform", "translate3d(var(--tw-translate-x), var(--tw-translate-y), 0) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))"));

            foreach (var (prefix, varName) in new[] { ("translate-x", "--tw-translate-x"), ("translate-y", "--tw-translate-y") })
            {
                if (!TryValue(v, prefix, out var val)) continue;
                var resolved = Length(val, neg, true, new[] { ("full", "100%"), ("px", "1px") });
                if (resolved == null) return null;
                return UtilityResult.Of((varName, resolved), ("transform", Transform));
            }
            if (TryValue(v, "scale-x", out var sx)) return ScaleVal(sx, neg, "--tw-scale-x");
            if (TryValue(v, "scale-y", out var sy)) return ScaleVal(sy, neg, "--tw-scale-y");
            if (TryValue(v, "scale", out var sc))
            {
                var val = ScaleNumber(sc, neg);
                if (val == null) return null;
                return UtilityResult.Of(("--tw-scale-x", val), ("--tw-scale-y", val), ("transform", Transform));
            }
            if (TryValue(v, "rotate", out var rot))
            {
                var val = IsArbitrary(rot, out var ri) ? ri : rot + "deg";
                if (neg) val = "-" + val;
                return UtilityResult.Of(("--tw-rotate", val), ("transform", Transform));
            }
            if (TryValue(v, "skew-x", out var skx)) { var val = (neg ? "-" : "") + (IsArbitrary(skx, out var i) ? i : skx + "deg"); return UtilityResult.Of(("--tw-skew-x", val), ("transform", Transform)); }
            if (TryValue(v, "skew-y", out var sky)) { var val = (neg ? "-" : "") + (IsArbitrary(sky, out var i) ? i : sky + "deg"); return UtilityResult.Of(("--tw-skew-y", val), ("transform", Transform)); }

            if (TryValue(v, "origin", out var org))
            {
                var val = org.Replace("-", " ");
                if (IsArbitrary(org, out var oi)) val = oi;
                return UtilityResult.Of(("transform-origin", val));
            }
            return null;
        }

        private UtilityResult ScaleVal(string s, bool neg, string varName)
        {
            var val = ScaleNumber(s, neg);
            if (val == null) return null;
            return UtilityResult.Of((varName, val), ("transform", Transform));
        }
        private static string ScaleNumber(string s, bool neg)
        {
            string val;
            if (s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']') val = s.Substring(1, s.Length - 2);
            else if (int.TryParse(s, out var n)) val = (n / 100.0).ToString(CultureInfo.InvariantCulture);
            else return null;
            return neg ? "-" + val : val;
        }

        private UtilityResult TryTransition(string v)
        {
            if (v == "transition")
            {
                var p = new UtilityPart();
                p.Declarations.Add(("transition-property", "color, background-color, border-color, text-decoration-color, fill, stroke, opacity, box-shadow, transform, filter, -webkit-backdrop-filter"));
                p.Declarations.Add(("transition-property", "color, background-color, border-color, text-decoration-color, fill, stroke, opacity, box-shadow, transform, filter, backdrop-filter"));
                p.Declarations.Add(("transition-property", "color, background-color, border-color, text-decoration-color, fill, stroke, opacity, box-shadow, transform, filter, backdrop-filter, -webkit-backdrop-filter"));
                p.Declarations.Add(("transition-timing-function", "cubic-bezier(0.4, 0, 0.2, 1)"));
                p.Declarations.Add(("transition-duration", "150ms"));
                return new UtilityResult().Add(p);
            }
            if (v == "transition-none") return UtilityResult.Of(("transition-property", "none"));
            if (v == "transition-all") return Trans("all");
            if (v == "transition-colors") return Trans("color, background-color, border-color, text-decoration-color, fill, stroke");
            if (v == "transition-opacity") return Trans("opacity");
            if (v == "transition-shadow") return Trans("box-shadow");
            if (v == "transition-transform") return Trans("transform");
            if (TryValue(v, "transition", out var tp) && IsArbitrary(tp, out var inner)) return Trans(inner);

            if (TryValue(v, "duration", out var dv))
            {
                if (IsArbitrary(dv, out var di)) return UtilityResult.Of(("transition-duration", di));
                if (_t.Duration.TryGetValue(dv, out var d)) return UtilityResult.Of(("transition-duration", d));
                return null;
            }
            if (TryValue(v, "delay", out var dlv))
            {
                if (IsArbitrary(dlv, out var di)) return UtilityResult.Of(("transition-delay", di));
                if (_t.Duration.TryGetValue(dlv, out var d)) return UtilityResult.Of(("transition-delay", d));
                return null;
            }
            switch (v)
            {
                case "ease-linear": return UtilityResult.Of(("transition-timing-function", "linear"));
                case "ease-in": return UtilityResult.Of(("transition-timing-function", "cubic-bezier(0.4, 0, 1, 1)"));
                case "ease-out": return UtilityResult.Of(("transition-timing-function", "cubic-bezier(0, 0, 0.2, 1)"));
                case "ease-in-out": return UtilityResult.Of(("transition-timing-function", "cubic-bezier(0.4, 0, 0.2, 1)"));
            }
            if (TryValue(v, "animate", out var av))
            {
                if (av == "spin") return UtilityResult.Of(("animation", "spin 1s linear infinite"));
                if (av == "ping") return UtilityResult.Of(("animation", "ping 1s cubic-bezier(0, 0, 0.2, 1) infinite"));
                if (av == "pulse") return UtilityResult.Of(("animation", "pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite"));
                if (av == "bounce") return UtilityResult.Of(("animation", "bounce 1s infinite"));
                if (av == "none") return UtilityResult.Of(("animation", "none"));
                return null;
            }
            return null;
        }
        private static UtilityResult Trans(string props) => UtilityResult.Of(
            ("transition-property", props),
            ("transition-timing-function", "cubic-bezier(0.4, 0, 0.2, 1)"),
            ("transition-duration", "150ms"));

        private UtilityResult TryFilter(string v)
        {
            if (v == "filter") return UtilityResult.Of(("filter", FilterChain));
            if (v == "filter-none") return UtilityResult.Of(("filter", "none"));
            if (v == "backdrop-filter") return UtilityResult.Of(("-webkit-backdrop-filter", BackdropChain), ("backdrop-filter", BackdropChain));
            if (v == "backdrop-filter-none") return UtilityResult.Of(("-webkit-backdrop-filter", "none"), ("backdrop-filter", "none"));

            if (v == "blur" || v.StartsWith("blur-"))
            {
                var key = v == "blur" ? "" : v.Substring(5);
                string b;
                if (IsArbitrary(key, out var bi)) b = bi;
                else if (!_t.Blur.TryGetValue(key, out b)) return null;
                var val = key == "none" ? "" : $"blur({b})";
                return UtilityResult.Of(("--tw-blur", key == "none" ? " " : val), ("filter", FilterChain));
            }
            if (v == "backdrop-blur" || v.StartsWith("backdrop-blur-"))
            {
                var key = v == "backdrop-blur" ? "" : v.Substring("backdrop-blur-".Length);
                string b;
                if (IsArbitrary(key, out var bi)) b = bi;
                else if (!_t.Blur.TryGetValue(key, out b)) return null;
                return UtilityResult.Of(("--tw-backdrop-blur", $"blur({b})"), ("-webkit-backdrop-filter", BackdropChain), ("backdrop-filter", BackdropChain));
            }
            return null;
        }

        private UtilityResult TryEffects(string v, bool neg)
        {
            if (TryValue(v, "opacity", out var ov))
            {
                if (IsArbitrary(ov, out var oi)) return UtilityResult.Of(("opacity", oi));
                if (_t.Opacity.TryGetValue(ov, out var o)) return UtilityResult.Of(("opacity", o));
                return null;
            }
            if (TryValue(v, "z", out var zv))
            {
                if (IsArbitrary(zv, out var zi)) return UtilityResult.Of(("z-index", neg ? "-" + zi : zi));
                if (_t.ZIndex.TryGetValue(zv, out var z)) return UtilityResult.Of(("z-index", neg && z != "auto" ? "-" + z : z));
                return null;
            }
            if (v == "outline") return UtilityResult.Of(("outline-style", "solid"));
            if (v == "outline-none") return UtilityResult.Of(("outline", "2px solid transparent"), ("outline-offset", "2px"));
            if (v == "outline-dashed") return UtilityResult.Of(("outline-style", "dashed"));
            if (TryValue(v, "outline-offset", out var oof))
            {
                var val = IsArbitrary(oof, out var oi2) ? oi2 : oof + "px";
                return UtilityResult.Of(("outline-offset", val));
            }
            if (TryValue(v, "outline", out var owv) && (int.TryParse(owv, out _) || IsArbitrary(owv, out _)))
            {
                var val = IsArbitrary(owv, out var oi3) ? oi3 : owv + "px";
                return UtilityResult.Of(("outline-width", val));
            }
            return null;
        }

        private UtilityResult TryLayoutValue(string v)
        {
            if (TryValue(v, "overflow-x", out var oxv)) return UtilityResult.Of(("overflow-x", oxv));
            if (TryValue(v, "overflow-y", out var oyv)) return UtilityResult.Of(("overflow-y", oyv));
            if (TryValue(v, "overflow", out var ovv)) return UtilityResult.Of(("overflow", ovv));
            if (TryValue(v, "object", out var obv))
            {
                if (obv == "contain" || obv == "cover" || obv == "fill" || obv == "none" || obv == "scale-down")
                    return UtilityResult.Of(("-o-object-fit", obv), ("object-fit", obv));
                return UtilityResult.Of(("-o-object-position", obv.Replace("-", " ")), ("object-position", obv.Replace("-", " ")));
            }
            if (TryValue(v, "cursor", out var cv)) return UtilityResult.Of(("cursor", cv));
            if (TryValue(v, "float", out var flv)) return UtilityResult.Of(("float", flv));
            if (TryValue(v, "select", out var slv)) return UtilityResult.Of(("-webkit-user-select", slv), ("-moz-user-select", slv), ("user-select", slv));
            return null;
        }

        private UtilityResult TryAspect(string v)
        {
            if (!TryValue(v, "aspect", out var av)) return null;
            if (av == "auto") return UtilityResult.Of(("aspect-ratio", "auto"));
            if (av == "square") return UtilityResult.Of(("aspect-ratio", "1 / 1"));
            if (av == "video") return UtilityResult.Of(("aspect-ratio", "16 / 9"));
            if (IsArbitrary(av, out var inner)) return UtilityResult.Of(("aspect-ratio", inner));
            return null;
        }

        // Resolves theme(...) references inside arbitrary calc() values.
        private string ResolveThemeFn(string value)
        {
            int idx;
            while ((idx = value.IndexOf("theme(", StringComparison.Ordinal)) >= 0)
            {
                int close = value.IndexOf(')', idx);
                if (close < 0) break;
                var path = value.Substring(idx + 6, close - (idx + 6)).Trim();
                var resolved = ResolveThemePath(path) ?? "0";
                value = value.Substring(0, idx) + resolved + value.Substring(close + 1);
            }
            return value;
        }
        private string ResolveThemePath(string path)
        {
            // Only borderRadius.* is needed by the current vocabulary.
            var parts = path.Split('.');
            if (parts.Length == 2 && parts[0] == "borderRadius")
                return _t.BorderRadius.TryGetValue(parts[1], out var r) ? r : null;
            return null;
        }

        // ---- small utilities ----------------------------------------------

        private static bool TryValue(string core, string prefix, out string value)
        {
            value = null;
            if (core == prefix) { value = ""; return true; }
            if (core.Length > prefix.Length + 1 && core[prefix.Length] == '-' && core.StartsWith(prefix, StringComparison.Ordinal))
            {
                value = core.Substring(prefix.Length + 1);
                return true;
            }
            return false;
        }

        private bool IsFontSizeKey(string v)
        {
            int slash = v.IndexOf('/');
            var key = slash > 0 ? v.Substring(0, slash) : v;
            return _t.FontSize.ContainsKey(key);
        }

        private static bool IsLengthLike(string v)
        {
            if (string.IsNullOrEmpty(v)) return false;
            if (v.StartsWith("#")) return false;
            foreach (var unit in new[] { "px", "rem", "em", "vh", "vw", "%", "ch", "pt", "ex" })
                if (v.EndsWith(unit)) return true;
            return double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        // camelCase → kebab-case (matches Tailwind's arbitrary-property dasherizing).
        private static string Kebab(string s)
        {
            var sb = new StringBuilder(s.Length + 4);
            foreach (var ch in s)
            {
                if (ch >= 'A' && ch <= 'Z') { sb.Append('-').Append(char.ToLowerInvariant(ch)); }
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        // Swaps the colour in a shadow value for var(--tw-shadow-color).
        private static string ColorizeShadow(string value)
        {
            foreach (var fn in new[] { "rgba(", "rgb(", "hsla(", "hsl(" })
            {
                int idx = value.IndexOf(fn, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    int close = value.IndexOf(')', idx);
                    if (close >= 0)
                        return value.Substring(0, idx) + "var(--tw-shadow-color)" + value.Substring(close + 1);
                }
            }
            int hash = value.LastIndexOf('#');
            if (hash >= 0)
            {
                int end = hash + 1;
                while (end < value.Length && Uri.IsHexDigit(value[end])) end++;
                return value.Substring(0, hash) + "var(--tw-shadow-color)" + value.Substring(end);
            }
            return value;
        }

        private static UtilityResult WithSuffix(string suffix, params (string, string)[] decls)
        {
            var p = new UtilityPart { SelectorSuffix = suffix };
            p.Declarations.AddRange(decls);
            return new UtilityResult().Add(p);
        }

        // ---- static, value-less utilities ----------------------------------

        private static readonly Dictionary<string, List<(string, string)>> _static = new(StringComparer.Ordinal)
        {
            ["block"] = D(("display", "block")),
            ["inline-block"] = D(("display", "inline-block")),
            ["inline"] = D(("display", "inline")),
            ["flex"] = D(("display", "flex")),
            ["inline-flex"] = D(("display", "inline-flex")),
            ["table"] = D(("display", "table")),
            ["inline-table"] = D(("display", "inline-table")),
            ["grid"] = D(("display", "grid")),
            ["inline-grid"] = D(("display", "inline-grid")),
            ["contents"] = D(("display", "contents")),
            ["flow-root"] = D(("display", "flow-root")),
            ["list-item"] = D(("display", "list-item")),
            ["hidden"] = D(("display", "none")),

            ["static"] = D(("position", "static")),
            ["fixed"] = D(("position", "fixed")),
            ["absolute"] = D(("position", "absolute")),
            ["relative"] = D(("position", "relative")),
            ["sticky"] = D(("position", "sticky")),

            ["visible"] = D(("visibility", "visible")),
            ["invisible"] = D(("visibility", "hidden")),
            ["collapse"] = D(("visibility", "collapse")),
            ["isolate"] = D(("isolation", "isolate")),
            ["isolation-auto"] = D(("isolation", "auto")),

            ["items-start"] = D(("align-items", "flex-start")),
            ["items-end"] = D(("align-items", "flex-end")),
            ["items-center"] = D(("align-items", "center")),
            ["items-baseline"] = D(("align-items", "baseline")),
            ["items-stretch"] = D(("align-items", "stretch")),

            ["justify-start"] = D(("justify-content", "flex-start")),
            ["justify-end"] = D(("justify-content", "flex-end")),
            ["justify-center"] = D(("justify-content", "center")),
            ["justify-between"] = D(("justify-content", "space-between")),
            ["justify-around"] = D(("justify-content", "space-around")),
            ["justify-evenly"] = D(("justify-content", "space-evenly")),

            ["justify-items-start"] = D(("justify-items", "start")),
            ["justify-items-center"] = D(("justify-items", "center")),
            ["justify-items-end"] = D(("justify-items", "end")),

            ["content-center"] = D(("align-content", "center")),
            ["content-start"] = D(("align-content", "flex-start")),
            ["content-end"] = D(("align-content", "flex-end")),
            ["content-between"] = D(("align-content", "space-between")),

            ["self-auto"] = D(("align-self", "auto")),
            ["self-start"] = D(("align-self", "flex-start")),
            ["self-end"] = D(("align-self", "flex-end")),
            ["self-center"] = D(("align-self", "center")),
            ["self-stretch"] = D(("align-self", "stretch")),

            ["place-items-center"] = D(("place-items", "center")),
            ["place-items-start"] = D(("place-items", "start")),
            ["place-content-center"] = D(("place-content", "center")),

            ["float-left"] = D(("float", "left")),
            ["float-right"] = D(("float", "right")),
            ["float-none"] = D(("float", "none")),

            ["italic"] = D(("font-style", "italic")),
            ["uppercase"] = D(("text-transform", "uppercase")),
            ["lowercase"] = D(("text-transform", "lowercase")),
            ["capitalize"] = D(("text-transform", "capitalize")),
            ["normal-case"] = D(("text-transform", "none")),

            ["underline"] = D(("text-decoration-line", "underline")),
            ["overline"] = D(("text-decoration-line", "overline")),
            ["line-through"] = D(("text-decoration-line", "line-through")),
            ["no-underline"] = D(("text-decoration-line", "none")),

            ["truncate"] = D(("overflow", "hidden"), ("text-overflow", "ellipsis"), ("white-space", "nowrap")),

            ["pointer-events-none"] = D(("pointer-events", "none")),
            ["pointer-events-auto"] = D(("pointer-events", "auto")),

            ["resize"] = D(("resize", "both")),
            ["resize-none"] = D(("resize", "none")),
            ["resize-x"] = D(("resize", "horizontal")),
            ["resize-y"] = D(("resize", "vertical")),

            ["select-none"] = D(("-webkit-user-select", "none"), ("-moz-user-select", "none"), ("user-select", "none")),
            ["select-text"] = D(("-webkit-user-select", "text"), ("-moz-user-select", "text"), ("user-select", "text")),
            ["select-all"] = D(("-webkit-user-select", "all"), ("-moz-user-select", "all"), ("user-select", "all")),
            ["select-auto"] = D(("-webkit-user-select", "auto"), ("-moz-user-select", "auto"), ("user-select", "auto")),

            ["appearance-none"] = D(("appearance", "none")),

            ["border-collapse"] = D(("border-collapse", "collapse")),
            ["border-separate"] = D(("border-collapse", "separate")),
            ["border-solid"] = D(("border-style", "solid")),
            ["border-dashed"] = D(("border-style", "dashed")),
            ["border-dotted"] = D(("border-style", "dotted")),
            ["border-none"] = D(("border-style", "none")),

            ["scroll-smooth"] = D(("scroll-behavior", "smooth")),
            ["scroll-auto"] = D(("scroll-behavior", "auto")),

            ["tabular-nums"] = D(("--tw-numeric-spacing", "tabular-nums"), ("font-variant-numeric", "var(--tw-ordinal) var(--tw-slashed-zero) var(--tw-numeric-figure) var(--tw-numeric-spacing) var(--tw-numeric-fraction)")),

            ["align-middle"] = D(("vertical-align", "middle")),
            ["align-top"] = D(("vertical-align", "top")),
            ["align-bottom"] = D(("vertical-align", "bottom")),
            ["align-baseline"] = D(("vertical-align", "baseline")),

            ["list-none"] = D(("list-style-type", "none")),
            ["list-disc"] = D(("list-style-type", "disc")),
            ["list-decimal"] = D(("list-style-type", "decimal")),

            ["transform"] = D(("transform", Transform)),
            ["transform-none"] = D(("transform", "none")),

            ["filter"] = D(("filter", FilterChain)),
            ["filter-none"] = D(("filter", "none")),
            ["backdrop-filter"] = D(("-webkit-backdrop-filter", BackdropChain), ("backdrop-filter", BackdropChain)),

            ["bg-cover"] = D(("background-size", "cover")),
            ["bg-contain"] = D(("background-size", "contain")),
            ["bg-no-repeat"] = D(("background-repeat", "no-repeat")),

            ["sr-only"] = D(
                ("position", "absolute"), ("width", "1px"), ("height", "1px"),
                ("padding", "0"), ("margin", "-1px"), ("overflow", "hidden"),
                ("clip", "rect(0, 0, 0, 0)"), ("white-space", "nowrap"), ("border-width", "0")),
            ["not-sr-only"] = D(
                ("position", "static"), ("width", "auto"), ("height", "auto"),
                ("padding", "0"), ("margin", "0"), ("overflow", "visible"),
                ("clip", "auto"), ("white-space", "normal")),
        };

        private static List<(string, string)> D(params (string, string)[] decls) => new(decls);

        private static readonly Dictionary<string, (string Shadow, string Colored)> _shadows = new(StringComparer.Ordinal)
        {
            ["sm"] = ("0 1px 2px 0 rgb(0 0 0 / 0.05)", "0 1px 2px 0 var(--tw-shadow-color)"),
            [""] = ("0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)", "0 1px 3px 0 var(--tw-shadow-color), 0 1px 2px -1px var(--tw-shadow-color)"),
            ["md"] = ("0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)", "0 4px 6px -1px var(--tw-shadow-color), 0 2px 4px -2px var(--tw-shadow-color)"),
            ["lg"] = ("0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)", "0 10px 15px -3px var(--tw-shadow-color), 0 4px 6px -4px var(--tw-shadow-color)"),
            ["xl"] = ("0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)", "0 20px 25px -5px var(--tw-shadow-color), 0 8px 10px -6px var(--tw-shadow-color)"),
            ["2xl"] = ("0 25px 50px -12px rgb(0 0 0 / 0.25)", "0 25px 50px -12px var(--tw-shadow-color)"),
            ["inner"] = ("inset 0 2px 4px 0 rgb(0 0 0 / 0.05)", "inset 0 2px 4px 0 var(--tw-shadow-color)"),
            ["none"] = ("0 0 #0000", "0 0 #0000"),
        };
    }
}
