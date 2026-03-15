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

            AudioManager.Instance.PlayLevelComplete();
            confettiAnimator?.SetTrigger("Play");

            StartCoroutine(AnimateStars(lp.stars));

            int coins = 5 + lp.stars * 10;
            gm.Progress.coins += coins;
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
    }
}
