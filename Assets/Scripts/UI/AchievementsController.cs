using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    /// <summary>
    /// Achievements screen — built entirely in code so it works
    /// regardless of what's wired in the scene Inspector.
    /// Shows: level star ratings, character collection, topic mastery badges.
    /// </summary>
    public class AchievementsController : MonoBehaviour
    {
        private void Start()
        {
            // Wire escape key
            BuildUI();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) GoBack();
        }

        private void GoBack()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.TransitionTo(GameState.Home);
        }

        // ── Build entire UI in code ──────────────────────────────────────────────

        private void BuildUI()
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var loc      = LocalizationManager.Instance;
            var gm       = GameManager.Instance;
            var progress = gm?.Progress;

            // ── Root scroll view ─────────────────────────────────────────────────
            var root = new GameObject("AchievementsRoot");
            root.transform.SetParent(canvas.transform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

            // Card background
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.18f, 0.97f);

            // ScrollView
            var sv = MakeScrollView(root.transform);

            // ── Title ────────────────────────────────────────────────────────────
            AddLabel(sv, loc?.Get("achievements_title") ?? "Achievements",
                52f, new Color(1f, 0.85f, 0.2f), 70f, FontStyles.Bold);

            AddDivider(sv, new Color(0.4f, 0.35f, 0.6f));

            // ── Level stars ──────────────────────────────────────────────────────
            AddSectionHeader(sv, loc?.Get("label_total_stars") ?? "Stars", new Color(1f, 0.85f, 0.2f));

            int totalStars = 0;
            if (gm?.AllLevels != null)
            {
                foreach (var level in gm.AllLevels)
                {
                    var lp = progress?.GetOrCreateLevel(level.levelIndex);
                    int stars = lp?.stars ?? 0;
                    totalStars += stars;
                    string levelName = loc?.Get($"level_{level.levelIndex + 1}_name") ?? $"Level {level.levelIndex + 1}";
                    AddLevelRow(sv, levelName, stars, lp?.completed ?? false);
                }
            }

            AddLabel(sv, $"Total: {totalStars} *", 34f, new Color(1f, 0.85f, 0.2f), 50f, FontStyles.Bold);
            AddDivider(sv, new Color(0.4f, 0.35f, 0.6f));

            // ── Characters ───────────────────────────────────────────────────────
            AddSectionHeader(sv, loc?.Get("characters_title") ?? "Characters", new Color(0.4f, 0.8f, 1f));

            if (gm?.AllCharacters != null)
            {
                foreach (var ch in gm.AllCharacters)
                {
                    bool unlocked = progress?.IsCharacterUnlocked(ch.characterType) ?? false;
                    string name = loc?.Get($"char_{ch.characterType.ToString().ToLower()}_name") ?? ch.characterType.ToString();
                    string phrase = unlocked
                        ? (loc?.Get($"char_{ch.characterType.ToString().ToLower()}_catchphrase") ?? "")
                        : "???";
                    AddCharacterRow(sv, name, phrase, unlocked);
                }
            }

            AddDivider(sv, new Color(0.4f, 0.35f, 0.6f));

            // ── Topic mastery ────────────────────────────────────────────────────
            AddSectionHeader(sv, loc?.Get("label_topic_accuracy") ?? "Accuracy", new Color(0.4f, 1f, 0.7f));

            bool anyData = false;
            if (progress != null)
            {
                foreach (MathTopic topic in System.Enum.GetValues(typeof(MathTopic)))
                {
                    if (topic == MathTopic.MixedReview) continue;
                    var ta = progress.GetOrCreateTopicAccuracy(topic);
                    if (ta.totalProblems == 0) continue;
                    anyData = true;
                    string topicName = loc?.Get($"topic_{topic}") ?? topic.ToString();
                    float acc = ta.totalProblems > 0
                        ? (float)ta.correctFirstAttempt / ta.totalProblems
                        : 0f;
                    AddTopicBar(sv, topicName, acc, ta.totalProblems);
                }
            }
            if (!anyData)
                AddLabel(sv, loc?.Get("label_no_data") ?? "No data yet.", 28f, new Color(0.6f, 0.6f, 0.7f), 44f, FontStyles.Normal);

            AddDivider(sv, new Color(0.4f, 0.35f, 0.6f));

            // ── Back button ──────────────────────────────────────────────────────
            AddActionButton(sv, loc?.Get("btn_back") ?? "Back",
                new Color(0.2f, 0.55f, 0.9f), 72f, GoBack);

            Canvas.ForceUpdateCanvases();
        }

        // ── Row builders ────────────────────────────────────────────────────────

        private static void AddLevelRow(Transform parent, string levelName, int stars, bool completed)
        {
            var row = new GameObject("LevelRow");
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 54f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(0, 0, 4, 4);

            // Level name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(row.transform, false);
            var nLe = nameGo.AddComponent<LayoutElement>();
            nLe.flexibleWidth = 3f;
            var nTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nTmp.text = levelName;
            nTmp.fontSize = 30f;
            nTmp.color = completed ? Color.white : new Color(0.55f, 0.55f, 0.65f);
            nTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Stars
            var starGo = new GameObject("Stars");
            starGo.transform.SetParent(row.transform, false);
            var sLe = starGo.AddComponent<LayoutElement>();
            sLe.flexibleWidth = 2f;
            var sTmp = starGo.AddComponent<TextMeshProUGUI>();
            sTmp.text = new string('*', stars) + new string('-', 3 - stars);
            sTmp.fontSize = 32f;
            sTmp.color = stars == 3 ? new Color(1f, 0.85f, 0.1f)
                        : stars > 0 ? new Color(0.9f, 0.7f, 0.3f)
                        : new Color(0.4f, 0.4f, 0.5f);
            sTmp.alignment = TextAlignmentOptions.MidlineRight;
        }

        private static void AddCharacterRow(Transform parent, string name, string catchphrase, bool unlocked)
        {
            var row = new GameObject("CharRow");
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 64f;
            var vlg = row.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(8, 8, 4, 4);

            // Background tint
            var bg = row.AddComponent<Image>();
            bg.color = unlocked
                ? new Color(0.18f, 0.22f, 0.38f, 0.6f)
                : new Color(0.1f, 0.1f, 0.15f, 0.4f);

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(row.transform, false);
            var nLe = nameGo.AddComponent<LayoutElement>();
            nLe.preferredHeight = 34f;
            var nTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nTmp.text = (unlocked ? "[OK] " : "[?] ") + name;
            nTmp.fontSize = 28f;
            nTmp.color = unlocked ? new Color(0.4f, 0.9f, 0.6f) : new Color(0.45f, 0.45f, 0.55f);
            nTmp.fontStyle = FontStyles.Bold;

            var phraseGo = new GameObject("Phrase");
            phraseGo.transform.SetParent(row.transform, false);
            var pLe = phraseGo.AddComponent<LayoutElement>();
            pLe.preferredHeight = 26f;
            var pTmp = phraseGo.AddComponent<TextMeshProUGUI>();
            pTmp.text = catchphrase;
            pTmp.fontSize = 22f;
            pTmp.color = unlocked ? new Color(0.75f, 0.75f, 0.85f) : new Color(0.35f, 0.35f, 0.4f);
            pTmp.fontStyle = FontStyles.Italic;
        }

        private static void AddTopicBar(Transform parent, string topicName, float acc, int total)
        {
            var row = new GameObject("TopicRow");
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 64f;
            var vlg = row.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Header row
            var hRow = new GameObject("HRow");
            hRow.transform.SetParent(row.transform, false);
            var hLe = hRow.AddComponent<LayoutElement>();
            hLe.preferredHeight = 34f;
            var hlg = hRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(hRow.transform, false);
            var nLe = nameGo.AddComponent<LayoutElement>();
            nLe.flexibleWidth = 3f;
            var nTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nTmp.text = topicName;
            nTmp.fontSize = 26f;
            nTmp.color = Color.white;

            var pctGo = new GameObject("Pct");
            pctGo.transform.SetParent(hRow.transform, false);
            var pLe = pctGo.AddComponent<LayoutElement>();
            pLe.flexibleWidth = 1f;
            var pTmp = pctGo.AddComponent<TextMeshProUGUI>();
            pTmp.text = $"{acc * 100f:F0}%";
            pTmp.fontSize = 26f;
            pTmp.color = AccuracyColor(acc);
            pTmp.fontStyle = FontStyles.Bold;
            pTmp.alignment = TextAlignmentOptions.MidlineRight;

            // Bar bg
            var bgGo = new GameObject("BarBg");
            bgGo.transform.SetParent(row.transform, false);
            var bgLe = bgGo.AddComponent<LayoutElement>();
            bgLe.preferredHeight = 18f;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.25f, 0.35f);

            // Bar fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(acc), 1f);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = AccuracyColor(acc);
        }

        // ── Generic UI helpers ───────────────────────────────────────────────────

        private static Transform MakeScrollView(Transform parent)
        {
            var sv = new GameObject("ScrollView");
            sv.transform.SetParent(parent, false);
            var svRt = sv.AddComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero;
            svRt.anchorMax = Vector2.one;
            svRt.offsetMin = svRt.offsetMax = Vector2.zero;

            var scrollRect = sv.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 40f;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(sv.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            scrollRect.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot    = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(28, 28, 28, 28);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRt;

            return content.transform;
        }

        private static void AddLabel(Transform parent, string text, float fontSize,
            Color color, float height, FontStyles style)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static void AddSectionHeader(Transform parent, string text, Color color)
        {
            var go = new GameObject("SectionHeader");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 48f;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(color.r * 0.15f, color.g * 0.15f, color.b * 0.15f, 0.8f);
            var goTxt = new GameObject("Text");
            goTxt.transform.SetParent(go.transform, false);
            var rt = goTxt.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 0); rt.offsetMax = Vector2.zero;
            var tmp = goTxt.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 30f; tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static void AddDivider(Transform parent, Color color)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 2f;
            var img = go.AddComponent<Image>();
            img.color = color;
        }

        private static void AddActionButton(Transform parent, string text,
            Color color, float height, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("ActionButton");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lRt = lblGo.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 34f; tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private static Color AccuracyColor(float acc)
        {
            if (acc >= 0.85f) return new Color(0.2f, 0.85f, 0.45f);
            if (acc >= 0.6f)  return new Color(1f,   0.7f,  0.1f);
            return new Color(0.95f, 0.35f, 0.25f);
        }
    }
}
