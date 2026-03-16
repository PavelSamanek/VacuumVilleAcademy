using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 9 Minigame: Tap numbered sprinkler heads in correct ascending or skip-count order.
    /// Wrong order → water splash on Luna. Full sequence → rainbow. 90 seconds.
    /// </summary>
    public class SequenceSprinkler : BaseMinigame
    {
        [Header("Sprinkler")]
        [SerializeField] private SprinklerHead[] sprinklerHeads; // 8–10 heads
        [SerializeField] private Transform vacuumTransform;
        [SerializeField] private ParticleSystem splashParticles;
        [SerializeField] private ParticleSystem rainbowParticles;
        [SerializeField] private TextMeshProUGUI sequenceHintLabel;

        private List<int> _sequence = new();
        private int _nextIndex;
        private int _setsCompleted;
        private int _skipValue;

        protected override float TimeLimit => 90f;
        protected override int MaxScore   => 5;

        [System.Serializable]
        public class SprinklerHead
        {
            public Button button;
            public TextMeshProUGUI label;
            public Image indicator;
            public int assignedNumber;
            public bool activated;
        }

        protected override bool IsSetupComplete() =>
            sprinklerHeads != null && sprinklerHeads.Length > 0;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            if (sequenceHintLabel == null) sequenceHintLabel = CreateHintLabel();
            RepositionHeads();
            StartCoroutine(RoundLoop());
        }

        /// <summary>
        /// Scene heads use anchor (0.5,0) — bottom edge — with some at negative y,
        /// placing them below the canvas. Redistribute all heads into a 2-row grid.
        /// </summary>
        private void RepositionHeads()
        {
            if (sprinklerHeads == null || sprinklerHeads.Length == 0) return;

            const int cols   = 4;
            // Two rows in the centre of the canvas, leaving room for the hint label at top
            float[] rowYMin = { 0.53f, 0.30f }; // bottom edge of each row (fraction of canvas)
            float[] rowYMax = { 0.74f, 0.51f }; // top edge of each row
            float xStep = 1f / cols;
            const float xPad = 0.01f;

            for (int i = 0; i < sprinklerHeads.Length; i++)
            {
                var head = sprinklerHeads[i];
                if (head.button == null) continue;

                var rt  = (RectTransform)head.button.transform;
                int row = i / cols;
                int col = i % cols;
                if (row >= rowYMin.Length) row = rowYMin.Length - 1;

                rt.anchorMin = new Vector2(col * xStep + xPad, rowYMin[row]);
                rt.anchorMax = new Vector2((col + 1) * xStep - xPad, rowYMax[row]);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
        }

        private TextMeshProUGUI CreateHintLabel()
        {
            var go = new GameObject("SequenceHintLabel");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.88f);
            rt.anchorMax = new Vector2(1f, 0.96f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.2f, 0.5f, 0.8f, 0.8f);
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 48f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private IEnumerator RoundLoop()
        {
            while (GameActive && _setsCompleted < 5)
            {
                GenerateSequence();
                AssignToHeads();
                _nextIndex = 0;
                SetButtonsInteractable(true);

                // Wait until sequence complete
                while (_nextIndex < _sequence.Count && GameActive)
                    yield return null;

                if (_nextIndex >= _sequence.Count)
                {
                    _setsCompleted++;
                    AddScore(1);
                    if (rainbowParticles) rainbowParticles.Play();
                    AudioManager.Instance.PlayCorrect();
                    MinigameVFX.FloatingText(this, "+1", transform.position, new Color(0.412f, 0.941f, 0.682f));
                    MinigameVFX.ScreenFlash(this, new Color(0.412f, 0.941f, 0.682f), 0.3f, 0.4f);
                    yield return new WaitForSeconds(1f);
                }
            }
            CompleteEarly();
        }

        private void GenerateSequence()
        {
            _sequence.Clear();
            int[] skipOptions = { 1, 2, 5, 10 };
            _skipValue = skipOptions[Random.Range(0, skipOptions.Length)];

            int start = Random.Range(1, 51);
            int count = sprinklerHeads.Length;
            for (int i = 0; i < count; i++)
                _sequence.Add(start + i * _skipValue);

            if (sequenceHintLabel)
            {
                string hint = _skipValue == 1
                    ? LocalizationManager.Instance.Get("sequence_ascending")
                    : LocalizationManager.Instance.Get("sequence_skip", _skipValue);
                sequenceHintLabel.text = hint;
            }
        }

        private void AssignToHeads()
        {
            // Shuffle sequence across heads
            var shuffled = new List<int>(_sequence);
            for (int i = shuffled.Count - 1; i > 0; i--)
            { int j = Random.Range(0, i + 1); (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]); }

            for (int i = 0; i < sprinklerHeads.Length && i < shuffled.Count; i++)
            {
                var head = sprinklerHeads[i];
                head.assignedNumber = shuffled[i];
                head.activated      = false;
                head.button.gameObject.SetActive(true);
                head.label.text     = shuffled[i].ToString();
                head.label.color    = Color.white;
                if (head.indicator) head.indicator.color = new Color(0.18f, 0.52f, 0.88f);

                int idx = i;
                head.button.onClick.RemoveAllListeners();
                head.button.onClick.AddListener(() => OnSprinklerTapped(idx));
                head.button.interactable = true;
            }
        }

        private void OnSprinklerTapped(int headIndex)
        {
            if (!GameActive || _nextIndex >= _sequence.Count) return;

            var head = sprinklerHeads[headIndex];
            if (head.activated) return;

            int expected = _sequence[_nextIndex];

            if (head.assignedNumber == expected)
            {
                head.activated = true;
                head.button.interactable = false;
                if (head.indicator) head.indicator.color = new Color(0.4f, 0.9f, 0.4f);

                AudioManager.Instance.PlayCorrect();
                AudioManager.Instance?.PlaySFX("Audio/SFX/sprinkler/water_burst");
                NotifyCorrect();
                MoveVacuumTo(head.button.transform.position);
                MinigameVFX.PulseRing(this, head.button.transform.position, new Color(0.412f, 0.941f, 0.682f));
                _nextIndex++;
            }
            else
            {
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/sprinkler/wrong_splash");
                NotifyWrong();
                if (splashParticles)
                {
                    if (vacuumTransform != null)
                        splashParticles.transform.position = vacuumTransform.position;
                    splashParticles.Play();
                }
                MinigameVFX.ShakeRect(this, (RectTransform)head.button.transform);
                // Flash wrong button red briefly
                StartCoroutine(FlashWrong(head));
            }
        }

        private void MoveVacuumTo(Vector3 target)
        {
            if (vacuumTransform == null) return;
            target.z = 0;
            StopCoroutine(nameof(SlideTo));
            StartCoroutine(SlideTo(target));
        }

        private IEnumerator SlideTo(Vector3 target)
        {
            float speed = 6f;
            while (vacuumTransform != null && Vector3.Distance(vacuumTransform.position, target) > 0.1f)
            {
                vacuumTransform.position = Vector3.MoveTowards(
                    vacuumTransform.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator FlashWrong(SprinklerHead head)
        {
            if (head.indicator)
            {
                head.indicator.color = new Color(1f, 0.57f, 0f);
                yield return new WaitForSeconds(0.3f);
                head.indicator.color = new Color(0.18f, 0.52f, 0.88f);
            }
        }

        private void SetButtonsInteractable(bool state)
        {
            foreach (var h in sprinklerHeads)
                h.button.interactable = state && !h.activated;
        }
    }
}
