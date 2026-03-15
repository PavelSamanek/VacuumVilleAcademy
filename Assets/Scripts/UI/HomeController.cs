using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    public class HomeController : MonoBehaviour
    {
        private SessionBreakMonitor _breakMonitor;

        private void Start()
        {
            _breakMonitor = gameObject.AddComponent<SessionBreakMonitor>();

            WireButton("PlayButton",       () => GameManager.Instance.TransitionTo(GameState.LevelSelect));
            WireButton("CharactersButton", () => GameManager.Instance.TransitionTo(GameState.Achievements));
            WireButton("ParentButton",     () => GameManager.Instance.TransitionTo(GameState.ParentDashboard));
            WireButton("SettingsButton",   () => GameManager.Instance.TransitionTo(GameState.Settings));

            LocalizeUI();
        }

        private void LocalizeUI()
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return;
            SetButtonLabel("PlayButton",       loc.Get("btn_play"));
            SetButtonLabel("CharactersButton", loc.Get("btn_characters"));
            SetButtonLabel("ParentButton",     loc.Get("btn_parents"));
            SetButtonLabel("SettingsButton",   loc.Get("btn_settings"));
        }

        private static void SetButtonLabel(string buttonName, string text)
        {
            var go = GameObject.Find(buttonName);
            if (go == null) return;
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        private void WireButton(string name, System.Action action)
        {
            var go = GameObject.Find(name);
            if (go == null) return;
            var btn = go.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                action();
            });
        }
    }
}
