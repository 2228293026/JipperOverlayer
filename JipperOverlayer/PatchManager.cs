using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JipperOverlayer;

internal static class PatchManager
{
    private static Harmony _harmony;
    private static readonly Dictionary<string, object> _delegateCache = new();
    private static readonly Dictionary<string, MethodInfo> _methodCache = new();
    private static readonly Dictionary<string, FieldInfo> _fieldCache = new();
    private static readonly Dictionary<Type, PatchRegistration> _registeredPatches = new();
    private static readonly HashSet<Type> _appliedPatches = new();
    private static readonly List<MethodInfo> _appliedManualPatches = new();
    private static readonly object _lock = new();

    public static void Initialize(Harmony harmony)
    {
        lock (_lock)
        {
            _harmony = harmony;
            _registeredPatches.Clear();
            _appliedPatches.Clear();
            _delegateCache.Clear();
            _methodCache.Clear();
            _fieldCache.Clear();
        }
    }

    public static void RegisterPatch(Type patchType, Func<bool> toggle)
    {
        lock (_lock)
        {
            _registeredPatches[patchType] = new PatchRegistration(patchType, toggle);
        }
    }

    public static void RegisterPatches(Func<bool> toggle, params Type[] patchTypes)
    {
        foreach (var patchType in patchTypes)
            RegisterPatch(patchType, toggle);
    }

    /// <summary>Register a manual Harmony patch for a dynamically-resolved target method.</summary>
    public static void RegisterManualPatch(MethodInfo targetMethod, MethodInfo postfixMethod, Func<bool> toggle, string id)
    {
        if (targetMethod == null || postfixMethod == null) return;
        lock (_lock)
        {
            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
            _appliedManualPatches.Add(targetMethod);
        }
        Main.Mod.Logger.Log($"Applied manual patch: {targetMethod.DeclaringType.Name}.{targetMethod.Name} -> {postfixMethod.Name}");
    }

