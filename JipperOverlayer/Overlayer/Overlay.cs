using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JipperOverlayer.Overlayer.Util;
using JipperOverlayer.Overlayer.Jongyeol;
using Object = UnityEngine.Object;

namespace JipperOverlayer.Overlayer;

public class Overlay
{
    public static Overlay Instance;
    public IOverlayTextManager OverlayTextManager;
    public GameObject GameObject;
    public Canvas Canvas;
    public TextMeshProUGUI ProgressText;
    public TextMeshProUGUI AccuracyText;
    public TextMeshProUGUI XAccuracyText;
    public TextMeshProUGUI TimeText;
    public TextMeshProUGUI MapTimeText;
    public TextMeshProUGUI CheckpointText;
    public TextMeshProUGUI AttemptText;
    public TextMeshProUGUI BestText;
    public RectTransform ComboTransform;
    public TextMeshProUGUI ComboTitle;
    public TextMeshProUGUI ComboText;
    public RectTransform ComboTextTransform;
    internal RectTransform _comboTitleTransform;
    public TextMeshProUGUI BPMText;
    public TextMeshProUGUI[] JudgementTexts = new TextMeshProUGUI[4];
    public TextMeshProUGUI TimingScaleText;
    public ProgressBar ProgressBar;
    public static readonly Color PurePerfectColor = new(1, 0.8549019607843137f, 0);
    public int[] Hit;
    private GameObject _mainContainer;
    private GameObject _bpmObject;
    private GameObject[] _judgementObjects = new GameObject[4];
    private GameObject _comboObject;
    private GameObject _timingScaleObject;
    private GameObject _attemptObject;
    private GameObject _progressBarObject;
    internal static scrEnableIfBeta BetaWatermark;
    internal static Vector2? BetaWatermarkOriginalPos;
    internal int LastTime = -1;
    internal int LastMapTime = -1;
    internal int StartTile;
    public int NoCheckStartTile;
    public int[] Checkpoints;
    internal float LastTileBpm = -1;
    internal float LastCurBpm = -1;
    internal bool SongPlaying;
    public float StartProgress;
    public bool AutoOnceEnabled;
    internal bool IsDeath;
    internal string MusicTimeCache;
    internal string MapTimeCache;
    public PlayCount.Hash LastHash;
    private float _lastSavedStartProgress = -1;
    public float LastMultiplier = 1f;
    internal string _musicTimeLabel;
    internal string _mapTimeLabel;
    public JongyeolModule Jongyeol;
    internal static readonly StringBuilder _textSb = new(256);

    private static readonly IReadOnlyList<TextMeshProUGUI> _emptyTexts = Array.Empty<TextMeshProUGUI>();
    protected IReadOnlyList<TextMeshProUGUI> ExtraTexts => Jongyeol?.ExtraTexts ?? _emptyTexts;

    public Overlay(bool enableJongyeol = false)
    {
        Instance = this;
        if (enableJongyeol) Jongyeol = new JongyeolModule(this);
        GameObject = new GameObject("JipperOverlayer Overlay");
        Canvas = GameObject.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = GameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        GameObject.SetActive(false);
        InitializeStatus();
        InitializeBPM();
        InitializeJudgement();
        InitializeCombo();
        InitializeProgressBar();
        InitializeTimingScale();
        InitializeAttempt();
        Jongyeol?.InitializeExtraTexts();
        OnChangePlayers();
        UpdateSize();
        var mono = GameObject.AddComponent<OverlayMono>();
        mono.Overlay = this;
        mono.enabled = false;
        RefreshTimeLabels();
        Object.DontDestroyOnLoad(GameObject);
        if (ADOBase.controller is { paused: false } && ADOBase.conductor is { isGameWorld: true })
            Show(0);
    }

    public void OnChangePlayers()
    {
        Hit = VersionSafe.GetHitMarginsCount();
        SetupTextManager();
    }

    protected void SetupTextManager()
    {
        var s = Main.Settings;
        OverlayTextManager = VersionSafe.IsCoopMode()
            ? new OverlayTextManagerCoop(this)
            : new OverlayTextManagerNormal();
        if (Jongyeol != null)
        {
            if (OverlayTextManager is OverlayTextManagerNormal normal) normal.DecimalPrecision = s.JongyeolDecimalPrecision;
            else if (OverlayTextManager is OverlayTextManagerCoop coop) coop.DecimalPrecision = s.JongyeolDecimalPrecision;
            Jongyeol.DecimalPrecision = s.JongyeolDecimalPrecision;
        }
    }

    protected void InitializeStatus()
    {
        var mainGo = new GameObject("Main"); _mainContainer = mainGo; var go = mainGo;
        var t = go.AddComponent<RectTransform>();
        t.SetParent(Canvas.transform);
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(0, 1);
        t.anchoredPosition = new Vector2(16, -16);
        t.sizeDelta = new Vector2(456, 100);
        SetupMainText("Progress", ref ProgressText);
        SetupMainText("Accuracy", ref AccuracyText);
        SetupMainText("XAccuracy", ref XAccuracyText);
        SetupMainText("MusicTime", ref TimeText);
        SetupMainText("MapTime", ref MapTimeText);
        SetupMainText("Checkpoint", ref CheckpointText);
        SetupMainText("Best", ref BestText);
    }

