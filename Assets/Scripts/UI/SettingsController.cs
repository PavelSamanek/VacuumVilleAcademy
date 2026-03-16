using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.UI
{
    /// <summary>
    /// Settings screen — built entirely in code so it works regardless of
    /// what's wired in the scene Inspector.
    /// Contains: language toggle (Czech / English), volume sliders, back button.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        // Slider refs kept so OnLanguageChanged can rebuild the labels
        private Slider _musicSlider;
        private Slider _sfxSlider;
        private Slider _voiceSlider;

        // Language button refs for active highlight
        private Button _czechBtn;
        private Button _englishBtn;

        private void Start()
        {
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

            var loc   = LocalizationManager.Instance;
            var audio = AudioManager.Instance;
            var currentLang = loc?.CurrentLanguage ?? Language.Czech;

            // ── Root panel ───────────────────────────────────────────────────────
            var root = new GameObject("SettingsRoot", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;
            var rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.07f, 0.06f, 0.16f, 0.97f);

            var sv = MakeScrollView(root.transform);

            // ── Title ────────────────────────────────────────────────────────────
            AddLabel(sv, loc?.Get("btn_settings") ?? "Settings",
                52f, new Color(0.6f, 0.85f, 1f), 70f, FontStyles.Bold);
            AddDivider(sv);

            // ── Language section ─────────────────────────────────────────────────
            AddLabel(sv, loc?.Get("select_language") ?? "Language",
                34f, new Color(0.6f, 0.85f, 1f), 48f, FontStyles.Bold);

            var langRow = new GameObject("LangRow", typeof(RectTransform));
            langRow.transform.SetParent(sv, false);
            var lrLe = langRow.AddComponent<LayoutElement>();
            lrLe.preferredHeight = 80f;
            var lrHlg = langRow.AddComponent<HorizontalLayoutGroup>();
            lrHlg.spacing = 16f;
            lrHlg.childControlWidth = true;
            lrHlg.childControlHeight = true;
            lrHlg.childForceExpandWidth = true;
            lrHlg.childForceExpandHeight = false;

            _czechBtn   = MakeLangButton(langRow.transform, "Cestina (CZ)",
                currentLang == Language.Czech);
            _englishBtn = MakeLangButton(langRow.transform, "English (EN)",
                currentLang == Language.English);

            _czechBtn.onClick.AddListener(() => SelectLanguage(Language.Czech));
            _englishBtn.onClick.AddListener(() => SelectLanguage(Language.English));

            AddDivider(sv);

            // ── Volume section ───────────────────────────────────────────────────
            AddLabel(sv, loc?.Get("label_volume") ?? "Volume", 34f, new Color(0.6f, 0.85f, 1f), 48f, FontStyles.Bold);

            float musicVol = audio != null ? audio.MusicVolume
                : PlayerPrefs.GetFloat("vol_music", 0.7f);
            float sfxVol   = audio != null ? audio.SfxVolume
                : PlayerPrefs.GetFloat("vol_sfx", 1f);
            float voiceVol = audio != null ? audio.VoiceVolume
                : PlayerPrefs.GetFloat("vol_voice", 1f);

            _musicSlider = AddVolumeRow(sv, loc?.Get("label_music") ?? "Music", musicVol,
                v => { if (audio != null) audio.MusicVolume = v; });
            _sfxSlider   = AddVolumeRow(sv, loc?.Get("label_sfx")   ?? "Sound FX", sfxVol,
                v => { if (audio != null) audio.SfxVolume = v; });
            _voiceSlider = AddVolumeRow(sv, loc?.Get("label_voice") ?? "Voice", voiceVol,
                v => { if (audio != null) audio.VoiceVolume = v; });

            AddDivider(sv);

            // ── Back button ──────────────────────────────────────────────────────
            AddActionButton(sv, loc?.Get("btn_back") ?? "← Back",
                new Color(0.2f, 0.55f, 0.9f), 72f, GoBack);

            Canvas.ForceUpdateCanvases();
        }

        // ── Language selection ───────────────────────────────────────────────────

        private void SelectLanguage(Language lang)
        {
            AudioManager.Instance?.PlayButton();
            PlayerPrefs.SetInt("lang", (int)lang);
            PlayerPrefs.Save();
            LocalizationManager.Instance?.SetLanguage(lang);
            if (GameManager.Instance != null)
                GameManager.Instance.Progress.selectedLanguage = lang;

            // Update button highlights
            SetLangHighlight(_czechBtn,   lang == Language.Czech);
            SetLangHighlight(_englishBtn, lang == Language.English);
        }

        private static void SetLangHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img) img.color = active
                ? new Color(0.25f, 0.70f, 0.45f)   // green = selected
                : new Color(0.22f, 0.25f, 0.42f);  // dark navy = unselected
        }

        // ── UI builders ──────────────────────────────────────────────────────────

        private static Button MakeLangButton(Transform parent, string label, bool active)
        {
            var go  = new GameObject("LangBtn", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le  = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 80f;
            var img = go.AddComponent<Image>();
            img.color = active
                ? new Color(0.25f, 0.70f, 0.45f)
                : new Color(0.22f, 0.25f, 0.42f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lRt = (RectTransform)lGo.transform;
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 32f;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        private static Slider AddVolumeRow(Transform parent, string label, float value,
            UnityEngine.Events.UnityAction<float> onChange)
        {
            // Row container — must be RectTransform from the start for LayoutGroup
            var row = new GameObject("VolumeRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
            var vlg = row.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Label
            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(row.transform, false);
            var lLe = lGo.AddComponent<LayoutElement>();
            lLe.preferredHeight = 34f;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28f;
            tmp.color = Color.white;

            // Slider root — RectTransform from the start so AddComponent<Slider> works
            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(row.transform, false);
            var slLe = sliderGo.AddComponent<LayoutElement>();
            slLe.preferredHeight = 30f;
            var sliderRt = (RectTransform)sliderGo.transform;
            sliderRt.anchorMin = Vector2.zero;
            sliderRt.anchorMax = Vector2.one;
            sliderRt.offsetMin = sliderRt.offsetMax = Vector2.zero;

            // Background track
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.25f, 0.35f);

            // Fill area
            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = (RectTransform)fillArea.transform;
            faRt.anchorMin = new Vector2(0f, 0.25f);
            faRt.anchorMax = new Vector2(1f, 0.75f);
            faRt.offsetMin = new Vector2(5f, 0f);
            faRt.offsetMax = new Vector2(-15f, 0f);

            // Fill image
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.70f, 0.95f);

            // Handle sliding area
            var handleArea = new GameObject("HandleSlideArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var haRt = (RectTransform)handleArea.transform;
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(10f, 0f);
            haRt.offsetMax = new Vector2(-10f, 0f);

            // Handle knob
            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleArea.transform, false);
            var handleRt = (RectTransform)handleGo.transform;
            handleRt.sizeDelta = new Vector2(30f, 30f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = Color.white;

            // Wire Slider component
            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect      = fillRt;
            slider.handleRect    = handleRt;
            slider.targetGraphic = handleImg;
            slider.minValue      = 0f;
            slider.maxValue      = 1f;
            slider.value         = value;
            slider.onValueChanged.AddListener(onChange);

            return slider;
        }

        private static Transform MakeScrollView(Transform parent)
        {
            var sv = new GameObject("ScrollView", typeof(RectTransform));
            sv.transform.SetParent(parent, false);
            var svRt = (RectTransform)sv.transform;
            svRt.anchorMin = Vector2.zero;
            svRt.anchorMax = Vector2.one;
            svRt.offsetMin = svRt.offsetMax = Vector2.zero;

            var scrollRect = sv.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 40f;

            var vp = new GameObject("Viewport", typeof(RectTransform));
            vp.transform.SetParent(sv.transform, false);
            var vpRt = (RectTransform)vp.transform;
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            scrollRect.viewport = vpRt;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(vp.transform, false);
            var contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
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

        private static void AddDivider(Transform parent)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 2f;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.45f);
        }

        private static void AddActionButton(Transform parent, string text,
            Color color, float height, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("ActionButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lRt = (RectTransform)lGo.transform;
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 34f; tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
