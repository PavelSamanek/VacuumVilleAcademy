using UnityEngine;
using UnityEngine.UI;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    public class AchievementsController : MonoBehaviour
    {
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (backButton == null)
            {
                var go = GameObject.Find("BackButton");
                if (go != null) backButton = go.GetComponent<Button>();
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(GoBack);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                GoBack();
        }

        private void GoBack()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.TransitionTo(GameState.Home);
        }
    }
}