    public static void ApplyAll()
    {
        lock (_lock)
        {
            if (_harmony == null) return;
            foreach (var registration in _registeredPatches.Values)
            {
                if (_appliedPatches.Contains(registration.PatchType)) continue;
                if (!registration.IsEnabled()) continue;
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
        lock (_lock)
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
                    catch (Exception e)
                    {
                        Main.Mod.Logger.Warning($"Failed to apply patch {registration.PatchType.Name}: {e.Message}");
                    }
                }
                else if (!shouldBeEnabled && isApplied)
                {
                    try
                    {
                        _harmony.CreateClassProcessor(registration.PatchType).Unpatch();
                        _appliedPatches.Remove(registration.PatchType);
                    }
                    catch (Exception e)
                    {
                        Main.Mod.Logger.Warning($"Failed to unpatch {registration.PatchType.Name}: {e.Message}");
                    }
                }
            }
        }
    }

    public static void UnpatchAll()
    {
        lock (_lock)
        {
            _harmony?.UnpatchAll(Main.Mod.Info.Id);
            _appliedPatches.Clear();
            _appliedManualPatches.Clear();
        }
    }

    #region 反射缓存

    /// <summary>获取并缓存 MethodInfo（实例或静态）。</summary>
    public static MethodInfo GetMethodInfo(Type declaringType, string methodName, Type[] parameters = null, Type[] generics = null)
    {
        if (declaringType == null) throw new ArgumentNullException(nameof(declaringType));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("方法名不能为空", nameof(methodName));

        string key = $"{declaringType.FullName}.{methodName}";
        if (parameters != null)
            key += "_" + string.Join(",", parameters.Select(t => t.FullName));
        if (generics != null)
            key += "_generic_" + string.Join(",", generics.Select(t => t.FullName));

        lock (_lock)
        {
            if (_methodCache.TryGetValue(key, out var cached))
                return cached;

            MethodInfo method;
            if (parameters != null)
                method = AccessTools.Method(declaringType, methodName, parameters, generics);
            else
                method = AccessTools.Method(declaringType, methodName, generics);

            if (method == null)
                throw new MissingMethodException($"在 {declaringType} 中找不到方法 {methodName}");

            _methodCache[key] = method;
            return method;
        }
    }

    /// <summary>获取并缓存 FieldInfo。</summary>
    public static FieldInfo GetFieldInfo(Type declaringType, string fieldName)
    {
        if (declaringType == null) throw new ArgumentNullException(nameof(declaringType));
        if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("字段名不能为空", nameof(fieldName));

        string key = $"{declaringType.FullName}.{fieldName}";

        lock (_lock)
        {
            if (_fieldCache.TryGetValue(key, out var cached))
                return cached;

            var field = AccessTools.Field(declaringType, fieldName);
            if (field == null)
                throw new MissingFieldException($"在 {declaringType} 中找不到字段 {fieldName}");

            _fieldCache[key] = field;
            return field;
        }
    }

    /// <summary>创建并缓存实例字段访问委托。</summary>
    public static AccessTools.FieldRef<T, F> CreateFieldRef<T, F>(string fieldName) where T : class
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("字段名不能为空", nameof(fieldName));

        var key = $"Field:{typeof(T).FullName}.{fieldName}";
        lock (_lock)
        {
            if (_delegateCache.TryGetValue(key, out var cached))
                return (AccessTools.FieldRef<T, F>)cached;

            var fieldRef = AccessTools.FieldRefAccess<T, F>(fieldName);
            _delegateCache[key] = fieldRef;
            return fieldRef;
        }
    }

    /// <summary>创建并缓存实例属性 Getter 委托。</summary>
    public static Func<T, F> CreatePropertyGetter<T, F>(string propertyName) where T : class
    {
        var key = $"PropGet:{typeof(T).FullName}.{propertyName}";
        lock (_lock)
        {
            if (_delegateCache.TryGetValue(key, out var cached))
                return (Func<T, F>)cached;

            var prop = AccessTools.Property(typeof(T), propertyName);
            if (prop == null) throw new MissingMemberException($"Property '{propertyName}' not found on {typeof(T)}");
            var getMethod = prop.GetGetMethod(true);
            if (getMethod == null) throw new InvalidOperationException($"Property '{propertyName}' has no getter");

            var del = (Func<T, F>)Delegate.CreateDelegate(typeof(Func<T, F>), getMethod);
            _delegateCache[key] = del;
            return del;
        }
    }

    /// <summary>创建并缓存实例属性 Setter 委托。</summary>
    public static Action<T, F> CreatePropertySetter<T, F>(string propertyName) where T : class
    {
        var key = $"PropSet:{typeof(T).FullName}.{propertyName}";
        lock (_lock)
        {
            if (_delegateCache.TryGetValue(key, out var cached))
                return (Action<T, F>)cached;

            var prop = AccessTools.Property(typeof(T), propertyName);
            if (prop == null) throw new MissingMemberException($"Property '{propertyName}' not found on {typeof(T)}");
            var setMethod = prop.GetSetMethod(true);
            if (setMethod == null) throw new InvalidOperationException($"Property '{propertyName}' has no setter");

            var del = (Action<T, F>)Delegate.CreateDelegate(typeof(Action<T, F>), setMethod);
            _delegateCache[key] = del;
            return del;
        }
    }

    /// <summary>创建并缓存静态字段 Getter 委托。</summary>
    public static Func<TField> CreateStaticFieldGetter<TField>(Type declaringType, string fieldName)
    {
        var key = $"StaticFieldGet:{declaringType.FullName}.{fieldName}";
        lock (_lock)
        {
            if (_delegateCache.TryGetValue(key, out var cached))
                return (Func<TField>)cached;

            var fi = AccessTools.Field(declaringType, fieldName);
            if (fi == null) throw new MissingMemberException($"{declaringType}.{fieldName}");
            if (!fi.IsStatic) throw new ArgumentException("Field is not static");

            var method = new DynamicMethod($"get_{fieldName}", typeof(TField), Type.EmptyTypes, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, fi);
            il.Emit(OpCodes.Ret);
            var del = (Func<TField>)method.CreateDelegate(typeof(Func<TField>));
            _delegateCache[key] = del;
            return del;
        }
    }

    /// <summary>创建并缓存静态字段 Setter 委托。</summary>
    public static Action<TField> CreateStaticFieldSetter<TField>(Type declaringType, string fieldName)
    {
        var key = $"StaticFieldSet:{declaringType.FullName}.{fieldName}";
        lock (_lock)
        {
            if (_delegateCache.TryGetValue(key, out var cached))
                return (Action<TField>)cached;

            var fi = AccessTools.Field(declaringType, fieldName);
            if (fi == null) throw new MissingMemberException($"{declaringType}.{fieldName}");
            if (!fi.IsStatic) throw new ArgumentException("Field is not static");

            var method = new DynamicMethod($"set_{fieldName}", typeof(void), new[] { typeof(TField) }, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, fi);
            il.Emit(OpCodes.Ret);
            var del = (Action<TField>)method.CreateDelegate(typeof(Action<TField>));
            _delegateCache[key] = del;
            return del;
        }
    }

    /// <summary>创建并缓存静态属性 Getter 委托。</summary>
    public static Func<TField> CreateStaticPropertyGetter<TField>(Type declaringType, string propertyName)
    {
        var key = $"StaticPropGet:{declaringType.FullName}.{propertyName}";
        lock (_lock)
        {
            if (_delegateCache.TryGetValue(key, out var cached))
                return (Func<TField>)cached;

            var prop = AccessTools.Property(declaringType, propertyName);
            if (prop == null) throw new MissingMemberException($"{declaringType}.{propertyName}");
            var getMethod = prop.GetGetMethod(true);
            if (getMethod == null) throw new InvalidOperationException("Property has no getter");
            var del = (Func<TField>)Delegate.CreateDelegate(typeof(Func<TField>), getMethod);
            _delegateCache[key] = del;
            return del;
        }
    }

    /// <summary>创建并缓存静态属性 Setter 委托。</summary>
    public static Action<TField> CreateStaticPropertySetter<TField>(Type declaringType, string propertyName)
    {
        var key = $"StaticPropSet:{declaringType.FullName}.{propertyName}";
        lock (_lock)
        {
            if (_delegateCache.TryGetValue(key, out var cached))
                return (Action<TField>)cached;

            var prop = AccessTools.Property(declaringType, propertyName);
            if (prop == null) throw new MissingMemberException($"{declaringType}.{propertyName}");
            var setMethod = prop.GetSetMethod(true);
            if (setMethod == null) throw new InvalidOperationException("Property has no setter");
            var del = (Action<TField>)Delegate.CreateDelegate(typeof(Action<TField>), setMethod);
            _delegateCache[key] = del;
            return del;
        }
    }

    #endregion

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
