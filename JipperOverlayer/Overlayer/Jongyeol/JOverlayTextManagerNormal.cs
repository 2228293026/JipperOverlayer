using System;

namespace JipperOverlayer.Overlayer.Jongyeol;

public class JOverlayTextManagerNormal : OverlayTextManagerNormal, IJOverlayTextManager
{
    public override void UpdateAccuracy(Overlay overlay, int index)
    {
        float xacc = VersionSafe.GetPercentXAcc();
        if (float.IsNaN(xacc)) xacc = 1;
        if (Main.Settings.ShowAccuracy)
        {
            float acc = VersionSafe.GetPercentAcc();
            float maxAcc = 1 + (scrController.instance.currentSeqID - overlay.NoCheckStartTile + 1) * 0.0001f;
            overlay.AccuracyText.text = $"<color=white>Accuracy |</color> {Math.Round(acc * 100, 4)}%";
            overlay.AccuracyText.color = Main.Settings.Colors.GetAccuracyColor(xacc == 1 ? 1 : acc / maxAcc, xacc == 1);
        }
        if (Main.Settings.ShowXAccuracy)
        {
            overlay.XAccuracyText.text = $"<color=white>X-Accuracy |</color> {Math.Round(xacc * 100, 4)}%";
            overlay.XAccuracyText.color = Main.Settings.Colors.GetAccuracyColor(xacc, xacc == 1);
        }
    }

    public override void UpdateProgress(Overlay overlay)
    {
        int cur = scrController.instance.currentSeqID;
        int last = ADOBase.lm.listFloors.Count - 1;
        overlay.ProgressText.text = $"<color=white>Progress |</color> {cur} / {last}{(cur == last ? "" : $" [-{last - cur}]")} ({Math.Round(Progress * 100, 5)}%)";
        overlay.ProgressText.color = Main.Settings.Colors.GetProgressColor(Progress);
    }

    public override void UpdateBestText(Overlay overlay)
    {
        float best = CurBest > Progress || overlay.AutoOnceEnabled ? CurBest : Progress;
        overlay.BestText.text = $"<color=white>Best |</color> {Math.Round(best * 100, 5)}%";
        overlay.BestText.color = Main.Settings.Colors.GetBestColor(best);
    }
}
