using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VacuumVille.UI
{
    /// <summary>
    /// Triggers when a child answers a math problem correctly.
    /// The solved equation ("5 + 5 = 10") shatters — every character flies apart
    /// while 400+ reticle particles, four shockwave rings, and a golden screen flash
    /// reward the correct tap.  Streak bonuses escalate the effect.
    ///
    /// Attach (or AddComponent) to the same GameObject as TaskDisplayController.
    /// Call Explode(sourceRect, solvedText, streak) from HandleCorrect().
    /// </summary>
    public class EquationExplosionVFX : MonoBehaviour
    {
        // ── Particle counts ──────────────────────────────────────────────────────
        private const int   ReticleCount     = 420;
        private const int   ChipCount        = 80;
        private const int   StarCount        = 55;
        private const int   FragsPerChar     = 7;    // extra reticles per equation char
        private const float Gravity          = -1020f;

        // ── Streak escalation ────────────────────────────────────────────────────
        private const int   StreakThreshold  = 3;
        private const float StreakMultiplier = 1.55f;

        // ── Canvas / sprites ─────────────────────────────────────────────────────
        private Canvas        _canvas;
        private RectTransform _canvasRt;
        private Camera        _cam;

        private static Sprite _circleSprite;
        private static Sprite _starSprite;
        private static Sprite _chipSprite;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Start()
        {
            // Walk up to root canvas so particles are drawn above all UI
            _canvas = GetComponentInParent<Canvas>();
            while (_canvas != null && _canvas.transform.parent != null)
            {
                var p = _canvas.transform.parent.GetComponentInParent<Canvas>();
                if (p == null) break;
                _canvas = p;
            }
            if (_canvas == null) return;

            _canvasRt = _canvas.GetComponent<RectTransform>();
            _cam      = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                            ? null
                            : _canvas.worldCamera;

            if (!_circleSprite) _circleSprite = Resources.Load<Sprite>("Sprites/circle");
            if (!_starSprite)   _starSprite   = Resources.Load<Sprite>("Sprites/star_particle");
            if (!_chipSprite)   _chipSprite   = Resources.Load<Sprite>("Sprites/chip_particle");
        }

        // ── Public entry point ───────────────────────────────────────────────────

        /// <summary>
        /// Call this immediately after the correct answer is registered.
        /// <paramref name="sourceRect"/> is the equation display RectTransform.
        /// <paramref name="solvedText"/> is the fully resolved equation, e.g. "5 + 5 = 10".
        /// </summary>
        public void Explode(RectTransform sourceRect, string solvedText, int streak = 0)
        {
            if (_canvas == null || _canvasRt == null || sourceRect == null) return;
            float mult = streak >= StreakThreshold ? StreakMultiplier : 1f;
            StartCoroutine(ExplodeRoutine(sourceRect, solvedText, mult, streak));
        }

        // ── Main coroutine ───────────────────────────────────────────────────────

        private IEnumerator ExplodeRoutine(RectTransform src, string text, float mult, int streak)
        {
            // Convert equation world position → canvas-local position
            Vector2 screenPos;
            screenPos = RectTransformUtility.WorldToScreenPoint(_cam, src.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRt, screenPos, _cam, out Vector2 center);

            // ── 1. Anticipation slow-motion ──────────────────────────────────────
            // Brief time-warp before the explosion — creates exciting suspense.
            Time.timeScale = 0.08f;
            yield return new WaitForSecondsRealtime(0.06f);
            Time.timeScale = 1f;

            // ── 2. Golden screen flash ───────────────────────────────────────────
            StartCoroutine(ScreenFlash(new Color(1f, 0.94f, 0.28f, 0.70f), 0.42f));

            // ── 3. Character fragments ───────────────────────────────────────────
            // Each visible character in the equation becomes a flying TMP particle.
            if (!string.IsNullOrEmpty(text))
                SpawnCharFragments(text, center, mult);

            // ── 4. Shockwave rings (staggered) ───────────────────────────────────
            for (int r = 0; r < 4; r++)
            {
                Color rc = r == 0
                    ? new Color(1f, 0.95f, 0.3f, 0.88f)
                    : Color.HSVToRGB(Random.value, 0.8f, 1f);
                StartCoroutine(ShockwaveRing(center, r * 0.065f, rc));
            }

            // ── 5. Canvas shake ──────────────────────────────────────────────────
            StartCoroutine(ShakeCanvas(mult));

            // ── 6. Particle burst (batched to avoid single-frame spike) ──────────
            int spawned = 0;

            int rc2 = Mathf.RoundToInt(ReticleCount * mult);
            for (int i = 0; i < rc2; i++)
            {
                SpawnParticle(center, _circleSprite ?? _chipSprite,
                    4f, 14f, 200f, 960f, 0.45f, 1.0f);
                if (++spawned % 50 == 0) yield return null;
            }

            int cc = Mathf.RoundToInt(ChipCount * mult);
            for (int i = 0; i < cc; i++)
            {
                SpawnParticle(center, _chipSprite,
                    10f, 24f, 140f, 680f, 0.55f, 1.0f);
                if (++spawned % 50 == 0) yield return null;
            }

            int sc = Mathf.RoundToInt(StarCount * mult);
            for (int i = 0; i < sc; i++)
            {
                SpawnParticle(center, _starSprite,
                    14f, 30f, 110f, 560f, 0.70f, 1.35f);
                if (++spawned % 50 == 0) yield return null;
            }

            // ── 7. Streak celebration text ───────────────────────────────────────
            if (streak >= StreakThreshold)
                StartCoroutine(StreakText(center, streak));
        }

        // ── Character fragments ──────────────────────────────────────────────────

        private void SpawnCharFragments(string text, Vector2 center, float mult)
        {
            // Estimate the horizontal extent so chars start at their visual positions
            float charWidth = 36f;
            float totalW    = text.Length * charWidth;
            float startX    = -totalW * 0.5f;

            for (int ci = 0; ci < text.Length; ci++)
            {
                char ch = text[ci];
                if (ch == ' ') continue;

                Vector2 charOrigin = center + new Vector2(startX + ci * charWidth + charWidth * 0.5f, 0f);

                // One large label fragment
                SpawnCharParticle(ch.ToString(), charOrigin, 54f, 0.55f, 1.05f);

                // Extra tiny reticles around this char position
                int frags = Mathf.RoundToInt(FragsPerChar * mult);
                for (int j = 0; j < frags; j++)
                    SpawnParticle(charOrigin, _circleSprite ?? _chipSprite,
                        3f, 10f, 280f, 820f, 0.35f, 0.85f);
            }
        }

        private void SpawnCharParticle(string ch, Vector2 origin, float fontSize,
                                       float lifeMin, float lifeMax)
        {
            var go  = new GameObject("CF_" + ch);
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetAsLastSibling();

            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(64f, 64f);
            rt.anchoredPosition = origin + Random.insideUnitCircle * 18f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text                 = ch;
            tmp.fontSize             = fontSize;
            tmp.fontStyle            = FontStyles.Bold;
            tmp.color                = Color.HSVToRGB(Random.value, 0.85f, 1f);
            tmp.alignment            = TextAlignmentOptions.Center;
            tmp.enableWordWrapping   = false;
            tmp.raycastTarget        = false;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(260f, 780f);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            float spin  = Random.Range(-960f, 960f);
            float life  = Random.Range(lifeMin, lifeMax);

            StartCoroutine(AnimateTMP(go, rt, tmp, vel, spin, life));
        }

        private IEnumerator AnimateTMP(GameObject go, RectTransform rt, TextMeshProUGUI tmp,
                                       Vector2 vel, float spin, float lifetime)
        {
            float   elapsed = 0f;
            Vector2 pos     = rt.anchoredPosition;
            Color   c       = tmp.color;

            while (elapsed < lifetime && go != null)
            {
                float dt = Time.deltaTime;
                vel.y  += Gravity * dt;
                pos    += vel * dt;
                rt.anchoredPosition = pos;
                rt.localRotation    = Quaternion.Euler(0f, 0f, spin * elapsed);
                tmp.color           = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, elapsed / lifetime));
                elapsed            += dt;
                yield return null;
            }
            if (go) Destroy(go);
        }

        // ── General image particle ───────────────────────────────────────────────

        private void SpawnParticle(Vector2 origin, Sprite sprite,
                                   float sMin, float sMax,
                                   float vMin, float vMax,
                                   float lMin, float lMax)
        {
            var go  = new GameObject("EP");
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetAsLastSibling();

            float sz = Random.Range(sMin, sMax);
            var rt   = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(sz, sz);
            rt.anchoredPosition = origin + Random.insideUnitCircle * 28f;

            var img = go.AddComponent<Image>();
            if (sprite) img.sprite = sprite;
            img.raycastTarget = false;
            img.color         = Color.HSVToRGB(Random.value, 0.92f, 1f);

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(vMin, vMax);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            float spin  = Random.Range(-720f, 720f);
            float life  = Random.Range(lMin, lMax);

            StartCoroutine(AnimateImage(go, rt, img, vel, spin, life));
        }

        private IEnumerator AnimateImage(GameObject go, RectTransform rt, Image img,
                                         Vector2 vel, float spin, float lifetime)
        {
            float   elapsed = 0f;
            Vector2 pos     = rt.anchoredPosition;
            Color   c       = img.color;

            while (elapsed < lifetime && go != null)
            {
                float dt = Time.deltaTime;
                vel.y  += Gravity * dt;
                pos    += vel * dt;
                rt.anchoredPosition = pos;
                rt.localRotation    = Quaternion.Euler(0f, 0f, spin * elapsed);
                img.color           = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, elapsed / lifetime));
                elapsed            += dt;
                yield return null;
            }
            if (go) Destroy(go);
        }

        // ── Shockwave ring ───────────────────────────────────────────────────────

        private IEnumerator ShockwaveRing(Vector2 center, float delay, Color color)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            var go  = new GameObject("VFX_Shockwave");
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetAsLastSibling();
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f); rt.anchoredPosition = center;
            var img = go.AddComponent<Image>();
            img.sprite = _circleSprite; img.color = color; img.raycastTarget = false;

            float e = 0f, dur = 0.62f;
            while (e < dur && go != null)
            {
                float p = e / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.04f, 5.8f, p * p);
                img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.88f, 0f, p));
                e += Time.deltaTime; yield return null;
            }
            if (go) Destroy(go);
        }

        // ── Screen flash ─────────────────────────────────────────────────────────

        private IEnumerator ScreenFlash(Color color, float dur)
        {
            var go  = new GameObject("VFX_Flash");
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetAsLastSibling();
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color; img.raycastTarget = false;

            float e = 0f;
            while (e < dur && go != null)
            {
                img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(color.a, 0f, e / dur));
                e += Time.deltaTime; yield return null;
            }
            if (go) Destroy(go);
        }

        // ── Canvas shake ─────────────────────────────────────────────────────────
        // Shakes individual content children (not the root canvas rect) so
        // Screen Space - Overlay canvases shake visually.

        private IEnumerator ShakeCanvas(float mult)
        {
            // Collect direct children that have rect transforms
            var targets = new System.Collections.Generic.List<(RectTransform rt, Vector2 origin)>();
            for (int i = 0; i < _canvas.transform.childCount; i++)
            {
                var rt = _canvas.transform.GetChild(i).GetComponent<RectTransform>();
                if (rt != null) targets.Add((rt, rt.anchoredPosition));
            }

            float dur = 0.38f, elapsed = 0f;
            float amp = 11f * mult;

            while (elapsed < dur)
            {
                float p   = 1f - elapsed / dur;
                float xOff = Mathf.Sin(elapsed * 58f) * amp * p;
                float yOff = Mathf.Cos(elapsed * 47f) * amp * 0.55f * p;

                foreach (var (rt, origin) in targets)
                    if (rt != null)
                        rt.anchoredPosition = origin + new Vector2(xOff, yOff);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore positions
            foreach (var (rt, origin) in targets)
                if (rt != null) rt.anchoredPosition = origin;
        }

        // ── Streak celebration ────────────────────────────────────────────────────

        private IEnumerator StreakText(Vector2 center, int streak)
        {
            yield return new WaitForSeconds(0.12f);

            var go  = new GameObject("VFX_StreakText");
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetAsLastSibling();
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(440f, 110f);
            rt.anchoredPosition = center + new Vector2(0f, 60f);
            var tmp = go.AddComponent<TextMeshProUGUI>();

            bool mega = streak >= 5;
            tmp.text      = mega ? $"MEGA STREAK  x{streak}!" : $"STREAK  x{streak}!";
            tmp.fontSize  = mega ? 72f : 60f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = Color.HSVToRGB(0.12f, 1f, 1f);   // gold
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            Vector2 startPos = rt.anchoredPosition;
            float elapsed = 0f, dur = 1.25f;

            while (elapsed < dur && go != null)
            {
                float p    = elapsed / dur;
                float ease = 1f - (1f - p) * (1f - p);
                rt.anchoredPosition = startPos + new Vector2(0f, 140f * ease);

                // Bouncy scale pulse that fades out
                float pulse = 1f + Mathf.Sin(p * Mathf.PI * 5f) * 0.09f * (1f - p);
                go.transform.localScale = Vector3.one * pulse;

                float alpha = p < 0.45f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.45f) / 0.55f);
                tmp.color   = new Color(tmp.color.r, tmp.color.g, tmp.color.b, alpha);

                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go) Destroy(go);
        }
    }
}
