#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.DevTools
{
    /// <summary>
    /// Debug overlay — always-on-top panel to jump directly to any minigame.
    ///
    /// HOW TO TOGGLE:
    ///   • Set ENABLED = false  → hides the button entirely (zero runtime cost).
    ///   • The whole file is stripped from Release builds automatically
    ///     (only compiled when UNITY_EDITOR or DEVELOPMENT_BUILD is defined).
    /// </summary>
    public class DebugMinigameLauncher : MonoBehaviour
    {
        // ▼─────────────────────────────────────────────────────────────────────
        private const bool ENABLED = true;   // ← flip to false to hide overlay
        // ▲─────────────────────────────────────────────────────────────────────

        private GameObject _panel;
        private bool _open;

        private void Start()
        {
            if (!ENABLED) return;
            var canvasGo = BuildCanvas();
            BuildToggleButton(canvasGo.transform);
            _panel = BuildPanel(canvasGo.transform);
            _panel.SetActive(false);
        }

        // ── Canvas ─────────────────────────────────────────────────────────────

        private GameObject BuildCanvas()
        {
            var go = new GameObject("[DEBUG] Canvas");
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 9999;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        // ── Toggle button (small tab, top-right corner) ─────────────────────

        private void BuildToggleButton(Transform canvasRoot)
        {
            var rt = MakeRect("DBG_Toggle", canvasRoot,
                new Vector2(0.84f, 0.94f), new Vector2(0.99f, 0.995f));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.85f, 0.15f, 0.15f, 0.92f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(TogglePanel);
            MakeLabel(rt, "DBG\nMini", 18f).fontStyle = FontStyles.Bold;
        }

        // ── Main panel ─────────────────────────────────────────────────────────

        private GameObject BuildPanel(Transform canvasRoot)
        {
            var panel = MakeRect("DBG_Panel", canvasRoot,
                new Vector2(0.28f, 0.06f), new Vector2(0.99f, 0.93f)).gameObject;
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.97f);

            // Title bar
            var titleRt = MakeRect("Title", panel.transform,
                new Vector2(0f, 0.93f), Vector2.one);
            titleRt.gameObject.AddComponent<Image>().color = new Color(0.75f, 0.10f, 0.10f);
            MakeLabel(titleRt, "DEBUG — Jump to Minigame", 22f).fontStyle = FontStyles.Bold;

            // Close button inside title bar
            var closeRt = MakeRect("CloseBtn", titleRt,
                new Vector2(0.87f, 0.05f), new Vector2(0.99f, 0.95f));
            var cImg = closeRt.gameObject.AddComponent<Image>();
            cImg.color = new Color(1f, 1f, 1f, 0.15f);
            var cBtn = closeRt.gameObject.AddComponent<Button>();
            cBtn.targetGraphic = cImg;
            cBtn.onClick.AddListener(TogglePanel);
            MakeLabel(closeRt, "X", 20f);

            // Scrollable minigame list
            var listRt = MakeRect("List", panel.transform,
                Vector2.zero, new Vector2(1f, 0.93f));
            BuildScrollList(listRt);

            return panel;
        }

        // ── Scrollable list of minigames ───────────────────────────────────────

        private void BuildScrollList(RectTransform area)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // ScrollRect
            var sv = new GameObject("Scroll");
            sv.transform.SetParent(area, false);
            var svRt = sv.AddComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero;
            svRt.anchorMax = Vector2.one;
            svRt.offsetMin = svRt.offsetMax = Vector2.zero;
            var sr = sv.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 30f;

            // Viewport
            var vp = new GameObject("Viewport");
            vp.transform.SetParent(sv.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            sr.viewport = vpRt;

            // Content
            var ct = new GameObject("Content");
            ct.transform.SetParent(vp.transform, false);
            var ctRt = ct.AddComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 1f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;
            var vlg = ct.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlWidth  = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            var csf = ct.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = ctRt;

            foreach (var level in gm.AllLevels)
            {
                if (level != null) AddEntry(ctRt, level);
            }
        }

        private void AddEntry(RectTransform parent, LevelDefinition level)
        {
            var row = new GameObject($"Row_{level.levelIndex}");
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 68f;
            le.minHeight = 54f;
            var img = row.AddComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.24f);
            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;

            // Colour flash on hover (ColorBlock)
            var cb = btn.colors;
            cb.normalColor      = new Color(0.18f, 0.18f, 0.24f);
            cb.highlightedColor = new Color(0.28f, 0.28f, 0.38f);
            cb.pressedColor     = new Color(0.85f, 0.15f, 0.15f);
            btn.colors = cb;

            // Red level-number badge on the left
            var badgeRt = MakeRect("Badge", row.transform,
                new Vector2(0f, 0f), new Vector2(0f, 1f));
            badgeRt.offsetMin = Vector2.zero;
            badgeRt.offsetMax = new Vector2(52f, 0f);
            badgeRt.gameObject.AddComponent<Image>().color = new Color(0.85f, 0.15f, 0.15f, 0.75f);
            MakeLabel(badgeRt, (level.levelIndex + 1).ToString(), 22f).fontStyle = FontStyles.Bold;

            // Level name + minigame type text
            var textRt = MakeRect("Text", row.transform,
                Vector2.zero, Vector2.one);
            textRt.offsetMin = new Vector2(60f, 0f);
            textRt.offsetMax = new Vector2(-6f, 0f);
            var tmp = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            string levelName = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.Get(level.levelNameKey)
                : level.levelNameKey;
            tmp.SetText($"<b>{levelName}</b>\n<size=16><color=#999999>{level.minigameType}</color></size>");
            tmp.fontSize = 21f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            var cap = level;
            btn.onClick.AddListener(() =>
            {
                _open = false;
                _panel.SetActive(false);
                GameManager.Instance.DebugJumpToMinigame(cap);
            });
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void TogglePanel()
        {
            _open = !_open;
            _panel.SetActive(_open);
        }

        private static RectTransform MakeRect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static TextMeshProUGUI MakeLabel(RectTransform parent, string text, float size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            return tmp;
        }
    }
}
#endif
