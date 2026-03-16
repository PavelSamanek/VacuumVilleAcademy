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
    /// UI is built entirely in code so layout is correct on all screen sizes.
    /// </summary>
    public class ScramblerShutdown : BaseMinigame
    {
        [Header("Scrambler (optional — UI built in code)")]
        [SerializeField] private Animator scramblerAnimator;
        [SerializeField] private ParticleSystem shutdownParticles;
        [SerializeField] private float panelTimeLimit = 30f;

        private int _currentPanelIndex;
        private bool _panelActive;

        // Runtime-built panel data
        private PanelRuntime[] _runtimePanels;

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

        // ── Runtime panel struct (built in code) ────────────────────────────────
        private class PanelRuntime
        {
            public Image       background;
            public TextMeshProUGUI problemLabel;
            public Button[]    answerButtons  = new Button[3];
            public TextMeshProUGUI[] answerLabels = new TextMeshProUGUI[3];
            public Slider      timerSlider;
            public GameObject  solvedOverlay;
            public TextMeshProUGUI topicLabel;
        }

        protected override bool IsSetupComplete() => true; // always starts; UI built in OnMinigameBegin

        // ── Layout ───────────────────────────────────────────────────────────────

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            BuildUI();
            StartCoroutine(PanelSequence());
        }

        private void BuildUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Hide old scene panel GameObjects (Panel_Addition, Panel_Subtraction, etc.)
            // These were placed in the Inspector but have incorrect sizing/colors.
            for (int c = 0; c < canvas.transform.childCount; c++)
            {
                var child = canvas.transform.GetChild(c);
                if (child.name.StartsWith("Panel_"))
                    child.gameObject.SetActive(false);
            }

            // ── Header ────────────────────────────────────────────────────────
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(canvas.transform, false);
            SetAnchors((RectTransform)headerGo.transform, 0f, 0.88f, 1f, 1f);
            var headerBg = headerGo.AddComponent<Image>();
            headerBg.color = new Color(0.06f, 0.05f, 0.18f, 0.95f);

            var titleTmp = MakeTMP(headerGo.transform, "TitleLabel");
            SetAnchors((RectTransform)titleTmp.transform, 0f, 0f, 1f, 1f);
            titleTmp.text      = LocalizationManager.Instance?.Get("minigame_instruction_scramblershutdown") ?? "Scrambler Shutdown";
            titleTmp.fontSize  = 28f;
            titleTmp.color     = new Color(0.6f, 0.85f, 1f);
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.enableWordWrapping = true;

            // ── Panels row container ──────────────────────────────────────────
            var rowGo = new GameObject("PanelsRow", typeof(RectTransform));
            rowGo.transform.SetParent(canvas.transform, false);
            SetAnchors((RectTransform)rowGo.transform, 0.01f, 0.10f, 0.99f, 0.87f);

            // ── Build 5 panels ────────────────────────────────────────────────
            _runtimePanels = new PanelRuntime[5];
            float panelW = 1f / 5f;
            float gap    = 0.005f;

            for (int i = 0; i < 5; i++)
            {
                float xMin = i * panelW + gap;
                float xMax = (i + 1) * panelW - gap;
                _runtimePanels[i] = BuildPanel(rowGo.transform, i, xMin, xMax);
            }
        }

        private PanelRuntime BuildPanel(Transform parent, int index, float xMin, float xMax)
        {
            var p = new PanelRuntime();

            // Panel root
            var go = new GameObject($"Panel_{index}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            SetAnchors(rt, xMin, 0f, xMax, 1f);

            // Background
            p.background = go.AddComponent<Image>();
            p.background.color = new Color(0.12f, 0.14f, 0.32f, 1f); // dark navy

            // Topic label (small, top strip)
            p.topicLabel = MakeTMP(go.transform, "TopicLabel");
            SetAnchors((RectTransform)p.topicLabel.transform, 0f, 0.87f, 1f, 1f);
            p.topicLabel.text      = TopicShortName(PanelTopics[index]);
            p.topicLabel.fontSize  = 20f;
            p.topicLabel.color     = new Color(0.5f, 0.7f, 1f);
            p.topicLabel.fontStyle = FontStyles.Bold;
            p.topicLabel.alignment = TextAlignmentOptions.Center;

            // Problem label
            p.problemLabel = MakeTMP(go.transform, "ProblemLabel");
            SetAnchors((RectTransform)p.problemLabel.transform, 0.04f, 0.57f, 0.96f, 0.86f);
            p.problemLabel.text      = "?";
            p.problemLabel.fontSize  = 38f;
            p.problemLabel.color     = Color.white;
            p.problemLabel.fontStyle = FontStyles.Bold;
            p.problemLabel.alignment = TextAlignmentOptions.Center;
            p.problemLabel.enableWordWrapping = true;
            p.problemLabel.enableAutoSizing = true;
            p.problemLabel.fontSizeMin = 18f;
            p.problemLabel.fontSizeMax = 44f;

            // Timer slider
            p.timerSlider = BuildSlider(go.transform, 0f, 0.53f, 1f, 0.57f);

            // 3 answer buttons stacked in bottom 50%
            float btnH = 0.155f;
            float[] btnY = { 0.36f, 0.20f, 0.04f };
            for (int b = 0; b < 3; b++)
            {
                var btnGo = new GameObject($"Btn_{b}", typeof(RectTransform));
                btnGo.transform.SetParent(go.transform, false);
                SetAnchors((RectTransform)btnGo.transform, 0.05f, btnY[b], 0.95f, btnY[b] + btnH);

                var img = btnGo.AddComponent<Image>();
                img.color = new Color(0.22f, 0.25f, 0.45f);

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;

                var lbl = MakeTMP(btnGo.transform, "Label");
                SetAnchors((RectTransform)lbl.transform, 0f, 0f, 1f, 1f);
                lbl.text      = "-";
                lbl.fontSize  = 36f;
                lbl.fontStyle = FontStyles.Bold;
                lbl.color     = Color.white;
                lbl.alignment = TextAlignmentOptions.Center;

                p.answerButtons[b] = btn;
                p.answerLabels[b]  = lbl;
            }

            // Solved overlay
            var overlayGo = new GameObject("SolvedOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(go.transform, false);
            SetAnchors((RectTransform)overlayGo.transform, 0f, 0f, 1f, 1f);
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0.41f, 0.94f, 0.67f, 0.35f);
            // Checkmark label
            var checkTmp = MakeTMP(overlayGo.transform, "Check");
            SetAnchors((RectTransform)checkTmp.transform, 0f, 0.4f, 1f, 0.65f);
            checkTmp.text      = "OK";
            checkTmp.fontSize  = 64f;
            checkTmp.color     = new Color(0.41f, 0.94f, 0.67f);
            checkTmp.fontStyle = FontStyles.Bold;
            checkTmp.alignment = TextAlignmentOptions.Center;
            overlayGo.SetActive(false);
            p.solvedOverlay = overlayGo;

            return p;
        }

        // ── Panel Sequence ───────────────────────────────────────────────────────

        private IEnumerator PanelSequence()
        {
            for (int i = 0; i < _runtimePanels.Length; i++)
            {
                _currentPanelIndex = i;
                yield return StartCoroutine(RunPanel(_runtimePanels[i], PanelTopics[i]));
            }

            // All panels — shutdown!
            if (shutdownParticles) shutdownParticles.Play();
            if (scramblerAnimator != null) scramblerAnimator.SetTrigger("Shutdown");
            AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/shutdown");
            AudioManager.Instance.PlayLevelComplete();
            MinigameVFX.ScreenFlash(this, new Color(0.412f, 0.941f, 0.682f), 0.5f, 0.6f);
            yield return new WaitForSeconds(1.5f);
            CompleteEarly();
        }

        private IEnumerator RunPanel(PanelRuntime panel, MathTopic topic)
        {
            var gm = GameManager.Instance;
            var ta = gm.Progress.GetOrCreateTopicAccuracy(topic);
            var problem = MathTaskGenerator.Generate(topic, ta);

            // Highlight active panel
            panel.background.color = new Color(0.14f, 0.18f, 0.40f, 1f);

            // Display problem
            if (panel.problemLabel != null)
                panel.problemLabel.text = string.IsNullOrEmpty(problem.equationText)
                    ? problem.questionTextFallback ?? LocalizationManager.Instance.Get(problem.questionTextKey)
                    : problem.equationText;

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
                btn.gameObject.SetActive(true);
                btn.interactable = true;

                var lbl = panel.answerLabels[i];
                if (lbl != null) { lbl.text = val.ToString(); lbl.color = Color.white; }

                var img = btn.GetComponent<Image>();
                if (img) img.color = new Color(0.22f, 0.25f, 0.45f);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (!_panelActive) return;
                    answered = true;
                    _panelActive = false;
                    bool isCorrect = val == correct;
                    HandlePanelAnswer(panel, btn, isCorrect, topic);
                });
            }

            // Timer countdown
            _panelActive = true;
            float elapsed = 0f;
            while (elapsed < panelTimeLimit && !answered)
            {
                elapsed += Time.deltaTime;
                if (panel.timerSlider)
                    panel.timerSlider.value = 1f - (elapsed / panelTimeLimit);
                yield return null;
            }

            if (!answered)
            {
                _panelActive = false;
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/panel_wrong");
                if (scramblerAnimator != null) scramblerAnimator.SetTrigger("Laugh");
                panel.background.color = new Color(0.28f, 0.10f, 0.10f, 1f); // dim red = timed out
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                yield return new WaitForSeconds(0.6f);
            }

            // Restore normal background for non-active panel
            if (!panel.solvedOverlay || !panel.solvedOverlay.activeSelf)
                panel.background.color = new Color(0.10f, 0.11f, 0.24f, 1f);
        }

        private void HandlePanelAnswer(PanelRuntime panel, Button pressedButton, bool correct, MathTopic topic)
        {
            var ta = GameManager.Instance.Progress.GetOrCreateTopicAccuracy(topic);
            ta.RecordResult(correct);

            var img = pressedButton.GetComponent<Image>();

            if (correct)
            {
                if (img) img.color = new Color(0.41f, 0.94f, 0.67f);
                if (panel.solvedOverlay) panel.solvedOverlay.SetActive(true);
                panel.background.color = new Color(0.12f, 0.28f, 0.20f, 1f); // dark green tint

                AddScore(1);
                AudioManager.Instance.PlayCorrect();
                AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/panel_solve");
                if (scramblerAnimator != null) scramblerAnimator.SetTrigger("Shocked");
                MinigameVFX.PulseRing(this, pressedButton.transform.position, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.FloatingText(this, "+1", pressedButton.transform.position, new Color(0.412f, 0.941f, 0.682f));
            }
            else
            {
                if (img) img.color = new Color(1f, 0.57f, 0f);
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/scrambler/panel_wrong");
                if (scramblerAnimator != null) scramblerAnimator.SetTrigger("Laugh");
                MinigameVFX.ShakeRect(this, (RectTransform)pressedButton.transform);
            }
        }

        // ── UI helpers ───────────────────────────────────────────────────────────

        private static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI MakeTMP(Transform parent, string name)
        {
            var go  = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Slider BuildSlider(Transform parent, float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject("TimerSlider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetAnchors((RectTransform)go.transform, xMin, yMin, xMax, yMax);

            var bg = new GameObject("BG", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            SetAnchors((RectTransform)bg.transform, 0f, 0.2f, 1f, 0.8f);
            bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.3f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRt = (RectTransform)fillArea.transform;
            SetAnchors(faRt, 0f, 0.2f, 1f, 0.8f);
            faRt.offsetMin = new Vector2(4f, 0f);
            faRt.offsetMax = new Vector2(-4f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = (RectTransform)fill.transform;
            SetAnchors(fillRt, 0f, 0f, 1f, 1f);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.70f, 0.95f);

            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            SetAnchors((RectTransform)handleArea.transform, 0f, 0f, 1f, 1f);

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            ((RectTransform)handle.transform).sizeDelta = new Vector2(0f, 0f); // invisible handle
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.clear;

            var slider = go.AddComponent<Slider>();
            slider.fillRect      = fillRt;
            slider.handleRect    = (RectTransform)handle.transform;
            slider.targetGraphic = handleImg;
            slider.minValue      = 0f;
            slider.maxValue      = 1f;
            slider.value         = 1f;
            slider.interactable  = false;
            return slider;
        }

        private static string TopicShortName(MathTopic topic) => topic switch
        {
            MathTopic.AdditionTo20        => "+",
            MathTopic.SubtractionWithin20 => "−",
            MathTopic.Multiplication2x5x  => "×",
            MathTopic.DivisionBy2_3_5     => "÷",
            MathTopic.NumberOrdering      => "123",
            _                             => "?"
        };
    }
}
