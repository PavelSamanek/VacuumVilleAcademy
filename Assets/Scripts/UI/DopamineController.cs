using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.UI
{
    /// <summary>
    /// Central hub for all psychology-driven engagement mechanics.
    ///
    /// Streak tiers: Normal → Warm(3) → Hot(5) → Fire(8) → Unstoppable(12)
    ///   Each tier escalates colour, particle count, and audio.
    ///
    /// Variable ratio lucky bonus: fires every 5–8 correct answers (random window).
    ///   Unpredictable timing is 3× more engaging than a fixed schedule.
    ///
    /// Comeback: correct after wrong on the same problem → "COMEBACK!" banner.
    ///   Turns frustration into a triumph loop and prevents quit behaviour.
    ///
    /// Anticipation stagger: answer buttons bounce in one by one, creating a
    ///   0.25 s "just out of reach" window that maximises the dopamine anticipation peak.
    ///
    /// Near-completion: with one task left the progress bar pulses gold and
    ///   "SO CLOSE!" floats up — the brain cannot leave an incomplete loop.
    /// </summary>
    public class DopamineController : MonoBehaviour
    {
        // ── Streak tier definition ───────────────────────────────────────────────
        public enum StreakTier { Normal = 0, Warm = 1, Hot = 2, Fire = 3, Unstoppable = 4 }

        private static readonly Color[] TierColors =
        {
            new Color(0.13f, 0.59f, 0.95f),   // Normal       — blue
            new Color(1.00f, 0.85f, 0.10f),   // Warm   (≥3)  — gold
            new Color(1.00f, 0.55f, 0.10f),   // Hot    (≥5)  — orange
            new Color(1.00f, 0.22f, 0.02f),   // Fire   (≥8)  — red
            new Color(0.62f, 0.10f, 1.00f),   // Unstoppable (≥12) — purple
        };

        public static StreakTier GetTier(int streak)
        {
            if (streak >= 12) return StreakTier.Unstoppable;
            if (streak >=  8) return StreakTier.Fire;
            if (streak >=  5) return StreakTier.Hot;
            if (streak >=  3) return StreakTier.Warm;
            return StreakTier.Normal;
        }

        public static Color ColorForTier(StreakTier tier)
            => TierColors[(int)tier];

        // ── Variable ratio lucky trigger ─────────────────────────────────────────
        // Fires exactly once in every randomly-chosen 5–8 correct-answer window.
        // Variable ratio schedules are the most compulsive reward patterns known.

        private int _correctsSinceLucky = 0;
        private int _luckyTarget;

        private void Awake() => ResetLuckyTarget();

        private void ResetLuckyTarget() => _luckyTarget = UnityEngine.Random.Range(5, 9);

        /// Returns true when the lucky bonus should fire.
        /// Only counts first-attempt correct answers (clean wins deserve the reward).
        public bool TryLucky(bool firstAttempt)
        {
            if (!firstAttempt) return false;
            _correctsSinceLucky++;
            if (_correctsSinceLucky >= _luckyTarget)
            {
                _correctsSinceLucky = 0;
                ResetLuckyTarget();
                return true;
            }
            return false;
        }

        // ── Comeback detection ───────────────────────────────────────────────────

        private bool _hadWrongThisProblem = false;

        public void RecordWrong() => _hadWrongThisProblem = true;
        public void ResetProblem() => _hadWrongThisProblem = false;

        /// Call when correct — returns true once if wrong was recorded this problem.
        public bool ConsumeComeback()
        {
            bool comeback = _hadWrongThisProblem;
            _hadWrongThisProblem = false;
            return comeback;
        }

        // ── Button stagger-in ────────────────────────────────────────────────────
        // Buttons pop in one at a time with elastic overshoot.
        // The ~0.25 s wait before tapping is the dopamine anticipation peak.

        public void BeginButtonStagger(Button[] buttons, Action onComplete)
        {
            StartCoroutine(StaggerRoutine(buttons, onComplete));
        }

        private IEnumerator StaggerRoutine(Button[] buttons, Action onComplete)
        {
            // Hide all first
            foreach (var btn in buttons)
                if (btn != null) btn.transform.localScale = Vector3.zero;

            yield return null; // let hide propagate

            float staggerDelay = 0.085f;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    StartCoroutine(ElasticPopIn(buttons[i].transform));
                if (i < buttons.Length - 1)
                    yield return new WaitForSeconds(staggerDelay);
            }

            // Wait for the last pop to reach scale 1
            yield return new WaitForSeconds(0.3f);
            onComplete?.Invoke();
        }

        private IEnumerator ElasticPopIn(Transform t)
        {
            float dur = 0.32f, elapsed = 0f;
            while (elapsed < dur)
            {
                float s = EaseOutElastic(elapsed / dur);
                if (t != null) t.localScale = Vector3.one * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        // ── Streak milestone banner ──────────────────────────────────────────────

        public void ShowStreakMilestone(int streak, Canvas canvas, Vector2 center)
        {
            if (canvas == null) return;
            StreakTier tier = GetTier(streak);
            if (tier == StreakTier.Normal) return;
            StartCoroutine(StreakMilestoneRoutine(streak, tier, canvas, center));
        }

        private IEnumerator StreakMilestoneRoutine(int streak, StreakTier tier, Canvas canvas, Vector2 center)
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) yield break;

            string key = tier switch
            {
                StreakTier.Warm        => "streak_warm",
                StreakTier.Hot         => "streak_hot",
                StreakTier.Fire        => "streak_fire",
                StreakTier.Unstoppable => "streak_unstoppable",
                _                     => ""
            };
            if (string.IsNullOrEmpty(key)) yield break;

            string text = loc.Get(key, streak);
            Color  col  = ColorForTier(tier);
            float  sz   = tier >= StreakTier.Fire ? 82f : tier == StreakTier.Hot ? 70f : 60f;

            // Glow rings for Fire/Unstoppable
            if (tier >= StreakTier.Fire)
                for (int r = 0; r < 3; r++)
                    StartCoroutine(GlowRing(canvas, center, col, r * 0.10f));

            var go  = MakeText(canvas, text, sz, col, center, new Vector2(520f, 120f));
            var rt  = go.GetComponent<RectTransform>();
            var tmp = go.GetComponent<TextMeshProUGUI>();
            go.transform.localScale = Vector3.zero;

            float elapsed = 0f, duration = 1.6f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration && go != null)
            {
                float p = elapsed / duration;
                go.transform.localScale = Vector3.one * EaseOutElastic(Mathf.Clamp01(p / 0.22f));

                float ease = 1f - (1f - p) * (1f - p);
                rt.anchoredPosition = startPos + new Vector2(0f, 200f * ease);

                float alpha = p < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.65f) / 0.35f);

                if (tier == StreakTier.Unstoppable)
                {
                    float hue = (Time.time * 1.2f) % 1f;
                    var c = Color.HSVToRGB(hue, 0.9f, 1f);
                    tmp.color = new Color(c.r, c.g, c.b, alpha);
                }
                else
                    tmp.color = new Color(col.r, col.g, col.b, alpha);

                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go) Destroy(go);
        }

        // ── Lucky bonus ──────────────────────────────────────────────────────────
        // Rainbow rain + giant gold text — complete surprise, maximum dopamine spike.

        public void ShowLuckyBonus(Canvas canvas, RectTransform canvasRt)
        {
            if (canvas == null) return;
            StartCoroutine(LuckyBonusRoutine(canvas, canvasRt));
        }

        private IEnumerator LuckyBonusRoutine(Canvas canvas, RectTransform canvasRt)
        {
            var loc = LocalizationManager.Instance;
            string label = loc != null ? loc.Get("feedback_lucky") : "LUCKY BONUS!";

            float halfW = canvasRt != null ? canvasRt.rect.width  * 0.5f : 400f;
            float halfH = canvasRt != null ? canvasRt.rect.height * 0.5f : 700f;

            // Rainbow particle rain from top
            var star = Resources.Load<Sprite>("Sprites/star_particle");
            for (int i = 0; i < 90; i++)
            {
                float rx = UnityEngine.Random.Range(-halfW, halfW);
                float ry = halfH + 20f;
                SpawnRainParticle(canvas, star, new Vector2(rx, ry));
            }

            // Big bouncing text
            var go  = MakeText(canvas, label, 90f, new Color(1f, 0.88f, 0.1f),
                               new Vector2(0f, halfH * 0.3f), new Vector2(560f, 130f));
            var rt  = go.GetComponent<RectTransform>();
            var tmp = go.GetComponent<TextMeshProUGUI>();
            go.transform.localScale = Vector3.zero;

            // Screen flash gold
            StartCoroutine(ScreenFlash(canvas, new Color(1f, 0.9f, 0.1f, 0.45f), 0.5f));

            float elapsed = 0f, duration = 2.2f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration && go != null)
            {
                float p = elapsed / duration;
                go.transform.localScale = Vector3.one * EaseOutElastic(Mathf.Clamp01(p / 0.18f));

                float hue = (Time.time * 1.4f) % 1f;
                var c = Color.HSVToRGB(hue, 0.9f, 1f);
                float alpha = p < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.6f) / 0.4f);
                tmp.color = new Color(c.r, c.g, c.b, alpha);

                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go) Destroy(go);
        }

        // ── Comeback banner ──────────────────────────────────────────────────────

        public void ShowComeback(Canvas canvas, Vector2 center)
        {
            if (canvas == null) return;
            StartCoroutine(ComebackRoutine(canvas, center));
        }

        private IEnumerator ComebackRoutine(Canvas canvas, Vector2 center)
        {
            var loc = LocalizationManager.Instance;
            string label = loc != null ? loc.Get("feedback_comeback") : "COMEBACK!";

            var go  = MakeText(canvas, label, 74f, new Color(1f, 0.84f, 0.1f),
                               center + new Vector2(0f, 70f), new Vector2(460f, 110f));
            var rt  = go.GetComponent<RectTransform>();
            var tmp = go.GetComponent<TextMeshProUGUI>();
            go.transform.localScale = Vector3.zero;

            float elapsed = 0f, duration = 1.3f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration && go != null)
            {
                float p = elapsed / duration;
                go.transform.localScale = Vector3.one * EaseOutElastic(Mathf.Clamp01(p / 0.20f));
                float ease = 1f - (1f - p) * (1f - p);
                rt.anchoredPosition = startPos + new Vector2(0f, 110f * ease);
                float alpha = p < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.5f) / 0.5f);
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go) Destroy(go);
        }

        // ── Near-completion urgency ──────────────────────────────────────────────
        // One task left: progress bar pulses gold + "SO CLOSE!" floats up.
        // The Zeigarnik effect — incomplete loops pull the brain forward.

        public void UpdateProgressUrgency(int done, int required, Slider bar,
                                          Canvas canvas, Vector2 barCenter)
        {
            if (required - done == 1)
                StartCoroutine(SoCloseRoutine(bar, canvas, barCenter));
        }

        private IEnumerator SoCloseRoutine(Slider bar, Canvas canvas, Vector2 center)
        {
            // Pulse bar fill gold
            Image fill = bar?.fillRect?.GetComponent<Image>();
            if (fill != null)
                StartCoroutine(PulseImageColor(fill, new Color(1f, 0.88f, 0.1f), 1.8f));

            var loc = LocalizationManager.Instance;
            string label = loc != null ? loc.Get("feedback_so_close") : "So close!";

            var go  = MakeText(canvas, label, 50f, new Color(1f, 0.88f, 0.1f),
                               center + new Vector2(0f, 36f), new Vector2(380f, 76f));
            var rt  = go.GetComponent<RectTransform>();
            var tmp = go.GetComponent<TextMeshProUGUI>();
            go.transform.localScale = Vector3.zero;

            float elapsed = 0f, duration = 1.1f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration && go != null)
            {
                float p = elapsed / duration;
                go.transform.localScale = Vector3.one * EaseOutElastic(Mathf.Clamp01(p / 0.22f));
                float ease = 1f - (1f - p) * (1f - p);
                rt.anchoredPosition = startPos + new Vector2(0f, 80f * ease);
                float alpha = p < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.5f) / 0.5f);
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go) Destroy(go);
        }

        // ── Progress segment filled ──────────────────────────────────────────────

        public void ProgressSegmentFilled(Canvas canvas, Vector2 barRightEdge)
        {
            StartCoroutine(FillBurstRoutine(canvas, barRightEdge));
        }

        private IEnumerator FillBurstRoutine(Canvas canvas, Vector2 pos)
        {
            var circle = Resources.Load<Sprite>("Sprites/circle");
            for (int i = 0; i < 14; i++)
            {
                var go  = new GameObject("PF");
                go.transform.SetParent(canvas.transform, false);
                go.transform.SetAsLastSibling();
                float sz  = UnityEngine.Random.Range(5f, 15f);
                var rt    = go.AddComponent<RectTransform>();
                rt.sizeDelta        = new Vector2(sz, sz);
                rt.anchoredPosition = pos;
                var img   = go.AddComponent<Image>();
                img.sprite = circle; img.raycastTarget = false;
                img.color  = Color.HSVToRGB(UnityEngine.Random.Range(0.10f, 0.18f), 0.95f, 1f);
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(90f, 220f);
                Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                StartCoroutine(FlyAndFade(go, rt, img, vel, UnityEngine.Random.Range(0.3f, 0.6f)));
            }
            yield break;
        }

        // ── Home-screen Play button attract ─────────────────────────────────────
        // A breathing scale pulse on the Play button — subconsciously draws the eye.

        public void StartPlayButtonAttract(Transform target)
        {
            if (target != null) StartCoroutine(BreathingPulse(target));
        }

        private IEnumerator BreathingPulse(Transform t)
        {
            Vector3 original = t.localScale;
            float time = 0f;
            while (t != null)
            {
                float pulse = 1f + Mathf.Sin(time * 1.85f * Mathf.PI) * 0.06f;
                t.localScale = original * pulse;
                time += Time.deltaTime;
                yield return null;
            }
        }

        // ── Shared VFX helpers ───────────────────────────────────────────────────

        private IEnumerator GlowRing(Canvas canvas, Vector2 center, Color color, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            var go  = new GameObject("VFX_GR");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110f, 110f); rt.anchoredPosition = center;
            var img = go.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("Sprites/circle");
            img.color  = color; img.raycastTarget = false;

            float e = 0f, dur = 0.7f;
            while (e < dur && go != null)
            {
                float p = e / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 4.2f, p);
                img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.75f, 0f, p));
                e += Time.deltaTime; yield return null;
            }
            if (go) Destroy(go);
        }

        private IEnumerator ScreenFlash(Canvas canvas, Color color, float dur)
        {
            var go  = new GameObject("VFX_SF");
            go.transform.SetParent(canvas.transform, false);
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

        private void SpawnRainParticle(Canvas canvas, Sprite sprite, Vector2 startPos)
        {
            var go  = new GameObject("LR");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            float sz  = UnityEngine.Random.Range(10f, 26f);
            var rt    = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(sz, sz);
            rt.anchoredPosition = startPos;
            var img   = go.AddComponent<Image>();
            img.sprite = sprite; img.raycastTarget = false;
            img.color  = Color.HSVToRGB(UnityEngine.Random.value, 0.9f, 1f);
            float vx  = UnityEngine.Random.Range(-90f, 90f);
            float vy  = UnityEngine.Random.Range(-250f, -850f);
            float spin = UnityEngine.Random.Range(-360f, 360f);
            float life = UnityEngine.Random.Range(0.9f, 2.0f);
            StartCoroutine(FlyAndFade(go, rt, img, new Vector2(vx, vy), life, spin, -620f));
        }

        private IEnumerator FlyAndFade(GameObject go, RectTransform rt, Image img,
                                       Vector2 vel, float lifetime,
                                       float spin = 0f, float gravity = -700f)
        {
            float e = 0f;
            Vector2 pos = rt.anchoredPosition;
            Color c = img.color;
            while (e < lifetime && go != null)
            {
                float dt = Time.deltaTime;
                vel.y  += gravity * dt;
                pos    += vel * dt;
                rt.anchoredPosition = pos;
                if (spin != 0f) rt.localRotation = Quaternion.Euler(0f, 0f, spin * e);
                img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, e / lifetime));
                e += dt; yield return null;
            }
            if (go) Destroy(go);
        }

        private IEnumerator PulseImageColor(Image img, Color target, float duration)
        {
            if (img == null) yield break;
            Color orig = img.color;
            float e = 0f;
            while (e < duration && img != null)
            {
                float t = (Mathf.Sin(e * 4f * Mathf.PI) + 1f) * 0.5f;
                img.color = Color.Lerp(orig, target, t * 0.75f);
                e += Time.deltaTime; yield return null;
            }
            if (img != null) img.color = orig;
        }

        private static GameObject MakeText(Canvas canvas, string text, float fontSize,
                                           Color color, Vector2 pos, Vector2 size)
        {
            var go  = new GameObject("VFX_T");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text              = text;
            tmp.fontSize          = fontSize;
            tmp.fontStyle         = FontStyles.Bold;
            tmp.color             = color;
            tmp.alignment         = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget     = false;
            return go;
        }

        // ── Easing ───────────────────────────────────────────────────────────────

        private static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }
}
