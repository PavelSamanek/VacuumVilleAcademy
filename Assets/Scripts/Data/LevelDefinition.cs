using UnityEngine;
using VacuumVille.Data;

namespace VacuumVille.Data
{
    [CreateAssetMenu(fileName = "Level_00", menuName = "VacuumVille/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        public int levelIndex;
        public string levelNameKey;        // localization key e.g. "level_1_name"
        public string roomNameKey;         // localization key e.g. "room_bedroom"
        public string narrativeIntroKey;   // localization key for comic strip text

        [Header("Math")]
        public MathTopic mathTopic;
        public Difficulty startingDifficulty = Difficulty.Easy;
        public int tasksRequiredToUnlockMinigame = 10;

        [Header("Minigame")]
        public MinigameType minigameType;
        public string minigameNameKey;
        public string minigameDescriptionKey;

        [Header("Character")]
        public CharacterType assignedCharacter;

        [Header("Progression")]
        public int unlockAfterLevel = -1;  // -1 = always unlocked
        public int starThreshold2 = 70;    // % first-attempt accuracy for 2 stars
        public int starThreshold3 = 90;    // % first-attempt accuracy for 3 stars
        public string stickerSpriteKey;
    }
}
