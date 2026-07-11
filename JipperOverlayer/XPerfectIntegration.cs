using System;
using System.Reflection;
using UnityModManagerNet;

namespace JipperOverlayer
{
    public static class XPerfectIntegration
    {
        private static Func<int> _getXPerfect, _getPlusPerfect, _getMinusPerfect;
        private static Func<int, int> _getPlayerXPerfect, _getPlayerPlusPerfect, _getPlayerMinusPerfect;
        public static bool IsAvailable { get; private set; }

        public static int PlusPerfect => _getPlusPerfect?.Invoke() ?? 0;
        public static int XPerfect => _getXPerfect?.Invoke() ?? 0;
        public static int MinusPerfect => _getMinusPerfect?.Invoke() ?? 0;

        public static int GetPlayerXPerfect(int player) => _getPlayerXPerfect?.Invoke(player) ?? 0;
        public static int GetPlayerPlusPerfect(int player) => _getPlayerPlusPerfect?.Invoke(player) ?? 0;
        public static int GetPlayerMinusPerfect(int player) => _getPlayerMinusPerfect?.Invoke(player) ?? 0;

        public static (int plus, int x, int minus) GetPlayer(int player) =>
            (GetPlayerPlusPerfect(player), GetPlayerXPerfect(player), GetPlayerMinusPerfect(player));

        private static bool _subscribedToToggle;

        /// <summary>懒加载：在需要时调用（如 UpdateJudgement 或 OnUpdate）</summary>
        public static void EnsureInitialized()
        {
            if (IsAvailable) return;
            TryCache();
            SubscribeToToggle();
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

                // Per-player methods (nullable — optional for single-player only overlays)
                var mPlayerX = type.GetMethod("GetPlayerXPerfectCount", BindingFlags.Public | BindingFlags.Static);
                var mPlayerPlus = type.GetMethod("GetPlayerPlusPerfectCount", BindingFlags.Public | BindingFlags.Static);
                var mPlayerMinus = type.GetMethod("GetPlayerMinusPerfectCount", BindingFlags.Public | BindingFlags.Static);
                if (mPlayerX != null && mPlayerPlus != null && mPlayerMinus != null)
                {
                    _getPlayerXPerfect = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), mPlayerX);
                    _getPlayerPlusPerfect = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), mPlayerPlus);
                    _getPlayerMinusPerfect = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), mPlayerMinus);
                }
                else
                {
                    // v136 XPerfect 没有 per-player 方法，回退到主值
                    _getPlayerXPerfect = _ => XPerfect;
                    _getPlayerPlusPerfect = _ => PlusPerfect;
                    _getPlayerMinusPerfect = _ => MinusPerfect;
                }

                IsAvailable = true;
                Loader.Log("[XPerfectIntegration] Integration ready.");
            }
            catch (Exception ex)
            {
                Loader.Log($"[XPerfectIntegration] Failed: {ex}");
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
                TryCache();
                if (!IsAvailable)
                    _subscribedToToggle = false;
            }
            else
            {
                if (IsAvailable)
                {
                    IsAvailable = false;
                    _getXPerfect = _getPlusPerfect = _getMinusPerfect = null;
                    _getPlayerXPerfect = _getPlayerPlusPerfect = _getPlayerMinusPerfect = null;
                    Loader.Log("[XPerfectIntegration] XPerfect disabled.");
                    Overlayer.Overlay.Instance?.UpdateJudgement();
                }
                _subscribedToToggle = false;
            }
            return true;
        }
    }
}
