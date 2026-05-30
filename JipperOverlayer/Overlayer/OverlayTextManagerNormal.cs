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
        if (Main.Settings.ShowAccuracy)
        {
            float acc = VersionSafe.GetPercentAcc();
            float maxAcc = 1 + (scrController.instance.currentSeqID - overlay.NoCheckStartTile + 1) * 0.0001f;
            overlay.AccuracyText.text = $"<color=white>Accuracy |</color> {Math.Round(acc * 100, DecimalPrecision)}%";
            overlay.AccuracyText.color = Main.Settings.Colors.GetAccuracyColor(xacc == 1 ? 1 : acc / maxAcc, xacc == 1);
        }
        if (Main.Settings.ShowXAccuracy)
        {
            overlay.XAccuracyText.text = $"<color=white>{(Main.Settings.JongyeolMode ? "X-Accuracy" : "XAccuracy")} |</color> {Math.Round(xacc * 100, DecimalPrecision)}%";
            overlay.XAccuracyText.color = Main.Settings.Colors.GetAccuracyColor(xacc, xacc == 1);
        }
    }

    public void UpdateProgress(Overlay overlay)
    {
        if (Main.Settings.JongyeolMode)
        {
            int cur = scrController.instance.currentSeqID;
            int last = ADOBase.lm.listFloors.Count - 1;
            overlay.ProgressText.text = $"<color=white>Progress |</color> {cur} / {last}{(cur == last ? "" : $" [-{last - cur}]")} ({Math.Round(Progress * 100, 5)}%)";
        }
        else
            overlay.ProgressText.text = $"<color=white>Progress |</color> {Math.Round(Progress * 100, DecimalPrecision)}%";
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
        overlay.CheckpointText.text = $"<color=white>CheckPoint |</color> {scrController.checkpointsUsed} ({CurCheck}/{overlay.Checkpoints.Length})";
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
        int precision = Main.Settings.JongyeolMode ? 5 : 2;
        overlay.BestText.text = $"<color=white>Best |</color> {Math.Round(best * 100, precision)}%";
        overlay.BestText.color = Main.Settings.Colors.GetBestColor(best);
    }
}
