// ============================================================
// ProjectSetup.cs
// Run via: Unity Menu → VacuumVille → Setup Entire Project
// This creates ALL scenes, ScriptableObject assets, prefabs,
// and wires up every serialized field automatically.
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VacuumVille.Data;

namespace VacuumVille.Editor
{
    public static class ProjectSetup
    {
        private static readonly string[] SceneNames = {
            "LanguageSelect", "Home", "LevelSelect", "LevelIntro",
            "CharacterSelect", "MathTask", "MinigameUnlock", "LevelComplete",
            "ParentDashboard", "Settings",
            "Minigame_SockSortSweep",
            "Minigame_CrumbCollectCountdown",
            "Minigame_CushionCannonCatch",
            "Minigame_DrainDefense",
            "Minigame_BoxTowerBuilder",
            "Minigame_StreamerUntangleSprint",
            "Minigame_FlowerBedFrenzy",
            "Minigame_AtticBinBlitz",
            "Minigame_SequenceSprinkler",
            "Minigame_GrandHallRestoration",
            "Minigame_ScramblerShutdown"
        };

        // ── Entry Point ──────────────────────────────────────────────────────────

        [MenuItem("VacuumVille/Setup Entire Project", priority = 1)]
        public static void SetupEntireProject()
        {
            int step = 0, total = 7;
            Progress(++step, total, "Creating folder structure...");
            CreateFolderStructure();

            Progress(++step, total, "Creating level ScriptableObjects...");
            CreateLevelAssets();

            Progress(++step, total, "Creating character ScriptableObjects...");
            CreateCharacterAssets();

            Progress(++step, total, "Creating placeholder sprites...");
            CreatePlaceholderSprites();

            Progress(++step, total, "Creating all scenes...");
            CreateAllScenes();

            Progress(++step, total, "Registering scenes in Build Settings...");
            RegisterScenesInBuildSettings();

            Progress(++step, total, "Saving assets...");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
            Debug.Log("[VacuumVille] ✅ Project setup complete! Press Play on the LanguageSelect scene.");
            EditorUtility.DisplayDialog("Setup Complete",
                "All scenes, assets, and ScriptableObjects have been created.\n\n" +
                "Open 'Assets/Scenes/LanguageSelect.unity' and press Play to start.",
                "OK");
        }

        // ── Folder Structure ─────────────────────────────────────────────────────

        static void CreateFolderStructure()
        {
            string[] folders = {
                "Assets/Scenes",
                "Assets/Prefabs",
                "Assets/Prefabs/UI",
                "Assets/Prefabs/Minigames",
                "Assets/Prefabs/Characters",
                "Assets/Resources/Levels",
                "Assets/Resources/Characters",
                "Assets/Resources/Sprites",
                "Assets/Resources/Audio/Music",
                "Assets/Resources/Audio/SFX",
                "Assets/Resources/Audio/Voice/cs-CZ",
                "Assets/Resources/Audio/Voice/en-US",
                "Assets/Resources/Localization/cs-CZ",
                "Assets/Resources/Localization/en-US",
                "Assets/Materials",
                "Assets/Animations",
            };

            foreach (var path in folders)
            {
                var parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string parent = current;
                    current = $"{current}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(current))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                }
            }
        }

        // ── Level ScriptableObjects ───────────────────────────────────────────────

        static void CreateLevelAssets()
        {
            var entries = LevelRegistry.Levels;
            foreach (var e in entries)
            {
                string path = $"Assets/Resources/Levels/Level_{e.index:D2}.asset";
                if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(path) != null) continue;

                var asset = ScriptableObject.CreateInstance<LevelDefinition>();
                asset.levelIndex                    = e.index;
                asset.levelNameKey                  = e.nameKey;
                asset.roomNameKey                   = e.room;
                asset.mathTopic                     = e.topic;
                asset.startingDifficulty            = e.startDiff;
                asset.tasksRequiredToUnlockMinigame = e.tasksRequired;
                asset.minigameType                  = e.minigame;
                asset.minigameNameKey               = $"minigame_{e.minigame.ToString().ToLower()}_name";
                asset.minigameDescriptionKey        = $"minigame_{e.minigame.ToString().ToLower()}_desc";
                asset.assignedCharacter             = e.character;
                asset.unlockAfterLevel              = e.unlockAfter;
                asset.stickerSpriteKey              = $"sticker_level_{e.index}";

                AssetDatabase.CreateAsset(asset, path);
            }
        }

        // ── Character ScriptableObjects ───────────────────────────────────────────

        struct CharData
        {
            public CharacterType type;
            public string brand;
            public CharacterSkill skill;
            public int unlockAfterLevel;
        }

