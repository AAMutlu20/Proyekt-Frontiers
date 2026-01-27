using UnityEngine;
using UnityEngine.UI;

namespace Audio.Bridges
{
    /// <summary>
    /// Add this to volume sliders in settings menu
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class VolumeSlider : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private AudioChannel channel;
        
        private Slider _slider;
        
        private void Start()
        {
            _slider = GetComponent<Slider>();
            
            // Load current volume
            if (AudioManager.Instance && channel)
            {
                _slider.value = AudioManager.Instance.GetChannelVolume(channel);
            }
            
            // Listen for changes
            _slider.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        private void OnVolumeChanged(float value)
        {
            if (AudioManager.Instance && channel)
            {
                AudioManager.Instance.SetChannelVolume(channel, value);
            }
        }
        
        private void OnDestroy()
        {
            if (_slider)
            {
                _slider.onValueChanged.RemoveListener(OnVolumeChanged);
            }
        }
    }
}