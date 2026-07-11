using System;

namespace JipperOverlayer;

public static class Loader
{
    public static IModLoader Instance { get; internal set; }

    public static string ModPath => Instance?.ModPath ?? ".";
    public static void Log(string m) => Instance?.Log(m);
    public static void Warning(string m) => Instance?.Warning(m);
    public static void Error(string m) => Instance?.Error(m);

    public static event Action<float> OnUpdate
    {
        add => Instance.OnUpdate += value;
        remove => Instance.OnUpdate -= value;
    }
    public static event Action<bool> OnToggle
    {
        add => Instance.OnToggle += value;
        remove => Instance.OnToggle -= value;
    }
    public static event Action OnGUI
    {
        add => Instance.OnGUI += value;
        remove => Instance.OnGUI -= value;
    }
    public static event Action OnSaveGUI
    {
        add => Instance.OnSaveGUI += value;
        remove => Instance.OnSaveGUI -= value;
    }
}
