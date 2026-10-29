// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using System.Threading;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace ArknightsLocalization.RichTextFix;

// A short line is selected only within the exact active story. No global
// majority winner, scene-name guess, or stale cached story identity is used.
internal static class StoryContextTranslations
{
    private static ManualLogSource? log;
    private static int successLogged, errorLogged, missingControllerLogged;
    internal static void Enable(ManualLogSource logger) => log = logger;

    internal static string NormalizeStoryId(string id)
    {
        string value = id.Replace('\\', '/').Trim().ToLowerInvariant();
        foreach (string prefix in new[] { "dyn/gamedata/story/", "gamedata/story/", "story/" })
            if (value.StartsWith(prefix, StringComparison.Ordinal)) { value = value[prefix.Length..]; break; }
        if (value.EndsWith(".txt", StringComparison.Ordinal)) value = value[..^4];
        return value;
    }

    internal static string? Lookup(object component, string original, string? role)
    {
        if (role is not ("story" or "story-name")) return null;
        try
        {
            if (component is not Il2CppObjectBase obj) return null;
            Component? text = obj.TryCast<Component>();
            if (text == null) return null;
            Torappu.AVG.AVGController? controller = null;
            // Story panels, playback rows and reader rows live below their AVG
            // controller. Restrict lookup to the actual owning hierarchy.
            for (Transform? parent = text.transform; parent != null && controller == null; parent = parent.parent)
                foreach (MonoBehaviour owner in parent.GetComponents<MonoBehaviour>())
                {
                    if (owner == null) continue;
                    controller = owner.TryCast<Torappu.AVG.AVGController>();
                    if (controller != null) break;
                }
            if (controller == null)
            {
                if (Interlocked.Exchange(ref missingControllerLogged, 1) == 0)
                    log?.LogInfo("Story context lookup found no owning AVG controller; retaining ordinary lookup.");
                return null;
            }
            string? id = controller.storyId;
            if (string.IsNullOrEmpty(id)) return null;
            string key = NormalizeStoryId(id);
            if (key.Length == 0) return null;
            string scope = (role == "story-name" ? "story-speaker:" : "story-file:") + key;
            string? translated = DynamicTranslations.Lookup(scope, original) ?? ContextTranslations.Lookup(scope, original);
            if (translated != null && Interlocked.Exchange(ref successLogged, 1) == 0)
                log?.LogInfo("Applied an exact story-scoped translation for " + key + ".");
            return translated;
        }
        catch (Exception exception)
        {
            if (Interlocked.Exchange(ref errorLogged, 1) == 0)
                log?.LogWarning("Story context lookup unavailable; retaining ordinary lookup: " + exception.GetType().Name);
            return null;
        }
    }
}
