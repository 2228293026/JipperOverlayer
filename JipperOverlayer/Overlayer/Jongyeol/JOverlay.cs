using System;
using System.Collections.Generic;
using System.Linq;
using JipperOverlayer.Overlayer;
using JipperOverlayer.Overlayer.Features;
using TMPro;
using UnityEngine;

namespace JipperOverlayer.Overlayer.Jongyeol;

public class JOverlay : Overlay
{
    public static new JOverlay Instance;
    public new IJOverlayTextManager OverlayTextManager;
    public TextMeshProUGUI FPSText;
    public TextMeshProUGUI AuthorText;
    public TextMeshProUGUI StateText;
    public TextMeshProUGUI DeathText;
    public TextMeshProUGUI StartText;
    public TextMeshProUGUI TimingText;

    private System.Collections.Generic.List<float> _timings;
    private bool _purePerfect;
    private int _deathCount;
    private int _lastDeath = -1;
    private int _pseudoFloor = -1;
    private float _lastCurKps = -1;
    private static object GetLevelData() => scnGame.instance ? scnGame.instance.levelData : null;
    private float _fpsTime;
    private bool _perToCom;

    public JOverlay() { Instance = this; }

    protected override void SetupTextManager()
    {
        base.OverlayTextManager = OverlayTextManager = VersionSafe.IsCoopMode()
            ? new JOverlayTextManagerCoop(this)
            : new JOverlayTextManagerNormal();
    }

    protected override void InitializeStatus()
    {
        base.InitializeStatus();
        SetupMainText("FPS", ref FPSText);
        SetupMainText("Author", ref AuthorText);
        SetupMainText("State", ref StateText);
        SetupMainText("Death", ref DeathText);
        SetupMainText("Start", ref StartText);
        SetupMainText("Timing", ref TimingText);
    }

    public override void SetupLocationMain()
    {
        if (!FPSText) return;
        int y = -15;
        bool checkAuto = !Main.Settings.RemoveNotRequireInAuto || !RDC.auto;
        var s = Main.Settings;

        SetupLocationMainText(FPSText, s.ShowFPS, ref y);
        var levelData = GetLevelData();
        SetupLocationMainText(AuthorText, !string.IsNullOrEmpty(levelData?.GetType().GetProperty("author")?.GetValue(levelData) as string) && s.ShowAuthor, ref y);
        SetupLocationMainText(ProgressText, s.ShowProgress, ref y);
        SetupLocationMainText(AccuracyText, checkAuto && s.ShowAccuracy, ref y);
        SetupLocationMainText(XAccuracyText, checkAuto && s.ShowXAccuracy, ref y);
        SetupLocationMainText(TimeText, s.ShowMusicTime, ref y);
        SetupLocationMainText(MapTimeText, s.ShowMapTime, ref y);
        Checkpoints ??= scrLevelMaker.instance.listFloors.FindAll(f => f.GetComponent<ffxCheckpoint>()).Select(f => f.seqID).ToArray();
        SetupLocationMainText(CheckpointText, checkAuto && s.ShowCheckpoint && Checkpoints.Length > 0, ref y);
        SetupLocationMainText(BestText, checkAuto && s.ShowBest, ref y);
        SetupLocationMainText(StateText, s.ShowState, ref y);
        SetupLocationMainText(DeathText, scrController.instance.noFail && s.ShowDeath, ref y);
        SetupLocationMainText(StartText, StartTile != 0 && s.ShowStart, ref y);
        SetupLocationMainText(TimingText, checkAuto && s.ShowTiming, ref y);
        UpdateProgress();
        VersionSafe.CalculatePercentAcc();
        UpdateTime();
        UpdateAuthor();
        UpdateState();
        UpdateDeath();
        UpdateStart();
        if (_timings != null) return;
        _timings = [];
        UpdateTiming(0);
        _timings.Clear();
    }

