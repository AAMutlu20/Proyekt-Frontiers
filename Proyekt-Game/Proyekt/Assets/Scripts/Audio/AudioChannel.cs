using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(fileName = "AudioChannel", menuName = "Audio/Audio Channel")]
    public class AudioChannel : ScriptableObject
    {
        [SerializeField] private string channelName;
        [Range(0f, 1f)]
        [SerializeField] private float defaultVolume = 1f;
        
        public string ChannelName => channelName;
        public float DefaultVolume => defaultVolume;
    }
}