using HarmonyLib;
using JipperOverlayer.Overlayer;
using JipperOverlayer.Overlayer.Features;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace JipperOverlayer;

public static class Main
{
    public static Harmony Harmony { get; private set; }
    public static Settings Settings { get; private set; }

    private static Overlay _overlay;
    private static GameObject _overlayGo;
    private static bool _enabled;

    public static void Init(IModLoader loader)
    {
        Loader.Instance = loader;
        Settings = Settings.Load();

        loader.OnToggle += OnToggle;
        loader.OnGUI += () =>
        {
            if (Settings != null) Settings.OnGUI();
        };
        loader.OnSaveGUI += OnSaveGUI;
        loader.OnUpdate += OnUpdate;

        Harmony = new Harmony("JipperOverlayer");

        Log("JipperOverlayer initialized.");
    }

    private static void OnToggle(bool value)
    {
        if (value)
        {
            Enable();
        }
        else
        {
            Disable();
        }
    }

    public static void Enable()
    {
        if (_enabled) return;
        _enabled = true;

        Log("JipperOverlayer enabled.");

        PatchManager.Initialize(Harmony);
        VersionSafe.Setup();
        RegisterFeatures();

        BundleLoader.LoadBundle();
        FontManager.ScanFonts();
        PlayCount.Load();

        if (_overlayGo == null)
        {
            _overlayGo = new GameObject("JipperOverlayer");
            UnityEngine.Object.DontDestroyOnLoad(_overlayGo);
        }

        CreateOverlay();
        PatchManager.ApplyAll();

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public static void Disable()
    {
        if (!_enabled) return;
        _enabled = false;

        Log("JipperOverlayer disabled.");
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        _overlay?.Destroy();
        _overlay = null;
        Overlay.Instance = null;

        if (_overlayGo != null)
        {
            UnityEngine.Object.Destroy(_overlayGo);
            _overlayGo = null;
        }

        PlayCount.Dispose();
        BundleLoader.UnloadBundle();
        PatchManager.UnpatchAll();
    }

    private static void RegisterFeatures()
    {
        GameLifecyclePatches.Register();

        if (VersionSafe.IsV141OrLater)
        {
            Log("API: v141+ — registering v141 patches");
            V141Patches.RegisterAll();
        }
        else
        {
            Log("API: v136  — registering v136 patches");
            V136Patches.RegisterAll();
        }
    }

    private static void CreateOverlay()
    {
        _overlay = new Overlay(Settings.JongyeolMode);
    }

    public static void RecreateOverlay()
    {
        _overlay?.Destroy();
        _overlay = null;
        Overlay.Instance = null;
        CreateOverlay();
        if (ADOBase.controller == null || ADOBase.conductor is not { isGameWorld: true })
            return;
        if (_overlay == null || _overlay.GameObject.activeSelf) return;

        if (ADOBase.controller.paused)
        {
            _overlay.Show(scrController.instance.currentSeqID, suppressNativeUI: true);
            if (_overlay.Canvas)
                _overlay.Canvas.enabled = false;
        }
        else
        {
            _overlay.Show(scrController.instance.currentSeqID);
        }
    }

    private static void OnSceneUnloaded(Scene _)
    {
        try { _overlay?.Hide(); } catch { }
    }

    private static void OnSaveGUI()
    {
        Settings.OnSaveGUI();
    }

    private static void OnUpdate(float deltaTime)
    {
        XPerfectIntegration.EnsureInitialized();
        if (Settings.JongyeolMode)
            try { _overlay?.Jongyeol?.UpdateFPS(deltaTime); }
            catch { }
    }

    // Convenience wrappers
    public static void Log(string msg) => Loader.Log(msg);
    public static void Warning(string msg) => Loader.Warning(msg);
    public static void Error(string msg) => Loader.Error(msg);
}
