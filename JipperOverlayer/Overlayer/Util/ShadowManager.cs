using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JipperOverlayer.Overlayer.Util;

internal static class ShadowManager
{
    internal static readonly Shader ShaderRef = (Shader)typeof(ShaderUtilities).GetProperty("ShaderRef_MobileSDF", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);

    private static readonly Dictionary<TMP_FontAsset, Material> MaterialCache = new();
    private static System.Reflection.MemberInfo _cachedMaterialMember;
    private static bool _cachedMaterialLogged;

    public static void ClearCache() => MaterialCache.Clear();

    public static void ApplyShadow(TextMeshProUGUI text) => Apply(text, 0.5f);
    public static void ApplyDarkShadow(TextMeshProUGUI text) => Apply(text, 0.7f);

    private static void Apply(TextMeshProUGUI text, float alpha)
    {
        try
        {
            var font = text.font;
            if (font == null) return;
            if (!MaterialCache.TryGetValue(font, out var mat))
            {
                var fontMat = GetFontMaterial(font);
                if (fontMat == null)
                {
                    Main.Mod.Logger.Warning($"Shadow: Cannot get material from font '{font.name}', skipping");
                    return;
                }
                mat = new Material(fontMat);
                if (ShaderRef) mat.shader = ShaderRef;
                mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.01f);
                mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, alpha));
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                MaterialCache[font] = mat;
            }
            text.fontSharedMaterial = mat;
        }
        catch (Exception e) { Main.Mod.Logger.Warning($"Shadow error: {e.Message}"); }
    }

    private static Material GetFontMaterial(TMP_FontAsset font)
    {
        if (_cachedMaterialMember == null)
        {
            var t = font.GetType();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            _cachedMaterialMember = (System.Reflection.MemberInfo)t.GetProperty("material", flags) ?? t.GetField("material", flags);
        }

        Material result = null;
        if (_cachedMaterialMember is System.Reflection.PropertyInfo pi)
        {
            var val = pi.GetValue(font);
            if (val != null) result = (Material)val;
        }
        else if (_cachedMaterialMember is System.Reflection.FieldInfo fi)
        {
            var val = fi.GetValue(font);
            if (val != null) result = (Material)val;
        }

        if (!_cachedMaterialLogged)
        {
            _cachedMaterialLogged = true;
            string foundBy = _cachedMaterialMember != null
                ? $"{_cachedMaterialMember.MemberType} \"{_cachedMaterialMember.Name}\""
                : "none";
            Main.Mod.Logger.Log($"Overlay: Font material resolved via {foundBy}");
        }
        return result;
    }
}
