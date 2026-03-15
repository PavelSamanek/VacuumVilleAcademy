using UnityEngine;

namespace VacuumVille.Core
{
    /// <summary>
    /// Disables the AudioListener on this GameObject's camera at startup.
    /// AudioManager carries a permanent AudioListener on its DontDestroyOnLoad
    /// object, so scene cameras don't need one. Add this to every scene camera.
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public class DisableRedundantListener : MonoBehaviour
    {
        private void Awake()
        {
            var al = GetComponent<AudioListener>();
            if (al != null) al.enabled = false;
        }
    }
}
