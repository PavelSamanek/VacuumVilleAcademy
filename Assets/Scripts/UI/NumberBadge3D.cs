using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VacuumVille.UI
{
    /// <summary>
    /// Attaches to a choice-button root. Builds a layered pseudo-3D badge appearance
    /// and provides ExplodeCorrect() which spawns a particle burst when tapped correctly.
    /// Call SetNumber() and SetColor() from TaskDisplayController.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class NumberBadge3D : MonoBehaviour
    {
        // Particle counts & physics
        private const int ChipCount  = 90;
        private const int StarCount  = 30;
        private const float ParticleGravity = -900f;

        // Layer colours relative to badge face colour
        private static readonly Color ShadowTint    = new Color(0f,    0f,    0f,    0.45f);
        private static readonly Color DepthDarkness = new Color(0f,    0f,    0f,    0.35f);

        // Cached sprites (loaded once)
        private static Sprite _badgeSprite;
        private static Sprite _chipSprite;
        private static Sprite _starSprite;

        // Badge layers
        private Image _shadowLayer;
        private Image _depthLayer;
        private Image _faceLayer;
        private Image _highlightLayer;

        // Label layers
        private TextMeshProUGUI _labelMain;
        private TextMeshProUGUI _labelShadow;

        private Color _faceColor = new Color(0.13f, 0.59f, 0.95f);

        private RectTransform _rt;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            LoadSprites();
            BuildLayers();
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public void SetNumber(string text)
        {
            if (_labelMain   != null) _labelMain.text   = text;
            if (_labelShadow != null) _labelShadow.text = text;
        }

        public void SetColor(Color c)
        {
            _faceColor = c;
            if (_faceLayer      != null) _faceLayer.color      = c;
            if (_depthLayer     != null) _depthLayer.color      = MulColor(c, DepthDarkness);
            if (_highlightLayer != null) _highlightLayer.color  = new Color(1f, 1f, 1f, 0.28f);
        }

        public void ExplodeCorrect()
        {
            Core.AudioManager.Instance?.PlaySFX("Audio/SFX/answer_explode");
            StartCoroutine(ParticleBurst());
            StartCoroutine(PunchScale());
        }

        // ── Layer Construction ──────────────────────────────────────────────────

        private void BuildLayers()
        {
            // Find or create the label that the button already has
            _labelMain = GetComponentInChildren<TextMeshProUGUI>(true);

            if (_badgeSprite == null)
            {
                // No badge sprite — skip visual layers, just enhance label
                EnsureLabelShadow();
                return;
            }

            // We insert layers below the existing children
            // 1. Shadow (offset down-right, darkened)
            _shadowLayer = MakeLayer("Badge_Shadow", new Vector2(6, -6), new Vector2(1.05f, 1.05f));
            _shadowLayer.color = ShadowTint;

            // 2. Depth/side face (offset slightly up, dark tint of badge colour)
            _depthLayer = MakeLayer("Badge_Depth", new Vector2(4, -4), Vector2.one);
            _depthLayer.color = MulColor(_faceColor, DepthDarkness);

            // 3. Main face
            _faceLayer = MakeLayer("Badge_Face", Vector2.zero, Vector2.one);
            _faceLayer.color = _faceColor;

            // 4. Highlight stripe (top portion, white semi-transparent)
            _highlightLayer = MakeLayer("Badge_Highlight", new Vector2(0, 14), new Vector2(0.85f, 0.4f));
            _highlightLayer.color = new Color(1f, 1f, 1f, 0.28f);

            // Bring label above badge layers
            if (_labelMain != null)
                _labelMain.transform.SetAsLastSibling();

            EnsureLabelShadow();
        }

        private Image MakeLayer(string layerName, Vector2 offset, Vector2 scale)
        {
            var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = offset;
            rt.localScale = new Vector3(scale.x, scale.y, 1f);

            var img = go.GetComponent<Image>();
            img.sprite = _badgeSprite;
            img.type   = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private void EnsureLabelShadow()
        {
            if (_labelMain == null) return;

            // Clone label as shadow (darker, offset)
            var shadowGo = Instantiate(_labelMain.gameObject, _labelMain.transform.parent);
            shadowGo.name = "Label_Shadow";

            // Insert just below the main label
            int siblingIdx = _labelMain.transform.GetSiblingIndex();
            shadowGo.transform.SetSiblingIndex(Mathf.Max(0, siblingIdx - 1));

            _labelShadow = shadowGo.GetComponent<TextMeshProUGUI>();
            _labelShadow.color = new Color(0f, 0f, 0f, 0.55f);
            _labelShadow.raycastTarget = false;

            var shadowRt = shadowGo.GetComponent<RectTransform>();
            var mainRt   = _labelMain.GetComponent<RectTransform>();
            shadowRt.anchorMin        = mainRt.anchorMin;
            shadowRt.anchorMax        = mainRt.anchorMax;
            shadowRt.sizeDelta        = mainRt.sizeDelta;
            shadowRt.anchoredPosition = mainRt.anchoredPosition + new Vector2(2.5f, -2.5f);
        }

        // ── Particle Burst ──────────────────────────────────────────────────────

        private IEnumerator ParticleBurst()
        {
            // Find the root canvas to parent particles so they render above everything
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            while (rootCanvas != null && rootCanvas.transform.parent != null)
            {
                var parent = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
                if (parent == null) break;
                rootCanvas = parent;
            }

            if (rootCanvas == null) yield break;

            // World position of this badge centre
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                rootCanvas.worldCamera, _rt.position);

            // Spawn chips
            if (_chipSprite != null)
                for (int i = 0; i < ChipCount; i++)
                    SpawnParticle(rootCanvas, screenPos, _chipSprite, 14f, 22f);

            // Spawn stars
            if (_starSprite != null)
                for (int i = 0; i < StarCount; i++)
                    SpawnParticle(rootCanvas, screenPos, _starSprite, 10f, 16f);

            yield break;
        }

        private void SpawnParticle(Canvas canvas, Vector2 screenOrigin, Sprite sprite,
                                   float sizeMin, float sizeMax)
        {
            var go = new GameObject("P", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();

            float size = Random.Range(sizeMin, sizeMax);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            // Convert screen pos to canvas local
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.GetComponent<RectTransform>(), screenOrigin,
                    canvas.worldCamera, out Vector2 localPos))
                rt.anchoredPosition = localPos;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;

            // Random tint — bright, saturated
            float hue = Random.value;
            img.color = Color.HSVToRGB(hue, 0.85f, 1f);

            // Random initial velocity (radial burst)
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(380f, 780f);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

            float lifetime = Random.Range(0.55f, 0.95f);
            float spin = Random.Range(-540f, 540f);

            StartCoroutine(AnimateParticle(go, rt, img, vel, spin, lifetime));
        }

        private IEnumerator AnimateParticle(GameObject go, RectTransform rt, Image img,
                                            Vector2 vel, float spin, float lifetime)
        {
            float elapsed = 0f;
            Vector2 pos   = rt.anchoredPosition;

            while (elapsed < lifetime && go != null)
            {
                float dt = Time.deltaTime;
                vel.y  += ParticleGravity * dt;
                pos    += vel * dt;
                rt.anchoredPosition = pos;
                rt.localRotation    = Quaternion.Euler(0, 0, spin * elapsed);

                float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);

                elapsed += dt;
                yield return null;
            }

            if (go != null) Destroy(go);
        }

        private IEnumerator PunchScale()
        {
            Vector3 original = transform.localScale;
            float elapsed = 0f;
            float duration = 0.18f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
                transform.localScale = original * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = original;
        }

        // ── Sprite Loading ──────────────────────────────────────────────────────

        private static void LoadSprites()
        {
            if (_badgeSprite == null) _badgeSprite = Resources.Load<Sprite>("Sprites/badge_3d");
            if (_chipSprite  == null) _chipSprite  = Resources.Load<Sprite>("Sprites/chip_particle");
            if (_starSprite  == null) _starSprite  = Resources.Load<Sprite>("Sprites/star_particle");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static Color MulColor(Color base_, Color mul)
        {
            float r = base_.r * (1f - mul.a) + mul.r * mul.a;
            float g = base_.g * (1f - mul.a) + mul.g * mul.a;
            float b = base_.b * (1f - mul.a) + mul.b * mul.a;
            return new Color(r, g, b, base_.a);
        }
    }
}
