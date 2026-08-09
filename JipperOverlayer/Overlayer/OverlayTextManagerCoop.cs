using System;
using System.Text;
using UnityEngine;

namespace JipperOverlayer.Overlayer;

public class OverlayTextManagerCoop : IOverlayTextManager
{
    public PlayerData[] PlayerDatas;
    public float MaxProgress;
    public float CurBest = -1;
    public int CurCheck;
    public int LastCheckpoint = -1;
    public int DecimalPrecision = 2;
    private string[] _accStrings;
    private string[] _xaccStrings;

    public OverlayTextManagerCoop(Overlay overlay)
    {
        PlayerDatas = new PlayerData[scrPlayerManager.playerCount];
        _accStrings = new string[PlayerDatas.Length + 1];
        _xaccStrings = new string[PlayerDatas.Length + 1];
        if (overlay.ProgressText) overlay.ProgressText.color = Color.white;
        if (overlay.AccuracyText) overlay.AccuracyText.color = Color.white;
        if (overlay.XAccuracyText) overlay.XAccuracyText.color = Color.white;
    }

    public void SetBest(float best) => CurBest = best;

    public void CacheProgress(scrPlanet planet)
    {
        var allFloors = GameRefs.LevelMaker?.listFloors;
        if (allFloors == null) return;
        float count = allFloors.Count;
        if ((object)planet == null)
        {
            for (int i = 0; i < PlayerDatas.Length; i++)
                SetProgress(ref PlayerDatas[i],
                    (scrPlayerManager.instance.allPlayers[i].planetarySystem.chosenPlanet.currfloor.seqID + 1) / count);
        }
        else
        {
            SetProgress(ref PlayerDatas[planet.player.playerID],
                (planet.currfloor.seqID + 1) / count);
        }
    }

    protected void SetProgress(ref PlayerData pData, float progress)
    {
        pData.Progress = progress;
        pData.ProgressString = $" | {ColorToString(Main.Settings.Colors.GetProgressColor(progress))}{Math.Round(progress * 100, DecimalPrecision)}%</color>";
        if (MaxProgress < progress) MaxProgress = progress;
    }

    public void SeedProgress(float progress)
    {
        for (int i = 0; i < PlayerDatas.Length; i++)
            SetProgress(ref PlayerDatas[i], progress);
    }

    public void UpdateAccuracy(Overlay overlay, int index)
    {
        if (Main.Settings.ShowAccuracy)
        {
            if (index == -1)
                for (int i = 0; i < PlayerDatas.Length; i++)
                    SetAccuracy(ref PlayerDatas[i], overlay.NoCheckStartTile, i);
            else SetAccuracy(ref PlayerDatas[index], overlay.NoCheckStartTile, index);

            _accStrings[0] = Main.Settings.Labels.Accuracy;
            for (int i = 0; i < PlayerDatas.Length; i++) _accStrings[i + 1] = PlayerDatas[i].AccuracyString;
            overlay.AccuracyText.text = string.Concat(_accStrings);
        }
        if (Main.Settings.ShowXAccuracy)
        {
            if (index == -1)
                for (int i = 0; i < PlayerDatas.Length; i++)
                    SetXAccuracy(ref PlayerDatas[i], i);
            else SetXAccuracy(ref PlayerDatas[index], index);

            _xaccStrings[0] = Main.Settings.Labels.XAccuracy;
            for (int i = 0; i < PlayerDatas.Length; i++) _xaccStrings[i + 1] = PlayerDatas[i].XAccuracyString;
            overlay.XAccuracyText.text = string.Concat(_xaccStrings);
        }
    }

    protected void SetAccuracy(ref PlayerData pData, int noCheckStartTile, int i)
    {
        float acc = scrMistakesManager.marginTrackers[i].percentAcc;
        float maxAcc = 1 + (scrPlayerManager.instance.allPlayers[i].planetarySystem.chosenPlanet.currfloor.seqID - noCheckStartTile + 1) * 0.0001f;
        float xacc = scrMistakesManager.marginTrackers[i].percentXAcc;
        if (float.IsNaN(xacc)) xacc = 1;
        pData.AccuracyString = $" | {ColorToString(Main.Settings.Colors.GetAccuracyColor(xacc == 1 ? 1 : acc / maxAcc, xacc == 1))}{Math.Round(acc * 100, DecimalPrecision)}%</color>";
    }

    protected void SetXAccuracy(ref PlayerData pData, int i)
    {
        float xacc = scrMistakesManager.marginTrackers[i].percentXAcc;
        if (float.IsNaN(xacc)) xacc = 1;
        pData.XAccuracyString = $" | {ColorToString(Main.Settings.Colors.GetAccuracyColor(xacc, xacc == 1))}{Math.Round(xacc * 100, DecimalPrecision)}%</color>";
    }

    public void UpdateProgress(Overlay overlay)
    {
        var strings = new string[PlayerDatas.Length + 1];
        strings[0] = Main.Settings.Labels.Progress;
        if (overlay.StartTile > 0)
            strings[0] += $" | {ColorToString(Main.Settings.Colors.GetProgressColor(overlay.StartProgress))}{Math.Round(overlay.StartProgress * 100, DecimalPrecision)}%</color> ~";
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
        while (overlay.Checkpoints.Length > CurCheck && GameRefs.CurrentSeqID >= overlay.Checkpoints[CurCheck])
        {
            CurCheck++; updated = true;
        }
        if (LastCheckpoint == GameRefs.CheckpointsUsed && !updated) return;
        overlay.CheckpointText.text = $"<color=white>{Main.Settings.Labels.Checkpoint} |</color> {GameRefs.CheckpointsUsed} ({CurCheck}/{overlay.Checkpoints.Length})";
        LastCheckpoint = GameRefs.CheckpointsUsed;
    }

