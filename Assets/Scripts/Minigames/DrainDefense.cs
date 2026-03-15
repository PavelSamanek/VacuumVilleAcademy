using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 4 Minigame: Rubber ducks float toward 5 drains.
    /// Player swipes/tilts Bubbles to block drains. Each duck has a point value.
    /// 60 seconds. Score = points saved.
    /// </summary>
    public class DrainDefense : BaseMinigame
    {
        [Header("Drain Defense")]
        [SerializeField] private Transform[] drainPositions;    // 5 drains
        [SerializeField] private Transform vacuumTransform;
        [SerializeField] private GameObject duckPrefab;
        [SerializeField] private float duckSpawnInterval = 1.5f;
        [SerializeField] private float duckSpeed = 1.5f;
        [SerializeField] private TextMeshProUGUI savedPointsLabel;

        private int _savedPoints;
        private int _drainedPoints;
        private List<DuckInstance> _activeDucks = new();
        private Camera _cam;

        protected override float TimeLimit => 60f;
        protected override int MaxScore   => 50;

        private class DuckInstance
        {
            public GameObject Go;
            public int Points;
            public int TargetDrain;
            public bool Blocked;
        }

        protected override bool IsSetupComplete() =>
            duckPrefab != null && drainPositions != null && drainPositions.Length > 0;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            _cam = Camera.main;
            if (savedPointsLabel == null) savedPointsLabel = CreateHUDLabel();
            StartCoroutine(SpawnDucks());
        }

        private TextMeshProUGUI CreateHUDLabel()
        {
            var go = new GameObject("SavedPointsLabel");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.89f);
            rt.anchorMax = new Vector2(1f, 0.97f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0.5f, 0.7f, 0.8f);
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

        private IEnumerator SpawnDucks()
        {
            while (GameActive)
            {
                SpawnDuck();
                yield return new WaitForSeconds(duckSpawnInterval);
            }
        }

        private void SpawnDuck()
        {
            int drain    = Random.Range(0, drainPositions.Length);
            int points   = Random.Range(1, 6);
            float spawnX = drainPositions[drain].position.x;
            // Spawn well above the screen (canvas is Screen Space Overlay, positions are in pixels)
            float spawnY = drainPositions[drain].position.y + 1400f;

            var go  = Instantiate(duckPrefab, transform); // must be child of Canvas to render
            go.transform.position = new Vector3(spawnX, spawnY, 0);
            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = points.ToString();
            MinigameVFX.SpawnPop(this, go.transform);

            _activeDucks.Add(new DuckInstance { Go = go, Points = points, TargetDrain = drain });
            AudioManager.Instance?.PlaySFX("Audio/SFX/draindefense/duck_quack");
        }

        private void Update()
        {
            if (!GameActive) return;

            HandleVacuumInput();
            MoveDucks();
            CheckBlocking();
        }

        private void HandleVacuumInput()
        {
            Vector3 target = vacuumTransform.position;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(0))
            {
                // Canvas is Screen Space Overlay — screen pixels == UI world position
                target.x = Input.mousePosition.x;
                target.y = vacuumTransform.position.y;
            }
#else
            if (Input.touchCount > 0)
            {
                target.x = Input.GetTouch(0).position.x;
                target.y = vacuumTransform.position.y;
            }
            else
            {
                float tilt = Input.acceleration.x;
                target.x += tilt * 400f * Time.deltaTime;
            }
#endif

            float minX = drainPositions[0].position.x - 60f;
            float maxX = drainPositions[drainPositions.Length - 1].position.x + 60f;
            target.x = Mathf.Clamp(target.x, minX, maxX);
            vacuumTransform.position = Vector3.Lerp(vacuumTransform.position, target, Time.deltaTime * 10f);
        }

        private void MoveDucks()
        {
            for (int i = _activeDucks.Count - 1; i >= 0; i--)
            {
                var duck = _activeDucks[i];
                if (duck.Blocked || duck.Go == null) continue;

                duck.Go.transform.position += Vector3.down * duckSpeed * Time.deltaTime;

                // Reached drain
                if (duck.Go.transform.position.y <= drainPositions[duck.TargetDrain].position.y)
                {
                    DuckDrained(duck);
                    _activeDucks.RemoveAt(i);
                }
            }
        }

        private void CheckBlocking()
        {
            for (int i = _activeDucks.Count - 1; i >= 0; i--)
            {
                var duck = _activeDucks[i];
                if (duck.Blocked) continue;

                float dist = Vector3.Distance(vacuumTransform.position, duck.Go.transform.position);
                if (dist < 80f)
                {
                    DuckBlocked(duck);
                    _activeDucks.RemoveAt(i);
                }
            }
        }

        private void DuckBlocked(DuckInstance duck)
        {
            _savedPoints += duck.Points;
            AddScore(duck.Points);
            AudioManager.Instance.PlayCorrect();
            AudioManager.Instance?.PlaySFX("Audio/SFX/draindefense/duck_splash");
            if (savedPointsLabel)
                savedPointsLabel.text = LocalizationManager.Instance.Get("saved_points", _savedPoints);

            if (duck.Go != null)
            {
                Vector3 pos = duck.Go.transform.position;
                MinigameVFX.PulseRing(this, pos, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.FloatingText(this, "+" + duck.Points, pos, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.CollectBurst(this, duck.Go, new Color(0.412f, 0.941f, 0.682f));
                duck.Go = null;
            }
        }

        private void DuckDrained(DuckInstance duck)
        {
            _drainedPoints += duck.Points;
            AudioManager.Instance.PlayWrong();
            AudioManager.Instance?.PlaySFX("Audio/SFX/draindefense/duck_drain");
            if (duck.Go != null)
            {
                MinigameVFX.PulseRing(this, duck.Go.transform.position, new Color(1f, 0.569f, 0f));
                MinigameVFX.ShakeRect(this, (RectTransform)vacuumTransform);
                MinigameVFX.CollectBurst(this, duck.Go, new Color(1f, 0.569f, 0f));
                duck.Go = null;
            }
        }

        protected override void OnMinigameEnd()
        {
            foreach (var d in _activeDucks)
                if (d.Go) Destroy(d.Go);
        }
    }
}
