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
                path = Path.Combine(Loader.ModPath, "jipperoverlayerbundle");
                break;
            case Platform.Linux:
                path = Path.Combine(Loader.ModPath, "Linux/jipperoverlayerbundle");
                break;
            case Platform.Mac:
                path = Path.Combine(Loader.ModPath, "Mac/jipperoverlayerbundle");
                break;
            default:
                Loader.Warning("Unsupported platform, defaulting to Windows path");
                goto case Platform.Windows;
        }

        Loader.Log("Unity Version: " + Application.unityVersion);
        if (Application.unityVersion.StartsWith("2022")) path += "2022";
        else if (Application.unityVersion.StartsWith("6000")) path += "6000";

        if (!File.Exists(path))
        {
            Loader.Warning($"Bundle not found at: {path}");
            CreateFallbackFont();
            return;
        }

        Bundle = AssetBundle.LoadFromFile(path);
        if (Bundle == null)
        {
            Loader.Warning("Failed to load AssetBundle");
            CreateFallbackFont();
            return;
        }

        foreach (UnityEngine.Object asset in Bundle.LoadAllAssets())
        {
            Loader.Log($"  Bundle asset: {asset.name} ({asset.GetType().Name})");

            if (asset is Font font && FontAsset == null)
            {
                try
                {
                    FontAsset = TMP_FontAsset.CreateFontAsset(font);
                }
                catch (Exception e)
                {
                    Loader.Warning($"Font creation failed: {e.Message}");
                }

                if (FontAsset != null)
                {
                    FontAsset.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
                    FontAsset.fallbackFontAssetTable.Add(RDConstants.data.chineseFontTMPro);
                    Loader.Log("TMP font ready");
                }
            }
            else if (asset is GameObject go && go.name == "ProgressBar")
            {
                ProgressObject = go;
            }
        }

        if (FontAsset == null) CreateFallbackFont();
        Loader.Log($"Bundle loaded. Font: {FontAsset != null}, ProgressBar: {ProgressObject != null}");
    }

    static void CreateFallbackFont()
    {
        FontAsset = RDConstants.data.chineseFontTMPro;
        Loader.Log($"Using fallback font: {FontAsset?.name}");
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
