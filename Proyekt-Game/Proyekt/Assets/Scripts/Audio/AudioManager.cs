using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    /// <summary>
    /// Simple audio manager for music, ambiance, and SFX
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
    
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambianceGroup;
    
        [Header("Audio Sources")]
        private AudioSource _musicSource;
        private AudioSource _ambianceSource;
        private AudioSource _sfxSource;
    
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
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.outputAudioMixerGroup = musicGroup;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        
            _ambianceSource = gameObject.AddComponent<AudioSource>();
            _ambianceSource.outputAudioMixerGroup = ambianceGroup;
            _ambianceSource.loop = true;
            _ambianceSource.playOnAwake = false;
        
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.outputAudioMixerGroup = sfxGroup;
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }
    
        /// <summary>
        /// Play music (loops automatically)
        /// </summary>
        public void PlayMusic(AudioClip clip, float volume = 0.5f)
        {
            if (!clip) return;
        
            _musicSource.clip = clip;
            _musicSource.volume = volume;
            _musicSource.Play();
        }
    
        /// <summary>
        /// Stop music
        /// </summary>
        public void StopMusic()
        {
            _musicSource.Stop();
        }
    
        /// <summary>
        /// Play ambiance (loops automatically)
        /// </summary>
        public void PlayAmbiance(AudioClip clip, float volume = 0.3f)
        {
            if (!clip) return;
        
            _ambianceSource.clip = clip;
            _ambianceSource.volume = volume;
            _ambianceSource.Play();
        }
    
        /// <summary>
        /// Stop ambiance
        /// </summary>
        public void StopAmbiance()
        {
            _ambianceSource.Stop();
        }
    
        /// <summary>
        /// Play one-shot SFX
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (!clip) return;
        
            _sfxSource.PlayOneShot(clip, volume);
        }
    
        /// <summary>
        /// Play 3D positional SFX
        /// </summary>
        public void PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (!clip) return;
        
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }
}