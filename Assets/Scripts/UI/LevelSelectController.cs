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
            if (gm == null) { Debug.LogError("[LevelSelectController] GameManager.Instance is null!"); return; }

            Debug.Log($"[LevelSelectController] Setting up {roomButtons.Length} buttons. AllLevels count: {gm.AllLevels?.Length}");

            for (int i = 0; i < roomButtons.Length; i++)
            {
                if (roomButtons[i] == null)
                {
                    Debug.LogWarning($"[LevelSelectController] roomButtons[{i}] is null.");
                    continue;
                }

                var def = gm.GetLevel(i);
                if (def == null) { Debug.LogWarning($"[LevelSelectController] No level definition for index {i}"); continue; }

                bool unlocked = gm.IsLevelUnlocked(i);
                var lp = gm.Progress.GetOrCreateLevel(i);

                Debug.Log($"[LevelSelectController] Button {i}: {def.levelNameKey}, unlocked={unlocked}");
                roomButtons[i].Setup(def, lp, unlocked, OnLevelSelected);
            }
        }

        private void OnLevelSelected(LevelDefinition level)
        {
            Debug.Log($"[LevelSelectController] Level selected: {level.levelNameKey}");
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.StartLevel(level);
        }
    }


}
