using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.UI
{
    public class LevelCompleteController : MonoBehaviour
    {
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFilled;
        [SerializeField] private Sprite starEmpty;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI coinsEarnedText;
        [SerializeField] private TextMeshProUGUI stickerPopup;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Animator confettiAnimator;

        private void Start()
        {
            var gm  = GameManager.Instance;
            var lp  = gm.Progress.GetOrCreateLevel(gm.ActiveLevel.levelIndex);

            titleText.text  = LocalizationManager.Instance.Get("level_complete_title");
            titleText.color = Color.white;
            if (coinsEarnedText != null) coinsEarnedText.color = Color.white;
            if (stickerPopup    != null) stickerPopup.color    = new Color(1f, 0.9f, 0.4f); // gold

            // Set button labels via localization (strips any unsupported icon characters)
            SetButtonLabel(replayButton,    "btn_replay");
            SetButtonLabel(homeButton,      "btn_home");
            SetButtonLabel(nextLevelButton, "btn_next");

            // Generate star sprites procedurally if not assigned in Inspector
            if (starFilled == null) starFilled = CreateStarSprite(filled: true);
            if (starEmpty  == null) starEmpty  = CreateStarSprite(filled: false);

            AudioManager.Instance.PlayLevelComplete();
            confettiAnimator?.SetTrigger("Play");

            StartCoroutine(AnimateStars(lp.stars));
            StartCoroutine(ShowStarRatingMessage(lp.stars));
            StartCoroutine(PulseNextButtonDelayed());

            int coins = 5 + lp.stars * 10;
            gm.Progress.coins += coins;
            if (coinsEarnedText != null)
                coinsEarnedText.text = LocalizationManager.Instance.GetPlural("coins_earned", coins);

            if (lp.stickerCollected)
                stickerPopup.text = LocalizationManager.Instance.Get(
                    "sticker_collected", LocalizationManager.Instance.Get(gm.ActiveLevel.roomNameKey));

            bool nextExists = gm.GetLevel(gm.ActiveLevel.levelIndex + 1) != null;
            nextLevelButton.gameObject.SetActive(nextExists);
            nextLevelButton.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayButton();
                var next = gm.GetLevel(gm.ActiveLevel.levelIndex + 1);
                if (next != null) gm.StartLevel(next);
            });

            replayButton.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayButton();
                gm.StartLevel(gm.ActiveLevel);
            });

            homeButton.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayButton();
                gm.TransitionTo(Data.GameState.LevelSelect);
            });
        }

        // ── Star-rating message ──────────────────────────────────────────────────
        // Shows a tier message after the stars settle — gives children a clear
        // social-proof signal that validates their effort.

        private IEnumerator ShowStarRatingMessage(int stars)
        {
            yield return new WaitForSeconds(0.9f);  // wait for stars to finish popping

            string msg = stars switch
            {
                3 => LocalizationManager.Instance.Get("feedback_new_best"),
                2 => LocalizationManager.Instance.Get("feedback_well_done"),
                _ => LocalizationManager.Instance.Get("feedback_good_start")
            };

            Color col = stars == 3
                ? new Color(1f, 0.85f, 0.1f)   // gold
                : stars == 2
                    ? new Color(0.75f, 0.75f, 0.75f)  // silver
                    : new Color(0.80f, 0.55f, 0.2f);  // bronze

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            var go  = new GameObject("VFX_StarMsg");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(520f, 110f);
            rt.anchoredPosition = new Vector2(0f, -80f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text              = msg;
            tmp.fontSize          = stars == 3 ? 72f : 58f;
            tmp.fontStyle         = FontStyles.Bold;
            tmp.color             = col;
            tmp.alignment         = TextAlignmentOptions.Center;
            tmp.raycastTarget     = false;
            go.transform.localScale = Vector3.zero;

            float elapsed = 0f, duration = 2.2f;
            const float c4 = (2f * UnityEngine.Mathf.PI) / 3f;

            while (elapsed < duration && go != null)
            {
                float p  = elapsed / duration;
                float sp = Mathf.Clamp01(p / 0.22f);
                float s  = sp >= 1f ? 1f
                    : Mathf.Pow(2f, -10f * sp) * Mathf.Sin((sp * 10f - 0.75f) * c4) + 1f;
                go.transform.localScale = Vector3.one * s;

                if (stars == 3)
                {
                    float hue = (Time.time * 0.9f) % 1f;
                    var  rc   = Color.HSVToRGB(hue, 0.8f, 1f);
                    tmp.color = new Color(rc.r, rc.g, rc.b,
                                    p < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.7f) / 0.3f));
                }
                else
                {
                    float alpha = p < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.7f) / 0.3f);
                    tmp.color   = new Color(col.r, col.g, col.b, alpha);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go) Destroy(go);
        }

        // ── Next-level button pulse ──────────────────────────────────────────────
        // Starts pulsing after 1.5 s — Zeigarnik pull toward the next level.

        private IEnumerator PulseNextButtonDelayed()
        {
            if (nextLevelButton == null) yield break;
            yield return new WaitForSeconds(1.5f);

            var rt  = nextLevelButton.GetComponent<RectTransform>();
            if (rt == null) yield break;

            float time = 0f;
            Vector3 orig = rt.localScale;
            while (nextLevelButton != null)
            {
                float pulse = 1f + Mathf.Sin(time * 2.2f * Mathf.PI) * 0.07f;
                rt.localScale = orig * pulse;
                time += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator AnimateStars(int count)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                starImages[i].sprite = starEmpty;
                starImages[i].transform.localScale = Vector3.zero;
            }

            yield return new WaitForSeconds(0.4f);

            for (int i = 0; i < starImages.Length; i++)
            {
                starImages[i].sprite = i < count ? starFilled : starEmpty;
                yield return StartCoroutine(PopScale(starImages[i].transform));
                yield return new WaitForSeconds(0.15f);
            }
        }

        private IEnumerator PopScale(Transform t)
        {
            float duration = 0.3f;
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                float s = Mathf.LerpUnclamped(0f, 1f,
                    EaseOutBack(elapsed / duration));
                t.localScale = Vector3.one * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static void SetButtonLabel(Button btn, string locKey)
        {
            if (btn == null) return;
            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.text = LocalizationManager.Instance.Get(locKey);
        }

        /// <summary>Generates a 5-pointed star sprite procedurally.</summary>
        private static Sprite CreateStarSprite(bool filled)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            Color32 starColor = filled
                ? new Color32(255, 210, 20, 255)   // gold
                : new Color32(180, 180, 180, 255);  // gray
            Color32 transparent = new Color32(0, 0, 0, 0);

            float cx = size / 2f, cy = size / 2f;
            float r1 = size / 2f - 4f;   // outer radius
            float r2 = r1 * 0.42f;        // inner radius
            float sectorRad = Mathf.PI / 5f; // 36° per sector (10 sectors total)

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                bool inside = false;
                if (dist <= r1 + 1f)
                {
                    // Angle measured clockwise from top
                    float angle = Mathf.Repeat(Mathf.PI / 2f - Mathf.Atan2(dy, dx), 2f * Mathf.PI);
                    int sector = (int)(angle / sectorRad);
                    float frac = (angle - sector * sectorRad) / sectorRad;
                    // Even sectors: tip → inner corner; Odd: inner corner → tip
                    float boundary = (sector % 2 == 0)
                        ? Mathf.Lerp(r1, r2, frac)
                        : Mathf.Lerp(r2, r1, frac);
                    inside = dist <= boundary;
                }

                pixels[y * size + x] = inside ? starColor : transparent;
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
