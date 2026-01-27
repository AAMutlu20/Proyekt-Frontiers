using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(fileName = "AudioClip", menuName = "Audio/Audio Clip Data")]
    public class AudioClipData : ScriptableObject
    {
        [Header("Clip Settings")]
        public AudioClip clip;
        public AudioChannel channel;
        
        [Header("Playback Settings")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 3f)] public float pitch = 1f;
        [Range(0f, 1f)] public float pitchVariation;
        public bool loop;
        
        [Header("3D Settings")]
        public bool is3D;
        [Range(0f, 100f)] public float minDistance = 1f;
        [Range(0f, 500f)] public float maxDistance = 50f;
    }
}