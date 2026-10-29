// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using BepInEx.Logging;

namespace ArknightsLocalization.RichTextFix;

internal static class CatalogDisplayTranslations
{
    private const string Fraction = @"\s*[0-9][0-9,]*\s*/\s*[0-9][0-9,]*\s*";
    private static readonly Regex Progress = new(@"\A(?<text>.+?)(?<progress>\s*(?:（" + Fraction + @"）|\(" + Fraction + @"\)|\[" + Fraction + @"\]))\z", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Bullet = new(@"\A[-・·•][ \t]*", RegexOptions.Compiled);
    private static readonly HashSet<string> reported = new(StringComparer.Ordinal);
    private static ManualLogSource? log;
    internal static void Enable(ManualLogSource source) => log = source;
    private static string? Exact(string role, string text) => ContextTranslations.Lookup(role, text)
        ?? ContextTranslations.Lookup("flat:" + role, text.Replace("\n", ""));
    internal static string? Lookup(string? role, string original)
    {
        if (role is not ("medal-display" or "gallery-display" or "sandbox-display")) return null;
        string plain = CompositeDisplayTranslations.Normalize(original);
        string? target = Exact(role, plain) ?? DynamicTranslations.Lookup(role, plain);
        if (target != null) return target;
        target = Decorated(role, plain);
        if (target != null) return target;
        // A title and its acquisition condition can be combined in a reused row.
        target = CompositeDisplayTranslations.Blocks(plain, value => Exact(role, value) ?? Decorated(role, value));
        if (target != null) return target;
        if (log != null && plain.Length < 10000 && Regex.IsMatch(plain, "[ぁ-ヿ]") &&
            reported.Count < 64 && reported.Add(role + "\n" + original))
            log.LogInfo("Unmatched public collection description (" + role + "): " + JsonSerializer.Serialize(original));
        return null;
    }

    private static string? Decorated(string role, string plain)
    {
        // Captured MedalListNotGetItemView text prepends '-' before appending
        // a rich-text progress counter. Keep both decorations verbatim.
        string prefix = role == "medal-display" ? Bullet.Match(plain).Value : "";
        string body = plain[prefix.Length..];
        Match progress = Progress.Match(body);
        string condition = progress.Success ? progress.Groups["text"].Value.Trim() : body;
        string? translated = Exact(role, condition) ?? DynamicTranslations.Lookup(role, condition);
        return translated == null ? null : prefix + translated + (progress.Success ? progress.Groups["progress"].Value : "");
    }
}