        static readonly CharData[] CharacterData = {
            new CharData { type = CharacterType.Rumble,         brand = "Roomba e5",       skill = CharacterSkill.SpeedBoost,      unlockAfterLevel = -1 },
            new CharData { type = CharacterType.Xixi,           brand = "Xiaomi Mi Robot", skill = CharacterSkill.HintReveal,      unlockAfterLevel = 0  },
            new CharData { type = CharacterType.Rocky,          brand = "Roborock S7",     skill = CharacterSkill.Shield,          unlockAfterLevel = 1  },
            new CharData { type = CharacterType.Bubbles,        brand = "Roborock Mini",   skill = CharacterSkill.DoublePoints,    unlockAfterLevel = 2  },
            new CharData { type = CharacterType.Max,            brand = "Xiaomi S-Series", skill = CharacterSkill.ScoreMultiplier, unlockAfterLevel = 3  },
            new CharData { type = CharacterType.Zara,           brand = "Roborock Q8 Max", skill = CharacterSkill.VisualHint,      unlockAfterLevel = 4  },
            new CharData { type = CharacterType.Turbo,          brand = "Roomba j7+",      skill = CharacterSkill.TaskSkip,        unlockAfterLevel = 5  },
            new CharData { type = CharacterType.ProfessorPebble,brand = "Xiaomi X10 Ultra",skill = CharacterSkill.ExtendedTimer,   unlockAfterLevel = 6  },
            new CharData { type = CharacterType.Luna,           brand = "Roborock S8 Pro", skill = CharacterSkill.SequenceReveal,  unlockAfterLevel = 7  },
        };

