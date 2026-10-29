// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using System.Text.RegularExpressions;

namespace ArknightsLocalization.RichTextFix;

internal static class ShopContentTranslations
{
    private static readonly Regex Quantity = new(@"\A(?<name>.+?)(?<quantity>\s*[x×ｘ]\s*[0-9０-９][0-9０-９,，]*)\z", RegexOptions.CultureInvariant);

    internal static string? Lookup(string? role, string original)
    {
        if (role != "shop-content") return null;
        string plain = RichTextStyleRestorer.PlainText(original).Replace("\r\n", "\n");
        string[] lines = plain.Split('\n');
        bool changed = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0) continue;
            string prefix = "";
            if ("·・•.．".Contains(line[0])) { prefix = line[..1]; line = line[1..].TrimStart(); }
            string quantity = "";
            Match match = Quantity.Match(line);
            if (match.Success) { line = match.Groups["name"].Value.TrimEnd(); quantity = match.Groups["quantity"].Value; }
            string? target = ContextTranslations.Lookup("shop-item-name", line);
            if (target == null)
            {
                // Entirely known item lines only; never partly rewrite prose.
                if (!ContextTranslations.IsTarget("shop-item-name", line)) return null;
                target = line;
            }
            changed |= target != line;
            lines[i] = prefix + target + quantity;
        }
        return changed ? string.Join("\n", lines) : null;
    }
}
