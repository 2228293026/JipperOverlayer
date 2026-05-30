using System.Collections;
using UnityEngine;

namespace JipperOverlayer.Overlayer;

public class OverlayMono : MonoBehaviour
{
    public Overlay Overlay;
    private Coroutine _comboAnim;
    private bool _lastPaused;

    private void Update()
    {
        if (Overlay == null) return;
        Overlay.UpdateTime();
        bool paused = ADOBase.controller?.paused ?? _lastPaused;
        if (paused != _lastPaused)
        {
            _lastPaused = paused;
            if (Overlay.Canvas)
                Overlay.Canvas.enabled = !paused;
        }
    }

    public void StartComboBump()
    {
        if (_comboAnim != null) StopCoroutine(_comboAnim);
        _comboAnim = StartCoroutine(ComboAnim());
    }

    public void StopComboBump()
    {
        if (_comboAnim != null) StopCoroutine(_comboAnim);
        _comboAnim = null;
    }

    private IEnumerator ComboAnim()
    {
        double elapsed = 0;
        while (elapsed < 500)
        {
            float t = (float)(elapsed / 500);
            if (t > 1) t = 1;
            Overlay.ComboText.fontSize = (int)(30 * OutExpoChange(t) + 78);
            if (Overlay._comboTitleTransform)
            {
                try { Overlay._comboTitleTransform.anchoredPosition = new Vector2(0, Overlay.ComboTextTransform.sizeDelta.y / 2); }
                catch { }
            }
            yield return null;
            elapsed += Time.deltaTime * 1000;
        }
        Overlay.ComboText.fontSize = 78;
        _comboAnim = null;
    }

    private static float OutExpoChange(double t) => (float)(t == 1 ? 0 : System.Math.Pow(2, -10 * t));
}
