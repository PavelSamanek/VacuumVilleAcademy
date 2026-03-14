using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace VacuumVille.Tests
{
    /// <summary>
    /// Checks that both localization JSON files contain all keys the game will look up at runtime.
    /// Reads the files directly from disk so no MonoBehaviour or Resources.Load is needed.
    /// </summary>
    public class LocalizationCoverageTests
    {
        private string _czechJson;
        private string _englishJson;

        [SetUp]
        public void LoadFiles()
        {
            string root = Application.dataPath;
            string czechPath   = Path.Combine(root, "Resources/Localization/cs-CZ/strings.json");
            string englishPath = Path.Combine(root, "Resources/Localization/en-US/strings.json");

            Assert.IsTrue(File.Exists(czechPath),   $"cs-CZ strings.json not found at {czechPath}");
            Assert.IsTrue(File.Exists(englishPath), $"en-US strings.json not found at {englishPath}");

            _czechJson   = File.ReadAllText(czechPath);
            _englishJson = File.ReadAllText(englishPath);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void AssertKey(string key)
        {
            string search = $"\"key\": \"{key}\"";
            Assert.IsTrue(_czechJson.Contains(search),
                $"cs-CZ strings.json is missing key: {key}");
            Assert.IsTrue(_englishJson.Contains(search),
                $"en-US strings.json is missing key: {key}");
        }

        // ── Level names ──────────────────────────────────────────────────────

        [Test]
        public void LevelNames_AllElevenPresent()
        {
            for (int i = 1; i <= 11; i++)
                AssertKey($"level_{i}_name");
        }

        // ── Room names ───────────────────────────────────────────────────────

        [Test]
        public void RoomNames_AllPresent()
        {
            string[] rooms = {
                "room_bedroom", "room_kitchen", "room_livingroom", "room_bathroom",
                "room_garage",  "room_hallway", "room_backyard",   "room_attic",
                "room_rooftop", "room_grandhall", "room_secretlab"
            };
            foreach (var key in rooms)
                AssertKey(key);
        }

        // ── Math question keys ───────────────────────────────────────────────

        [Test]
        public void QuestionKeys_AllPresent()
        {
            string[] keys = {
                "q_how_many", "q_addition", "q_subtraction", "q_missing_addition",
                "q_multiplication", "q_division", "q_ordering", "q_count_shapes"
            };
            foreach (var key in keys)
                AssertKey(key);
        }

        // ── Feedback keys ────────────────────────────────────────────────────

        [Test]
        public void FeedbackKeys_AllPresent()
        {
            string[] keys = {
                "feedback_try_again", "feedback_correct_was",
                "streak_label", "hint_button", "hint_count"
            };
            foreach (var key in keys)
                AssertKey(key);
        }

        // ── Reteach keys ─────────────────────────────────────────────────────

        [Test]
        public void ReteachKeys_AllTopicsPresent()
        {
            // MixedReview has no reteach panel — all other topics must have one
            string[] topics = {
                "Counting1To10", "AdditionTo10", "SubtractionWithin10",
                "AdditionTo20",  "SubtractionWithin20", "Multiplication2x5x",
                "DivisionBy2_3_5", "NumberOrdering", "ShapeCounting"
            };
            foreach (var t in topics)
                AssertKey($"reteach_{t}");
        }

        // ── UI button labels ─────────────────────────────────────────────────

        [Test]
        public void ButtonLabels_CorePresent()
        {
            string[] keys = {
                "btn_play", "btn_next", "btn_back", "btn_home",
                "btn_replay", "btn_settings", "btn_start"
            };
            foreach (var key in keys)
                AssertKey(key);
        }

        // ── Character keys ───────────────────────────────────────────────────

        [Test]
        public void CharacterKeys_AllPresent()
        {
            string[] chars = { "rumble", "xixi", "rocky", "bubbles", "max", "zara", "turbo", "pebble", "luna" };
            foreach (var c in chars)
            {
                AssertKey($"char_{c}_name");
                AssertKey($"char_{c}_catchphrase");
            }
        }

        // ── Shape names ──────────────────────────────────────────────────────

        [Test]
        public void ShapeNames_AllPresent()
        {
            string[] shapes = { "triangle", "square", "circle", "rectangle", "pentagon", "hexagon" };
            foreach (var s in shapes)
                AssertKey($"shape_{s}");
        }
    }
}
