using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Auto-injects animated background into the Canvas on scene load — no scene setup needed.
public class UIBackgroundAnimator : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindObjectOfType<UIBackgroundAnimator>() != null) return;
        new GameObject("[UIBackground]").AddComponent<UIBackgroundAnimator>();
    }

    // ── Star data ─────────────────────────────────────────────────────────────
    private RectTransform[] _starRects;
    private Image[]         _starImages;
    private float[]         _starSpeeds;
    private float[]         _twinkleSpeeds;
    private float[]         _twinklePhases;

    // ── Orb data ──────────────────────────────────────────────────────────────
    private RectTransform[] _orbRects;
    private Vector2[]       _orbOrigins;
    private float[]         _orbSpeeds;
    private float[]         _orbPhases;
    private float[]         _orbRadii;

    private float _w, _h;
    private Canvas _canvas;

    private const int StarCount = 65;

    private void Awake()
    {
        _canvas = FindObjectOfType<Canvas>();
    }

    private IEnumerator Start()
    {
        yield return null; // let Canvas layout first
        Canvas.ForceUpdateCanvases();

        if (_canvas == null) yield break;

        var cr = _canvas.GetComponent<RectTransform>();
        _w = cr.rect.width;
        _h = cr.rect.height;

        if (_w == 0 || _h == 0) yield break;

        BuildGradient();
        BuildOrbs();
        BuildStars();
    }

    // ── Gradient background ───────────────────────────────────────────────────

    private void BuildGradient()
    {
        // 1×2 texture stretched to fill → cheap gradient
        var tex = new Texture2D(1, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };
        tex.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.18f)); // bottom: deep navy
        tex.SetPixel(0, 1, new Color(0.10f, 0.04f, 0.26f)); // top: dark indigo
        tex.Apply();

        var go  = MakeFullscreen("BG_Gradient", 0);
        var raw = go.AddComponent<RawImage>();
        raw.texture      = tex;
        raw.raycastTarget = false;
    }

    // ── Glow orbs ─────────────────────────────────────────────────────────────

    private static readonly Color[] OrbColors =
    {
        new Color(0.30f, 0.10f, 0.90f, 0.10f), // violet
        new Color(0.10f, 0.25f, 0.95f, 0.09f), // blue
        new Color(0.90f, 0.20f, 0.55f, 0.08f), // pink
        new Color(0.10f, 0.80f, 0.95f, 0.07f), // cyan
        new Color(0.95f, 0.65f, 0.10f, 0.06f), // gold
    };

    private void BuildOrbs()
    {
        int count  = OrbColors.Length;
        _orbRects  = new RectTransform[count];
        _orbOrigins = new Vector2[count];
        _orbSpeeds  = new float[count];
        _orbPhases  = new float[count];
        _orbRadii   = new float[count];

        var parent = MakeFullscreen("BG_Orbs", 1);

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Orb_" + i);
            go.transform.SetParent(parent.transform, false);

            var rt  = go.AddComponent<RectTransform>();
            float sz = Random.Range(250f, 450f);
            rt.sizeDelta = new Vector2(sz, sz);

            var origin = new Vector2(
                Random.Range(-_w * 0.38f, _w * 0.38f),
                Random.Range(-_h * 0.38f, _h * 0.38f));
            rt.anchoredPosition = origin;

            var img = go.AddComponent<Image>();
            img.color         = OrbColors[i];
            img.raycastTarget = false;

            _orbRects[i]   = rt;
            _orbOrigins[i] = origin;
            _orbSpeeds[i]  = Random.Range(0.018f, 0.048f);
            _orbPhases[i]  = Random.Range(0f, Mathf.PI * 2f);
            _orbRadii[i]   = Random.Range(35f, 90f);
        }
    }

    // ── Stars ─────────────────────────────────────────────────────────────────

    private void BuildStars()
    {
        _starRects     = new RectTransform[StarCount];
        _starImages    = new Image[StarCount];
        _starSpeeds    = new float[StarCount];
        _twinkleSpeeds = new float[StarCount];
        _twinklePhases = new float[StarCount];

        var parent = MakeFullscreen("BG_Stars", 2);

        for (int i = 0; i < StarCount; i++)
        {
            var go = new GameObject("Star_" + i);
            go.transform.SetParent(parent.transform, false);

            var rt = go.AddComponent<RectTransform>();
            float sz = Random.Range(1.5f, 5.5f);
            rt.sizeDelta        = new Vector2(sz, sz);
            rt.anchoredPosition = RandomPos();

            var img = go.AddComponent<Image>();
            float b = Random.Range(0.7f, 1f);
            img.color         = new Color(b, b, b, Random.Range(0.3f, 0.9f));
            img.raycastTarget = false;

            _starRects[i]     = rt;
            _starImages[i]    = img;
            _starSpeeds[i]    = Random.Range(10f, 30f);
            _twinkleSpeeds[i] = Random.Range(0.6f, 2.2f);
            _twinklePhases[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_starRects == null) return;

        float dt = Time.deltaTime;
        float t  = Time.time;

        // Stars: drift upward + twinkle
        for (int i = 0; i < StarCount; i++)
        {
            var pos = _starRects[i].anchoredPosition;
            pos.y += _starSpeeds[i] * dt;

            if (pos.y > _h * 0.5f + 8f)
                pos = new Vector2(Random.Range(-_w * 0.5f, _w * 0.5f), -_h * 0.5f - 8f);

            _starRects[i].anchoredPosition = pos;

            float alpha = Mathf.Lerp(0.15f, 0.95f,
                (Mathf.Sin(t * _twinkleSpeeds[i] + _twinklePhases[i]) + 1f) * 0.5f);
            var c = _starImages[i].color;
            _starImages[i].color = new Color(c.r, c.g, c.b, alpha);
        }

        // Orbs: gentle sinusoidal drift
        if (_orbRects == null) return;
        for (int i = 0; i < _orbRects.Length; i++)
        {
            float ot = t * _orbSpeeds[i] + _orbPhases[i];
            _orbRects[i].anchoredPosition = _orbOrigins[i] + new Vector2(
                Mathf.Sin(ot)         * _orbRadii[i],
                Mathf.Cos(ot * 0.7f) * _orbRadii[i] * 0.55f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private GameObject MakeFullscreen(string name, int siblingIndex)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_canvas.transform, false);
        go.transform.SetSiblingIndex(siblingIndex);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return go;
    }

    private Vector2 RandomPos() =>
        new Vector2(Random.Range(-_w * 0.5f, _w * 0.5f),
                    Random.Range(-_h * 0.5f, _h * 0.5f));
}
