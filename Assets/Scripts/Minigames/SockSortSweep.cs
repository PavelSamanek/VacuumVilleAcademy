using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 1 Minigame: Socks numbered 1-10 are scattered on the floor.
    /// Player drags the vacuum (Rumble) to collect them in ascending order.
    /// </summary>
    public class SockSortSweep : BaseMinigame
    {
        [Header("Sock Sort")]
        [SerializeField] private Transform vacuumTransform;
        [SerializeField] private GameObject sockPrefab;
        [SerializeField] private Transform[] sockSpawnPoints;
        [SerializeField] private TextMeshProUGUI nextNumberLabel;
        [SerializeField] private ParticleSystem collectParticles;

        private List<SockItem> _socks = new();
        private int _nextExpected = 1;
        private bool _dragging;
        private Camera _cam;

        protected override float TimeLimit => 90f;
        protected override int MaxScore   => 10;

        private class SockItem
        {
            public int Number;
            public GameObject Go;
            public bool Collected;
        }

        protected override void OnMinigameBegin()
        {
            _cam = Camera.main;
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

            for (int n = 1; n <= 10; n++)
            {
                var go  = Instantiate(sockPrefab, positions[n - 1], Quaternion.identity);
                var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl) lbl.text = n.ToString();
                _socks.Add(new SockItem { Number = n, Go = go });
            }
        }

        private void Update()
        {
            if (!GameActive) return;

            // Move vacuum to touch/mouse position
            Vector3 worldPos = Vector3.zero;
            bool input = false;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(0))
            {
                worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0;
                input = true;
            }
#else
            if (Input.touchCount > 0)
            {
                worldPos = _cam.ScreenToWorldPoint(Input.GetTouch(0).position);
                worldPos.z = 0;
                input = true;
            }
#endif

            if (input)
            {
                vacuumTransform.position = Vector3.Lerp(
                    vacuumTransform.position, worldPos, Time.deltaTime * 12f);
                CheckCollection();
            }
        }

        private void CheckCollection()
        {
            foreach (var sock in _socks)
            {
                if (sock.Collected) continue;
                if (Vector3.Distance(vacuumTransform.position, sock.Go.transform.position) < 0.6f)
                {
                    if (sock.Number == _nextExpected)
                    {
                        CollectSock(sock);
                    }
                    else
                    {
                        // Wrong order: bounce
                        StartCoroutine(BounceSock(sock.Go.transform));
                        AudioManager.Instance.PlayWrong();
                    }
                }
            }
        }

        private void CollectSock(SockItem sock)
        {
            sock.Collected = true;
            sock.Go.SetActive(false);

            if (collectParticles)
            {
                collectParticles.transform.position = sock.Go.transform.position;
                collectParticles.Play();
            }

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
            Vector3 origin = t.position;
            float duration = 0.3f;
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                float y = Mathf.Abs(Mathf.Sin(elapsed / duration * Mathf.PI)) * 0.5f;
                t.position = origin + new Vector3(0, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.position = origin;
        }
    }
}
