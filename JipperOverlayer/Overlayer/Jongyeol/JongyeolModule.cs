using System;
using System.Collections.Generic;
using JipperOverlayer.Overlayer.Util;
using TMPro;
using UnityEngine;

namespace JipperOverlayer.Overlayer.Jongyeol;

public class JongyeolModule
{
    private readonly Overlay _overlay;

    public TextMeshProUGUI FPSText, AuthorText, StateText, DeathText, StartText, TimingText;
    public readonly List<TextMeshProUGUI> ExtraTexts = new();

    private List<float> _timings;
    private bool _purePerfect;
    private int _pseudoFloor = -1;
    private float _lastCurKps = -1, _fpsTime, _timingsSum;
    private bool _perToCom;
    public int DecimalPrecision = 2;

    public JongyeolModule(Overlay overlay)
    {
        _overlay = overlay;
    }

    public void InitializeExtraTexts()
    {
        _overlay.SetupMainText("FPS", ref FPSText);
        _overlay.SetupMainText("Author", ref AuthorText);
        _overlay.SetupMainText("State", ref StateText);
        _overlay.SetupMainText("Death", ref DeathText);
        _overlay.SetupMainText("Start", ref StartText);
        _overlay.SetupMainText("Timing", ref TimingText);
        _overlay.DeathText = DeathText;
        _overlay.StateText = StateText;
        ExtraTexts.AddRange([FPSText, AuthorText, StateText, DeathText, StartText, TimingText]);
    }

    // ===== SetupLocation — replaces Overlay.SetupLocationMain when active =====

    public void SetupLocation()
    {
        if (!FPSText) return;
        int y = -15;
        var s = Main.Settings;
        bool checkAuto = !s.RemoveNotRequireInAuto || !RDC.auto;

        _overlay.Checkpoints ??= Overlay.CollectCheckpoints();

        foreach (int elemId in s.JongyeolDisplayOrder)
        {
            var elem = (DisplayElement)elemId;
            var text = GetJongyeolStackText(elem);
            if (text == null) continue;
            bool enabled = IsJongyeolElementEnabled(elem, s, checkAuto);
            SetupText(text, enabled, ref y);
        }

        _overlay.UpdateProgress();
        VersionSafe.CalculatePercentAcc();
        _overlay.UpdateTime();
        UpdateAuthor();
        UpdateState();
        UpdateDeath();
        if (_timings != null) return;
        _timings = [];
        UpdateTiming(0);
        _timings.Clear();
        _timingsSum = 0;
    }

    TextMeshProUGUI GetJongyeolStackText(DisplayElement elem) => elem switch
    {
        DisplayElement.FPS => FPSText,
        DisplayElement.Author => AuthorText,
        DisplayElement.Progress => _overlay.ProgressText,
        DisplayElement.Accuracy => _overlay.AccuracyText,
        DisplayElement.XAccuracy => _overlay.XAccuracyText,
        DisplayElement.MusicTime => _overlay.TimeText,
        DisplayElement.MapTime => _overlay.MapTimeText,
        DisplayElement.Checkpoint => _overlay.CheckpointText,
        DisplayElement.Best => _overlay.BestText,
        DisplayElement.State => StateText,
        DisplayElement.Death => DeathText,
        DisplayElement.Start => StartText,
        DisplayElement.Timing => TimingText,
        _ => null,
    };

    bool IsJongyeolElementEnabled(DisplayElement elem, Settings s, bool checkAuto)
    {
        if (elem == DisplayElement.Author)
        {
            var ld = scnGame.instance?.levelData;
            return !string.IsNullOrEmpty(ld?.GetType().GetProperty("author")?.GetValue(ld) as string) && s.ShowAuthor;
        }
        if (elem == DisplayElement.Death) return scrController.instance.noFail && s.ShowDeath;
        if (elem == DisplayElement.Start) return _overlay.StartTile != 0 && s.ShowStart;
        if (elem == DisplayElement.Checkpoint) return checkAuto && s.ShowCheckpoint && (_overlay.Checkpoints?.Length > 0);
        return elem switch
        {
            DisplayElement.FPS => s.ShowFPS,
            DisplayElement.Progress => s.ShowProgress,
            DisplayElement.Accuracy => checkAuto && s.ShowAccuracy,
            DisplayElement.XAccuracy => checkAuto && s.ShowXAccuracy,
            DisplayElement.MusicTime => s.ShowMusicTime,
            DisplayElement.MapTime => s.ShowMapTime,
            DisplayElement.Best => checkAuto && s.ShowBest,
            DisplayElement.State => s.ShowState,
            DisplayElement.Timing => checkAuto && s.ShowTiming,
            _ => false,
        };
    }

