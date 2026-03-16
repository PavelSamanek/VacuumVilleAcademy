using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    /// <summary>
    /// PIN-gated parent dashboard.
    /// PIN challenge: hold the number "7" button for 3 seconds.
    /// After unlock, dashboard content is built entirely in code.
    /// </summary>
    public class ParentDashboardController : MonoBehaviour
    {
        [Header("PIN Gate")]
        [SerializeField] private GameObject pinGatePanel;
        [SerializeField] private Button pinHoldButton;
        [SerializeField] private Slider pinHoldProgress;
        [SerializeField] private float pinHoldDuration = 3f;
        [SerializeField] private Button pinGateBackButton;

        [Header("Dashboard")]
        [SerializeField] private GameObject dashboardPanel;

        private float _holdTimer;
        private bool _holding;

        private void Start()
        {
            pinGatePanel.SetActive(true);
            dashboardPanel.SetActive(false);

            pinHoldButton.GetComponent<EventTriggerHelper>()?.onPointerDown.AddListener(OnPinHoldStart);
            pinHoldButton.GetComponent<EventTriggerHelper>()?.onPointerUp.AddListener(OnPinHoldEnd);

            if (pinGateBackButton == null)
            {
                var go = GameObject.Find("PinGateBackButton");
                if (go != null) pinGateBackButton = go.GetComponent<Button>();
            }
            if (pinGateBackButton != null)
                pinGateBackButton.onClick.AddListener(GoBack);
        }

        private void GoBack()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.TransitionTo(GameState.Home);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { GoBack(); return; }
            if (!_holding) return;

            _holdTimer += Time.deltaTime;
            pinHoldProgress.value = _holdTimer / pinHoldDuration;

            if (_holdTimer >= pinHoldDuration)
            {
                _holding = false;
                UnlockDashboard();
            }
        }

        private void OnPinHoldStart() { _holding = true;  _holdTimer = 0f; }
        private void OnPinHoldEnd()   { _holding = false; _holdTimer = 0f; pinHoldProgress.value = 0f; }

        // ── Dashboard ───────────────────────────────────────────────────────────

        private void UnlockDashboard()
        {
            pinGatePanel.SetActive(false);
            dashboardPanel.SetActive(true);
            BuildDashboard();
        }

        private void BuildDashboard()
        {
            // Clear any existing children
            foreach (Transform child in dashboardPanel.transform)
                Destroy(child.gameObject);

            // ── Scroll view fills the whole panel ───────────────────────────────
            var sv = new GameObject("ScrollView");
            sv.transform.SetParent(dashboardPanel.transform, false);
            var svRt = sv.AddComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero;
            svRt.anchorMax = Vector2.one;
            svRt.offsetMin = svRt.offsetMax = Vector2.zero;
            // White card so dark text is readable on the dark themed background
            var svBg = sv.AddComponent<Image>();
            svBg.color = new Color(0.96f, 0.96f, 0.99f, 0.97f);
            svBg.raycastTarget = false;

            var scrollRect = sv.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 30f;

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
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRt;

            var loc = LocalizationManager.Instance;
            var progress = GameManager.Instance?.Progress;
            if (progress == null) return;

            // ── Title ────────────────────────────────────────────────────────────
            AddLabel(content.transform, loc?.Get("parent_dashboard_title") ?? "Přehled pro rodiče",
                48f, new Color(0.15f, 0.15f, 0.3f), 70f);

            AddDivider(content.transform);

            // ── Stats ─────────────────────────────────────────────────────────────
            string timeText = loc != null
                ? loc.GetPlural("minutes_played", progress.totalMinutesPlayed)
                : $"{progress.totalMinutesPlayed} min";
            AddStatRow(content.transform,
                loc?.Get("label_time_played") ?? "Čas hraní:", timeText);

            string starsText = loc != null
                ? loc.GetPlural("stars_total", progress.totalStars)
                : $"{progress.totalStars} \u25CF";
            AddStatRow(content.transform,
                loc?.Get("label_total_stars") ?? "Celkem hvězd:", starsText);

            AddDivider(content.transform);

            // ── Per-topic accuracy ─────────────────────────────────────────────
            AddLabel(content.transform,
                loc?.Get("label_topic_accuracy") ?? "Přesnost podle témat:",
                34f, new Color(0.3f, 0.3f, 0.5f), 50f);

            bool anyTopicData = false;
            foreach (MathTopic topic in System.Enum.GetValues(typeof(MathTopic)))
            {
                if (topic == MathTopic.MixedReview) continue;
                var ta = progress.GetOrCreateTopicAccuracy(topic);
                if (ta.totalProblems == 0) continue;
                anyTopicData = true;

                float pct = (float)ta.correctFirstAttempt / ta.totalProblems;
                string topicName = loc?.Get($"topic_{topic}") ?? topic.ToString();
                AddTopicRow(content.transform, topicName, pct, ta.totalProblems);
            }

            if (!anyTopicData)
                AddLabel(content.transform,
                    loc?.Get("label_no_data") ?? "Zatím žádná data.",
                    30f, new Color(0.5f, 0.5f, 0.5f), 44f);

            AddDivider(content.transform);

            // ── Reset button ─────────────────────────────────────────────────────
            AddButton(content.transform,
                loc?.Get("reset_progress") ?? "Smazat postup",
                new Color(0.85f, 0.3f, 0.3f), 80f, OnResetProgress);

            // ── Close button (inside scroll content at the very bottom) ──────────
            AddButton(content.transform,
                loc?.Get("back_button") ?? "← Zpět",
                new Color(0, 0.75f, 0.65f), 80f, GoBack);

            // Force layout rebuild then scroll to top
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            scrollRect.verticalNormalizedPosition = 1f;
        }

        // ── UI builder helpers ──────────────────────────────────────────────────

        private static void AddLabel(Transform parent, string text, float size,
            Color color, float height)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static void AddStatRow(Transform parent, string label, string value)
        {
            var row = new GameObject("StatRow");
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 56f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            var lGo = new GameObject("Key");
            lGo.transform.SetParent(row.transform, false);
            var lLe = lGo.AddComponent<LayoutElement>();
            lLe.flexibleWidth = 1f;
            var lTmp = lGo.AddComponent<TextMeshProUGUI>();
            lTmp.text = label;
            lTmp.fontSize = 32f;
            lTmp.color = new Color(0.3f, 0.3f, 0.4f);
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var vGo = new GameObject("Value");
            vGo.transform.SetParent(row.transform, false);
            var vLe = vGo.AddComponent<LayoutElement>();
            vLe.flexibleWidth = 1f;
            var vTmp = vGo.AddComponent<TextMeshProUGUI>();
            vTmp.text = value;
            vTmp.fontSize = 32f;
            vTmp.color = new Color(0.1f, 0.1f, 0.2f);
            vTmp.fontStyle = FontStyles.Bold;
            vTmp.alignment = TextAlignmentOptions.MidlineRight;
        }

        private static void AddTopicRow(Transform parent, string topicName,
            float accuracy, int total)
        {
            var row = new GameObject("TopicRow");
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
            var vlg = row.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4f;

            // Label row
            var lblRow = new GameObject("LblRow");
            lblRow.transform.SetParent(row.transform, false);
            var lLe = lblRow.AddComponent<LayoutElement>();
            lLe.preferredHeight = 36f;
            var hlg = lblRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(lblRow.transform, false);
            var nLe = nameGo.AddComponent<LayoutElement>();
            nLe.flexibleWidth = 2f;
            var nTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nTmp.text = topicName;
            nTmp.fontSize = 28f;
            nTmp.color = new Color(0.2f, 0.2f, 0.35f);
            nTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var pctGo = new GameObject("Pct");
            pctGo.transform.SetParent(lblRow.transform, false);
            var pLe = pctGo.AddComponent<LayoutElement>();
            pLe.flexibleWidth = 1f;
            var pTmp = pctGo.AddComponent<TextMeshProUGUI>();
            pTmp.text = $"{accuracy * 100f:F0}%  ({total})";
            pTmp.fontSize = 28f;
            pTmp.color = AccuracyColor(accuracy);
            pTmp.fontStyle = FontStyles.Bold;
            pTmp.alignment = TextAlignmentOptions.MidlineRight;

            // Progress bar bg
            var bgGo = new GameObject("BarBg");
            bgGo.transform.SetParent(row.transform, false);
            var bgLe = bgGo.AddComponent<LayoutElement>();
            bgLe.preferredHeight = 20f;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.85f, 0.85f, 0.9f);

            // Progress bar fill
            var fillGo = new GameObject("BarFill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(accuracy, 1f);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = AccuracyColor(accuracy);
        }

        private static Color AccuracyColor(float accuracy)
        {
            if (accuracy >= 0.85f) return new Color(0.2f, 0.75f, 0.4f);
            if (accuracy >= 0.6f)  return new Color(1f, 0.7f, 0.1f);
            return new Color(0.9f, 0.35f, 0.25f);
        }

        private static void AddDivider(Transform parent)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 2f;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.8f, 0.8f, 0.85f);
        }

        private static void AddButton(Transform parent, string text, Color color,
            float height, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            AddLabelToButton(go.transform, text, 32f);
        }

        private static void AddLabelToButton(Transform parent, string text, float size)
        {
            var lGo = new GameObject("Label");
            lGo.transform.SetParent(parent, false);
            var lRt = lGo.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ── Reset progress ──────────────────────────────────────────────────────

        private void OnResetProgress()
        {
            StartCoroutine(ConfirmReset());
        }

        private IEnumerator ConfirmReset()
        {
            yield return new WaitForSeconds(3f);
            SaveSystem.Delete();
            GameManager.Instance?.TransitionTo(VacuumVille.Data.GameState.LanguageSelect);
        }
    }
}
