namespace JipperOverlayer.Overlayer.Jongyeol;

public static class Jbpm
{
    public static bool CheckPseudo
    {
        get => Main.Settings.CheckPseudo;
        set => Main.Settings.CheckPseudo = value;
    }

    public static float BpmColorMax
    {
        get => Main.Settings.BpmColorMax;
        set => Main.Settings.BpmColorMax = value;
    }
}
