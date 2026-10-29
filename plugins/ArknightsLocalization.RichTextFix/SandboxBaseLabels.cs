// Refined distribution: modified 2026-10-29 by 芽咩洛. GPL-3.0; original attribution in UPSTREAM.md.
using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Torappu.UI.SandboxPerm.SandboxV3;

namespace ArknightsLocalization.RichTextFix;

// These four captions are sprites in ui/sandboxv3/[uc]common.ab.
// Resolve the exact hierarchy both on image activation and data refresh.
internal static class SandboxBaseLabels
{
    private static readonly string[] captions = { "天灾预警系统", "后勤保障系统", "激光开采模块", "物流交易仓" };
    private static readonly string[] english = { "CATASTROPHE EARLY WARNING SYSTEM", "LOGISTICS SUPPORT SYSTEM", "LASER MINING MODULE", "TRANSSHIPMENT PORT" };
    private static ManualLogSource? log;
    private static bool reported;
    internal static MethodInfo RefreshTarget() => AccessTools.Method(typeof(SandboxV3BasementDetailView), "OnValueChanged", new[] { typeof(SandboxV3BasementDetailViewProperty) })
        ?? throw new MissingMethodException("SandboxV3BasementDetailView.OnValueChanged(SandboxV3BasementDetailViewProperty)");
    internal static int CaptionIndex(string path)
    {
        for (int i = 1; i <= 4; i++)
            if (path == "basement_detail_state/panel_left/root_build (" + i + ")/title_" + i + "/ime_title") return i - 1;
        return -1;
    }
    internal static void Enable(ManualLogSource logger)
    {
        log = logger;
        var harmony = new Harmony("arklocalizer.sandbox.captions");
        harmony.Patch(AccessTools.Method(typeof(Image), "OnEnable", Type.EmptyTypes), postfix: new HarmonyMethod(typeof(SandboxBaseLabels), nameof(ImageEnabled)));
        harmony.Patch(RefreshTarget(), postfix: new HarmonyMethod(typeof(SandboxBaseLabels), nameof(Refresh)));
        foreach (var view in Resources.FindObjectsOfTypeAll<SandboxV3BasementDetailView>())
            if (view.gameObject.scene.IsValid()) Refresh(view);
    }
    private static void ImageEnabled(Image __instance)
    {
        if (__instance.name != "ime_title") return;
        string path = __instance.name;
        for (Transform? p = __instance.transform.parent; p != null; p = p.parent)
        {
            path = p.name + "/" + path;
            if (p.name == "basement_detail_state") break;
        }
        int index = CaptionIndex(path);
        if (index >= 0) Apply(__instance, index);
    }
    private static void Refresh(SandboxV3BasementDetailView __instance)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            Image? image = __instance.transform.Find("panel_left/root_build (" + (i+1) + ")/title_" + (i+1) + "/ime_title")?.GetComponent<Image>();
            if (image != null && Apply(image, i)) count++;
        }
        if (!reported && count == 4)
        {
            reported = true;
            log?.LogInfo("Sandbox base caption objects updated: 4/4; visual confirmation pending.");
        }
    }
    private static bool Apply(Image image, int index)
    {
        try
        {
            Font? font = CnFontOverride.LoadFont();
            if (font == null) return false;
            // Hide only the Image component after both replacement lines exist;
            // the parent object, layout, children and buttons stay active.
            MakeLine(image, font, "ArklocalizerBaseCaption", captions[index], .35f, 1f, 28, index);
            MakeLine(image, font, "ArklocalizerBaseEnglish", english[index], 0f, .35f, 15, index);
            image.enabled = false;
            return true;
        }
        catch (Exception e) { log?.LogWarning("Sandbox caption replacement failed: " + e.Message); return false; }
    }
    private static void MakeLine(Image image, Font font, string name, string caption, float low, float high, int size, int index)
    {
        Text? label = image.transform.Find(name)?.GetComponent<Text>();
        if (label == null)
        {
            var child = new GameObject(name);
            var rect = child.AddComponent<RectTransform>();
            rect.SetParent(image.transform, false);
            rect.anchorMin = new Vector2(0, low);
            rect.anchorMax = new Vector2(1, high);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            label = child.AddComponent<Text>();
            label.raycastTarget = false;
            label.font = font;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = size;
            label.alignment = (index == 0 || index == 2) ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }
        label.color = image.color;
        label.text = caption;
    }
}
