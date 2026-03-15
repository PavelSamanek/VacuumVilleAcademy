using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VacuumVille.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Defaults")]
        [SerializeField] private AudioClip correctSound;
        [SerializeField] private AudioClip wrongSound;
        [SerializeField] private AudioClip levelCompleteSound;
        [SerializeField] private AudioClip minigameUnlockSound;
        [SerializeField] private AudioClip coinSound;
        [SerializeField] private AudioClip buttonClickSound;

        // Volume settings (0-1), persisted via PlayerPrefs
        public float MusicVolume
        {
            get => musicSource.volume;
            set { musicSource.volume = value; PlayerPrefs.SetFloat("vol_music", value); }
        }

        public float SfxVolume
        {
            get => sfxSource.volume;
            set { sfxSource.volume = value; PlayerPrefs.SetFloat("vol_sfx", value); }
        }

        public float VoiceVolume
        {
            get => voiceSource.volume;
            set { voiceSource.volume = value; PlayerPrefs.SetFloat("vol_voice", value); }
        }

        private Coroutine _musicFadeCoroutine;
        private Dictionary<string, AudioClip> _voiceCache = new();
        private Dictionary<string, AudioClip> _sfxCache = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSources();
            LoadSfxClips();

            MusicVolume = PlayerPrefs.GetFloat("vol_music", 0.7f);
            SfxVolume   = PlayerPrefs.GetFloat("vol_sfx",   1.0f);
            VoiceVolume = PlayerPrefs.GetFloat("vol_voice",  1.0f);
        }

        private void LoadSfxClips()
        {
            if (correctSound       == null) correctSound       = Resources.Load<AudioClip>("Audio/SFX/correct");
            if (wrongSound         == null) wrongSound         = Resources.Load<AudioClip>("Audio/SFX/wrong");
            if (levelCompleteSound == null) levelCompleteSound = Resources.Load<AudioClip>("Audio/SFX/level_complete");
            if (minigameUnlockSound== null) minigameUnlockSound= Resources.Load<AudioClip>("Audio/SFX/minigame_unlock");
            if (coinSound          == null) coinSound          = Resources.Load<AudioClip>("Audio/SFX/coin");
            if (buttonClickSound   == null) buttonClickSound   = Resources.Load<AudioClip>("Audio/SFX/button_click");
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
            }
        }

        // ── Music ───────────────────────────────────────────────────────────────

        public void PlayMusic(AudioClip clip, float fadeDuration = 0.5f)
        {
            if (clip == null) return;
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeDuration));
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
        {
            float targetVolume = MusicVolume;
            // Fade out
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(targetVolume, 0, t / duration);
                yield return null;
            }
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.loop = true;
            musicSource.Play();
            // Fade in
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0, targetVolume, t / duration);
                yield return null;
            }
            musicSource.volume = targetVolume;
        }

        // ── SFX ────────────────────────────────────────────────────────────────

        public void PlayCorrect()         { if (correctSound        != null) sfxSource.PlayOneShot(correctSound); }
        public void PlayWrong()           { if (wrongSound          != null) sfxSource.PlayOneShot(wrongSound); }
        public void PlayCoin()            { if (coinSound           != null) sfxSource.PlayOneShot(coinSound); }
        public void PlayButton()          { if (buttonClickSound    != null) sfxSource.PlayOneShot(buttonClickSound); }
        public void PlayLevelComplete()   { if (levelCompleteSound  != null) sfxSource.PlayOneShot(levelCompleteSound); }
        public void PlayMinigameUnlock()  { if (minigameUnlockSound != null) sfxSource.PlayOneShot(minigameUnlockSound); }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null) sfxSource.PlayOneShot(clip);
        }

        public void PlaySFX(string resourcePath)
        {
            // Always re-check: Unity may unload the native clip between scenes
            // even though the C# wrapper stays in the cache (UnloadUnusedAssets).
            if (!_sfxCache.TryGetValue(resourcePath, out AudioClip clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>(resourcePath);
                if (clip != null)
                    _sfxCache[resourcePath] = clip;
                else
                {
                    Debug.LogWarning($"[AudioManager] SFX not found: {resourcePath}");
                    return;
                }
            }
            sfxSource.PlayOneShot(clip);
        }

        // ── Voice ───────────────────────────────────────────────────────────────

        public void PlayVoice(string voiceLineKey)
        {
            string locale = LocalizationManager.Instance?.CurrentLanguage == Data.Language.Czech
                ? "cs-CZ" : "en-US";
            string path = $"Audio/Voice/{locale}/{voiceLineKey}";

            if (!_voiceCache.TryGetValue(path, out AudioClip clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>(path);
                if (clip != null)
                    _voiceCache[path] = clip;
                else
                {
                    Debug.LogWarning($"[AudioManager] Voice clip not found: {path}");
                    return;
                }
            }

            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }

        public void StopVoice() => voiceSource.Stop();

        public bool IsVoicePlaying => voiceSource.isPlaying;

        // ── Mute All ────────────────────────────────────────────────────────────

        public void SetMuteAll(bool mute)
        {
            AudioListener.pause = mute;
        }
    }
}
