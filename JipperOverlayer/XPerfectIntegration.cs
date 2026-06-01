using System;
using System.Reflection;
using UnityModManagerNet;

namespace JipperOverlayer
{
    public static class XPerfectIntegration
    {
        private static Func<int> _getXPerfect, _getPlusPerfect, _getMinusPerfect;
        public static bool IsAvailable { get; private set; }

        public static int PlusPerfect => _getPlusPerfect?.Invoke() ?? 0;
        public static int XPerfect => _getXPerfect?.Invoke() ?? 0;
        public static int MinusPerfect => _getMinusPerfect?.Invoke() ?? 0;

        private static bool _subscribedToToggle;

        /// <summary>懒加载：在需要时调用（如 UpdateJudgement 或 OnUpdate）</summary>
        public static void EnsureInitialized()
        {
            if (IsAvailable) return;            // 已就绪，无需操作
            TryCache();                         // 尝试缓存委托
            SubscribeToToggle();                // 尝试订阅事件
        }

        private static void TryCache()
        {
            if (IsAvailable) return;
            var mod = UnityModManager.FindMod("XPerfect");
            if (mod == null || !mod.Enabled || !mod.Active || mod.Assembly == null) return;
            CacheDelegates(mod.Assembly);
            Overlayer.Overlay.Instance?.UpdateJudgement();
        }

        private static void CacheDelegates(Assembly asm)
        {
            try
            {
                var type = asm.GetType("XPerfect.AccuracyState");
                if (type == null) return;

                var xProp = type.GetProperty("XPerfectCount", BindingFlags.Public | BindingFlags.Static);
                var plusProp = type.GetProperty("PlusPerfectCount", BindingFlags.Public | BindingFlags.Static);
                var minusProp = type.GetProperty("MinusPerfectCount", BindingFlags.Public | BindingFlags.Static);
                if (xProp == null || plusProp == null || minusProp == null) return;

                _getXPerfect = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), xProp.GetGetMethod());
                _getPlusPerfect = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), plusProp.GetGetMethod());
                _getMinusPerfect = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), minusProp.GetGetMethod());

                IsAvailable = true;
                Main.Mod.Logger.Log("[XPerfectIntegration] Integration ready.");
            }
            catch (Exception ex)
            {
                Main.Mod.Logger.Log($"[XPerfectIntegration] Failed: {ex}");
            }
        }

        private static void SubscribeToToggle()
        {
            if (_subscribedToToggle) return;
            var mod = UnityModManager.FindMod("XPerfect");
            if (mod == null) return;
            mod.OnToggle += OnXPerfectToggle;
            _subscribedToToggle = true;
        }

        private static bool OnXPerfectToggle(UnityModManager.ModEntry modEntry, bool enabled)
        {
            if (enabled)
            {
                TryCache();                     // 重新尝试缓存
                if (!IsAvailable)               // 缓存失败 → 重置订阅标记，允许下次重试
                    _subscribedToToggle = false;
            }
            else
            {
                if (IsAvailable)
                {
                    IsAvailable = false;
                    _getXPerfect = _getPlusPerfect = _getMinusPerfect = null;
                    Main.Mod.Logger.Log("[XPerfectIntegration] XPerfect disabled.");
                    Overlayer.Overlay.Instance?.UpdateJudgement();
                }
                _subscribedToToggle = false;    // 重置订阅，下次可以重新绑定
            }
            return true;
        }
    }
}