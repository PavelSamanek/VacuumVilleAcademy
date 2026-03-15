using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    public class CharacterSelectController : MonoBehaviour
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

            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                if (backButton != null)
                {
                    var tmp = backButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = loc.Get("btn_back");
                }
                LocalizeLabel("Title",      loc.Get("characters_title"));
                LocalizeLabel("TitleLabel", loc.Get("characters_title"));
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
                GoBack();
        }

        private void GoBack()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.TransitionTo(GameState.Home);
        }
    }
}
