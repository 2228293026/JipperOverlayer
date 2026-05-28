using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildBundles
{
    const string BundleName = "jipperoverlayerbundle";

    [MenuItem("Assets/Build Overlayer Bundle")]
    static void Build()
    {
        Assign("Assets/Font/MAPLESTORY_OTF_BOLD.OTF", BundleName);
        Assign("Assets/ProgressBar.prefab", BundleName);
        AssetDatabase.RemoveUnusedAssetBundleNames();

        string outputDir = "Assets/AssetBundles";
        Directory.CreateDirectory(outputDir);
        var opts = BuildAssetBundleOptions.AssetBundleStripUnityVersion;
        BuildPipeline.BuildAssetBundles(outputDir, opts, BuildTarget.StandaloneWindows64);

        Debug.Log($"Build done: {Path.GetFullPath(outputDir)}");
    }

    static void Assign(string path, string bundle)
    {
        var importer = AssetImporter.GetAtPath(path);
        if (importer == null) { Debug.LogError($"No importer for {path}"); return; }
        if (importer.assetBundleName != bundle)
        {
            importer.assetBundleName = bundle;
            importer.SaveAndReimport();
        }
    }
}
