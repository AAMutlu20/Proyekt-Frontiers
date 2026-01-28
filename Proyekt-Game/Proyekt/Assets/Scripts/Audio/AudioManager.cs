using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    /// <summary>
    /// Simple audio manager for music, ambiance, and SFX with fade support
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
    
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambianceGroup;
    
        [Header("Fade Settings")]
        [SerializeField] private float defaultFadeDuration = 2f;
    
        [Header("Audio Sources")]
        private AudioSource musicSource;
        private AudioSource ambianceSource;
        private AudioSource sfxSource;
    
        private Coroutine musicFadeCoroutine;
        private Coroutine ambianceFadeCoroutine;
    
        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        
            // Create audio sources
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0f; // Start at 0 for fade in
        
            ambianceSource = gameObject.AddComponent<AudioSource>();
            ambianceSource.outputAudioMixerGroup = ambianceGroup;
            ambianceSource.loop = true;
            ambianceSource.playOnAwake = false;
            ambianceSource.volume = 0f; // Start at 0 for fade in
        
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.outputAudioMixerGroup = sfxGroup;
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    
        /// <summary>
        /// Play music with fade in (loops automatically)
        /// </summary>
        public void PlayMusic(AudioClip clip, float targetVolume = 0.5f, float fadeDuration = -1f)
        {
            if (!clip) return;
        
            if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
        
            musicSource.clip = clip;
            musicSource.Play();
            musicFadeCoroutine = StartCoroutine(FadeAudioSource(musicSource, targetVolume, fadeDuration));
        }
    
        /// <summary>
        /// Stop music with fade out
        /// </summary>
        public void StopMusic(float fadeDuration = -1f)
        {
            if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
        
            musicFadeCoroutine = StartCoroutine(FadeOutAndStop(musicSource, fadeDuration));
        }
    
        /// <summary>
        /// Play ambiance with fade in (loops automatically)
        /// </summary>
        public void PlayAmbiance(AudioClip clip, float targetVolume = 0.3f, float fadeDuration = -1f)
        {
            if (!clip) return;
        
            if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        
            if (ambianceFadeCoroutine != null)
                StopCoroutine(ambianceFadeCoroutine);
        
            ambianceSource.clip = clip;
            ambianceSource.Play();
            ambianceFadeCoroutine = StartCoroutine(FadeAudioSource(ambianceSource, targetVolume, fadeDuration));
        }
    
        /// <summary>
        /// Stop ambiance with fade out
        /// </summary>
        public void StopAmbiance(float fadeDuration = -1f)
        {
            if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        
            if (ambianceFadeCoroutine != null)
                StopCoroutine(ambianceFadeCoroutine);
        
            ambianceFadeCoroutine = StartCoroutine(FadeOutAndStop(ambianceSource, fadeDuration));
        }
    
        /// <summary>
        /// Play one-shot SFX
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (!clip) return;
        
            sfxSource.PlayOneShot(clip, volume);
        }
    
        /// <summary>
        /// Play 3D positional SFX
        /// </summary>
        public void PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (!clip) return;
        
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    
        /// <summary>
        /// Fade audio source to target volume
        /// </summary>
        private IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
        
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }
        
            source.volume = targetVolume;
        }
    
        /// <summary>
        /// Fade out and stop audio source
        /// </summary>
        private IEnumerator FadeOutAndStop(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
        
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
        
            source.volume = 0f;
            source.Stop();
        }
    }
}