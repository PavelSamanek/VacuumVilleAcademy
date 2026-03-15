using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

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

        protected override bool IsSetupComplete() => flowerCellPrefab != null;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            EnsureLayout();
            StartCoroutine(RowLoop());
        }

        private void EnsureLayout()
        {
            // Question label — top strip
            if (rowQuestionLabel == null)
            {
                var go = new GameObject("RowQuestionLabel");
                go.transform.SetParent(transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.78f);
                rt.anchorMax = new Vector2(1f, 0.92f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var bg = go.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.45f);
                bg.raycastTarget = false;
                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(go.transform, false);
                var trt = txtGo.AddComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
                rowQuestionLabel = txtGo.AddComponent<TextMeshProUGUI>();
                rowQuestionLabel.fontSize = 52f;
                rowQuestionLabel.fontStyle = FontStyles.Bold;
                rowQuestionLabel.color = Color.white;
                rowQuestionLabel.alignment = TextAlignmentOptions.Center;
            }

            // Garden grid — middle strip
            if (gardenGrid == null)
            {
                var go = new GameObject("GardenGrid");
                go.transform.SetParent(transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.03f, 0.22f);
                rt.anchorMax = new Vector2(0.97f, 0.75f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                gardenGrid = go.AddComponent<GridLayoutGroup>();
                gardenGrid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                gardenGrid.constraintCount = gridColumns;
                gardenGrid.spacing         = new Vector2(10, 10);
                gardenGrid.padding         = new RectOffset(10, 10, 10, 10);
                gardenGrid.childAlignment  = TextAnchor.UpperCenter;
                gardenGrid.cellSize        = new Vector2(70, 70);
            }

            // Answer buttons — bottom strip, positioned directly with anchors (no layout group)
            if (answerButtons == null || answerButtons.Length == 0)
            {
                answerButtons = new Button[3];
                answerLabels  = new TextMeshProUGUI[3];
                float[] xMin = { 0.02f, 0.36f, 0.70f };
                float[] xMax = { 0.32f, 0.66f, 0.98f };
                for (int i = 0; i < 3; i++)
                {
                    var btn = new GameObject($"AnswerBtn_{i}");
                    btn.transform.SetParent(transform, false);
                    var brt = btn.AddComponent<RectTransform>();
                    brt.anchorMin = new Vector2(xMin[i], 0.02f);
                    brt.anchorMax = new Vector2(xMax[i], 0.19f);
                    brt.offsetMin = brt.offsetMax = Vector2.zero;
                    var img = btn.AddComponent<Image>();
                    img.color = new Color(0.13f, 0.59f, 0.95f);
                    var b = btn.AddComponent<Button>();
                    b.targetGraphic = img;
                    var lGo = new GameObject("Label");
                    lGo.transform.SetParent(btn.transform, false);
                    var lrt = lGo.AddComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                    var tmp = lGo.AddComponent<TextMeshProUGUI>();
                    tmp.fontSize = 60f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;
                    answerButtons[i] = b;
                    answerLabels[i]  = tmp;
                }
            }
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

        private void ClearGrid()
        {
            foreach (var cell in _gridCells)
                if (cell != null) Destroy(cell);
            _gridCells.Clear();
        }

        private IEnumerator PresentRow()
        {
            ClearGrid();

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
                MinigameVFX.SpawnPop(this, cell.transform);
                AudioManager.Instance?.PlaySFX("Audio/SFX/flowerbed/flower_drop");
                yield return new WaitForSeconds(0.05f);
            }

            // Show question
            if (rowQuestionLabel != null)
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
                AudioManager.Instance?.PlaySFX("Audio/SFX/flowerbed/wilt");
                yield return new WaitForSeconds(0.8f);
            }
        }

        private void SetupAnswerButtons()
        {
            // Reposition scene-placed buttons to on-screen bottom strip
            if (answerButtons != null)
            {
                float[] xMin = { 0.02f, 0.36f, 0.70f };
                float[] xMax = { 0.32f, 0.66f, 0.98f };
                for (int i = 0; i < answerButtons.Length && i < xMin.Length; i++)
                {
                    if (answerButtons[i] == null) continue;
                    var rt = (RectTransform)answerButtons[i].transform;
                    if (rt.parent != transform) rt.SetParent(transform, false);
                    rt.anchorMin = new Vector2(xMin[i], 0.02f);
                    rt.anchorMax = new Vector2(xMax[i], 0.19f);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                }
            }

            // Pick a button color that contrasts with the current background.
            // For green-heavy themes (Multiplication) the theme color blends in,
            // so we use orange as a strong complement.
            var topic = GameManager.Instance?.ActiveLevel?.mathTopic
                        ?? VacuumVille.Data.MathTopic.Multiplication2x5x;
            Color themeBtn = PersistentBackground.GetButtonColorForTopic(topic);
            // Green theme → switch to orange for contrast on green background
            bool isGreenTheme = topic == VacuumVille.Data.MathTopic.Multiplication2x5x;
            Color btnColor = isGreenTheme ? new Color(1f, 0.55f, 0.10f) : themeBtn;

            var choices = GenerateChoices(_correctAnswer, 2, 50);
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int val = choices[i];
                answerButtons[i].gameObject.SetActive(true);
                answerLabels[i].text = val.ToString();
                answerLabels[i].color = Color.white;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerTapped(val));
                answerButtons[i].interactable = true;
                var img = answerButtons[i].GetComponent<Image>();
                if (img) img.color = btnColor;
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
                AudioManager.Instance?.PlaySFX("Audio/SFX/flowerbed/water_spray");
                if (waterParticles) waterParticles.Play();
                TintGrid(new Color(0.4f, 0.9f, 0.4f));
                Vector3 gridPos = gardenGrid != null ? gardenGrid.transform.position : transform.position;
                MinigameVFX.PulseRing(this, gridPos, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.FloatingText(this, "+1", gridPos, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.ScreenFlash(this, new Color(0.412f, 0.941f, 0.682f), 0.2f, 0.3f);
            }
            else
            {
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/flowerbed/wilt");
                ShowWilted();
                if (answerButtons != null && answerButtons.Length > 0)
                    MinigameVFX.ShakeRect(this, (RectTransform)answerButtons[0].transform);
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
