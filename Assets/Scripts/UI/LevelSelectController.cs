using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    /// <summary>
    /// Builds level-select buttons at runtime so no fragile YAML hand-crafting is needed.
    /// Attach this to the Canvas. Everything else is created in code.
    /// </summary>
    public class LevelSelectController : MonoBehaviour
    {
        private static readonly Color[] ButtonColors =
        {
            new Color(0.40f, 0.80f, 0.60f), // green
            new Color(0.60f, 0.80f, 0.90f), // blue
            new Color(0.90f, 0.80f, 0.50f), // yellow
            new Color(0.70f, 0.60f, 0.90f), // purple
            new Color(0.40f, 0.85f, 0.80f), // teal
            new Color(0.95f, 0.65f, 0.50f), // orange
            new Color(0.65f, 0.60f, 0.85f), // indigo
            new Color(0.50f, 0.85f, 0.50f), // green2
            new Color(0.90f, 0.85f, 0.40f), // yellow2
            new Color(0.50f, 0.70f, 0.90f), // blue2
            new Color(0.90f, 0.60f, 0.70f), // pink
        };

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[LevelSelectController] GameManager.Instance is null!");
                return;
            }

            if (gm.AllLevels == null || gm.AllLevels.Length == 0)
            {
                Debug.LogError("[LevelSelectController] No levels loaded in GameManager.");
                return;
            }

            Debug.Log($"[LevelSelectController] Building {gm.AllLevels.Length} buttons.");

            // Scrollable content container
            var scroll = CreateScrollView();

            float yPos = -(gm.AllLevels.Length * 110f) / 2f + 55f;
            yPos = 0f; // will use layout group instead

            var content = scroll.content;

            for (int i = 0; i < gm.AllLevels.Length; i++)
            {
                var def = gm.GetLevel(i);
                if (def == null) continue;

                bool unlocked = gm.IsLevelUnlocked(i);
                var lp = gm.Progress.GetOrCreateLevel(i);

                CreateButton(content, def, lp, unlocked, i);
            }
        }

        private ScrollRect CreateScrollView()
        {
            // ScrollView root
            var svGo = new GameObject("ScrollView");
            svGo.transform.SetParent(transform, false);
            var svRt = svGo.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0, 0);
            svRt.anchorMax = new Vector2(1, 1);
            svRt.offsetMin = new Vector2(40, 40);
            svRt.offsetMax = new Vector2(-40, -80);

            var scrollRect = svGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;

            // Viewport
            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(svGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vpRt;

            // Content
            var ctGo = new GameObject("Content");
            ctGo.transform.SetParent(vpGo.transform, false);
            var ctRt = ctGo.AddComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0, 1);
            ctRt.anchorMax = new Vector2(1, 1);
            ctRt.pivot = new Vector2(0.5f, 1);
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;

            var layout = ctGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(0, 0, 12, 12);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = ctGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = ctRt;
            return scrollRect;
        }

        private void CreateButton(RectTransform parent, LevelDefinition def,
            LevelProgress lp, bool unlocked, int index)
        {
            var go = new GameObject($"Level_{index + 1}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 110);

            // Background image
            var img = go.AddComponent<Image>();
            Color baseColor = ButtonColors[index % ButtonColors.Length];
            img.color = unlocked
                ? baseColor
                : new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);

            // Button
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = unlocked;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(20, 0);
            labelRt.offsetMax = new Vector2(-20, 0);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            string levelName = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.Get(def.levelNameKey)
                : def.levelNameKey;
            tmp.text = $"{def.levelIndex + 1}.  {levelName}";
            tmp.fontSize = 42;
            tmp.color = unlocked ? Color.white : new Color(1, 1, 1, 0.5f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            // Stars row
            if (lp.stars > 0)
            {
                var starsGo = new GameObject("Stars");
                starsGo.transform.SetParent(go.transform, false);
                var starsRt = starsGo.AddComponent<RectTransform>();
                starsRt.anchorMin = new Vector2(1, 0);
                starsRt.anchorMax = new Vector2(1, 1);
                starsRt.pivot = new Vector2(1, 0.5f);
                starsRt.offsetMin = new Vector2(-140, 0);
                starsRt.offsetMax = new Vector2(-20, 0);

                var starsText = starsGo.AddComponent<TextMeshProUGUI>();
                starsText.text = new string('★', lp.stars) + new string('☆', 3 - lp.stars);
                starsText.fontSize = 32;
                starsText.color = new Color(1f, 0.85f, 0.1f);
                starsText.alignment = TextAlignmentOptions.MidlineRight;
            }

            // Click handler
            if (unlocked)
            {
                var capturedDef = def;
                btn.onClick.AddListener(() => OnLevelSelected(capturedDef));
            }

            Debug.Log($"[LevelSelectController] Button {index + 1}: '{levelName}', unlocked={unlocked}");
        }

        private void OnLevelSelected(LevelDefinition level)
        {
            Debug.Log($"[LevelSelectController] Selected: {level.levelNameKey}");
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.StartLevel(level);
        }
    }
}
