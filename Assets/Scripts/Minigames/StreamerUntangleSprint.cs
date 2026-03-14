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

        protected override void OnMinigameBegin()
        {
            missesLabel.text = $"✗ 0/{MaxMisses}";
            StartCoroutine(RunLoop());
        }

        private IEnumerator RunLoop()
        {
            while (GameActive && _knotsResolved < totalKnots && _misses < MaxMisses)
            {
                vacuumAnimator?.SetBool("Running", true);
                yield return new WaitForSeconds(Random.Range(1f, 2f)); // running between knots
                vacuumAnimator?.SetBool("Running", false);
                yield return StartCoroutine(KnotEncounter());
            }
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
                knotTimerBar.value = 1f - (elapsed / knotTimerDuration);
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
                vacuumAnimator?.SetTrigger("Bump");
                yield return new WaitForSeconds(0.5f);
            }

            SetKnotPanelVisible(false);
        }

        private void GenerateKnotProblem()
        {
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
                knotAnswerLabels[i].text = val.ToString();
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
                vacuumAnimator?.SetTrigger("Cheer");
            }
            else
            {
                _misses++;
                UpdateMissesLabel();
                AudioManager.Instance.PlayWrong();
                vacuumAnimator?.SetTrigger("Oops");

                if (_misses >= MaxMisses) CompleteEarly();
            }
        }

        private void UpdateMissesLabel()
        {
            if (missesLabel)
                missesLabel.text = $"✗ {_misses}/{MaxMisses}";
        }

        private void SetKnotPanelVisible(bool visible)
        {
            knotProblemText?.transform.parent.gameObject.SetActive(visible);
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