    internal void SetupMainText(string name, ref TextMeshProUGUI text)
    {
        var go = new GameObject(name);
        var t = go.AddComponent<RectTransform>();
        t.SetParent(_mainContainer.transform);
        t.anchorMin = t.anchorMax = new Vector2(0, 1);
        t.sizeDelta = new Vector2(456, 30);
        text = go.AddComponent<TextMeshProUGUI>();
        text.font = BundleLoader.FontAsset;
        text.fontSize = 25;
        ShadowManager.ApplyShadow(text);
    }

    public void SetupLocationMain()
    {
        if (Jongyeol != null) { Jongyeol.SetupLocation(); return; }
        int y = -15;
        var s = Main.Settings;
        SetupLocationMainText(ProgressText, s.ShowProgress, ref y);
        SetupLocationMainText(AccuracyText, s.ShowAccuracy, ref y);
        SetupLocationMainText(XAccuracyText, s.ShowXAccuracy, ref y);
        SetupLocationMainText(TimeText, s.ShowMusicTime, ref y);
        SetupLocationMainText(MapTimeText, s.ShowMapTime, ref y);
        SetupLocationMainText(CheckpointText,
            s.ShowCheckpoint &&
            (Checkpoints ??= CollectCheckpoints()).Length > 0, ref y);
        SetupLocationMainText(BestText, s.ShowBest, ref y);
        UpdateProgress();
        VersionSafe.CalculatePercentAcc();
        UpdateTime();
    }

    protected static void SetupLocationMainText(TextMeshProUGUI text, bool enabled, ref int y)
    {
        text.enabled = enabled;
        if (!enabled) return;
        text.rectTransform.anchoredPosition = new Vector2(228, y);
        y -= 35;
    }

    public void SetupLocationJudgement()
    {
        bool coop = VersionSafe.IsCoopMode() && scrPlayerManager.playerCount > 1;
        int count = coop ? Math.Min(scrPlayerManager.playerCount, 4) : 1;
        for (int i = 0; i < 4; i++)
        {
            if (JudgementTexts[i] == null) continue;
            bool active = i < count;
            JudgementTexts[i].enabled = active;
            _judgementObjects[i].SetActive(active);
            if (!active) continue;
            var rt = JudgementTexts[i].rectTransform;
            rt.sizeDelta = new Vector2(1000, 30);
            rt.anchoredPosition = coop
                ? new Vector2(i % 2 == 0 ? -250 : 250, 35 - 30 * (i / 2))
                : new Vector2(0, Main.Settings.JudgementLocationUp ? 85 : 5);
            JudgementTexts[i].alignment = TextAlignmentOptions.Center;
        }
    }
    protected void InitializeBPM()
    {
        var go = new GameObject("BPM");
        var t = go.AddComponent<RectTransform>();
        t.SetParent(Canvas.transform);
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(1, 1);
        t.anchoredPosition = new Vector2(-16, -16);
        t.sizeDelta = new Vector2(456, 90);
        BPMText = go.AddComponent<TextMeshProUGUI>();
        BPMText.font = BundleLoader.FontAsset;
        BPMText.alignment = TextAlignmentOptions.TopRight;
        BPMText.lineSpacing = 30;
        BPMText.fontSize = 25;
        ShadowManager.ApplyShadow(BPMText);
        _bpmObject = go;
    }

