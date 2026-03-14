using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.UI
{
    /// <summary>
    /// Monitors continuous play time. After the configured limit (default 20 min),
    /// the companion character yawns and suggests a break.
    /// Not forced — child can choose to keep playing.
    /// </summary>
    public class SessionBreakMonitor : MonoBehaviour
    {
        [SerializeField] private GameObject breakPanel;
        [SerializeField] private TextMeshProUGUI breakPromptLabel;
        [SerializeField] private Button keepPlayingButton;
        [SerializeField] private Button takeBreakButton;
        [SerializeField] private Animator companionAnimator;

        private float _sessionSeconds;
        private bool _breakShown;

        private float SessionLimitSeconds
            => PlayerPrefs.GetFloat("session_time_limit", 20f) * 60f;

        private void Start()
        {
            if (breakPanel == null || keepPlayingButton == null || takeBreakButton == null)
            {
                Debug.LogWarning("[SessionBreakMonitor] One or more UI references are not assigned in the Inspector. Component disabled.");
                enabled = false;
                return;
            }
            breakPanel.SetActive(false);
            keepPlayingButton.onClick.AddListener(OnKeepPlaying);
            takeBreakButton.onClick.AddListener(OnTakeBreak);
        }

        private void Update()
        {
            if (_breakShown) return;

            _sessionSeconds += Time.deltaTime;
            if (_sessionSeconds >= SessionLimitSeconds)
                ShowBreakSuggestion();
        }

        private void ShowBreakSuggestion()
        {
            _breakShown = true;
            breakPanel.SetActive(true);
            breakPromptLabel.text = LocalizationManager.Instance.Get("session_break_prompt");
            companionAnimator?.SetTrigger("Yawn");
            AudioManager.Instance.PlayVoice("companion_break_suggestion");
        }

        private void OnKeepPlaying()
        {
            breakPanel.SetActive(false);
            _sessionSeconds = 0f;   // reset timer
            _breakShown     = false;
            AudioManager.Instance.PlayButton();
        }

        private void OnTakeBreak()
        {
            AudioManager.Instance.PlayButton();
            // Save and go home
            GameManager.Instance.TransitionTo(Data.GameState.Home);
        }
    }
}
