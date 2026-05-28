using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityModManagerNet;
using JipperOverlayer.Overlayer.Settings;

namespace JipperOverlayer;

public class Settings : UnityModManager.ModSettings
{
    public bool ShowProgress = true, ShowAccuracy, ShowXAccuracy = true;
    public bool ShowMusicTime = true, ShowMapTime, ShowMapTimeIfNotMusic = true;
    public bool ShowCheckpoint, ShowBest, ShowProgressBar = true;
    public bool ShowBPM = true, ShowCombo = true, ShowJudgement = true, ShowTimingScale = true;
    public bool ShowAttempt = true, ShowFullAttempt = true;
    public float Size = 1f;
    public bool JudgementLocationUp, EnableAutoCombo = true;
    public float BpmColorMax = 8000f;
    public int ComboColorMax = 1000, TimeTextTypeValue;
    public bool JongyeolMode, ShowFPS = true, ShowAuthor = true, ShowState = true;
    public bool HideDebugText = true, ShowDeath = true, ShowStart = true, ShowTiming = true;
    public bool RemoveNotRequireInAuto = true, CheckPseudo = true, YellowCombo = true;

    [JsonIgnore] public ColorConfig Colors;

    public void OnGUI(UnityModManager.ModEntry modEntry)
    {
        Size = Slide("Size", Size, 0, 2, () => Overlayer.Overlay.Instance?.UpdateSize());

        // ===== Status section (matches original ResourcePack layout) =====
        ShowProgress = Tog("Show Progress", ShowProgress);
        if (ShowProgress) Colors.Progress.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgress()), "Progress Color",
            () => { Colors.Progress = new([(0f, Color.white), (1f, new Color(0.8745f, 0.7098f, 1f))]); Colors.Save(Main.Mod); });

        ShowAccuracy = Tog("Show Accuracy", ShowAccuracy);
        if (ShowAccuracy) Colors.Accuracy.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateAccuracy()), "Accuracy Color",
            () => { Colors.Accuracy = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.8549f, 0)); Colors.Save(Main.Mod); });

        ShowXAccuracy = Tog("Show XAccuracy", ShowXAccuracy);
        if (ShowXAccuracy) Colors.XAccuracy.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateAccuracy()), "XAccuracy Color",
            () => { Colors.XAccuracy = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.8549f, 0)); Colors.Save(Main.Mod); });

        ShowMusicTime = Tog("Show Music Time", ShowMusicTime);
        if (ShowMusicTime) Colors.MusicTime.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateTime()), "Music Time Color",
            () => { Colors.MusicTime = new([(1f, Color.white)]); Colors.Save(Main.Mod); });

        ShowMapTime = Tog("Show Map Time", ShowMapTime);
        if (ShowMapTime) Colors.MapTime.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateTime()), "Map Time Color",
            () => { Colors.MapTime = new([(1f, Color.white)]); Colors.Save(Main.Mod); });

        ShowMapTimeIfNotMusic = Tog("Show Map Time If No Music", ShowMapTimeIfNotMusic);

        // TimeTextType enum selector
        GUILayout.BeginHorizontal();
        GUILayout.Label("Time Text Type", GUILayout.Width(100));
        string[] timeTypes = ["Korean", "English"];
        int newTtt = GUILayout.SelectionGrid(TimeTextTypeValue, timeTypes, 2);
        if (newTtt != TimeTextTypeValue) { TimeTextTypeValue = newTtt; Overlayer.Overlay.Instance?.UpdateTime(); }
        GUILayout.EndHorizontal();

        ShowCheckpoint = Tog("Show Checkpoint", ShowCheckpoint);
        ShowBest = Tog("Show Best", ShowBest);
        if (ShowBest) Colors.Best.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgress()), "Best Color",
            () => { Colors.Best = new([(0f, Color.white), (1f, new Color(0.8745f, 0.7098f, 1f))]); Colors.Save(Main.Mod); });

        ShowProgressBar = Tog("Show Progress Bar", ShowProgressBar);
        if (ShowProgressBar)
        {
            Colors.ProgressBar.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgressBar()), "Progress Bar Color",
                () => { Colors.ProgressBar = new([(1f, new Color(0.9216f, 0.8039f, 0.9765f))]); Colors.Save(Main.Mod); });
            Colors.ProgressBarBackground.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgressBar()), "Progress Bar Background Color",
                () => { Colors.ProgressBarBackground = new([(1f, Color.white)]); Colors.Save(Main.Mod); });
            Colors.ProgressBarBorder.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgressBar()), "Progress Bar Border Color",
                () => { Colors.ProgressBarBorder = new([(1f, Color.black)]); Colors.Save(Main.Mod); });
        }

        GUILayout.Space(10);

        // ===== Combo (matches original Combo.cs layout) =====
        ShowCombo = Tog("Show Combo", ShowCombo);
        if (ShowCombo)
        {
            EnableAutoCombo = Tog("Enable Auto Combo", EnableAutoCombo);
            ComboColorMax = (int)Slide("Combo Color Max", ComboColorMax, 1, 5000, () => { });
            Colors.Combo.SettingGUI(ColorChanged(null), "Combo Color");
        }

        // ===== BPM (matches original BPM.cs layout) =====
        ShowBPM = Tog("Show BPM", ShowBPM);
        if (ShowBPM)
        {
            BpmColorMax = Slide("BPM Color Max", BpmColorMax, 100, 20000, () => { });
            Colors.Bpm.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateBPM()), "BPM Color");
        }

        // ===== Judgement =====
        ShowJudgement = Tog("Show Judgement", ShowJudgement);
        if (ShowJudgement) JudgementLocationUp = Tog("Judgement Location Up", JudgementLocationUp);

        // ===== Timing Scale =====
        ShowTimingScale = Tog("Show Timing Scale", ShowTimingScale);

        // ===== Attempt (matches original Attempt.cs layout) =====
        ShowAttempt = Tog("Show Attempt", ShowAttempt);
        ShowFullAttempt = Tog("Show Full Attempt", ShowFullAttempt);

        bool prevJongyeol = JongyeolMode;
        JongyeolMode = Tog("Jongyeol Mode", JongyeolMode);
        if (JongyeolMode != prevJongyeol) { Main.RecreateOverlay(); PatchManager.RefreshPatches(); }
        if (JongyeolMode)
        {
            ShowFPS = Tog("Show FPS", ShowFPS);
            ShowAuthor = Tog("Show Author", ShowAuthor);
            ShowState = Tog("Show State", ShowState);
            ShowDeath = Tog("Show Death", ShowDeath);
            ShowStart = Tog("Show Start", ShowStart);
            ShowTiming = Tog("Show Timing", ShowTiming);
            HideDebugText = Tog("Hide Debug Text", HideDebugText);
            RemoveNotRequireInAuto = Tog("Remove Not Required In Auto", RemoveNotRequireInAuto);
            CheckPseudo = Tog("Check Pseudo", CheckPseudo);
            YellowCombo = Tog("Yellow Combo", YellowCombo);
        }

        // Apply toggle changes to overlay
        Overlayer.Overlay.Instance?.RefreshVisibility();
    }

    static bool Tog(string label, bool v)
    {
        GUILayout.BeginHorizontal();
        v = GUILayout.Toggle(v, GUIContent.none, GUILayout.ExpandWidth(false));
        GUILayout.Label(label, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        return v;
    }

    static float Slide(string label, float v, float min, float max, Action onChange)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120));
        float nv = GUILayout.HorizontalSlider(v, min, max);
        GUILayout.Label(nv.ToString("F2"), GUILayout.Width(35));
        GUILayout.EndHorizontal();
        if (Math.Abs(nv - v) > 0.001f) { onChange?.Invoke(); return nv; }
        return v;
    }

    // Color change callback: update overlay only (saving handled by OnSaveGUI)
    static Action ColorChanged(Action updateOverlay) => () => {
        updateOverlay?.Invoke();
        Overlayer.Overlay.Instance?.RefreshVisibility();
    };

    public void OnSaveGUI(UnityModManager.ModEntry modEntry) { Save(modEntry); Colors?.Save(modEntry); }
    public override void Save(UnityModManager.ModEntry modEntry) { UnityModManagerNet.UnityModManager.ModSettings.Save<Settings>(this, modEntry); }
    public static Settings Load(UnityModManager.ModEntry modEntry)
    {
        var s = UnityModManagerNet.UnityModManager.ModSettings.Load<Settings>(modEntry);
        s.Colors = ColorConfig.Load(modEntry);
        return s;
    }
}

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
