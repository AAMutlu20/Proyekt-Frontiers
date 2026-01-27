using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audio.Bridges
{
    /// <summary>
    /// Add this to UI buttons for automatic audio
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIAudioController : MonoBehaviour, IPointerEnterHandler
    {
        [Header("Audio")]
        [SerializeField] private AudioLibrary audioLibrary;
        [SerializeField] private bool playClickSound = true;
        [SerializeField] private bool playHoverSound = true;
        
        private Button _button;
        
        private void Start()
        {
            _button = GetComponent<Button>();
            
            if (playClickSound)
            {
                _button.onClick.AddListener(OnButtonClick);
            }
        }
        
        private void OnButtonClick()
        {
            if (audioLibrary && audioLibrary.buttonClick)
            {
                AudioManager.Instance?.PlaySound(audioLibrary.buttonClick);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playHoverSound && audioLibrary && audioLibrary.buttonHover)
            {
                AudioManager.Instance?.PlaySound(audioLibrary.buttonHover);
            }
        }
        
        private void OnDestroy()
        {
            if (_button)
            {
                _button.onClick.RemoveListener(OnButtonClick);
            }
        }
    }
}