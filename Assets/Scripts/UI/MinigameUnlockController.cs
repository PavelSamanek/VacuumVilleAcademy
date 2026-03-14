using UnityEngine;
using UnityEngine.UI;
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
