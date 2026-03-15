using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 6 Minigame: Zara auto-runs down a hallway.
    /// At each streamer knot, player solves a subtraction problem before the timer depletes.
    /// Missing 3 knots ends the run early.
    /// </summary>
    public class StreamerUntangleSprint : BaseMinigame
    {
        [Header("Sprint")]
        [SerializeField] private Slider knotTimerBar;
        [SerializeField] private float knotTimerDuration = 5f;
        [SerializeField] private TextMeshProUGUI knotProblemText;
        [SerializeField] private Button[] knotAnswerButtons;
        [SerializeField] private TextMeshProUGUI[] knotAnswerLabels;
        [SerializeField] private int totalKnots = 15;
        [SerializeField] private Animator vacuumAnimator;
        [SerializeField] private TextMeshProUGUI missesLabel;

        private int _knotsResolved;
        private int _misses;
        private const int MaxMisses = 3;
        private bool _atKnot;
        private int _correctAnswer;

        protected override float TimeLimit => 200f;
        protected override int MaxScore   => 15;

        protected override bool IsSetupComplete() =>
            knotAnswerButtons != null && knotAnswerButtons.Length > 0;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");

            // Auto-create HUD labels if the scene doesn't have them wired up
            if (missesLabel   == null) missesLabel   = CreateHUDLabel("MissesLabel",   new Vector2(0,  340), 42f);
            if (knotProblemText == null) knotProblemText = CreateHUDLabel("KnotProblem", new Vector2(0,  140), 72f);

            if (knotAnswerButtons != null && knotAnswerButtons.Length < 3)
                EnsureThirdAnswerButton();

            // Ensure answer buttons start hidden until first knot
            SetKnotPanelVisible(false);

            missesLabel.text = $"X 0/{MaxMisses}";
            StartCoroutine(RunLoop());
        }

        private TextMeshProUGUI CreateHUDLabel(string name, Vector2 anchoredPos, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(600, 80);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            // White for equation text, red for misses label
            tmp.color = name == "KnotProblem" ? Color.white : new Color(1f, 0.35f, 0.35f);
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void EnsureThirdAnswerButton()
        {
            // Build a 3rd answer button programmatically next to the existing two
            if (knotAnswerButtons == null || knotAnswerButtons.Length == 0) return;
            var template = knotAnswerButtons[knotAnswerButtons.Length - 1];
            var newBtn = Instantiate(template.gameObject, template.transform.parent);
            newBtn.name = "AnswerBtn_3";
            var rt = newBtn.GetComponent<RectTransform>();
            var templateRt = template.GetComponent<RectTransform>();
            float spacing = templateRt.sizeDelta.x + 20f;
            rt.anchoredPosition = templateRt.anchoredPosition + new Vector2(spacing, 0);

            var newBtnComp = newBtn.GetComponent<Button>();
            var newLbl = newBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Grow arrays
            var btns = new System.Collections.Generic.List<Button>(knotAnswerButtons) { newBtnComp };
            var lbls = new System.Collections.Generic.List<TextMeshProUGUI>(knotAnswerLabels) { newLbl };
            knotAnswerButtons = btns.ToArray();
            knotAnswerLabels = lbls.ToArray();
        }

        private IEnumerator RunLoop()
        {
            while (GameActive && _knotsResolved < totalKnots && _misses < MaxMisses)
            {
                if (vacuumAnimator != null) vacuumAnimator.SetBool("Running", true);
                AudioManager.Instance?.PlaySFX("Audio/SFX/streamer/running");
                yield return new WaitForSeconds(Random.Range(1f, 2f)); // running between knots
                if (vacuumAnimator != null) vacuumAnimator.SetBool("Running", false);
                yield return StartCoroutine(KnotEncounter());
            }
            if (_knotsResolved >= totalKnots)
                AudioManager.Instance?.PlaySFX("Audio/SFX/streamer/streamer_free");
            CompleteEarly();
        }

        private IEnumerator KnotEncounter()
        {
            _atKnot = true;
            GenerateKnotProblem();

            float elapsed = 0f;
            bool answered = false;

            while (elapsed < knotTimerDuration && !answered)
            {
                float frac = 1f - (elapsed / knotTimerDuration);
                if (knotTimerBar != null)
                {
                    knotTimerBar.value = frac;
                    MinigameVFX.TimerUrgencyUpdate(
                        knotTimerBar.fillRect != null ? knotTimerBar.fillRect.GetComponent<Image>() : null,
                        frac);
                }
                elapsed += Time.deltaTime;
                yield return null;

                if (!_atKnot) { answered = true; }
            }

            if (!answered)
            {
                // Timeout = miss
                _atKnot = false;
                _misses++;
                UpdateMissesLabel();
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/streamer/bump");
                if (vacuumAnimator != null) vacuumAnimator.SetTrigger("Bump");
                yield return new WaitForSeconds(0.5f);
            }

            SetKnotPanelVisible(false);
        }

        private void GenerateKnotProblem()
        {
            // Reposition scene-placed buttons to on-screen bottom strip
            if (knotAnswerButtons != null)
            {
                float[] xMin = { 0.02f, 0.36f, 0.70f };
                float[] xMax = { 0.32f, 0.66f, 0.98f };
                for (int i = 0; i < knotAnswerButtons.Length && i < xMin.Length; i++)
                {
                    if (knotAnswerButtons[i] == null) continue;
                    var rt = (RectTransform)knotAnswerButtons[i].transform;
                    if (rt.parent != transform) rt.SetParent(transform, false);
                    rt.anchorMin = new Vector2(xMin[i], 0.02f);
                    rt.anchorMax = new Vector2(xMax[i], 0.13f);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                }
            }

            int maxNum = 20;
            int a = Random.Range(5, maxNum + 1);
            int b = Random.Range(1, a);
            _correctAnswer = a - b;

            knotProblemText.text = $"{a} - {b} = ?";
            AudioManager.Instance.PlayVoice($"q_subtraction_{a}_{b}");

            var choices = GenerateChoices(_correctAnswer, 0, maxNum);
            for (int i = 0; i < knotAnswerButtons.Length; i++)
            {
                int val = choices[i];
                knotAnswerButtons[i].gameObject.SetActive(true);
                knotAnswerLabels[i].text = val.ToString();
                knotAnswerLabels[i].color = new Color(0.1f, 0.1f, 0.2f);
                knotAnswerButtons[i].onClick.RemoveAllListeners();
                knotAnswerButtons[i].onClick.AddListener(() => OnKnotAnswer(val));
                knotAnswerButtons[i].interactable = true;
                var img = knotAnswerButtons[i].GetComponent<Image>();
                if (img) img.color = Color.white;
            }

            SetKnotPanelVisible(true);
        }

        private void OnKnotAnswer(int answer)
        {
            if (!_atKnot) return;
            _atKnot = false;

            if (answer == _correctAnswer)
            {
                _knotsResolved++;
                AddScore(1);
                AudioManager.Instance.PlayCorrect();
                AudioManager.Instance?.PlaySFX("Audio/SFX/streamer/knot_snap");
                if (vacuumAnimator != null) vacuumAnimator.SetTrigger("Cheer");
                Vector3 knotPos = knotProblemText != null ? knotProblemText.transform.position : transform.position;
                MinigameVFX.PulseRing(this, knotPos, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.FloatingText(this, "+1", knotPos, new Color(0.412f, 0.941f, 0.682f));
            }
            else
            {
                _misses++;
                UpdateMissesLabel();
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/streamer/bump");
                if (vacuumAnimator != null) vacuumAnimator.SetTrigger("Oops");
                if (missesLabel != null) MinigameVFX.ShakeRect(this, (RectTransform)missesLabel.transform);

                if (_misses >= MaxMisses) CompleteEarly();
            }
        }

        private void UpdateMissesLabel()
        {
            if (missesLabel)
                missesLabel.text = $"X {_misses}/{MaxMisses}";
        }

        private void SetKnotPanelVisible(bool visible)
        {
            // Toggle the question text (or its panel, if it sits inside one)
            if (knotProblemText != null)
            {
                var textParent = knotProblemText.transform.parent;
                if (textParent != null && textParent != transform)
                    textParent.gameObject.SetActive(visible);
                else
                    knotProblemText.gameObject.SetActive(visible);
            }

            if (knotAnswerButtons == null) return;

            // Also activate the button panel if buttons share one that isn't the root
            if (knotAnswerButtons.Length > 0 && knotAnswerButtons[0] != null)
            {
                var btnPanel = knotAnswerButtons[0].transform.parent;
                if (btnPanel != null && btnPanel != transform)
                    btnPanel.gameObject.SetActive(visible);
            }

            // Toggle each button individually
            foreach (var btn in knotAnswerButtons)
                if (btn != null) btn.gameObject.SetActive(visible);
        }

        private static int[] GenerateChoices(int correct, int min, int max)
        {
            var set = new HashSet<int> { correct };
            int attempts = 0;
            while (set.Count < 3 && attempts++ < 30)
            {
                int w = correct + (Random.Range(0, 2) == 0 ? 1 : -1) * Random.Range(1, 4);
                if (w >= min && w <= max && w != correct) set.Add(w);
            }
            int pad = min;
            while (set.Count < 3) { if (!set.Contains(pad)) set.Add(pad); pad++; }
            var arr = new List<int>(set);
            for (int i = arr.Count - 1; i > 0; i--)
            { int j = Random.Range(0, i + 1); (arr[i], arr[j]) = (arr[j], arr[i]); }
            return arr.ToArray();
        }
    }
}
