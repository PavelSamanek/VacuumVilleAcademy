using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 8 Minigame: Items on a conveyor belt must be swiped left/right
    /// into bins with target counts. 90 seconds, 3 bins, items divided equally.
    /// </summary>
    public class AtticBinBlitz : BaseMinigame
    {
        [Header("Bin Blitz")]
        [SerializeField] private Transform conveyorSpawn;
        [SerializeField] private Transform conveyorEnd;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private BinSlot[] bins;               // 2 or 3 bins
        [SerializeField] private float conveyorSpeed = 2.5f;
        [SerializeField] private float spawnInterval = 1.2f;
        [SerializeField] private TextMeshProUGUI problemLabel;

        private int _totalItems;
        private int _sortedCorrectly;
        private int _divisor;
        private int _dividend;
        private int _correctPerBin;
        private List<ConveyorItem> _activeItems = new();

        protected override float TimeLimit => 90f;
        protected override int MaxScore   => 30;

        [System.Serializable]
        public class BinSlot
        {
            public Transform transform;
            public TextMeshProUGUI targetLabel;
            public TextMeshProUGUI currentLabel;
            public int targetCount;
            public int currentCount;
            [System.NonSerialized] public Color baseColor = Color.white;
        }

        private class ConveyorItem
        {
            public GameObject Go;
            public bool Swiped;
            public Vector2 SwipeStart;
            public bool TouchActive;
        }

        protected override bool IsSetupComplete() => itemPrefab != null;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            EnsureLayout();
            SetupDivisionProblem();
            StartCoroutine(SpawnLoop());
        }

        private void EnsureLayout()
        {
            // Division equation label — top strip
            if (problemLabel == null)
            {
                var go = new GameObject("ProblemLabel");
                go.transform.SetParent(transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.84f);
                rt.anchorMax = new Vector2(1f, 0.94f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var bg = go.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.4f);
                bg.raycastTarget = false;
                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(go.transform, false);
                var trt = txtGo.AddComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
                problemLabel = txtGo.AddComponent<TextMeshProUGUI>();
                problemLabel.fontSize = 60f;
                problemLabel.fontStyle = FontStyles.Bold;
                problemLabel.color = Color.white;
                problemLabel.alignment = TextAlignmentOptions.Center;
            }

            Color[] binColors =
            {
                new Color(0.20f, 0.50f, 0.90f), // blue — left swipe
                new Color(0.58f, 0.28f, 0.80f), // purple — centre
                new Color(0.18f, 0.72f, 0.40f)  // green — right swipe
            };
            string[] arrows = bins.Length >= 3
                ? new[] { "←", "↕", "→" }
                : new[] { "←", "→" };

            for (int i = 0; i < bins.Length; i++)
            {
                var bin  = bins[i];
                var binT = bin.transform;
                if (binT == null) continue;

                Color col = binColors[Mathf.Min(i, binColors.Length - 1)];
                bin.baseColor = col;

                // Colour the bin's own Image (add one if somehow missing)
                var img = binT.GetComponent<Image>();
                if (img == null) img = binT.gameObject.AddComponent<Image>();
                img.color = col;

                // The scene places targetLabel / currentLabel as SIBLINGS of the bin,
                // which means the bin's Image renders on top and hides them.
                // Fix: reparent labels INTO the bin so they render above its Image.
                if (bin.targetLabel != null && bin.targetLabel.transform.parent != binT)
                {
                    bin.targetLabel.transform.SetParent(binT, false);
                    var lrt = bin.targetLabel.GetComponent<RectTransform>();
                    lrt.anchorMin = new Vector2(0.05f, 0.1f);
                    lrt.anchorMax = new Vector2(0.95f, 0.9f);
                    lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                    bin.targetLabel.fontSize      = 52f;
                    bin.targetLabel.fontStyle     = FontStyles.Bold;
                    bin.targetLabel.color         = Color.white;
                    bin.targetLabel.alignment     = TextAlignmentOptions.Center;
                    bin.targetLabel.enableWordWrapping = false;
                }

                // currentLabel no longer needed — hide it
                if (bin.currentLabel != null)
                    bin.currentLabel.gameObject.SetActive(false);

                // Direction arrow above the bin (created once)
                if (binT.Find("DirectionArrow") == null)
                {
                    var arrowGo = new GameObject("DirectionArrow");
                    arrowGo.transform.SetParent(binT, false);
                    var rt = arrowGo.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = new Vector2(0f, 70f);
                    var tmp = arrowGo.AddComponent<TextMeshProUGUI>();
                    tmp.text      = arrows[Mathf.Min(i, arrows.Length - 1)];
                    tmp.fontSize  = 56f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color     = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private void SetupDivisionProblem()
        {
            _divisor   = Mathf.Max(1, bins.Length);
            int[] facts = { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            _correctPerBin = facts[Random.Range(0, facts.Length)];
            _dividend  = _divisor * _correctPerBin;

            // Show division equation above the conveyor
            if (problemLabel != null)
                problemLabel.text = $"{_dividend} ÷ {_divisor} = ?";

            foreach (var bin in bins)
            {
                bin.targetCount  = _correctPerBin;
                bin.currentCount = 0;
                if (bin.targetLabel != null)
                {
                    bin.targetLabel.text  = $"0 / {_correctPerBin}";
                    bin.targetLabel.color = Color.white;
                }
            }
        }

        private IEnumerator SpawnLoop()
        {
            int spawned = 0;
            while (GameActive && spawned < _dividend)
            {
                SpawnItem();
                spawned++;
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnItem()
        {
            var go = Instantiate(itemPrefab, transform); // parent to Canvas so UI renders
            go.transform.position = conveyorSpawn.position;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // sprite is inverted
            MinigameVFX.SpawnPop(this, go.transform);
            AudioManager.Instance?.PlaySFX("Audio/SFX/atticbin/item_swipe");
            _activeItems.Add(new ConveyorItem { Go = go });
        }

        private void Update()
        {
            if (!GameActive) return;

            MoveItems();
            HandleSwipeInput();
        }

        private void MoveItems()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                var item = _activeItems[i];
                if (item.Swiped || item.Go == null) continue;

                item.Go.transform.position = Vector3.MoveTowards(
                    item.Go.transform.position, conveyorEnd.position,
                    conveyorSpeed * Time.deltaTime);

                if (Vector3.Distance(item.Go.transform.position, conveyorEnd.position) < 20f)
                {
                    Destroy(item.Go);
                    _activeItems.RemoveAt(i);
                }
            }
        }

        private void HandleSwipeInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            {
                foreach (var item in _activeItems)
                {
                    if (item.Swiped) continue;
                    // Canvas is Screen Space Overlay — screen pixels match UI world position
                    Vector3 sp = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
                    if (Vector3.Distance(sp, item.Go.transform.position) < 60f)
                    {
                        item.TouchActive = true;
                        item.SwipeStart  = Input.mousePosition;
                    }
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                Vector2 delta = (Vector2)Input.mousePosition;
                foreach (var item in _activeItems)
                {
                    if (!item.TouchActive) continue;
                    item.TouchActive = false;
                    float dx = delta.x - item.SwipeStart.x;
                    int binIdx = dx < 0 ? 0 : bins.Length - 1;
                    if (bins.Length == 3) binIdx = dx < -50 ? 0 : dx > 50 ? 2 : 1;
                    SortItemToBin(item, binIdx);
                }
            }
#else
            foreach (Touch touch in Input.touches)
            {
                Vector3 wp = new Vector3(touch.position.x, touch.position.y, 0);

                if (touch.phase == TouchPhase.Began)
                {
                    foreach (var item in _activeItems)
                    {
                        if (!item.Swiped && Vector3.Distance(wp, item.Go.transform.position) < 60f)
                        { item.TouchActive = true; item.SwipeStart = touch.position; }
                    }
                }
                if (touch.phase == TouchPhase.Ended)
                {
                    foreach (var item in _activeItems)
                    {
                        if (!item.TouchActive) continue;
                        item.TouchActive = false;
                        float dx = touch.position.x - item.SwipeStart.x;
                        int binIdx = dx < 0 ? 0 : bins.Length - 1;
                        if (bins.Length == 3) binIdx = dx < -50 ? 0 : dx > 50 ? 2 : 1;
                        SortItemToBin(item, binIdx);
                    }
                }
            }
#endif
        }

        private void SortItemToBin(ConveyorItem item, int binIdx)
        {
            if (item.Swiped || binIdx >= bins.Length) return;
            item.Swiped = true;

            var bin = bins[binIdx];
            bin.currentCount++;
            if (bin.targetLabel != null)
            {
                bin.targetLabel.text  = $"{bin.currentCount} / {bin.targetCount}";
                bin.targetLabel.color = Color.white;
            }

            bool overflowed = bin.currentCount > bin.targetCount;

            if (overflowed)
            {
                // Overflow: reset bin
                bin.currentCount = 0;
                if (bin.targetLabel != null)
                {
                    bin.targetLabel.text  = $"0 / {bin.targetCount}";
                    bin.targetLabel.color = Color.white;
                }
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/atticbin/bin_overflow");
                StartCoroutine(FlashBin(bin, Color.red));
                if (bin.transform != null) MinigameVFX.ShakeRect(this, (RectTransform)bin.transform);
            }
            else
            {
                _sortedCorrectly++;
                AddScore(1);
                AudioManager.Instance.PlayCorrect();
                AudioManager.Instance?.PlaySFX("Audio/SFX/atticbin/bin_land");
                StartCoroutine(FlashBin(bin, new Color(0.4f, 0.9f, 0.4f)));
                if (bin.transform != null)
                {
                    MinigameVFX.PulseRing(this, bin.transform.position, new Color(0.412f, 0.941f, 0.682f));
                    MinigameVFX.FloatingText(this, "+1", bin.transform.position, new Color(0.412f, 0.941f, 0.682f));
                }

                if (_sortedCorrectly >= _dividend) CompleteEarly();
            }

            StartCoroutine(MoveItemToBin(item.Go.transform, bin.transform.position));
        }

        private IEnumerator MoveItemToBin(Transform item, Vector3 target)
        {
            float duration = 0.3f;
            float elapsed  = 0f;
            Vector3 start  = item.position;
            while (elapsed < duration)
            {
                item.position = Vector3.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(item.gameObject);
        }

        private IEnumerator FlashBin(BinSlot bin, Color flashColor)
        {
            var img = bin.transform?.GetComponent<Image>();
            if (!img) yield break;
            img.color = flashColor;
            yield return new WaitForSeconds(0.3f);
            img.color = bin.baseColor; // restore themed colour, not white
        }
    }
}
