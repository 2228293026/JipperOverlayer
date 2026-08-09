using System;
using System.Text;
using UnityEngine;

namespace JipperOverlayer.Overlayer;

public class OverlayTextManagerNormal : IOverlayTextManager
{
    private static readonly StringBuilder _sb = new(128);
    public float Progress;
    public int CurCheck;
    public int LastCheckpoint = -1;
    public float CurBest = -1;
    public int DecimalPrecision = 2;

    public void SetBest(float best) => CurBest = best;

    public void CacheProgress(scrPlanet planet)
    {
        Progress = GameRefs.PercentComplete;
    }

    public void SeedProgress(float progress) => Progress = progress;

    public void UpdateAccuracy(Overlay overlay, int index)
    {
        float xacc = VersionSafe.GetPercentXAcc();
        if (float.IsNaN(xacc)) xacc = 1;
        var labels = Main.Settings.Labels;
        if (Main.Settings.ShowAccuracy)
        {
            float acc = VersionSafe.GetPercentAcc();
            float maxAcc = 1 + (GameRefs.CurrentSeqID - overlay.NoCheckStartTile + 1) * 0.0001f;
            _sb.Clear();
            _sb.Append("<color=white>");
            _sb.Append(labels.Accuracy);
            _sb.Append(" |</color> ");
            _sb.Append(Math.Round(acc * 100, DecimalPrecision));
            _sb.Append('%');
            overlay.AccuracyText.SetText(_sb);
            overlay.AccuracyText.color = Main.Settings.Colors.GetAccuracyColor(xacc == 1 ? 1 : acc / maxAcc, xacc == 1);
        }
        if (Main.Settings.ShowXAccuracy)
        {
            _sb.Clear();
            _sb.Append("<color=white>");
            _sb.Append(labels.XAccuracy);
            _sb.Append(" |</color> ");
            _sb.Append(Math.Round(xacc * 100, DecimalPrecision));
            _sb.Append('%');
            overlay.XAccuracyText.SetText(_sb);
            overlay.XAccuracyText.color = Main.Settings.Colors.GetXAccuracyColor(xacc, xacc == 1);
        }
    }

    public void UpdateProgress(Overlay overlay)
    {
        var labels = Main.Settings.Labels;
        if (Main.Settings.JongyeolMode)
        {
            int cur = GameRefs.CurrentSeqID;
            var floors = GameRefs.LevelMaker?.listFloors;
            int last = floors != null && floors.Count > 0 ? floors.Count - 1 : 0;
            overlay.ProgressText.text = $"<color=white>{labels.Progress} |</color> {cur} / {last}{(cur == last ? "" : $" [-{last - cur}]")} ({Math.Round(Progress * 100, DecimalPrecision)}%)";
            overlay.ProgressText.color = Main.Settings.Colors.GetProgressColor(Progress);
        }
        else
        {
            var colors = Main.Settings.Colors;
            _sb.Clear();
            _sb.Append("<color=white>");
            _sb.Append(labels.Progress);
            _sb.Append(" |</color> ");
            if (overlay.StartTile > 0)
            {
                _sb.Append("<color=#");
                _sb.Append(ColorUtility.ToHtmlStringRGB(colors.GetProgressColor(overlay.StartProgress)));
                _sb.Append(">");
                _sb.Append(Math.Round(overlay.StartProgress * 100, DecimalPrecision));
                _sb.Append("%</color> ~ ");
            }
            _sb.Append("<color=#");
            _sb.Append(ColorUtility.ToHtmlStringRGB(colors.GetProgressColor(Progress)));
            _sb.Append(">");
            _sb.Append(Math.Round(Progress * 100, DecimalPrecision));
            _sb.Append("%</color>");
            overlay.ProgressText.SetText(_sb);
            overlay.ProgressText.color = Color.white;
        }
    }

    public void UpdateProgressBar(Overlay overlay)
    {
        var bar = overlay.ProgressBar;
        bar.LineTransform.SizeDeltaX(Progress * 638);
        bar.BackgroundImage.color = Main.Settings.Colors.GetProgressBarBackgroundColor(Progress);
        bar.LineImage.color = Main.Settings.Colors.GetProgressBarColor(Progress);
        bar.BorderImage.color = Main.Settings.Colors.GetProgressBarBorderColor(Progress);
    }

    public void UpdateCheckpoint(Overlay overlay)
    {
        bool updated = false;
        while (overlay.Checkpoints.Length > CurCheck && GameRefs.CurrentSeqID >= overlay.Checkpoints[CurCheck])
        {
            CurCheck++; updated = true;
        }
        if (LastCheckpoint == GameRefs.CheckpointsUsed && !updated) return;
        _sb.Clear();
        _sb.Append("<color=white>");
        _sb.Append(Main.Settings.Labels.Checkpoint);
        _sb.Append(" |</color> ");
        _sb.Append(GameRefs.CheckpointsUsed);
        _sb.Append(" (");
        _sb.Append(CurCheck);
        _sb.Append('/');
        _sb.Append(overlay.Checkpoints.Length);
        _sb.Append(')');
        overlay.CheckpointText.SetText(_sb);
        LastCheckpoint = GameRefs.CheckpointsUsed;
    }

