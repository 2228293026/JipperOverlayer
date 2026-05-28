using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    private RectTransform _comboTitleTransform;
    public TextMeshProUGUI BPMText;
    public TextMeshProUGUI JudgementText;
    public TextMeshProUGUI TimingScaleText;
    public ProgressBar ProgressBar;
    public Color PurePerfectColor = new(1, 0.8549019607843137f, 0);
    public int[] Hit;
    private GameObject _mainContainer;
    private GameObject _bpmObject;
    private GameObject _judgementObject;
    private GameObject _comboObject;
    private GameObject _timingScaleObject;
    private GameObject _attemptObject;
    private GameObject _progressBarObject;
    public static readonly Shader ShaderRef = (Shader)typeof(ShaderUtilities).GetProperty("ShaderRef_MobileSDF", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
    protected int LastTime = -1;
    protected int LastMapTime = -1;
    protected int StartTile;
    public int NoCheckStartTile;
    public int[] Checkpoints;
    protected float LastTileBpm = -1;
    protected float LastCurBpm = -1;
    private readonly Stopwatch _stopwatch = new();
    protected bool SongPlaying;
    public float StartProgress;
    public bool AutoOnceEnabled;
    protected bool IsDeath;
    protected string MusicTimeCache;
    protected string MapTimeCache;
    public PlayCount.Hash LastHash;
    private float _lastSavedStartProgress = -1;
    public float LastMultiplier = 1f;

    public Overlay()
    {
        Instance = this;
        OnChangePlayers();
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
        UpdateSize();
        Object.DontDestroyOnLoad(GameObject);
        if (ADOBase.controller is { paused: false } && ADOBase.conductor is { isGameWorld: true })
            Show(0);
    }

    public void OnChangePlayers()
    {
        Hit = VersionSafe.GetHitMarginsCount();
        SetupTextManager();
    }

    protected virtual void SetupTextManager()
    {
        OverlayTextManager = VersionSafe.IsCoopMode()
            ? new OverlayTextManagerCoop(this)
            : new OverlayTextManagerNormal();
    }

    protected virtual void InitializeStatus()
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

    protected void SetupMainText(string name, ref TextMeshProUGUI text)
    {
        var go = new GameObject(name);
        var t = go.AddComponent<RectTransform>();
        t.SetParent(_mainContainer.transform);
        t.anchorMin = t.anchorMax = new Vector2(0, 1);
        t.sizeDelta = new Vector2(456, 30);
        text = go.AddComponent<TextMeshProUGUI>();
        text.font = BundleLoader.FontAsset;
        text.fontSize = 25;
        SetupShadow(text);
    }

    public virtual void SetupLocationMain()
    {
        int y = -15;
        var s = Main.Settings;
        SetupLocationMainText(ProgressText, s.ShowProgress, ref y);
        SetupLocationMainText(AccuracyText, s.ShowAccuracy, ref y);
        SetupLocationMainText(XAccuracyText, s.ShowXAccuracy, ref y);
        SetupLocationMainText(TimeText, s.ShowMusicTime, ref y);
        SetupLocationMainText(MapTimeText, s.ShowMapTime, ref y);
        SetupLocationMainText(CheckpointText,
            s.ShowCheckpoint &&
            (Checkpoints ??= scrLevelMaker.instance.listFloors.Where(f => f.GetComponent<ffxCheckpoint>())
                .Select(f => f.seqID).ToArray()).Length > 0, ref y);
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
        JudgementText.rectTransform.anchoredPosition = new Vector2(0, Main.Settings.JudgementLocationUp ? 85 : 5);
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
        SetupShadow(BPMText);
        _bpmObject = go;
    }

    private void InitializeJudgement()
    {
        var go = new GameObject("Judgement");
        var t = go.AddComponent<RectTransform>();
        t.SetParent(Canvas.transform);
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0);
        t.sizeDelta = new Vector2(1000, 30);
        JudgementText = go.AddComponent<TextMeshProUGUI>();
        SetupLocationJudgement();
        JudgementText.font = BundleLoader.FontAsset;
        JudgementText.fontSize = 25;
        JudgementText.alignment = TextAlignmentOptions.Bottom;
        JudgementText.color = new Color(0.8509804f, 0.345098f, 1);
        SetupShadow(JudgementText);
        _judgementObject = go;
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
        _comboTitleTransform = t;
        ComboTitle = title.AddComponent<TextMeshProUGUI>();
        ComboTitle.font = BundleLoader.FontAsset;
        ComboTitle.fontSize = 40;
        ComboTitle.text = "Perfect";
        ComboTitle.alignment = TextAlignmentOptions.Center;
        var fitter = title.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetupDarkShadow(ComboTitle);

        var val = new GameObject("ComboValue");
        t = val.AddComponent<RectTransform>();
        t.SetParent(ComboTransform);
        t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.45f);
        t.anchoredPosition = Vector2.zero;
        ComboTextTransform = t;
        ComboText = val.AddComponent<TextMeshProUGUI>();
        ComboText.font = BundleLoader.FontAsset;
        ComboText.fontSize = 108;
        ComboText.alignment = TextAlignmentOptions.Top;
        fitter = val.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetupDarkShadow(ComboText);
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
        SetupShadow(TimingScaleText);
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
        SetupShadow(AttemptText);
        _attemptObject = go;
    }

    public void UpdateSize()
    {
        var t = GameObject.transform;
        float size = Main.Settings.Size;
        var scale = new Vector3(size, size, 1);
        for (int i = 0; i < t.childCount; i++) t.GetChild(i).localScale = scale;
        if (TimingScaleText) TimingScaleText.rectTransform.anchoredPosition = new Vector2(0, 90 + 40 * size);
        var txtLevelName = ADOBase.controller?.txtLevelName?.GetComponent<RectTransform>();
        if (txtLevelName)
        {
            txtLevelName.anchoredPosition = new Vector2(0, -20 - 7 * size);
            txtLevelName.localScale = new Vector3(0.5f * size, 0.5f * size);
        }
        if (ComboTransform) ComboTransform.anchoredPosition = new Vector2(0, -43 - 14 * size);
    }

    public void RefreshVisibility()
    {
        var s = Main.Settings;
        if (_mainContainer) _mainContainer.SetActive(s.ShowProgress || s.ShowAccuracy || s.ShowXAccuracy || s.ShowMusicTime || s.ShowMapTime || s.ShowCheckpoint || s.ShowBest);
        if (_bpmObject) _bpmObject.SetActive(s.ShowBPM);
        if (_judgementObject) { _judgementObject.SetActive(s.ShowJudgement); if (s.ShowJudgement) SetupLocationJudgement(); }
        if (_comboObject) _comboObject.SetActive(s.ShowCombo);
        if (_timingScaleObject) _timingScaleObject.SetActive(s.ShowTimingScale);
        if (_attemptObject) { _attemptObject.SetActive(s.ShowAttempt || s.ShowFullAttempt); if (_attemptObject.activeSelf) UpdateAttempts(); }
        if (_progressBarObject) _progressBarObject.SetActive(s.ShowProgressBar);
        if (GameObject.activeSelf) SetupLocationMain();
    }

    protected void SetupShadow(TextMeshProUGUI text) => Shadow(text, 0.5f);
    protected void SetupDarkShadow(TextMeshProUGUI text) => Shadow(text, 0.7f);

    private void Shadow(TextMeshProUGUI text, float a)
    {
        Task.Delay(1).ContinueWith(_ =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    var baseMat = text.fontSharedMaterial ?? text.fontMaterial;
                    var mat = new Material(baseMat);
                    if (ShaderRef) mat.shader = ShaderRef;
                    mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                    mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
                    mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.01f);
                    mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                    mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, a));
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1f);
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1f);
                    mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                    mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                    text.fontSharedMaterial = mat;
                }
                catch (Exception e) { Main.Mod.Logger.Warning($"Shadow error: {e.Message}"); }
            });
        });
    }

    public void UpdateAccuracy(int index = -1)
    {
        if (!GameObject.activeSelf) return;
        OverlayTextManager?.UpdateAccuracy(this, index);
    }

    public virtual void UpdateProgress(scrPlanet planet = null)
    {
        if (!GameObject.activeSelf) return;
        OverlayTextManager?.CacheProgress(planet);
        if (Main.Settings.ShowProgress) OverlayTextManager?.UpdateProgress(this);
        if (Main.Settings.ShowCheckpoint) UpdateCheckPointText();
        if (Main.Settings.ShowProgressBar) UpdateProgressBar();
        if (Main.Settings.ShowBest) OverlayTextManager?.UpdateBest(this);
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
        string v0 = "", v1 = "";
        int count = 0;
        if (Main.Settings.ShowAttempt)
            v0 = count++ == 0 ? $"Attempt {PlayCount.GetData(LastHash)?.GetAttempts(StartProgress) ?? 0}" : "";
        if (Main.Settings.ShowFullAttempt)
            v1 = count++ <= 1 ? $"Full Attempt {PlayCount.GetData(LastHash)?.GetAttempts() ?? 0}" : "";
        AttemptText.text = count switch { 0 => "", 1 => v0 + v1, _ => $"{v0}\n{v1}" };
    }

    public void UpdateJudgement()
    {
        if (!GameObject.activeSelf || Hit == null) return;
        // Hit[10] only exists in v141+ (11-element array); v136 has 10 elements
        string ep = Hit.Length > 10 ? $"<color=#60FF4E>{Hit[3] + Hit[10]}</color> " : $"<color=#60FF4E>{Hit[3]}</color> ";
        JudgementText.text = $"{Hit[9]} <color=red>{Hit[0]} <color=#FF6F4E>{Hit[1]} <color=#A0FF4E>{Hit[2]} {ep}{Hit[4]}</color> {Hit[5]}</color> {Hit[6]}</color> {Hit[8]}";
    }

    public virtual void UpdateTime()
    {
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
                MusicTimeCache ??= GetTimeString(totalTime, hourNeed);
                string timeStr = time == 0 && SongPlaying ? MusicTimeCache : (time > 0 ? (SongPlaying = true, GetTimeString(time, hourNeed)).Item2 : GetTimeString(time, hourNeed));
                TimeText.text = $"<color=white>{(s.TimeTextTypeValue == 0 ? "음악 시간" : "Music Time")} |</color> {timeStr}~{MusicTimeCache}";
                LastTime = (int)time;
                TimeText.color = s.Colors.GetMusicTimeColor(time / totalTime);
            }
        }
        if (s.ShowMapTime || requireMusicToMap)
        {
            float time = (float)(scrConductor.instance.addoffset + scrConductor.instance.songposition_minusi);
            float totalTime = (float)scrLevelMaker.instance.listFloors.Last().entryTime;
            if (time < 0) time = 0; else if (time > totalTime) time = totalTime;
            if ((!s.ShowMapTime || LastMapTime == (int)time) && (!requireMusicToMap || LastTime == (int)time)) return;
            bool hourNeed = totalTime >= 3600;
            MapTimeCache ??= GetTimeString(totalTime, hourNeed);
            string tStr = time == totalTime ? MapTimeCache : GetTimeString(time, hourNeed);
            string txt = $"<color=white>{(s.TimeTextTypeValue == 0 ? "맵 시간" : "Map Time")} |</color> {tStr}~{MapTimeCache}";
            if (s.ShowMapTime) { MapTimeText.text = txt; LastMapTime = (int)time; MapTimeText.color = s.Colors.GetMapTimeColor(time / totalTime); }
            if (requireMusicToMap) { TimeText.text = txt; LastTime = (int)time; TimeText.color = s.Colors.GetMusicTimeColor(time / totalTime); }
        }
    }

    private static string GetTimeString(float time, bool hour)
    {
        int t = (int)time;
        return hour ? $"{t / 3600}:{t % 3600 / 60:00}:{t % 60:00}" : $"{t / 60}:{t % 60:00}";
    }

    public void UpdateCombo(int combo, bool bump)
    {
        if (!GameObject.activeSelf) return;
        ComboText.text = combo.ToString();
        ComboText.color = UpdateComboColor(combo);
        if (bump) { _stopwatch.Restart(); UpdateComboSize(); }
        else { _stopwatch.Stop(); ComboText.fontSize = 78; if (_comboTitleTransform) _comboTitleTransform.anchoredPosition = new Vector2(0, 43.505f); }
    }

    public virtual Color UpdateComboColor(int combo)
    {
        if (combo > Main.Settings.ComboColorMax) combo = Main.Settings.ComboColorMax;
        return Main.Settings.Colors.GetComboColor((float)combo / Main.Settings.ComboColorMax);
    }

    public void UpdateComboSize()
    {
        if (!_stopwatch.IsRunning || !GameObject.activeSelf) return;
        double t = _stopwatch.Elapsed.TotalMilliseconds / 500;
        if (t > 1) { t = 1; _stopwatch.Stop(); }
        ComboText.fontSize = 30 * OutExpoChange(t) + 78;
        Task.Delay(1).ContinueWith(_ => MainThreadDispatcher.Enqueue(UpdateComboLocation));
    }

    private void UpdateComboLocation()
    {
        try { if (_comboTitleTransform) _comboTitleTransform.anchoredPosition = new Vector2(0, ComboTextTransform.sizeDelta.y / 2); }
        catch { }
    }

    private static float OutExpoChange(double t) => (float)(t == 1 ? 0 : Math.Pow(2, -10 * t));

    public virtual void OnNonPerfectHit() { }

    public virtual void UpdateBPM()
    {
        if (!GameObject.activeSelf) return;
        var floor = scrController.instance.currFloor ?? scrController.instance.firstFloor;
        if (floor == null) return;
        var conductor = scrConductor.instance;
        float bpm = (float)(conductor.bpm * conductor.song.pitch * VersionSafe.GetPlanetSpeed(scrController.instance));
        float cbpm = floor.nextfloor ? (float)(60.0 / (floor.nextfloor.entryTime - floor.entryTime) * conductor.song.pitch) : bpm;
        float kps = cbpm / 60;
        if (LastTileBpm == bpm && LastCurBpm == cbpm) return;
        string hex = ColorToHex(Main.Settings.Colors.GetBpmColor(bpm / Main.Settings.BpmColorMax));
        BPMText.text = $"<color=white>TBPM | <color=#{hex}>{Math.Round(bpm, 2)}</color>\nCBPM |</color> {Math.Round(cbpm, 2)}\n<color=white>KPS |</color> {Math.Round(kps, 2)}";
        if (LastCurBpm != cbpm) BPMText.color = Main.Settings.Colors.GetBpmColor(cbpm / Main.Settings.BpmColorMax);
        LastTileBpm = bpm; LastCurBpm = cbpm;
    }

    protected static string ColorToHex(Color c) =>
        $"{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}{(c.a == 1 ? "" : Mathf.RoundToInt(c.a * 255).ToString("X2"))}";

    public void UpdateTimingScale()
    {
        if (!GameObject.activeSelf || scrController.instance?.currFloor == null) return;
        TimingScaleText.text = $"Timing Scale - {Math.Round(scrController.instance.currFloor.marginScale * 100, 2)}%";
    }

    public virtual void Show(int floor)
    {
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
        GameObject.SetActive(true);
        SongPlaying = false; IsDeath = false;

        if (Main.Settings.ShowProgress || Main.Settings.ShowMusicTime || Main.Settings.ShowCheckpoint || Main.Settings.ShowBest)
            SetupLocationMain();
        if (Main.Settings.ShowJudgement) UpdateJudgement();
        if (Main.Settings.ShowCombo) UpdateCombo(0, false);
        if (Main.Settings.ShowBPM) UpdateBPM();
        if (Main.Settings.ShowTimingScale) UpdateTimingScale();
        if (Main.Settings.ShowAttempt) UpdateAttempts();
        Features.GameLifecycleHelper.ComboCount = 0;
    }

    public void Death()
    {
        IsDeath = true;
        if (AutoOnceEnabled || _lastSavedStartProgress == -1) return;
        PlayCount.SetBest(LastHash, _lastSavedStartProgress, OverlayTextManager.GetProgress(), LastMultiplier);
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

    public virtual void Hide()
    {
        if (GameObject == null || !GameObject.activeSelf) return;
        GameObject.SetActive(false);
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
        StartProgress = StartTile = NoCheckStartTile = -1;
        OverlayTextManager = null;
    }

    public void Destroy()
    {
        Object.Destroy(GameObject);
        GC.SuppressFinalize(this);
    }
}
