using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;
using VacuumVille.Math;

namespace VacuumVille.UI
{
    /// <summary>
    /// Controls the math task screen. Generates problems, displays them,
    /// handles answer input, feedback animations, hint system, and progress bar.
    /// </summary>
    public class TaskDisplayController : MonoBehaviour
    {
        [Header("Problem Display")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private GameObject visualObjectContainer;
        [SerializeField] private Image visualObjectPrefab;
        [SerializeField] private TextMeshProUGUI operandAText;
        [SerializeField] private TextMeshProUGUI operatorText;
        [SerializeField] private TextMeshProUGUI operandBText;

        [Header("Answer Choices")]
        [SerializeField] private Button[] choiceButtons;        // exactly 3
        [SerializeField] private TextMeshProUGUI[] choiceLabels;

        [Header("Progress")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressLabel;

        [Header("Feedback")]
        [SerializeField] private GameObject correctParticles;
        [SerializeField] private GameObject wrongShake;
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Hint")]
        [SerializeField] private Button hintButton;
        [SerializeField] private TextMeshProUGUI hintCountLabel;

        [Header("Streak")]
        [SerializeField] private GameObject streakIndicator;
        [SerializeField] private TextMeshProUGUI streakLabel;

        [Header("Professor Pebble Re-teach")]
        [SerializeField] private GameObject reteachPanel;
        [SerializeField] private TextMeshProUGUI reteachText;

        // State
        private MathProblem _current;
        private int _attemptCount;
        private int _hintsRemaining = 3;
        private int _streak;
        private bool _inputLocked;
        private int _tasksInSession;
        private int _firstAttemptCorrectInSession;

        // Wrong answer exclusion for hint
        private readonly HashSet<int> _excludedChoices = new();

        private static readonly Color ColorDefault  = Color.white;
        private static readonly Color ColorCorrect  = new Color(0.41f, 0.94f, 0.67f);  // #69F0AE
        private static readonly Color ColorWrong    = new Color(1f,   0.57f, 0f);      // #FF9100

        private void Start()
        {
            UpdateProgressBar();
            GenerateNext();
        }

        // ── Problem Flow ────────────────────────────────────────────────────────

        private void GenerateNext()
        {
            if (_inputLocked) return;

            _excludedChoices.Clear();
            _attemptCount = 0;
            _inputLocked  = false;

            var gm  = GameManager.Instance;
            var ta  = gm.Progress.GetOrCreateTopicAccuracy(gm.ActiveLevel.mathTopic);
            _current = MathTaskGenerator.Generate(gm.ActiveLevel.mathTopic, ta);

            DisplayProblem(_current);
            AudioManager.Instance.PlayVoice(_current.voiceLineKey);
        }

        private void DisplayProblem(MathProblem p)
        {
            // Question text
            string qText = p.questionTextFallback;
            if (!string.IsNullOrEmpty(p.questionTextKey))
                qText = LocalizationManager.Instance.Get(p.questionTextKey);
            questionText.text = qText;

            // Operator display
            if (p.operands != null && p.operands.Length >= 2)
            {
                operandAText.text  = p.operands[0].ToString();
                operatorText.text  = p.operatorSymbol;
                operandBText.text  = p.operands[1].ToString();
            }

            // Visual objects
            BuildVisualObjects(p);

            // Choices
            ResetChoiceButtons();
            for (int i = 0; i < choiceButtons.Length && i < p.choices.Length; i++)
            {
                int answer = p.choices[i];
                int idx    = i;
                choiceLabels[i].text = answer.ToString();
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceTapped(idx, answer));
                choiceButtons[i].interactable = true;
                SetButtonColor(i, ColorDefault);
            }

            feedbackText.text = "";
        }

        private void BuildVisualObjects(MathProblem p)
        {
            foreach (Transform child in visualObjectContainer.transform)
                Destroy(child.gameObject);

            if (p.visualCount <= 0 || string.IsNullOrEmpty(p.visualAssetKey)) return;

            Sprite sprite = Resources.Load<Sprite>($"Sprites/{p.visualAssetKey}");
            if (sprite == null) return;

            for (int i = 0; i < p.visualCount; i++)
            {
                var img = Instantiate(visualObjectPrefab, visualObjectContainer.transform);
                img.sprite = sprite;
            }
        }

        // ── Answer Handling ─────────────────────────────────────────────────────

        private void OnChoiceTapped(int buttonIndex, int answer)
        {
            if (_inputLocked) return;

            bool correct = answer == _current.correctAnswer;
            _attemptCount++;

            if (correct)
            {
                HandleCorrect(buttonIndex);
            }
            else
            {
                _excludedChoices.Add(buttonIndex);
                HandleWrong(buttonIndex);
                if (_attemptCount >= 3) RevealAnswer();
            }
        }

        private void HandleCorrect(int buttonIndex)
        {
            _inputLocked = true;
            bool firstAttempt = _attemptCount == 1;
            SetButtonColor(buttonIndex, ColorCorrect);

            AudioManager.Instance.PlayCorrect();
            if (correctParticles) correctParticles.SetActive(true);
            characterAnimator?.SetTrigger("Cheer");

            _streak++;
            UpdateStreakDisplay();

            _tasksInSession++;
            if (firstAttempt) _firstAttemptCorrectInSession++;

            GameManager.Instance.RecordTaskResult(firstAttempt);
            UpdateProgressBar();
            CheckAdaptiveDifficulty();

            StartCoroutine(NextAfterDelay(0.8f));
        }

        private void HandleWrong(int buttonIndex)
        {
            SetButtonColor(buttonIndex, ColorWrong);
            choiceButtons[buttonIndex].interactable = false;

            AudioManager.Instance.PlayWrong();
            StartCoroutine(ShakeButton(choiceButtons[buttonIndex].transform));

            _streak = 0;
            UpdateStreakDisplay();

            feedbackText.text = LocalizationManager.Instance.Get("feedback_try_again");
        }

        private void RevealAnswer()
        {
            _inputLocked = true;
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (int.TryParse(choiceLabels[i].text, out int val) && val == _current.correctAnswer)
                    SetButtonColor(i, ColorCorrect);
            }
            feedbackText.text = LocalizationManager.Instance.Get("feedback_correct_was",
                _current.correctAnswer);

            _tasksInSession++;
            GameManager.Instance.RecordTaskResult(false);
            UpdateProgressBar();
            StartCoroutine(NextAfterDelay(1.5f));
        }