    public override void UpdateProgress(scrPlanet planet = null)
    {
        if (!GameObject.activeSelf) return;
        if (_purePerfect) CheckPurePerfect();
        base.UpdateProgress(planet);
        UpdateState();
        UpdateDeath();
    }

    public override void UpdateTime()
    {
        if (!GameObject.activeSelf || IsDeath) return;
        bool requireMusicToMap = false;
        var s = Main.Settings;
        if (s.ShowMusicTime)
        {
            var song = scrConductor.instance.song;
            if (!song?.clip && s.ShowMapTimeIfNotMusic) requireMusicToMap = true;
            else
            {
                float time = song!.time;
                float totalTime = song.clip?.length ?? 0;
                if (time > 0) SongPlaying = true;
                else if (time == 0 && SongPlaying) time = totalTime;
                bool hourNeed = totalTime >= 3600;
                MusicTimeCache ??= GetTimeString2(totalTime, hourNeed);
                string timeStr = time == 0 && SongPlaying ? MusicTimeCache : GetTimeString2(time, hourNeed);
                TimeText.text = $"<color=white>{(s.TimeTextTypeValue == 0 ? "음악 시간" : "Music Time")} |</color> {timeStr}~{MusicTimeCache}";
                TimeText.color = s.Colors.GetMusicTimeColor(time / totalTime);
            }
        }
        if (s.ShowMapTime || requireMusicToMap)
        {
            float time = (float)(scrConductor.instance.addoffset + scrConductor.instance.songposition_minusi);
            float totalTime = (float)scrLevelMaker.instance.listFloors.Last().entryTime;
            if (time < 0) time = 0;
            else if (time > totalTime) time = totalTime;
            if (!s.ShowMapTime && !requireMusicToMap) return;
            bool hourNeed = totalTime >= 3600;
            MapTimeCache ??= GetTimeString2(totalTime, hourNeed);
            string timeStr = time == totalTime ? MapTimeCache : GetTimeString2(time, hourNeed);
            string text = $"<color=white>{(s.TimeTextTypeValue == 0 ? "맵 시간" : "Map Time")} |</color> {timeStr}~{MapTimeCache}";
            if (s.ShowMapTime) { MapTimeText.text = text; MapTimeText.color = s.Colors.GetMapTimeColor(time / totalTime); }
            if (requireMusicToMap) { TimeText.text = text; TimeText.color = s.Colors.GetMusicTimeColor(time / totalTime); }
        }
    }

    private static string GetTimeString2(float time, bool hour)
    {
        int t = (int)time;
        return hour ? $"{t / 3600}:{t % 3600 / 60:00}:{time % 60:00.0}" : $"{t / 60}:{time % 60:00.0}";
    }

    public override Color UpdateComboColor(int combo)
    {
        if (_purePerfect) return PurePerfectColor;
        float value = (float)combo / (scrController.instance.currentSeqID - StartTile + Hit[0] + Hit[6] + 1) * 2;
        if (value > 1) value = 1;
        return GetColor(value, 0.2f, false);
    }

    private Color GetColor(float value, float middle = 0.5f, bool ppColor = true)
    {
        return value < middle ? new Color(1 - value / middle * 0.0117647f, value / middle, value / middle * 0.3019608f) :
               value < 1f || !ppColor ? new Color(0.9882353f - (value - middle) / (1 - middle) * 0.6156863f, 1, 0.3019608f + (value - middle) / (1 - middle) * 0.01f) :
               PurePerfectColor;
    }

    public void UpdateFPS(float deltaTime)
    {
        if (!Main.Settings.ShowFPS || !GameObject.activeSelf || (_fpsTime += deltaTime) < 0.01f) return;
        FPSText.text = $"FPS | {1 / deltaTime:F4}";
        _fpsTime %= 0.01f;
    }

    public void UpdateAuthor()
    {
        if (!Main.Settings.ShowAuthor || !GameObject.activeSelf) return;
        var ld = GetLevelData();
        string author = ld?.GetType().GetProperty("author")?.GetValue(ld) as string ?? "";
        AuthorText.text = $"Author | {author}";
    }

