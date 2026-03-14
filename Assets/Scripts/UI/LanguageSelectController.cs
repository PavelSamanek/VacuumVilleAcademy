using UnityEngine;
using UnityEngine.UI;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    /// <summary>
    /// First screen shown on launch. Two large flag buttons (Czech / English).
    /// Czech is pre-highlighted. Selection is saved and persisted.
    /// </summary>
    public class LanguageSelectController : MonoBehaviour
    {
        [SerializeField] private Button czechButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Image czechHighlight;
        [SerializeField] private Image englishHighlight;

        private Language _selected = Language.Czech;

        private void Start()
        {
            // Pre-highlight previously chosen language, but always require the user to confirm
            var saved = (Language)PlayerPrefs.GetInt("lang", (int)Language.Czech);
            _selected = saved;
            SetHighlight(saved);

            czechButton.onClick.AddListener(() => SelectLanguage(Language.Czech));
            englishButton.onClick.AddListener(() => SelectLanguage(Language.English));
        }

        private void SelectLanguage(Language lang)
        {
            _selected = lang;
            SetHighlight(lang);
            AudioManager.Instance.PlayButton();
            ApplyLanguage(lang);
        }

        private void ApplyLanguage(Language lang)
        {
            LocalizationManager.Instance.SetLanguage(lang);
            GameManager.Instance.Progress.selectedLanguage = lang;
            PlayerPrefs.SetInt("lang", (int)lang);
            PlayerPrefs.SetString("language_chosen", "1");
            PlayerPrefs.Save();

            GameManager.Instance.TransitionTo(GameState.Home);
        }

        private void SetHighlight(Language lang)
        {
            czechHighlight.gameObject.SetActive(lang == Language.Czech);
            englishHighlight.gameObject.SetActive(lang == Language.English);
        }
    }
}
