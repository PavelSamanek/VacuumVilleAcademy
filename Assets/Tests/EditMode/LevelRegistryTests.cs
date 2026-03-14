using System.Linq;
using NUnit.Framework;
using VacuumVille.Data;

namespace VacuumVille.Tests
{
    public class LevelRegistryTests
    {
        [Test]
        public void Registry_HasExactly11Levels()
        {
            Assert.AreEqual(11, LevelRegistry.Levels.Length);
        }

        [Test]
        public void Registry_IndicesAreSequential_ZeroToTen()
        {
            for (int i = 0; i < LevelRegistry.Levels.Length; i++)
                Assert.AreEqual(i, LevelRegistry.Levels[i].index,
                    $"Level at position {i} has index {LevelRegistry.Levels[i].index}");
        }

        [Test]
        public void Registry_AllIndicesUnique()
        {
            var indices = LevelRegistry.Levels.Select(l => l.index).ToList();
            Assert.AreEqual(indices.Count, indices.Distinct().Count(),
                "Duplicate level index found");
        }

        [Test]
        public void Registry_AllTasksRequiredPositive()
        {
            foreach (var lvl in LevelRegistry.Levels)
                Assert.Greater(lvl.tasksRequired, 0,
                    $"Level {lvl.index} has tasksRequired={lvl.tasksRequired}");
        }

        [Test]
        public void Registry_AllNameKeysFollowPattern()
        {
            foreach (var lvl in LevelRegistry.Levels)
            {
                string expected = $"level_{lvl.index + 1}_name";
                Assert.AreEqual(expected, lvl.nameKey,
                    $"Level {lvl.index} has nameKey='{lvl.nameKey}', expected '{expected}'");
            }
        }

        [Test]
        public void Registry_AllRoomKeysNonEmpty()
        {
            foreach (var lvl in LevelRegistry.Levels)
                Assert.IsFalse(string.IsNullOrEmpty(lvl.room),
                    $"Level {lvl.index} has empty room key");
        }

        [Test]
        public void Registry_UnlockAfterLevel_ValidOrMinusOne()
        {
            foreach (var lvl in LevelRegistry.Levels)
            {
                Assert.IsTrue(lvl.unlockAfter == -1 || lvl.unlockAfter < lvl.index,
                    $"Level {lvl.index} unlockAfter={lvl.unlockAfter} must be -1 or a lower index");
            }
        }

        [Test]
        public void Registry_FirstLevel_UnlockedByDefault()
        {
            Assert.AreEqual(-1, LevelRegistry.Levels[0].unlockAfter,
                "The first level must have unlockAfter=-1 (always unlocked)");
        }

        [Test]
        public void Registry_NoTwoLevelsShareTheSameTopic()
        {
            // Each level covers a distinct skill — overlapping topics suggest a registry error
            var topics = LevelRegistry.Levels
                .Where(l => l.topic != MathTopic.MixedReview)
                .Select(l => l.topic)
                .ToList();
            Assert.AreEqual(topics.Count, topics.Distinct().Count(),
                "Two non-MixedReview levels share the same MathTopic");
        }
    }
}
