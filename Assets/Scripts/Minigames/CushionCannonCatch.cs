using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;

namespace VacuumVille.Minigames
{
    /// <summary>
    /// Level 3 Minigame: Two streams of cushions merge into one pile.
    /// Player taps the correct sum before the pile lands on Rocky.
    /// 5 rounds, increasing speed each round.
    /// </summary>
    public class CushionCannonCatch : BaseMinigame
    {
        [Header("Cannon Catch")]
        [SerializeField] private Transform cannonLeft;
        [SerializeField] private Transform cannonRight;
        [SerializeField] private Transform mergePoint;
        [SerializeField] private Transform landingPoint;
        [SerializeField] private GameObject cushionPrefab;
        [SerializeField] private Button[] answerButtons;   // 3 buttons
        [SerializeField] private TextMeshProUGUI[] answerLabels;
        [SerializeField] private Animator vacuumAnimator;
        [SerializeField] private float initialFallSpeed = 2f;

        private int _round;
        private int _a, _b, _correct;
        private float _currentSpeed;
        private bool _awaitingAnswer;
        private GameObject _flyingPile;

        protected override float TimeLimit => 120f;
        protected override int MaxScore   => 5;

        protected override bool IsSetupComplete() =>
            cushionPrefab != null && answerButtons != null && answerButtons.Length > 0;

        protected override void OnMinigameBegin()
        {
            AudioManager.Instance?.PlaySFX("Audio/SFX/shared/vacuum_start");
            _currentSpeed = initialFallSpeed;
            EnsurePositions();
            StartNextRound();
        }

        private void EnsurePositions()
        {
            if (cannonLeft  == null) cannonLeft  = CreateAnchor("CannonLeft",  new Vector2(-280,  380));
            if (cannonRight == null) cannonRight = CreateAnchor("CannonRight", new Vector2( 280,  380));
            if (mergePoint  == null) mergePoint  = CreateAnchor("MergePoint",  new Vector2(   0,  100));
            if (landingPoint== null) landingPoint= CreateAnchor("LandingPoint",new Vector2(   0, -380));
        }

        private Transform CreateAnchor(string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        private void StartNextRound()
        {
            if (_round >= 5) { CompleteEarly(); return; }
            _round++;
            _currentSpeed += 0.3f;

            _a = Random.Range(1, 6);
            _b = Random.Range(1, 6);
            _correct = _a + _b;

            SetupAnswerButtons();
            StartCoroutine(AnimateCushions());
        }

        private void SetupAnswerButtons()
        {
            // Reposition scene-placed buttons to on-screen bottom strip
            if (answerButtons != null)
            {
                float[] xMin = { 0.02f, 0.36f, 0.70f };
                float[] xMax = { 0.32f, 0.66f, 0.98f };
                for (int i = 0; i < answerButtons.Length && i < xMin.Length; i++)
                {
                    if (answerButtons[i] == null) continue;
                    var rt = (RectTransform)answerButtons[i].transform;
                    if (rt.parent != transform) rt.SetParent(transform, false);
                    rt.anchorMin = new Vector2(xMin[i], 0.02f);
                    rt.anchorMax = new Vector2(xMax[i], 0.13f);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                }
            }

            var choices = GenerateChoices(_correct, 2, 10);
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int val = choices[i];
                int idx = i;
                answerButtons[i].gameObject.SetActive(true);
                answerLabels[i].text = val.ToString();
                answerLabels[i].color = new Color(0.1f, 0.1f, 0.2f);
                answerButtons[i].interactable = true;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerTapped(val));
                var img = answerButtons[i].GetComponent<Image>();
                if (img) img.color = Color.white;
            }
        }

