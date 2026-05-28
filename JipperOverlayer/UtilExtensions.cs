using UnityEngine;

namespace JipperOverlayer;

internal static class UtilExtensions
{
    public static void SizeDeltaX(this RectTransform rt, float x)
    {
        Vector2 sd = rt.sizeDelta;
        sd.x = x;
        rt.sizeDelta = sd;
    }
}
