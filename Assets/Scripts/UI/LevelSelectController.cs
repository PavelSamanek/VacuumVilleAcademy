using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    public class LevelSelectController : MonoBehaviour
    {
        private static readonly Color[] ButtonColors =
        {
            new Color(0.40f, 0.80f, 0.60f),
            new Color(0.60f, 0.80f, 0.90f),
            new Color(0.90f, 0.80f, 0.50f),
            new Color(0.70f, 0.60f, 0.90f),
            new Color(0.40f, 0.85f, 0.80f),
            new Color(0.95f, 0.65f, 0.50f),
            new Color(0.65f, 0.60f, 0.85f),
            new Color(0.50f, 0.85f, 0.50f),
            new Color(0.90f, 0.85f, 0.40f),
            new Color(0.50f, 0.70f, 0.90f),
            new Color(0.90f, 0.60f, 0.70f),
        };

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogError("[LevelSelect] GameManager is null"); return; }
            if (gm.AllLevels == null || gm.AllLevels.Length == 0) { Debug.LogError("[LevelSelect] No levels"); return; }

            var content = BuildScrollView();

            for (int i = 0; i < gm.AllLevels.Length; i++)
            {
                var def = gm.GetLevel(i);
                if (def == null) continue;
                bool unlocked = gm.IsLevelUnlocked(i);
                var lp = gm.Progress.GetOrCreateLevel(i);
                BuildButton(content, def, lp, unlocked, i);
            }

            Debug.Log($"[LevelSelect] Built {gm.AllLevels.Length} buttons.");
        }

        // ── Layout ─────────────────────────────────────────────────────────────

        private RectTransform BuildScrollView()
        {
            // Scroll root — anchors fill the canvas
            var sv = new GameObject("ScrollView");
            sv.transform.SetParent(transform, false);
            var svRt = sv.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.04f, 0.04f);
            svRt.anchorMax = new Vector2(0.96f, 0.96f);
            svRt.offsetMin = svRt.offsetMax = Vector2.zero;

            var sr = sv.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 25f;

            // Viewport — RectMask2D clips without needing Image+Mask
            var vp = new GameObject("Viewport");
            vp.transform.SetParent(sv.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            sr.viewport = vpRt;

            // Content — anchored top, grows downward
            var ct = new GameObject("Content");
            ct.transform.SetParent(vp.transform, false);
            var ctRt = ct.AddComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 1f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.pivot    = new Vector2(0.5f, 1f);
            ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;

            var vlg = ct.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(0, 0, 8, 8);
            vlg.childControlWidth  = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            var csf = ct.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = ctRt;
            return ctRt;
        }

        private void BuildButton(RectTransform parent, LevelDefinition def,
            LevelProgress lp, bool unlocked, int index)
        {
            var go = new GameObject($"Btn_{index + 1}");
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 100f;
            le.minHeight = 80f;

            Color base_ = ButtonColors[index % ButtonColors.Length];
            float a = unlocked ? 1f : 0.35f;
            var img = go.AddComponent<Image>();
            img.color = new Color(base_.r, base_.g, base_.b, a);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = unlocked;

            // Label
            var lGo = new GameObject("Label");
            lGo.transform.SetParent(go.transform, false);
            var lRt = lGo.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(24f, 0f);
            lRt.offsetMax = new Vector2(-24f, 0f);

            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            string levelName = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.Get(def.levelNameKey)
                : def.levelNameKey;
            tmp.SetText($"{def.levelIndex + 1}.  {levelName}");
            tmp.fontSize = 38f;
            tmp.color = new Color(1f, 1f, 1f, unlocked ? 1f : 0.5f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            if (lp.stars > 0)
                AddStars(go.transform, lp.stars);

            if (unlocked)
            {
                var cap = def;
                btn.onClick.AddListener(() => OnSelected(cap));
            }
        }

        private void AddStars(Transform parent, int stars)
        {
            var sg = new GameObject("Stars");
            sg.transform.SetParent(parent, false);
            var sr = sg.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(1f, 0f);
            sr.anchorMax = new Vector2(1f, 1f);
            sr.pivot     = new Vector2(1f, 0.5f);
            sr.offsetMin = new Vector2(-150f, 0f);
            sr.offsetMax = new Vector2(-16f,  0f);

            var t = sg.AddComponent<TextMeshProUGUI>();
            t.SetText(new string('★', stars) + new string('☆', 3 - stars));
            t.fontSize  = 28f;
            t.color     = new Color(1f, 0.85f, 0.1f);
            t.alignment = TextAlignmentOptions.MidlineRight;
        }

        // ── Navigation ─────────────────────────────────────────────────────────

        private void OnSelected(LevelDefinition def)
        {
            Debug.Log($"[LevelSelect] Selected level {def.levelIndex + 1}");
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.StartLevel(def);
        }
    }
}
