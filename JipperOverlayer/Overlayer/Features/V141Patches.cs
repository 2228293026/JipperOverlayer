using HarmonyLib;

// =========================================================================
// v141+ feature patches ONLY — mutually exclusive with V136Patches
// Targets: scrMarginTracker (per-player), scrPlayer, etc.
// =========================================================================

namespace JipperOverlayer.Overlayer.Features;

internal static class V141Patches
{
    public static void RegisterAll()
    {
        // BPM (v141: scrPlayer.Hit)
        PatchManager.RegisterPatches(() => Main.Settings.ShowBPM, typeof(ScrPlayerHitBpmPatch));

        // Combo (v141: scrMarginTracker.AddHit) — disabled when JCombo takes over
        PatchManager.RegisterPatches(() => Main.Settings.ShowCombo && !(Main.Settings.JongyeolMode && Main.Settings.YellowCombo), typeof(ScrMarginAddHitComboPatch));

        // Judgement (v141: scrMarginTracker.AddHit / Reset)
        PatchManager.RegisterPatches(() => Main.Settings.ShowJudgement,
            typeof(ScrMarginAddHitJudgementPatch),
            typeof(ScrMarginResetPatch));

        // Accuracy (v141: scrMarginTracker.CalculatePercentAcc)
        PatchManager.RegisterPatches(() => Main.Settings.ShowProgress || Main.Settings.ShowAccuracy ||
              Main.Settings.ShowXAccuracy || Main.Settings.ShowMusicTime || Main.Settings.ShowMapTime ||
              Main.Settings.ShowCheckpoint || Main.Settings.ShowBest || Main.Settings.ShowProgressBar,
            typeof(ScrMarginCalcAccPatch));

        // Jongyeol Combo (v141: scrMarginTracker.AddHit)
        PatchManager.RegisterPatches(() => Main.Settings.JongyeolMode && Main.Settings.YellowCombo,
            typeof(ScrMarginAddHitJComboPatch));

        // Player count change (v141: scrMistakesManager.SetPlayerCount)
        PatchManager.RegisterPatches(() => true, typeof(MistakesManagerSetPlayerCountPatch));
    }
}

// ===== BPM =====

[HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.Hit))]
internal static class ScrPlayerHitBpmPatch
{
    static void Postfix() { if (Main.Settings.ShowBPM) GameLifecycleHelper.GetOverlay()?.UpdateBPM(); }
}

// ===== Combo =====

[HarmonyPatch(typeof(scrMarginTracker), nameof(scrMarginTracker.AddHit))]
internal static class ScrMarginAddHitComboPatch
{
    static void Postfix(HitMargin hit)
    {
        var overlay = GameLifecycleHelper.GetOverlay();
        if (overlay == null || !Main.Settings.ShowCombo) return;
        if (hit == HitMargin.Perfect || (Main.Settings.EnableAutoCombo && hit == HitMargin.Auto))
            overlay.UpdateCombo(++GameLifecycleHelper.ComboCount, true);
        else if (Main.Settings.EnableAutoCombo || hit != HitMargin.Auto)
        {
            overlay.UpdateCombo(GameLifecycleHelper.ComboCount = 0, false);
            overlay.OnNonPerfectHit();
        }
    }
}

// ===== Judgement =====

[HarmonyPatch(typeof(scrMarginTracker), nameof(scrMarginTracker.AddHit))]
internal static class ScrMarginAddHitJudgementPatch
{
    static void Postfix() { if (Main.Settings.ShowJudgement) GameLifecycleHelper.GetOverlay()?.UpdateJudgement(); }
}

[HarmonyPatch(typeof(scrMarginTracker), nameof(scrMarginTracker.Reset))]
internal static class ScrMarginResetPatch
{
    static void Postfix() { if (Main.Settings.ShowJudgement) GameLifecycleHelper.GetOverlay()?.UpdateJudgement(); }
}

[HarmonyPatch(typeof(scrMistakesManager), nameof(scrMistakesManager.SetPlayerCount))]
internal static class MistakesManagerSetPlayerCountPatch
{
    static void Postfix() => GameLifecycleHelper.GetOverlay()?.OnChangePlayers();
}

// ===== Accuracy =====

[HarmonyPatch(typeof(scrMarginTracker), nameof(scrMarginTracker.CalculatePercentAcc))]
internal static class ScrMarginCalcAccPatch
{
    static void Postfix(scrMarginTracker __instance)
    {
        int index = 0;
        if (scrController.coopMode)
        {
            for (int i = 0; i < scrPlayerManager.playerCount; i++)
            {
                if (scrMistakesManager.marginTrackers[i] != __instance) continue;
                index = i; break;
            }
        }
        GameLifecycleHelper.GetOverlay()?.UpdateAccuracy(index);
    }
}

// ===== Jongyeol Combo =====

[HarmonyPatch(typeof(scrMarginTracker), nameof(scrMarginTracker.AddHit))]
internal static class ScrMarginAddHitJComboPatch
{
    static void Postfix(HitMargin hit)
    {
        var overlay = Jongyeol.JOverlay.Instance;
        if (overlay == null) return;
        switch (hit)
        {
            case HitMargin.Perfect:
            case HitMargin.EarlyPerfect:
            case HitMargin.LatePerfect:
            case HitMargin.Auto when Main.Settings.EnableAutoCombo:
                overlay.UpdateCombo(++GameLifecycleHelper.ComboCount, true);
                break;
            case HitMargin.Auto when !Main.Settings.EnableAutoCombo:
                break;
            default:
                overlay.UpdateCombo(GameLifecycleHelper.ComboCount = 0, false);
                break;
        }
        if (hit is not HitMargin.Perfect and not HitMargin.Auto)
            overlay.PerfectToCombo();
    }
}
