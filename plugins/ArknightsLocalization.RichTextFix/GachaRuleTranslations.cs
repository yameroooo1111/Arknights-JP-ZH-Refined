// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BepInEx.Logging;

namespace ArknightsLocalization.RichTextFix;

internal static class GachaRuleTranslations
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly HashSet<string> reported = new(StringComparer.Ordinal);
    private static ManualLogSource? log;
    internal static void Enable(ManualLogSource source) => log = source;
    internal static string Compact(string text) => Whitespace.Replace(
        CompositeDisplayTranslations.Normalize(text), "");

    internal static string? Lookup(string? role, string original)
    {
        if (role != "gacha-rule") return null;
        string plain = Compact(original);
        if (plain.Length > 50000) return null;
        string? direct = ContextTranslations.Lookup("gacha-rule", plain);
        if (direct != null) return direct;
        // Server-supplied paragraphs can combine headings and several complete
        // sentences into one Text. Only consume entire reviewed sentences.
        var entries = ContextTranslations.Entries("gacha-rule");
        StringBuilder result = new();
        int offset = 0;
        while (offset < plain.Length)
        {
            string? source = null, target = null;
            foreach (var pair in entries)
                if (pair.Key.Length > (source?.Length ?? 0) &&
                    plain.AsSpan(offset).StartsWith(pair.Key.AsSpan(), StringComparison.Ordinal))
                { source = pair.Key; target = pair.Value; }
            if (source == null)
            {
                // Only this explicitly bound public gacha-description field is
                // captured; never log arbitrary UI/player/chat text.
                if (log != null && reported.Count < 64 && reported.Add(original))
                    log.LogInfo("Unmatched public gacha description: " + JsonSerializer.Serialize(original));
                return null;
            }
            result.Append(target);
            offset += source.Length;
        }
        return result.Length == 0 ? null : result.ToString();
    }
}