    public void UpdateState()
    {
        if (!Main.Settings.ShowState || !GameObject.activeSelf) return;
        string s;
        StateText.color = Color.white;
        if (scrController.instance.currentSeqID == StartTile) s = "대기";
        else if (scrController.instance.currFloor?.nextfloor is { auto: true })
        {
            s = "자동 플레이 타일";
            StateText.color = new Color(1, 0.5f, 0);
        }
        else if (RDC.auto) { s = "자동 플레이"; StateText.color = new Color(0.1058824f, 1, 0); }
        else if (_purePerfect) { s = "완벽한 플레이"; StateText.color = PurePerfectColor; }
        else
        {
            int[] hits = Hit;
            if (_deathCount != 0) s = "완주";
            else if (hits[0] != 0) s = "클리어";
            else if (hits[1] != 0 || hits[5] != 0) s = "노미스";
            else s = "완벽주의";
        }
        if (scrController.instance.currentSeqID != ADOBase.lm.listFloors.Count) s += " 중";
        if (StartTile != 0) s += "(중간에서 시작)";
        StateText.text = $"<color=white>State |</color> {s}";
    }

    private void CheckPurePerfect()
    {
        for (int i = 0; i < 10; i++)
        {
            if (i is 3 or 7) i++;
            if (Hit[i] != 0) { _purePerfect = false; return; }
        }
    }

    public void UpdateDeath()
    {
        if (!Main.Settings.ShowDeath || !GameObject.activeSelf) return;
        if (_lastDeath != (_deathCount = Hit[8] + Hit[9]))
        {
            DeathText.text = $"<color=white>Death |</color> {_deathCount}";
            _lastDeath = _deathCount;
        }
        float max = (scrController.instance.currentSeqID - StartTile) * 0.05f;
        DeathText.color = GetColor(1 - Math.Min(_deathCount, max) / max);
    }

    public void UpdateStart()
    {
        if (!Main.Settings.ShowStart || !GameObject.activeSelf || StartTile != scrController.instance.currentSeqID) return;
        StartText.text = $"Start | {StartTile} ({Math.Round(OverlayTextManager.GetProgress() * 100, 5)}%)";
    }

    public void UpdateTiming(float timing)
    {
        if (!Main.Settings.ShowTiming || !GameObject.activeSelf) return;
        _timings.Add(timing);
        TimingText.text = $"<color=white>Timing |</color> {Math.Round(timing, 5)} ({Math.Round(_timings.Average(), 5)})";
        TimingText.color = GetColor(1 - Math.Min(Math.Abs(timing), 150) / 150);
    }

    public override void UpdateBPM()
    {
        if (!GameObject.activeSelf) return;
        scrFloor floor = scrController.instance.currFloor ?? scrController.instance.firstFloor;
        if (floor == null || floor.seqID <= _pseudoFloor) return;
        var conductor = scrConductor.instance;
        float bpm = (float)(conductor.bpm * conductor.song.pitch * VersionSafe.GetPlanetSpeed(scrController.instance));
        bool checkPseudo = Jbpm.CheckPseudo;
        float cbpm = 0;
        int count = 0;
        bool isPseudo = checkPseudo && CheckPseudo(floor, bpm, out cbpm, out count);
        if (!isPseudo)
            cbpm = floor.nextfloor ? (float)(60.0 / (floor.nextfloor.entryTime - floor.entryTime) * conductor.song.pitch) : bpm;
        float kps = cbpm / 60;
        if (isPseudo) kps *= count;
        if (LastTileBpm == bpm && LastCurBpm == cbpm && Math.Abs(_lastCurKps - kps) < 0.001f) return;
        string colorHex = ColorToHex(Main.Settings.Colors.GetBpmColor(bpm / Main.Settings.BpmColorMax));
        BPMText.text = $"<color=white>TBPM | <color=#{colorHex}>{Math.Round(bpm, 2)}</color>\nCBPM |</color> {Math.Round(cbpm, 2)}\n<color=white>KPS |</color> {(isPseudo ? $"<color=#{ColorToHex(Main.Settings.Colors.GetBpmColor(cbpm * count / Main.Settings.BpmColorMax))}>" : "")}{Math.Round(kps, 2)}{(isPseudo ? "</color>" : "")}";
        if (LastCurBpm != cbpm) BPMText.color = Main.Settings.Colors.GetBpmColor(cbpm / Main.Settings.BpmColorMax);
        LastTileBpm = bpm; LastCurBpm = cbpm; _lastCurKps = kps;
    }

