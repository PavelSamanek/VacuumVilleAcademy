using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    /// <summary>
    /// PIN-gated parent dashboard. Shows time played, per-topic accuracy,
    /// star counts. Allows: time limits, sound toggles, difficulty reset.
    /// PIN challenge: hold the number "7" button for 3 seconds.
    /// </summary>
    public class ParentDashboardController : MonoBehaviour
    {
        [Header("PIN Gate")]
        [SerializeField] private GameObject pinGatePanel;
        [SerializeField] private Button pinHoldButton;
        [SerializeField] private Slider pinHoldProgress;
        [SerializeField] private float pinHoldDuration = 3f;

        [Header("Dashboard")]
        [SerializeField] private GameObject dashboardPanel;
        [SerializeField] private TextMeshProUGUI totalTimeLabel;
        [SerializeField] private TextMeshProUGUI totalStarsLabel;
        [SerializeField] private TextMeshProUGUI coinsLabel;
        [SerializeField] private TopicAccuracyRow[] topicRows;

        [Header("Settings within Dashboard")]
        [SerializeField] private Slider timeLimitSlider;   // 10–60 min per session
        [SerializeField] private TextMeshProUGUI timeLimitLabel;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle voiceToggle;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private Button closeButton;

        private float _holdTimer;
        private bool _holding;

        [System.Serializable]
        public class TopicAccuracyRow
        {
            public MathTopic topic;
            public TextMeshProUGUI topicLabel;
            public TextMeshProUGUI accuracyLabel;
            public Slider accuracyBar;
            public Image badgeImage;
        }

        private void Start()
        {
            pinGatePanel.SetActive(true);
            dashboardPanel.SetActive(false);

            pinHoldButton.GetComponent<EventTriggerHelper>()?.onPointerDown.AddListener(OnPinHoldStart);
            pinHoldButton.GetComponent<EventTriggerHelper>()?.onPointerUp.AddListener(OnPinHoldEnd);

            timeLimitSlider.onValueChanged.AddListener(OnTimeLimitChanged);
            musicToggle.onValueChanged.AddListener(v =>
                AudioManager.Instance.MusicVolume = v ? 0.7f : 0f);
            sfxToggle.onValueChanged.AddListener(v =>
                AudioManager.Instance.SfxVolume = v ? 1f : 0f);
            voiceToggle.onValueChanged.AddListener(v =>
                AudioManager.Instance.VoiceVolume = v ? 1f : 0f);

            resetProgressButton.onClick.AddListener(OnResetProgress);
            closeButton.onClick.AddListener(() =>
                GameManager.Instance.TransitionTo(GameState.Home));
        }

        private void Update()
        {
            if (!_holding) return;

            _holdTimer += Time.deltaTime;
            pinHoldProgress.value = _holdTimer / pinHoldDuration;

            if (_holdTimer >= pinHoldDuration)
            {
                _holding = false;
                UnlockDashboard();
            }
        }

        private void OnPinHoldStart() { _holding = true; _holdTimer = 0f; }
        private void OnPinHoldEnd()   { _holding = false; _holdTimer = 0f; pinHoldProgress.value = 0f; }

        private void UnlockDashboard()
        {
            pinGatePanel.SetActive(false);
            dashboardPanel.SetActive(true);
            PopulateDashboard();
        }

        private void PopulateDashboard()
        {
            var progress = GameManager.Instance.Progress;

            totalTimeLabel.text  = LocalizationManager.Instance.GetPlural(
                "minutes_played", progress.totalMinutesPlayed);
            totalStarsLabel.text = LocalizationManager.Instance.GetPlural(
                "stars_total", progress.totalStars);
            coinsLabel.text      = progress.coins.ToString();

            timeLimitSlider.value = PlayerPrefs.GetFloat("session_time_limit", 20f);
            musicToggle.isOn      = AudioManager.Instance.MusicVolume > 0f;
            sfxToggle.isOn        = AudioManager.Instance.SfxVolume   > 0f;
            voiceToggle.isOn      = AudioManager.Instance.VoiceVolume  > 0f;

            foreach (var row in topicRows)
            {
                var ta = progress.GetOrCreateTopicAccuracy(row.topic);
                float accuracy = ta.totalProblems > 0
                    ? (float)ta.correctFirstAttempt / ta.totalProblems * 100f
                    : 0f;

                row.topicLabel.text   = LocalizationManager.Instance.Get($"topic_{row.topic}");
                row.accuracyLabel.text = $"{accuracy:F0}%  ({ta.totalProblems} {LocalizationManager.Instance.Get("problems")})";
                row.accuracyBar.value  = accuracy / 100f;

                // Badge
                if (row.badgeImage != null)
                {
                    string badgeKey = accuracy >= 90 ? "badge_gold"
                        : accuracy >= 75 ? "badge_silver"
                        : accuracy >= 60 ? "badge_bronze" : "";
                    if (!string.IsNullOrEmpty(badgeKey))
                    {
                        row.badgeImage.sprite = Resources.Load<Sprite>($"Sprites/{badgeKey}");
                        row.badgeImage.gameObject.SetActive(true);
                    }
                    else row.badgeImage.gameObject.SetActive(false);
                }
            }
        }

        private void OnTimeLimitChanged(float value)
        {
            int mins = Mathf.RoundToInt(value);
            timeLimitLabel.text = LocalizationManager.Instance.GetPlural("minutes_played", mins);
            PlayerPrefs.SetFloat("session_time_limit", mins);
        }

        private void OnResetProgress()
        {
            // Show confirmation first
            StartCoroutine(ConfirmReset());
        }

        private IEnumerator ConfirmReset()
        {
            // Simple: disable button for 3 seconds as safety — real confirm dialog would use a prefab
            resetProgressButton.interactable = false;
            var lbl = resetProgressButton.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = LocalizationManager.Instance.Get("confirm_reset_countdown");
            yield return new WaitForSeconds(3f);
            SaveSystem.Delete();
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
