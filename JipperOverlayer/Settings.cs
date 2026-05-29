using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityModManagerNet;
using JipperOverlayer.Overlayer;
using JipperOverlayer.Overlayer.Localization;
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
    public Language CurrentLanguage;
    public int FontIndex;
    public string FontName;
    public bool CustomPositionsEnabled;
    public float MainPX = 0.008f, MainPY = 0.985f;
    public float BPMPX = 0.992f, BPMPY = 0.985f;
    public float JudgePX = 0.5f, JudgePY = 0.005f;
    public float ComboPX = 0.5f, ComboPY = 0.947f;
    public float TimingPX = 0.5f, TimingPY = 0.12f;
    public float AttmptPX = 0.661f, AttmptPY = 0.032f;
    public float ProgBarPX = 0.5f, ProgBarPY = 0.991f;

    [JsonIgnore] public ColorConfig Colors;

    public void OnGUI(UnityModManager.ModEntry modEntry)
    {
        Size = Slide(Tr.Get(Tr.Key.Size), Size, 0, 2, () => Overlayer.Overlay.Instance?.UpdateSize());

        // Language selector
        GUILayout.BeginHorizontal();
        GUILayout.Label(Tr.Get(Tr.Key.LangLabel), GUILayout.Width(100));
        var langs = new[] { "English", "한국어", "中文" };
        int langIdx = (int)CurrentLanguage;
        int newLang = GUILayout.SelectionGrid(langIdx, langs, 3);
        if (newLang != langIdx) { CurrentLanguage = (Overlayer.Localization.Language)newLang; Overlayer.Overlay.Instance?.RefreshVisibility(); }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // ===== Font foldout =====
        _fontFold = GUILayout.Toggle(_fontFold, Tr.Get(Tr.Key.Font), GUILayout.ExpandWidth(false));
        if (_fontFold && FontManager.FontNames != null)
        {
            for (int i = 0; i < FontManager.FontNames.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                bool sel = FontIndex == i;
                bool now = GUILayout.Toggle(sel, GUIContent.none, GUILayout.ExpandWidth(false));
                GUILayout.Label(FontManager.FontNames[i], GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                if (now && !sel) { FontIndex = i; FontName = FontManager.FontNames[i]; Overlayer.Overlay.Instance?.ApplyFontToAll(); }
            }
        }

        // ===== Custom Positions =====
        CustomPositionsEnabled = Tog(Tr.Get(Tr.Key.CustomPositions), CustomPositionsEnabled);
        if (CustomPositionsEnabled)
        {
            PosSlide("  Main X", ref MainPX, 0, 1);
            PosSlide("  Main Y", ref MainPY, 0, 1);
            PosSlide("  BPM X", ref BPMPX, 0, 1);
            PosSlide("  BPM Y", ref BPMPY, 0, 1);
            PosSlide("  Judge X", ref JudgePX, 0, 1);
            PosSlide("  Judge Y", ref JudgePY, 0, 1);
            PosSlide("  Combo X", ref ComboPX, 0, 1);
            PosSlide("  Combo Y", ref ComboPY, 0, 1);
            PosSlide("  Timing X", ref TimingPX, 0, 1);
            PosSlide("  Timing Y", ref TimingPY, 0, 1);
            PosSlide("  Attmpt X", ref AttmptPX, 0, 1);
            PosSlide("  Attmpt Y", ref AttmptPY, 0, 1);
            PosSlide("  ProgBarX", ref ProgBarPX, 0, 1);
            PosSlide("  ProgBarY", ref ProgBarPY, 0, 1);

            if (GUILayout.Button(Tr.Get(Tr.Key.ResetPositions), GUILayout.ExpandWidth(false)))
                ResetCustomPos();
        }

        GUILayout.Space(5);

        // ===== Status section =====
        ShowProgress = Tog(Tr.Get(Tr.Key.ShowProgress), ShowProgress);
        if (ShowProgress) Colors.Progress.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgress()), Tr.Get(Tr.Key.ProgressColor),
            () => { Colors.Progress = new([(0f, Color.white), (1f, new Color(0.8745f, 0.7098f, 1f))]); Colors.Save(Main.Mod); });

        ShowAccuracy = Tog(Tr.Get(Tr.Key.ShowAccuracy), ShowAccuracy);
        if (ShowAccuracy) Colors.Accuracy.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateAccuracy()), Tr.Get(Tr.Key.AccuracyColor),
            () => { Colors.Accuracy = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.8549f, 0)); Colors.Save(Main.Mod); });

        ShowXAccuracy = Tog(Tr.Get(Tr.Key.ShowXAccuracy), ShowXAccuracy);
        if (ShowXAccuracy) Colors.XAccuracy.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateAccuracy()), Tr.Get(Tr.Key.XaccuracyColor),
            () => { Colors.XAccuracy = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.8549f, 0)); Colors.Save(Main.Mod); });

        ShowMusicTime = Tog(Tr.Get(Tr.Key.ShowMusicTime), ShowMusicTime);
        if (ShowMusicTime) Colors.MusicTime.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateTime()), Tr.Get(Tr.Key.MusicTimeColor),
            () => { Colors.MusicTime = new([(1f, Color.white)]); Colors.Save(Main.Mod); });

        ShowMapTime = Tog(Tr.Get(Tr.Key.ShowMapTime), ShowMapTime);
        if (ShowMapTime) Colors.MapTime.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateTime()), Tr.Get(Tr.Key.MapTimeColor),
            () => { Colors.MapTime = new([(1f, Color.white)]); Colors.Save(Main.Mod); });

        ShowMapTimeIfNotMusic = Tog(Tr.Get(Tr.Key.ShowMapIfNo), ShowMapTimeIfNotMusic);

        GUILayout.BeginHorizontal();
        GUILayout.Label(Tr.Get(Tr.Key.TimeTextType), GUILayout.Width(100));
        string[] timeTypes = [Tr.Get(Tr.Key.TimeTextKorean), Tr.Get(Tr.Key.TimeTextEnglish)];
        int newTtt = GUILayout.SelectionGrid(TimeTextTypeValue, timeTypes, 2);
        if (newTtt != TimeTextTypeValue) { TimeTextTypeValue = newTtt; Overlayer.Overlay.Instance?.UpdateTime(); }
        GUILayout.EndHorizontal();

        ShowCheckpoint = Tog(Tr.Get(Tr.Key.ShowCheckpoint), ShowCheckpoint);
        ShowBest = Tog(Tr.Get(Tr.Key.ShowBest), ShowBest);
        if (ShowBest) Colors.Best.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgress()), Tr.Get(Tr.Key.BestColor),
            () => { Colors.Best = new([(0f, Color.white), (1f, new Color(0.8745f, 0.7098f, 1f))]); Colors.Save(Main.Mod); });

        ShowProgressBar = Tog(Tr.Get(Tr.Key.ShowProgressBar), ShowProgressBar);
        if (ShowProgressBar)
        {
            Colors.ProgressBar.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgressBar()), Tr.Get(Tr.Key.ProgressBarColor),
                () => { Colors.ProgressBar = new([(1f, new Color(0.9216f, 0.8039f, 0.9765f))]); Colors.Save(Main.Mod); });
            Colors.ProgressBarBackground.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgressBar()), Tr.Get(Tr.Key.ProgressBarBgColor),
                () => { Colors.ProgressBarBackground = new([(1f, Color.white)]); Colors.Save(Main.Mod); });
            Colors.ProgressBarBorder.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgressBar()), Tr.Get(Tr.Key.ProgressBarBorderColor),
                () => { Colors.ProgressBarBorder = new([(1f, Color.black)]); Colors.Save(Main.Mod); });
        }

        GUILayout.Space(10);
        ShowCombo = Tog(Tr.Get(Tr.Key.ShowCombo), ShowCombo);
        if (ShowCombo)
        {
            EnableAutoCombo = Tog(Tr.Get(Tr.Key.EnableAutoCombo), EnableAutoCombo);
            ComboColorMax = (int)Slide(Tr.Get(Tr.Key.ComboColorMax), ComboColorMax, 1, 5000, () => { });
            Colors.Combo.SettingGUI(ColorChanged(null), Tr.Get(Tr.Key.ComboColor));
        }
        ShowBPM = Tog(Tr.Get(Tr.Key.ShowBpm), ShowBPM);
        if (ShowBPM)
        {
            BpmColorMax = Slide(Tr.Get(Tr.Key.BpmColorMax), BpmColorMax, 100, 20000, () => { });
            Colors.Bpm.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateBPM()), Tr.Get(Tr.Key.BpmColor));
        }
        ShowJudgement = Tog(Tr.Get(Tr.Key.ShowJudgement), ShowJudgement);
        if (ShowJudgement) JudgementLocationUp = Tog(Tr.Get(Tr.Key.JudgementUp), JudgementLocationUp);
        ShowTimingScale = Tog(Tr.Get(Tr.Key.ShowTimingScale), ShowTimingScale);
        ShowAttempt = Tog(Tr.Get(Tr.Key.ShowAttempt), ShowAttempt);
        ShowFullAttempt = Tog(Tr.Get(Tr.Key.ShowFullAttempt), ShowFullAttempt);

        bool prevJongyeol = JongyeolMode;
        JongyeolMode = Tog(Tr.Get(Tr.Key.JongyeolMode), JongyeolMode);
        if (JongyeolMode != prevJongyeol) { Main.RecreateOverlay(); PatchManager.RefreshPatches(); }
        if (JongyeolMode)
        {
            ShowFPS = Tog(Tr.Get(Tr.Key.ShowFps), ShowFPS);
            ShowAuthor = Tog(Tr.Get(Tr.Key.ShowAuthor), ShowAuthor);
            ShowState = Tog(Tr.Get(Tr.Key.ShowState), ShowState);
            ShowDeath = Tog(Tr.Get(Tr.Key.ShowDeath), ShowDeath);
            ShowStart = Tog(Tr.Get(Tr.Key.ShowStart), ShowStart);
            ShowTiming = Tog(Tr.Get(Tr.Key.ShowTiming), ShowTiming);
            HideDebugText = Tog(Tr.Get(Tr.Key.HideDebugText), HideDebugText);
            RemoveNotRequireInAuto = Tog(Tr.Get(Tr.Key.RemoveAutoReq), RemoveNotRequireInAuto);
            CheckPseudo = Tog(Tr.Get(Tr.Key.CheckPseudo), CheckPseudo);
            YellowCombo = Tog(Tr.Get(Tr.Key.YellowCombo), YellowCombo);
        }

        Overlayer.Overlay.Instance?.RefreshVisibility();
    }

    void ResetCustomPos()
    {
        MainPX = 0.008f; MainPY = 0.985f; BPMPX = 0.992f; BPMPY = 0.985f;
        JudgePX = 0.5f; JudgePY = 0.005f; ComboPX = 0.5f; ComboPY = 0.947f;
        TimingPX = 0.5f; TimingPY = 0.12f; AttmptPX = 0.661f; AttmptPY = 0.032f;
        ProgBarPX = 0.5f; ProgBarPY = 0.991f;
        Overlayer.Overlay.Instance?.ApplyPositionOffsets();
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
        if (!_slideFields.TryGetValue(label, out var text))
            _slideFields[label] = text = v.ToString("F2");
        string newText = GUILayout.TextField(text, GUILayout.Width(55));
        if (newText != text)
        {
            _slideFields[label] = newText;
            if (float.TryParse(newText, out float parsed))
                nv = Mathf.Clamp(parsed, min, max);
        }
        else if (newText == text && Math.Abs(nv - v) > 0.001f)
            _slideFields[label] = nv.ToString("F2");
        GUILayout.EndHorizontal();
        if (Math.Abs(nv - v) > 0.001f) { onChange?.Invoke(); return nv; }
        return v;
    }

    void PosSlide(string label, ref float v, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(70));
        float nv = GUILayout.HorizontalSlider(v, min, max, GUILayout.ExpandWidth(true));
        if (!_slideFields.TryGetValue(label, out var text))
            _slideFields[label] = text = v.ToString("F3");
        string newText = GUILayout.TextField(text, GUILayout.Width(50));
        if (newText != text)
        {
            _slideFields[label] = newText;
            if (float.TryParse(newText, out float parsed))
                nv = Mathf.Clamp(parsed, min, max);
        }
        else if (newText == text && Math.Abs(nv - v) > 0.001f)
            _slideFields[label] = nv.ToString("F3");
        GUILayout.EndHorizontal();
        if (Math.Abs(nv - v) > 0.001f) { v = nv; Overlayer.Overlay.Instance?.ApplyPositionOffsets(); }
    }

    static readonly Dictionary<string, string> _slideFields = new();
    private static bool _fontFold;

    static Action ColorChanged(Action updateOverlay) => () => updateOverlay?.Invoke();

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
