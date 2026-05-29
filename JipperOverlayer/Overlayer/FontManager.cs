using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace JipperOverlayer.Overlayer;

public static class FontManager
{
    public class FontEntry
    {
        public string name;
        public TMP_FontAsset font;
        public string sourceFontName;
    }

    public static List<FontEntry> FontList;
    public static string[] FontNames;

    public static void ScanFonts()
    {
        FontList = [];

        // 1. Bundle font
        if (BundleLoader.FontAsset != null)
            FontList.Add(new FontEntry { name = "Bundle Font", font = BundleLoader.FontAsset, sourceFontName = "Bundle Font" });

        // 2. Game Font objects — convert to TMP (skips fonts with path-like names from other mods)
        var allFonts = Resources.FindObjectsOfTypeAll<Font>();
        foreach (var f in allFonts)
        {
            if (f == null || string.IsNullOrEmpty(f.name)) continue;
            if (f.name.Contains("\\") || f.name.Contains("/")) continue;
            bool exists = false;
            foreach (var e in FontList)
                if (e.sourceFontName == f.name) { exists = true; break; }
            if (exists) continue;
            var tmpFont = TMP_FontAsset.CreateFontAsset(f);
            if (tmpFont != null)
                FontList.Add(new FontEntry { name = f.name, font = tmpFont, sourceFontName = f.name });
        }

        // 3. Custom fonts from CustomFonts directory
        try
        {
            string customDir = Path.Combine(Main.Mod.Path, "CustomFonts");
            if (!Directory.Exists(customDir))
                Directory.CreateDirectory(customDir);
            ScanCustomDir(customDir, "*.ttf");
            ScanCustomDir(customDir, "*.otf");
        }
        catch (Exception e) { Main.Mod.Logger.Warning($"CustomFonts: {e.Message}"); }

        FontNames = new string[FontList.Count];
        for (int i = 0; i < FontList.Count; i++)
            FontNames[i] = FontList[i].name;

        // Resolve saved font name → index (handles list changes between sessions)
        if (Main.Settings != null && !string.IsNullOrEmpty(Main.Settings.FontName))
        {
            int idx = FindFontIndex(Main.Settings.FontName);
            if (idx >= 0) Main.Settings.FontIndex = idx;
            else Main.Settings.FontIndex = 0;
        }

        // Link CJK fallback
        TMP_FontAsset cjk = null;
        try { cjk = RDConstants.data.chineseFontTMPro; } catch { }
        if (cjk != null)
        {
            foreach (var entry in FontList)
            {
                if (entry.font == null || entry.font == cjk) continue;
                entry.font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                if (!entry.font.fallbackFontAssetTable.Contains(cjk))
                    entry.font.fallbackFontAssetTable.Add(cjk);
            }
        }

        Main.Mod.Logger.Log($"FontManager: {FontList.Count} fonts");
    }

    static void ScanCustomDir(string dir, string pattern)
    {
        foreach (var file in Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly))
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string entryName = "Custom: " + fileName;

                bool exists = false;
                foreach (var e in FontList)
                    if (e.name.Equals(entryName, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
                if (exists)
                {
                    Main.Mod.Logger.Log($"FontManager: Custom font '{fileName}' already loaded, skipping");
                    continue;
                }

                Font font = new Font(file);
                TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(font);
                if (tmpFont != null)
                {
                    FontList.Add(new FontEntry { name = entryName, font = tmpFont, sourceFontName = fileName });
                    Main.Mod.Logger.Log($"FontManager: Loaded custom font '{fileName}'");
                }
                else
                {
                    Main.Mod.Logger.Warning($"FontManager: Failed to create TMP_FontAsset from '{fileName}'");
                }
            }
            catch (Exception e) { Main.Mod.Logger.Warning($"FontManager: Skip {Path.GetFileName(file)}: {e.Message}"); }
        }
    }

    public static TMP_FontAsset GetFont(int index)
    {
        if (FontList == null || index < 0 || index >= FontList.Count)
            return BundleLoader.FontAsset;
        return FontList[index].font ?? BundleLoader.FontAsset;
    }

    public static int FindFontIndex(string fontName)
    {
        if (string.IsNullOrEmpty(fontName) || FontList == null) return 0;
        for (int i = 0; i < FontList.Count; i++)
            if (FontList[i].name == fontName) return i;
        return 0;
    }
}