    public void UpdateBest(Overlay overlay)
    {
        if (GameRefs.IsAuto && !overlay.AutoOnceEnabled) overlay.AutoOnceEnabled = true;
        if (CurBest == -1)
            CurBest = PlayCount.GetData(overlay.LastHash)?.GetBest(overlay.StartProgress, overlay.LastMultiplier) ?? 0;
        else if (CurBest > MaxProgress || overlay.AutoOnceEnabled) return;
        UpdateBestText(overlay);
    }

    public float GetProgress() => MaxProgress;

    public void UpdateBestText(Overlay overlay)
    {
        float best = CurBest > MaxProgress || overlay.AutoOnceEnabled ? CurBest : MaxProgress;
        overlay.BestText.text = $"<color=white>{Main.Settings.Labels.Best} |</color> {Math.Round(best * 100, DecimalPrecision)}%";
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

    // ===== Jongyeol-mode helpers (coop, per-player) =====

    private int[] _playerDeath;
    private int[] _lastPlayerDeath;

    public void UpdateDeath(Overlay overlay)
    {
        var s = Main.Settings;
        if (!s.ShowDeath || !overlay.GameObject.activeSelf || overlay.DeathText == null) return;
        int count = VersionSafe.GetPlayerCount();
        if (_playerDeath == null || _playerDeath.Length != count)
        {
            _playerDeath = new int[count];
            _lastPlayerDeath = new int[count];
            for (int i = 0; i < count; i++) _lastPlayerDeath[i] = -1;
        }
        var sb = new StringBuilder();
        sb.Append("<color=white>");
        sb.Append(s.Labels.Death);
        sb.Append("</color>");
        bool changed = false;
        for (int i = 0; i < count; i++)
        {
            int[] hits = VersionSafe.GetHitMarginsCountForPlayer(i);
            int death = hits[8] + hits[9];
            _playerDeath[i] = death;
            if (_lastPlayerDeath[i] != death) { _lastPlayerDeath[i] = death; changed = true; }
            string hex = VersionSafe.GetPlayerColorHex(i);
            sb.Append(" | <color=#");
            sb.Append(hex);
            sb.Append(">");
            sb.Append(death);
            sb.Append("</color>");
        }
        if (changed) overlay.DeathText.text = sb.ToString();
        overlay.DeathText.color = Color.white;
    }

    public void UpdateState(Overlay overlay, bool _)
    {
        var s = Main.Settings;
        if (!s.ShowState || !overlay.GameObject.activeSelf || overlay.StateText == null) return;
        var labels = s.Labels;
        int count = VersionSafe.GetPlayerCount();
        var sb = new StringBuilder();
        sb.Append("<color=white>");
        sb.Append(labels.State);
        sb.Append("</color>");
        for (int i = 0; i < count; i++)
        {
            int[] hits = VersionSafe.GetHitMarginsCountForPlayer(i);
            var p = scrPlayerManager.instance.allPlayers[i];
            string state = GetPlayerState(p, hits, overlay);
            string hex = VersionSafe.GetPlayerColorHex(i);
            sb.Append(" | <color=#");
            sb.Append(hex);
            sb.Append(">");
            sb.Append(state);
            sb.Append("</color>");
        }
        if (overlay.StartTile != 0) { sb.Append("  "); sb.Append(labels.StateMidStart); }
        overlay.StateText.text = sb.ToString();
        overlay.StateText.color = Color.white;
    }

    private static string GetPlayerState(scrPlayer player, int[] hits, Overlay overlay)
    {
        var labels = Main.Settings.Labels;
        string state;

        if (GameRefs.CurrentSeqID == overlay.StartTile)
            state = labels.StateWaiting;
        else if (!GameRefs.IsAuto && player.auto)
            state = labels.StateAuto;  // respawn waiting
        else
        {
            var curFloor = player.planetarySystem?.chosenPlanet?.currfloor;
            if (curFloor != null && curFloor.nextfloor is { auto: true })
                state = labels.StateAutoTile;
            else if (GameRefs.IsAuto)
                state = labels.StateAuto;
            else if (IsPurePerfect(hits))
                state = labels.StatePerfectPlay;
            else
            {
                int death = hits[8] + hits[9];
                if (death != 0) state = labels.StateComplete;
                else if (hits[0] != 0) state = labels.StateClear;
                else if (hits[1] != 0 || hits[5] != 0) state = labels.StateNoMiss;
                else state = labels.StatePerfectionist;
            }
        }
        var allFloors = GameRefs.LevelMaker?.listFloors;
        if (allFloors != null && GameRefs.CurrentSeqID != allFloors.Count)
            state += labels.StateSuffix;
        return state;
    }

    private static bool IsPurePerfect(int[] hits)
    {
        for (int i = 0; i < hits.Length && i < 10; i++)
        {
            if (i is 3 or 7) continue;
            if (hits[i] != 0) return false;
        }
        return true;
    }

    public bool CheckPurePerfect(Overlay overlay)
    {
        int count = VersionSafe.GetPlayerCount();
        for (int p = 0; p < count; p++)
        {
            int[] hits = VersionSafe.GetHitMarginsCountForPlayer(p);
            for (int i = 0; i < hits.Length && i < 10; i++)
            {
                if (i is 3 or 7) continue;
                if (hits[i] != 0) return false;
            }
        }
        return true;
    }

    public int GetTooJudgement(Overlay overlay)
    {
        int total = 0;
        int count = VersionSafe.GetPlayerCount();
        for (int i = 0; i < count; i++)
        {
            int[] hits = VersionSafe.GetHitMarginsCountForPlayer(i);
            total += hits[0] + hits[6];
        }
        return total;
    }
}