    private void InitializeJudgement()
    {
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject($"Judgement{i + 1}");
            var t = go.AddComponent<RectTransform>();
            t.SetParent(Canvas.transform);
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0);
            t.sizeDelta = new Vector2(1000, 30);
            JudgementTexts[i] = go.AddComponent<TextMeshProUGUI>();
            JudgementTexts[i].font = BundleLoader.FontAsset;
            JudgementTexts[i].fontSize = 25;
            JudgementTexts[i].color = new Color(0.8509804f, 0.345098f, 1);
            ShadowManager.ApplyShadow(JudgementTexts[i]);
            _judgementObjects[i] = go;
        }
        SetupLocationJudgement();
    }

    protected void InitializeCombo()
    {
        var go = new GameObject("Combo");
        var t = go.AddComponent<RectTransform>();
        t.SetParent(Canvas.transform);
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 1);
        t.sizeDelta = new Vector2(300, 200);
        ComboTransform = t;

        var title = new GameObject("ComboTitle");
        t = title.AddComponent<RectTransform>();
        t.SetParent(ComboTransform);
        t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.45f);
        t.pivot = new Vector2(0.5f, 0);
        t.sizeDelta = new Vector2(300, 0);
        _comboTitleTransform = t;
        ComboTitle = title.AddComponent<TextMeshProUGUI>();
        ComboTitle.font = BundleLoader.FontAsset;
        ComboTitle.fontSize = 40;
        ComboTitle.text = Main.Settings.Labels.ComboTitle;
        ComboTitle.alignment = TextAlignmentOptions.Center;
        var fitter = title.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ShadowManager.ApplyDarkShadow(ComboTitle);

        var val = new GameObject("ComboValue");
        t = val.AddComponent<RectTransform>();
        t.SetParent(ComboTransform);
        t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.45f);
        t.anchoredPosition = Vector2.zero;
        t.sizeDelta = new Vector2(300, 0);
        ComboTextTransform = t;
        ComboText = val.AddComponent<TextMeshProUGUI>();
        ComboText.font = BundleLoader.FontAsset;
        ComboText.fontSize = 108;
        ComboText.alignment = TextAlignmentOptions.Top;
        fitter = val.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ShadowManager.ApplyDarkShadow(ComboText);
        _comboObject = go;
    }

    protected void InitializeProgressBar()
    {
        if (BundleLoader.ProgressObject == null) return;
        var go = Object.Instantiate(BundleLoader.ProgressObject);
        var t = go.GetComponent<RectTransform>();
        t.SetParent(Canvas.transform);
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 1);
        t.anchoredPosition = new Vector2(0, -10);
        t.sizeDelta = new Vector2(642, 18);
        ProgressBar = new ProgressBar(t);
        _progressBarObject = go;
    }

    protected void InitializeTimingScale()
    {
        var go = new GameObject("TimingScale");
        var t = go.AddComponent<RectTransform>();
        t.SetParent(Canvas.transform);
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0);
        t.sizeDelta = new Vector2(300, 30);
        TimingScaleText = go.AddComponent<TextMeshProUGUI>();
        TimingScaleText.font = BundleLoader.FontAsset;
        TimingScaleText.fontSize = 20;
        TimingScaleText.alignment = TextAlignmentOptions.Bottom;
        ShadowManager.ApplyShadow(TimingScaleText);
        _timingScaleObject = go;
    }

    protected void InitializeAttempt()
    {
        var go = new GameObject("Attempt");
        var t = go.AddComponent<RectTransform>();
        t.SetParent(Canvas.transform);
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0);
        t.anchoredPosition = new Vector2(310, 35);
        t.sizeDelta = new Vector2(300, 30);
        AttemptText = go.AddComponent<TextMeshProUGUI>();
        AttemptText.font = BundleLoader.FontAsset;
        AttemptText.fontSize = 25;
        AttemptText.alignment = TextAlignmentOptions.BottomLeft;
        ShadowManager.ApplyShadow(AttemptText);
        _attemptObject = go;
    }

    private static float? _originalLevelNameWidth;

    public void UpdateSize()
    {
        var t = GameObject.transform;
        float size = Main.Settings.Size;
        var scale = new Vector3(size, size, 1);
        for (int i = 0; i < t.childCount; i++) t.GetChild(i).localScale = scale;
        if (TimingScaleText) TimingScaleText.rectTransform.anchoredPosition = new Vector2(0, 90 + 40 * size);
        var levelName = ADOBase.controller?.txtLevelName;
        if (levelName)
        {
            var rt = levelName.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -20 - 7 * size);
            rt.localScale = new Vector3(0.5f * size, 0.5f * size);
            // Track original width once for idempotent 2.5x widening
            if (_originalLevelNameWidth == null)
                _originalLevelNameWidth = Math.Abs(rt.sizeDelta.x);
            rt.sizeDelta = new Vector2(_originalLevelNameWidth.Value * 2.5f, rt.sizeDelta.y);
            levelName.text = levelName.text.Replace('\n', ' ');
        }
        if (ComboTransform) ComboTransform.anchoredPosition = new Vector2(0, -43 - 14 * size);
        if (GameObject.activeSelf)
        {
            AdjustBetaWatermark(size);
            RepositionAutoText(_mainContainer != null && _mainContainer.activeSelf, size);
        }
    }

    public void ApplyFontToAll()
    {
        var font = FontManager.GetFont(Main.Settings.FontIndex);
        if (font == null) return;
        ShadowManager.ClearCache();
        if (ProgressText) ProgressText.font = font;
        if (AccuracyText) AccuracyText.font = font;
        if (XAccuracyText) XAccuracyText.font = font;
        if (TimeText) TimeText.font = font;
        if (MapTimeText) MapTimeText.font = font;
        if (CheckpointText) CheckpointText.font = font;
        if (BestText) BestText.font = font;
        if (BPMText) BPMText.font = font;
        foreach (var jt in JudgementTexts) if (jt) jt.font = font;
        if (ComboTitle) ComboTitle.font = font;
        if (ComboText) ComboText.font = font;
        if (TimingScaleText) TimingScaleText.font = font;
        if (AttemptText) AttemptText.font = font;
        foreach (var t in ExtraTexts)
            if (t) t.font = font;
        foreach (var t in new[] { ProgressText, AccuracyText, XAccuracyText, TimeText, MapTimeText, CheckpointText, BestText,
            BPMText, TimingScaleText, AttemptText })
        { if (t) ShadowManager.ApplyShadow(t); }
        foreach (var jt in JudgementTexts)
        { if (jt) ShadowManager.ApplyShadow(jt); }
        foreach (var t in ExtraTexts)
        { if (t) ShadowManager.ApplyShadow(t); }
        if (ComboTitle) ShadowManager.ApplyDarkShadow(ComboTitle);
        if (ComboText) ShadowManager.ApplyDarkShadow(ComboText);
    }

    public void ApplyAlignment()
    {
        var s = Main.Settings;
        var mainAlign = (TextAlignmentOptions)s.MainAlign;
        if (BPMText) BPMText.alignment = (TextAlignmentOptions)s.BPMAlign;
        var judgeAlign = (TextAlignmentOptions)s.JudgeAlign;
        if (JudgementTexts[0]) JudgementTexts[0].alignment = judgeAlign;
        if (ComboTitle) ComboTitle.alignment = (TextAlignmentOptions)s.ComboAlign;
        if (ComboText) ComboText.alignment = (TextAlignmentOptions)s.ComboValAlign;
        if (TimingScaleText) TimingScaleText.alignment = (TextAlignmentOptions)s.TimingAlign;
        if (AttemptText) AttemptText.alignment = (TextAlignmentOptions)s.AttemptAlign;
        if (ProgressText) ProgressText.alignment = mainAlign;
        if (AccuracyText) AccuracyText.alignment = mainAlign;
        if (XAccuracyText) XAccuracyText.alignment = mainAlign;
        if (TimeText) TimeText.alignment = mainAlign;
        if (MapTimeText) MapTimeText.alignment = mainAlign;
        if (CheckpointText) CheckpointText.alignment = mainAlign;
        if (BestText) BestText.alignment = mainAlign;
        foreach (var t in ExtraTexts)
            if (t) t.alignment = mainAlign;
    }

    public void ApplyFontStyle()
    {
        var s = Main.Settings;
        var mainStyle = (FontStyles)s.MainStyle;
        if (ProgressText) ProgressText.fontStyle = mainStyle;
        if (AccuracyText) AccuracyText.fontStyle = mainStyle;
        if (XAccuracyText) XAccuracyText.fontStyle = mainStyle;
        if (TimeText) TimeText.fontStyle = mainStyle;
        if (MapTimeText) MapTimeText.fontStyle = mainStyle;
        if (CheckpointText) CheckpointText.fontStyle = mainStyle;
        if (BestText) BestText.fontStyle = mainStyle;
        if (BPMText) BPMText.fontStyle = (FontStyles)s.BPMStyle;
        var judgeStyle = (FontStyles)s.JudgeStyle;
        foreach (var jt in JudgementTexts) if (jt) jt.fontStyle = judgeStyle;
        if (ComboTitle) ComboTitle.fontStyle = (FontStyles)s.ComboStyle;
        if (ComboText) ComboText.fontStyle = (FontStyles)s.ComboValStyle;
        if (TimingScaleText) TimingScaleText.fontStyle = (FontStyles)s.TimingStyle;
        if (AttemptText) AttemptText.fontStyle = (FontStyles)s.AttemptStyle;
        foreach (var t in ExtraTexts)
            if (t) t.fontStyle = mainStyle;
    }

    public void ApplyPositionOffsets()
    {
        var s = Main.Settings;
        // Reset to default anchored positions
        if (_mainContainer)
            _mainContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(16, -16);
        if (_bpmObject)
            _bpmObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16, -16);
        if (ComboTransform)
            ComboTransform.anchoredPosition = new Vector2(0, -43 - 14 * s.Size);
        if (_timingScaleObject)
            TimingScaleText.rectTransform.anchoredPosition = new Vector2(0, 90 + 40 * s.Size);
        if (_attemptObject)
            _attemptObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(VersionSafe.IsCoopMode() && scrPlayerManager.playerCount > 1 ? 550 : 310, 35);
        if (_progressBarObject)
            _progressBarObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

        if (!s.CustomPositionsEnabled) return;
        var o = Main.Settings;
        if (_mainContainer)
            _mainContainer.GetComponent<RectTransform>().position += new Vector3(o.MainOffsetX, o.MainOffsetY, 0);
        if (_bpmObject)
            _bpmObject.GetComponent<RectTransform>().position += new Vector3(o.BPMOffsetX, o.BPMOffsetY, 0);
        for (int i = 0; i < 4; i++)
        {
            if (!_judgementObjects[i]) continue;
            bool coop = VersionSafe.IsCoopMode() && scrPlayerManager.playerCount > 1;
            float ox, oy;
            if (i == 0 && !coop) { ox = o.JudgeOffsetX; oy = o.JudgeOffsetY; }
            else { ox = i == 0 ? o.P1JudgeOffsetX : i == 1 ? o.P2JudgeOffsetX : i == 2 ? o.P3JudgeOffsetX : o.P4JudgeOffsetX;
                oy = i == 0 ? o.P1JudgeOffsetY : i == 1 ? o.P2JudgeOffsetY : i == 2 ? o.P3JudgeOffsetY : o.P4JudgeOffsetY; }
            JudgementTexts[i].rectTransform.position += new Vector3(ox, oy, 0);
        }
        if (ComboTransform)
            ComboTransform.position += new Vector3(o.ComboOffsetX, o.ComboOffsetY, 0);
        if (_timingScaleObject)
            TimingScaleText.rectTransform.position += new Vector3(o.TimingOffsetX, o.TimingOffsetY, 0);
        if (_attemptObject)
        {
            bool coop = VersionSafe.IsCoopMode() && scrPlayerManager.playerCount > 1;
            _attemptObject.GetComponent<RectTransform>().position += new Vector3(coop ? o.AttemptCoopOffsetX : o.AttemptOffsetX, coop ? o.AttemptCoopOffsetY : o.AttemptOffsetY, 0);
        }
        if (_progressBarObject)
            _progressBarObject.GetComponent<RectTransform>().position += new Vector3(o.ProgBarOffsetX, o.ProgBarOffsetY, 0);
    }

    public void RefreshVisibility()
    {
        var s = Main.Settings;
        if (_mainContainer) _mainContainer.SetActive(s.ShowProgress || s.ShowAccuracy || s.ShowXAccuracy || s.ShowMusicTime || s.ShowMapTime || s.ShowCheckpoint || s.ShowBest);
        if (_bpmObject) { _bpmObject.SetActive(s.ShowBPM); if (s.ShowBPM && GameObject.activeSelf) UpdateBPM(); }
        for (int i = 0; i < 4; i++) if (_judgementObjects[i]) _judgementObjects[i].SetActive(s.ShowJudgement && i < (VersionSafe.IsCoopMode() && scrPlayerManager.playerCount > 1 ? Math.Min(scrPlayerManager.playerCount, 4) : 1)); if (s.ShowJudgement) { SetupLocationJudgement(); if (GameObject.activeSelf) UpdateJudgement(); }
        if (_comboObject) { _comboObject.SetActive(s.ShowCombo); if (s.ShowCombo && GameObject.activeSelf) UpdateCombo(Features.GameLifecycleHelper.ComboCount, false); }
        if (_timingScaleObject) { _timingScaleObject.SetActive(s.ShowTimingScale); if (s.ShowTimingScale && GameObject.activeSelf) UpdateTimingScale(); }
        if (_attemptObject) { _attemptObject.SetActive(s.ShowAttempt || s.ShowFullAttempt); if (_attemptObject.activeSelf) UpdateAttempts(); }
        if (_progressBarObject) { _progressBarObject.SetActive(s.ShowProgressBar); if (s.ShowProgressBar && GameObject.activeSelf) UpdateProgressBar(); }
        if (GameObject != null && GameObject.activeSelf) SetupLocationMain();
        if (GameObject != null && GameObject.activeSelf && ADOBase.controller is { paused: false }) AdjustBetaWatermark(s.Size);
        ApplyPositionOffsets();
        ApplyAlignment();
        ApplyFontStyle();
        RepositionAutoText(_mainContainer != null && _mainContainer.activeSelf, s.Size);
        RefreshTimeLabels();
    }

    private static void AdjustBetaWatermark(float size)
    {
        if (BetaWatermark == null || !BetaWatermark.gameObject.activeInHierarchy) return;
        var rt = BetaWatermark.GetComponent<RectTransform>();
        if (rt == null) return;
        if (BetaWatermarkOriginalPos == null)
            BetaWatermarkOriginalPos = rt.anchoredPosition;
        var pos = rt.anchoredPosition;
        pos.y = BetaWatermarkOriginalPos.Value.y - (Main.Settings.ShowBPM ? 110f * size : 0);
        rt.anchoredPosition = pos;
    }

    private static void ResetBetaWatermark()
    {
        if (BetaWatermark == null || BetaWatermarkOriginalPos == null) return;
        var rt = BetaWatermark.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchoredPosition = BetaWatermarkOriginalPos.Value;
    }

    internal static int[] CollectCheckpoints()
    {
        var floors = scrLevelMaker.instance.listFloors;
        int count = 0;
        for (int i = 0; i < floors.Count; i++)
            if (floors[i].GetComponent<ffxCheckpoint>()) count++;
        int[] result = new int[count];
        int idx = 0;
        for (int i = 0; i < floors.Count; i++)
            if (floors[i].GetComponent<ffxCheckpoint>()) result[idx++] = floors[i].seqID;
        return result;
    }


    public void UpdateAccuracy(int index = -1)
    {
        if (!GameObject.activeSelf) return;
        OverlayTextManager?.UpdateAccuracy(this, index);
    }

    public void UpdateProgress(scrPlanet planet = null)
    {
        var s = Main.Settings;
        if (!GameObject.activeSelf) return;
        OverlayTextManager?.CacheProgress(planet);
        if (s.ShowProgress) OverlayTextManager?.UpdateProgress(this);
        if (s.ShowCheckpoint) UpdateCheckPointText();
        if (s.ShowProgressBar) UpdateProgressBar();
        if (s.ShowBest) OverlayTextManager?.UpdateBest(this);
        Jongyeol?.CheckPurePerfect();
        Jongyeol?.UpdateState();
        Jongyeol?.UpdateDeath();
        Jongyeol?.UpdateStart();
    }

    public void UpdateProgressBar()
    {
        try { if (ProgressBar?.LineTransform != null) OverlayTextManager?.UpdateProgressBar(this); }
        catch (Exception e) { Main.Mod.Logger.Warning($"ProgressBar: {e.Message}"); }
    }

    public void UpdateCheckPointText()
    {
        if (Checkpoints == null || Checkpoints.Length == 0) return;
        OverlayTextManager?.UpdateCheckpoint(this);
    }

    public void UpdateAttempts()
    {
        var s = Main.Settings;
        var labels = s.Labels;
        string v0 = "", v1 = "";
        int count = 0;
        if (s.ShowAttempt)
            v0 = count++ == 0 ? $"{labels.Attempt} {PlayCount.GetData(LastHash)?.GetAttempts(StartProgress) ?? 0}" : "";
        if (s.ShowFullAttempt)
            v1 = count++ <= 1 ? $"{labels.FullAttempt} {PlayCount.GetData(LastHash)?.GetAttempts() ?? 0}" : "";
        AttemptText.text = count switch { 0 => "", 1 => v0 + v1, _ => $"{v0}\n{v1}" };
    }

    public void UpdateJudgement()
    {
        if (!GameObject.activeSelf || Hit == null) return;
        if (VersionSafe.IsCoopMode() && scrMistakesManager.marginTrackers != null)
        {
            int count = Math.Min(Math.Min(scrPlayerManager.playerCount, scrMistakesManager.marginTrackers.Length), 4);
            for (int p = 0; p < count; p++)
            {
                if (JudgementTexts[p] == null) continue;
                _textSb.Clear();
                var h = scrMistakesManager.marginTrackers[p].hitMarginsCount;
                string hex = ColorUtility.ToHtmlStringRGB(scrPlayerManager.playerColors[p].ToRealColor());
                _textSb.Append($"<color=#{hex}>P{p + 1}</color> ");
                AppendJudgementLine(_textSb, h);
                _textSb.Append($" <color=#{hex}>P{p + 1}</color>");
                JudgementTexts[p].text = _textSb.ToString();
            }
        }
        else
        {
            _textSb.Clear();
            AppendJudgementLine(_textSb, Hit);
            JudgementTexts[0].text = _textSb.ToString();
        }
    }

    private static void AppendJudgementLine(StringBuilder sb, int[] h)
    {
        sb.Append(h[9]);
        sb.Append(" <color=red>");
        sb.Append(h[0]);
        sb.Append(" <color=#FF6F4E>");
        sb.Append(h[1]);
        sb.Append(" <color=#A0FF4E>");
        sb.Append(h[2]);
        sb.Append(" <color=#60FF4E>");
        sb.Append(h[3] + (h.Length > 10 ? h[10] : 0));
        sb.Append("</color> ");
        sb.Append(h[4]);
        sb.Append("</color> ");
        sb.Append(h[5]);
        sb.Append("</color> ");
        sb.Append(h[6]);
        sb.Append("</color> ");
        sb.Append(h[8]);
    }

    public void UpdateTime()
    {
        if (Jongyeol != null) { Jongyeol.UpdateTime(); return; }
        if (!GameObject.activeSelf || IsDeath) return;
        var s = Main.Settings;
        bool requireMusicToMap = false;
        if (s.ShowMusicTime)
        {
            var song = scrConductor.instance.song;
            if (!song?.clip && s.ShowMapTimeIfNotMusic) requireMusicToMap = true;
            else
            {
                float time = song!.time;
                float totalTime = song.clip?.length ?? 0;
                if (LastTime == (int)time) return;
                bool hourNeed = totalTime >= 3600;
                MusicTimeCache ??= TimeFormatter.Format(totalTime, hourNeed);
                string timeStr;
                if (time == 0 && SongPlaying) timeStr = MusicTimeCache;
                else if (time > 0) { SongPlaying = true; timeStr = TimeFormatter.Format(time, hourNeed); }
                else timeStr = TimeFormatter.Format(time, hourNeed);
                TimeText.text = $"{_musicTimeLabel} {timeStr}~{MusicTimeCache}";
                LastTime = (int)time;
                TimeText.color = s.Colors.GetMusicTimeColor(time / totalTime);
            }
        }
        if (s.ShowMapTime || requireMusicToMap)
        {
            float time = (float)(scrConductor.instance.addoffset + scrConductor.instance.songposition_minusi);
            var floors = scrLevelMaker.instance.listFloors;
            float totalTime = (float)floors[floors.Count - 1].entryTime;
            if (time < 0) time = 0; else if (time > totalTime) time = totalTime;
            if ((!s.ShowMapTime || LastMapTime == (int)time) && (!requireMusicToMap || LastTime == (int)time)) return;
            bool hourNeed = totalTime >= 3600;
            MapTimeCache ??= TimeFormatter.Format(totalTime, hourNeed);
            string tStr = time == totalTime ? MapTimeCache : TimeFormatter.Format(time, hourNeed);
            string txt = $"{_mapTimeLabel} {tStr}~{MapTimeCache}";
            if (s.ShowMapTime) { MapTimeText.text = txt; LastMapTime = (int)time; MapTimeText.color = s.Colors.GetMapTimeColor(time / totalTime); }
            if (requireMusicToMap) { TimeText.text = txt; LastTime = (int)time; TimeText.color = s.Colors.GetMusicTimeColor(time / totalTime); }
        }
    }


    public void UpdateCombo(int combo, bool bump)
    {
        if (!GameObject.activeSelf) return;
        ComboText.text = combo.ToString();
        ComboText.color = UpdateComboColor(combo);
        if (bump) { var m = GameObject.GetComponent<OverlayMono>(); if (m) m.StartComboBump(); }
        else { var m = GameObject.GetComponent<OverlayMono>(); if (m) m.StopComboBump(); ComboText.fontSize = 78; if (_comboTitleTransform) _comboTitleTransform.anchoredPosition = new Vector2(0, 43.505f); }
    }

    public Color UpdateComboColor(int combo)
    {
        var s = Main.Settings;
        if (Jongyeol != null) return Jongyeol.UpdateComboColor(combo);
        if (combo > s.ComboColorMax) combo = s.ComboColorMax;
        return s.Colors.GetComboColor((float)combo / s.ComboColorMax);
    }

    public void OnNonPerfectHit() { Jongyeol?.OnNonPerfectHit(); }

    public void UpdateBPM()
    {
        var s = Main.Settings;
        if (Jongyeol != null) { Jongyeol.UpdateBPM(); return; }
        if (!GameObject.activeSelf) return;
        var floor = scrController.instance.currFloor ?? scrController.instance.firstFloor;
        if (floor == null) return;
        var bpm = BpmCalculator.Calculate(floor, (float)(scrConductor.instance.song.pitch * VersionSafe.GetPlanetSpeed(scrController.instance)));
        if (LastTileBpm == bpm.TileBpm && LastCurBpm == bpm.CurrentBpm) return;
        string hex = BpmCalculator.ColorToHex(s.Colors.GetBpmColor(bpm.TileBpm / s.BpmColorMax));
        _textSb.Clear();
        var labels = s.Labels;
        _textSb.Append("<color=white>");
        _textSb.Append(labels.TBPM);
        _textSb.Append(" | <color=#");
        _textSb.Append(hex);
        _textSb.Append('>');
        _textSb.Append(Math.Round(bpm.TileBpm, 2));
        _textSb.Append("</color>\n");
        _textSb.Append(labels.CBPM);
        _textSb.Append(" |</color> ");
        _textSb.Append(Math.Round(bpm.CurrentBpm, 2));
        _textSb.Append("\n<color=white>");
        _textSb.Append(labels.KPS);
        _textSb.Append(" |</color> ");
        _textSb.Append(Math.Round(bpm.Kps, 2));
        BPMText.text = _textSb.ToString();
        if (LastCurBpm != bpm.CurrentBpm) BPMText.color = s.Colors.GetBpmColor(bpm.CurrentBpm / s.BpmColorMax);
        LastTileBpm = bpm.TileBpm; LastCurBpm = bpm.CurrentBpm;
    }

    internal void RefreshTimeLabels()
    {
        var s = Main.Settings;
        _musicTimeLabel = $"<color=white>{s.Labels.MusicTime} |</color>";
        _mapTimeLabel = $"<color=white>{s.Labels.MapTime} |</color>";
    }

    private static scrShowIfDebug _autoText;
    private static Vector2? _autoTextOriginalPos;

    private static void RepositionAutoText(bool needRoom, float size = 1)
    {
        if (_autoText == null)
        {
            var all = Resources.FindObjectsOfTypeAll<scrShowIfDebug>();
            foreach (var s in all)
            {
                if (!s.gameObject.scene.IsValid()) continue;
                if (!VersionSafe.GetHideWithNoAuto(s))
                    continue;
                _autoText = s;
                break;
            }
        }
        if (_autoText == null) return;
        var rt = _autoText.GetComponent<RectTransform>();
        if (rt == null) return;
        if (_autoTextOriginalPos == null)
            _autoTextOriginalPos = rt.anchoredPosition;
        var pos = rt.anchoredPosition;
        pos.x = needRoom ? 300f * size : _autoTextOriginalPos.Value.x;
        rt.anchoredPosition = pos;
    }


    public void UpdateTimingScale()
    {
        if (!GameObject.activeSelf || scrController.instance?.currFloor == null) return;
        TimingScaleText.text = $"{Main.Settings.Labels.TimingScale} - {Math.Round(scrController.instance.currFloor.marginScale * 100, 2)}%";
    }

    public void Show(int floor, bool suppressNativeUI = false)
    {
        var s = Main.Settings;
        Jongyeol?.OnShow(floor);
        if (_lastSavedStartProgress != -1)
        {
            if (!AutoOnceEnabled) PlayCount.SetBest(LastHash, _lastSavedStartProgress, OverlayTextManager.GetProgress(), LastMultiplier);
            _lastSavedStartProgress = -1;
        }
        var hash = PlayCount.GetMapHash();
        if (LastHash != hash) { LastHash = hash; Checkpoints = null; MapTimeCache = null; }
        MusicTimeCache = null;
        if (scnEditor.instance) { if (scrController.checkpointsUsed == 0) NoCheckStartTile = floor; }
        else if (!GCS.practiceMode) NoCheckStartTile = 0;
        else NoCheckStartTile = floor;
        AutoOnceEnabled = RDC.auto || ADOBase.controller.noFail;
        StartTile = floor;
        _lastSavedStartProgress = StartProgress = (float)floor / ADOBase.lm.listFloors.Count;
        LastMultiplier = (float)(ADOBase.conductor.song.pitch * VersionSafe.GetPlanetSpeed(scrController.instance));
        if (!AutoOnceEnabled) PlayCount.AddAttempts(LastHash, StartProgress);
        SetupTextManager();
        ApplyFontToAll();
        GameObject.SetActive(true);
        var mono = GameObject.GetComponent<OverlayMono>();
        if (mono) mono.enabled = true;
        SongPlaying = false; IsDeath = false;

        if (s.ShowProgress || s.ShowMusicTime || s.ShowCheckpoint || s.ShowBest || Jongyeol != null)
            SetupLocationMain();
        if (s.ShowJudgement) { SetupLocationJudgement(); UpdateJudgement(); }
        if (s.ShowCombo) UpdateCombo(0, false);
        if (s.ShowBPM) UpdateBPM();
        if (!suppressNativeUI) AdjustBetaWatermark(s.Size);
        if (s.ShowTimingScale) UpdateTimingScale();
        if (s.ShowAttempt) UpdateAttempts();
        ApplyPositionOffsets();
        ApplyAlignment();
        ApplyFontStyle();
        Features.GameLifecycleHelper.ComboCount = 0;
        if (!suppressNativeUI) RepositionAutoText(s.ShowProgress || s.ShowAccuracy || s.ShowXAccuracy || s.ShowMusicTime || s.ShowMapTime || s.ShowCheckpoint || s.ShowBest, s.Size);
        RefreshTimeLabels();
    }

    public void Death()
    {
        IsDeath = true;
        if (AutoOnceEnabled || _lastSavedStartProgress == -1) return;
        PlayCount.SetBest(LastHash, _lastSavedStartProgress, OverlayTextManager.GetProgress(), LastMultiplier);
        PlayCount.Save();
        _lastSavedStartProgress = -1;
        OverlayTextManager.SetBest(OverlayTextManager.GetProgress());
    }

    public void Clear()
    {
        if (AutoOnceEnabled || _lastSavedStartProgress == -1) return;
        PlayCount.SetBest(LastHash, _lastSavedStartProgress, 1, LastMultiplier);
        _lastSavedStartProgress = -1;
        OverlayTextManager.SetBest(1);
    }

    public void Hide()
    {
        Jongyeol?.OnHide();
        ResetBetaWatermark();
        RepositionAutoText(false);
        _autoText = null;
        _autoTextOriginalPos = null;
        if (GameObject == null || !GameObject.activeSelf) return;
        GameObject.SetActive(false);
        if (GameObject.TryGetComponent<OverlayMono>(out var mono)) mono.enabled = false;
        try
        {
            if (!AutoOnceEnabled && _lastSavedStartProgress != -1)
            {
                PlayCount.SetBest(LastHash, _lastSavedStartProgress, OverlayTextManager.GetProgress(), LastMultiplier);
                _lastSavedStartProgress = -1;
            }
            if (StartProgress == OverlayTextManager.GetProgress() && !AutoOnceEnabled)
                PlayCount.RemoveAttempts(LastHash, StartProgress);
        }
        catch (Exception e) { Main.Mod.Logger.Warning($"Hide: {e.Message}"); }
        PlayCount.Save();
        StartProgress = StartTile = NoCheckStartTile = -1;
        OverlayTextManager = null;
    }

    public void Destroy()
    {
        ResetBetaWatermark();
        Object.Destroy(GameObject);
        GC.SuppressFinalize(this);
    }
}
