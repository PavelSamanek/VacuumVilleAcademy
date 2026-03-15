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

        protected override bool IsSetupComplete() => shapeTilePrefab != null;

        protected override void OnMinigameBegin()
        {
            _totalTiles = 0;
            foreach (var r in mosaicRegions) _totalTiles += r.totalSlots;

            SetupRegionButtons();
            StartCoroutine(TileLoop());
        }

        private void SetupRegionButtons()
        {
            foreach (var region in mosaicRegions)
            {
                var r = region;
                region.button.onClick.RemoveAllListeners();
                region.button.onClick.AddListener(() => OnRegionTapped(r));
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
            float xPos = Random.Range(tileSpawnLine.position.x - 280f, tileSpawnLine.position.x + 280f);
            var go  = Instantiate(shapeTilePrefab, transform); // parent to Canvas so UI renders
            go.transform.position = new Vector3(xPos, tileSpawnLine.position.y, 0);
            // Load the correct shape sprite (triangle, square, circle, etc.)
            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                var spr = Resources.Load<Sprite>($"Sprites/{region.shapeKey}");
                if (spr != null) img.sprite = spr;
            }
            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = LocalizationManager.Instance.Get($"shape_{region.shapeKey}");

            if (shapeNameLabel)
                shapeNameLabel.text = LocalizationManager.Instance.Get($"tap_region_{region.shapeKey}");

            AudioManager.Instance.PlayVoice($"shape_{region.shapeKey}");

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
            AddScore(1);
            _tilesPlaced++;

            StartCoroutine(SnapTileToRegion(_currentTile.Go.transform, region.button.transform.position));
            _currentTile = null;

            if (_tilesPlaced >= _totalTiles)
            {
                if (celebrationParticles) celebrationParticles.Play();
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