        private IEnumerator AnimateCushions()
        {
            _awaitingAnswer = false;

            // Spawn left group (parented to Canvas so UI elements render)
            var leftPile = Instantiate(cushionPrefab, transform);
            leftPile.transform.position = cannonLeft.position;
            var leftLbl  = leftPile.GetComponentInChildren<TextMeshProUGUI>();
            if (leftLbl) { leftLbl.text = _a.ToString(); leftLbl.color = new Color(0.1f, 0.1f, 0.2f); }
            MinigameVFX.SpawnPop(this, leftPile.transform);

            // Spawn right group
            var rightPile = Instantiate(cushionPrefab, transform);
            rightPile.transform.position = cannonRight.position;
            var rightLbl  = rightPile.GetComponentInChildren<TextMeshProUGUI>();
            if (rightLbl) { rightLbl.text = _b.ToString(); rightLbl.color = new Color(0.1f, 0.1f, 0.2f); }
            MinigameVFX.SpawnPop(this, rightPile.transform);

            AudioManager.Instance?.PlaySFX("Audio/SFX/cushioncannon/cannon_launch");
            // Move both toward merge point
            yield return StartCoroutine(MovePair(leftPile.transform, rightPile.transform, mergePoint.position));

            Destroy(leftPile);
            Destroy(rightPile);
            AudioManager.Instance?.PlaySFX("Audio/SFX/cushioncannon/cushion_merge");

            // Merged pile falls toward landing
            _flyingPile = Instantiate(cushionPrefab, transform);
            _flyingPile.transform.position = mergePoint.position;
            var mergeLbl = _flyingPile.GetComponentInChildren<TextMeshProUGUI>();
            if (mergeLbl) { mergeLbl.text = "?"; mergeLbl.color = new Color(0.1f, 0.1f, 0.2f); }
            MinigameVFX.SpawnPop(this, _flyingPile.transform);

            _awaitingAnswer = true;
            AudioManager.Instance.PlayVoice($"q_addition_sum_{_a}_{_b}");

            float elapsed = 0f;
            float duration = 3f / _currentSpeed;
            while (elapsed < duration && _awaitingAnswer)
            {
                float t = elapsed / duration;
                _flyingPile.transform.position = Vector3.Lerp(mergePoint.position, landingPoint.position, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_awaitingAnswer)
            {
                // Pile landed — wrong by timeout
                Destroy(_flyingPile);
                if (vacuumAnimator != null) vacuumAnimator.SetTrigger("Dodge");
                AudioManager.Instance.PlayWrong();
                yield return new WaitForSeconds(0.8f);
                StartNextRound();
            }
        }

        private IEnumerator MovePair(Transform a, Transform b, Vector3 target)
        {
            float duration = 1.5f / _currentSpeed;
            float elapsed  = 0f;
            Vector3 startA = a.position, startB = b.position;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                a.position = Vector3.Lerp(startA, target, t);
                b.position = Vector3.Lerp(startB, target, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void OnAnswerTapped(int answer)
        {
            if (!_awaitingAnswer) return;
            _awaitingAnswer = false;

            if (_flyingPile) Destroy(_flyingPile);

            if (answer == _correct)
            {
                AddScore(1);
                AudioManager.Instance.PlayCorrect();
                AudioManager.Instance?.PlaySFX("Audio/SFX/cushioncannon/cushion_land");
                if (vacuumAnimator != null) vacuumAnimator.SetTrigger("Cheer");
                MinigameVFX.PulseRing(this, mergePoint.position, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.FloatingText(this, "+1", mergePoint.position, new Color(0.412f, 0.941f, 0.682f));
                MinigameVFX.ScreenFlash(this, new Color(0.412f, 0.941f, 0.682f));
            }
            else
            {
                AudioManager.Instance.PlayWrong();
                if (vacuumAnimator != null) vacuumAnimator.SetTrigger("Oops");
                MinigameVFX.ScreenFlash(this, new Color(1f, 0.569f, 0f));
            }

            StartCoroutine(DelayedNextRound(0.6f));
        }

        private IEnumerator DelayedNextRound(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextRound();
        }

        private static int[] GenerateChoices(int correct, int min, int max)
        {
            var set = new System.Collections.Generic.HashSet<int> { correct };
            int attempts = 0;
            while (set.Count < 3 && attempts++ < 30)
            {
                int w = correct + (Random.Range(0, 2) == 0 ? 1 : -1) * Random.Range(1, 3);
                if (w >= min && w <= max && w != correct) set.Add(w);
            }
            int pad = min;
            while (set.Count < 3) { if (!set.Contains(pad)) set.Add(pad); pad++; }
            var arr = new System.Collections.Generic.List<int>(set);
            for (int i = arr.Count - 1; i > 0; i--)
            { int j = Random.Range(0, i + 1); (arr[i], arr[j]) = (arr[j], arr[i]); }
            return arr.ToArray();
        }
    }
}
