using UnityEngine;
using UnityEngine.UI;
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
