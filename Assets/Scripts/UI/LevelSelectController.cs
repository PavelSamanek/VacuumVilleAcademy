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

            int count = gm.AllLevels.Length;
            float btnH = 110f;
            float spacing = 14f;
            float totalH = count * btnH + (count - 1) * spacing;
            float startY = totalH / 2f - btnH / 2f;

            Debug.Log($"[LevelSelect] Building {count} buttons, totalH={totalH}");

            for (int i = 0; i < count; i++)
            {
                var def = gm.GetLevel(i);
                if (def == null) continue;

                bool unlocked = gm.IsLevelUnlocked(i);
                var lp = gm.Progress.GetOrCreateLevel(i);

                float y = startY - i * (btnH + spacing);
                BuildButton(def, lp, unlocked, i, y);
            }
        }

        private void BuildButton(LevelDefinition def, LevelProgress lp, bool unlocked, int index, float y)
        {
            // --- root ---
            var go = new GameObject($"Btn_{index + 1}");
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(960f, 110f);
            rt.anchoredPosition = new Vector2(0f, y);

            Color base_ = ButtonColors[index % ButtonColors.Length];
            float a = unlocked ? 1f : 0.35f;
            var img = go.AddComponent<Image>();
            img.color = new Color(base_.r, base_.g, base_.b, a);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = unlocked;

            // --- label ---
            var lGo = new GameObject("Label");
            lGo.transform.SetParent(go.transform, false);

            var lRt = lGo.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(24f, 0f);
            lRt.offsetMax = new Vector2(-24f, 0f);

            var tmp = lGo.AddComponent<TextMeshProUGUI>();

            string name_ = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.Get(def.levelNameKey)
                : def.levelNameKey;
            tmp.SetText($"{def.levelIndex + 1}.  {name_}");
            tmp.fontSize = 44f;
            tmp.color = new Color(1f, 1f, 1f, unlocked ? 1f : 0.5f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            // --- click ---
            if (unlocked)
            {
                var cap = def;
                btn.onClick.AddListener(() => OnSelected(cap));
            }
        }

        private void OnSelected(LevelDefinition def)
        {
            Debug.Log($"[LevelSelect] Selected level {def.levelIndex + 1}");
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.StartLevel(def);
        }
    }
}
