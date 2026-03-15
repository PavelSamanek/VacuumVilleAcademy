using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VacuumVille.UI
{
    /// <summary>
    /// Procedurally builds the Home screen decoration at runtime:
    ///   - Full-screen gradient background image
    ///   - 4 semi-transparent floating robot vacuums (bobbing animation)
    ///   - Continuously spawning drifting star particles
    /// All elements are created as children of the scene Canvas so they
    /// respect the Screen Space Overlay coordinate system.
    /// </summary>
    public class HomeScreenDecor : MonoBehaviour
    {
        private static readonly string[] VacuumSpritePaths =
        {
            "Sprites/vacuum_rumble",
            "Sprites/vacuum_xixi",
            "Sprites/vacuum_luna",
            "Sprites/vacuum_rocky",
        };

        // Anchored positions in the 1080×1920 canvas — placed at corners/sides
        // away from the centred button column.
        private static readonly Vector2[] VacuumPositions =
        {
            new Vector2(-400f,  720f),   // top-left
            new Vector2( 380f,  560f),   // top-right
            new Vector2(-390f, -580f),   // bottom-left
            new Vector2( 370f, -700f),   // bottom-right
        };

        private static readonly float[] VacuumScales   = { 1.0f, 0.85f, 0.90f, 0.75f };
        private static readonly float[] VacuumAlphas   = { 0.50f, 0.45f, 0.45f, 0.40f };
        private static readonly float[] BobSpeeds      = { 0.55f, 0.62f, 0.58f, 0.66f };
        private static readonly float[] BobAmplitudes  = { 22f,   18f,   20f,   16f   };
        private static readonly float[] BobOffsets     = { 0f,    1.1f,  2.4f,  3.6f  };

        private Canvas _canvas;

        private void Start()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) _canvas = FindObjectOfType<Canvas>();
            if (_canvas == null) return;

            BuildBackground();
            BuildFloatingVacuums();
            SetupLogo();
            StartCoroutine(StarSpawnLoop());
            StartCoroutine(DotSpawnLoop());
        }

        // ── Logo ─────────────────────────────────────────────────────────────

        private void SetupLogo()
        {
            var logoGo = GameObject.Find("Logo");
            if (logoGo == null) return;

            var rt = logoGo.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(260f, 260f);

            var img = logoGo.GetComponent<Image>();
            if (img == null) return;

            var sp = Resources.Load<Sprite>("Sprites/vacuum_rumble");
            if (sp != null)
            {
                img.sprite = sp;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                // Bright gold fallback
                img.color = new Color(1f, 0.84f, 0.1f);
            }

            if (rt != null) StartCoroutine(BobRoutine(rt, 0.48f, 14f, 0f));
        }

        // ── Background ───────────────────────────────────────────────────────

        private void BuildBackground()
        {
            var go = new GameObject("BG_Gradient");
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetAsFirstSibling();

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            var sp = Resources.Load<Sprite>("Sprites/bg_home");
            if (sp != null)
            {
                img.sprite = sp;
                img.type   = Image.Type.Simple;
                img.preserveAspect = false;
                img.color  = Color.white;
            }
            else
            {
                // Fallback solid colour if sprite didn't load
                img.color = new Color(0.05f, 0.14f, 0.43f);
            }
        }

        // ── Floating vacuum robots ────────────────────────────────────────────

        private void BuildFloatingVacuums()
        {
            for (int i = 0; i < VacuumPositions.Length; i++)
            {
                var sp = Resources.Load<Sprite>(VacuumSpritePaths[i % VacuumSpritePaths.Length]);
                if (sp == null) continue;

                var go = new GameObject($"DecorVacuum_{i}");
                go.transform.SetParent(_canvas.transform, false);
                go.transform.SetSiblingIndex(1);   // above BG, below everything else

                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = VacuumPositions[i];
                float size = 180f * VacuumScales[i];
                rt.sizeDelta = new Vector2(size, size);

                var img = go.AddComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
                img.color = new Color(1f, 1f, 1f, VacuumAlphas[i]);

                // Slight random tint per robot for variety
                var tints = new Color[]
                {
                    new Color(0.85f, 0.95f, 1.00f),
                    new Color(0.95f, 1.00f, 0.90f),
                    new Color(1.00f, 0.90f, 0.95f),
                    new Color(0.90f, 0.95f, 1.00f),
                };
                img.color = new Color(tints[i].r, tints[i].g, tints[i].b, VacuumAlphas[i]);

                StartCoroutine(BobRoutine(rt, BobSpeeds[i], BobAmplitudes[i], BobOffsets[i]));
            }
        }

        private IEnumerator BobRoutine(RectTransform rt, float speed, float amplitude, float offset)
        {
            if (rt == null) yield break;
            Vector2 origin = rt.anchoredPosition;
            float t = offset;
            while (rt != null)
            {
                rt.anchoredPosition = origin + new Vector2(
                    Mathf.Sin(t * speed * 0.7f * Mathf.PI * 2f) * amplitude * 0.3f,
                    Mathf.Sin(t * speed       * Mathf.PI * 2f) * amplitude);
                t += Time.deltaTime;
                yield return null;
            }
        }

        // ── Drifting star particles ───────────────────────────────────────────

        private IEnumerator StarSpawnLoop()
        {
            while (true)
            {
                SpawnStar();
                yield return new WaitForSeconds(Random.Range(0.5f, 1.1f));
            }
        }

        private void SpawnStar()
        {
            var go = new GameObject("Star");
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetSiblingIndex(1);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            float startX = Random.Range(-500f, 500f);
            float startY = Random.Range(-920f, 920f);
            rt.anchoredPosition = new Vector2(startX, startY);
            float sz = Random.Range(14f, 28f);
            rt.sizeDelta = new Vector2(sz, sz);

            var img = go.AddComponent<Image>();
            var sp = Resources.Load<Sprite>("Sprites/star_particle");
            if (sp != null) img.sprite = sp;
            img.color = new Color(1f, 1f, 0.85f, 0f);
            img.raycastTarget = false;

            StartCoroutine(AnimateStar(rt, img));
        }

        private IEnumerator AnimateStar(RectTransform rt, Image img)
        {
            float dur = Random.Range(2.5f, 5.0f);
            float rise = Random.Range(60f, 180f);
            Vector2 start = rt.anchoredPosition;
            float drift = Random.Range(-30f, 30f);
            float t = 0f;
            while (t < dur && rt != null)
            {
                float frac = t / dur;
                float alpha = frac < 0.25f
                    ? frac / 0.25f
                    : frac > 0.65f
                        ? (1f - frac) / 0.35f
                        : 1f;
                if (img != null) img.color = new Color(1f, 1f, 0.85f, alpha * 0.65f);
                if (rt != null)
                {
                    rt.anchoredPosition = start + new Vector2(drift * frac, rise * frac);
                    // Twinkle scale
                    float scale = 1f + Mathf.Sin(frac * Mathf.PI * 6f) * 0.12f;
                    rt.localScale = new Vector3(scale, scale, 1f);
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        // ── Background dot field (subtle depth effect) ───────────────────────

        private IEnumerator DotSpawnLoop()
        {
            // Spawn initial batch
            for (int i = 0; i < 20; i++)
            {
                SpawnDot(instant: true);
                yield return null;
            }
            // Keep topping up slowly
            while (true)
            {
                SpawnDot(instant: false);
                yield return new WaitForSeconds(1.8f);
            }
        }

        private void SpawnDot(bool instant)
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetSiblingIndex(1);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Random.Range(-520f, 520f), Random.Range(-950f, 950f));
            float sz = Random.Range(4f, 12f);
            rt.sizeDelta = new Vector2(sz, sz);

            var img = go.AddComponent<Image>();
            var sp = Resources.Load<Sprite>("Sprites/dot_decor");
            if (sp != null) img.sprite = sp;
            img.raycastTarget = false;

            float targetAlpha = Random.Range(0.08f, 0.22f);
            img.color = new Color(0.7f, 0.85f, 1f, instant ? targetAlpha : 0f);

            StartCoroutine(AnimateDot(rt, img, targetAlpha, instant));
        }

        private IEnumerator AnimateDot(RectTransform rt, Image img, float maxAlpha, bool instant)
        {
            if (!instant)
            {
                float t = 0f;
                while (t < 1f && img != null)
                {
                    img.color = new Color(0.7f, 0.85f, 1f, Mathf.Lerp(0f, maxAlpha, t));
                    t += Time.deltaTime * 0.5f;
                    yield return null;
                }
            }

            // Gentle pulse for lifetime
            float life = Random.Range(8f, 20f);
            float elapsed = 0f;
            while (elapsed < life && rt != null)
            {
                float pulse = maxAlpha * (0.7f + 0.3f * Mathf.Sin(elapsed * 1.2f));
                if (img != null) img.color = new Color(0.7f, 0.85f, 1f, pulse);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fade out
            float ft = 0f;
            while (ft < 1.5f && img != null)
            {
                if (img != null) img.color = new Color(0.7f, 0.85f, 1f, Mathf.Lerp(maxAlpha * 0.7f, 0f, ft / 1.5f));
                ft += Time.deltaTime;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }
    }
}
