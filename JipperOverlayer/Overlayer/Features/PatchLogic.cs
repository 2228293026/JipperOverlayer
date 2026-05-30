using HarmonyLib;

namespace JipperOverlayer.Overlayer.Features;

internal static class PatchLogic
{
    public static void BpmPostfix() => GameLifecycleHelper.GetOverlay()?.UpdateBPM();

    public static void JudgementPostfix() { if (Main.Settings.ShowJudgement) GameLifecycleHelper.GetOverlay()?.UpdateJudgement(); }

    public static void ResetPostfix() { if (Main.Settings.ShowJudgement) GameLifecycleHelper.GetOverlay()?.UpdateJudgement(); }

    public static void AccuracyPostfixV136() => GameLifecycleHelper.GetOverlay()?.UpdateAccuracy(-1);

    public static void AccuracyPostfixV141(scrMarginTracker __instance)
    {
        int index = VersionSafe.GetPlayerIndex(__instance);
        GameLifecycleHelper.GetOverlay()?.UpdateAccuracy(index);
    }

    public static void ComboPostfix(HitMargin hit)
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

    public static void JComboPostfix(HitMargin hit)
    {
        var overlay = Overlay.Instance;
        if (overlay?.Jongyeol == null) return;
        switch (hit)
        {
            case HitMargin.Perfect:
            case HitMargin.EarlyPerfect:
            case HitMargin.LatePerfect:
            case HitMargin.Auto when Main.Settings.EnableAutoCombo:
                overlay.UpdateCombo(++GameLifecycleHelper.ComboCount, true);
                break;
            case HitMargin.VeryEarly:
            case HitMargin.VeryLate:
                if (!Main.Settings.AllowOrangeCombo) goto default;
                overlay.UpdateCombo(++GameLifecycleHelper.ComboCount, true);
                break;
            case HitMargin.Auto when !Main.Settings.EnableAutoCombo:
                break;
            default:
                overlay.UpdateCombo(GameLifecycleHelper.ComboCount = 0, false);
                break;
        }
        if (hit is not HitMargin.Perfect and not HitMargin.Auto)
            overlay.Jongyeol.OnNonPerfectHit();
    }
}
