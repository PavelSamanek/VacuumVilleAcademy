using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider voiceSlider;
        [SerializeField] private Button czechButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button backButton;

        private void Start()
        {
            var audio = AudioManager.Instance;

            // Sliders — initialise from current volume and wire changes
            if (musicSlider != null)
            {
                musicSlider.value = audio != null ? audio.MusicVolume : PlayerPrefs.GetFloat("vol_music", 0.7f);
                musicSlider.onValueChanged.AddListener(v => { if (audio != null) audio.MusicVolume = v; });
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = audio != null ? audio.SfxVolume : PlayerPrefs.GetFloat("vol_sfx", 1f);
                sfxSlider.onValueChanged.AddListener(v => { if (audio != null) audio.SfxVolume = v; });
            }

            if (voiceSlider != null)
            {
                voiceSlider.value = audio != null ? audio.VoiceVolume : PlayerPrefs.GetFloat("vol_voice", 1f);
                voiceSlider.onValueChanged.AddListener(v => { if (audio != null) audio.VoiceVolume = v; });
            }

            // Language buttons
            if (czechButton != null)
                czechButton.onClick.AddListener(() => SetLanguage(Language.Czech));

            if (englishButton != null)
                englishButton.onClick.AddListener(() => SetLanguage(Language.English));

            // Back button — auto-find by name if not assigned in Inspector
            if (backButton == null)
                backButton = GameObject.Find("BackButton")?.GetComponent<Button>();

            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
            else
                Debug.LogWarning("[SettingsController] BackButton not found — assign it in the Inspector or name the GameObject 'BackButton'.");
        }

        private void SetLanguage(Language lang)
        {
            AudioManager.Instance?.PlayButton();
            PlayerPrefs.SetInt("lang", (int)lang);
            LocalizationManager.Instance?.SetLanguage(lang);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                OnBack();
        }

        private void OnBack()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance.TransitionTo(GameState.Home);
        }
    }
}
