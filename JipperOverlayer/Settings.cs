using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityModManagerNet;
using JipperOverlayer.Overlayer;
using JipperOverlayer.Overlayer.Features;
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
    public int ComboColorMax = 1000;
    public bool JongyeolMode, ShowFPS = true, ShowAuthor = true, ShowState = true;
    public float FPSRefreshRate = 0.2f;
    public int JongyeolDecimalPrecision = 5;
    public bool HideDebugText = true, ShowDeath = true, ShowStart = true, ShowTiming = true;
    public bool RemoveNotRequireInAuto = true, CheckPseudo = true, AllowELCombo = true, AllowOrangeCombo = true;
    public Language CurrentLanguage;
    public int FontIndex;
    public string FontName;
    public bool CustomPositionsEnabled;
    public int MainAlign = 257, BPMAlign = 260, JudgeAlign = 1026;
    public int ComboAlign = 514, ComboValAlign = 258;
    public int TimingAlign = 1026, AttemptAlign = 1025;
    public int MainStyle, BPMStyle, JudgeStyle, ComboStyle, ComboValStyle, TimingStyle, AttemptStyle;
    public float MainPX = 0.008f, MainPY = 0.985f;
    public float BPMPX = 0.992f, BPMPY = 0.985f;
    public float JudgePX = 0.5f, JudgePY = 0.005f;
    public float P1JudgePX = 0.37f, P1JudgePY = 0.032f;
    public float P2JudgePX = 0.63f, P2JudgePY = 0.032f;
    public float P3JudgePX = 0.37f, P3JudgePY = 0.005f;
    public float P4JudgePX = 0.63f, P4JudgePY = 0.005f;
    public float ComboPX = 0.5f, ComboPY = 0.947f;
    public float TimingPX = 0.5f, TimingPY = 0.12f;
    public float AttmptPX = 0.661f, AttmptPY = 0.032f;
    public float ProgBarPX = 0.5f, ProgBarPY = 0.991f;
    public float MainOffsetX, MainOffsetY, BPMOffsetX, BPMOffsetY, JudgeOffsetX, JudgeOffsetY;
    public float P1JudgeOffsetX, P1JudgeOffsetY, P2JudgeOffsetX, P2JudgeOffsetY;
    public float P3JudgeOffsetX, P3JudgeOffsetY, P4JudgeOffsetX, P4JudgeOffsetY;
    public float ComboOffsetX, ComboOffsetY, TimingOffsetX, TimingOffsetY;
    public float AttemptOffsetX, AttemptOffsetY, AttemptCoopOffsetX, AttemptCoopOffsetY, ProgBarOffsetX, ProgBarOffsetY;
    public int ConfigVersion;

    [JsonIgnore] public ColorConfig Colors;
    [JsonIgnore] public LabelConfig Labels;

    public void OnGUI(UnityModManager.ModEntry modEntry)
    {
        DrawGeneralSection();
        DrawDisplaySection();
        DrawJongyeolSection();
        DrawTextSettings();
        DrawLabelsSection();
        Overlay.Instance?.RefreshVisibility();
    }

    void DrawGeneralSection()
    {
        if (GUILayout.Button($"{( _generalFold ? "▼" : "▷")} {Tr.Get(Tr.Key.General)}", GUI.skin.label, GUILayout.ExpandWidth(true)))
            _generalFold = !_generalFold;
        if (!_generalFold) return;

        Size = Slide(Tr.Get(Tr.Key.Size), Size, 0, 2, () => Overlayer.Overlay.Instance?.UpdateSize());

        GUILayout.BeginHorizontal();
        GUILayout.Label(Tr.Get(Tr.Key.LangLabel), GUILayout.Width(100));
        var langs = new[] { "English", "한국어", "中文" };
        int langIdx = (int)CurrentLanguage;
        int newLang = GUILayout.SelectionGrid(langIdx, langs, 3);
        if (newLang != langIdx) { CurrentLanguage = (Overlayer.Localization.Language)newLang; Overlayer.Overlay.Instance?.RefreshVisibility(); }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (GUILayout.Button($"{( _fontFold ? "▼" : "▷")} {Tr.Get(Tr.Key.Font)}", GUI.skin.label, GUILayout.ExpandWidth(false)))
            _fontFold = !_fontFold;
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

        CustomPositionsEnabled = Tog(Tr.Get(Tr.Key.CustomPositions), CustomPositionsEnabled);
        if (CustomPositionsEnabled)
        {
            PosGroup("Main/BPM", () =>
            {
                PosSlide2(Tr.Get(Tr.Key.PosMain), ref MainOffsetX, ref MainOffsetY);
                PosSlide2(Tr.Get(Tr.Key.PosBPM), ref BPMOffsetX, ref BPMOffsetY);
            });
            PosGroup(Tr.Get(Tr.Key.PosJudge), () =>
            {
                PosSlide2(Tr.Get(Tr.Key.PosJudge), ref JudgeOffsetX, ref JudgeOffsetY);
                if (VersionSafe.IsV141OrLater)
                {
                    PosSlide2(Tr.Get(Tr.Key.PosP1), ref P1JudgeOffsetX, ref P1JudgeOffsetY);
                    PosSlide2(Tr.Get(Tr.Key.PosP2), ref P2JudgeOffsetX, ref P2JudgeOffsetY);
                    PosSlide2(Tr.Get(Tr.Key.PosP3), ref P3JudgeOffsetX, ref P3JudgeOffsetY);
                    PosSlide2(Tr.Get(Tr.Key.PosP4), ref P4JudgeOffsetX, ref P4JudgeOffsetY);
                }
            });
            PosGroup(Tr.Get(Tr.Key.JudgementOther), () =>
            {
                PosSlide2(Tr.Get(Tr.Key.PosCombo), ref ComboOffsetX, ref ComboOffsetY);
                PosSlide2(Tr.Get(Tr.Key.PosTiming), ref TimingOffsetX, ref TimingOffsetY);
                PosSlide2(Tr.Get(Tr.Key.PosAttempt), ref AttemptOffsetX, ref AttemptOffsetY);
                if (VersionSafe.IsV141OrLater)
                    PosSlide2($"{Tr.Get(Tr.Key.PosAttempt)}\n{Tr.Get(Tr.Key.Coop)}", ref AttemptCoopOffsetX, ref AttemptCoopOffsetY);
                PosSlide2(Tr.Get(Tr.Key.PosProgBar), ref ProgBarOffsetX, ref ProgBarOffsetY);
            });
            if (GUILayout.Button(Tr.Get(Tr.Key.ResetPositions), GUILayout.ExpandWidth(false)))
                ResetCustomPos();
        }

        GUILayout.Space(5);
    }

    void DrawDisplaySection()
    {
        if (GUILayout.Button($"{( _displayFold ? "▼" : "▷")} {Tr.Get(Tr.Key.Display)}", GUI.skin.label, GUILayout.ExpandWidth(true)))
            _displayFold = !_displayFold;
        if (!_displayFold) return;

        DrawDisplaySub("progress", Tr.Get(Tr.Key.ProgressAccuracy), () =>
        {
            ShowProgress = Tog(Tr.Get(Tr.Key.ShowProgress), ShowProgress);
            if (ShowProgress) Colors.Progress.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateProgress()), Tr.Get(Tr.Key.ProgressColor),
                () => { Colors.Progress = new([(0f, Color.white), (1f, new Color(0.8745f, 0.7098f, 1f))]); Colors.Save(Main.Mod); });

            ShowAccuracy = Tog(Tr.Get(Tr.Key.ShowAccuracy), ShowAccuracy);
            if (ShowAccuracy) Colors.Accuracy.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateAccuracy()), Tr.Get(Tr.Key.AccuracyColor),
                () => { Colors.Accuracy = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.8549f, 0)); Colors.Save(Main.Mod); });

            ShowXAccuracy = Tog(Tr.Get(Tr.Key.ShowXAccuracy), ShowXAccuracy);
            if (ShowXAccuracy) Colors.XAccuracy.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateAccuracy()), Tr.Get(Tr.Key.XaccuracyColor),
                () => { Colors.XAccuracy = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.8549f, 0)); Colors.Save(Main.Mod); });
        });

        DrawDisplaySub("time", Tr.Get(Tr.Key.TimeSection), () =>
        {
            ShowMusicTime = Tog(Tr.Get(Tr.Key.ShowMusicTime), ShowMusicTime);
            if (ShowMusicTime) Colors.MusicTime.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateTime()), Tr.Get(Tr.Key.MusicTimeColor),
                () => { Colors.MusicTime = new([(1f, Color.white)]); Colors.Save(Main.Mod); });

            ShowMapTime = Tog(Tr.Get(Tr.Key.ShowMapTime), ShowMapTime);
            if (ShowMapTime) Colors.MapTime.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateTime()), Tr.Get(Tr.Key.MapTimeColor),
                () => { Colors.MapTime = new([(1f, Color.white)]); Colors.Save(Main.Mod); });

            ShowMapTimeIfNotMusic = Tog(Tr.Get(Tr.Key.ShowMapIfNo), ShowMapTimeIfNotMusic);
        });

        DrawDisplaySub("progbar", Tr.Get(Tr.Key.ProgressBarBest), () =>
        {
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
        });

        DrawDisplaySub("combo", Tr.Get(Tr.Key.ComboSection), () =>
        {
            ShowCombo = Tog(Tr.Get(Tr.Key.ShowCombo), ShowCombo);
            if (ShowCombo)
            {
                EnableAutoCombo = Tog(Tr.Get(Tr.Key.EnableAutoCombo), EnableAutoCombo);
                ComboColorMax = (int)Slide(Tr.Get(Tr.Key.ComboColorMax), ComboColorMax, 1, 5000, () => { });
                Colors.Combo.SettingGUI(ColorChanged(null), Tr.Get(Tr.Key.ComboColor));
            }
        });

        DrawDisplaySub("bpm", Tr.Get(Tr.Key.BpmSection), () =>
        {
            ShowBPM = Tog(Tr.Get(Tr.Key.ShowBpm), ShowBPM);
            if (ShowBPM)
            {
                BpmColorMax = Slide(Tr.Get(Tr.Key.BpmColorMax), BpmColorMax, 100, 20000, () => { });
                Colors.Bpm.SettingGUI(ColorChanged(() => Overlayer.Overlay.Instance?.UpdateBPM()), Tr.Get(Tr.Key.BpmColor));
            }
        });

        DrawDisplaySub("judge", Tr.Get(Tr.Key.JudgementOther), () =>
        {
            ShowJudgement = Tog(Tr.Get(Tr.Key.ShowJudgement), ShowJudgement);
            if (ShowJudgement) JudgementLocationUp = Tog(Tr.Get(Tr.Key.JudgementUp), JudgementLocationUp);
            ShowTimingScale = Tog(Tr.Get(Tr.Key.ShowTimingScale), ShowTimingScale);
            ShowAttempt = Tog(Tr.Get(Tr.Key.ShowAttempt), ShowAttempt);
            ShowFullAttempt = Tog(Tr.Get(Tr.Key.ShowFullAttempt), ShowFullAttempt);
        });

        GUILayout.Space(5);
    }

    void DrawDisplaySub(string key, string label, Action content)
    {
        bool expanded = _expandedDisplaySub == key;
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        if (GUILayout.Button($"{(expanded ? "▼" : "▷")} {label}", GUI.skin.label, GUILayout.ExpandWidth(true)))
            _expandedDisplaySub = expanded ? null : key;
        GUILayout.EndHorizontal();
        if (expanded)
        {
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
            GUILayout.Space(36);
            GUILayout.BeginVertical();
            content();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }

    void PosGroup(string label, Action content)
    {
        bool expanded = _expandedPos == label;
        if (GUILayout.Button($"{(expanded ? "▼" : "▷")} {label}", GUI.skin.label, GUILayout.ExpandWidth(true)))
            _expandedPos = expanded ? null : label;
        if (!expanded) return;
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        GUILayout.BeginVertical();
        content();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    void DrawTextSettings()
    {
        if (GUILayout.Button($"{( _alignFold ? "▼" : "▷")} {Tr.Get(Tr.Key.TextSettings)}", GUI.skin.label, GUILayout.ExpandWidth(true)))
            _alignFold = !_alignFold;
        if (!_alignFold) return;
        DrawAlignment(Tr.Get(Tr.Key.AlignMain), ref MainAlign, ref MainStyle);
        DrawAlignment(Tr.Get(Tr.Key.AlignBpm), ref BPMAlign, ref BPMStyle);
        DrawAlignment(Tr.Get(Tr.Key.AlignJudge), ref JudgeAlign, ref JudgeStyle);
        DrawAlignment(Tr.Get(Tr.Key.AlignCombo), ref ComboAlign, ref ComboStyle);
        DrawAlignment(Tr.Get(Tr.Key.AlignComboVal), ref ComboValAlign, ref ComboValStyle);
        DrawAlignment(Tr.Get(Tr.Key.AlignTiming), ref TimingAlign, ref TimingStyle);
        DrawAlignment(Tr.Get(Tr.Key.AlignAttempt), ref AttemptAlign, ref AttemptStyle);
        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        if (GUILayout.Button(Tr.Get(Tr.Key.ApplyAlignment))) { Overlayer.Overlay.Instance?.ApplyAlignment(); Overlayer.Overlay.Instance?.ApplyFontStyle(); }
        if (GUILayout.Button(Tr.Get(Tr.Key.AlignReset), GUILayout.Width(50))) { ResetAlignment(); ResetStyle(); Overlayer.Overlay.Instance?.ApplyAlignment(); Overlayer.Overlay.Instance?.ApplyFontStyle(); }
        GUILayout.EndHorizontal();
    }

    void DrawJongyeolSection()
    {
        bool prevJongyeol = JongyeolMode;
        JongyeolMode = Tog(Tr.Get(Tr.Key.JongyeolMode), JongyeolMode);
        if (JongyeolMode != prevJongyeol) { Main.RecreateOverlay(); PatchManager.RefreshPatches(); }
        if (!JongyeolMode) return;

        DrawDisplaySub("jDisplay", Tr.Get(Tr.Key.DisplayOptions), () =>
        {
            ShowFPS = Tog(Tr.Get(Tr.Key.ShowFps), ShowFPS);
            if (ShowFPS)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(36);
                FPSRefreshRate = Slide(Tr.Get(Tr.Key.FPSRefreshRate), FPSRefreshRate, 0.05f, 1f, () => { });
                GUILayout.EndHorizontal();
            }
            ShowAuthor = Tog(Tr.Get(Tr.Key.ShowAuthor), ShowAuthor);
            ShowState = Tog(Tr.Get(Tr.Key.ShowState), ShowState);
            ShowDeath = Tog(Tr.Get(Tr.Key.ShowDeath), ShowDeath);
            ShowStart = Tog(Tr.Get(Tr.Key.ShowStart), ShowStart);
            ShowTiming = Tog(Tr.Get(Tr.Key.ShowTiming), ShowTiming);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Tr.Get(Tr.Key.DecimalPrecision), GUILayout.Width(120));
            JongyeolDecimalPrecision = (int)GUILayout.HorizontalSlider(JongyeolDecimalPrecision, 0, 5);
            if (!_slideFields.TryGetValue("DecimalPrecision", out var dpText))
                _slideFields["DecimalPrecision"] = dpText = JongyeolDecimalPrecision.ToString();
            string newDpText = GUILayout.TextField(dpText, GUILayout.Width(55));
            if (newDpText != dpText)
            {
                _slideFields["DecimalPrecision"] = newDpText;
                if (int.TryParse(newDpText, out int dpParsed))
                    JongyeolDecimalPrecision = Mathf.Clamp(dpParsed, 0, 5);
            }
            else if (newDpText == dpText && JongyeolDecimalPrecision.ToString() != dpText)
                _slideFields["DecimalPrecision"] = JongyeolDecimalPrecision.ToString();
            GUILayout.EndHorizontal();
            var o = Overlay.Instance;
            if (o?.OverlayTextManager is OverlayTextManagerNormal n) n.DecimalPrecision = JongyeolDecimalPrecision;
            else if (o?.OverlayTextManager is OverlayTextManagerCoop c) c.DecimalPrecision = JongyeolDecimalPrecision;
            if (o?.Jongyeol != null) o.Jongyeol.DecimalPrecision = JongyeolDecimalPrecision;
        });

        DrawDisplaySub("jBehavior", Tr.Get(Tr.Key.BehaviorOptions), () =>
        {
            HideDebugText = Tog(Tr.Get(Tr.Key.HideDebugText), HideDebugText);
            RemoveNotRequireInAuto = Tog(Tr.Get(Tr.Key.RemoveAutoReq), RemoveNotRequireInAuto);
            CheckPseudo = Tog(Tr.Get(Tr.Key.CheckPseudo), CheckPseudo);
            bool prevEL = AllowELCombo;
            AllowELCombo = Tog(Tr.Get(Tr.Key.AllowELCombo), AllowELCombo);
            if (prevEL != AllowELCombo) { PatchManager.RefreshPatches(); if (!AllowELCombo) AllowOrangeCombo = false; }
            if (AllowELCombo) AllowOrangeCombo = Tog(Tr.Get(Tr.Key.AllowOrangeCombo), AllowOrangeCombo, 20);
        });
    }

    void ResetCustomPos()
    {
        MainOffsetX = MainOffsetY = BPMOffsetX = BPMOffsetY = JudgeOffsetX = JudgeOffsetY = 0;
        P1JudgeOffsetX = P1JudgeOffsetY = P2JudgeOffsetX = P2JudgeOffsetY = 0;
        P3JudgeOffsetX = P3JudgeOffsetY = P4JudgeOffsetX = P4JudgeOffsetY = 0;
        ComboOffsetX = ComboOffsetY = TimingOffsetX = TimingOffsetY = 0;
        AttemptOffsetX = AttemptOffsetY = AttemptCoopOffsetX = AttemptCoopOffsetY = ProgBarOffsetX = ProgBarOffsetY = 0;
        _slideFields.Clear();
        Overlayer.Overlay.Instance?.ApplyPositionOffsets();
    }

    void ResetAlignment()
    {
        MainAlign = 257; BPMAlign = 260; JudgeAlign = 1026;
        ComboAlign = 514; ComboValAlign = 258;
        TimingAlign = 1026; AttemptAlign = 1025;
    }

    void ResetStyle()
    {
        MainStyle = BPMStyle = JudgeStyle = ComboStyle = ComboValStyle = TimingStyle = AttemptStyle = 0;
    }

    static bool Tog(string label, bool v, int indent = 0)
    {
        GUILayout.BeginHorizontal();
        if (indent > 0) GUILayout.Space(indent);
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

    void PosSlide2(string label, ref float vx, ref float vy)
    {
        const float min = -2000, max = 2000;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(55));
        GUILayout.Label("X", GUILayout.Width(14));
        float nx = GUILayout.HorizontalSlider(vx, min, max, GUILayout.ExpandWidth(true));
        if (!_slideFields.TryGetValue(label + "X", out var tx))
            _slideFields[label + "X"] = tx = $"{(int)vx}";
        string ntx = GUILayout.TextField(tx, GUILayout.Width(42));
        if (ntx != tx) { _slideFields[label + "X"] = ntx; if (float.TryParse(ntx, out float p)) nx = Mathf.Clamp(p, min, max); }
        else if (ntx == tx && Math.Abs(nx - vx) > 0.001f) _slideFields[label + "X"] = $"{(int)nx}";
        GUILayout.Space(4);
        GUILayout.Label("Y", GUILayout.Width(14));
        float ny = GUILayout.HorizontalSlider(vy, min, max, GUILayout.ExpandWidth(true));
        if (!_slideFields.TryGetValue(label + "Y", out var ty))
            _slideFields[label + "Y"] = ty = $"{(int)vy}";
        string nty = GUILayout.TextField(ty, GUILayout.Width(42));
        if (nty != ty) { _slideFields[label + "Y"] = nty; if (float.TryParse(nty, out float p)) ny = Mathf.Clamp(p, min, max); }
        else if (nty == ty && Math.Abs(ny - vy) > 0.001f) _slideFields[label + "Y"] = $"{(int)ny}";
        GUILayout.EndHorizontal();
        if (Math.Abs(nx - vx) > 0.001f || Math.Abs(ny - vy) > 0.001f) { vx = nx; vy = ny; Overlayer.Overlay.Instance?.ApplyPositionOffsets(); }
    }

    static readonly Dictionary<string, string> _slideFields = new();
    private static bool _generalFold, _displayFold, _fontFold, _alignFold;
    private static string _expandedAlign, _expandedDisplaySub, _expandedPos;
    private static bool _labelsFold;

    void DrawLabelsSection()
    {
        if (GUILayout.Button($"{( _labelsFold ? "▼" : "▷")} {Tr.Get(Tr.Key.CustomLabels)}", GUI.skin.label, GUILayout.ExpandWidth(true)))
            _labelsFold = !_labelsFold;
        if (!_labelsFold) return;
        GUI.changed = false;

        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        if (GUILayout.Button("English Preset", GUILayout.ExpandWidth(false))) { Labels = LabelConfig.GetPreset(Language.English); Labels.Save(Main.Mod); }
        if (GUILayout.Button("한국어", GUILayout.ExpandWidth(false))) { Labels = LabelConfig.GetPreset(Language.Korean); Labels.Save(Main.Mod); }
        if (GUILayout.Button("中文", GUILayout.ExpandWidth(false))) { Labels = LabelConfig.GetPreset(Language.Chinese); Labels.Save(Main.Mod); }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GUILayout.Label("  -- Standard --");
        DrawLabelField("Progress", ref Labels.Progress);
        DrawLabelField("Accuracy", ref Labels.Accuracy);
        DrawLabelField("XAccuracy", ref Labels.XAccuracy);
        DrawLabelField("Music Time", ref Labels.MusicTime);
        DrawLabelField("Map Time", ref Labels.MapTime);
        DrawLabelField("CheckPoint", ref Labels.Checkpoint);
        DrawLabelField("Best", ref Labels.Best);
        DrawLabelField("TBPM", ref Labels.TBPM);
        DrawLabelField("CBPM", ref Labels.CBPM);
        DrawLabelField("KPS", ref Labels.KPS);
        DrawLabelField("Attempt", ref Labels.Attempt);
        DrawLabelField("Full Attempt", ref Labels.FullAttempt);
        DrawLabelField("Timing Scale", ref Labels.TimingScale);
        DrawLabelField("Combo Title", ref Labels.ComboTitle);
        DrawLabelField("Combo Title Alt", ref Labels.ComboTitleAlt);

        GUILayout.Space(3);
        GUILayout.Label("  -- Jongyeol --");
        DrawLabelField("FPS", ref Labels.FPS);
        DrawLabelField("Author", ref Labels.Author);
        DrawLabelField("State", ref Labels.State);
        DrawLabelField("Death", ref Labels.Death);
        DrawLabelField("Start", ref Labels.Start);
        DrawLabelField("Timing", ref Labels.Timing);

        GUILayout.Space(3);
        GUILayout.Label("  -- State Texts --");
        DrawLabelField("Waiting", ref Labels.StateWaiting);
        DrawLabelField("Auto Tile", ref Labels.StateAutoTile);
        DrawLabelField("Auto", ref Labels.StateAuto);
        DrawLabelField("Perfect Play", ref Labels.StatePerfectPlay);
        DrawLabelField("Completed", ref Labels.StateComplete);
        DrawLabelField("Clear", ref Labels.StateClear);
        DrawLabelField("No Miss", ref Labels.StateNoMiss);
        DrawLabelField("Perfectionist", ref Labels.StatePerfectionist);
        DrawLabelField("Suffix", ref Labels.StateSuffix);
        DrawLabelField("Mid Start", ref Labels.StateMidStart);

        if (GUI.changed)
        {
            var o = Overlay.Instance;
            if (o != null && o.GameObject.activeSelf)
            {
                o.ComboTitle.text = Main.Settings.Labels.ComboTitle;
                o.RefreshTimeLabels();
                o.UpdateProgress();
                o.UpdateTime();
                o.UpdateAccuracy();
                o.UpdateBPM();
                o.UpdateJudgement();
                o.UpdateCombo(GameLifecycleHelper.ComboCount, false);
                o.UpdateTimingScale();
                o.UpdateAttempts();
                if (Main.Settings.ShowBest) o.OverlayTextManager?.UpdateBest(o);
                if (Main.Settings.ShowCheckpoint) o.UpdateCheckPointText();
                if (Main.Settings.ShowProgressBar) o.UpdateProgressBar();
            }
        }
    }

    static void DrawLabelField(string label, ref string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.Label(label, GUILayout.Width(100));
        value = GUILayout.TextField(value, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
    }

    static Action ColorChanged(Action updateOverlay) => () => updateOverlay?.Invoke();

    static readonly int[] AlignValues = [257, 258, 260, 513, 514, 516, 1025, 1026, 1028];
    static readonly string[] AlignLabels = ["TL", "T", "TR", "L", "C", "R", "BL", "B", "BR"];

    static string AlignLabel(int v)
    {
        int idx = Array.IndexOf(AlignValues, v);
        return idx >= 0 ? AlignLabels[idx] : "C";
    }

    static void DrawAlignment(string label, ref int align, ref int style)
    {
        bool expanded = _expandedAlign == label;
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        GUILayout.Label(label, GUILayout.ExpandWidth(true));
        GUILayout.Label(AlignLabel(align), GUILayout.Width(28));
        if (GUILayout.Button(expanded ? "▲" : "▼", GUILayout.Width(20), GUILayout.Height(18)))
            _expandedAlign = expanded ? null : label;
        (int bit, Tr.Key key)[] styles = [(1, Tr.Key.StyleBold), (2, Tr.Key.StyleItalic), (4, Tr.Key.StyleUnderline), (64, Tr.Key.StyleStrike), (512, Tr.Key.StyleHighlight)];
        foreach (var st in styles)
        {
            bool active = (style & st.bit) != 0;
            bool now = GUILayout.Toggle(active, Tr.Get(st.key), GUILayout.Width(24), GUILayout.Height(18));
            if (now != active) { style = now ? (style | st.bit) : (style & ~st.bit); Overlayer.Overlay.Instance?.ApplyFontStyle(); }
        }
        GUILayout.EndHorizontal();

        if (!expanded) return;
        int idx = Array.IndexOf(AlignValues, align);
        if (idx < 0) idx = 4;
        GUILayout.BeginHorizontal();
        GUILayout.Space(24);
        int newIdx = GUILayout.SelectionGrid(idx, AlignLabels, 3, GUILayout.Height(60));
        GUILayout.EndHorizontal();
        if (newIdx != idx) { align = AlignValues[newIdx]; Overlayer.Overlay.Instance?.ApplyAlignment(); }
    }

    public void OnSaveGUI(UnityModManager.ModEntry modEntry) { Save(modEntry); Colors?.Save(modEntry); Labels?.Save(modEntry); }
    public override void Save(UnityModManager.ModEntry modEntry) { UnityModManagerNet.UnityModManager.ModSettings.Save<Settings>(this, modEntry); }
    public static Settings Load(UnityModManager.ModEntry modEntry)
    {
        var s = UnityModManagerNet.UnityModManager.ModSettings.Load<Settings>(modEntry);
        if (s.ConfigVersion < 2)
        {
            float Sw = 1920, Sh = 1080;
            s.MainOffsetX = s.MainPX * Sw - 16;
            s.MainOffsetY = (s.MainPY - 1f) * Sh + 16;
            s.BPMOffsetX = (s.BPMPX - 1f) * Sw + 16;
            s.BPMOffsetY = (s.BPMPY - 1f) * Sh + 16;
            s.JudgeOffsetX = (s.JudgePX - 0.5f) * Sw;
            s.JudgeOffsetY = s.JudgePY * Sh - (s.JudgementLocationUp ? 85f : 5f);
            s.ComboOffsetX = (s.ComboPX - 0.5f) * Sw;
            s.ComboOffsetY = (s.ComboPY - 1f) * Sh + 43f + 14f * s.Size;
            s.TimingOffsetX = (s.TimingPX - 0.5f) * Sw;
            s.TimingOffsetY = s.TimingPY * Sh - 90f - 40f * s.Size;
            s.AttemptOffsetX = (s.AttmptPX - 0.5f) * Sw - 310f;
            s.AttemptOffsetY = s.AttmptPY * Sh - 35f;
            s.ProgBarOffsetX = (s.ProgBarPX - 0.5f) * Sw;
            s.ProgBarOffsetY = (s.ProgBarPY - 1f) * Sh + 10f;
            s.ConfigVersion = 2;
            s.Save(modEntry);
        }
        s.Colors = ColorConfig.Load(modEntry);
        s.Labels = LabelConfig.Load(modEntry);
        return s;
    }
}
