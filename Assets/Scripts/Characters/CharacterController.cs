using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VacuumVille.Core;
using VacuumVille.Data;

namespace VacuumVille.Characters
{
    /// <summary>
    /// Attached to the vacuum character GameObject in each scene.
    /// Drives animations and plays catchphrase on cheer/oops triggers.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private CharacterType characterType;
        [SerializeField] private Image bodyImage;
        [SerializeField] private TextMeshProUGUI speechBubble;
        [SerializeField] private float speechDuration = 2f;

        private Animator _anim;
        private CharacterDefinition _def;
        private Coroutine _speechCoroutine;

        private void Start()
        {
            _anim = GetComponent<Animator>();
            _def  = GameManager.Instance?.GetCharacter(characterType);

            if (_def?.thumbnailSprite != null && bodyImage != null)
                bodyImage.sprite = _def.thumbnailSprite;

            if (_def?.animatorController != null)
                _anim.runtimeAnimatorController = _def.animatorController;

            if (speechBubble) speechBubble.gameObject.SetActive(false);
        }

        public void TriggerCheer()
        {
            _anim.SetTrigger("Cheer");
            ShowSpeech(LocalizationManager.Instance?.Get(_def?.catchphraseKey ?? ""));
        }

        public void TriggerOops()
        {
            _anim.SetTrigger("Oops");
        }

        public void TriggerIdle()
        {
            _anim.SetTrigger("Idle");
        }

        private void ShowSpeech(string text)
        {
            if (string.IsNullOrEmpty(text) || speechBubble == null) return;
            if (_speechCoroutine != null) StopCoroutine(_speechCoroutine);
            _speechCoroutine = StartCoroutine(ShowSpeechRoutine(text));
        }

        private System.Collections.IEnumerator ShowSpeechRoutine(string text)
        {
            speechBubble.text = text;
            speechBubble.gameObject.SetActive(true);
            yield return new WaitForSeconds(speechDuration);
            speechBubble.gameObject.SetActive(false);
        }
    }
}
