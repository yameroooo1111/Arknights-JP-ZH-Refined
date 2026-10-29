// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ArknightsLocalization.RichTextFix;

// Only exact, reviewed blocks in explicitly bound UI fields may compose.
internal static class CompositeDisplayTranslations
{
    private static readonly Regex LineWhitespace = new(@"[ \t]*\n[ \t]*", RegexOptions.Compiled);
    private static readonly Regex NameSeparator = new(@"(\s*[/／]\s*)", RegexOptions.Compiled);

    internal static string Normalize(string value) => LineWhitespace.Replace(
        RichTextStyleRestorer.PlainText(value).Replace("\\r\\n", "\n").Replace("\\n", "\n")
        .Replace("\r\n", "\n").Replace('\r', '\n'), "\n").Trim();

    internal static string? Lookup(string? role, string original)
    {
        if (role == "gacha-operator-list") return OperatorList(original);
        if (role != "operator-trait") return null;
        string plain = Normalize(original);
        return Blocks(plain, value => ContextTranslations.Lookup("trait-block", value));
    }

    internal static string? OperatorList(string original)
    {
        string[] parts = NameSeparator.Split(RichTextStyleRestorer.PlainText(original));
        if (parts.Length > 2049) return null;
        bool changed = false;
        for (int i = 0; i < parts.Length; i += 2)
        {
            string name = parts[i].Trim();
            string? target = ContextTranslations.Lookup("gacha-operator-name", name);
            if (target == null) return null; // Never discard or guess a future JP pool member.
            changed |= target != name;
            int start = parts[i].IndexOf(name, StringComparison.Ordinal);
            parts[i] = parts[i][..start] + target + parts[i][(start + name.Length)..];
        }
        if (!changed) return null;
        return OfficialRichStyles.Restore(original, string.Concat(parts),
            name => ContextTranslations.Lookup("gacha-operator-name", name));
    }

    // Dynamic programming allows a multiline base trait followed by a module
    // block. Reject unknown lines and multiple distinct complete interpretations.
    internal static string? Blocks(string plain, Func<string, string?> lookup)
    {
        string? whole = lookup(plain);
        if (whole != null) return whole;
        string[] lines = plain.Split('\n');
        if (lines.Length is < 2 or > 32 || plain.Length > 8192) return null;
        string?[] suffix = new string?[lines.Length + 1];
        bool[] ambiguous = new bool[lines.Length + 1];
        suffix[lines.Length] = "";
        for (int start = lines.Length - 1; start >= 0; start--)
        {
            for (int end = start + 1; end <= lines.Length; end++)
            {
                string block = string.Join("\n", lines, start, end - start).Trim();
                string? translated = block.Length == 0 ? "" : lookup(block);
                if (translated == null || (suffix[end] == null && !ambiguous[end])) continue;
                if (ambiguous[end]) { ambiguous[start] = true; continue; }
                string candidate = translated + (end == lines.Length ? "" : "\n" + suffix[end]);
                if (suffix[start] != null && suffix[start] != candidate) ambiguous[start] = true;
                suffix[start] = candidate;
            }
        }
        return ambiguous[0] ? null : suffix[0];
    }
}
