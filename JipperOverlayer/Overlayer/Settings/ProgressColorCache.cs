using Newtonsoft.Json;
using UnityEngine;

namespace JipperOverlayer.Overlayer.Settings;

public class ProgressColorCache : ColorCache {
    public float Progress;
    [JsonIgnore] public string ProgressString;

    public ProgressColorCache() { }

    public ProgressColorCache(float progress, Color color) : base(color) {
        Progress = progress;
    }
}
