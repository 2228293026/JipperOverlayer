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
        Loader.Log($"API version: {(IsV141OrLater ? "v141+" : "v136")}");

        if (IsV141OrLater)
            BindV141Delegates();
        else
            BindV136Delegates();
    }

    private static bool DetectApiVersion()
    {
        try { return AccessTools.TypeByName("scrMarginTracker") != null
                    && PatchManager.CreateStaticPropertyGetter<object>(typeof(ADOBase), "playerManager") != null; }
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

        // Try zero-reflection IL getter; fall back to original GetValue lambda
        {
            var getter = TryStaticFieldGetter<int[]>(mmType, "hitMarginsCount");
            _getHitMarginsCount = getter ?? (() => new int[11]);
        }

        // Instance field: cached FieldInfo (field type varies by version)
        var speedField = TryFieldInfo(typeof(scrController), "speed");
        _getPlanetSpeed = ctrl =>
        {
            if (speedField == null) return 1.0;
            var v = speedField.GetValue(ctrl);
            return v is double d ? d : v is float f ? f : 1.0;
        };

        // Resolve mistakesManager via cached FieldInfo + cached static getter
        var mmField = TryFieldInfo(typeof(scrController), "mistakesManager");
        var instanceGetter = TryStaticFieldGetter<scrController>(typeof(scrController), "_instance");
        scrMistakesManager GetMM()
        {
            if (mmField == null || instanceGetter == null) return null;
            var ctrl = instanceGetter();
            return ctrl != null ? (scrMistakesManager)mmField.GetValue(ctrl) : null;
        }

        var calcAcc = TryMethodInfo(mmType, "CalculatePercentAcc");
        _calculatePercentAcc = () => calcAcc?.Invoke(GetMM(), null);

        // Instance fields on mistakesManager
        var accField = TryFieldInfo(mmType, "percentAcc");
        _getPercentAcc = () =>
        {
            var mm = GetMM();
            return mm != null && accField != null ? (float)accField.GetValue(mm) : 1f;
        };

        var xAccField = TryFieldInfo(mmType, "percentXAcc");
        _getPercentXAcc = () =>
        {
            var mm = GetMM();
            return mm != null && xAccField != null ? (float)xAccField.GetValue(mm) : 1f;
        };

        _isCoopMode = () => false;
        _getHideWithNoAuto = _ => true;
        _getPlayerCount = () => 1;
        _getPlayerIndex = _ => 0;
        _getHitMarginsCountForPlayer = (_) => GetHitMarginsCount();
        _getPlayerColorHex = (_) => "";
    }

    // ===== Safe wrappers — return null on miss instead of throwing =====
    private static Func<TField> TryStaticFieldGetter<TField>(Type type, string name)
    {
        try { return PatchManager.CreateStaticFieldGetter<TField>(type, name); }
        catch (Exception e) { Loader.Warning($"VersionSafe: 字段 {type.Name}.{name} 不存在 ({e.Message})"); return null; }
    }

    private static FieldInfo TryFieldInfo(Type type, string name)
    {
        try { return PatchManager.GetFieldInfo(type, name); }
        catch (Exception e) { Loader.Warning($"VersionSafe: 字段 {type.Name}.{name} 不存在 ({e.Message})"); return null; }
    }

    private static MethodInfo TryMethodInfo(Type type, string name)
    {
        try { return PatchManager.GetMethodInfo(type, name); }
        catch (Exception e) { Loader.Warning($"VersionSafe: 方法 {type.Name}.{name} 不存在 ({e.Message})"); return null; }
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