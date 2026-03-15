using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 5 Minigame: Boxes labeled with numbers fall from above.
    /// Player taps two boxes whose numbers add to the target sum shown at the top.
    /// Correct pairs get vacuumed up. Wrong pairs stack up.
    /// Game ends at 20 correct pairs or when the path is blocked.
    /// </summary>
    public class BoxTowerBuilder : BaseMinigame
    {
        [Header("Box Tower")]
        [SerializeField] private TextMeshProUGUI targetSumLabel;
        [SerializeField] private GameObject boxPrefab;
        [SerializeField] private Transform spawnLine;
        [SerializeField] private Transform stackLine;
        [SerializeField] private int maxStackHeight = 6;
        [SerializeField] private float fallSpeed = 2.5f;
        [SerializeField] private ParticleSystem vacuumParticles;

        private int _targetSum;
        private int _stackHeight;
        private int _correctPairs;
        private int _firstSelectedNumber = -1;
        private BoxItem _firstSelectedBox;
        private List<BoxItem> _fallingBoxes = new();
        private Coroutine _spawnCoroutine;

        protected override float TimeLimit => 180f;
        protected override int MaxScore   => 20;

        private class BoxItem
        {
            public int Number;
            public GameObject Go;
            public bool Selected;
            public bool Collected;
        }

        protected override bool IsSetupComplete() =>
            boxPrefab != null && spawnLine != null;

        protected override void OnMinigameBegin()
        {
            NewTargetSum();
            _spawnCoroutine = StartCoroutine(SpawnLoop());
        }

        private void NewTargetSum()
        {
            _targetSum = Random.Range(5, 20);
            if (targetSumLabel)
                targetSumLabel.text = LocalizationManager.Instance.Get("target_sum", _targetSum);
        }

        private IEnumerator SpawnLoop()
        {
            while (GameActive)
            {
                if (_fallingBoxes.Count < 8)
                    SpawnBox();
                yield return new WaitForSeconds(1.2f);
            }
        }

        private void SpawnBox()
        {
            // Ensure at least one box has a valid partner for the target sum
            int num;
            bool spawnPartner = _fallingBoxes.Count > 0 && Random.Range(0, 3) == 0;
            if (spawnPartner)
            {
                int existing = _fallingBoxes[Random.Range(0, _fallingBoxes.Count)].Number;
                int partner  = _targetSum - existing;
                num = (partner > 0 && partner <= 20) ? partner : Random.Range(1, _targetSum);
            }
            else
            {
                num = Random.Range(1, _targetSum);
            }

            // Canvas is Screen Space Overlay — positions are in screen pixels
            float xPos = Random.Range(spawnLine.position.x - 280f, spawnLine.position.x + 280f);
            var go  = Instantiate(boxPrefab, transform); // parent to Canvas so UI renders
            go.transform.position = new Vector3(xPos, spawnLine.position.y, 0);
            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = num.ToString();

            var btn = go.GetComponentInChildren<Button>();
            var item = new BoxItem { Number = num, Go = go };
            if (btn) btn.onClick.AddListener(() => OnBoxTapped(item));

            _fallingBoxes.Add(item);
        }

        private void Update()
        {
            if (!GameActive) return;

            for (int i = _fallingBoxes.Count - 1; i >= 0; i--)
            {
                var box = _fallingBoxes[i];
                if (box.Collected || box.Go == null) continue;

                box.Go.transform.position += Vector3.down * fallSpeed * Time.deltaTime;

                if (box.Go.transform.position.y <= stackLine.position.y + _stackHeight * 80f)
                {
                    StackBox(box);
                    _fallingBoxes.RemoveAt(i);
                }
            }
        }

        private void OnBoxTapped(BoxItem box)
        {
            if (!GameActive || box.Collected) return;

            if (_firstSelectedBox == null)
            {
                // First selection
                _firstSelectedBox = box;
                box.Selected = true;
                HighlightBox(box, true);
                AudioManager.Instance.PlayButton();
            }
            else if (box == _firstSelectedBox)
            {
                // Deselect
                _firstSelectedBox = null;
                box.Selected = false;
                HighlightBox(box, false);
            }
            else
            {
                // Second selection — check sum
                int sum = _firstSelectedBox.Number + box.Number;
                if (sum == _targetSum)
                {
                    CollectPair(_firstSelectedBox, box);
                }
                else
                {
                    AudioManager.Instance.PlayWrong();
                    HighlightBox(_firstSelectedBox, false);
                    _firstSelectedBox.Selected = false;
                    _firstSelectedBox = null;
                }
            }
        }

        private void CollectPair(BoxItem a, BoxItem b)
        {
            a.Collected = true;
            b.Collected = true;
            _fallingBoxes.Remove(a);
            _fallingBoxes.Remove(b);

            if (vacuumParticles)
            {
                vacuumParticles.transform.position = a.Go.transform.position;
                vacuumParticles.Play();
            }

            Destroy(a.Go);
            Destroy(b.Go);

            AudioManager.Instance.PlayCorrect();
            AddScore(1);
            _correctPairs++;
            _firstSelectedBox = null;

            if (_correctPairs >= 20) CompleteEarly();
        }

        private void StackBox(BoxItem box)
        {
            box.Go.transform.position = new Vector3(
                box.Go.transform.position.x,
                stackLine.position.y + _stackHeight * 80f, 0);
            _stackHeight++;
            AudioManager.Instance.PlaySFX(null);

            if (_stackHeight >= maxStackHeight)
            {
                // Path blocked
                FinishMinigame_PathBlocked();
            }
        }

        private void FinishMinigame_PathBlocked()
        {
            GameActive = false;
            CompleteEarly();
        }

        private void HighlightBox(BoxItem box, bool on)
        {
            if (box.Go == null) return;
            var img = box.Go.GetComponent<Image>();
            if (img) img.color = on ? new Color(1f, 0.85f, 0.3f) : Color.white;
        }
    }
}
