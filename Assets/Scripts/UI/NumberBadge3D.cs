using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VacuumVille.UI
{
    /// <summary>
    /// Pseudo-3D extruded coin badge for answer buttons and level-select numbers.
    /// Builds 7 depth-slab layers that shift on a sine wave to simulate a rotating 3D coin.
    /// A specular stripe sweeps the face periodically for a shiny toy-like feel.
    /// Call SetNumber() + SetColor() to configure, ExplodeCorrect() on correct tap.
    /// Self-contained — creates its own TMP label if none found in children.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class NumberBadge3D : MonoBehaviour
    {
        // ── 3-D geometry constants ───────────────────────────────────────────────
        private const int   DepthSlabs      = 7;
        private const float SlabSpacing     = 3.1f;   // px between depth slabs
        private const float GlowScale       = 1.22f;  // glow corona scale relative to badge
        private const float ShadowOffset    = 9f;     // drop shadow displacement

        // ── Tilt animation ───────────────────────────────────────────────────────
        private const float TiltPeriod     = 2.8f;    // seconds for one full tilt cycle
        private const float TiltAmplitude  = 12f;     // max pixel shift of slab stack

        // ── Specular sweep ───────────────────────────────────────────────────────
        private const float SpecularPause  = 4.0f;    // seconds between sweeps
        private const float SpecularSpeed  = 0.5f;    // seconds the sweep takes

        // ── Explosion particles ──────────────────────────────────────────────────
        private const int   ReticleCount   = 340;
        private const int   ChipCount      = 65;
        private const int   StarCount      = 45;
        private const float Gravity        = -950f;

        // ── Shared sprites (loaded once) ─────────────────────────────────────────
        private static Sprite _badgeSprite;
        private static Sprite _chipSprite;
        private static Sprite _starSprite;
        private static Sprite _circleSprite;

        // ── Layer references ─────────────────────────────────────────────────────
        private Image   _glowLayer;
        private Image   _shadowLayer;
        private Image[] _depthSlabs  = new Image[DepthSlabs];
        private Image   _faceLayer;
        private Image   _innerRim;
        private Image   _highlightArc;
        private Image   _specularLine;

        // ── Label references ─────────────────────────────────────────────────────
        private TextMeshProUGUI _labelMain;
        private TextMeshProUGUI _labelChromR;   // red chromatic offset
        private TextMeshProUGUI _labelChromB;   // blue chromatic offset

        // ── State ────────────────────────────────────────────────────────────────
        private Color _faceColor = new Color(0.13f, 0.59f, 0.95f);
        private RectTransform _rt;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            LoadSprites();
            BuildLayers();
        }

        private void OnEnable()
        {
            StartCoroutine(AppearBounce());
            StartCoroutine(TiltLoop());
            StartCoroutine(SpecularSweep());
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void SetNumber(string text)
        {
            if (_labelMain)   _labelMain.text   = text;
            if (_labelChromR) _labelChromR.text = text;
            if (_labelChromB) _labelChromB.text = text;
        }

        public void SetColor(Color c)
        {
            _faceColor = c;
            ApplyFaceColor();
        }

        public void ExplodeCorrect()
        {
            Core.AudioManager.Instance?.PlaySFX("Audio/SFX/answer_explode");
            StartCoroutine(PunchScale());
            StartCoroutine(ExplodeBurst());
        }

        // ── Layer construction ───────────────────────────────────────────────────

        private void BuildLayers()
        {
            // Resolve or create the main label first so we can bring it to front later
            _labelMain = GetComponentInChildren<TextMeshProUGUI>(true);
            if (_labelMain == null)
                _labelMain = CreateLabel("Label", Vector2.zero, 0f, Color.white);

            _labelMain.color     = Color.white;
            _labelMain.fontStyle |= FontStyles.Bold;

            // ── Layer stack (inserted as first siblings so they sit behind label) ──

            // 0. Glow corona — soft aura behind badge
            _glowLayer = MakeLayer("Badge_Glow", Vector2.zero, GlowScale);
            _glowLayer.color = GlowColor(_faceColor);

            // 1. Drop shadow
            _shadowLayer = MakeLayer("Badge_Shadow", new Vector2(ShadowOffset, -ShadowOffset), 1.06f);
            _shadowLayer.color = new Color(0f, 0f, 0f, 0.5f);

            // 2–8. Depth slabs: simulate extruded side faces of the coin
            for (int i = 0; i < DepthSlabs; i++)
            {
                float d = (i + 1) * SlabSpacing;
                _depthSlabs[i] = MakeLayer($"Badge_Slab{i}", new Vector2(d, -d), 1f);
                _depthSlabs[i].color = SlabColor(_faceColor, i);
            }

            // 9. Main face
            _faceLayer = MakeLayer("Badge_Face", Vector2.zero, 1f);
            _faceLayer.color = _faceColor;

            // 10. Inner rim: edge-lit ring (slightly smaller, soft white)
            _innerRim = MakeLayer("Badge_InnerRim", Vector2.zero, 0.87f);
            _innerRim.color = new Color(1f, 1f, 1f, 0.09f);

            // 11. Highlight arc: top-left glossy spot
            _highlightArc = MakeLayer("Badge_Highlight", new Vector2(-4f, 11f), 0f);
            var hlRt = _highlightArc.GetComponent<RectTransform>();
            hlRt.localScale = new Vector3(0.62f, 0.36f, 1f);
            _highlightArc.color = new Color(1f, 1f, 1f, 0.34f);

            // 12. Specular line (sweeps across face, starts invisible)
            _specularLine = MakeLayer("Badge_Specular", new Vector2(0f, 60f), 0f);
            var specRt = _specularLine.GetComponent<RectTransform>();
            specRt.localScale = new Vector3(1.05f, 0.18f, 1f);
            _specularLine.color = Color.clear;

            // Bring label stack to front
            _labelMain.transform.SetAsLastSibling();
            BuildChromaticLabels();
        }

        private TextMeshProUGUI CreateLabel(string n, Vector2 offset, float shadowOffset, Color color)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = offset;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize              = 64f;
            tmp.color                 = color;
            tmp.alignment             = TextAlignmentOptions.Center;
            tmp.fontStyle             = FontStyles.Bold;
            tmp.enableWordWrapping    = false;
            tmp.raycastTarget         = false;
            return tmp;
        }

        private void BuildChromaticLabels()
        {
            // Subtle chromatic aberration — two offset copies with red/blue tint
            var mrt = _labelMain.GetComponent<RectTransform>();
            _labelChromR = CloneLabel("Label_ChromR", new Vector2(-2f, 2f),  new Color(1f, 0.35f, 0.35f, 0.45f));
            _labelChromB = CloneLabel("Label_ChromB", new Vector2( 2f, -2f), new Color(0.35f, 0.55f, 1f,   0.45f));

            // Insert below main label
            int idx = _labelMain.transform.GetSiblingIndex();
            _labelChromR.transform.SetSiblingIndex(Mathf.Max(0, idx - 1));
            _labelChromB.transform.SetSiblingIndex(Mathf.Max(0, idx - 1));
            _labelMain.transform.SetAsLastSibling();
        }

        private TextMeshProUGUI CloneLabel(string name, Vector2 offset, Color color)
        {
            var go = Object.Instantiate(_labelMain.gameObject, _labelMain.transform.parent);
            go.name = name;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.color         = color;
            tmp.raycastTarget = false;
            var rt  = go.GetComponent<RectTransform>();
            var mrt = _labelMain.GetComponent<RectTransform>();
            rt.anchorMin        = mrt.anchorMin;
            rt.anchorMax        = mrt.anchorMax;
            rt.sizeDelta        = mrt.sizeDelta;
            rt.anchoredPosition = mrt.anchoredPosition + offset;
            return tmp;
        }

        private Image MakeLayer(string layerName, Vector2 offset, float uniformScale)
        {
            var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = offset;
            if (uniformScale > 0f)
                rt.localScale = Vector3.one * uniformScale;

            var img = go.GetComponent<Image>();
            img.sprite        = _badgeSprite;
            img.type          = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private void ApplyFaceColor()
        {
            if (_glowLayer)  _glowLayer.color  = GlowColor(_faceColor);
            if (_faceLayer)  _faceLayer.color  = _faceColor;
            for (int i = 0; i < DepthSlabs; i++)
                if (_depthSlabs[i]) _depthSlabs[i].color = SlabColor(_faceColor, i);
        }

        // ── Tilt loop ────────────────────────────────────────────────────────────
        // Shifts depth slabs left/right on a sine wave.  The highlight shifts the
        // OPPOSITE direction — light stays fixed while the badge rotates.  Result:
        // convincing illusion of a 3-D coin slowly rocking back and forth.

        private IEnumerator TiltLoop()
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                float tilt = Mathf.Sin(t / TiltPeriod * Mathf.PI * 2f); // −1 … +1

                // Shift depth slabs — deeper slabs move more (perspective)
                for (int i = 0; i < DepthSlabs; i++)
                {
                    if (_depthSlabs[i] == null) continue;
                    float d   = (i + 1) * SlabSpacing;
                    float xShift = tilt * TiltAmplitude * (1f + i * 0.08f);
                    _depthSlabs[i].rectTransform.anchoredPosition = new Vector2(d + xShift, -d);
                }

                // Shift highlight opposite to tilt
                if (_highlightArc != null)
                    _highlightArc.rectTransform.anchoredPosition = new Vector2(-tilt * 9f, 11f);

                // Pulse glow in sync with tilt
                if (_glowLayer != null)
                {
                    float pulse = 0.28f + 0.12f * Mathf.Abs(tilt);
                    Color gc = GlowColor(_faceColor);
                    _glowLayer.color = new Color(gc.r, gc.g, gc.b, pulse);
                }

                yield return null;
            }
        }

        // ── Specular sweep ───────────────────────────────────────────────────────

        private IEnumerator SpecularSweep()
        {
            while (true)
            {
                yield return new WaitForSeconds(SpecularPause);
                if (_specularLine == null) yield break;

                var rt = _specularLine.rectTransform;
                float elapsed = 0f;

                while (elapsed < SpecularSpeed)
                {
                    float p = elapsed / SpecularSpeed;
                    float y = Mathf.Lerp(32f, -32f, p);
                    rt.anchoredPosition   = new Vector2(0f, y);
                    _specularLine.color   = new Color(1f, 1f, 1f, Mathf.Sin(p * Mathf.PI) * 0.55f);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                _specularLine.color = Color.clear;
            }
        }

        // ── Appear bounce ────────────────────────────────────────────────────────
        // Pops in from flat (like being pressed out of the screen toward you).

        private IEnumerator AppearBounce()
        {
            transform.localScale = new Vector3(1.25f, 0.04f, 1f);

            // Phase 1: spring open
            float elapsed = 0f, dur = 0.22f;
            while (elapsed < dur)
            {
                float p  = elapsed / dur;
                float ep = 1f - (1f - p) * (1f - p);   // ease-out quad
                transform.localScale = new Vector3(
                    Mathf.Lerp(1.25f, 0.90f, ep),
                    Mathf.Lerp(0.04f, 1.10f, ep),
                    1f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 2: settle
            elapsed = 0f; dur = 0.11f;
            while (elapsed < dur)
            {
                float p = elapsed / dur;
                transform.localScale = new Vector3(
                    Mathf.Lerp(0.90f, 1f, p),
                    Mathf.Lerp(1.10f, 1f, p),
                    1f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        // ── Punch scale ──────────────────────────────────────────────────────────

        private IEnumerator PunchScale()
        {
            Vector3 orig = transform.localScale;
            float elapsed = 0f, dur = 0.22f;
            while (elapsed < dur)
            {
                float s = 1f + Mathf.Sin(elapsed / dur * Mathf.PI) * 0.38f;
                transform.localScale = orig * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = orig;
        }

        // ── Explosion ────────────────────────────────────────────────────────────

        private IEnumerator ExplodeBurst()
        {
            Canvas root = GetRootCanvas();
            if (root == null) yield break;

            Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            var   cRt  = root.GetComponent<RectTransform>();

            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, _rt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cRt, sp, cam, out Vector2 lp);

            // Screen flash — tinted to badge color
            StartCoroutine(ScreenFlash(root.transform,
                new Color(_faceColor.r * 0.6f + 0.4f, _faceColor.g * 0.6f + 0.4f, _faceColor.b * 0.6f + 0.4f, 0.6f), 0.38f));

            // Shockwave rings
            for (int r = 0; r < 3; r++)
                StartCoroutine(ShockwaveRing(root.transform, lp, r * 0.075f,
                    r == 0 ? new Color(_faceColor.r, _faceColor.g, _faceColor.b, 0.9f)
                           : Color.HSVToRGB(Random.value, 0.75f, 1f)));

            // Particles — yielding every 40 to avoid a single-frame spike
            int spawned = 0;
            for (int i = 0; i < ReticleCount; i++)
            {
                SpawnParticle(root.transform, lp, _circleSprite ?? _chipSprite,
                    4f, 13f, 260f, 920f, 0.5f, 1.0f);
                if (++spawned % 40 == 0) yield return null;
            }
            for (int i = 0; i < ChipCount; i++)
            {
                SpawnParticle(root.transform, lp, _chipSprite,
                    10f, 22f, 200f, 680f, 0.55f, 0.95f);
                if (++spawned % 40 == 0) yield return null;
            }
            for (int i = 0; i < StarCount; i++)
            {
                SpawnParticle(root.transform, lp, _starSprite,
                    12f, 26f, 150f, 580f, 0.7f, 1.3f);
                if (++spawned % 40 == 0) yield return null;
            }
        }

        // ── VFX helpers ──────────────────────────────────────────────────────────

        private IEnumerator ScreenFlash(Transform parent, Color color, float dur)
        {
            var go  = new GameObject("VFX_Flash");
            go.transform.SetParent(parent, false);
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

        private IEnumerator ShockwaveRing(Transform parent, Vector2 center,
                                          float delay, Color color)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            var go  = new GameObject("VFX_Ring");
            go.transform.SetParent(parent, false); go.transform.SetAsLastSibling();
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90f, 90f); rt.anchoredPosition = center;
            var img = go.AddComponent<Image>();
            img.sprite = _circleSprite; img.color = color; img.raycastTarget = false;

            float e = 0f, dur = 0.58f;
            while (e < dur && go != null)
            {
                float p = e / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 5.0f, p);
                img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.85f, 0f, p));
                e += Time.deltaTime; yield return null;
            }
            if (go) Destroy(go);
        }

        private void SpawnParticle(Transform parent, Vector2 origin, Sprite sprite,
                                   float sMin, float sMax,
                                   float vMin, float vMax,
                                   float lMin, float lMax)
        {
            var go  = new GameObject("P");
            go.transform.SetParent(parent, false); go.transform.SetAsLastSibling();

            float sz = Random.Range(sMin, sMax);
            var rt   = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(sz, sz);
            rt.anchoredPosition = origin + Random.insideUnitCircle * 22f;

            var img = go.AddComponent<Image>();
            if (sprite) img.sprite = sprite;
            img.raycastTarget = false;
            img.color         = Color.HSVToRGB(Random.value, 0.9f, 1f);

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(vMin, vMax);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            float spin  = Random.Range(-720f, 720f);
            float life  = Random.Range(lMin, lMax);

            StartCoroutine(AnimateParticle(go, rt, img, vel, spin, life));
        }

        private IEnumerator AnimateParticle(GameObject go, RectTransform rt, Image img,
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

        // ── Utility ──────────────────────────────────────────────────────────────

        private Canvas GetRootCanvas()
        {
            Canvas c = GetComponentInParent<Canvas>();
            while (c != null && c.transform.parent != null)
            {
                var p = c.transform.parent.GetComponentInParent<Canvas>();
                if (p == null) break;
                c = p;
            }
            return c;
        }

        private static void LoadSprites()
        {
            if (!_badgeSprite)  _badgeSprite  = Resources.Load<Sprite>("Sprites/badge_3d");
            if (!_chipSprite)   _chipSprite   = Resources.Load<Sprite>("Sprites/chip_particle");
            if (!_starSprite)   _starSprite   = Resources.Load<Sprite>("Sprites/star_particle");
            if (!_circleSprite) _circleSprite = Resources.Load<Sprite>("Sprites/circle");
        }

        /// Darkened, slightly desaturated slab color for depth face i (0 = nearest)
        private static Color SlabColor(Color face, int slabIndex)
        {
            float t   = (float)slabIndex / (DepthSlabs - 1);      // 0 … 1
            float dark = Mathf.Lerp(0.72f, 0.28f, t);             // brightest near top
            return new Color(face.r * dark, face.g * dark, face.b * dark, 1f);
        }

        private static Color GlowColor(Color face)
            => new Color(face.r, face.g, face.b, 0.32f);
    }
}
