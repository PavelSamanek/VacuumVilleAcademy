using UnityEngine;
using VacuumVille.Data;

namespace VacuumVille.Core
{
    /// <summary>
    /// Ensures singleton managers exist before any scene loads.
    /// Runs automatically via RuntimeInitializeOnLoadMethod — no scene setup needed.
    /// This makes every scene independently playable in the editor.
    /// </summary>
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureManagers()
        {
            if (GameManager.Instance != null) return; // Already initialized (normal boot)

            // Mark as bootstrapped so GameManager.Start() doesn't redirect to LanguageSelect
            GameManager.IsBootstrapped = true;

            // LocalizationManager
            var locGo = new GameObject("[LocalizationManager]");
            Object.DontDestroyOnLoad(locGo);
            var lm = locGo.AddComponent<LocalizationManager>();
            var lang = (Language)PlayerPrefs.GetInt("lang", (int)Language.Czech);
            lm.SetLanguage(lang);

            // AudioManager
            var audioGo = new GameObject("[AudioManager]");
            Object.DontDestroyOnLoad(audioGo);
            audioGo.AddComponent<AudioManager>();

            // PersistentBackground — gradient + ambient particles behind every scene
            var bgGo = new GameObject("[PersistentBackground]");
            Object.DontDestroyOnLoad(bgGo);
            bgGo.AddComponent<PersistentBackground>();

            // GameManager (last — it references the others)
            var gmGo = new GameObject("[GameManager]");
            Object.DontDestroyOnLoad(gmGo);
            var gm = gmGo.AddComponent<GameManager>();
            gm.Progress.selectedLanguage = lang;
        }
    }
}
