using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 10 Minigame: Shape tiles fall slowly from the top.
    /// Player taps the correct destination region in the floor mosaic.
    /// Completing the mosaic triggers a celebration.
    /// </summary>
    public class GrandHallRestoration : BaseMinigame
    {
        [Header("Mosaic")]
        [SerializeField] private MosaicRegion[] mosaicRegions;  // one per shape type
        [SerializeField] private GameObject shapeTilePrefab;
        [SerializeField] private Transform tileSpawnLine;
        [SerializeField] private float tileDropSpeed = 1.2f;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private ParticleSystem celebrationParticles;
        [SerializeField] private TextMeshProUGUI shapeNameLabel;

        private FallingTile _currentTile;
        private int _tilesPlaced;
        private int _totalTiles;

        protected override float TimeLimit => 180f;
        protected override int MaxScore   => 30;

        [System.Serializable]
        public class MosaicRegion
        {
            public string shapeKey;     // "triangle", "square", etc.
            public Button button;
            public Image fillImage;
            public int totalSlots;
            public int filledSlots;
        }

        private class FallingTile
        {
            public string ShapeKey;
            public GameObject Go;
            public bool WaitingForPlacement;
        }

        protected override bool IsSetupComplete() =>
            mosaicRegions != null && mosaicRegions.Length > 0;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            _totalTiles = 0;
            foreach (var r in mosaicRegions) _totalTiles += r.totalSlots;

            if (shapeNameLabel == null) shapeNameLabel = CreateHUDLabel("ShapeNameLabel", new Vector2(0, 260), 52f, new Color(0.15f, 0.15f, 0.35f));
            SetupRegionButtons();
            StartCoroutine(TileLoop());
        }

        private TextMeshProUGUI CreateHUDLabel(string name, Vector2 anchoredPos, float fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(600, 70);
            var bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(1f, 1f, 1f, 0.85f);
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void SetupRegionButtons()
        {
            // Reposition regions to bottom strip (4 equal columns)
            float xStep = 1f / Mathf.Max(1, mosaicRegions.Length);
            const float xPad = 0.01f;
            for (int i = 0; i < mosaicRegions.Length; i++)
            {
                var rt = (RectTransform)mosaicRegions[i].button.transform;
                if (rt.parent != transform) rt.SetParent(transform, false);
                rt.anchorMin = new Vector2(i * xStep + xPad, 0.04f);
                rt.anchorMax = new Vector2((i + 1) * xStep - xPad, 0.19f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                // Style the region button
                var img = mosaicRegions[i].button.GetComponent<Image>();
                Color[] regionColors = {
                    new Color(0.27f, 0.51f, 0.93f),  // blue
                    new Color(0.55f, 0.76f, 0.29f),  // green
                    new Color(0.96f, 0.73f, 0.16f),  // yellow
                    new Color(0.91f, 0.35f, 0.35f),  // red
                };
                if (img) img.color = regionColors[Mathf.Min(i, regionColors.Length - 1)];

                // Shape label inside button
                var lbl = mosaicRegions[i].button.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl)
                {
                    lbl.text = LocalizationManager.Instance.Get($"shape_{mosaicRegions[i].shapeKey}");
                    lbl.color = Color.white;
                    lbl.fontSize = 36f;
                    lbl.fontStyle = FontStyles.Bold;
                    lbl.alignment = TextAlignmentOptions.Center;
                    lbl.enableWordWrapping = false;
                }

                var r = mosaicRegions[i];
                mosaicRegions[i].button.onClick.RemoveAllListeners();
                mosaicRegions[i].button.onClick.AddListener(() => OnRegionTapped(r));
            }
        }

        private IEnumerator TileLoop()
        {
            while (GameActive && _tilesPlaced < _totalTiles)
            {
                SpawnNextTile();
                yield return new WaitUntil(() => _currentTile == null || !GameActive);
                yield return new WaitForSeconds(0.3f);
            }
            CompleteEarly();
        }

        private void SpawnNextTile()
        {
            // Pick a region that still needs tiles
            var incomplete = new List<MosaicRegion>();
            foreach (var r in mosaicRegions)
                if (r.filledSlots < r.totalSlots) incomplete.Add(r);
            if (incomplete.Count == 0) return;

            var region = incomplete[Random.Range(0, incomplete.Count)];

            // Canvas is Screen Space Overlay — positions are in screen pixels
            float spawnY  = tileSpawnLine != null ? tileSpawnLine.position.y : Screen.height;
            float spawnX  = tileSpawnLine != null
                ? Random.Range(tileSpawnLine.position.x - 280f, tileSpawnLine.position.x + 280f)
                : Random.Range(Screen.width * 0.1f, Screen.width * 0.9f);

            GameObject go;
            if (shapeTilePrefab != null)
            {
                go = Instantiate(shapeTilePrefab, transform);
            }
            else
            {
                // Build a simple tile at runtime — rounded coloured square with a label
                go = new GameObject("Tile");
                go.transform.SetParent(transform, false);
                var rt2 = go.AddComponent<RectTransform>();
                rt2.sizeDelta = new Vector2(160, 160);
                var tileImg = go.AddComponent<UnityEngine.UI.Image>();
                Color[] tileColors = {
                    new Color(0.27f, 0.51f, 0.93f),
                    new Color(0.55f, 0.76f, 0.29f),
                    new Color(0.96f, 0.73f, 0.16f),
                    new Color(0.91f, 0.35f, 0.35f),
                };
                int colorIdx = System.Array.FindIndex(mosaicRegions, r2 => r2.shapeKey == region.shapeKey);
                tileImg.color = tileColors[Mathf.Max(0, colorIdx) % tileColors.Length];
                var lblGo = new GameObject("Label");
                lblGo.transform.SetParent(go.transform, false);
                var lblRt = lblGo.AddComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
                lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
                var tmp = lblGo.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 52f; tmp.fontStyle = FontStyles.Bold;
                tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
            }

            go.transform.position = new Vector3(spawnX, spawnY, 0);
            // Load the correct shape sprite (triangle, square, circle, etc.)
            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                var spr = Resources.Load<Sprite>($"Sprites/{region.shapeKey}");
                if (spr != null) img.sprite = spr;
            }
            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) { lbl.text = LocalizationManager.Instance.Get($"shape_{region.shapeKey}"); lbl.color = Color.white; }

            if (shapeNameLabel)
                shapeNameLabel.text = LocalizationManager.Instance.Get($"tap_region_{region.shapeKey}");

            AudioManager.Instance.PlayVoice($"shape_{region.shapeKey}");

            MinigameVFX.SpawnPop(this, go.transform);
            AudioManager.Instance?.PlaySFX("Audio/SFX/grandhall/tile_whoosh");
            _currentTile = new FallingTile
            {
                ShapeKey           = region.shapeKey,
                Go                 = go,
                WaitingForPlacement = true
            };

            StartCoroutine(DropTile(_currentTile));
        }

        private IEnumerator DropTile(FallingTile tile)
        {
            float targetY = Screen.height * 0.35f; // hover level (screen pixels, ~35% from bottom)
            while (tile.Go != null && tile.Go.transform.position.y > targetY && tile.WaitingForPlacement)
            {
                tile.Go.transform.position += Vector3.down * tileDropSpeed * Time.deltaTime;
                yield return null;
            }
            // Tile just hovers at targetY waiting for tap
        }

        private void OnRegionTapped(MosaicRegion region)
        {
            if (_currentTile == null || !_currentTile.WaitingForPlacement) return;

            if (region.shapeKey == _currentTile.ShapeKey)
            {
                PlaceTile(region);
            }
            else
            {
                AudioManager.Instance.PlayWrong();
                AudioManager.Instance?.PlaySFX("Audio/SFX/grandhall/tile_bounce");
                NotifyWrong();
                MinigameVFX.ShakeRect(this, (RectTransform)region.button.transform);
                StartCoroutine(BounceAway(_currentTile.Go.transform));
            }
        }

        private void PlaceTile(MosaicRegion region)
        {
            _currentTile.WaitingForPlacement = false;

            region.filledSlots++;
            float fillAmount = (float)region.filledSlots / region.totalSlots;
            if (region.fillImage) region.fillImage.fillAmount = fillAmount;

            AudioManager.Instance.PlayCorrect();
            AudioManager.Instance?.PlaySFX("Audio/SFX/grandhall/tile_snap");
            NotifyCorrect();
            AddScore(1);
            _tilesPlaced++;

            Vector3 regionPos = region.button.transform.position;
            MinigameVFX.PulseRing(this, regionPos, new Color(0.412f, 0.941f, 0.682f));
            MinigameVFX.FloatingText(this, "+1", regionPos, new Color(0.412f, 0.941f, 0.682f));

            StartCoroutine(SnapTileToRegion(_currentTile.Go.transform, regionPos));
            _currentTile = null;

            if (_tilesPlaced >= _totalTiles)
            {
                if (celebrationParticles) celebrationParticles.Play();
                MinigameVFX.ScreenFlash(this, new Color(0.412f, 0.941f, 0.682f), 0.4f, 0.5f);
                CompleteEarly();
            }
        }

        private IEnumerator SnapTileToRegion(Transform tile, Vector3 target)
        {
            float duration = 0.25f;
            float elapsed  = 0f;
            Vector3 start  = tile.position;
            while (elapsed < duration)
            {
                tile.position = Vector3.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(tile.gameObject);
        }

        private IEnumerator BounceAway(Transform tile)
        {
            if (tile == null) yield break;
            Vector3 origin = tile.position;
            Vector3 bounce = origin + new Vector3(Random.Range(-200f, 200f), 100f, 0);
            float   t      = 0f;
            while (t < 0.4f)
            {
                tile.position = Vector3.Lerp(origin, bounce, t / 0.4f);
                t += Time.deltaTime;
                yield return null;
            }
            tile.position = origin;
        }
    }
}
