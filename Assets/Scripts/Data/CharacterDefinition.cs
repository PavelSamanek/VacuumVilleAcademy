using UnityEngine;
using VacuumVille.Data;

namespace VacuumVille.Data
{
    public enum CharacterSkill
    {
        SpeedBoost,         // Rumble   - speed boost in timed minigames
        HintReveal,         // Xixi     - reveals one wrong answer
        Shield,             // Rocky    - one mistake protection
        DoublePoints,       // Bubbles  - double points on first-attempt correct
        ScoreMultiplier,    // Max      - score multiplier in minigames
        VisualHint,         // Zara     - shows diagram for word problems
        TaskSkip,           // Turbo    - skip one task per level (earned)
        ExtendedTimer,      // Pebble   - extended timer in timed minigames
        SequenceReveal      // Luna     - briefly shows correct sequence
    }

    [CreateAssetMenu(fileName = "Character_Rumble", menuName = "VacuumVille/Character Definition")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public CharacterType characterType;
        public string nameKey;              // localization key
        public string catchphraseKey;       // localization key
        public string brandName;            // e.g. "Roomba e5"

        [Header("Skill")]
        public CharacterSkill skill;
        public string skillDescriptionKey;  // localization key

        [Header("Unlock")]
        public int unlockAfterLevelIndex;   // 0 = always unlocked (Rumble)

        [Header("Assets")]
        public RuntimeAnimatorController animatorController;
        public Sprite thumbnailSprite;
        public AudioClip[] voiceLines;      // indexed by voice line ID
    }
}