    public void PerfectToCombo()
    {
        if (_perToCom) return;
        ComboTitle.text = "Combo";
        _perToCom = true;
    }

    public override void OnNonPerfectHit() { PerfectToCombo(); }

    private bool CheckPseudo(scrFloor curFloor, float bpm, out float cbpm, out int count)
    {
        if (bpm < 200 || !scnGame.instance) { cbpm = count = 0; return false; }
        double allAngle = 0;
        double maxAngle = bpm < 400 ? 0.5236 : 1.0472;
        scrFloor floor = curFloor;
        int midSpin = 0;
        while (floor.nextfloor != null)
        {
            if (floor.midSpin) { floor = floor.nextfloor; midSpin++; continue; }
            double angle = floor.angleLength;
            allAngle += angle;
            if (allAngle > maxAngle && (bpm < 600 || allAngle - angle > 1e-14 || !Check90(angle)))
            {
                if (angle < maxAngle && Math.Abs(angle - (floor.prevfloor?.angleLength ?? 0)) < 1e-14)
                {
                    float speed = floor.speed;
                    do { floor = floor.nextfloor; }
                    while (floor != null && Math.Abs(floor.angleLength - floor.prevfloor.angleLength) < 1e-14 && Math.Abs(speed - floor.speed) < 1e-14);
                    _pseudoFloor = floor.seqID - 1;
                    cbpm = count = 0; return false;
                }
                break;
            }
            floor = floor.nextfloor;
        }
        if (Check90(curFloor.angleLength))
        {
            int c2 = 0;
            scrFloor f2 = curFloor;
            if (f2.midSpin) f2 = f2.nextfloor;
            while (c2 < 3 && f2 != null && Check90(f2.angleLength)) { c2++; f2 = f2.nextfloor; if (f2?.midSpin == true) f2 = f2.nextfloor; }
            if (c2 < 3)
            {
                f2 = curFloor;
                if (f2.midSpin) f2 = f2.prevfloor;
                while (c2 < 3 && f2 != null && Check90(f2.angleLength)) { c2++; f2 = f2.prevfloor; if (f2?.midSpin == true) f2 = f2.prevfloor; }
            }
            if (c2 >= 3) { cbpm = count = 0; return false; }
        }
        count = floor.seqID - curFloor.seqID + 1 - midSpin;
        if (count <= 1) { cbpm = 0; return false; }
        cbpm = floor.nextfloor != null ? (float)(60 / (floor.nextfloor.entryTime - curFloor.entryTime)) : (float)(60 / (floor.entryTime - curFloor.entryTime + 60 / bpm));
        cbpm *= scrConductor.instance?.song?.pitch ?? 1;
        _pseudoFloor = floor.seqID;
        return true;
    }

    private static bool Check90(double angle) => Math.Abs(angle - 1.57079642638564) < 1e-14;

    public override void Show(int floor)
    {
        _perToCom = false; _purePerfect = true; _pseudoFloor = -1;
        if (scrController.checkpointsUsed == 0) ComboTitle.text = "Perfect";
        base.Show(floor);
    }

    public override void Hide()
    {
        base.Hide();
        _timings = null;
    }
}