        static void CreateCharacterAssets()
        {
            foreach (var c in CharacterData)
            {
                string path = $"Assets/Resources/Characters/{c.type}.asset";
                if (AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path) != null) continue;

                var asset = ScriptableObject.CreateInstance<CharacterDefinition>();
                asset.characterType       = c.type;
                asset.nameKey             = $"char_{c.type.ToString().ToLower()}_name";
                asset.catchphraseKey      = $"char_{c.type.ToString().ToLower()}_catchphrase";
                asset.brandName           = c.brand;
                asset.skill               = c.skill;
                asset.skillDescriptionKey = $"char_{c.type.ToString().ToLower()}_skill";
                asset.unlockAfterLevelIndex = c.unlockAfterLevel;

                AssetDatabase.CreateAsset(asset, path);
            }
        }

        // ── Placeholder Sprites ───────────────────────────────────────────────────
        // Creates colored circle/square textures so the game runs visually
        // before real art is added.

        static readonly (string name, Color color, bool isCircle)[] SpriteData = {
            ("vacuum_rumble",     new Color(0.2f, 0.2f, 0.2f), true),
            ("vacuum_xixi",       new Color(0.9f, 0.9f, 1.0f), true),
            ("vacuum_rocky",      new Color(0.3f, 0.3f, 0.4f), true),
            ("vacuum_bubbles",    new Color(1.0f, 0.8f, 0.9f), true),
            ("vacuum_max",        new Color(0.8f, 0.8f, 0.9f), true),
            ("vacuum_zara",       new Color(0.6f, 0.4f, 0.8f), true),
            ("vacuum_turbo",      new Color(0.2f, 0.2f, 0.3f), true),
            ("vacuum_pebble",     new Color(0.95f,0.9f, 0.8f), true),
            ("vacuum_luna",       new Color(0.15f,0.15f,0.2f), true),
            ("sock",              new Color(1.0f, 0.6f, 0.2f), false),
            ("crumb",             new Color(0.8f, 0.6f, 0.3f), true),
            ("cushion",           new Color(0.4f, 0.7f, 1.0f), false),
            ("duck",              new Color(1.0f, 0.9f, 0.1f), true),
            ("box",               new Color(0.7f, 0.5f, 0.3f), false),
            ("flower",            new Color(1.0f, 0.4f, 0.6f), true),
            ("toy",               new Color(0.4f, 0.8f, 0.4f), true),
            ("star",              new Color(1.0f, 0.85f, 0.1f), false),
            ("blocks",            new Color(0.4f, 0.6f, 1.0f), false),
            ("triangle",          new Color(1.0f, 0.5f, 0.3f), false),
            ("square",            new Color(0.3f, 0.7f, 1.0f), false),
            ("circle",            new Color(0.5f, 1.0f, 0.5f), true),
            ("rectangle",         new Color(0.8f, 0.4f, 0.8f), false),
            ("pentagon",          new Color(1.0f, 0.7f, 0.2f), false),
            ("hexagon",           new Color(0.2f, 0.8f, 0.8f), false),
            ("badge_gold",        new Color(1.0f, 0.84f, 0.0f), true),
            ("badge_silver",      new Color(0.75f,0.75f,0.75f), true),
            ("badge_bronze",      new Color(0.8f, 0.5f, 0.2f), true),
            ("star_filled",       new Color(1.0f, 0.85f, 0.0f), false),
            ("star_empty",        new Color(0.7f, 0.7f, 0.7f), false),
            ("btn_bg",            new Color(0.0f, 0.75f, 0.65f), false),
        };

        static void CreatePlaceholderSprites()
        {
            foreach (var (name, color, isCircle) in SpriteData)
            {
                string path = $"Assets/Resources/Sprites/{name}.png";
                if (File.Exists(path)) continue;

                var tex = isCircle ? CreateCircleTexture(128, color) : CreateRoundedRectTexture(128, 96, color);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);

                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType         = TextureImporterType.Sprite;
                    ti.spriteImportMode    = SpriteImportMode.Single;
                    ti.mipmapEnabled       = false;
                    ti.filterMode          = FilterMode.Bilinear;
                    ti.alphaIsTransparency = true;
                    ti.SaveAndReimport();
                }
            }
        }

        static Texture2D CreateCircleTexture(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size / 2f;
            Vector2 center = new Vector2(r, r);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float aa = Mathf.Clamp01(r - d);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, aa));
            }
            tex.Apply();
            return tex;
        }

        static Texture2D CreateRoundedRectTexture(int w, int h, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float radius = 16f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, w - radius);
                float cy = Mathf.Clamp(py, radius, h - radius);
                float d  = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float aa = Mathf.Clamp01(radius - d);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, aa));
            }
            tex.Apply();
            return tex;
        }

        // ── Scene Creation ────────────────────────────────────────────────────────

        static void CreateAllScenes()
        {
            CreateLanguageSelectScene();
            CreateHomeScene();
            CreateLevelSelectScene();
            CreateMathTaskScene();
            CreateLevelCompleteScene();
            CreateParentDashboardScene();
            CreateMinigameScenes();
            // Stub scenes for remaining
            foreach (string name in new[] { "LevelIntro", "CharacterSelect", "MinigameUnlock", "Settings" })
                CreateStubScene(name);
        }

        static Scene NewScene(string name)
        {
            string path = $"Assets/Scenes/{name}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Set up 2D camera
            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.96f, 0.96f, 1f);
            cam.tag              = "MainCamera";
            cam.transform.position = new Vector3(0, 0, -10);

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler   = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, path);
            return scene;
        }

        // ─── Language Select ────────────────────────────────────────────────────

        static void CreateLanguageSelectScene()
        {
            var scene = NewScene("LanguageSelect");
            var canvas = GameObject.Find("Canvas");

            // Background
            SetBg(canvas, new Color(0.13f, 0.57f, 0.80f));

            // Title
            MakeLabel(canvas, "TitleLabel", "Vyber jazyk / Select Language",
                new Vector2(0, 400), new Vector2(800, 100), 48);

            // Czech button
            var czBtn = MakeButton(canvas, "CzechButton", "🇨🇿  Čeština",
                new Vector2(-220, 0), new Vector2(380, 160));

            // English button
            var enBtn = MakeButton(canvas, "EnglishButton", "🇬🇧  English",
                new Vector2(220, 0), new Vector2(380, 160));

            // Highlight rings
            MakeImage(canvas, "CzechHighlight",  new Vector2(-220, 0),  new Vector2(400, 180), new Color(1,1,0,0.5f));
            MakeImage(canvas, "EnglishHighlight", new Vector2(220,  0), new Vector2(400, 180), new Color(1,1,0,0));

            // Manager singletons
            AddSingletonsToScene();

            // Controller
            var ctrl = canvas.AddComponent<UI.LanguageSelectController>();
            // (Inspector refs assigned via reflection helper below)
            SetField(ctrl, "czechButton",   czBtn.GetComponent<Button>());
            SetField(ctrl, "englishButton", enBtn.GetComponent<Button>());
            SetField(ctrl, "czechHighlight",  canvas.transform.Find("CzechHighlight").GetComponent<Image>());
            SetField(ctrl, "englishHighlight",canvas.transform.Find("EnglishHighlight").GetComponent<Image>());

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LanguageSelect.unity");
        }

        // ─── Home ───────────────────────────────────────────────────────────────

        static void CreateHomeScene()
        {
            var scene = NewScene("Home");
            var canvas = GameObject.Find("Canvas");
            SetBg(canvas, new Color(0.98f, 0.97f, 1f));

            MakeLabel(canvas, "Title", "Akademie Robotů", new Vector2(0, 700), new Vector2(900, 120), 64);
            MakeButton(canvas, "PlayButton",       "▶  Hrát",         new Vector2(0,  200), new Vector2(600, 140));
            MakeButton(canvas, "CharactersButton", "👾  Postavičky",  new Vector2(0,    0), new Vector2(600, 120));
            MakeButton(canvas, "ParentButton",     "👨‍👩‍👧  Rodiče",    new Vector2(0, -160), new Vector2(600, 120));
            MakeButton(canvas, "SettingsButton",   "⚙  Nastavení",    new Vector2(0, -310), new Vector2(600, 120));

            // Vacuum logo placeholder (circle)
            MakeImage(canvas, "Logo", new Vector2(0, 500), new Vector2(200, 200), new Color(0.2f, 0.2f, 0.2f));

            var ctrl = canvas.AddComponent<UI.HomeController>();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Home.unity");
        }

        // ─── Level Select ───────────────────────────────────────────────────────

        static void CreateLevelSelectScene()
        {
            var scene = NewScene("LevelSelect");
            var canvas = GameObject.Find("Canvas");
            SetBg(canvas, new Color(0.95f, 0.97f, 1f));

            MakeLabel(canvas, "Title", "Vyber místnost", new Vector2(0, 850), new Vector2(900, 100), 48);

            // Scroll view for room buttons
            var scrollGO  = new GameObject("ScrollView");
            scrollGO.transform.SetParent(canvas.transform, false);
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            var scrollRt   = scrollGO.GetComponent<RectTransform>();
            scrollRt.anchorMin        = new Vector2(0, 0.05f);
            scrollRt.anchorMax        = new Vector2(1, 0.9f);
            scrollRt.offsetMin        = new Vector2(20, 0);
            scrollRt.offsetMax        = new Vector2(-20, 0);

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            var contentRt = contentGO.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1);
            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing    = 20;
            vlg.padding    = new RectOffset(10, 10, 10, 10);
            vlg.childForceExpandWidth = true;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            // Create 11 room buttons
            var roomButtons = new List<UI.LevelRoomButton>();
            string[] levelNames = {
                "Ložnice","Kuchyně","Obývák","Koupelna","Garáž",
                "Chodba","Zahrada","Půda","Střecha","Velký sál","Tajná lab"
            };
            for (int i = 0; i < 11; i++)
            {
                var btnGO = new GameObject($"RoomButton_{i}");
                btnGO.transform.SetParent(contentGO.transform, false);
                var rt = btnGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 130);
                btnGO.AddComponent<Image>().color = RoomColor(i);
                var btn = btnGO.AddComponent<Button>();

                var nameLbl = MakeLabel(btnGO, "Name", levelNames[i], new Vector2(-200, 20), new Vector2(500, 60), 32);
                var lockImg = MakeImage(btnGO, "Lock", new Vector2(400, 0), new Vector2(60, 60), Color.gray);

                // Stars
                var starRow = new GameObject("Stars");
                starRow.transform.SetParent(btnGO.transform, false);
                var starRt = starRow.AddComponent<RectTransform>();
                starRt.anchoredPosition = new Vector2(-100, -30);
                var hlg = starRow.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                var stars = new Image[3];
                for (int s = 0; s < 3; s++)
                {
                    var sImg = MakeImage(starRow, $"Star{s}", new Vector2(0,0), new Vector2(40,40), new Color(0.7f,0.7f,0.7f));
                    stars[s] = sImg.GetComponent<Image>();
                }

                var rbComp = btnGO.AddComponent<UI.LevelRoomButton>();
                SetField(rbComp, "button",     btn);
                SetField(rbComp, "nameLabel",  nameLbl.GetComponent<TextMeshProUGUI>());
                SetField(rbComp, "lockIcon",   lockImg.GetComponent<Image>());
                SetField(rbComp, "starImages", stars);
                roomButtons.Add(rbComp);
            }

            var ctrl = canvas.AddComponent<UI.LevelSelectController>();
            SetField(ctrl, "roomButtons", roomButtons.ToArray());

            MakeButton(canvas, "BackButton", "← Zpět", new Vector2(0, -850), new Vector2(400, 100));

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LevelSelect.unity");
        }

        // ─── Math Task ──────────────────────────────────────────────────────────

        static void CreateMathTaskScene()
        {
            var scene = NewScene("MathTask");
            var canvas = GameObject.Find("Canvas");
            SetBg(canvas, new Color(0.97f, 0.98f, 1f));

            // Character area (top left)
            var charArea = MakeImage(canvas, "CharacterArea", new Vector2(-380, 700), new Vector2(250, 250),
                new Color(0.9f, 0.9f, 1f, 0.3f));
            var charAnim = charArea.AddComponent<Animator>();

            // Progress bar (top)
            var progressBg = MakeImage(canvas, "ProgressBG", new Vector2(100, 870), new Vector2(600, 40), new Color(0.85f,0.85f,0.95f));
            var progressFill = MakeImage(progressBg, "ProgressFill", Vector2.zero, Vector2.zero, new Color(0f, 0.75f, 0.65f));
            var progressSlider = progressBg.AddComponent<Slider>();
            progressSlider.fillRect = progressFill.GetComponent<RectTransform>();
            progressSlider.minValue = 0; progressSlider.maxValue = 1; progressSlider.value = 0;
            var progressLbl = MakeLabel(canvas, "ProgressLabel", "0 / 10", new Vector2(100, 830), new Vector2(300, 50), 28);

            // Hint button
            var hintBtn  = MakeButton(canvas, "HintButton", "💡 Nápověda", new Vector2(380, 870), new Vector2(220, 70));
            var hintLbl  = MakeLabel(canvas, "HintCount", "(3 zbývá)", new Vector2(380, 810), new Vector2(220, 40), 22);

            // Streak indicator (hidden initially)
            var streakGO = MakeImage(canvas, "StreakIndicator", new Vector2(0, 750), new Vector2(400, 70), new Color(1f,0.7f,0f,0.9f));
            streakGO.SetActive(false);
            var streakLbl = MakeLabel(streakGO, "StreakLabel", "Série 5! 🔥", Vector2.zero, new Vector2(400,70), 32);

            // Question display
            var questionLbl = MakeLabel(canvas, "QuestionText", "Kolik jich je?",
                new Vector2(0, 500), new Vector2(900, 100), 48);

            // Operand row
            var opA   = MakeLabel(canvas, "OperandA",  "3", new Vector2(-150, 350), new Vector2(120, 100), 72);
            var opSym = MakeLabel(canvas, "Operator",  "+", new Vector2(0,   350), new Vector2(80,  100), 72);
            var opB   = MakeLabel(canvas, "OperandB",  "5", new Vector2(150,  350), new Vector2(120, 100), 72);

            // Visual object container
            var visualContainer = new GameObject("VisualObjectContainer");
            visualContainer.transform.SetParent(canvas.transform, false);
            var vcRt = visualContainer.AddComponent<RectTransform>();
            vcRt.anchoredPosition = new Vector2(0, 180);
            vcRt.sizeDelta        = new Vector2(800, 200);
            var vlg2 = visualContainer.AddComponent<GridLayoutGroup>();
            vlg2.cellSize    = new Vector2(60, 60);
            vlg2.spacing     = new Vector2(10, 10);
            vlg2.startCorner = GridLayoutGroup.Corner.UpperLeft;
            vlg2.constraint  = GridLayoutGroup.Constraint.FixedColumnCount;
            vlg2.constraintCount = 10;

            // Visual object prefab (image placeholder)
            var voImgGO = new GameObject("VisualObjectPrefab");
            voImgGO.transform.SetParent(canvas.transform, false);
            var voImg   = voImgGO.AddComponent<Image>();
            voImg.color = new Color(0.4f, 0.7f, 1f);
            voImgGO.SetActive(false);

            // Answer buttons (3)
            var choiceBtns  = new Button[3];
            var choiceLbls  = new TextMeshProUGUI[3];
            float[] xPos    = { -320f, 0f, 320f };
            for (int i = 0; i < 3; i++)
            {
                var btn = MakeButton(canvas, $"Choice_{i}", (i + 1).ToString(),
                    new Vector2(xPos[i], -300), new Vector2(280, 180));
                choiceBtns[i] = btn.GetComponent<Button>();
                choiceLbls[i] = btn.GetComponentInChildren<TextMeshProUGUI>();
                choiceLbls[i].fontSize = 72;
            }

            // Feedback text
            var feedbackLbl = MakeLabel(canvas, "FeedbackText", "",
                new Vector2(0, -520), new Vector2(800, 80), 36);
            feedbackLbl.GetComponent<TextMeshProUGUI>().color = new Color(0.3f, 0.7f, 0.5f);

            // Correct particle system placeholder
            var particleGO = new GameObject("CorrectParticles");
            particleGO.transform.SetParent(canvas.transform, false);
            particleGO.SetActive(false);

            // Wrong shake placeholder
            var shakeGO = new GameObject("WrongShake");
            shakeGO.transform.SetParent(canvas.transform, false);
            shakeGO.SetActive(false);

            // Professor Pebble re-teach panel
            var reteachPanel = new GameObject("ReteachPanel");
            reteachPanel.transform.SetParent(canvas.transform, false);
            var rtRt = reteachPanel.AddComponent<RectTransform>();
            rtRt.anchoredPosition = new Vector2(0, -650);
            rtRt.sizeDelta        = new Vector2(860, 140);
            reteachPanel.AddComponent<Image>().color = new Color(1f, 0.95f, 0.8f, 0.95f);
            var reteachLbl = MakeLabel(reteachPanel, "ReteachText",
                "Pojďme si to zopakovat!", Vector2.zero, new Vector2(800, 120), 32);
            reteachPanel.SetActive(false);

            // Wire up TaskDisplayController
            var ctrl = canvas.AddComponent<UI.TaskDisplayController>();
            SetField(ctrl, "questionText",          questionLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "visualObjectContainer", visualContainer);
            SetField(ctrl, "visualObjectPrefab",    voImg);
            SetField(ctrl, "operandAText",          opA.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "operatorText",          opSym.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "operandBText",          opB.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "choiceButtons",         choiceBtns);
            SetField(ctrl, "choiceLabels",          choiceLbls);
            SetField(ctrl, "progressBar",           progressSlider);
            SetField(ctrl, "progressLabel",         progressLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "hintButton",            hintBtn.GetComponent<Button>());
            SetField(ctrl, "hintCountLabel",        hintLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "streakIndicator",       streakGO);
            SetField(ctrl, "streakLabel",           streakLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "feedbackText",          feedbackLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "characterAnimator",     charAnim);
            SetField(ctrl, "reteachPanel",          reteachPanel);
            SetField(ctrl, "reteachText",           reteachLbl.GetComponent<TextMeshProUGUI>());

            hintBtn.GetComponent<Button>().onClick.AddListener(ctrl.OnHintTapped);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MathTask.unity");
        }

        // ─── Level Complete ─────────────────────────────────────────────────────

        static void CreateLevelCompleteScene()
        {
            var scene = NewScene("LevelComplete");
            var canvas = GameObject.Find("Canvas");
            SetBg(canvas, new Color(0.95f, 1f, 0.97f));

            var titleLbl = MakeLabel(canvas, "TitleText", "Skvělá práce!",
                new Vector2(0, 600), new Vector2(800, 120), 64);

            // 3 stars
            var stars = new Image[3];
            float[] starX = { -200f, 0f, 200f };
            for (int i = 0; i < 3; i++)
            {
                var s = MakeImage(canvas, $"Star_{i}", new Vector2(starX[i], 350), new Vector2(150, 150),
                    new Color(0.7f, 0.7f, 0.7f));
                stars[i] = s.GetComponent<Image>();
            }

            var coinsLbl  = MakeLabel(canvas, "CoinsEarned", "",     new Vector2(0,  150), new Vector2(600, 70), 36);
            var stickerLbl = MakeLabel(canvas, "StickerPopup", "",   new Vector2(0,   50), new Vector2(700, 70), 32);

            var nextBtn   = MakeButton(canvas, "NextLevel",   "Další úroveň →", new Vector2(0, -150), new Vector2(600, 130));
            var replayBtn = MakeButton(canvas, "ReplayButton","↺ Znovu",         new Vector2(0, -320), new Vector2(600, 110));
            var homeBtn   = MakeButton(canvas, "HomeButton",  "🏠 Domů",         new Vector2(0, -460), new Vector2(600, 110));

            var confettiGO = new GameObject("Confetti");
            confettiGO.transform.SetParent(canvas.transform, false);
            var confettiAnim = confettiGO.AddComponent<Animator>();

            var ctrl = canvas.AddComponent<UI.LevelCompleteController>();
            SetField(ctrl, "starImages",         stars);
            SetField(ctrl, "titleText",          titleLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "coinsEarnedText",    coinsLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "stickerPopup",       stickerLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "nextLevelButton",    nextBtn.GetComponent<Button>());
            SetField(ctrl, "replayButton",       replayBtn.GetComponent<Button>());
            SetField(ctrl, "homeButton",         homeBtn.GetComponent<Button>());
            SetField(ctrl, "confettiAnimator",   confettiAnim);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LevelComplete.unity");
        }

        // ─── Parent Dashboard ───────────────────────────────────────────────────

        static void CreateParentDashboardScene()
        {
            var scene = NewScene("ParentDashboard");
            var canvas = GameObject.Find("Canvas");
            SetBg(canvas, new Color(0.95f, 0.95f, 1f));

            // PIN Gate panel
            var pinPanel = new GameObject("PinGatePanel");
            pinPanel.transform.SetParent(canvas.transform, false);
            pinPanel.AddComponent<RectTransform>().sizeDelta = new Vector2(800, 600);
            pinPanel.AddComponent<Image>().color = new Color(0.2f, 0.3f, 0.5f, 0.97f);

            MakeLabel(pinPanel, "PinTitle", "Přehled pro rodiče",
                new Vector2(0, 200), new Vector2(700, 80), 44);
            MakeLabel(pinPanel, "PinInstruction", "Podrž číslo 7 po dobu 3 sekund",
                new Vector2(0, 100), new Vector2(700, 60), 30);

            var pinHoldBg = MakeImage(pinPanel, "PinHoldBG", new Vector2(0, -20), new Vector2(400, 40), new Color(0.5f,0.5f,0.7f));
            var pinFill   = MakeImage(pinHoldBg, "PinFill",   Vector2.zero, Vector2.zero, new Color(0f,0.8f,0.6f));
            var pinSlider = pinHoldBg.AddComponent<Slider>();
            pinSlider.fillRect = pinFill.GetComponent<RectTransform>();
            pinSlider.value = 0; pinSlider.minValue = 0; pinSlider.maxValue = 1;

            var pinHoldBtn = MakeButton(pinPanel, "PinHoldButton", "7",
                new Vector2(0, -150), new Vector2(200, 200));

            // Dashboard panel (hidden initially)
            var dashPanel = new GameObject("DashboardPanel");
            dashPanel.transform.SetParent(canvas.transform, false);
            dashPanel.AddComponent<RectTransform>().sizeDelta = new Vector2(1000, 1800);
            dashPanel.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);
            dashPanel.SetActive(false);

            MakeLabel(dashPanel, "DashTitle", "Přehled pro rodiče",
                new Vector2(0, 800), new Vector2(800, 80), 44);

            var timeLbl  = MakeLabel(dashPanel, "TotalTime",  "0 minut",  new Vector2(0, 680), new Vector2(700, 60), 32);
            var starsLbl = MakeLabel(dashPanel, "TotalStars", "0 hvězd",  new Vector2(0, 610), new Vector2(700, 60), 32);
            var coinsLbl = MakeLabel(dashPanel, "CoinsLabel", "0 mincí",  new Vector2(0, 540), new Vector2(700, 60), 32);

            var closeBtnGO = MakeButton(dashPanel, "CloseButton", "✕ Zavřít",
                new Vector2(0, -820), new Vector2(500, 110));
            var resetBtnGO = MakeButton(dashPanel, "ResetProgress", "⚠ Smazat postup",
                new Vector2(0, -700), new Vector2(500, 100));

            var ctrl = canvas.AddComponent<UI.ParentDashboardController>();
            SetField(ctrl, "pinGatePanel",        pinPanel);
            SetField(ctrl, "pinHoldButton",       pinHoldBtn.GetComponent<Button>());
            SetField(ctrl, "pinHoldProgress",     pinSlider);
            SetField(ctrl, "dashboardPanel",      dashPanel);
            SetField(ctrl, "totalTimeLabel",      timeLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "totalStarsLabel",     starsLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "coinsLabel",          coinsLbl.GetComponent<TextMeshProUGUI>());
            SetField(ctrl, "resetProgressButton", resetBtnGO.GetComponent<Button>());
            SetField(ctrl, "closeButton",         closeBtnGO.GetComponent<Button>());

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/ParentDashboard.unity");
        }

        // ─── Minigame Scenes ────────────────────────────────────────────────────

        static void CreateMinigameScenes()
        {
            // Each minigame gets a base canvas with common UI elements wired to BaseMinigame.
            string[] names = {
                "SockSortSweep","CrumbCollectCountdown","CushionCannonCatch",
                "DrainDefense","BoxTowerBuilder","StreamerUntangleSprint",
                "FlowerBedFrenzy","AtticBinBlitz","SequenceSprinkler",
                "GrandHallRestoration","ScramblerShutdown"
            };

            foreach (var name in names)
                CreateMinigameScene(name);
        }

        static void CreateMinigameScene(string minigameName)
        {
            var scene = NewScene($"Minigame_{minigameName}");
            var canvas = GameObject.Find("Canvas");
            SetBg(canvas, new Color(0.93f, 0.97f, 1f));

            MakeLabel(canvas, "MinigameTitle", minigameName,
                new Vector2(0, 880), new Vector2(900, 80), 36);

            // Score
            var scoreLbl = MakeLabel(canvas, "ScoreText", "0",
                new Vector2(400, 880), new Vector2(200, 70), 44);

            // Timer bar
            var timerBg   = MakeImage(canvas, "TimerBG", new Vector2(0, 820), new Vector2(900, 30), new Color(0.8f,0.8f,0.9f));
            var timerFill = MakeImage(timerBg, "TimerFill", Vector2.zero, Vector2.zero, new Color(0.2f, 0.7f, 0.4f));
            var timerSlider = timerBg.AddComponent<Slider>();
            timerSlider.fillRect = timerFill.GetComponent<RectTransform>();
            timerSlider.value = 1; timerSlider.minValue = 0; timerSlider.maxValue = 1;
            var timerLbl = MakeLabel(canvas, "TimerText", "60", new Vector2(-420, 820), new Vector2(80, 40), 28);

            // Instruction panel
            var instrPanel = new GameObject("InstructionPanel");
            instrPanel.transform.SetParent(canvas.transform, false);
            instrPanel.AddComponent<RectTransform>().sizeDelta = new Vector2(860, 700);
            instrPanel.AddComponent<Image>().color = new Color(0.2f, 0.3f, 0.5f, 0.95f);
            var instrLbl = MakeLabel(instrPanel, "InstructionText", "Instructions here...",
                new Vector2(0, 80), new Vector2(800, 400), 34);
            var startBtn = MakeButton(instrPanel, "StartButton", "Start!",
                new Vector2(0, -220), new Vector2(500, 130));

            // Completion panel
            var compPanel = new GameObject("CompletionPanel");
            compPanel.transform.SetParent(canvas.transform, false);
            compPanel.AddComponent<RectTransform>().sizeDelta = new Vector2(800, 600);
            compPanel.AddComponent<Image>().color = new Color(0.95f, 1f, 0.95f, 0.97f);
            compPanel.SetActive(false);

            var compScore = MakeLabel(compPanel, "CompletionScore", "0",
                new Vector2(0, 150), new Vector2(600, 120), 72);
            MakeLabel(compPanel, "CompletionLabel", "Body",
                new Vector2(0, 50), new Vector2(400, 60), 36);

            var compStars = new Image[3];
            float[] sx = { -200f, 0f, 200f };
            for (int i = 0; i < 3; i++)
            {
                var s = MakeImage(compPanel, $"CompStar_{i}", new Vector2(sx[i], -80), new Vector2(120, 120),
                    new Color(0.7f, 0.7f, 0.7f));
                compStars[i] = s.GetComponent<Image>();
            }

            MakeButton(compPanel, "CompContinueBtn", "Pokračovat →",
                new Vector2(0, -260), new Vector2(500, 110));

            // Add the specific minigame component
            System.Type mgType = System.Type.GetType($"VacuumVille.Minigames.{minigameName}, Assembly-CSharp");
            Minigames.BaseMinigame mgComp = mgType != null
                ? (Minigames.BaseMinigame)canvas.AddComponent(mgType)
                : canvas.AddComponent<Minigames.BaseMinigameStub>();

            if (mgComp != null)
            {
                SetField(mgComp, "scoreText",       scoreLbl.GetComponent<TextMeshProUGUI>());
                SetField(mgComp, "timerBar",        timerSlider);
                SetField(mgComp, "timerText",       timerLbl.GetComponent<TextMeshProUGUI>());
                SetField(mgComp, "instructionPanel",instrPanel);
                SetField(mgComp, "instructionText", instrLbl.GetComponent<TextMeshProUGUI>());
                SetField(mgComp, "startButton",     startBtn.GetComponent<Button>());
                SetField(mgComp, "completionPanel", compPanel);
                SetField(mgComp, "completionScoreText", compScore.GetComponent<TextMeshProUGUI>());
                SetField(mgComp, "completionStars", compStars);
            }

            // Vacuum character placeholder
            var vacuumGO = new GameObject("Vacuum");
            vacuumGO.transform.SetParent(canvas.transform, false);
            var vacImg = vacuumGO.AddComponent<Image>();
            vacImg.color = new Color(0.2f, 0.2f, 0.3f);
            var vacRt = vacuumGO.GetComponent<RectTransform>();
            vacRt.anchoredPosition = new Vector2(0, -600);
            vacRt.sizeDelta        = new Vector2(150, 150);

            // Attach vacuumTransform reference if field exists
            SetField(mgComp, "vacuumTransform", vacuumGO.transform);

            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/Minigame_{minigameName}.unity");
        }

        // ─── Stub scene ─────────────────────────────────────────────────────────

        static void CreateStubScene(string name)
        {
            var scene = NewScene(name);
            var canvas = GameObject.Find("Canvas");
            SetBg(canvas, new Color(0.97f, 0.97f, 1f));
            MakeLabel(canvas, "Title", name, new Vector2(0, 200), new Vector2(900, 120), 56);
            MakeButton(canvas, "BackButton", "← Zpět", new Vector2(0, -200), new Vector2(500, 120));
            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{name}.unity");
        }

        // ── Build Settings ────────────────────────────────────────────────────────

        static void RegisterScenesInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            // LanguageSelect first = index 0 (boot scene)
            var ordered = new List<string> { "LanguageSelect", "Home", "LevelSelect",
                "LevelIntro", "CharacterSelect", "MathTask", "MinigameUnlock",
                "LevelComplete", "ParentDashboard", "Settings" };
            ordered.AddRange(new[] {
                "Minigame_SockSortSweep","Minigame_CrumbCollectCountdown",
                "Minigame_CushionCannonCatch","Minigame_DrainDefense",
                "Minigame_BoxTowerBuilder","Minigame_StreamerUntangleSprint",
                "Minigame_FlowerBedFrenzy","Minigame_AtticBinBlitz",
                "Minigame_SequenceSprinkler","Minigame_GrandHallRestoration",
                "Minigame_ScramblerShutdown"
            });

            foreach (var sn in ordered)
            {
                string path = $"Assets/Scenes/{sn}.unity";
                if (File.Exists(path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ── UI Builder Helpers ────────────────────────────────────────────────────

        static GameObject MakeLabel(GameObject parent, string goName, string text,
            Vector2 pos, Vector2 size, float fontSize)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = new Color(0.15f, 0.15f, 0.2f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing   = false;
            tmp.overflowMode       = TextOverflowModes.Ellipsis;
            return go;
        }

        static GameObject MakeButton(GameObject parent, string goName, string label,
            Vector2 pos, Vector2 size)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0.75f, 0.65f);
            go.AddComponent<Button>();

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lrt  = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp  = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = Mathf.Clamp(size.y * 0.35f, 24, 52);
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        static GameObject MakeImage(GameObject parent, string goName,
            Vector2 pos, Vector2 size, Color color)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        static void SetBg(GameObject canvas, Color color)
        {
            var cam = Camera.main;
            if (cam) cam.backgroundColor = color;
        }

        static void AddSingletonsToScene()
        {
            var mgr = new GameObject("[GameManager]");
            mgr.AddComponent<Core.GameManager>();

            var audio = new GameObject("[AudioManager]");
            var am = audio.AddComponent<Core.AudioManager>();
            audio.AddComponent<AudioSource>(); // music
            audio.AddComponent<AudioSource>(); // sfx
            audio.AddComponent<AudioSource>(); // voice

            var loc = new GameObject("[LocalizationManager]");
            loc.AddComponent<Core.LocalizationManager>();
        }

        static Color RoomColor(int index)
        {
            Color[] colors = {
                new Color(0.9f, 0.8f, 1.0f), new Color(1.0f, 0.9f, 0.7f),
                new Color(0.7f, 0.9f, 1.0f), new Color(0.7f, 1.0f, 0.9f),
                new Color(1.0f, 0.8f, 0.7f), new Color(0.8f, 0.7f, 1.0f),
                new Color(0.8f, 1.0f, 0.7f), new Color(1.0f, 0.7f, 0.8f),
                new Color(0.7f, 0.8f, 1.0f), new Color(1.0f, 1.0f, 0.7f),
                new Color(0.9f, 0.9f, 0.9f)
            };
            return index < colors.Length ? colors[index] : Color.white;
        }

        // ── Reflection Field Setter ───────────────────────────────────────────────

        static void SetField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;
            var type  = target.GetType();
            System.Reflection.FieldInfo fi = null;
            while (type != null && fi == null)
            {
                fi = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                type = type.BaseType;
            }
            if (fi != null) fi.SetValue(target, value);
            else Debug.LogWarning($"[Setup] Field not found: {fieldName} on {target.GetType().Name}");
        }

        static void Progress(int step, int total, string msg)
            => EditorUtility.DisplayProgressBar("VacuumVille Setup", msg, (float)step / total);
    }
}
#endif
