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

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        [Header("Streak")]
        [SerializeField] private GameObject streakIndicator;
        [SerializeField] private TextMeshProUGUI streakLabel;

        [Header("Professor Pebble Re-teach")]
        [SerializeField] private GameObject reteachPanel;
        [SerializeField] private TextMeshProUGUI reteachText;

        // 3D badges — one per choice button, auto-created in Start
        private NumberBadge3D[] _badges;

        // Equation explosion VFX — auto-added in Start
        private EquationExplosionVFX _explosionVFX;

        // Dopamine / engagement system — auto-added in Start
        private DopamineController _dopamine;

        // Canvas helpers (lazily resolved)
        private Canvas        _canvas;
        private RectTransform _canvasRt;
        private Camera        _canvasCam;

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

        private Color _colorDefault = new Color(0.13f, 0.59f, 0.95f); // updated per level theme
        private static readonly Color ColorCorrect  = new Color(0.41f, 0.94f, 0.67f);  // #69F0AE
        private static readonly Color ColorWrong    = new Color(1f,   0.57f, 0f);      // #FF9100

        private void RefreshThemeColor()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.ActiveLevel != null)
                _colorDefault = PersistentBackground.GetButtonColorForTopic(gm.ActiveLevel.mathTopic);
        }

        private void Start()
        {
            RefreshThemeColor();
            if (backButton == null)
            {
                var go = GameObject.Find("BackButton");
                if (go != null) backButton = go.GetComponent<Button>();
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(GoBack);
            }

            // Ensure text is readable on any background
            EnsureTextReadability();

            // Attach 3D badge component to each choice button
            _badges = new NumberBadge3D[choiceButtons.Length];
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                _badges[i] = choiceButtons[i].GetComponent<NumberBadge3D>()
                          ?? choiceButtons[i].gameObject.AddComponent<NumberBadge3D>();
            }

            // Equation explosion VFX
            _explosionVFX = GetComponent<EquationExplosionVFX>()
                         ?? gameObject.AddComponent<EquationExplosionVFX>();

            // Dopamine controller
            _dopamine = GetComponent<DopamineController>()
                     ?? gameObject.AddComponent<DopamineController>();

            UpdateProgressBar();
            GenerateNext();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                GoBack();
        }

        private void GoBack()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.TransitionTo(GameState.LevelIntro);
        }

        // ── Problem Flow ────────────────────────────────────────────────────────

        // ── Canvas helpers ───────────────────────────────────────────────────────

        private Canvas GetCanvas()
        {
            if (_canvas != null) return _canvas;
            _canvas = GetComponentInParent<Canvas>();
            while (_canvas != null && _canvas.transform.parent != null)
            {
                var p = _canvas.transform.parent.GetComponentInParent<Canvas>();
                if (p == null) break;
                _canvas = p;
            }
            if (_canvas != null)
            {
                _canvasRt  = _canvas.GetComponent<RectTransform>();
                _canvasCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                             ? null : _canvas.worldCamera;
            }
            return _canvas;
        }

        private Vector2 ToCanvasPos(RectTransform rt)
        {
            var canvas = GetCanvas();
            if (canvas == null || _canvasRt == null || rt == null) return Vector2.zero;
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(_canvasCam, rt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, sp, _canvasCam, out Vector2 lp);
            return lp;
        }

        // ── Problem Flow ─────────────────────────────────────────────────────────

        private void GenerateNext()
        {
            if (_inputLocked) return;

            RefreshThemeColor();
            _excludedChoices.Clear();
            _attemptCount = 0;
            _inputLocked  = false;
            _dopamine?.ResetProblem();

            var gm  = GameManager.Instance;
            var ta  = gm.Progress.GetOrCreateTopicAccuracy(gm.ActiveLevel.mathTopic);
            _current = MathTaskGenerator.Generate(gm.ActiveLevel.mathTopic, ta);

            DisplayProblem(_current);

            // For numerical equations (addition/subtraction/multiplication/division)
            // voice the specific numbers: "kolik je 2 plus 2" / "what is 2 plus 2"
            bool hasEquation = _current.operands != null
                && _current.operands.Length >= 2
                && !string.IsNullOrEmpty(_current.operatorSymbol)
                && _current.format == ProblemFormat.MultipleChoice;
            if (hasEquation)
                AudioManager.Instance.PlayEquationVoice(_current.operands, _current.operatorSymbol);
            else
                AudioManager.Instance.PlayVoice(_current.voiceLineKey);
        }

        private void DisplayProblem(MathProblem p)
        {
            // Question text
            string qText = p.questionTextFallback;
            if (!string.IsNullOrEmpty(p.questionTextKey))
            {
                // q_count_shapes needs the shape name as {0} argument
                if (p.questionTextKey == "q_count_shapes" && !string.IsNullOrEmpty(p.visualAssetKey))
                {
                    string shapeName = LocalizationManager.Instance.Get($"shape_{p.visualAssetKey}");
                    qText = LocalizationManager.Instance.Get(p.questionTextKey, shapeName);
                }
                else
                {
                    qText = LocalizationManager.Instance.Get(p.questionTextKey);
                }
            }
            questionText.text = qText;

            // Equation display — show equationText as a single string (e.g. "? + 1 = 20", "3,  ?,  7")
            // This keeps the prompt (questionText) and the equation clearly separated.
            if (operandAText) operandAText.text = "";
            if (operatorText) operatorText.text = "";
            if (operandBText) operandBText.text = "";
            if (!string.IsNullOrEmpty(p.equationText))
            {
                if (operandAText)
                {
                    operandAText.enableAutoSizing = false;
                    operandAText.fontSize = choiceLabels.Length > 0 ? choiceLabels[0].fontSize : 72f;
                    operandAText.enableWordWrapping = false;
                    operandAText.overflowMode = TMPro.TextOverflowModes.Overflow;
                    operandAText.text = p.equationText;
                }
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
                if (_badges != null && i < _badges.Length && _badges[i] != null)
                {
                    _badges[i].SetNumber(answer.ToString());
                    _badges[i].SetColor(_colorDefault);
                }
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceTapped(idx, answer));
                choiceButtons[i].interactable = true;
                SetButtonColor(i, _colorDefault);
            }

            feedbackText.text = "";

            // Stagger buttons in for anticipation — locks input until animation completes
            if (_dopamine != null)
            {
                _inputLocked = true;
                _dopamine.BeginButtonStagger(choiceButtons, () => _inputLocked = false);
            }
        }

        private void BuildVisualObjects(MathProblem p)
        {
            foreach (Transform child in visualObjectContainer.transform)
                Destroy(child.gameObject);

            if (p.visualCount <= 0 || string.IsNullOrEmpty(p.visualAssetKey)) return;

            Sprite sprite = Resources.Load<Sprite>($"Sprites/{p.visualAssetKey}");
            if (sprite == null) return;

            // Responsive cell size: fewer items → larger icons
            float cell = p.visualCount <= 5 ? 110f : p.visualCount <= 10 ? 90f : 72f;
            var grid = visualObjectContainer.GetComponent<GridLayoutGroup>()
                    ?? visualObjectContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize       = new Vector2(cell, cell);
            grid.spacing        = new Vector2(8f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint     = GridLayoutGroup.Constraint.Flexible;

            for (int i = 0; i < p.visualCount; i++)
            {
                var img = Instantiate(visualObjectPrefab, visualObjectContainer.transform);
                img.gameObject.SetActive(true);
                img.sprite        = sprite;
                img.preserveAspect = true;
                // Reset any prefab-level scale that may flip the sprite
                img.transform.localScale = Vector3.one;
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

            // Trigger 3D explosion on tapped badge
            if (_badges != null && buttonIndex < _badges.Length && _badges[buttonIndex] != null)
                _badges[buttonIndex].ExplodeCorrect();
            else
                AudioManager.Instance.PlayCorrect();

            // Reveal the solved equation ("5 + 5 = 10") then explode it
            if (_current != null && !string.IsNullOrEmpty(_current.equationText)
                && operandAText != null)
            {
                string solved = _current.equationText.Replace("?", _current.correctAnswer.ToString());
                operandAText.text = solved;
                _explosionVFX?.Explode(operandAText.rectTransform, solved, _streak);
            }

            if (correctParticles) correctParticles.SetActive(true);
            if (characterAnimator != null) characterAnimator.SetTrigger("Cheer");

            // ── Dopamine hooks ────────────────────────────────────────────────
            var canvas = GetCanvas();
            Vector2 eqCenter = ToCanvasPos(operandAText?.rectTransform ?? questionText?.rectTransform);

            // Comeback — correct after wrong on this problem
            if (_dopamine != null && _dopamine.ConsumeComeback() && canvas != null)
                _dopamine.ShowComeback(canvas, eqCenter);

            // Streak milestone — show banner when tier increases
            var oldTier = DopamineController.GetTier(_streak);
            _streak++;
            var newTier = DopamineController.GetTier(_streak);

            if (newTier > oldTier && canvas != null)
            {
                _dopamine?.ShowStreakMilestone(_streak, canvas, eqCenter);
                AudioManager.Instance?.PlayStreakTier(newTier.ToString().ToLower());
            }

            // Lucky bonus — variable ratio surprise reward
            if (_dopamine != null && _dopamine.TryLucky(firstAttempt) && canvas != null)
            {
                _dopamine.ShowLuckyBonus(canvas, _canvasRt);
                AudioManager.Instance?.PlaySFX("Audio/SFX/lucky_bonus");
            }
            // ── End dopamine hooks ────────────────────────────────────────────

            UpdateStreakDisplay();

            _tasksInSession++;
            if (firstAttempt) _firstAttemptCorrectInSession++;

            GameManager.Instance.RecordTaskResult(firstAttempt);
            UpdateProgressBar();
            CheckAdaptiveDifficulty();

            StartCoroutine(NextAfterDelay(1.5f));
        }

        private void HandleWrong(int buttonIndex)
        {
            SetButtonColor(buttonIndex, ColorWrong);
            choiceButtons[buttonIndex].interactable = false;

            AudioManager.Instance.PlayWrong();
            StartCoroutine(ShakeButton(choiceButtons[buttonIndex].transform));
            _dopamine?.RecordWrong();

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
            var gm   = GameManager.Instance;
            int done = gm.TasksCompletedThisSession;
            int req  = gm.ActiveLevel.tasksRequiredToUnlockMinigame;

            float ratio = (float)done / req;
            progressBar.value  = Mathf.Clamp01(ratio);
            progressLabel.text = $"{done} / {req}";

            // Dopamine: near-completion urgency + fill burst
            var canvas = GetCanvas();
            if (canvas != null && _dopamine != null && progressBar != null)
            {
                Vector2 barPos = ToCanvasPos(progressBar.GetComponent<RectTransform>());
                _dopamine.UpdateProgressUrgency(done, req, progressBar, canvas, barPos);
                if (done > 0 && done <= req)
                    _dopamine.ProgressSegmentFilled(canvas, barPos);
            }
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
                SetButtonColor(Array.IndexOf(choiceButtons, btn), _colorDefault);
            }
        }

        private void SetButtonColor(int index, Color color)
        {
            var img = choiceButtons[index].GetComponent<Image>();
            if (img) img.color = color;
            if (_badges != null && index < _badges.Length && _badges[index] != null)
                _badges[index].SetColor(color);
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

        // ── Text readability ─────────────────────────────────────────────────────

        private void EnsureTextReadability()
        {
            // Force white text so it reads clearly on all themed dark backgrounds.
            SetReadable(questionText,  bold: true);
            SetReadable(operandAText,  bold: true);
            SetReadable(operatorText,  bold: true);
            SetReadable(operandBText,  bold: false);
            SetReadable(feedbackText,  bold: false);
            SetReadable(progressLabel, bold: false);
            SetReadable(streakLabel,   bold: true);

            // Add a semi-transparent dark card behind the question area so text
            // is legible on any background without touching TMP materials.
            AddContentCard();
        }

        private void AddContentCard()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Only add once
            if (canvas.transform.Find("ContentCard") != null) return;

            var go = new GameObject("ContentCard", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsFirstSibling();   // behind all content

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0.25f);   // covers top 75 % of screen
            rt.anchorMax        = new Vector2(1f, 1.00f);
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;

            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color           = new Color(0f, 0f, 0f, 0.45f);
            img.raycastTarget   = false;
        }

        private static void SetReadable(TMP_Text t, bool bold)
        {
            if (t == null) return;
            t.color = Color.white;
            if (bold) t.fontStyle |= TMPro.FontStyles.Bold;
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
