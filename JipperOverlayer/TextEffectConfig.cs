using JipperOverlayer.Overlayer;
using UnityEngine;

namespace JipperOverlayer;

[System.Serializable]
public class TextEffectConfig
{
    // Underlay-based shadow.
    public bool ShadowEnabled = true;
    public float ShadowOffsetX = 1f;
    public float ShadowOffsetY = -1f;
    public ColorCache ShadowColor = new(new Color(0, 0, 0, 0.5f));
    public float ShadowSoftness = 0f;

    // TMP outline (Outline keyword).
    public bool OutlineEnabled = false;
    public float OutlineWidth = 0.01f;
    public float OutlineSoftness = 0f;
    public ColorCache OutlineColor = new(Color.black);
}