    private static void SetupText(TextMeshProUGUI text, bool enabled, ref int y)
    {
        if (text == null) return;
        text.enabled = enabled;
        if (!enabled) return;
        text.rectTransform.anchoredPosition = new Vector2(228, y);
        y -= 35;
    }

    // ===== Extra Update Methods =====

    public void UpdateFPS(float deltaTime)
    {
        var s = Main.Settings;
        if (!s.ShowFPS || !_overlay.GameObject.activeSelf || (_fpsTime += deltaTime) < s.FPSRefreshRate) return;
        _fpsTime %= s.FPSRefreshRate;
        Overlay._textSb.Clear();
        Overlay._textSb.Append("<color=white>");
        Overlay._textSb.Append(s.Labels.FPS);
        Overlay._textSb.Append(" |</color> ");
        Overlay._textSb.Append((1f / deltaTime).ToString($"F{DecimalPrecision}"));
        FPSText.text = Overlay._textSb.ToString();
        FPSText.color = s.Colors.JFps;
    }

    public void UpdateAuthor()
    {
        var s = Main.Settings;
        if (!s.ShowAuthor || !_overlay.GameObject.activeSelf) return;
        var ld = scnGame.instance?.levelData;
        string author = ld?.GetType().GetProperty("author")?.GetValue(ld) as string ?? "";
        AuthorText.text = $"<color=white>{s.Labels.Author} |</color> {author}";
    }

    public void UpdateColors()
    {
        if (!_overlay.GameObject.activeSelf) return;
        var c = Main.Settings.Colors;
        if (FPSText) FPSText.color = c.JFps;
        if (AuthorText) AuthorText.color = c.JAuthor;
        if (StartText) StartText.color = c.JStart;
    }

    public void UpdateState()
    {
        _overlay.OverlayTextManager?.UpdateState(_overlay, _purePerfect);
    }

    public void CheckPurePerfect()
    {
        _purePerfect = _overlay.OverlayTextManager?.CheckPurePerfect(_overlay) ?? true;
    }

    public void UpdateDeath()
    {
        _overlay.OverlayTextManager?.UpdateDeath(_overlay);
    }

    public void UpdateStart()
    {
        var s = Main.Settings;
        if (!s.ShowStart || !_overlay.GameObject.activeSelf || _overlay.StartTile != scrController.instance.currentSeqID) return;
        StartText.text = $"<color=white>{s.Labels.Start} |</color> {_overlay.StartTile} ({Math.Round(_overlay.OverlayTextManager.GetProgress() * 100, DecimalPrecision)}%)";
        StartText.color = s.Colors.JStart;
    }

    public void UpdateTiming(float timing)
    {
        var s = Main.Settings;
        if (!s.ShowTiming || !_overlay.GameObject.activeSelf) return;
        if (_timings.Count >= 5000)
        {
            for (int i = 0; i < 1000; i++) _timingsSum -= _timings[i];
            _timings.RemoveRange(0, 1000);
        }
        _timings.Add(timing);
        _timingsSum += timing;
        TimingText.text = $"<color=white>{s.Labels.Timing} |</color> {Math.Round(timing, DecimalPrecision)} ({Math.Round(_timingsSum / _timings.Count, DecimalPrecision)})";
        TimingText.color = s.Colors.JTiming.GetColor(1 - Math.Min(Math.Abs(timing), 150) / 150);
    }

    // ===== Overridden Update Methods =====

