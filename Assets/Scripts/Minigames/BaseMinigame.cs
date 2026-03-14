using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Base class for all minigames. Handles timer, score, star rating, and completion.
    /// </summary>
    public abstract class BaseMinigame : MonoBehaviour
    {
        [Header("Common UI")]
        [SerializeField] protected TextMeshProUGUI scoreText;
        [SerializeField] protected TextMeshProUGUI timerText;
        [SerializeField] protected Slider timerBar;
        [SerializeField] protected GameObject instructionPanel;
        [SerializeField] protected TextMeshProUGUI instructionText;
        [SerializeField] protected Button startButton;
        [SerializeField] protected GameObject completionPanel;
        [SerializeField] protected TextMeshProUGUI completionScoreText;
        [SerializeField] protected Image[] completionStars;
        [SerializeField] protected Sprite starFilled;

        protected int Score;
        protected bool GameActive;

        protected virtual float TimeLimit => 60f;
        protected virtual int MaxScore   => 100;

        private float _timeRemaining;
        private Coroutine _timerCoroutine;

        protected virtual void Start()
        {
            if (instructionPanel)
            {
                instructionPanel.SetActive(true);
                instructionText.text = LocalizationManager.Instance.Get(
                    $"minigame_instruction_{GetType().Name.ToLower()}");
            }

            startButton?.onClick.AddListener(BeginMinigame);
            UpdateScoreUI();
        }

        protected void BeginMinigame()
        {
            if (instructionPanel) instructionPanel.SetActive(false);

            if (!IsSetupComplete())
            {
                Debug.LogWarning($"[{GetType().Name}] Scene not fully configured — auto-completing as placeholder.");
                AddScore(50);
                FinishMinigame();
                return;
            }

            GameActive = true;
            _timeRemaining = TimeLimit;
            _timerCoroutine = StartCoroutine(TimerLoop());
            OnMinigameBegin();
        }

        /// <summary>
        /// Override to validate required scene references. Return false to auto-complete (placeholder mode).
        /// </summary>
        protected virtual bool IsSetupComplete() => true;

        protected abstract void OnMinigameBegin();

        protected virtual IEnumerator TimerLoop()
        {
            while (_timeRemaining > 0 && GameActive)
            {
                _timeRemaining -= Time.deltaTime;
                if (timerBar)  timerBar.value = _timeRemaining / TimeLimit;
                if (timerText) timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();
                yield return null;
            }
            if (GameActive) FinishMinigame();
        }

        protected void AddScore(int points)
        {
            Score += points;
            UpdateScoreUI();
        }

        protected void UpdateScoreUI()
        {
            if (scoreText) scoreText.text = Score.ToString();
        }

        protected void FinishMinigame()
        {
            GameActive = false;
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            OnMinigameEnd();
            ShowCompletion();
            GameManager.Instance.CompleteMinigame(Score);
        }

        protected virtual void OnMinigameEnd() { }

        private void ShowCompletion()
        {
            if (!completionPanel) return;
            completionPanel.SetActive(true);
            completionScoreText.text = Score.ToString();

            int stars = Score >= MaxScore * 0.9f ? 3 : Score >= MaxScore * 0.6f ? 2 : 1;
            for (int i = 0; i < completionStars.Length; i++)
                completionStars[i].sprite = i < stars ? starFilled
                    : completionStars[i].sprite; // leave empty sprite as-is
        }

        protected void CompleteEarly() => FinishMinigame();
    }
}
