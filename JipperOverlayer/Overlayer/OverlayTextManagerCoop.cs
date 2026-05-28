using System;
using UnityEngine;

namespace JipperOverlayer.Overlayer;

public class OverlayTextManagerCoop : IOverlayTextManager
{
    public PlayerData[] PlayerDatas;
    public float MaxProgress;
    public float CurBest = -1;
    public int CurCheck;
    public int LastCheckpoint = -1;

    public OverlayTextManagerCoop(Overlay overlay)
    {
        PlayerDatas = new PlayerData[scrPlayerManager.playerCount];
        overlay.ProgressText.color = Color.white;
        overlay.AccuracyText.color = Color.white;
        overlay.XAccuracyText.color = Color.white;
    }

    public void SetBest(float best) => CurBest = best;

    public void CacheProgress(scrPlanet planet)
    {
        if ((object)planet == null)
        {
            float count = ADOBase.lm.listFloors.Count;
            for (int i = 0; i < PlayerDatas.Length; i++)
                SetProgress(ref PlayerDatas[i],
                    (scrPlayerManager.instance.allPlayers[i].planetarySystem.chosenPlanet.currfloor.seqID + 1) / count);
        }
        else
        {
            SetProgress(ref PlayerDatas[planet.player.playerID],
                (planet.currfloor.seqID + 1) / (float)ADOBase.lm.listFloors.Count);
        }
    }

    protected virtual void SetProgress(ref PlayerData pData, float progress)
    {
        pData.Progress = progress;
        pData.ProgressString = $" | {ColorToString(Main.Settings.Colors.GetProgressColor(progress))}{Math.Round(progress * 100, 2)}%</color>";
        if (MaxProgress < progress) MaxProgress = progress;
    }

    public void UpdateAccuracy(Overlay overlay, int index)
    {
        if (Main.Settings.ShowAccuracy)
        {
            if (index == -1)
                for (int i = 0; i < PlayerDatas.Length; i++)
                    SetAccuracy(ref PlayerDatas[i], overlay.NoCheckStartTile, i);
            else SetAccuracy(ref PlayerDatas[index], overlay.NoCheckStartTile, index);

            var strings = new string[PlayerDatas.Length + 1];
            strings[0] = "Accuracy";
            for (int i = 0; i < PlayerDatas.Length; i++) strings[i + 1] = PlayerDatas[i].AccuracyString;
            overlay.AccuracyText.text = string.Concat(strings);
        }
        if (Main.Settings.ShowXAccuracy)
        {
            if (index == -1)
                for (int i = 0; i < PlayerDatas.Length; i++)
                    SetXAccuracy(ref PlayerDatas[i], i);
            else SetXAccuracy(ref PlayerDatas[index], index);

            var strings = new string[PlayerDatas.Length + 1];
            strings[0] = "XAccuracy";
            for (int i = 0; i < PlayerDatas.Length; i++) strings[i + 1] = PlayerDatas[i].XAccuracyString;
            overlay.XAccuracyText.text = string.Concat(strings);
        }
    }

    protected virtual void SetAccuracy(ref PlayerData pData, int noCheckStartTile, int i)
    {
        float acc = scrMistakesManager.marginTrackers[i].percentAcc;
        float maxAcc = 1 + (scrPlayerManager.instance.allPlayers[i].planetarySystem.chosenPlanet.currfloor.seqID - noCheckStartTile + 1) * 0.0001f;
        float xacc = scrMistakesManager.marginTrackers[i].percentXAcc;
        if (float.IsNaN(xacc)) xacc = 1;
        pData.AccuracyString = $" | {ColorToString(Main.Settings.Colors.GetAccuracyColor(xacc == 1 ? 1 : acc / maxAcc, xacc == 1))}{Math.Round(acc * 100, 2)}%</color>";
    }

    protected virtual void SetXAccuracy(ref PlayerData pData, int i)
    {
        float xacc = scrMistakesManager.marginTrackers[i].percentXAcc;
        if (float.IsNaN(xacc)) xacc = 1;
        pData.XAccuracyString = $" | {ColorToString(Main.Settings.Colors.GetAccuracyColor(xacc, xacc == 1))}{Math.Round(xacc * 100, 2)}%</color>";
    }

    public void UpdateProgress(Overlay overlay)
    {
        var strings = new string[PlayerDatas.Length + 1];
        strings[0] = "Progress";
        for (int i = 0; i < PlayerDatas.Length; i++) strings[i + 1] = PlayerDatas[i].ProgressString;
        overlay.ProgressText.text = string.Concat(strings);
    }

    public void UpdateProgressBar(Overlay overlay)
    {
        var bar = overlay.ProgressBar;
        bar.LineTransform.SizeDeltaX(MaxProgress * 638);
        bar.BackgroundImage.color = Main.Settings.Colors.GetProgressBarBackgroundColor(MaxProgress);
        bar.LineImage.color = Main.Settings.Colors.GetProgressBarColor(MaxProgress);
        bar.BorderImage.color = Main.Settings.Colors.GetProgressBarBorderColor(MaxProgress);
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
        else if (CurBest > MaxProgress || overlay.AutoOnceEnabled) return;
        UpdateBestText(overlay);
    }

    public float GetProgress() => MaxProgress;

    public virtual void UpdateBestText(Overlay overlay)
    {
        float best = CurBest > MaxProgress || overlay.AutoOnceEnabled ? CurBest : MaxProgress;
        overlay.BestText.text = $"<color=white>Best |</color> {Math.Round(best * 100, 2)}%";
        overlay.BestText.color = Main.Settings.Colors.GetBestColor(best);
    }

    public static string ColorToString(in Color color) => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>";

    public struct PlayerData
    {
        public float Progress;
        public string ProgressString;
        public string AccuracyString;
        public string XAccuracyString;
    }
}
