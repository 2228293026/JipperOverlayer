using System;
using System.IO;
using TMPro;
using UnityEngine;

namespace JipperOverlayer.Overlayer;

public class BundleLoader
{
    public static AssetBundle Bundle;
    public static TMP_FontAsset FontAsset;
    public static GameObject ProgressObject;

    public static void LoadBundle()
    {
        string path;
        switch (ADOBase.platform)
        {
            case Platform.Windows:
                path = Path.Combine(Main.Mod.Path, "jipperoverlayerbundle");
                break;
            case Platform.Linux:
                path = Path.Combine(Main.Mod.Path, "Linux/jipperoverlayerbundle");
                break;
            case Platform.Mac:
                path = Path.Combine(Main.Mod.Path, "Mac/jipperoverlayerbundle");
                break;
            default:
                Main.Mod.Logger.Warning("Unsupported platform, defaulting to Windows path");
                goto case Platform.Windows;
        }

        Main.Mod.Logger.Log("Unity Version: " + Application.unityVersion);
        if (Application.unityVersion.StartsWith("2022")) path += "2022";
        else if (Application.unityVersion.StartsWith("6000")) path += "6000";

        if (!File.Exists(path))
        {
            Main.Mod.Logger.Warning($"Bundle not found at: {path}");
            CreateFallbackFont();
            return;
        }

        Bundle = AssetBundle.LoadFromFile(path);
        if (Bundle == null)
        {
            Main.Mod.Logger.Warning("Failed to load AssetBundle");
            CreateFallbackFont();
            return;
        }

        foreach (UnityEngine.Object asset in Bundle.LoadAllAssets())
        {
            Main.Mod.Logger.Log($"  Bundle asset: {asset.name} ({asset.GetType().Name}");

            if (asset is Font font && FontAsset == null)
            {
                try
                {
                    FontAsset = TMP_FontAsset.CreateFontAsset(font);
                }
                catch (Exception e)
                {
                    Main.Mod.Logger.Warning($"Font creation failed: {e.Message}");
                }

                if (FontAsset != null)
                {
                    FontAsset.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
                    FontAsset.fallbackFontAssetTable.Add(RDConstants.data.chineseFontTMPro);
                    Main.Mod.Logger.Log("TMP font ready");
                }
            }
            else if (asset is GameObject go && go.name == "ProgressBar")
            {
                ProgressObject = go;
            }
        }

        if (FontAsset == null) CreateFallbackFont();
        Main.Mod.Logger.Log($"Bundle loaded. Font: {FontAsset != null}, ProgressBar: {ProgressObject != null}");
    }

    static void CreateFallbackFont()
    {
        FontAsset = RDConstants.data.chineseFontTMPro;
        Main.Mod.Logger.Log($"Using fallback font: {FontAsset?.name}");
    }

    public static void UnloadBundle()
    {
        if (Bundle != null)
        {
            Bundle.Unload(true);
            Bundle = null;
            if (FontAsset != RDConstants.data.chineseFontTMPro)
                FontAsset = null;
            ProgressObject = null;
        }
    }
}
