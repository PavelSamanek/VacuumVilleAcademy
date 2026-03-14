using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    public class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private LevelRoomButton[] roomButtons; // one per level

        private void Start()
        {
            if (roomButtons == null || roomButtons.Length == 0)
                roomButtons = GetComponentsInChildren<LevelRoomButton>(includeInactive: true);

            if (roomButtons == null || roomButtons.Length == 0)
            {
                Debug.LogWarning("[LevelSelectController] No LevelRoomButton components found as children.");
                return;
            }

            var gm = GameManager.Instance;
            for (int i = 0; i < roomButtons.Length; i++)
            {
                if (roomButtons[i] == null)
                {
                    Debug.LogWarning($"[LevelSelectController] roomButtons[{i}] is null — assign it in the Inspector.");
                    continue;
                }

                var def = gm.GetLevel(i);
                if (def == null) continue;

                bool unlocked = gm.IsLevelUnlocked(i);
                var lp = gm.Progress.GetOrCreateLevel(i);

                roomButtons[i].Setup(def, lp, unlocked, OnLevelSelected);
            }
        }

        private void OnLevelSelected(LevelDefinition level)
        {
            AudioManager.Instance.PlayButton();
            GameManager.Instance.StartLevel(level);
        }
    }


}
