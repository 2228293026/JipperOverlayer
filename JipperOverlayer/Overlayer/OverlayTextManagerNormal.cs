using System;

namespace JipperOverlayer.Overlayer;

public class OverlayTextManagerNormal : IOverlayTextManager
{
    public float Progress;
    public int CurCheck;
    public int LastCheckpoint = -1;
    public float CurBest = -1;
    public int DecimalPrecision = 2;

    public void SetBest(float best) => CurBest = best;

    public void CacheProgress(scrPlanet planet)
    {
        Progress = scrController.instance.percentComplete;
    }

    public void UpdateAccuracy(Overlay overlay, int index)
    {
        float xacc = VersionSafe.GetPercentXAcc();
        if (float.IsNaN(xacc)) xacc = 1;
        var labels = Main.Settings.Labels;
        if (Main.Settings.ShowAccuracy)
        {
            float acc = VersionSafe.GetPercentAcc();
            float maxAcc = 1 + (scrController.instance.currentSeqID - overlay.NoCheckStartTile + 1) * 0.0001f;
            overlay.AccuracyText.text = $"<color=white>{labels.Accuracy} |</color> {Math.Round(acc * 100, DecimalPrecision)}%";
            overlay.AccuracyText.color = Main.Settings.Colors.GetAccuracyColor(xacc == 1 ? 1 : acc / maxAcc, xacc == 1);
        }
        if (Main.Settings.ShowXAccuracy)
        {
            overlay.XAccuracyText.text = $"<color=white>{labels.XAccuracy} |</color> {Math.Round(xacc * 100, DecimalPrecision)}%";
            overlay.XAccuracyText.color = Main.Settings.Colors.GetAccuracyColor(xacc, xacc == 1);
        }
    }

    public void UpdateProgress(Overlay overlay)
    {
        var labels = Main.Settings.Labels;
        if (Main.Settings.JongyeolMode)
        {
            int cur = scrController.instance.currentSeqID;
            int last = ADOBase.lm.listFloors.Count - 1;
            overlay.ProgressText.text = $"<color=white>{labels.Progress} |</color> {cur} / {last}{(cur == last ? "" : $" [-{last - cur}]")} ({Math.Round(Progress * 100, DecimalPrecision)}%)";
        }
        else
            overlay.ProgressText.text = $"<color=white>{labels.Progress} |</color> {Math.Round(Progress * 100, DecimalPrecision)}%";
        overlay.ProgressText.color = Main.Settings.Colors.GetProgressColor(Progress);
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
        while (overlay.Checkpoints.Length > CurCheck && scrController.instance.currentSeqID >= overlay.Checkpoints[CurCheck])
        {
            CurCheck++; updated = true;
        }
        if (LastCheckpoint == scrController.checkpointsUsed && !updated) return;
        overlay.CheckpointText.text = $"<color=white>{Main.Settings.Labels.Checkpoint} |</color> {scrController.checkpointsUsed} ({CurCheck}/{overlay.Checkpoints.Length})";
        LastCheckpoint = scrController.checkpointsUsed;
    }

    public void UpdateBest(Overlay overlay)
    {
        if (RDC.auto && !overlay.AutoOnceEnabled) overlay.AutoOnceEnabled = true;
        if (CurBest == -1)
            CurBest = PlayCount.GetData(overlay.LastHash)?.GetBest(overlay.StartProgress, overlay.LastMultiplier) ?? 0;
        else if (CurBest > Progress || overlay.AutoOnceEnabled) return;
        UpdateBestText(overlay);
    }

    public float GetProgress() => Progress;

    public void UpdateBestText(Overlay overlay)
    {
        float best = CurBest > Progress || overlay.AutoOnceEnabled ? CurBest : Progress;
        overlay.BestText.text = $"<color=white>{Main.Settings.Labels.Best} |</color> {Math.Round(best * 100, DecimalPrecision)}%";
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
            overlay.DeathText.text = $"<color=white>{s.Labels.Death} |</color> {_deathCount}";
            _lastDeath = _deathCount;
        }
        float max = (scrController.instance.currentSeqID - overlay.StartTile) * 0.05f;
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
        if (scrController.instance.currentSeqID == overlay.StartTile) state = labels.StateWaiting;
        else if (scrController.instance.currFloor?.nextfloor is { auto: true })
        {
            state = labels.StateAutoTile;
            overlay.StateText.color = sColors.JStateAutoTile;
        }
        else if (RDC.auto) { state = labels.StateAuto; overlay.StateText.color = sColors.JStateAuto; }
        else if (isPurePerfect) { state = labels.StatePerfectPlay; overlay.StateText.color = sColors.JStatePerfectPlay; }
        else
        {
            int[] hits = overlay.Hit;
            if (_deathCount != 0) { state = labels.StateComplete; overlay.StateText.color = sColors.JStateComplete; }
            else if (hits[0] != 0) { state = labels.StateClear; overlay.StateText.color = sColors.JStateClear; }
            else if (hits[1] != 0 || hits[5] != 0) { state = labels.StateNoMiss; overlay.StateText.color = sColors.JStateNoMiss; }
            else { state = labels.StatePerfectionist; overlay.StateText.color = sColors.JStatePerfectionist; }
        }
        if (scrController.instance.currentSeqID != ADOBase.lm.listFloors.Count) state += labels.StateSuffix;
        if (overlay.StartTile != 0) state += labels.StateMidStart;
        overlay.StateText.text = $"<color=white>{labels.State} |</color> {state}";
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
