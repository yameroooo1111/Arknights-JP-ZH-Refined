// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using System.Threading;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Torappu.UI.HandBook;

namespace ArknightsLocalization.RichTextFix;

internal static class VoiceContextTranslations
{
    private static ManualLogSource? log;
    private static int successLogged, errorLogged;
    internal static void Enable(ManualLogSource logger) => log = logger;

    internal static string? Lookup(object component, string original, string? role)
    {
        if (role != "operator-voice") return null;
        try
        {
            if (component is not Il2CppObjectBase obj) return null;
            Component? text = obj.TryCast<Component>();
            if (text == null) return null;
            string? character = null;
            for (Transform? parent = text.transform; parent != null; parent = parent.parent)
                foreach (MonoBehaviour owner in parent.GetComponents<MonoBehaviour>())
                {
                    if (owner == null) continue;
                    var audio = owner.TryCast<HandBookInfoAudioView>();
                    var data = audio?.m_sourceData;
                    if (data != null)
                    {
                        // The exact voice row wins over the character-wide map.
                        string? row = ContextTranslations.Lookup("voice-row:" + data.charWordId, original);
                        if (row != null) return Report(row);
                        character ??= data.charId;
                    }
                    var view = owner.TryCast<HandBookInfoView>();
                    var state = owner.TryCast<HandBookInfoState>();
                    var bean = view?._stateBean ?? state?._stateBean;
                    if (bean?.selectedCard != null)
                        character ??= bean.selectedCard.characterKey;
                }
            if (string.IsNullOrEmpty(character)) return null;
            return Report(ContextTranslations.Lookup("voice-character:" + character, original));
        }
        catch (Exception exception)
        {
            if (Interlocked.Exchange(ref errorLogged, 1) == 0)
                log?.LogWarning("Voice context lookup unavailable; retaining ordinary lookup: " + exception.GetType().Name);
            return null;
        }
    }

    private static string? Report(string? value)
    {
        if (value != null && Interlocked.Exchange(ref successLogged, 1) == 0)
            log?.LogInfo("Applied an exact operator voice-context translation.");
        return value;
    }
}
