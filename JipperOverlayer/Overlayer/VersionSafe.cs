using System;
using System.Reflection;
using HarmonyLib;

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
        _isCoopMode = () => scrPlayerManager.playerCount > 1;
        _getHideWithNoAuto = instance => instance.hideWithNoAuto;
    }

    // ===== v136 — full reflection (no direct member access to avoid JIT resolution) =====

    private static void BindV136Delegates()
    {
        var mmType = typeof(scrMistakesManager);

        var hitField = mmType.GetField("hitMarginsCount", BindingFlags.Public | BindingFlags.Static);
        _getHitMarginsCount = () => (int[])(hitField?.GetValue(null)) ?? new int[11];

        var speedField = typeof(scrController).GetField("speed", BindingFlags.Public | BindingFlags.Instance);
        _getPlanetSpeed = ctrl => { var v = speedField?.GetValue(ctrl); return v is double d ? d : v is float f ? f : 1.0; };

        // Resolve mistakenManager via FieldInfo.GetValue — avoids JIT MissingMethodException
        var mmField = typeof(scrController).GetField("mistakesManager", BindingFlags.Public | BindingFlags.Instance);
        var _instanceField = typeof(scrController).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        object GetMM() => mmField?.GetValue(_instanceField?.GetValue(null));

        var calcAcc = mmType.GetMethod("CalculatePercentAcc", BindingFlags.Public | BindingFlags.Instance);
        _calculatePercentAcc = () => calcAcc?.Invoke(GetMM(), null);

        var accField = mmType.GetField("percentAcc", BindingFlags.Public | BindingFlags.Instance);
        _getPercentAcc = () => (float?)(accField?.GetValue(GetMM())) ?? 1f;

        var xAccField = mmType.GetField("percentXAcc", BindingFlags.Public | BindingFlags.Instance);
        _getPercentXAcc = () => (float?)(xAccField?.GetValue(GetMM())) ?? 1f;

        _isCoopMode = () => false;
        _getHideWithNoAuto = _ => true;
    }

    // ========== Public API (delegate-forwarded, zero reflection) ==========

    public static int[] GetHitMarginsCount() => _getHitMarginsCount?.Invoke() ?? new int[11];
    public static double GetPlanetSpeed(scrController ctrl) => _getPlanetSpeed?.Invoke(ctrl) ?? 1.0;
    public static void CalculatePercentAcc() => _calculatePercentAcc?.Invoke();
    public static float GetPercentAcc() => _getPercentAcc?.Invoke() ?? 1f;
    public static float GetPercentXAcc() => _getPercentXAcc?.Invoke() ?? 1f;
    public static bool IsCoopMode() => _isCoopMode?.Invoke() ?? false;
    public static int GetPlayerIndex(object tracker)
    {
        if (!IsV141OrLater || scrMistakesManager.marginTrackers == null) return 0;
        for (int i = 0; i < scrPlayerManager.playerCount; i++)
        {
            if ((object)scrMistakesManager.marginTrackers[i] == tracker) return i;
        }
        return 0;
    }
    public static bool GetHideWithNoAuto(scrShowIfDebug instance) => _getHideWithNoAuto?.Invoke(instance) ?? true;
}
