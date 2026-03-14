using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VacuumVille.Data;

namespace VacuumVille.Core
{
    /// <summary>
    /// Central state machine. Single source of truth for current game state,
    /// active level, and player progress. Persists across scenes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Set by Bootstrapper before AddComponent so Start() skips auto-navigation.</summary>
        internal static bool IsBootstrapped = false;

        // ── State ───────────────────────────────────────────────────────────────
        public GameState CurrentState { get; private set; } = GameState.Boot;
        public event Action<GameState, GameState> OnStateChanged; // (from, to)

        // ── Session data ────────────────────────────────────────────────────────
        public PlayerProgress Progress { get; private set; }
        public LevelDefinition[] AllLevels { get; private set; }
        public CharacterDefinition[] AllCharacters { get; private set; }

        // Active level session
        public LevelDefinition ActiveLevel { get; private set; }
        public int TasksCompletedThisSession { get; private set; }
        public int FirstAttemptCorrectThisSession { get; private set; }

        // Session timer
        private DateTime _sessionStart;
        private Coroutine _sessionTimerCoroutine;

        // ── Scene names ─────────────────────────────────────────────────────────
        private const string SceneLanguageSelect  = "LanguageSelect";
        private const string SceneHome           = "Home";
        private const string SceneLevelSelect    = "LevelSelect";
        private const string SceneLevelIntro     = "LevelIntro";
        private const string SceneCharacterSelect = "CharacterSelect";
        private const string SceneMathTask       = "MathTask";
        private const string SceneMinigameUnlock = "MinigameUnlock";
        private const string SceneLevelComplete  = "LevelComplete";
        private const string SceneMinigamePrefix = "Minigame_";
        private const string SceneParentDash     = "ParentDashboard";
        private const string SceneSettings       = "Settings";
        private const string SceneAchievements   = "Achievements";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Progress = SaveSystem.Load();
            AllLevels     = Resources.LoadAll<LevelDefinition>("Levels");
            AllCharacters = Resources.LoadAll<CharacterDefinition>("Characters");

            Array.Sort(AllLevels, (a, b) => a.levelIndex.CompareTo(b.levelIndex));
        }

        private void Start()
        {
            _sessionStart = DateTime.Now;
            _sessionTimerCoroutine = StartCoroutine(TrackSessionTime());

            // IsBootstrapped = true when Bootstrapper created us mid-scene (editor testing).
            // In that case, stay in the current scene instead of redirecting to LanguageSelect.
            if (!IsBootstrapped)
                TransitionTo(GameState.LanguageSelect);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Save(Progress);
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Save(Progress);
        }

        // ── State Machine ───────────────────────────────────────────────────────

        public void TransitionTo(GameState newState)
        {
            GameState previous = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(previous, newState);

            switch (newState)
            {
                case GameState.LanguageSelect:
                    LoadScene(SceneLanguageSelect);
                    break;
                case GameState.Home:
                    LoadScene(SceneHome);
                    break;
                case GameState.LevelSelect:
                    LoadScene(SceneLevelSelect);
                    break;
                case GameState.LevelIntro:
                    LoadScene(SceneLevelIntro);
                    break;
                case GameState.CharacterSelect:
                    LoadScene(SceneCharacterSelect);
                    break;
                case GameState.MathTask:
                    ResetSessionCounters();
                    LoadScene(SceneMathTask);
                    break;
                case GameState.MinigameUnlock:
                    LoadScene(SceneMinigameUnlock);
                    break;
                case GameState.Minigame:
                    string minigameScene = SceneMinigamePrefix + ActiveLevel.minigameType.ToString();
                    LoadScene(minigameScene);
                    break;
                case GameState.LevelComplete:
                    LoadScene(SceneLevelComplete);
                    break;
                case GameState.ParentDashboard:
                    LoadScene(SceneParentDash);
                    break;
                case GameState.Settings:
                    LoadScene(SceneSettings);
                    break;
                case GameState.Achievements:
                    LoadScene(SceneAchievements);
                    break;
            }
        }

        // ── Level Control ───────────────────────────────────────────────────────

        public void StartLevel(LevelDefinition level)
        {
            ActiveLevel = level;
            TransitionTo(GameState.LevelIntro);
        }

        public void BeginMathTasks()
        {
            TransitionTo(GameState.MathTask);
        }

        /// <summary>Called by TaskDisplayController after each problem attempt.</summary>
        public void RecordTaskResult(bool firstAttemptCorrect)
        {
            TasksCompletedThisSession++;
            if (firstAttemptCorrect) FirstAttemptCorrectThisSession++;

            var ta = Progress.GetOrCreateTopicAccuracy(ActiveLevel.mathTopic);
            ta.RecordResult(firstAttemptCorrect);

            if (TasksCompletedThisSession >= ActiveLevel.tasksRequiredToUnlockMinigame)
                TransitionTo(GameState.MinigameUnlock);
        }

        public void UnlockAndStartMinigame()
        {
            var lp = Progress.GetOrCreateLevel(ActiveLevel.levelIndex);
            lp.minigameUnlocked = true;
            SaveSystem.Save(Progress);
            TransitionTo(GameState.Minigame);
        }

        public void CompleteMinigame(int score)
        {
            var lp = Progress.GetOrCreateLevel(ActiveLevel.levelIndex);
            if (score > lp.bestMinigameScore) lp.bestMinigameScore = score;
            lp.completed = true;

            // Calculate stars
            float accuracy = TasksCompletedThisSession > 0
                ? (float)FirstAttemptCorrectThisSession / TasksCompletedThisSession * 100f
                : 0f;

            int stars = 1;
            if (accuracy >= ActiveLevel.starThreshold3) stars = 3;
            else if (accuracy >= ActiveLevel.starThreshold2) stars = 2;

            if (stars > lp.stars) lp.stars = stars;

            // Recalculate total
            Progress.totalStars = 0;
            foreach (var l in Progress.levels) Progress.totalStars += l.stars;

            // Sticker
            if (!lp.stickerCollected)
            {
                lp.stickerCollected = true;
                if (!Progress.collectedStickers.Contains(ActiveLevel.levelIndex))
                    Progress.collectedStickers.Add(ActiveLevel.levelIndex);
            }

            // Unlock next character
            UnlockCharacterForLevel(ActiveLevel.levelIndex + 1);

            SaveSystem.Save(Progress);
            TransitionTo(GameState.LevelComplete);
        }

        private void UnlockCharacterForLevel(int levelIndex)
        {
            foreach (var ch in AllCharacters)
            {
                if (ch.unlockAfterLevelIndex == levelIndex - 1)
                    Progress.UnlockCharacter(ch.characterType);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private void ResetSessionCounters()
        {
            TasksCompletedThisSession = 0;
            FirstAttemptCorrectThisSession = 0;
        }

        private void LoadScene(string sceneName)
        {
            Debug.Log($"[GameManager] Loading scene: {sceneName}");
            SceneManager.LoadSceneAsync(sceneName);
        }

        private IEnumerator TrackSessionTime()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f);
                Progress.totalMinutesPlayed++;
            }
        }

        public LevelDefinition GetLevel(int index)
            => Array.Find(AllLevels, l => l.levelIndex == index);

        public CharacterDefinition GetCharacter(CharacterType type)
            => Array.Find(AllCharacters, c => c.characterType == type);

        public bool IsLevelUnlocked(int levelIndex)
            => Progress.IsLevelUnlocked(levelIndex, AllLevels);
    }
}
