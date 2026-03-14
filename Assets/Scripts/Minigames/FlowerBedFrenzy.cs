using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 7 Minigame: Rows of flowers fall into a garden grid.
    /// Player taps the total count before the next row arrives.
    /// Correct = Turbo waters the flowers. Wrong = wilted effect.
    /// </summary>
    public class FlowerBedFrenzy : BaseMinigame
    {
        [Header("Flower Frenzy")]
        [SerializeField] private GridLayoutGroup gardenGrid;
        [SerializeField] private GameObject flowerCellPrefab;
        [SerializeField] private int gridColumns = 5;
        [SerializeField] private Button[] answerButtons;
        [SerializeField] private TextMeshProUGUI[] answerLabels;
        [SerializeField] private TextMeshProUGUI rowQuestionLabel; // "Kolik květin celkem?"
        [SerializeField] private ParticleSystem waterParticles;
        [SerializeField] private float rowInterval = 3f;

        private int _rowsAnswered;
        private int _totalFlowersInGrid;
        private int _correctAnswer;
        private List<GameObject> _gridCells = new();
        private bool _awaitingAnswer;
        private int _groupSize;
        private int _groupCount;

        protected override float TimeLimit => 120f;
        protected override int MaxScore   => 10;

        protected override void OnMinigameBegin()
        {
            StartCoroutine(RowLoop());
        }

        private IEnumerator RowLoop()
        {
            while (GameActive && _rowsAnswered < 10)
            {
                yield return StartCoroutine(PresentRow());
                yield return new WaitForSeconds(0.5f);
            }
            CompleteEarly();
        }

        private IEnumerator PresentRow()
        {
            // Pick multiplication fact from 2x or 5x table
            int multiplier = Random.Range(0, 2) == 0 ? 2 : 5;
            int groups      = Random.Range(1, 7);
            _groupSize      = multiplier;
            _groupCount     = groups;
            _correctAnswer  = multiplier * groups;

            // Animate flowers falling into grid
            for (int i = 0; i < _correctAnswer; i++)
            {
                var cell = Instantiate(flowerCellPrefab, gardenGrid.transform);
                _gridCells.Add(cell);
                yield return new WaitForSeconds(0.05f);
            }

            // Show question
            rowQuestionLabel.text = LocalizationManager.Instance.Get(
                "q_groups_total", _groupCount, _groupSize);
            AudioManager.Instance.PlayVoice("q_multiplication");

            SetupAnswerButtons();
            _awaitingAnswer = true;

            // Wait for answer or timeout
            float elapsed = 0f;
            while (_awaitingAnswer && elapsed < rowInterval)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_awaitingAnswer)
            {
                // Timeout
                _awaitingAnswer = false;
                ShowWilted();
                AudioManager.Instance.PlayWrong();
                yield return new WaitForSeconds(0.8f);
            }
        }

        private void SetupAnswerButtons()
        {
            var choices = GenerateChoices(_correctAnswer, 2, 50);
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int val = choices[i];
                answerLabels[i].text = val.ToString();
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerTapped(val));
                answerButtons[i].interactable = true;
                var img = answerButtons[i].GetComponent<Image>();
                if (img) img.color = Color.white;
            }
        }

        private void OnAnswerTapped(int answer)
        {
            if (!_awaitingAnswer) return;
            _awaitingAnswer = false;

            if (answer == _correctAnswer)
            {
                _rowsAnswered++;
                AddScore(1);
                AudioManager.Instance.PlayCorrect();
                if (waterParticles) waterParticles.Play();
                TintGrid(new Color(0.4f, 0.9f, 0.4f));
            }
            else
            {
                AudioManager.Instance.PlayWrong();
                ShowWilted();
            }
        }

        private void ShowWilted()
        {
            TintGrid(new Color(0.7f, 0.6f, 0.2f));
        }

        private void TintGrid(Color color)
        {
            foreach (var cell in _gridCells)
            {
                if (cell == null) continue;
                var img = cell.GetComponent<Image>();
                if (img) img.color = color;
            }
        }

        private static int[] GenerateChoices(int correct, int min, int max)
        {
            var set = new HashSet<int> { correct };
            int att = 0;
            while (set.Count < 3 && att++ < 30)
            {
                int w = correct + (Random.Range(0, 2) == 0 ? 1 : -1) * Random.Range(1, 6);
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
