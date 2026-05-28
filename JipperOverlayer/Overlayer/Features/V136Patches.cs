using HarmonyLib;

// =========================================================================
// v136 feature patches ONLY — mutually exclusive with V141Patches
// Targets: scrMistakesManager (static/singleton), scrController, etc.
// =========================================================================

namespace JipperOverlayer.Overlayer.Features;

internal static class V136Patches
{
    public static void RegisterAll()
    {
        // BPM (v136: scrController.Hit)
        PatchManager.RegisterPatches(() => Main.Settings.ShowBPM, typeof(ScrControllerHitBpmPatch));

        // Combo (v136: scrMistakesManager.AddHit) — disabled when JCombo takes over
        PatchManager.RegisterPatches(() => Main.Settings.ShowCombo && !(Main.Settings.JongyeolMode && Main.Settings.YellowCombo), typeof(ScrMistakesAddHitComboPatch));

        // Judgement (v136: scrMistakesManager.AddHit / Reset)
        PatchManager.RegisterPatches(() => Main.Settings.ShowJudgement,
            typeof(ScrMistakesAddHitJudgementPatch),
            typeof(ScrMistakesResetPatch));

        // Accuracy (v136: scrMistakesManager.CalculatePercentAcc)
        PatchManager.RegisterPatches(() => Main.Settings.ShowProgress || Main.Settings.ShowAccuracy ||
              Main.Settings.ShowXAccuracy || Main.Settings.ShowMusicTime || Main.Settings.ShowMapTime ||
              Main.Settings.ShowCheckpoint || Main.Settings.ShowBest || Main.Settings.ShowProgressBar,
            typeof(ScrMistakesCalcAccPatch));

        // Jongyeol Combo (v136: scrMistakesManager.AddHit)
        PatchManager.RegisterPatches(() => Main.Settings.JongyeolMode && Main.Settings.YellowCombo,
            typeof(ScrMistakesAddHitJComboPatch));
    }
}

// ===== BPM =====

[HarmonyPatch(typeof(scrController), "Hit")]
internal static class ScrControllerHitBpmPatch
{
    static void Postfix() { if (Main.Settings.ShowBPM) GameLifecycleHelper.GetOverlay()?.UpdateBPM(); }
}

// ===== Combo =====

[HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
internal static class ScrMistakesAddHitComboPatch
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

[HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
internal static class ScrMistakesAddHitJudgementPatch
{
    static void Postfix() { if (Main.Settings.ShowJudgement) GameLifecycleHelper.GetOverlay()?.UpdateJudgement(); }
}

[HarmonyPatch(typeof(scrMistakesManager), "Reset")]
internal static class ScrMistakesResetPatch
{
    static void Postfix() { if (Main.Settings.ShowJudgement) GameLifecycleHelper.GetOverlay()?.UpdateJudgement(); }
}

// ===== Accuracy =====

[HarmonyPatch(typeof(scrMistakesManager), "CalculatePercentAcc")]
internal static class ScrMistakesCalcAccPatch
{
    static void Postfix() { GameLifecycleHelper.GetOverlay()?.UpdateAccuracy(-1); }
}

// ===== Jongyeol Combo =====

[HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
internal static class ScrMistakesAddHitJComboPatch
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
