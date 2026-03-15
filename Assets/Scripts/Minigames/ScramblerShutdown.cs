using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;
using VacuumVille.Math;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 11 (Secret Lab) Boss Minigame:
    /// 5 panels, each with a different math topic. Player has 30 seconds per panel.
    /// Solving all 5 shuts down the Scrambler machine.
    /// Difficulty adapts to child's personal performance history.
    /// </summary>
    public class ScramblerShutdown : BaseMinigame
    {
        [Header("Scrambler")]
        [SerializeField] private PanelUI[] panels;          // exactly 5
        [SerializeField] private Animator scramblerAnimator;
        [SerializeField] private ParticleSystem shutdownParticles;
        [SerializeField] private float panelTimeLimit = 30f;

        private int _currentPanelIndex;
        private bool _panelActive;

        private static readonly MathTopic[] PanelTopics =
        {
            MathTopic.AdditionTo20,
            MathTopic.SubtractionWithin20,
            MathTopic.Multiplication2x5x,
            MathTopic.DivisionBy2_3_5,
            MathTopic.NumberOrdering
        };

        protected override float TimeLimit => 999f; // managed per panel
        protected override int MaxScore   => 5;

        [System.Serializable]
        public class PanelUI
        {
            public MathTopic topic;
            public Image panelBackground;
            public TextMeshProUGUI problemLabel;
            public Button[] answerButtons;
            public TextMeshProUGUI[] answerLabels;
            public Slider panelTimer;
            public Image solvedOverlay;
        }

        protected override bool IsSetupComplete() =>
            panels != null && panels.Length > 0;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            foreach (var p in panels)
                if (p.solvedOverlay) p.solvedOverlay.gameObject.SetActive(false);

            StartCoroutine(PanelSequence());
        }

        private IEnumerator PanelSequence()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                _currentPanelIndex = i;
                yield return StartCoroutine(RunPanel(panels[i], PanelTopics[i]));
            }

            // All panels solved — shutdown!
            if (shutdownParticles) shutdownParticles.Play();
            scramblerAnimator?.SetTrigger("Shutdown");
            AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/shutdown");
            AudioManager.Instance.PlayLevelComplete();
            MinigameVFX.ScreenFlash(this, new Color(0.412f, 0.941f, 0.682f), 0.5f, 0.6f);
            yield return new WaitForSeconds(1.5f);
            CompleteEarly();
        }

        private IEnumerator RunPanel(PanelUI panel, MathTopic topic)
        {
            var gm = GameManager.Instance;
            var ta = gm.Progress.GetOrCreateTopicAccuracy(topic);
            var problem = MathTaskGenerator.Generate(topic, ta);

            // Display problem
            panel.problemLabel.text = string.IsNullOrEmpty(problem.questionTextFallback)
                ? LocalizationManager.Instance.Get(problem.questionTextKey)
                : problem.questionTextFallback;

            AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/panel_hum");
            AudioManager.Instance.PlayVoice(problem.voiceLineKey);

            // Setup answer buttons
            int correct = problem.correctAnswer;
            var choices = problem.choices;
            bool answered = false;

            for (int i = 0; i < panel.answerButtons.Length && i < choices.Length; i++)
            {
                int val = choices[i];
                var btn = panel.answerButtons[i];
                panel.answerLabels[i].text = val.ToString();
                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (!_panelActive) return;
                    answered = true;
                    _panelActive = false;
                    bool correct2 = val == correct;
                    HandlePanelAnswer(panel, btn, correct2, topic);
                });
                var img = btn.GetComponent<Image>();
                if (img) img.color = Color.white;
            }

            // Timer countdown
            _panelActive = true;
            float elapsed = 0f;
            while (elapsed < panelTimeLimit && !answered)
            {
                elapsed += Time.deltaTime;
                if (panel.panelTimer)
                    panel.panelTimer.value = 1f - (elapsed / panelTimeLimit);
                yield return null;
            }

            if (!answered)
            {
                // Timeout - panel stays unsolved but continue
                _panelActive = false;
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/panel_wrong");
                scramblerAnimator?.SetTrigger("Laugh");
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                yield return new WaitForSeconds(0.6f);
            }
        }

        private void HandlePanelAnswer(PanelUI panel, Button pressedButton, bool correct, MathTopic topic)
        {
            var gm = GameManager.Instance;
            var ta = gm.Progress.GetOrCreateTopicAccuracy(topic);
            ta.RecordResult(correct);

            var img = pressedButton.GetComponent<Image>();

            if (correct)
            {
                if (img) img.color = new Color(0.41f, 0.94f, 0.67f);
                if (panel.solvedOverlay) panel.solvedOverlay.gameObject.SetActive(true);
                if (panel.panelBackground) panel.panelBackground.color = new Color(0.41f, 0.94f, 0.67f, 0.3f);

                AddScore(1);
                AudioManager.Instance.PlayCorrect();
                AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/panel_solve");
                scramblerAnimator?.SetTrigger("Shocked");
                MinigameVFX.PulseRing(this, pressedButton.transform.position, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.FloatingText(this, "+1", pressedButton.transform.position, new Color(0.412f, 0.941f, 0.682f));
            }
            else
            {
                if (img) img.color = new Color(1f, 0.57f, 0f);
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/panel_wrong");
                scramblerAnimator?.SetTrigger("Laugh");
                MinigameVFX.ShakeRect(this, (RectTransform)pressedButton.transform);
            }
        }
    }
}
