using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace JipperOverlayer;

internal static class PatchManager
{
    private static Harmony _harmony;
    private static readonly Dictionary<Type, PatchRegistration> _registeredPatches = new();
    private static readonly List<Type> _appliedPatches = new();
    private static readonly List<MethodInfo> _appliedManualPatches = new();

    public static void Initialize(Harmony harmony)
    {
        _harmony = harmony;
        _registeredPatches.Clear();
        _appliedPatches.Clear();
    }

    public static void RegisterPatch(Type patchType, Func<bool> toggle)
    {
        _registeredPatches[patchType] = new PatchRegistration(patchType, toggle);
    }

    public static void RegisterPatches(Func<bool> toggle, params Type[] patchTypes)
    {
        foreach (var patchType in patchTypes)
            RegisterPatch(patchType, toggle);
    }

    public static void RegisterPatchesSafe(Func<bool> toggle, params Type[] patchTypes)
    {
        foreach (var patchType in patchTypes)
            RegisterPatch(patchType, toggle);
    }

    /// <summary>Register a manual Harmony patch for a dynamically-resolved target method.</summary>
    public static void RegisterManualPatch(MethodInfo targetMethod, MethodInfo postfixMethod, Func<bool> toggle, string id)
    {
        if (targetMethod == null || postfixMethod == null) return;
        _harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
        _appliedManualPatches.Add(targetMethod);
        Main.Mod.Logger.Log($"Applied manual patch: {targetMethod.DeclaringType.Name}.{targetMethod.Name} -> {postfixMethod.Name}");
    }

    public static void ApplyAll()
    {
        if (_harmony == null) return;
        foreach (var registration in _registeredPatches.Values)
        {
            if (registration.IsEnabled())
            {
                try
                {
                    _harmony.CreateClassProcessor(registration.PatchType).Patch();
                    _appliedPatches.Add(registration.PatchType);
                }
                catch (Exception e)
                {
                    Main.Mod.Logger.Warning($"Failed to apply patch {registration.PatchType.Name}: {e.Message}");
                }
            }
        }
    }

    public static void RefreshPatches()
    {
        if (_harmony == null) return;
        foreach (var registration in _registeredPatches.Values)
        {
            bool isApplied = _appliedPatches.Contains(registration.PatchType);
            bool shouldBeEnabled = registration.IsEnabled();
            if (shouldBeEnabled && !isApplied)
            {
                try
                {
                    _harmony.CreateClassProcessor(registration.PatchType).Patch();
                    _appliedPatches.Add(registration.PatchType);
                }
                catch { }
            }
            else if (!shouldBeEnabled && isApplied)
            {
                try { _harmony.CreateClassProcessor(registration.PatchType).Unpatch(); }
                catch { }
                _appliedPatches.Remove(registration.PatchType);
            }
        }
    }

    public static void UnpatchAll()
    {
        _harmony?.UnpatchAll(Main.Mod.Info.Id);
        _appliedPatches.Clear();
        _appliedManualPatches.Clear();
    }

    private class PatchRegistration
    {
        public Type PatchType { get; }
        public Func<bool> IsEnabled { get; }
        public PatchRegistration(Type patchType, Func<bool> isEnabled)
        {
            PatchType = patchType;
            IsEnabled = isEnabled;
        }
    }
}
