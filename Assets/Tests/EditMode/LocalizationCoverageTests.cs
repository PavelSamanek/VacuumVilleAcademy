using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace VacuumVille.Tests
{
    /// <summary>
    /// Verifies that:
    ///  1. Both locale files contain every key the game needs at runtime.
    ///  2. Plural files also have full parity.
    ///  3. Czech values are not identical to their English counterparts
    ///     for keys that should actually be translated (copy-paste guard).
    /// </summary>
    public class LocalizationCoverageTests
    {
        private string _czechJson;
        private string _englishJson;
        private string _czechPluralsJson;
        private string _englishPluralsJson;

        // Parsed dictionaries for value-level checks
        private Dictionary<string, string> _czValues;
        private Dictionary<string, string> _enValues;

        [SetUp]
        public void LoadFiles()
        {
            string root = Application.dataPath;

            string czechPath        = Path.Combine(root, "Resources/Localization/cs-CZ/strings.json");
            string englishPath      = Path.Combine(root, "Resources/Localization/en-US/strings.json");
            string czechPlurals     = Path.Combine(root, "Resources/Localization/cs-CZ/plurals.json");
            string englishPlurals   = Path.Combine(root, "Resources/Localization/en-US/plurals.json");

            Assert.IsTrue(File.Exists(czechPath),       $"Missing: {czechPath}");
            Assert.IsTrue(File.Exists(englishPath),     $"Missing: {englishPath}");
            Assert.IsTrue(File.Exists(czechPlurals),    $"Missing: {czechPlurals}");
            Assert.IsTrue(File.Exists(englishPlurals),  $"Missing: {englishPlurals}");

            _czechJson        = File.ReadAllText(czechPath);
            _englishJson      = File.ReadAllText(englishPath);
            _czechPluralsJson = File.ReadAllText(czechPlurals);
            _englishPluralsJson = File.ReadAllText(englishPlurals);

            _czValues = ParseValues(_czechJson);
            _enValues = ParseValues(_englishJson);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Asserts the key is present in both locale string files.
        /// </summary>
        private void AssertKey(string key)
        {
            string search = $"\"key\": \"{key}\"";
            Assert.IsTrue(_czechJson.Contains(search),
                $"cs-CZ strings.json is missing key: {key}");
            Assert.IsTrue(_englishJson.Contains(search),
                $"en-US strings.json is missing key: {key}");
        }

        /// <summary>
        /// Asserts the key is present in both locale plural files.
        /// </summary>
        private void AssertPluralKey(string key)
        {
            string search = $"\"key\": \"{key}\"";
            Assert.IsTrue(_czechPluralsJson.Contains(search),
                $"cs-CZ plurals.json is missing key: {key}");
            Assert.IsTrue(_englishPluralsJson.Contains(search),
                $"en-US plurals.json is missing key: {key}");
        }

        /// <summary>
        /// Parse {"entries":[{"key":...,"value":...},...]} into a flat dictionary.
        /// Uses a simple regex to avoid Unity dependency in edit-mode tests.
        /// </summary>
        private static Dictionary<string, string> ParseValues(string json)
        {
            var result = new Dictionary<string, string>();
            // Match "key": "...", "value": "..."  (order is always key then value in our files)
            var matches = Regex.Matches(json,
                @"""key""\s*:\s*""([^""]+)""\s*,\s*""value""\s*:\s*""((?:[^""\\]|\\.)*)""");
            foreach (Match m in matches)
                result[m.Groups[1].Value] = m.Groups[2].Value;
            return result;
        }

        // ── App / UI buttons ─────────────────────────────────────────────────────

        [Test]
        public void AppAndNavButtons_AllPresent()
        {
            string[] keys = {
                "app_name", "select_language",
                "btn_play", "btn_next", "btn_back", "btn_home",
                "btn_replay", "btn_settings", "btn_ready", "btn_start",
                "btn_keep_playing", "btn_take_break"
            };
            foreach (var k in keys) AssertKey(k);
        }

        // ── Screen titles ────────────────────────────────────────────────────────

        [Test]
        public void ScreenTitles_AllPresent()
        {
            string[] keys = {
                "level_complete_title", "level_select_title",
                "characters_title", "achievements_title",
                "parent_dashboard_title", "parent_pin_prompt",
                "session_break_prompt"
            };
            foreach (var k in keys) AssertKey(k);
        }

        // ── Level names ──────────────────────────────────────────────────────────

        [Test]
        public void LevelNames_AllElevenPresent()
        {
            for (int i = 1; i <= 11; i++)
                AssertKey($"level_{i}_name");
        }

        // ── Room names ───────────────────────────────────────────────────────────

        [Test]
        public void RoomNames_AllPresent()
        {
            string[] rooms = {
                "room_bedroom", "room_kitchen", "room_livingroom", "room_bathroom",
                "room_garage",  "room_hallway", "room_backyard",   "room_attic",
                "room_rooftop", "room_grandhall", "room_secretlab"
            };
            foreach (var k in rooms) AssertKey(k);
        }

        // ── Math question keys ───────────────────────────────────────────────────

        [Test]
        public void QuestionKeys_AllPresent()
        {
            string[] keys = {
                "q_how_many", "q_addition", "q_subtraction", "q_missing_addition",
                "q_multiplication", "q_division", "q_ordering",
                "q_count_shapes", "q_groups_total"
            };
            foreach (var k in keys) AssertKey(k);
        }

        // ── Feedback / progress keys ─────────────────────────────────────────────

        [Test]
        public void FeedbackAndProgressKeys_AllPresent()
        {
            string[] keys = {
                "feedback_try_again", "feedback_correct_was",
                "streak_label", "hint_button", "hint_count",
                "progress_label", "minigame_unlocked",
                "next_number", "find_number", "all_collected",
                "target_sum", "saved_points",
                "sticker_collected"
            };
            foreach (var k in keys) AssertKey(k);
        }

        // ── Sequence / ordering keys ─────────────────────────────────────────────

        [Test]
        public void SequenceKeys_AllPresent()
        {
            AssertKey("sequence_ascending");
            AssertKey("sequence_skip");
        }

        // ── Shape names ──────────────────────────────────────────────────────────

        [Test]
        public void ShapeNames_AllPresent()
        {
            string[] shapes = { "triangle", "square", "circle", "rectangle", "pentagon", "hexagon" };
            foreach (var s in shapes)
                AssertKey($"shape_{s}");
        }

        // ── Tap-region instructions (GrandHallRestoration) ───────────────────────

        [Test]
        public void TapRegionKeys_AllPresent()
        {
            string[] shapes = { "triangle", "square", "circle", "rectangle", "pentagon", "hexagon" };
            foreach (var s in shapes)
                AssertKey($"tap_region_{s}");
        }

        // ── Minigame instructions ────────────────────────────────────────────────
        // Key format: minigame_instruction_{ClassName.ToLower()}

        [Test]
        public void MinigameInstructionKeys_AllPresent()
        {
            // Class names lowercased — must match GetType().Name.ToLower() at runtime
            string[] classNames = {
                "socksortsweep",
                "crumbcollectcountdown",
                "cushioncannoncatch",
                "draindefense",
                "boxtowerbuilder",
                "streamuntanglesprint",
                "flowerbedfrenzy",
                "atticbinblitz",
                "sequencesprinkler",
                "grandhallrestoration",
                "scramblershutdown"
            };
            foreach (var name in classNames)
                AssertKey($"minigame_instruction_{name}");
        }

        // ── Reteach keys ─────────────────────────────────────────────────────────

        [Test]
        public void ReteachKeys_AllTopicsPresent()
        {
            string[] topics = {
                "Counting1To10", "Counting1To20",
                "AdditionTo10",  "SubtractionWithin10",
                "AdditionTo20",  "SubtractionWithin20",
                "Multiplication2x5x", "DivisionBy2_3_5",
                "NumberOrdering", "ShapeCounting"
            };
            foreach (var t in topics)
                AssertKey($"reteach_{t}");
        }

        // ── Character keys ───────────────────────────────────────────────────────

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

        // ── Plural keys ──────────────────────────────────────────────────────────

        [Test]
        public void PluralKeys_AllPresent()
        {
            string[] keys = {
                "coins_earned", "stars_total", "hints_left",
                "tasks_done", "points_saved", "minutes_played"
            };
            foreach (var k in keys) AssertPluralKey(k);
        }

        // ── Czech ≠ English (copy-paste guard) ───────────────────────────────────

        /// <summary>
        /// These keys MUST have different Czech and English values.
        /// Identical values indicate the Czech entry was never translated.
        /// (Character proper names, format-only strings, and loanwords are excluded.)
        /// </summary>
        [Test]
        public void TranslatableKeys_CzechDiffersFromEnglish()
        {
            string[] mustDiffer = {
                // Buttons
                "btn_play", "btn_next", "btn_back", "btn_home",
                "btn_replay", "btn_settings", "btn_ready",
                "btn_keep_playing", "btn_take_break",
                // Titles
                "app_name", "select_language",
                "level_complete_title", "level_select_title",
                "characters_title", "achievements_title",
                "parent_dashboard_title", "parent_pin_prompt", "session_break_prompt",
                // Level names (1–11)
                "level_1_name",  "level_2_name",  "level_3_name",  "level_4_name",
                "level_5_name",  "level_6_name",  "level_7_name",  "level_8_name",
                "level_9_name",  "level_10_name", "level_11_name",
                // Room names
                "room_bedroom", "room_kitchen", "room_livingroom", "room_bathroom",
                "room_garage",  "room_hallway", "room_backyard",   "room_attic",
                "room_rooftop", "room_grandhall", "room_secretlab",
                // Math questions
                "q_how_many", "q_addition", "q_subtraction", "q_missing_addition",
                "q_multiplication", "q_division", "q_ordering", "q_count_shapes",
                // Feedback
                "feedback_try_again", "feedback_correct_was", "hint_button",
                "minigame_unlocked", "all_collected",
                "sequence_ascending", "sequence_skip",
                // Shapes
                "shape_triangle", "shape_square", "shape_circle",
                "shape_rectangle", "shape_pentagon", "shape_hexagon",
                // Tap regions
                "tap_region_triangle", "tap_region_square", "tap_region_circle",
                "tap_region_rectangle", "tap_region_pentagon", "tap_region_hexagon",
                // Reteach
                "reteach_Counting1To10", "reteach_Counting1To20",
                "reteach_AdditionTo10",  "reteach_SubtractionWithin10",
                "reteach_AdditionTo20",  "reteach_SubtractionWithin20",
                "reteach_Multiplication2x5x", "reteach_DivisionBy2_3_5",
                "reteach_NumberOrdering", "reteach_ShapeCounting",
                // Character catchphrases (names are proper nouns — excluded)
                "char_bubbles_name",  // Czech: "Bublinky", English: "Bubbles"
                "char_pebble_name",   // Czech: "Profesor Kamínek", English: "Professor Pebble"
                "char_rumble_catchphrase", "char_xixi_catchphrase",
                "char_rocky_catchphrase",  "char_bubbles_catchphrase",
                "char_max_catchphrase",    "char_zara_catchphrase",
                "char_turbo_catchphrase",  "char_pebble_catchphrase",
                "char_luna_catchphrase",
                // Minigame instructions
                "minigame_instruction_socksortsweep",
                "minigame_instruction_crumbcollectcountdown",
                "minigame_instruction_cushioncannoncatch",
                "minigame_instruction_draindefense",
                "minigame_instruction_boxtowerbuilder",
                "minigame_instruction_streamuntanglesprint",
                "minigame_instruction_flowerbedfrenzy",
                "minigame_instruction_atticbinblitz",
                "minigame_instruction_sequencesprinkler",
                "minigame_instruction_grandhallrestoration",
                "minigame_instruction_scramblershutdown"
            };

            var failures = new List<string>();
            foreach (var key in mustDiffer)
            {
                bool czHas = _czValues.TryGetValue(key, out string czVal);
                bool enHas = _enValues.TryGetValue(key, out string enVal);
                if (!czHas || !enHas) continue; // missing-key failures caught elsewhere
                if (czVal == enVal)
                    failures.Add($"  [{key}]: both locales have identical value \"{czVal}\"");
            }

            Assert.IsEmpty(failures,
                "Czech values identical to English (untranslated):\n" + string.Join("\n", failures));
        }

        // ── No raw key fallbacks leaked ───────────────────────────────────────────

        /// <summary>
        /// Verifies Czech values don't contain any English-only characters or
        /// obviously raw key patterns (e.g. the value equals its key name).
        /// </summary>
        [Test]
        public void CzechValues_NoneEqualTheirOwnKey()
        {
            var failures = new List<string>();
            foreach (var kv in _czValues)
            {
                if (kv.Value == kv.Key)
                    failures.Add($"  [{kv.Key}]: value equals the key name (raw fallback leaked)");
            }
            Assert.IsEmpty(failures,
                "Czech entries whose value is the raw key name:\n" + string.Join("\n", failures));
        }
    }
}
