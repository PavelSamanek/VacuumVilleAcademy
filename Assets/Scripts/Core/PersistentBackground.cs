using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VacuumVille.Data;

namespace VacuumVille.Core
{
    /// <summary>
    /// DontDestroyOnLoad canvas that renders a gradient background behind every scene.
    /// On the MathTask and LevelIntro scenes the background theme matches the active
    /// level's room, creating an immersive environment for each topic.
    /// </summary>
    public class PersistentBackground : MonoBehaviour
    {
        public static PersistentBackground Instance { get; private set; }

        // ── Theme definition ────────────────────────────────────────────────────

        private struct Theme
        {
            public string   spritePath;       // Resources path of gradient PNG
            public Color    starColor;        // ambient drifting star tint
            public Color    dotColor;         // ambient dot tint
            public float    starSpeed;        // rise speed multiplier
            public float    starAlpha;        // max star alpha
            public Color    buttonColor;      // answer-button face color for this theme
        }

        // Maps MathTopic → theme.  Home/default uses the home theme.
        private static readonly Dictionary<MathTopic, Theme> LevelThemes =
            new Dictionary<MathTopic, Theme>
        {
            { MathTopic.Counting1To10, new Theme {
                spritePath  = "Sprites/LevelBG/bg_bedroom",
                starColor   = new Color(0.85f, 0.80f, 1.00f),
                dotColor    = new Color(0.60f, 0.55f, 0.90f),
                starSpeed   = 0.6f, starAlpha = 0.75f,
                buttonColor = new Color(0.61f, 0.49f, 1.00f) } },   // soft lavender

            { MathTopic.Counting1To20, new Theme {
                spritePath  = "Sprites/LevelBG/bg_kitchen",
                starColor   = new Color(1.00f, 0.75f, 0.30f),
                dotColor    = new Color(1.00f, 0.60f, 0.20f),
                starSpeed   = 0.8f, starAlpha = 0.65f,
                buttonColor = new Color(1.00f, 0.65f, 0.18f) } },   // warm amber

            { MathTopic.AdditionTo10, new Theme {
                spritePath  = "Sprites/LevelBG/bg_livingroom",
                starColor   = new Color(0.40f, 0.95f, 0.90f),
                dotColor    = new Color(0.30f, 0.80f, 0.85f),
                starSpeed   = 0.7f, starAlpha = 0.60f,
                buttonColor = new Color(0.15f, 0.80f, 0.82f) } },   // teal

            { MathTopic.SubtractionWithin10, new Theme {
                spritePath  = "Sprites/LevelBG/bg_bathroom",
                starColor   = new Color(0.50f, 0.70f, 1.00f),
                dotColor    = new Color(0.30f, 0.50f, 1.00f),
                starSpeed   = 0.5f, starAlpha = 0.55f,
                buttonColor = new Color(0.25f, 0.55f, 0.95f) } },   // cobalt blue

            { MathTopic.AdditionTo20, new Theme {
                spritePath  = "Sprites/LevelBG/bg_garage",
                starColor   = new Color(0.90f, 0.65f, 0.30f),
                dotColor    = new Color(0.60f, 0.70f, 0.80f),
                starSpeed   = 1.1f, starAlpha = 0.70f,
                buttonColor = new Color(0.35f, 0.65f, 0.82f) } },   // steel blue

            { MathTopic.SubtractionWithin20, new Theme {
                spritePath  = "Sprites/LevelBG/bg_hallway",
                starColor   = new Color(0.95f, 0.55f, 1.00f),
                dotColor    = new Color(0.80f, 0.35f, 0.90f),
                starSpeed   = 0.75f, starAlpha = 0.65f,
                buttonColor = new Color(0.68f, 0.35f, 1.00f) } },   // violet

            { MathTopic.Multiplication2x5x, new Theme {
                spritePath  = "Sprites/LevelBG/bg_backyard",
                starColor   = new Color(0.60f, 1.00f, 0.40f),
                dotColor    = new Color(0.35f, 0.85f, 0.40f),
                starSpeed   = 0.65f, starAlpha = 0.60f,
                buttonColor = new Color(0.28f, 0.78f, 0.33f) } },   // grass green

            { MathTopic.DivisionBy2_3_5, new Theme {
                spritePath  = "Sprites/LevelBG/bg_attic",
                starColor   = new Color(1.00f, 0.85f, 0.40f),
                dotColor    = new Color(0.90f, 0.65f, 0.25f),
                starSpeed   = 0.5f, starAlpha = 0.55f,
                buttonColor = new Color(0.96f, 0.68f, 0.18f) } },   // warm gold

            { MathTopic.NumberOrdering, new Theme {
                spritePath  = "Sprites/LevelBG/bg_rooftop",
                starColor   = new Color(0.80f, 0.90f, 1.00f),
                dotColor    = new Color(0.65f, 0.80f, 1.00f),
                starSpeed   = 0.55f, starAlpha = 0.65f,
                buttonColor = new Color(0.35f, 0.65f, 1.00f) } },   // sky blue

            { MathTopic.ShapeCounting, new Theme {
                spritePath  = "Sprites/LevelBG/bg_grandhall",
                starColor   = new Color(1.00f, 0.85f, 0.30f),
                dotColor    = new Color(0.80f, 0.60f, 1.00f),
                starSpeed   = 0.6f, starAlpha = 0.70f,
                buttonColor = new Color(0.58f, 0.25f, 0.92f) } },   // deep purple

            { MathTopic.MixedReview, new Theme {
                spritePath  = "Sprites/LevelBG/bg_secretlab",
                starColor   = new Color(0.20f, 0.95f, 0.90f),
                dotColor    = new Color(0.10f, 0.75f, 0.80f),
                starSpeed   = 1.0f, starAlpha = 0.80f,
                buttonColor = new Color(0.08f, 0.85f, 0.92f) } },   // electric cyan
        };

