using HarmonyLib;
using MonsterLove.StateMachine;
using System;
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
            typeof(ControllerAwakeRewindPatch),
            typeof(ControllerChangeStatePatch)
        );

        PatchManager.RegisterPatches(() => Main.Settings.ShowProgress || Main.Settings.ShowAccuracy ||
              Main.Settings.ShowXAccuracy || Main.Settings.ShowMusicTime || Main.Settings.ShowMapTime ||
              Main.Settings.ShowCheckpoint || Main.Settings.ShowBest || Main.Settings.ShowProgressBar ||
              Main.Settings.ShowTimingScale || Main.Settings.ShowAttempt || Main.Settings.ShowFullAttempt,
            typeof(PlanetMoveToNextFloorPatch));
        PatchManager.RegisterPatches(() => Main.Settings.JongyeolMode,
            typeof(ScrShowIfDebugUpdatePatch),
            typeof(ScrShowIfDebugAwakePatch),
            typeof(RdcSetAutoPatch),
            typeof(ScrMiscGetHitMarginPatch));

        // Always capture beta watermark reference when it awakens
        PatchManager.RegisterPatches(() => true, typeof(BetaWatermarkCapturePatch));
    }

}

// ========== Lifecycle ==========

[HarmonyPatch(typeof(StateBehaviour), nameof(StateBehaviour.ChangeState), [typeof(Enum)])]
internal static class ControllerChangeStatePatch
{
    static void Postfix(Enum newState)
    {
        switch ((States)newState)
        {
            case States.Fail2: GameLifecycleHelper.GetOverlay()?.Death(); break;
            case States.Won: GameLifecycleHelper.GetOverlay()?.Clear(); break;
        }
    }
}

[HarmonyPatch(typeof(scnGame), nameof(scnGame.Play))]
internal static class ScnGamePlayPatch
{
    static void Postfix(int seqID)
    {
        if (GCS.practiceMode) return;
        GameLifecycleHelper.GetOverlay()?.Show(seqID);
    }
}

[HarmonyPatch(typeof(scrPressToStart), nameof(scrPressToStart.ShowText))]
internal static class PressToStartShowTextPatch
{
    static void Postfix()
    {
        if (!GCS.practiceMode && scnGame.instance) return;
        GameLifecycleHelper.GetOverlay()?.Show(scrController.instance.currentSeqID);
    }
}

[HarmonyPatch(typeof(scrUIController), nameof(scrUIController.WipeToBlack))]
internal static class UIControllerWipeToBlackPatch
{
    static void Postfix() => GameLifecycleHelper.GetOverlay()?.Hide();
}

[HarmonyPatch(typeof(scnEditor), "ResetScene")]
internal static class ScnEditorResetScenePatch
{
    static void Postfix() => GameLifecycleHelper.GetOverlay()?.Hide();
}

[HarmonyPatch(typeof(scrController), nameof(scrController.StartLoadingScene))]
internal static class ControllerStartLoadingScenePatch
{
    static void Postfix() => GameLifecycleHelper.GetOverlay()?.Hide();
}

[HarmonyPatch(typeof(scrController), nameof(scrController.Awake_Rewind))]
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

// ========== Progress / Timing / Attempt (version-agnostic, merged) ==========

[HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
internal static class PlanetMoveToNextFloorPatch
{
    static void Postfix(scrPlanet __instance)
    {
        var overlay = GameLifecycleHelper.GetOverlay();
        if (overlay == null) return;
        var s = Main.Settings;
        if (s.ShowProgress || s.ShowAccuracy || s.ShowXAccuracy || s.ShowMusicTime || s.ShowMapTime || s.ShowCheckpoint || s.ShowBest || s.ShowProgressBar)
            overlay.UpdateProgress(__instance);
        if (s.ShowTimingScale) overlay.UpdateTimingScale();
        if (s.ShowAttempt || s.ShowFullAttempt) overlay.UpdateAttempts();
    }
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

[HarmonyPatch(typeof(scrMisc), nameof(scrMisc.GetHitMargin))]
internal static class ScrMiscGetHitMarginPatch
{
    static void Postfix(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch)
    {
        float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
        float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
        Jongyeol.JOverlay.Instance?.UpdateTiming(timing);
    }
}

// ========== Beta watermark capture ==========

[HarmonyPatch(typeof(scrEnableIfBeta), "Awake")]
internal static class BetaWatermarkCapturePatch
{
    static void Postfix(scrEnableIfBeta __instance)
    {
        if (__instance.setBuildText)
        {
            Overlay.BetaWatermark = __instance;
            var rt = __instance.GetComponent<RectTransform>();
            if (rt != null)
                Overlay.BetaWatermarkOriginalPos = rt.anchoredPosition;
        }
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
