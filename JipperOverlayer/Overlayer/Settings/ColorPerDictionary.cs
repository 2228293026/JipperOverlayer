using System;
using System.Collections.Generic;
using JipperOverlayer.Overlayer.Localization;
using Newtonsoft.Json;
using UnityEngine;

namespace JipperOverlayer.Overlayer.Settings;

public class ColorPerDictionary {
    public ColorCache PerfectColor;
    public List<ProgressColorCache> List = [];
    [JsonIgnore] public bool Expanded;
    [JsonIgnore] public ProgressColorCache ExpandedCache;

    public ColorPerDictionary() { }

    public ColorPerDictionary(IEnumerable<(float, Color)> collection) {
        foreach (var item in collection) Add(item);
    }

    public ColorPerDictionary(ColorCache perfectColor) : this() { PerfectColor = perfectColor; }

    public ColorPerDictionary(Color color) : this(new ColorCache(color)) { }

    public ColorPerDictionary(IEnumerable<(float, Color)> collection, Color color) : this(collection) {
        PerfectColor = new ColorCache(color);
    }

    // Call after deserialization to ensure list is sorted
    public void EnsureSorted() {
        List.Sort((a, b) => a.Progress.CompareTo(b.Progress));
    }

    public Color GetColor(float key) {
        if (key < 0) key = 0;
        if (key > 1) key = 1;
        if (PerfectColor != null && key == 1) return PerfectColor;
        if (List.Count == 0) return PerfectColor ?? Color.white;
        int index = BinarySearch(key);
        if (index == 0) return List[0];
        if (index == List.Count) return List[List.Count - 1];
        if (List[index].Progress == key) return List[index];
        float s = List[index - 1].Progress;
        float e = List[index].Progress;
        return Color.Lerp(List[index - 1], List[index], (key - s) / (e - s));
    }

    int BinarySearch(float value) {
        if (List.Count == 0) return 0;
        int lo = 0, hi = List.Count - 1;
        while (lo <= hi) {
            int m = (lo + hi) / 2;
            if (List[m].Progress == value) return m;
            if (List[m].Progress < value) lo = m + 1;
            else hi = m - 1;
        }
        return lo;
    }

    public bool SettingGUI(Action onChanged, string text, Action onReset = null) {
        GUILayout.BeginHorizontal();
        Expanded = GUILayout.Toggle(Expanded, Expanded ? "◢" : "▶", new GUIStyle {
            fixedWidth = 12f, normal = new GUIStyleState { textColor = Color.white }, fontSize = 14, margin = new RectOffset(4, 2, 4, 4)
        });
        if (GUILayout.Button(text, GUI.skin.label)) Expanded = !Expanded;
        GUILayout.FlexibleSpace();
        if (onReset != null && GUILayout.Button("R", GUILayout.Width(20))) { onReset(); onChanged?.Invoke(); }
        GUILayout.EndHorizontal();
        if (!Expanded) return false;

        bool changed = false;
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        GUILayout.BeginVertical();

        if (GUILayout.Button(Tr.Get("add_color_stop"))) {
            List.Add(new ProgressColorCache(UnityEngine.Random.value, new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value)));
            SortList();
            onChanged?.Invoke(); changed = true;
        }

        for (int i = 0; i < List.Count; i++) {
            var cache = List[i];

            GUILayout.BeginHorizontal();
            bool exp = ExpandedCache == cache;
            exp = GUILayout.Toggle(exp, exp ? "◢" : "▶", new GUIStyle {
                fixedWidth = 12f, normal = new GUIStyleState { textColor = Color.white }, fontSize = 14, margin = new RectOffset(4, 2, 4, 4)
            });
            // Color swatch (solid color)
            var swatchRect = GUILayoutUtility.GetRect(16, 14);
            var prevSwatchColor = GUI.color;
            GUI.color = cache;
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
            GUI.color = prevSwatchColor;
            if (GUILayout.Button(cache.Progress * 100 + "%", GUI.skin.label)) exp = !exp;
            if (ExpandedCache == cache != exp) ExpandedCache = exp ? cache : null;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (!exp) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            GUILayout.BeginVertical();

            // Percent slider
            GUILayout.BeginHorizontal();
            GUILayout.Label(Tr.Get("percent"), GUILayout.Width(50));
            float oldP = cache.Progress;
            float p = GUILayout.HorizontalSlider(cache.Progress, 0, 1);
            if (Math.Abs(p - oldP) > 0.001f) {
                cache.Progress = p;
                SortList();
                onChanged?.Invoke(); changed = true;
            }
            GUILayout.Label(cache.Progress.ToString("F3"), GUILayout.Width(40));
            GUILayout.EndHorizontal();

            // Color editor
            if (cache.SettingGUI(cache.Progress.ToString(), cache)) {
                onChanged?.Invoke(); changed = true;
            }

            bool deleted = false;
            if (GUILayout.Button(Tr.Get("delete"))) {
                List.RemoveAt(i);
                i--;
                onChanged?.Invoke(); changed = true; deleted = true;
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (deleted) continue;
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        return changed;
    }

    void SortList() {
        List.Sort((a, b) => a.Progress.CompareTo(b.Progress));
    }

    public void Add((float, Color) item) {
        List.Add(new ProgressColorCache(item.Item1, item.Item2));
        SortList();
    }
}
