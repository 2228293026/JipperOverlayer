using System;

namespace JipperOverlayer;

public interface IModLoader
{
    string ModPath { get; }
    void Log(string msg);
    void Warning(string msg);
    void Error(string msg);

    event Action<float> OnUpdate;
    event Action<bool> OnToggle;
    event Action OnGUI;
    event Action OnSaveGUI;
}
