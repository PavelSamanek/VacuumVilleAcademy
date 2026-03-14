// Stub used by placeholder minigame scenes.
// Immediately completes the minigame so level progression is not blocked.
// Replace with the actual minigame component when the scene is implemented.
using System.Collections;
using UnityEngine;

namespace VacuumVille.Minigames
{
    public class BaseMinigameStub : BaseMinigame
    {
        protected override void OnMinigameBegin()
        {
            StartCoroutine(AutoComplete());
        }

        private IEnumerator AutoComplete()
        {
            yield return new WaitForSeconds(1f);
            AddScore(50);
            FinishMinigame();
        }
    }
}
