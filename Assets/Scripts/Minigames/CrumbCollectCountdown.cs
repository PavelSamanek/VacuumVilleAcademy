using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 2 Minigame: Numbered crumb piles (1-20) appear and disappear whack-a-mole style.
    /// Xixi auto-moves to where the player taps. Collect in ascending order.
    /// </summary>
    public class CrumbCollectCountdown : BaseMinigame
    {
        [Header("Crumb Collect")]
        [SerializeField] private Transform vacuumTransform;
        [SerializeField] private GameObject crumbPilePrefab;
        [SerializeField] private Transform[] gridSlots;         // 20 slots
        [SerializeField] private TextMeshProUGUI targetLabel;   // "Najdi: 7"
        [SerializeField] private float crumbVisibleTime = 3f;

        private int _targetNumber = 1;
        private readonly Dictionary<int, GameObject> _activeCrumbs = new();
        private readonly Dictionary<int, int> _crumbSlotIndex = new(); // number → slot index
        private readonly HashSet<int> _occupiedSlots = new();
        private Camera _cam;

        protected override float TimeLimit => 120f;
        protected override int MaxScore   => 20;

        protected override bool IsSetupComplete() =>
            crumbPilePrefab != null && gridSlots != null && gridSlots.Length > 0;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            _cam = Camera.main;
            UpdateTargetLabel();
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (GameActive && _targetNumber <= 20)
            {
                // Spawn a random subset of remaining numbers
                List<int> remaining = new();
                for (int n = _targetNumber; n <= Mathf.Min(_targetNumber + 5, 20); n++)
                    if (!_activeCrumbs.ContainsKey(n)) remaining.Add(n);

                if (remaining.Count > 0)
                {
                    int pick = remaining[Random.Range(0, remaining.Count)];
                    SpawnCrumb(pick);
                }

                yield return new WaitForSeconds(1.2f);
            }
        }

        private void SpawnCrumb(int number)
        {
            if (_activeCrumbs.ContainsKey(number)) return;

            // Build list of free slot indices
            var freeSlots = new List<int>();
            for (int s = 0; s < gridSlots.Length; s++)
                if (!_occupiedSlots.Contains(s)) freeSlots.Add(s);

            if (freeSlots.Count == 0) return; // no room right now

            int slotIdx = freeSlots[Random.Range(0, freeSlots.Count)];
            _occupiedSlots.Add(slotIdx);
            _crumbSlotIndex[number] = slotIdx;

            var go = Instantiate(crumbPilePrefab, transform);
            go.transform.position = gridSlots[slotIdx].position;
            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) { lbl.text = number.ToString(); lbl.color = new Color(0.1f, 0.1f, 0.2f); }

            _activeCrumbs[number] = go;
            MinigameVFX.SpawnPop(this, go.transform);
            StartCoroutine(DespawnAfter(number, crumbVisibleTime));
        }

        private IEnumerator DespawnAfter(int number, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_activeCrumbs.ContainsKey(number))
            {
                Destroy(_activeCrumbs[number]);
                _activeCrumbs.Remove(number);
                FreeSlot(number);
                AudioManager.Instance?.PlaySFX("Audio/SFX/crumbcollect/crumb_despawn");
                // Respawn soon
                yield return new WaitForSeconds(0.5f);
                if (GameActive && number >= _targetNumber) SpawnCrumb(number);
            }
        }

        private void FreeSlot(int number)
        {
            if (_crumbSlotIndex.TryGetValue(number, out int idx))
            {
                _occupiedSlots.Remove(idx);
                _crumbSlotIndex.Remove(number);
            }
        }

        private void Update()
        {
            if (!GameActive) return;

            Vector3 tapPos = Vector3.zero;
            bool tapped = false;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            // Canvas is Screen Space Overlay — screen pixels match UI world position
            { tapPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0); tapped = true; }
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            { var t0 = Input.GetTouch(0); tapPos = new Vector3(t0.position.x, t0.position.y, 0); tapped = true; }
#endif

            if (tapped)
            {
                // Move vacuum to tap
                StopCoroutine(nameof(MoveVacuumTo));
                StartCoroutine(MoveVacuumTo(tapPos));
                // Check if any crumb at tap position
                CheckTap(tapPos);
            }
        }

        private IEnumerator MoveVacuumTo(Vector3 target)
        {
            float speed = 600f; // pixels/sec for Screen Space Overlay canvas
            while (Vector3.Distance(vacuumTransform.position, target) > 5f)
            {
                vacuumTransform.position = Vector3.MoveTowards(
                    vacuumTransform.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }

        private void CheckTap(Vector3 worldPos)
        {
            foreach (var kvp in _activeCrumbs)
            {
                if (Vector3.Distance(worldPos, kvp.Value.transform.position) < 60f)
                {
                    if (kvp.Key == _targetNumber)
                    {
                        CollectCrumb(kvp.Key);
                    }
                    else
                    {
                        MinigameVFX.ShakeRect(this, (RectTransform)vacuumTransform);
                        AudioManager.Instance.PlayWrong();
                        NotifyWrong();
                    }
                    return;
                }
            }
        }

        private void CollectCrumb(int number)
        {
            Vector3 pos = Vector3.zero;
            if (_activeCrumbs.TryGetValue(number, out var go))
            {
                pos = go.transform.position;
                MinigameVFX.CollectBurst(this, go, new Color(0.412f, 0.941f, 0.682f));
                _activeCrumbs.Remove(number);
                FreeSlot(number);
            }

            MinigameVFX.PulseRing(this, pos, new Color(0.412f, 0.941f, 0.682f));
            MinigameVFX.FloatingText(this, "+1", pos, new Color(0.412f, 0.941f, 0.682f));
            AudioManager.Instance.PlayCorrect();
            AudioManager.Instance?.PlaySFX("Audio/SFX/crumbcollect/crumb_collect");
            MinigameVFX.ScreenFlash(this, new Color(0.412f, 0.941f, 0.682f));
            NotifyCorrect();
            AddScore(1);
            _targetNumber++;
            UpdateTargetLabel();

            if (_targetNumber > 20) CompleteEarly();
        }

        private void UpdateTargetLabel()
        {
            if (targetLabel)
                targetLabel.text = _targetNumber <= 20
                    ? LocalizationManager.Instance.Get("find_number", _targetNumber)
                    : LocalizationManager.Instance.Get("all_collected");
        }
    }
}