    public void UpdateBest(Overlay overlay)
    {
        if (GameRefs.IsAuto && !overlay.AutoOnceEnabled) overlay.AutoOnceEnabled = true;
        if (CurBest == -1)
            CurBest = PlayCount.GetData(overlay.LastHash)?.GetBest(overlay.StartProgress, overlay.LastMultiplier) ?? 0;
        else if (CurBest > Progress || overlay.AutoOnceEnabled) return;
        UpdateBestText(overlay);
    }

    public float GetProgress() => Progress;

    public void UpdateBestText(Overlay overlay)
    {
        float best = CurBest > Progress || overlay.AutoOnceEnabled ? CurBest : Progress;
        _sb.Clear();
        _sb.Append("<color=white>");
        _sb.Append(Main.Settings.Labels.Best);
        _sb.Append(" |</color> ");
        _sb.Append(Math.Round(best * 100, DecimalPrecision));
        _sb.Append('%');
        overlay.BestText.SetText(_sb);
        overlay.BestText.color = Main.Settings.Colors.GetBestColor(best);
    }

    // ===== Jongyeol-mode helpers (single-player) =====

    private int _deathCount;
    private int _lastDeath = -1;

    public void UpdateDeath(Overlay overlay)
    {
        var s = Main.Settings;
        if (!s.ShowDeath || !overlay.GameObject.activeSelf || overlay.DeathText == null) return;
        int[] hits = overlay.Hit;
        if (_lastDeath != (_deathCount = hits[8] + hits[9]))
        {
            _sb.Clear();
            _sb.Append("<color=white>");
            _sb.Append(s.Labels.Death);
            _sb.Append(" |</color> ");
            _sb.Append(_deathCount);
            overlay.DeathText.SetText(_sb);
            _lastDeath = _deathCount;
        }
        float max = (GameRefs.CurrentSeqID - overlay.StartTile) * 0.05f;
        if (max < 0.001f) return;
        overlay.DeathText.color = s.Colors.JDeath.GetColor(1 - Math.Min(_deathCount, max) / max);
    }

    public void UpdateState(Overlay overlay, bool isPurePerfect)
    {
        var s = Main.Settings;
        if (!s.ShowState || !overlay.GameObject.activeSelf || overlay.StateText == null) return;
        var sColors = s.Colors;
        var labels = s.Labels;
        string state;
        overlay.StateText.color = sColors.JStateWaiting;
        if (GameRefs.CurrentSeqID == overlay.StartTile) state = labels.StateWaiting;
        else if (GameRefs.CurrentFloor?.nextfloor is { auto: true })
        {
            state = labels.StateAutoTile;
            overlay.StateText.color = sColors.JStateAutoTile;
        }
        else if (GameRefs.IsAuto) { state = labels.StateAuto; overlay.StateText.color = sColors.JStateAuto; }
        else if (isPurePerfect) { state = labels.StatePerfectPlay; overlay.StateText.color = sColors.JStatePerfectPlay; }
        else
        {
            int[] hits = overlay.Hit;
            if (_deathCount != 0) { state = labels.StateComplete; overlay.StateText.color = sColors.JStateComplete; }
            else if (hits[0] != 0) { state = labels.StateClear; overlay.StateText.color = sColors.JStateClear; }
            else if (hits[1] != 0 || hits[5] != 0) { state = labels.StateNoMiss; overlay.StateText.color = sColors.JStateNoMiss; }
            else { state = labels.StatePerfectionist; overlay.StateText.color = sColors.JStatePerfectionist; }
        }
        var allFloors = GameRefs.LevelMaker?.listFloors;
        if (allFloors != null && GameRefs.CurrentSeqID != allFloors.Count) state += labels.StateSuffix;
        if (overlay.StartTile != 0) state += labels.StateMidStart;
        _sb.Clear();
        _sb.Append("<color=white>");
        _sb.Append(labels.State);
        _sb.Append(" |</color> ");
        _sb.Append(state);
        overlay.StateText.SetText(_sb);
    }

    public bool CheckPurePerfect(Overlay overlay)
    {
        int[] hits = overlay.Hit;
        for (int i = 0; i < hits.Length && i < 10; i++)
        {
            if (i is 3 or 7) continue;
            if (hits[i] != 0) return false;
        }
        return true;
    }

    public int GetTooJudgement(Overlay overlay) => overlay.Hit[0] + overlay.Hit[6];
}