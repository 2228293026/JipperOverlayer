using System;

namespace JipperOverlayer.Overlayer.Jongyeol;

public class JOverlayTextManagerCoop : OverlayTextManagerCoop, IJOverlayTextManager
{
    public JOverlayTextManagerCoop(Overlay overlay) : base(overlay) { }

    protected override void SetProgress(ref PlayerData pData, float progress)
    {
        pData.Progress = progress;
        pData.ProgressString = $" | {ColorToString(Main.Settings.Colors.GetProgressColor(progress))}{Math.Round(progress * 100, 5)}%</color>";
    }

    protected override void SetAccuracy(ref PlayerData pData, int noCheckStartTile, int i)
    {
        float acc = scrMistakesManager.marginTrackers[i].percentAcc;
        float maxAcc = 1 + (scrPlayerManager.instance.allPlayers[i].planetarySystem.chosenPlanet.currfloor.seqID - noCheckStartTile + 1) * 0.0001f;
        float xacc = scrMistakesManager.marginTrackers[i].percentXAcc;
        if (float.IsNaN(xacc)) xacc = 1;
        pData.AccuracyString = $" | {ColorToString(Main.Settings.Colors.GetAccuracyColor(xacc == 1 ? 1 : acc / maxAcc, xacc == 1))}{Math.Round(acc * 100, 5)}%</color>";
    }

    protected override void SetXAccuracy(ref PlayerData pData, int i)
    {
        float xacc = scrMistakesManager.marginTrackers[i].percentXAcc;
        if (float.IsNaN(xacc)) xacc = 1;
        pData.XAccuracyString = $" | {ColorToString(Main.Settings.Colors.GetAccuracyColor(xacc, xacc == 1))}{Math.Round(xacc * 100, 5)}%</color>";
    }

    public override void UpdateBestText(Overlay overlay)
    {
        float best = CurBest > MaxProgress || overlay.AutoOnceEnabled ? CurBest : MaxProgress;
        overlay.BestText.text = $"<color=white>Best |</color> {Math.Round(best * 100, 5)}%";
        overlay.BestText.color = Main.Settings.Colors.GetBestColor(best);
    }
}
