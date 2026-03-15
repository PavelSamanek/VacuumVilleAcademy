using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Pure-static VFX helpers for all minigames.
    /// All effects work inside a Screen Space Overlay Canvas (positions = screen pixels).
    /// Every coroutine-based method accepts a MonoBehaviour owner as the runner host.
    /// </summary>
    public static class MinigameVFX
    {
        // ── Shared assets ────────────────────────────────────────────────────────

        private static Sprite _circleSprite;
        private static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                    _circleSprite = Resources.Load<Sprite>("Sprites/circle");
                return _circleSprite;
            }
        }

        // ── 1. SpawnPop ──────────────────────────────────────────────────────────
        /// <summary>Scale from 0 → 1.2 → 1 over ~0.25 s. Call right after Instantiate.</summary>
        public static void SpawnPop(MonoBehaviour owner, Transform target)
        {
            if (owner == null || target == null) return;
            owner.StartCoroutine(SpawnPopRoutine(target));
        }

        private static IEnumerator SpawnPopRoutine(Transform t)
        {
            t.localScale = Vector3.zero;
            float elapsed = 0f, phase1 = 0.18f, phase2 = 0.07f;

            // Phase 1: ease-out scale to 1.2
            while (elapsed < phase1)
            {
                if (t == null) yield break;
                float p = elapsed / phase1;
                t.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, 1f - (1f - p) * (1f - p));
                elapsed += Time.deltaTime;
                yield return null;
            }
            // Phase 2: settle to 1.0
            elapsed = 0f;
            while (elapsed < phase2)
            {
                if (t == null) yield break;
                t.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, elapsed / phase2);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        // ── 2. CollectBurst ──────────────────────────────────────────────────────
        /// <summary>
        /// Flash to color, shrink to zero, then Destroy. Replaces bare Destroy() calls.
        /// Caller must null the reference immediately after calling this.
        /// </summary>
        public static void CollectBurst(MonoBehaviour owner, GameObject target, Color color)
        {
            if (owner == null || target == null) return;
            owner.StartCoroutine(CollectBurstRoutine(target, color));
        }

        private static IEnumerator CollectBurstRoutine(GameObject target, Color color)
        {
            if (target == null) yield break;

            var images = target.GetComponentsInChildren<Image>();
            // Flash
            foreach (var img in images) if (img) img.color = color;
            yield return new WaitForSeconds(0.07f);

            // Shrink
            float duration = 0.2f, elapsed = 0f;
            Vector3 startScale = target != null ? target.transform.localScale : Vector3.one;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                float t = elapsed / duration;
                target.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (target != null) Object.Destroy(target);
        }

        // ── 3. FloatingText ──────────────────────────────────────────────────────
        /// <summary>Spawns bold text at worldPos that floats up 130px and fades over 0.9s.</summary>
        public static void FloatingText(MonoBehaviour owner, string text, Vector3 worldPos, Color color)
        {
            if (owner == null) return;
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : owner.transform;
            owner.StartCoroutine(FloatingTextRoutine(parent, text, worldPos, color));
        }

        private static IEnumerator FloatingTextRoutine(Transform parent, string text, Vector3 startPos, Color color)
        {
            var go = new GameObject("VFX_FloatingText");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280f, 80f);
            go.transform.position = startPos;
            go.transform.SetAsLastSibling();

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 58f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            float elapsed = 0f, duration = 0.9f;
            while (elapsed < duration)
            {
                if (go == null) yield break;
                float p = elapsed / duration;
                go.transform.position = startPos + new Vector3(0f, 130f * (1f - (1f - p) * (1f - p)), 0f);
                float alpha = p < 0.35f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.35f) / 0.65f);
                tmp.color = new Color(color.r, color.g, color.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        // ── 4. ShakeRect ─────────────────────────────────────────────────────────
        /// <summary>
        /// Damped horizontal shake over 0.35s. Per design spec flashes orange (#FF9100),
        /// never red. Pass any RectTransform — button, panel, or game object.
        /// </summary>
        public static void ShakeRect(MonoBehaviour owner, RectTransform rect)
        {
            if (owner == null || rect == null) return;
            owner.StartCoroutine(ShakeRectRoutine(rect));
        }

        private static IEnumerator ShakeRectRoutine(RectTransform rect)
        {
            if (rect == null) yield break;

            var img = rect.GetComponent<Image>();
            Color origColor = Color.white;
            if (img != null) { origColor = img.color; img.color = new Color(1f, 0.569f, 0f); }

            Vector2 origin = rect.anchoredPosition;
            float duration = 0.35f, elapsed = 0f, amplitude = 18f, frequency = 4f;

            while (elapsed < duration)
            {
                if (rect == null) yield break;
                float p = 1f - (elapsed / duration);
                float offset = Mathf.Sin((elapsed / duration) * frequency * Mathf.PI * 2f) * amplitude * p;
                rect.anchoredPosition = origin + new Vector2(offset, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (rect != null)
            {
                rect.anchoredPosition = origin;
                if (img != null) img.color = origColor;
            }
        }

        // ── 5. PulseRing ─────────────────────────────────────────────────────────
        /// <summary>
        /// Spawns a circle Image that expands from small to large while fading — ripple effect.
        /// Uses Assets/Resources/Sprites/circle.png.
        /// </summary>
        public static void PulseRing(MonoBehaviour owner, Vector3 worldPos, Color color)
        {
            if (owner == null) return;
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : owner.transform;
            owner.StartCoroutine(PulseRingRoutine(parent, worldPos, color));
        }

        private static IEnumerator PulseRingRoutine(Transform parent, Vector3 pos, Color color)
        {
            var go = new GameObject("VFX_PulseRing");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.05f;
            go.transform.SetAsLastSibling();

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140f, 140f);

            var img = go.AddComponent<Image>();
            img.sprite = CircleSprite;
            img.color = new Color(color.r, color.g, color.b, 0.85f);
            img.raycastTarget = false;

            float elapsed = 0f, duration = 0.45f;
            while (elapsed < duration)
            {
                if (go == null) yield break;
                float p = elapsed / duration;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 2.8f, p);
                img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.85f, 0f, p));
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        // ── 6. ScreenFlash ───────────────────────────────────────────────────────
        /// <summary>Full-screen color overlay that fades from alpha to 0 over duration seconds.</summary>
        public static void ScreenFlash(MonoBehaviour owner, Color color,
            float alpha = 0.4f, float duration = 0.35f)
        {
            if (owner == null) return;
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : owner.transform;
            owner.StartCoroutine(ScreenFlashRoutine(parent, color, alpha, duration));
        }

        private static IEnumerator ScreenFlashRoutine(Transform parent, Color color,
            float startAlpha, float duration)
        {
            var go = new GameObject("VFX_ScreenFlash");
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, startAlpha);
            img.raycastTarget = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (img == null) yield break;
                img.color = new Color(color.r, color.g, color.b,
                    Mathf.Lerp(startAlpha, 0f, elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        // ── 7. IdleBob ───────────────────────────────────────────────────────────
        /// <summary>
        /// Gentle sine-wave up/down bob. Returns Coroutine handle for StopCoroutine.
        /// Stop it when the character starts moving; restart when idle again.
        /// </summary>
        public static Coroutine IdleBob(MonoBehaviour owner, Transform target,
            float bobAmount = 8f, float speed = 1.4f)
        {
            if (owner == null || target == null) return null;
            return owner.StartCoroutine(IdleBobRoutine(target, bobAmount, speed));
        }

        private static IEnumerator IdleBobRoutine(Transform target, float bobAmount, float speed)
        {
            Vector3 origin = target.position;
            float time = 0f;
            while (true)
            {
                if (target == null) yield break;
                float offset = Mathf.Sin(time * speed * Mathf.PI * 2f) * bobAmount;
                target.position = new Vector3(origin.x, origin.y + offset, origin.z);
                time += Time.deltaTime;
                yield return null;
            }
        }

        // ── 8. TimerUrgencyUpdate ────────────────────────────────────────────────
        /// <summary>
        /// Recolors a timer bar fill Image based on remaining fraction (0–1).
        /// Green > 50%, yellow 50%–25%, red below 25%.
        /// Call each frame from the timer loop.
        /// </summary>
        public static void TimerUrgencyUpdate(Image fillImage, float fraction)
        {
            if (fillImage == null) return;
            Color green  = new Color(0.412f, 0.941f, 0.682f);
            Color yellow = new Color(1f, 0.922f, 0.231f);
            Color red    = new Color(0.957f, 0.263f, 0.212f);

            Color target;
            if      (fraction > 0.5f)  target = Color.Lerp(yellow, green,  (fraction - 0.5f)  / 0.5f);
            else if (fraction > 0.25f) target = Color.Lerp(red,    yellow, (fraction - 0.25f) / 0.25f);
            else                       target = red;

            fillImage.color = target;
        }
    }
}
