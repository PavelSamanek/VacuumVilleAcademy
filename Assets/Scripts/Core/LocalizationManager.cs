using System;
using System.Collections.Generic;
using UnityEngine;
using VacuumVille.Data;

namespace VacuumVille.Core
{
    /// <summary>
    /// Loads JSON string tables from Resources/Localization/{locale}/ and
    /// resolves keys at runtime. Supports Czech (default) and English.
    /// Handles Czech plural forms: 1 / 2-4 / 5+
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public Language CurrentLanguage { get; private set; } = Language.Czech;

        public event Action OnLanguageChanged;

        private Dictionary<string, string> _strings = new();

        // Czech plural categories: one / few / many
        private Dictionary<string, string[]> _plurals = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetLanguage(Language lang)
        {
            CurrentLanguage = lang;
            LoadTable(lang);
            OnLanguageChanged?.Invoke();
        }

        private void LoadTable(Language lang)
        {
            string locale = lang == Language.Czech ? "cs-CZ" : "en-US";
            _strings.Clear();
            _plurals.Clear();

            TextAsset stringsAsset = Resources.Load<TextAsset>($"Localization/{locale}/strings");
            if (stringsAsset != null)
                ParseStrings(stringsAsset.text);
            else
                Debug.LogWarning($"[Localization] strings.json not found for locale {locale}");

            TextAsset pluralsAsset = Resources.Load<TextAsset>($"Localization/{locale}/plurals");
            if (pluralsAsset != null)
                ParsePlurals(pluralsAsset.text);
        }

        private void ParseStrings(string json)
        {
            // Simple flat JSON parser: {"key": "value", ...}
            var wrapper = JsonUtility.FromJson<StringTableWrapper>(json);
            if (wrapper?.entries == null) return;
            foreach (var e in wrapper.entries)
                _strings[e.key] = e.value;
        }

        private void ParsePlurals(string json)
        {
            var wrapper = JsonUtility.FromJson<PluralTableWrapper>(json);
            if (wrapper?.entries == null) return;
            foreach (var e in wrapper.entries)
                _plurals[e.key] = new[] { e.one, e.few, e.many };
        }

        /// <summary>Returns localized string for key. Falls back to key itself if missing.</summary>
        public string Get(string key)
        {
            if (_strings.TryGetValue(key, out string val)) return val;
            Debug.LogWarning($"[Localization] Missing key: {key}");
            return key;
        }

        /// <summary>Returns localized string with {0}, {1}... replacements.</summary>
        public string Get(string key, params object[] args)
        {
            string template = Get(key);
            return string.Format(template, args);
        }

        /// <summary>
        /// Returns plural form for count.
        /// Czech rules: 1 → one, 2-4 → few, 5+ → many
        /// English rules: 1 → one, other → many
        /// </summary>
        public string GetPlural(string key, int count)
        {
            if (!_plurals.TryGetValue(key, out string[] forms))
                return $"{count} {key}";

            int form = CurrentLanguage == Language.Czech
                ? CzechPluralForm(count)
                : EnglishPluralForm(count);

            return string.Format(forms[form], count);
        }

        private static int CzechPluralForm(int n)
        {
            if (n == 1) return 0;           // one
            if (n >= 2 && n <= 4) return 1; // few
            return 2;                        // many
        }

        private static int EnglishPluralForm(int n)
            => n == 1 ? 0 : 2; // one / many (index 1 "few" unused in English)

        // JSON wrapper types
        [Serializable] private class StringEntry { public string key; public string value; }
        [Serializable] private class StringTableWrapper { public StringEntry[] entries; }
        [Serializable] private class PluralEntry { public string key; public string one; public string few; public string many; }
        [Serializable] private class PluralTableWrapper { public PluralEntry[] entries; }
    }
}
