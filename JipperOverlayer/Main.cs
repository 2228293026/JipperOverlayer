using HarmonyLib;
using JipperOverlayer.Overlayer;
using JipperOverlayer.Overlayer.Features;
using JipperOverlayer.Overlayer.Jongyeol;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JipperOverlayer;

public static class Main
{
    public static UnityModManager.ModEntry Mod { get; private set; }
    public static Harmony Harmony { get; private set; }
    public static Settings Settings { get; private set; }

    private static Overlay _overlay;
    private static GameObject _overlayGo;

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        Mod = modEntry;
        Settings = Settings.Load(modEntry);

        modEntry.OnToggle = OnToggle;
        modEntry.OnGUI = Settings.OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;
        modEntry.OnHideGUI = OnSaveGUI;
        modEntry.OnUpdate = OnUpdate;

        Harmony = new Harmony(modEntry.Info.Id);
        Mod.Logger.Log("JipperOverlayer loaded.");

        return true;
    }

    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        if (value)
        {
            Mod.Logger.Log("JipperOverlayer enabled.");

            PatchManager.Initialize(Harmony);
            VersionSafe.Setup(Harmony);         // must be before RegisterFeatures
            RegisterFeatures();

            BundleLoader.LoadBundle();
            FontManager.ScanFonts();
            PlayCount.Load();

            // Create persistent GameObject for scene tracking
            if (_overlayGo == null)
            {
                _overlayGo = new GameObject("JipperOverlayer");
                Object.DontDestroyOnLoad(_overlayGo);
            }

            // Create overlay
            CreateOverlay();

            // Apply all registered patches
            PatchManager.ApplyAll();

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        else
        {
            Mod.Logger.Log("JipperOverlayer disabled.");
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            _overlay?.Destroy();
            _overlay = null;
            Overlay.Instance = null;
            JOverlay.Instance = null;

            if (_overlayGo != null)
            {
                Object.Destroy(_overlayGo);
                _overlayGo = null;
            }

            PlayCount.Dispose();
            BundleLoader.UnloadBundle();
            PatchManager.UnpatchAll();
        }
        return true;
    }

    private static void RegisterFeatures()
    {
        // Version-agnostic patches (always applied)
        GameLifecyclePatches.Register();

        // Version-specific: v141+ and v136 are MUTUALLY EXCLUSIVE
        if (VersionSafe.IsV141OrLater)
        {
            Mod.Logger.Log("API: v141+ — registering v141 patches");
            V141Patches.RegisterAll();
        }
        else
        {
            Mod.Logger.Log("API: v136  — registering v136 patches");
            V136Patches.RegisterAll();
        }
    }

    private static void CreateOverlay()
    {
        _overlay = Settings.JongyeolMode ? new JOverlay() : new Overlay();
    }

    public static void RecreateOverlay()
    {
        _overlay?.Destroy();
        _overlay = null;
        Overlay.Instance = null;
        JOverlay.Instance = null;
        CreateOverlay();
        // If game is active, show overlay (constructor's Show(0) may not match current floor)
        try
        {
            if (ADOBase.controller is { paused: false } && ADOBase.conductor is { isGameWorld: true })
            {
                int floor = scrController.instance.currentSeqID;
                // Prevent double Show by checking if the overlay is already active
                if (_overlay != null && !_overlay.GameObject.activeSelf)
                    _overlay.Show(floor);
            }
        }
        catch { }
    }

    private static void OnSceneUnloaded(Scene _)
    {
        try { _overlay?.Hide(); } catch { }
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        Settings.OnSaveGUI(modEntry);
    }

    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
    {
        if (Settings.JongyeolMode)
            try { JOverlay.Instance?.UpdateFPS(deltaTime); }
            catch { }
    }
}
