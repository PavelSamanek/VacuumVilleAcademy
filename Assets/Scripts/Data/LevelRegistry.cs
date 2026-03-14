using UnityEngine;
using VacuumVille.Data;

namespace VacuumVille.Data
{
    /// <summary>
    /// Runtime registry of all level definitions. Used where ScriptableObject
    /// assets cannot be loaded via Resources (e.g. editor tooling, tests).
    /// Each entry mirrors what the LevelDefinition ScriptableAssets contain.
    /// </summary>
    public static class LevelRegistry
    {
        public static readonly (int index, string nameKey, string room, MathTopic topic,
            int tasksRequired, MinigameType minigame, CharacterType character,
            int unlockAfter, Difficulty startDiff)[] Levels =
        {
            ( 0, "level_1_name",  "room_bedroom",    MathTopic.Counting1To10,        8,  MinigameType.SockSortSweep,          CharacterType.Rumble,          -1, Difficulty.Easy   ),
            ( 1, "level_2_name",  "room_kitchen",    MathTopic.Counting1To20,         8,  MinigameType.CrumbCollectCountdown,  CharacterType.Xixi,             0, Difficulty.Easy   ),
            ( 2, "level_3_name",  "room_livingroom", MathTopic.AdditionTo10,          10, MinigameType.CushionCannonCatch,     CharacterType.Rocky,            1, Difficulty.Easy   ),
            ( 3, "level_4_name",  "room_bathroom",   MathTopic.SubtractionWithin10,   10, MinigameType.DrainDefense,           CharacterType.Bubbles,          2, Difficulty.Easy   ),
            ( 4, "level_5_name",  "room_garage",     MathTopic.AdditionTo20,          10, MinigameType.BoxTowerBuilder,        CharacterType.Max,              3, Difficulty.Medium ),
            ( 5, "level_6_name",  "room_hallway",    MathTopic.SubtractionWithin20,   12, MinigameType.StreamerUntangleSprint, CharacterType.Zara,             4, Difficulty.Medium ),
            ( 6, "level_7_name",  "room_backyard",   MathTopic.Multiplication2x5x,    12, MinigameType.FlowerBedFrenzy,        CharacterType.Turbo,            5, Difficulty.Medium ),
            ( 7, "level_8_name",  "room_attic",      MathTopic.DivisionBy2_3_5,       12, MinigameType.AtticBinBlitz,          CharacterType.ProfessorPebble,  6, Difficulty.Medium ),
            ( 8, "level_9_name",  "room_rooftop",    MathTopic.NumberOrdering,        12, MinigameType.SequenceSprinkler,      CharacterType.Luna,             7, Difficulty.Medium ),
            ( 9, "level_10_name", "room_grandhall",  MathTopic.ShapeCounting,         15, MinigameType.GrandHallRestoration,   CharacterType.Rumble,           8, Difficulty.Hard   ),
            (10, "level_11_name", "room_secretlab",  MathTopic.MixedReview,           15, MinigameType.ScramblerShutdown,      CharacterType.Rumble,           9, Difficulty.Hard   ),
        };
    }
}
