using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 8 Minigame: Division tap-the-answer.
    /// A division equation appears at the top. Three coloured bins each show a number.
    /// Tap the bin with the correct quotient. 5 rounds, 60 seconds.
    /// </summary>
    public class AtticBinBlitz : BaseMinigame
    {
        [Header("Bin Blitz")]
        [SerializeField] private BinSlot[] bins;
        [SerializeField] private TextMeshProUGUI problemLabel;

        private int _round;
        private int _correctAnswer;
        private bool _answerPending;

        private const int TotalRounds = 5;

        protected override float TimeLimit => 60f;
        protected override int MaxScore   => TotalRounds;

        // Keeps original field names so the existing scene YAML deserialises correctly.
        [System.Serializable]
        public class BinSlot
        {
            public Transform transform;
            public TextMeshProUGUI targetLabel;   // repurposed as the answer label
            public TextMeshProUGUI currentLabel;  // hidden
            public int targetCount;
            public int currentCount;
            [System.NonSerialized] public Button  button;
            [System.NonSerialized] public Color   baseColor = Color.white;
            [System.NonSerialized] public int     displayedValue;
        }

        protected override bool IsSetupComplete() =>
            bins != null && bins.Length >= 2;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            EnsureLayout();
            StartCoroutine(RoundLoop());
        }

        private void EnsureLayout()
        {
            // Problem label — top strip
            if (problemLabel == null)
            {
                var go  = new GameObject("ProblemLabel");
                go.transform.SetParent(transform, false);
                var rt  = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.84f);
                rt.anchorMax = new Vector2(1f, 0.94f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var bg  = go.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.4f);
                bg.raycastTarget = false;
                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(go.transform, false);
                var trt = txtGo.AddComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
                problemLabel  = txtGo.AddComponent<TextMeshProUGUI>();
                problemLabel.fontSize  = 72f;
                problemLabel.fontStyle = FontStyles.Bold;
                problemLabel.color     = Color.white;
                problemLabel.alignment = TextAlignmentOptions.Center;
            }

            Color[] binColors =
            {
                new Color(0.20f, 0.50f, 0.90f), // blue
                new Color(0.58f, 0.28f, 0.80f), // purple
                new Color(0.18f, 0.72f, 0.40f)  // green
            };

            float xStep = 1f / bins.Length;

            for (int i = 0; i < bins.Length; i++)
            {
                var bin  = bins[i];
                var binT = bin.transform;
                if (binT == null) continue;

                Color col  = binColors[Mathf.Min(i, binColors.Length - 1)];
                bin.baseColor = col;

                // Position bin as a large tap target across the lower canvas
                var rt = binT as RectTransform ?? (RectTransform)binT;
                if (rt != null)
                {
                    if (rt.parent != transform) rt.SetParent(transform, false);
                    rt.anchorMin   = new Vector2(i * xStep + 0.01f, 0.28f);
                    rt.anchorMax   = new Vector2((i + 1) * xStep - 0.01f, 0.72f);
                    rt.offsetMin   = rt.offsetMax = Vector2.zero;
                }

                // Ensure an Image for colour
                var img = binT.GetComponent<Image>();
                if (img == null) img = binT.gameObject.AddComponent<Image>();
                img.color = col;

                // Ensure a Button for tap detection (add once if missing)
                bin.button = binT.GetComponent<Button>();
                if (bin.button == null) bin.button = binT.gameObject.AddComponent<Button>();
                bin.button.targetGraphic = img;

                // Style the answer label (reuse targetLabel)
                if (bin.targetLabel != null)
                {
                    if (bin.targetLabel.transform.parent != binT)
                        bin.targetLabel.transform.SetParent(binT, false);
                    var lrt = bin.targetLabel.GetComponent<RectTransform>();
                    lrt.anchorMin  = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin  = lrt.offsetMax = Vector2.zero;
                    bin.targetLabel.fontSize  = 96f;
                    bin.targetLabel.fontStyle = FontStyles.Bold;
                    bin.targetLabel.color     = Color.white;
                    bin.targetLabel.alignment = TextAlignmentOptions.Center;
                    bin.targetLabel.enableWordWrapping = false;
                }

                // Hide unused label
                if (bin.currentLabel != null)
                    bin.currentLabel.gameObject.SetActive(false);
            }
        }

        private IEnumerator RoundLoop()
        {
            while (GameActive && _round < TotalRounds)
            {
                _round++;
                GenerateQuestion();
                _answerPending = true;
                yield return new WaitUntil(() => !_answerPending || !GameActive);
                yield return new WaitForSeconds(0.6f);
            }
            CompleteEarly();
        }

        private void GenerateQuestion()
        {
            int divisor = bins.Length;
            int[] facts = { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            _correctAnswer = facts[Random.Range(0, facts.Length)];
            int dividend   = divisor * _correctAnswer;

            if (problemLabel != null)
                problemLabel.text = $"{dividend} ÷ {divisor} = ?";

            AudioManager.Instance.PlayVoice($"q_division_{dividend}_{divisor}");

            int correctBin = Random.Range(0, bins.Length);
            var wrongs     = GenerateWrongAnswers(_correctAnswer);
            int wrongIdx   = 0;

            for (int i = 0; i < bins.Length; i++)
            {
                var bin = bins[i];
                bin.displayedValue = (i == correctBin) ? _correctAnswer : wrongs[wrongIdx++];

                if (bin.targetLabel != null)
                    bin.targetLabel.text = bin.displayedValue.ToString();

                var img = bin.button?.GetComponent<Image>();
                if (img) img.color = bin.baseColor;

                if (bin.button != null)
                {
                    bin.button.interactable = true;
                    int idx = i;
                    bin.button.onClick.RemoveAllListeners();
                    bin.button.onClick.AddListener(() => OnBinTapped(idx));
                }
            }
        }

        private void OnBinTapped(int idx)
        {
            if (!_answerPending || !GameActive) return;
            _answerPending = false;

            foreach (var b in bins)
                if (b.button != null) b.button.interactable = false;

            var bin     = bins[idx];
            bool correct = bin.displayedValue == _correctAnswer;

            if (correct)
            {
                AddScore(1);
                AudioManager.Instance.PlayCorrect();
                AudioManager.Instance?.PlaySFX("Audio/SFX/atticbin/bin_land");
                MinigameVFX.ScreenFlash(this, new Color(0.412f, 0.941f, 0.682f));
                NotifyCorrect();
                StartCoroutine(FlashBin(bin, new Color(0.4f, 0.9f, 0.4f)));
                MinigameVFX.PulseRing(this, bin.button.transform.position, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.FloatingText(this, "+1", bin.button.transform.position, new Color(0.412f, 0.941f, 0.682f));
            }
            else
            {
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/atticbin/bin_overflow");
                NotifyWrong();
                StartCoroutine(FlashBin(bin, new Color(1f, 0.57f, 0f)));
                MinigameVFX.ShakeRect(this, (RectTransform)bin.button.transform);
            }
        }

        private IEnumerator FlashBin(BinSlot bin, Color flashColor)
        {
            var img = bin.button?.GetComponent<Image>();
            if (!img) yield break;
            img.color = flashColor;
            yield return new WaitForSeconds(0.4f);
            img.color = bin.baseColor;
        }

        private int[] GenerateWrongAnswers(int correct)
        {
            var set = new HashSet<int> { correct };
            int attempts = 0;
            while (set.Count < bins.Length && attempts++ < 50)
            {
                int w = correct + (Random.Range(0, 2) == 0 ? 1 : -1) * Random.Range(1, 5);
                if (w > 0 && w != correct) set.Add(w);
            }
            set.Remove(correct);
            return new List<int>(set).ToArray();
        }
    }
}
