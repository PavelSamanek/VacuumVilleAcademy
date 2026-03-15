using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 1 Minigame: Socks numbered 1-10 are scattered on the floor.
    /// Player drags the vacuum (Rumble) to collect them in ascending order.
    /// All objects live inside the Screen Space Overlay Canvas, so positions
    /// are compared in screen-pixel space.
    /// </summary>
    public class SockSortSweep : BaseMinigame
    {
        [Header("Sock Sort")]
        [SerializeField] private RectTransform vacuumTransform;
        [SerializeField] private GameObject sockPrefab;
        [SerializeField] private RectTransform[] sockSpawnPoints;
        [SerializeField] private TextMeshProUGUI nextNumberLabel;
        [SerializeField] private ParticleSystem collectParticles;

        private List<SockItem> _socks = new();
        private int _nextExpected = 1;
        private Canvas _canvas;

        // Collection radius in screen pixels — works at any resolution because
        // both the vacuum and the spawn transforms use transform.position (screen px).
        private const float CollectRadius = 80f;

        protected override float TimeLimit => 90f;
        protected override int MaxScore   => 10;

        private class SockItem
        {
            public int Number;
            public GameObject Go;
            public bool Collected;
        }

        protected override bool IsSetupComplete() =>
            sockPrefab != null && sockSpawnPoints != null && sockSpawnPoints.Length >= 10;

        protected override void OnMinigameBegin()
        {
            _canvas = vacuumTransform.GetComponentInParent<Canvas>();
            SpawnSocks();
            UpdateNextLabel();
        }

        private void SpawnSocks()
        {
            var positions = new List<Vector3>();
            foreach (var sp in sockSpawnPoints) positions.Add(sp.position);

            // Shuffle positions
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (positions[i], positions[j]) = (positions[j], positions[i]);
            }

            Transform parent = _canvas != null ? _canvas.transform : transform;
            for (int n = 1; n <= 10; n++)
            {
                var go  = Instantiate(sockPrefab, positions[n - 1], Quaternion.identity, parent);
                var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl) lbl.text = n.ToString();
                _socks.Add(new SockItem { Number = n, Go = go });
                MinigameVFX.SpawnPop(this, go.transform);
            }
        }

        private void Update()
        {
            if (!GameActive) return;

            // Input position is already in screen-pixel space — same coordinate
            // system as RectTransform.position for Screen Space Overlay canvas.
            Vector3 inputPos = Vector3.zero;
            bool hasInput = false;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(0))
            {
                inputPos = Input.mousePosition;
                inputPos.z = 0;
                hasInput = true;
            }
#else
            if (Input.touchCount > 0)
            {
                inputPos = Input.GetTouch(0).position;
                inputPos.z = 0;
                hasInput = true;
            }
#endif

            if (hasInput)
            {
                vacuumTransform.position = Vector3.Lerp(
                    vacuumTransform.position, inputPos, Time.deltaTime * 14f);
                CheckCollection();
            }
        }

        private void CheckCollection()
        {
            foreach (var sock in _socks)
            {
                if (sock.Collected || sock.Go == null) continue;
                if (Vector3.Distance(vacuumTransform.position, sock.Go.transform.position) < CollectRadius)
                {
                    if (sock.Number == _nextExpected)
                        CollectSock(sock);
                    else
                    {
                        MinigameVFX.ShakeRect(this, vacuumTransform);
                        StartCoroutine(BounceSock(sock.Go.transform));
                        AudioManager.Instance.PlayWrong();
                    }
                }
            }
        }

        private void CollectSock(SockItem sock)
        {
            sock.Collected = true;
            Vector3 pos = sock.Go.transform.position;

            if (collectParticles)
            {
                collectParticles.transform.position = pos;
                collectParticles.Play();
            }

            MinigameVFX.PulseRing(this, pos, new Color(0.412f, 0.941f, 0.682f));
            MinigameVFX.FloatingText(this, "+1", pos, new Color(0.412f, 0.941f, 0.682f));
            MinigameVFX.CollectBurst(this, sock.Go, new Color(0.412f, 0.941f, 0.682f));
            sock.Go = null;

            AudioManager.Instance.PlayCorrect();
            AddScore(1);
            _nextExpected++;
            UpdateNextLabel();

            if (_nextExpected > 10) CompleteEarly();
        }

        private void UpdateNextLabel()
        {
            if (nextNumberLabel)
                nextNumberLabel.text = _nextExpected <= 10
                    ? LocalizationManager.Instance.Get("next_number", _nextExpected)
                    : LocalizationManager.Instance.Get("all_collected");
        }

        private IEnumerator BounceSock(Transform t)
        {
            if (t == null) yield break;
            Vector3 origin = t.position;
            float duration = 0.3f;
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                float y = Mathf.Abs(Mathf.Sin(elapsed / duration * Mathf.PI)) * 40f;
                t.position = origin + new Vector3(0, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.position = origin;
        }
    }
}
