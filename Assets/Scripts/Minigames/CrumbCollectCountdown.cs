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
        private Camera _cam;

        protected override float TimeLimit => 120f;
        protected override int MaxScore   => 20;

        protected override bool IsSetupComplete() =>
            crumbPilePrefab != null && gridSlots != null && gridSlots.Length > 0;

        protected override void OnMinigameBegin()
        {
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

            var slot = gridSlots[Random.Range(0, gridSlots.Length)];
            var go   = Instantiate(crumbPilePrefab, slot.position, Quaternion.identity);
            var lbl  = go.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = number.ToString();

            _activeCrumbs[number] = go;
            StartCoroutine(DespawnAfter(number, crumbVisibleTime));
        }

        private IEnumerator DespawnAfter(int number, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_activeCrumbs.ContainsKey(number))
            {
                Destroy(_activeCrumbs[number]);
                _activeCrumbs.Remove(number);
                // Respawn soon
                yield return new WaitForSeconds(0.5f);
                if (GameActive && number >= _targetNumber) SpawnCrumb(number);
            }
        }

        private void Update()
        {
            if (!GameActive) return;

            Vector3 tapPos = Vector3.zero;
            bool tapped = false;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            { tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition); tapPos.z = 0; tapped = true; }
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            { tapPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position); tapPos.z = 0; tapped = true; }
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
            float speed = 8f;
            while (Vector3.Distance(vacuumTransform.position, target) > 0.05f)
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
                if (Vector3.Distance(worldPos, kvp.Value.transform.position) < 0.7f)
                {
                    if (kvp.Key == _targetNumber)
                    {
                        CollectCrumb(kvp.Key);
                    }
                    else
                    {
                        AudioManager.Instance.PlayWrong();
                    }
                    return;
                }
            }
        }

        private void CollectCrumb(int number)
        {
            if (_activeCrumbs.TryGetValue(number, out var go))
            {
                Destroy(go);
                _activeCrumbs.Remove(number);
            }

            AudioManager.Instance.PlayCorrect();
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