    public void UpdateTime()
    {
        if (!_overlay.GameObject.activeSelf || _overlay.IsDeath) return;
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
                if (time > 0) _overlay.SongPlaying = true;
                else if (time == 0 && _overlay.SongPlaying) time = totalTime;
                bool hourNeed = totalTime >= 3600;
                _overlay.MusicTimeCache ??= TimeFormatter.FormatWithDecimals(totalTime, hourNeed);
                string timeStr = time == 0 && _overlay.SongPlaying ? _overlay.MusicTimeCache : TimeFormatter.FormatWithDecimals(time, hourNeed);
                _overlay.TimeText.text = $"{_overlay._musicTimeLabel} {timeStr}~{_overlay.MusicTimeCache}";
                _overlay.TimeText.color = s.Colors.GetMusicTimeColor(time / totalTime);
            }
        }
        if (s.ShowMapTime || requireMusicToMap)
        {
            float time = (float)(scrConductor.instance.addoffset + scrConductor.instance.songposition_minusi);
            var floors = scrLevelMaker.instance.listFloors;
            float totalTime = (float)floors[floors.Count - 1].entryTime;
            if (time < 0) time = 0;
            else if (time > totalTime) time = totalTime;
            if (!s.ShowMapTime && !requireMusicToMap) return;
            bool hourNeed = totalTime >= 3600;
            _overlay.MapTimeCache ??= TimeFormatter.FormatWithDecimals(totalTime, hourNeed);
            string timeStr = time == totalTime ? _overlay.MapTimeCache : TimeFormatter.FormatWithDecimals(time, hourNeed);
            string text = $"{_overlay._mapTimeLabel} {timeStr}~{_overlay.MapTimeCache}";
            if (s.ShowMapTime) { _overlay.MapTimeText.text = text; _overlay.MapTimeText.color = s.Colors.GetMapTimeColor(time / totalTime); }
            if (requireMusicToMap) { _overlay.TimeText.text = text; _overlay.TimeText.color = s.Colors.GetMusicTimeColor(time / totalTime); }
        }
    }

    public void UpdateBPM()
    {
        var s = Main.Settings;
        if (!_overlay.GameObject.activeSelf) return;
        scrFloor floor = scrController.instance.currFloor ?? scrController.instance.firstFloor;
        if (floor == null || floor.seqID <= _pseudoFloor) return;
        var bpm = BpmCalculator.Calculate(floor, (float)(scrConductor.instance.song.pitch * VersionSafe.GetPlanetSpeed(scrController.instance)));
        bool checkPseudo = Jbpm.CheckPseudo;
        float cbpm = 0;
        int count = 0;
        bool isPseudo = checkPseudo && CheckPseudo(floor, bpm.TileBpm, out cbpm, out count);
        if (!isPseudo) cbpm = bpm.CurrentBpm;
        float kps = cbpm / 60;
        if (isPseudo) kps *= count;
        if (_overlay.LastTileBpm == bpm.TileBpm && _overlay.LastCurBpm == cbpm && Math.Abs(_lastCurKps - kps) < 0.001f) return;
        string colorHex = BpmCalculator.ColorToHex(s.Colors.GetBpmColor(bpm.TileBpm / s.BpmColorMax));
        string kpsPrefix = isPseudo ? $"<color=#{BpmCalculator.ColorToHex(s.Colors.GetBpmColor(cbpm * count / s.BpmColorMax))}>" : "";
        string kpsSuffix = isPseudo ? "</color>" : "";
        _overlay.BPMText.text = Overlay.BuildBpmText(s.BpmLineOrder, colorHex, s, bpm.TileBpm, cbpm, kps, kpsPrefix, kpsSuffix);
        if (_overlay.LastCurBpm != cbpm) _overlay.BPMText.color = s.Colors.GetBpmColor(cbpm / s.BpmColorMax);
        _overlay.LastTileBpm = bpm.TileBpm; _overlay.LastCurBpm = cbpm; _lastCurKps = kps;
    }

    public Color UpdateComboColor(int combo)
    {
        if (_purePerfect) return Main.Settings.Colors.JStatePerfectPlay;
        int too = _overlay.OverlayTextManager?.GetTooJudgement(_overlay) ?? 0;
        float value = (float)combo / (scrController.instance.currentSeqID - _overlay.StartTile + too + 1) * 2;
        if (value > 1) value = 1;
        return Main.Settings.Colors.JCombo.GetColor(value);
    }

    public void OnNonPerfectHit()
    {
        if (_perToCom) return;
        _overlay.ComboTitle.text = Main.Settings.Labels.ComboTitleAlt;
        _perToCom = true;
    }

    // ===== Show / Hide Hooks =====

    public void OnShow(int floor)
    {
        _perToCom = false; _purePerfect = true; _pseudoFloor = -1;
        _timingsSum = 0;
        if (scrController.checkpointsUsed == 0) _overlay.ComboTitle.text = Main.Settings.Labels.ComboTitle;
    }

    public void OnHide()
    {
        _timings = null;
        _timingsSum = 0;
    }

    // ===== Pseudo-BPM Detection =====

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
}
