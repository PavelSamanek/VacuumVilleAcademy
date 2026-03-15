using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    public class MinigameUnlockController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button playButton;

        private void Start()
        {
            if (backButton == null)
                backButton = GameObject.Find("BackButton")?.GetComponent<Button>();
            if (playButton == null)
                playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();

            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
            else
                Debug.LogWarning("[MinigameUnlockController] BackButton not found.");

            if (playButton != null)
                playButton.onClick.AddListener(OnPlay);

            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                if (backButton != null)
                {
                    var tmp = backButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = loc.Get("btn_back");
                }
                if (playButton != null)
                {
                    var tmp = playButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = loc.Get("btn_play");
                }
                LocalizeLabel("Title",        loc.Get("minigame_unlocked"));
                LocalizeLabel("TitleLabel",   loc.Get("minigame_unlocked"));
            }
        }

        private static void LocalizeLabel(string goName, string text)
        {
            var go = GameObject.Find(goName);
            if (go == null) return;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                OnBack();
        }

        private void OnBack()
        {
            AudioManager.Instance?.PlayButton();
            // Reaching this screen means math tasks are done — mark level complete before leaving
            var gm = GameManager.Instance;
            var lp = gm.Progress.GetOrCreateLevel(gm.ActiveLevel.levelIndex);
            if (!lp.completed)
            {
                lp.completed = true;
                VacuumVille.Core.SaveSystem.Save(gm.Progress);
            }
            gm.TransitionTo(GameState.LevelSelect);
        }

        private void OnPlay()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance.UnlockAndStartMinigame();
        }
    }
}
