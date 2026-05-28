using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace JipperOverlayer.Overlayer.Features;

internal static class GameLifecyclePatches
{
    public static void Register()
    {
        PatchManager.RegisterPatches(() => true,
            typeof(ScnGamePlayPatch),
            typeof(PressToStartShowTextPatch),
            typeof(UIControllerWipeToBlackPatch),
            typeof(ScnEditorResetScenePatch),
            typeof(ControllerStartLoadingScenePatch),
            typeof(MistakesManagerSetPlayerCountPatch),
            typeof(ControllerAwakeRewindPatch)
        );
        RegisterChangeStatePatch();

        PatchManager.RegisterPatches(() => Main.Settings.ShowProgress || Main.Settings.ShowAccuracy ||
              Main.Settings.ShowXAccuracy || Main.Settings.ShowMusicTime || Main.Settings.ShowMapTime ||
              Main.Settings.ShowCheckpoint || Main.Settings.ShowBest || Main.Settings.ShowProgressBar,
            typeof(PlanetMoveToNextFloorStatusPatch));
        PatchManager.RegisterPatches(() => Main.Settings.ShowTimingScale,
            typeof(PlanetMoveToNextFloorTimingPatch));
        PatchManager.RegisterPatches(() => Main.Settings.ShowAttempt || Main.Settings.ShowFullAttempt,
            typeof(PlanetMoveToNextFloorAttemptPatch));
        PatchManager.RegisterPatches(() => Main.Settings.JongyeolMode,
            typeof(ScrShowIfDebugUpdatePatch),
            typeof(ScrShowIfDebugAwakePatch),
            typeof(RdcSetAutoPatch),
            typeof(ScrMiscGetHitMarginPatch));
    }

    static void RegisterChangeStatePatch()
    {
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        MethodInfo target = null;
        var sbType = AccessTools.TypeByName("StateBehaviour") ?? AccessTools.TypeByName("MonsterLove.StateMachine.StateBehaviour");
        if (sbType != null) target = sbType.GetMethod("ChangeState", flags);
        if (target == null) target = typeof(scrController).GetMethod("ChangeState", flags);
        if (target == null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.GetMethod("ChangeState", flags, null, [typeof(Enum)], null) != null)
                    { target = t.GetMethod("ChangeState", flags, null, [typeof(Enum)], null); break; }
                }
                if (target != null) break;
            }
        }
        if (target != null)
        {
            var postfix = SymbolExtensions.GetMethodInfo((Enum newState) => OnChangeState(newState));
            PatchManager.RegisterManualPatch(target, postfix, () => true, "ChangeState");
        }
        else Main.Mod.Logger.Warning("ChangeState not found — death/clear disabled");
    }

    static void OnChangeState(Enum newState)
    {
        switch ((States)newState)
        {
            case States.Fail2: GameLifecycleHelper.GetOverlay()?.Death(); break;
            case States.Won: GameLifecycleHelper.GetOverlay()?.Clear(); break;
        }
    }
}

// ========== Lifecycle ==========

[HarmonyPatch(typeof(scnGame), "Play")]
internal static class ScnGamePlayPatch
{
    static void Postfix(int seqID)
    {
        if (GCS.practiceMode) return;
        GameLifecycleHelper.GetOverlay()?.Show(seqID);
    }
}

[HarmonyPatch(typeof(scrPressToStart), "ShowText")]
internal static class PressToStartShowTextPatch
{
    static void Postfix()
    {
        if (!GCS.practiceMode && scnGame.instance) return;
        GameLifecycleHelper.GetOverlay()?.Show(scrController.instance.currentSeqID);
    }
}

[HarmonyPatch(typeof(scrUIController), "WipeToBlack")]
internal static class UIControllerWipeToBlackPatch
{
    static void Postfix() => GameLifecycleHelper.GetOverlay()?.Hide();
}

[HarmonyPatch(typeof(scnEditor), "ResetScene")]
internal static class ScnEditorResetScenePatch
{
    static void Postfix() => GameLifecycleHelper.GetOverlay()?.Hide();
}

[HarmonyPatch(typeof(scrController), "StartLoadingScene")]
internal static class ControllerStartLoadingScenePatch
{
    static void Postfix() => GameLifecycleHelper.GetOverlay()?.Hide();
}

[HarmonyPatch(typeof(scrMistakesManager), "SetPlayerCount")]
internal static class MistakesManagerSetPlayerCountPatch
{
    static void Postfix() => GameLifecycleHelper.GetOverlay()?.OnChangePlayers();
}

[HarmonyPatch(typeof(scrController), "Awake_Rewind")]
internal static class ControllerAwakeRewindPatch
{
    static void Postfix(Text ___txtLevelName)
    {
        if (!___txtLevelName) return;
        RectTransform t = ___txtLevelName.GetComponent<RectTransform>();
        float size = Main.Settings.Size;
        t.anchoredPosition = new Vector2(0, -20 - 7 * size);
        t.localScale = new Vector3(0.5f * size, 0.5f * size);
        t.sizeDelta = new Vector2(Math.Abs(t.sizeDelta.x * 2.5f), t.sizeDelta.y);
        ___txtLevelName.text = ___txtLevelName.text.Replace('\n', ' ');
    }
}

// ========== Progress / Timing / Attempt (version-agnostic) ==========

[HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
internal static class PlanetMoveToNextFloorStatusPatch
{
    static void Postfix(scrPlanet __instance) { GameLifecycleHelper.GetOverlay()?.UpdateProgress(__instance); }
}

[HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
internal static class PlanetMoveToNextFloorTimingPatch
{
    static void Postfix() { if (Main.Settings.ShowTimingScale) GameLifecycleHelper.GetOverlay()?.UpdateTimingScale(); }
}

[HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
internal static class PlanetMoveToNextFloorAttemptPatch
{
    static void Postfix() { GameLifecycleHelper.GetOverlay()?.UpdateAttempts(); }
}

// ========== Jongyeol UI (version-agnostic) ==========

[HarmonyPatch(typeof(scrShowIfDebug), "Update")]
internal static class ScrShowIfDebugUpdatePatch
{
    static bool Prefix(Text ___txt)
    {
        if (Main.Settings.HideDebugText) { ___txt.enabled = false; return false; }
        return true;
    }
}

[HarmonyPatch(typeof(scrShowIfDebug), "Awake")]
internal static class ScrShowIfDebugAwakePatch
{
    static async void Postfix(scrShowIfDebug __instance)
    {
        await System.Threading.Tasks.Task.Delay(1);
        try
        {
            if (__instance)
            {
                var t = __instance.GetComponent<RectTransform>();
                t.anchoredPosition = new Vector2(300, t.anchoredPosition.y);
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(RDC), "set_auto")]
internal static class RdcSetAutoPatch
{
    static void Postfix()
    {
        if (!ADOBase.isScnGame) return;
        Jongyeol.JOverlay.Instance?.SetupLocationMain();
        Jongyeol.JOverlay.Instance?.UpdateState();
    }
}

[HarmonyPatch(typeof(scrMisc), "GetHitMargin")]
internal static class ScrMiscGetHitMarginPatch
{
    static void Postfix(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch)
    {
        float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
        float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
        Jongyeol.JOverlay.Instance?.UpdateTiming(timing);
    }
}

// ========== Helper ==========

internal static class GameLifecycleHelper
{
    public static int ComboCount;

    public static Overlay GetOverlay()
    {
        if (Main.Settings.JongyeolMode) return Jongyeol.JOverlay.Instance;
        return Overlay.Instance;
    }
}
