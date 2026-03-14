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
            GameManager.Instance.TransitionTo(GameState.LevelSelect);
        }

        private void OnPlay()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance.UnlockAndStartMinigame();
        }
    }
}