        private static readonly Theme HomeTheme = new Theme {
            spritePath  = "Sprites/bg_home",
            starColor   = new Color(1.00f, 1.00f, 0.90f),
            dotColor    = new Color(0.70f, 0.85f, 1.00f),
            starSpeed   = 0.7f, starAlpha = 0.55f,
            buttonColor = new Color(0.13f, 0.59f, 0.95f)    // default blue
        };

        // ── Public theme accessors ──────────────────────────────────────────────

        /// <summary>Returns the answer-button face color for the currently active theme.</summary>
        public Color CurrentButtonColor => _currentTheme.buttonColor;

        /// <summary>Returns the button color for a given topic (used by scenes loading before transition completes).</summary>
        public static Color GetButtonColorForTopic(MathTopic topic)
        {
            if (LevelThemes.TryGetValue(topic, out Theme t)) return t.buttonColor;
            return HomeTheme.buttonColor;
        }

        // ── Runtime state ───────────────────────────────────────────────────────

        private Canvas  _canvas;
        private Image   _bgImage;
        private Theme   _currentTheme;
        private Coroutine _transitionCo;

        // Live particle colour is applied to newly spawned particles
        private Color _liveStarColor = new Color(1f, 1f, 0.9f);
        private Color _liveDotColor  = new Color(0.7f, 0.85f, 1f);
        private float _liveStarAlpha = 0.55f;
        private float _liveStarSpeed = 0.7f;

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _currentTheme = HomeTheme;
            BuildCanvas();
            BuildGradient();
            ApplyTheme(HomeTheme, instant: true);

            StartCoroutine(StarLoop());
            StartCoroutine(DotLoop());

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Pick theme based on scene name and active level
            Theme next = PickThemeForScene(scene.name);
            if (_transitionCo != null) StopCoroutine(_transitionCo);
            _transitionCo = StartCoroutine(TransitionBG(next, 0.6f));
        }

        // ── Theme selection ─────────────────────────────────────────────────────

