using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    /// <summary>
    /// Brief intro screen shown before math tasks begin.
    /// Shows the level name. Tap anywhere to start, or press Back to return.
    /// </summary>
    public class LevelIntroController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        private bool _ready;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Auto-find title label
            if (titleLabel == null)
            {
                var go = GameObject.Find("Title");
                if (go != null) titleLabel = go.GetComponent<TextMeshProUGUI>();
            }

            // Set level name
            if (titleLabel != null && gm.ActiveLevel != null)
                titleLabel.text = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.Get(gm.ActiveLevel.levelNameKey)
                    : gm.ActiveLevel.levelNameKey;

            // Auto-find buttons
            if (startButton == null) startButton = FindButton("StartButton");
            if (backButton  == null) backButton  = FindButton("BackButton");

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(StartLevel);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(GoBack);
            }

            _ready = true;
        }

        private void Update()
        {
            if (!_ready) return;

            // Android back button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoBack();
                return;
            }

            // Tap anywhere to start (if no explicit start button)
            if (startButton == null && Input.GetMouseButtonDown(0))
                StartLevel();
        }

        private void StartLevel()
        {
            _ready = false;
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.BeginMathTasks();
        }

        private void GoBack()
        {
            _ready = false;
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.TransitionTo(GameState.LevelSelect);
        }

        private static Button FindButton(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }
    }
}
