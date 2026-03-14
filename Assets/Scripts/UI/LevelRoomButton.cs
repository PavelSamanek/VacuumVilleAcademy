using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    public class LevelRoomButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image newGlowEffect;

        private LevelDefinition _def;

        public void Setup(LevelDefinition def, LevelProgress lp, bool unlocked,
            System.Action<LevelDefinition> onSelect)
        {
            _def = def;
            if (nameLabel != null)
            {
                string localizedName = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.Get(def.levelNameKey)
                    : def.levelNameKey;
                // Prefix with level number so it's readable even without Czech font
                nameLabel.text = $"{def.levelIndex + 1}. {localizedName}";
            }
            if (button != null)   button.interactable = unlocked;
            if (lockIcon != null) lockIcon.gameObject.SetActive(!unlocked);

            // Dim locked buttons via CanvasGroup if present, otherwise tint the Image
            var group = GetComponent<CanvasGroup>();
            if (group)
            {
                group.alpha = unlocked ? 1f : 0.5f;
            }
            else
            {
                var img = GetComponent<Image>();
                if (img != null)
                {
                    var c = img.color;
                    img.color = new Color(c.r, c.g, c.b, unlocked ? 1f : 0.4f);
                }
            }

            if (starImages != null)
                for (int i = 0; i < starImages.Length; i++)
                    if (starImages[i] != null) starImages[i].gameObject.SetActive(i < lp.stars);

            bool isNew = unlocked && !lp.completed && lp.stars == 0;
            if (newGlowEffect) newGlowEffect.gameObject.SetActive(isNew);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelect?.Invoke(_def));
            }
        }
    }
}
