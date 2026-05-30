using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityModManagerNet;

namespace JipperOverlayer.Overlayer.Settings;

public class ColorConfig
{
    public ColorPerDictionary Progress = new([(0f, Color.white), (1f, new Color(0.8745f, 0.7098f, 1f))]);
    public ColorPerDictionary Accuracy = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.8549f, 0));
    public ColorPerDictionary XAccuracy = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.8549f, 0));
    public ColorPerDictionary MusicTime = new([(1f, Color.white)]);
    public ColorPerDictionary MapTime = new([(1f, Color.white)]);
    public ColorPerDictionary Best = new([(0f, Color.white), (1f, new Color(0.8745f, 0.7098f, 1f))]);
    public ColorPerDictionary Bpm = new([(0f, Color.white), (1f, Color.magenta)]);
    public ColorPerDictionary Combo = new([(0f, new Color(0.8745f, 0.7098f, 1f)), (1f, new Color(0.7176f, 0.3490f, 1f))]);
    public ColorPerDictionary ProgressBar = new([(1f, new Color(0.9216f, 0.8039f, 0.9765f))]);
    public ColorPerDictionary ProgressBarBackground = new([(1f, Color.white)]);
    public ColorPerDictionary ProgressBarBorder = new([(1f, Color.black)]);

    public Color GetProgressColor(float t) { return Progress.GetColor(t); }
    public void EnsureSorted() {
        Progress.EnsureSorted(); Accuracy.EnsureSorted(); XAccuracy.EnsureSorted();
        MusicTime.EnsureSorted(); MapTime.EnsureSorted(); Best.EnsureSorted();
        Bpm.EnsureSorted(); Combo.EnsureSorted(); ProgressBar.EnsureSorted();
        ProgressBarBackground.EnsureSorted(); ProgressBarBorder.EnsureSorted();
    }
    public Color GetAccuracyColor(float t, bool perfect) { return perfect ? new Color(1, 0.8549f, 0) : Accuracy.GetColor(t); }
    public Color GetXAccuracyColor(float t, bool perfect) { return perfect ? new Color(1, 0.8549f, 0) : XAccuracy.GetColor(t); }
    public Color GetMusicTimeColor(float t) { return MusicTime.GetColor(t); }
    public Color GetMapTimeColor(float t) { return MapTime.GetColor(t); }
    public Color GetBestColor(float t) { return Best.GetColor(t); }
    public Color GetBpmColor(float t) { return Bpm.GetColor(t); }
    public Color GetComboColor(float t) { return Combo.GetColor(t); }
    public Color GetProgressBarColor(float t) { return ProgressBar.GetColor(t); }
    public Color GetProgressBarBackgroundColor(float t) { return ProgressBarBackground.GetColor(t); }
    public Color GetProgressBarBorderColor(float t) { return ProgressBarBorder.GetColor(t); }

    public void Save(UnityModManager.ModEntry entry)
    {
        try { File.WriteAllText(Path.Combine(entry.Path, "colors.json"), JsonConvert.SerializeObject(this, Formatting.Indented)); }
        catch (Exception e) { Main.Mod?.Logger.Warning($"Save colors failed: {e.Message}"); }
    }

    public static ColorConfig Load(UnityModManager.ModEntry entry)
    {
        try {
            string p = Path.Combine(entry.Path, "colors.json");
            if (File.Exists(p)) {
                var jsonSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
                var cc = JsonConvert.DeserializeObject<ColorConfig>(File.ReadAllText(p), jsonSettings);
                if (cc != null) { cc.EnsureSorted(); return cc; }
            }
        }
        catch (Exception e) { Main.Mod?.Logger.Warning($"Load colors failed: {e.Message}"); }
        return new ColorConfig();
    }
}
