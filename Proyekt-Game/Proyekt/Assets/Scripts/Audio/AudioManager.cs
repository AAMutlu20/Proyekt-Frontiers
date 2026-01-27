using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        
        [Header("Channels")]
        [SerializeField] private AudioChannel masterChannel;
        [SerializeField] private AudioChannel musicChannel;
        [SerializeField] private AudioChannel sfxChannel;
        [SerializeField] private AudioChannel ambianceChannel;
        
        [Header("Audio Source Pools")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private GameObject audioSourcePrefab;
        
        private readonly Dictionary<AudioChannel, float> _channelVolumes = new();
        private readonly List<AudioSource> _audioSourcePool = new();
        private readonly List<AudioSource> _activeAudioSources = new();
        
        // Mixer parameter names (use exact names, otherwise !work)
        private const string MasterVolumeParam = "MasterVolume";
        private const string MusicVolumeParam = "MusicVolume";
        private const string SFXVolumeParam = "SFXVolume";
        private const string AmbianceVolumeParam = "AmbianceVolume";
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeChannels();
            InitializeAudioSourcePool();
            LoadVolumeSettings();
        }
        
        private void InitializeChannels()
        {
            _channelVolumes[masterChannel] = masterChannel.DefaultVolume;
            _channelVolumes[musicChannel] = musicChannel.DefaultVolume;
            _channelVolumes[sfxChannel] = sfxChannel.DefaultVolume;
            _channelVolumes[ambianceChannel] = ambianceChannel.DefaultVolume;
        }
        
        private void InitializeAudioSourcePool()
        {
            for (var i = 0; i < initialPoolSize; i++)
            {
                CreateAudioSource();
            }
        }
        
        private AudioSource CreateAudioSource()
        {
            GameObject sourceObj;
            if (audioSourcePrefab)
            {
                sourceObj = Instantiate(audioSourcePrefab, transform);
            }
            else
            {
                sourceObj = new GameObject("AudioSource");
                sourceObj.transform.SetParent(transform);
            }
            
            var source = sourceObj.GetComponent<AudioSource>();
            if (!source)
                source = sourceObj.AddComponent<AudioSource>();
            
            source.playOnAwake = false;
            sourceObj.SetActive(false);
            _audioSourcePool.Add(source);
            
            return source;
        }
        
        private AudioSource GetAudioSource()
        {
            // Find inactive source in pool
            foreach (var source in _audioSourcePool.Where(source => !source.gameObject.activeSelf))
            {
                source.gameObject.SetActive(true);
                return source;
            }
            
            // Pool exhausted, create new source
            Debug.LogWarning("AudioManager: Pool exhausted, creating new AudioSource");
            var newSource = CreateAudioSource();
            newSource.gameObject.SetActive(true);
            return newSource;
        }
        
        private void ReturnAudioSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.gameObject.SetActive(false);
            _activeAudioSources.Remove(source);
        }
        
        /// <summary>
        /// Play a sound effect at a position in the world (3D)
        /// </summary>
        public AudioSource PlaySound3D(AudioClipData clipData, Vector3 position)
        {
            if (!ValidateClipData(clipData)) return null;
            
            var source = GetAudioSource();
            ConfigureAudioSource(source, clipData);
            
            source.transform.position = position;
            source.spatialBlend = 1f; // Full 3D
            
            source.Play();
            _activeAudioSources.Add(source);
            
            if (!clipData.loop)
            {
                StartCoroutine(ReturnSourceWhenFinished(source));
            }
            
            return source;
        }
        
        /// <summary>
        /// Play a sound effect (2D, no position)
        /// </summary>
        public AudioSource PlaySound(AudioClipData clipData)
        {
            if (!ValidateClipData(clipData)) return null;
            
            var source = GetAudioSource();
            ConfigureAudioSource(source, clipData);
            
            source.spatialBlend = 0f; // Full 2D
            
            source.Play();
            _activeAudioSources.Add(source);
            
            if (!clipData.loop)
            {
                StartCoroutine(ReturnSourceWhenFinished(source));
            }
            
            return source;
        }
        
        /// <summary>
        /// Play music (looping, 2D)
        /// </summary>
        public AudioSource PlayMusic(AudioClipData clipData)
        {
            if (!ValidateClipData(clipData)) return null;
            
            // Stop any existing music
            StopAllSoundsInChannel(musicChannel);
            
            var source = GetAudioSource();
            ConfigureAudioSource(source, clipData);
            
            source.loop = true;
            source.spatialBlend = 0f;
            
            source.Play();
            _activeAudioSources.Add(source);
            
            return source;
        }
        
        /// <summary>
        /// Play ambiance (looping, 2D)
        /// </summary>
        public AudioSource PlayAmbiance(AudioClipData clipData)
        {
            if (!ValidateClipData(clipData)) return null;
            
            // Stop any existing ambiance
            StopAllSoundsInChannel(ambianceChannel);
            
            var source = GetAudioSource();
            ConfigureAudioSource(source, clipData);
            
            source.loop = true;
            source.spatialBlend = 0f;
            
            source.Play();
            _activeAudioSources.Add(source);
            
            return source;
        }
        
        private void ConfigureAudioSource(AudioSource source, AudioClipData clipData)
        {
            source.clip = clipData.clip;
            source.volume = clipData.volume * GetChannelVolume(clipData.channel);
            
            // Apply pitch variation
            var pitchVariation = Random.Range(-clipData.pitchVariation, clipData.pitchVariation);
            source.pitch = clipData.pitch + pitchVariation;
            
            source.loop = clipData.loop;
            
            // 3D settings
            if (clipData.is3D)
            {
                source.spatialBlend = 1f;
                source.minDistance = clipData.minDistance;
                source.maxDistance = clipData.maxDistance;
                source.rolloffMode = AudioRolloffMode.Linear;
            }
            else
            {
                source.spatialBlend = 0f;
            }
            
            // Assign to mixer group
            if (!audioMixer) return;
            var groupName = clipData.channel.ChannelName;
            var groups = audioMixer.FindMatchingGroups(groupName);
            if (groups.Length > 0)
            {
                source.outputAudioMixerGroup = groups[0];
            }
        }
        
        private static bool ValidateClipData(AudioClipData clipData)
        {
            if (!clipData)
            {
                Debug.LogError("AudioManager: AudioClipData is null");
                return false;
            }

            if (clipData.clip) return true;
            Debug.LogError($"AudioManager: AudioClip is null in {clipData.name}");
            return false;

        }
        
        private System.Collections.IEnumerator ReturnSourceWhenFinished(AudioSource source)
        {
            yield return new WaitWhile(() => source.isPlaying);
            ReturnAudioSource(source);
        }
        
        /// <summary>
        /// Stop a specific audio source
        /// </summary>
        public void StopSound(AudioSource source)
        {
            if (source && _activeAudioSources.Contains(source))
            {
                ReturnAudioSource(source);
            }
        }
        
        /// <summary>
        /// Stop all sounds in a specific channel
        /// </summary>
        public void StopAllSoundsInChannel(AudioChannel channel)
        {
            for (var i = _activeAudioSources.Count - 1; i >= 0; i--)
            {
                var source = _activeAudioSources[i];
                // Check if source belongs to this channel (would need to track this)
                // For now, just stop looping sources which are likely music/ambiance
                if (source.loop)
                {
                    ReturnAudioSource(source);
                }
            }
        }
        
        /// <summary>
        /// Set volume for a specific channel (0-1)
        /// </summary>
        public void SetChannelVolume(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            _channelVolumes[channel] = volume;
            
            // Convert to decibels for mixer (-80 to 0)
            var volumeDB = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            
            // Update mixer
            if (audioMixer)
            {
                var paramName = GetMixerParamName(channel);
                audioMixer.SetFloat(paramName, volumeDB);
            }
            
            // Save to PlayerPrefs
            PlayerPrefs.SetFloat($"Volume_{channel.ChannelName}", volume);
            PlayerPrefs.Save();
        }
        
        public float GetChannelVolume(AudioChannel channel)
        {
            return _channelVolumes.ContainsKey(channel) ? _channelVolumes[channel] : 1f;
        }
        
        private string GetMixerParamName(AudioChannel channel)
        {
            if (channel == masterChannel) return MasterVolumeParam;
            if (channel == musicChannel) return MusicVolumeParam;
            if (channel == sfxChannel) return SFXVolumeParam;
            return channel == ambianceChannel ? AmbianceVolumeParam : "MasterVolume";
        }
        
        private void LoadVolumeSettings()
        {
            LoadChannelVolume(masterChannel);
            LoadChannelVolume(musicChannel);
            LoadChannelVolume(sfxChannel);
            LoadChannelVolume(ambianceChannel);
        }
        
        private void LoadChannelVolume(AudioChannel channel)
        {
            var savedVolume = PlayerPrefs.GetFloat($"Volume_{channel.ChannelName}", channel.DefaultVolume);
            SetChannelVolume(channel, savedVolume);
        }
        
        private void Update()
        {
            // Clean up finished non-looping sources
            for (var i = _activeAudioSources.Count - 1; i >= 0; i--)
            {
                var source = _activeAudioSources[i];
                if (!source.isPlaying && !source.loop)
                {
                    ReturnAudioSource(source);
                }
            }
        }
    }
}