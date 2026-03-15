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