        private static Theme PickThemeForScene(string sceneName)
        {
            // MathTask and LevelIntro scenes use the active level's topic
            if (sceneName == "MathTask" || sceneName == "LevelIntro" ||
                sceneName.StartsWith("Minigame_"))
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.ActiveLevel != null)
                {
                    if (LevelThemes.TryGetValue(gm.ActiveLevel.mathTopic, out Theme t))
                        return t;
                }
            }
            return HomeTheme;
        }

        // ── Canvas + gradient ───────────────────────────────────────────────────

        private void BuildCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -200;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            var gr = gameObject.AddComponent<GraphicRaycaster>();
            gr.enabled = false;
        }

        private void BuildGradient()
        {
            var go = new GameObject("BG");
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _bgImage = go.AddComponent<Image>();
            _bgImage.type           = Image.Type.Simple;
            _bgImage.preserveAspect = false;
            _bgImage.raycastTarget  = false;
        }

        private void ApplyTheme(Theme t, bool instant)
        {
            _currentTheme  = t;
            _liveStarColor = t.starColor;
            _liveDotColor  = t.dotColor;
            _liveStarAlpha = t.starAlpha;
            _liveStarSpeed = t.starSpeed;

            var sp = Resources.Load<Sprite>(t.spritePath);
            if (sp != null)
            {
                _bgImage.sprite = sp;
                _bgImage.color  = Color.white;
            }
            else
            {
                _bgImage.sprite = null;
                _bgImage.color  = new Color(0.05f, 0.14f, 0.43f);
            }
        }

        private IEnumerator TransitionBG(Theme next, float duration)
        {
            // Fade the canvas out, swap sprite, fade back in
            float t = 0f;
            Color startCol = _bgImage.color;
            Color dark     = new Color(startCol.r * 0.1f, startCol.g * 0.1f, startCol.b * 0.1f, 1f);

            while (t < duration * 0.5f)
            {
                _bgImage.color = Color.Lerp(startCol, dark, t / (duration * 0.5f));
                t += Time.deltaTime;
                yield return null;
            }

            ApplyTheme(next, instant: true);
            _bgImage.color = dark;

            t = 0f;
            while (t < duration * 0.5f)
            {
                _bgImage.color = Color.Lerp(dark, Color.white, t / (duration * 0.5f));
                t += Time.deltaTime;
                yield return null;
            }
            _bgImage.color = Color.white;
        }

        // ── Ambient stars ────────────────────────────────────────────────────────

        private IEnumerator StarLoop()
        {
            while (true)
            {
                SpawnStar();
                yield return new WaitForSeconds(Random.Range(0.7f, 1.4f));
            }
        }

        private void SpawnStar()
        {
            var go = new GameObject("S");
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Random.Range(-520f, 520f), Random.Range(-960f, 960f));
            float sz = Random.Range(9f, 20f);
            rt.sizeDelta = new Vector2(sz, sz);

            var img = go.AddComponent<Image>();
            var sp  = Resources.Load<Sprite>("Sprites/star_particle");
            if (sp != null) img.sprite = sp;
            img.raycastTarget = false;

            Color c = _liveStarColor;
            img.color = new Color(c.r, c.g, c.b, 0f);

            float speed = _liveStarSpeed;
            float alpha = _liveStarAlpha;
            StartCoroutine(AnimateStar(rt, img, c, speed, alpha));
        }

        private IEnumerator AnimateStar(RectTransform rt, Image img,
                                        Color col, float speedMul, float maxAlpha)
        {
            float dur   = Random.Range(3f, 6f) / speedMul;
            float rise  = Random.Range(50f, 160f) * speedMul;
            float drift = Random.Range(-28f, 28f);
            Vector2 start = rt.anchoredPosition;
            float t = 0f;

            while (t < dur && rt != null)
            {
                float frac  = t / dur;
                float alpha = frac < 0.2f ? frac / 0.2f
                            : frac > 0.7f ? (1f - frac) / 0.3f : 1f;
                if (img != null) img.color = new Color(col.r, col.g, col.b, alpha * maxAlpha);
                if (rt  != null)
                {
                    rt.anchoredPosition = start + new Vector2(drift * frac, rise * frac);
                    float s = 1f + Mathf.Sin(frac * Mathf.PI * 5f) * 0.10f;
                    rt.localScale = new Vector3(s, s, 1f);
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        // ── Ambient dots ─────────────────────────────────────────────────────────

        private IEnumerator DotLoop()
        {
            for (int i = 0; i < 18; i++) { SpawnDot(true); yield return null; }
            while (true) { SpawnDot(false); yield return new WaitForSeconds(2f); }
        }

        private void SpawnDot(bool instant)
        {
            var go = new GameObject("D");
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Random.Range(-530f, 530f), Random.Range(-960f, 960f));
            float sz = Random.Range(4f, 11f);
            rt.sizeDelta = new Vector2(sz, sz);

            var img = go.AddComponent<Image>();
            var sp  = Resources.Load<Sprite>("Sprites/dot_decor");
            if (sp != null) img.sprite = sp;
            img.raycastTarget = false;

            float maxA = Random.Range(0.07f, 0.18f);
            Color dc   = _liveDotColor;
            img.color  = new Color(dc.r, dc.g, dc.b, instant ? maxA : 0f);

            StartCoroutine(AnimateDot(rt, img, dc, maxA, instant));
        }

        private IEnumerator AnimateDot(RectTransform rt, Image img,
                                       Color col, float maxA, bool instant)
        {
            if (!instant)
            {
                float t = 0f;
                while (t < 1f && img != null)
                {
                    img.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0f, maxA, t));
                    t += Time.deltaTime * 0.5f;
                    yield return null;
                }
            }

            float life = Random.Range(8f, 22f);
            float e = 0f;
            while (e < life && rt != null)
            {
                float p = maxA * (0.7f + 0.3f * Mathf.Sin(e * 1.1f));
                if (img != null) img.color = new Color(col.r, col.g, col.b, p);
                e += Time.deltaTime;
                yield return null;
            }

            float f = 0f;
            while (f < 1.5f && img != null)
            {
                img.color = new Color(col.r, col.g, col.b,
                    Mathf.Lerp(maxA * 0.7f, 0f, f / 1.5f));
                f += Time.deltaTime;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }
    }
}
