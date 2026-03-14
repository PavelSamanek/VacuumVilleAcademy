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
            StartCoroutine(RoundLoop());
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
                head.label.text     = shuffled[i].ToString();
                if (head.indicator) head.indicator.color = Color.white;

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
                MoveVacuumTo(head.button.transform.position);
                _nextIndex++;
            }
            else
            {
                AudioManager.Instance.PlayWrong();
                if (splashParticles)
                {
                    splashParticles.transform.position = vacuumTransform.position;
                    splashParticles.Play();
                }
                // Flash wrong button red briefly
                StartCoroutine(FlashWrong(head));
            }
        }

        private void MoveVacuumTo(Vector3 target)
        {
            target.z = 0;
            StopCoroutine(nameof(SlideTo));
            StartCoroutine(SlideTo(target));
        }

        private IEnumerator SlideTo(Vector3 target)
        {
            float speed = 6f;
            while (Vector3.Distance(vacuumTransform.position, target) > 0.1f)
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
                head.indicator.color = Color.white;
            }
        }

        private void SetButtonsInteractable(bool state)
        {
            foreach (var h in sprinklerHeads)
                h.button.interactable = state && !h.activated;
        }
    }
}
