using NUnit.Framework;
using VacuumVille.Data;

namespace VacuumVille.Tests
{
    public class PlayerProgressTests
    {
        // ── GetOrCreateLevel ─────────────────────────────────────────────────

        [Test]
        public void GetOrCreateLevel_CreatesEntryIfMissing()
        {
            var p = new PlayerProgress();
            var lp = p.GetOrCreateLevel(3);
            Assert.IsNotNull(lp);
            Assert.AreEqual(3, lp.levelIndex);
        }

        [Test]
        public void GetOrCreateLevel_ReturnsSameInstance()
        {
            var p = new PlayerProgress();
            var first  = p.GetOrCreateLevel(5);
            var second = p.GetOrCreateLevel(5);
            Assert.AreSame(first, second);
        }

        [Test]
        public void GetOrCreateLevel_DoesNotDuplicateEntries()
        {
            var p = new PlayerProgress();
            p.GetOrCreateLevel(2);
            p.GetOrCreateLevel(2);
            p.GetOrCreateLevel(2);
            Assert.AreEqual(1, p.levels.Count);
        }

        // ── GetOrCreateTopicAccuracy ─────────────────────────────────────────

        [Test]
        public void GetOrCreateTopicAccuracy_CreatesIfMissing()
        {
            var p  = new PlayerProgress();
            var ta = p.GetOrCreateTopicAccuracy(MathTopic.AdditionTo10);
            Assert.IsNotNull(ta);
            Assert.AreEqual(MathTopic.AdditionTo10, ta.topic);
        }

        [Test]
        public void GetOrCreateTopicAccuracy_ReturnsSameInstance()
        {
            var p  = new PlayerProgress();
            var t1 = p.GetOrCreateTopicAccuracy(MathTopic.Counting1To10);
            var t2 = p.GetOrCreateTopicAccuracy(MathTopic.Counting1To10);
            Assert.AreSame(t1, t2);
        }

        // ── TopicAccuracy.RollingAccuracy ────────────────────────────────────

        [Test]
        public void RollingAccuracy_EmptyQueue_ReturnsDefault()
        {
            var ta = new TopicAccuracy();
            Assert.AreEqual(0.75f, ta.RollingAccuracy, 0.001f,
                "Empty queue should return 0.75 (neutral default)");
        }

        [Test]
        public void RollingAccuracy_AllCorrect_ReturnsOne()
        {
            var ta = new TopicAccuracy();
            for (int i = 0; i < 10; i++) ta.RecordResult(true);
            Assert.AreEqual(1.0f, ta.RollingAccuracy, 0.001f);
        }

        [Test]
        public void RollingAccuracy_AllWrong_ReturnsZero()
        {
            var ta = new TopicAccuracy();
            for (int i = 0; i < 10; i++) ta.RecordResult(false);
            Assert.AreEqual(0.0f, ta.RollingAccuracy, 0.001f);
        }

        [Test]
        public void RollingAccuracy_HalfCorrect_ReturnsHalf()
        {
            var ta = new TopicAccuracy();
            for (int i = 0; i < 10; i++) ta.RecordResult(i % 2 == 0);
            Assert.AreEqual(0.5f, ta.RollingAccuracy, 0.001f);
        }

        [Test]
        public void RollingAccuracy_SlidingWindowCaps_AtTen()
        {
            var ta = new TopicAccuracy();
            // Record 10 wrong, then 10 correct — window should reflect only recent 10
            for (int i = 0; i < 10; i++) ta.RecordResult(false);
            for (int i = 0; i < 10; i++) ta.RecordResult(true);
            Assert.AreEqual(1.0f, ta.RollingAccuracy, 0.001f,
                "Rolling window should have evicted the 10 wrong answers");
        }

        // ── TopicAccuracy.RecordResult ───────────────────────────────────────

        [Test]
        public void RecordResult_IncrementsTotalProblems()
        {
            var ta = new TopicAccuracy();
            ta.RecordResult(true);
            ta.RecordResult(false);
            ta.RecordResult(true);
            Assert.AreEqual(3, ta.totalProblems);
        }

        [Test]
        public void RecordResult_IncrementsCorrectFirstAttemptOnlyWhenTrue()
        {
            var ta = new TopicAccuracy();
            ta.RecordResult(true);
            ta.RecordResult(false);
            ta.RecordResult(true);
            Assert.AreEqual(2, ta.correctFirstAttempt);
        }

        // ── LevelProgress unlock logic ───────────────────────────────────────

        [Test]
        public void IsLevelUnlocked_LevelZero_AlwaysTrue()
        {
            var p = new PlayerProgress();
            // Level 0 has no prerequisites
            var level0 = new LevelDefinition();
            level0.levelIndex = 0;
            level0.unlockAfterLevel = -1;
            Assert.IsTrue(p.IsLevelUnlocked(0, new[] { level0 }));
        }

        [Test]
        public void IsLevelUnlocked_Level1_LockedUntilLevel0Completed()
        {
            var p = new PlayerProgress();
            var level0 = new LevelDefinition { levelIndex = 0, unlockAfterLevel = -1 };
            var level1 = new LevelDefinition { levelIndex = 1, unlockAfterLevel = 0 };
            var allLevels = new[] { level0, level1 };

            Assert.IsFalse(p.IsLevelUnlocked(1, allLevels), "Level 1 should be locked before level 0 completed");

            p.GetOrCreateLevel(0).completed = true;
            Assert.IsTrue(p.IsLevelUnlocked(1, allLevels), "Level 1 should unlock after level 0 completed");
        }

        [Test]
        public void IsLevelUnlocked_MissingDefinition_ReturnsFalse()
        {
            var p = new PlayerProgress();
            Assert.IsFalse(p.IsLevelUnlocked(99, new LevelDefinition[0]),
                "Unknown level index should return false");
        }

        // ── LevelProgress ────────────────────────────────────────────────────

        [Test]
        public void FirstAttemptAccuracy_ZeroAttempts_ReturnsZero()
        {
            var lp = new LevelProgress();
            Assert.AreEqual(0f, lp.FirstAttemptAccuracy, 0.001f);
        }

        [Test]
        public void FirstAttemptAccuracy_CalculatesCorrectly()
        {
            var lp = new LevelProgress { totalAttempts = 4, firstAttemptCorrect = 3 };
            Assert.AreEqual(75f, lp.FirstAttemptAccuracy, 0.001f);
        }
    }
}
