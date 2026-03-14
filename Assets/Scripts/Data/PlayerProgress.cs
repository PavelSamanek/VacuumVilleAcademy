using System;
using System.Collections.Generic;
using UnityEngine;
using VacuumVille.Data;

namespace VacuumVille.Data
{
    [Serializable]
    public class LevelProgress
    {
        public int levelIndex;
        public int stars;                   // 0-3
        public bool minigameUnlocked;
        public bool completed;
        public int bestMinigameScore;
        public bool stickerCollected;

        // Accuracy tracking
        public int totalAttempts;
        public int firstAttemptCorrect;
        public float FirstAttemptAccuracy => totalAttempts > 0
            ? (float)firstAttemptCorrect / totalAttempts * 100f
            : 0f;
    }

    [Serializable]
    public class TopicAccuracy
    {
        public MathTopic topic;
        public int totalProblems;
        public int correctFirstAttempt;
        public Difficulty currentDifficulty = Difficulty.Easy;

        // Rolling window for adaptive difficulty (last 10 problems)
        public Queue<bool> recentResults = new Queue<bool>(10);

        public float RollingAccuracy
        {
            get
            {
                if (recentResults.Count == 0) return 0.75f;
                int correct = 0;
                foreach (bool r in recentResults) if (r) correct++;
                return (float)correct / recentResults.Count;
            }
        }

        public void RecordResult(bool firstAttemptCorrect)
        {
            totalProblems++;
            if (firstAttemptCorrect) this.correctFirstAttempt++;

            if (recentResults.Count >= 10) recentResults.Dequeue();
            recentResults.Enqueue(firstAttemptCorrect);
        }
    }

    [Serializable]
    public class PlayerProgress
    {
        public CharacterType selectedCharacter = CharacterType.Rumble;
        public Language selectedLanguage = Language.Czech;
        public int coins;
        public int totalStars;

        public List<LevelProgress> levels = new List<LevelProgress>();
        public List<TopicAccuracy> topicAccuracies = new List<TopicAccuracy>();
        public List<CharacterType> unlockedCharacters = new List<CharacterType> { CharacterType.Rumble };
        public List<int> collectedStickers = new List<int>();

        // Session stats
        public DateTime lastSessionDate;
        public int totalMinutesPlayed;

        public LevelProgress GetOrCreateLevel(int levelIndex)
        {
            var lp = levels.Find(l => l.levelIndex == levelIndex);
            if (lp == null)
            {
                lp = new LevelProgress { levelIndex = levelIndex };
                levels.Add(lp);
            }
            return lp;
        }

        public TopicAccuracy GetOrCreateTopicAccuracy(MathTopic topic)
        {
            var ta = topicAccuracies.Find(t => t.topic == topic);
            if (ta == null)
            {
                ta = new TopicAccuracy { topic = topic };
                topicAccuracies.Add(ta);
            }
            return ta;
        }

        public bool IsLevelUnlocked(int levelIndex, LevelDefinition[] allLevels)
        {
            if (levelIndex == 0) return true;
            var def = Array.Find(allLevels, l => l.levelIndex == levelIndex);
            if (def == null) return false;
            if (def.unlockAfterLevel < 0) return true;

            var prev = GetOrCreateLevel(def.unlockAfterLevel);
            return prev.completed;
        }

        public bool IsCharacterUnlocked(CharacterType type)
            => unlockedCharacters.Contains(type);

        public void UnlockCharacter(CharacterType type)
        {
            if (!unlockedCharacters.Contains(type))
                unlockedCharacters.Add(type);
        }
    }
}
