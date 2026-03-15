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
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            if (targetSumLabel == null) targetSumLabel = CreateTargetLabel();
            NewTargetSum();
            _spawnCoroutine = StartCoroutine(SpawnLoop());
        }

        private TextMeshProUGUI CreateTargetLabel()
        {
            var go = new GameObject("TargetSumLabel");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.89f);
            rt.anchorMax = new Vector2(1f, 0.97f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.1f, 0.2f, 0.5f, 0.85f);
            var tmp = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            tmp.transform.SetParent(go.transform, false);
            var trt = tmp.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            tmp.fontSize = 58f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
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
            if (lbl) { lbl.text = num.ToString(); lbl.color = new Color(0.1f, 0.1f, 0.2f); }

            var btn = go.GetComponentInChildren<Button>();
            var item = new BoxItem { Number = num, Go = go };
            if (btn) btn.onClick.AddListener(() => OnBoxTapped(item));

            MinigameVFX.SpawnPop(this, go.transform);
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
                AudioManager.Instance?.PlaySFX("Audio/SFX/boxtower/box_select");
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
                    AudioManager.Instance?.PlaySFX("Audio/SFX/boxtower/box_fall_land");
                    if (box.Go != null) MinigameVFX.ShakeRect(this, (RectTransform)box.Go.transform);
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

            Vector3 midPos = a.Go != null ? a.Go.transform.position : b.Go.transform.position;

            if (vacuumParticles)
            {
                vacuumParticles.transform.position = midPos;
                vacuumParticles.Play();
            }

            MinigameVFX.PulseRing(this, midPos, new Color(0.412f, 0.941f, 0.682f));
            MinigameVFX.FloatingText(this, "+1", midPos, new Color(0.412f, 0.941f, 0.682f));

            if (a.Go != null) { MinigameVFX.CollectBurst(this, a.Go, new Color(0.412f, 0.941f, 0.682f)); a.Go = null; }
            if (b.Go != null) { MinigameVFX.CollectBurst(this, b.Go, new Color(0.412f, 0.941f, 0.682f)); b.Go = null; }

            AudioManager.Instance.PlayCorrect();
            AudioManager.Instance?.PlaySFX("Audio/SFX/boxtower/box_vacuum");
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
