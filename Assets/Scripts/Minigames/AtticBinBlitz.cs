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
            SetupDivisionProblem();
            StartCoroutine(SpawnLoop());
        }

        private void SetupDivisionProblem()
        {
            _divisor   = bins.Length; // 2 or 3
            int[] facts = { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            _correctPerBin = facts[Random.Range(0, facts.Length)];
            _dividend  = _divisor * _correctPerBin;

            foreach (var bin in bins)
            {
                bin.targetCount  = _correctPerBin;
                bin.currentCount = 0;
                bin.targetLabel.text  = _correctPerBin.ToString();
                bin.currentLabel.text = "0";
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
            MinigameVFX.SpawnPop(this, go.transform);
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
            bin.currentLabel.text = bin.currentCount.ToString();

            bool overflowed = bin.currentCount > bin.targetCount;

            if (overflowed)
            {
                // Overflow: reset bin
                bin.currentCount = 0;
                bin.currentLabel.text = "0";
                AudioManager.Instance.PlayWrong();
                StartCoroutine(FlashBin(bin, Color.red));
                if (bin.transform != null) MinigameVFX.ShakeRect(this, (RectTransform)bin.transform);
            }
            else
            {
                _sortedCorrectly++;
                AddScore(1);
                AudioManager.Instance.PlayCorrect();
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

        private IEnumerator FlashBin(BinSlot bin, Color color)
        {
            var img = bin.transform.GetComponent<Image>();
            if (!img) yield break;
            img.color = color;
            yield return new WaitForSeconds(0.3f);
            img.color = Color.white;
        }
    }
}
