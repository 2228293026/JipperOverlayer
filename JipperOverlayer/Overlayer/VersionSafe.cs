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

    // Single-slot memo for the per-hit player nameplate hex string
    private static int _cachedPlayerHexIdx = -1;
    private static Color _cachedPlayerHexColor;
    private static string _cachedPlayerHex;

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
                    && PatchManager.CreateStaticMemberGetter(typeof(ADOBase), "playerManager") != null; }
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
            Color c = scrPlayerManager.playerColors[playerIdx].ToRealColor();
            if (_cachedPlayerHexIdx == playerIdx && _cachedPlayerHexColor == c)
                return _cachedPlayerHex;
            _cachedPlayerHexIdx = playerIdx;
            _cachedPlayerHexColor = c;
            return _cachedPlayerHex = ColorUtility.ToHtmlStringRGB(c);
        };
    }

    // ===== v136 — full reflection, no direct member access =====
    private static void BindV136Delegates()
    {
        var mmType = typeof(scrMistakesManager);

        // hitMarginsCount — static field or static property (type varies by version)
        var hitMarginsGetter = TryStaticMemberGetter(mmType, "hitMarginsCount");
        _getHitMarginsCount = () =>
        {
            var v = hitMarginsGetter?.Invoke();
            return v is int[] arr ? arr : new int[11];
        };

        // speed — instance field or property (type varies by version)
        var speedGetter = TryMemberGetter<scrController>("speed");
        _getPlanetSpeed = ctrl =>
        {
            if (speedGetter == null || ctrl == null) return 1.0;
            var v = speedGetter(ctrl);
            return v is double d ? d : v is float f ? f : 1.0;
        };

        // mistakesManager — instance field or property + cached static getter
        var mmGetter = TryMemberGetter<scrController>("mistakesManager");
        var instanceGetter = TryStaticMemberGetter(typeof(scrController), "_instance");
        scrMistakesManager GetMM()
        {
            if (mmGetter == null || instanceGetter == null) return null;
            var ctrl = instanceGetter();
            return ctrl is scrController c && mmGetter(c) is scrMistakesManager mm ? mm : null;
        }

        var calcAcc = TryMethodInfo(mmType, "CalculatePercentAcc");
        _calculatePercentAcc = () => calcAcc?.Invoke(GetMM(), null);

        // percentAcc / percentXAcc — instance fields or properties on mistakesManager
        var accGetter = TryMemberGetter<scrMistakesManager>("percentAcc");
        _getPercentAcc = () =>
        {
            var mm = GetMM();
            return mm != null && accGetter != null && accGetter(mm) is float f ? f : 1f;
        };

        var xAccGetter = TryMemberGetter<scrMistakesManager>("percentXAcc");
        _getPercentXAcc = () =>
        {
            var mm = GetMM();
            return mm != null && xAccGetter != null && xAccGetter(mm) is float f ? f : 1f;
        };

        _isCoopMode = () => false;
        _getHideWithNoAuto = _ => true;
        _getPlayerCount = () => 1;
        _getPlayerIndex = _ => 0;
        _getHitMarginsCountForPlayer = (_) => GetHitMarginsCount();
        _getPlayerColorHex = (_) => "";
    }

    // ===== Safe wrappers — return null on miss instead of throwing =====
    private static Func<object> TryStaticMemberGetter(Type type, string name)
    {
        try { return PatchManager.CreateStaticMemberGetter(type, name); }
        catch (Exception e) { Loader.Warning($"VersionSafe: 字段或属性 {type.Name}.{name} 不存在 ({e.Message})"); return null; }
    }

    private static Func<T, object> TryMemberGetter<T>(string name) where T : class
    {
        try { return PatchManager.CreateMemberGetter<T>(name); }
        catch (Exception e) { Loader.Warning($"VersionSafe: 字段或属性 {typeof(T).Name}.{name} 不存在 ({e.Message})"); return null; }
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