        // ── Hint System ─────────────────────────────────────────────────────────

        public void OnHintTapped()
        {
            if (_hintsRemaining <= 0 || _inputLocked) return;
            if (_excludedChoices.Count >= choiceButtons.Length - 1) return;

            // Disable one wrong answer that hasn't been excluded yet
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (_excludedChoices.Contains(i)) continue;
                if (int.TryParse(choiceLabels[i].text, out int val) && val != _current.correctAnswer)
                {
                    _excludedChoices.Add(i);
                    choiceButtons[i].interactable = false;
                    SetButtonColor(i, new Color(0.8f, 0.8f, 0.8f));
                    break;
                }
            }

            _hintsRemaining--;
            hintCountLabel.text = _hintsRemaining.ToString();
            hintButton.interactable = _hintsRemaining > 0;
        }

        // ── UI Helpers ──────────────────────────────────────────────────────────

        private void UpdateProgressBar()
        {
            var gm = GameManager.Instance;
            float ratio = (float)gm.TasksCompletedThisSession / gm.ActiveLevel.tasksRequiredToUnlockMinigame;
            progressBar.value = Mathf.Clamp01(ratio);
            progressLabel.text = $"{gm.TasksCompletedThisSession} / {gm.ActiveLevel.tasksRequiredToUnlockMinigame}";
        }

        private void UpdateStreakDisplay()
        {
            streakIndicator.SetActive(_streak >= 5);
            if (_streak >= 5)
                streakLabel.text = LocalizationManager.Instance.Get("streak_label", _streak);
        }

        private void ResetChoiceButtons()
        {
            foreach (var btn in choiceButtons)
            {
                btn.interactable = true;
                SetButtonColor(Array.IndexOf(choiceButtons, btn), ColorDefault);
            }
        }

        private void SetButtonColor(int index, Color color)
        {
            var img = choiceButtons[index].GetComponent<Image>();
            if (img) img.color = color;
        }

        private void CheckAdaptiveDifficulty()
        {
            var gm = GameManager.Instance;
            var ta = gm.Progress.GetOrCreateTopicAccuracy(gm.ActiveLevel.mathTopic);
            if (ta.RollingAccuracy < 0.55f && ta.totalProblems >= 10)
            {
                reteachPanel.SetActive(true);
                reteachText.text = LocalizationManager.Instance.Get(
                    $"reteach_{gm.ActiveLevel.mathTopic}");
                AudioManager.Instance.PlayVoice("professor_reteach");
            }
        }

        // ── Coroutines ──────────────────────────────────────────────────────────

        private IEnumerator NextAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (correctParticles) correctParticles.SetActive(false);
            _inputLocked = false;
            GenerateNext();
        }

        private IEnumerator ShakeButton(Transform t)
        {
            Vector3 origin = t.localPosition;
            float duration = 0.3f;
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                float x = Mathf.Sin(elapsed * 60f) * 8f;
                t.localPosition = origin + new Vector3(x, 0, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localPosition = origin;
        }

        // Fix for Array.IndexOf in Unity (System.Array)
        private static int IndexOf<T>(T[] array, T item)
        {
            for (int i = 0; i < array.Length; i++)
                if (EqualityComparer<T>.Default.Equals(array[i], item)) return i;
            return -1;
        }
    }
}
