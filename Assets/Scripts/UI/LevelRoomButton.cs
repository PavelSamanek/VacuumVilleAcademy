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
            if (nameLabel != null) nameLabel.text = LocalizationManager.Instance.Get(def.levelNameKey);
            if (button != null)   button.interactable = unlocked;
            if (lockIcon != null) lockIcon.gameObject.SetActive(!unlocked);

            var group = GetComponent<CanvasGroup>();
            if (group) group.alpha = unlocked ? 1f : 0.5f;

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
