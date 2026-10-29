// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using BepInEx.Logging;

namespace ArknightsLocalization.RichTextFix;

internal static class DisplayVerification
{
    private static ManualLogSource? log;
    private static readonly HashSet<string> bound = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, HashSet<string>> rendered = new(StringComparer.Ordinal);
    internal static void Enable(ManualLogSource source) => log = source;
    private static bool Eligible(string? role) => role is "operator-trait" or "medal-display" or "gallery-display" or "sandbox-display";
    internal static void Bound(string role, string owner, string field)
    {
        string key = owner + "." + field + " => " + role;
        if (Eligible(role) && bound.Add(key)) log?.LogInfo("Verified live display field: " + key);
    }
    internal static void Result(string? role, string original, string? translation)
    {
        if (log == null || !Eligible(role) || original.Length > 10000 || !Regex.IsMatch(original, "[ぁ-ヿ]")) return;
        if (!rendered.TryGetValue(role!, out var seen)) rendered[role!] = seen = new(StringComparer.Ordinal);
        if (seen.Count >= 48 || !seen.Add(original)) return;
        log.LogInfo("Live display lookup: " + JsonSerializer.Serialize(new {role, source = original, target = translation}));
    }
}
