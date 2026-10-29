// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Torappu.UI.ArtGallery;

namespace ArknightsLocalization.RichTextFix;

// Verified in the current artgallery prefab: img_text is an Image, not Text.
// Replace only these four caption graphics at runtime; retain icons/hotspots.
internal static class GalleryEntryLabels
{
    private static readonly string[] captions = { "时装回廊", "收藏回廊", "主题展厅", "形艺特辑" };
    private static ManualLogSource log = null!;
    private static bool reported;
    internal static void Enable(ManualLogSource logger)
    {
        log = logger;
        new Harmony("arklocalizer.gallery.captions").Patch(
            AccessTools.Method(typeof(ArtGalleryEntryView), "OnValueChanged"),
            postfix: new HarmonyMethod(typeof(GalleryEntryLabels), nameof(Apply)));
        foreach (var view in Resources.FindObjectsOfTypeAll<ArtGalleryEntryView>())
            if (view.gameObject.scene.IsValid()) Apply(view);
    }

    private static void Apply(ArtGalleryEntryView __instance)
    {
        try
        {
            Font? font = CnFontOverride.LoadFont();
            if (font == null) return;
            int count = 0;
            for (int i = 0; i < captions.Length; i++)
            {
                Transform? holder = __instance.transform.Find("panel_button/button_" + (i + 1) + "/img_text");
                Image? image = holder?.GetComponent<Image>();
                if (image == null) continue;
                Transform? existing = holder!.Find("ArklocalizerCaption");
                Text? label = existing?.GetComponent<Text>();
                if (label == null)
                {
                    var child = new GameObject("ArklocalizerCaption");
                    var rect = child.AddComponent<RectTransform>();
                    rect.SetParent(holder, false);
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    label = child.AddComponent<Text>();
                    label.raycastTarget = false;
                    label.font = font;
                    label.fontSize = 26;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 18;
                    label.resizeTextMaxSize = 26;
                    label.alignment = TextAnchor.MiddleCenter;
                    label.supportRichText = false;
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                }
                label.color = image.color;
                label.text = captions[i];
                if (label.text != captions[i]) continue;
                image.enabled = false;
                count++;
            }
            if (!reported && count == 4)
            {
                reported = true;
                log.LogInfo("Verified live gallery caption replacement: 4 image labels replaced; original icons and hotspots retained.");
            }
            else if (count != 4) log.LogWarning("Gallery caption hierarchy changed; replaced " + count + " of 4 verified labels.");
        }
        catch (Exception error) { log.LogWarning("Could not replace gallery entry captions: " + error.Message); }
    }
}
