using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace JipperOverlayer.Overlayer;

public static class VersionSafe
{
    public static bool IsInitialized { get; private set; }
    public static bool IsV141OrLater { get; private set; } = true;

    // Cached function pointers — zero reflection at runtime
    private static Func<int[]> _getHitMarginsCount;
    private static Func<scrController, double> _getPlanetSpeed;
    private static Action _calculatePercentAcc;
    private static Func<float> _getPercentAcc;
    private static Func<float> _getPercentXAcc;
    private static Func<bool> _isCoopMode;
    private static Func<scrShowIfDebug, bool> _getHideWithNoAuto;
    private static Func<int> _getPlayerCount;
    private static Func<object, int> _getPlayerIndex;
    private static Func<int, int[]> _getHitMarginsCountForPlayer;
    private static Func<int, string> _getPlayerColorHex;

    public static void Setup()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        IsV141OrLater = DetectApiVersion();
        Main.Mod.Logger.Log($"API version: {(IsV141OrLater ? "v141+" : "v136")}");

        if (IsV141OrLater)
            BindV141Delegates();
        else
            BindV136Delegates();
    }

    private static bool DetectApiVersion()
    {
        try { return AccessTools.TypeByName("scrMarginTracker") != null
                    && typeof(ADOBase).GetProperty("playerManager") != null; }
        catch { return false; }
    }

    // ===== v141+ — direct access, zero overhead =====

    private static void BindV141Delegates()
    {
        _getHitMarginsCount = () =>
        {
            if (scrMistakesManager.marginTrackers == null || scrMistakesManager.marginTrackers.Length == 0)
                return new int[11];
            return scrMistakesManager.marginTrackers[0].hitMarginsCount;
        };

        _getPlanetSpeed = ctrl =>
        {
            if (ctrl.playerOne?.planetarySystem != null)
                return ctrl.playerOne.planetarySystem.speed;
            return 1.0;
        };

        _calculatePercentAcc = () =>
        {
            if (scrMistakesManager.marginTrackers == null) return;
            foreach (var t in scrMistakesManager.marginTrackers)
                t?.CalculatePercentAcc();
        };

        _getPercentAcc = () => ADOBase.playerManager?.mistakesManager?.percentAcc ?? 1f;
        _getPercentXAcc = () => ADOBase.playerManager?.mistakesManager?.percentXAcc ?? 1f;
        _isCoopMode = () => GetPlayerCount() > 1;
        _getHideWithNoAuto = instance => instance.hideWithNoAuto;

        _getPlayerCount = () => scrMistakesManager.marginTrackers?.Length ?? 1;

        _getPlayerIndex = tracker =>
        {
            if (tracker == null || scrMistakesManager.marginTrackers == null)
                return 0;
            var trackers = scrMistakesManager.marginTrackers;
            for (int i = 0; i < trackers.Length; i++)
            {
                if (trackers[i] == tracker)
                    return i;
            }
            return 0;
        };
        _getHitMarginsCountForPlayer = (playerIdx) =>
        {
            if (scrMistakesManager.marginTrackers == null || playerIdx >= scrMistakesManager.marginTrackers.Length)
                return new int[11];
            return scrMistakesManager.marginTrackers[playerIdx]?.hitMarginsCount ?? new int[11];
        };

        _getPlayerColorHex = (playerIdx) =>
        {
            if (scrPlayerManager.playerColors == null || playerIdx >= scrPlayerManager.playerColors.Length)
                return "FFFFFF";
            return ColorUtility.ToHtmlStringRGB(scrPlayerManager.playerColors[playerIdx].ToRealColor());
        };
    }

    // ===== v136 — full reflection, no direct member access =====
    private static void BindV136Delegates()
    {
        var mmType = typeof(scrMistakesManager);

        var hitField = mmType.GetField("hitMarginsCount", BindingFlags.Public | BindingFlags.Static);
        _getHitMarginsCount = () => (int[])(hitField?.GetValue(null)) ?? new int[11];

        var speedField = typeof(scrController).GetField("speed", BindingFlags.Public | BindingFlags.Instance);
        _getPlanetSpeed = ctrl => { var v = speedField?.GetValue(ctrl); return v is double d ? d : v is float f ? f : 1.0; };

        // Resolve mistakesManager via FieldInfo.GetValue
        var mmField = typeof(scrController).GetField("mistakesManager", BindingFlags.Public | BindingFlags.Instance);
        var instanceField = typeof(scrController).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        object GetMM() => mmField?.GetValue(instanceField?.GetValue(null));

        var calcAcc = mmType.GetMethod("CalculatePercentAcc", BindingFlags.Public | BindingFlags.Instance);
        _calculatePercentAcc = () => calcAcc?.Invoke(GetMM(), null);

        var accField = mmType.GetField("percentAcc", BindingFlags.Public | BindingFlags.Instance);
        _getPercentAcc = () => (float?)(accField?.GetValue(GetMM())) ?? 1f;

        var xAccField = mmType.GetField("percentXAcc", BindingFlags.Public | BindingFlags.Instance);
        _getPercentXAcc = () => (float?)(xAccField?.GetValue(GetMM())) ?? 1f;

        _isCoopMode = () => false;
        _getHideWithNoAuto = _ => true;
        _getPlayerCount = () => 1;
        _getPlayerIndex = _ => 0;
        _getHitMarginsCountForPlayer = (_) => GetHitMarginsCount();
        _getPlayerColorHex = (_) => "";
    }

    // ========== Public API ==========
    public static int[] GetHitMarginsCount() => _getHitMarginsCount?.Invoke() ?? new int[11];
    public static double GetPlanetSpeed(scrController ctrl) => _getPlanetSpeed?.Invoke(ctrl) ?? 1.0;
    public static void CalculatePercentAcc() => _calculatePercentAcc?.Invoke();
    public static float GetPercentAcc() => _getPercentAcc?.Invoke() ?? 1f;
    public static float GetPercentXAcc() => _getPercentXAcc?.Invoke() ?? 1f;
    public static bool IsCoopMode() => _isCoopMode?.Invoke() ?? false;
    public static bool GetHideWithNoAuto(scrShowIfDebug instance) => _getHideWithNoAuto?.Invoke(instance) ?? true;

    public static int GetPlayerCount() => _getPlayerCount?.Invoke() ?? 1;

    public static int GetPlayerIndex(object tracker) => _getPlayerIndex?.Invoke(tracker) ?? 0;
    public static int[] GetHitMarginsCountForPlayer(int playerIdx) => _getHitMarginsCountForPlayer?.Invoke(playerIdx) ?? GetHitMarginsCount();
    public static string GetPlayerColorHex(int playerIdx) => _getPlayerColorHex?.Invoke(playerIdx) ?? "";
}