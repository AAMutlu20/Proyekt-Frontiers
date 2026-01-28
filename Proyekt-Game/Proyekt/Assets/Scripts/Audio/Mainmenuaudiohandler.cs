using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Example Main Menu Audio Handler
    /// Add this to your main menu scene
    /// </summary>
    public class MainMenuAudioHandler : MonoBehaviour
    {
        [SerializeField] private GameAudioClips audioClips;
        [SerializeField] private float fadeDuration = 2f;
    
        private void Start()
        {
            StartMenuMusic();
        }
    
        /// <summary>
        /// Start menu music and ambiance with fade in
        /// </summary>
        public void StartMenuMusic()
        {
            if (!audioClips) return;
        
            if (audioClips.menuMusic)
            {
                AudioManager.Instance?.PlayMusic(audioClips.menuMusic, 0.5f, fadeDuration);
            }
        
            if (audioClips.ambianceLoop)
            {
                AudioManager.Instance?.PlayAmbiance(audioClips.ambianceLoop, 0.3f, fadeDuration);
            }
        }
    
        /// <summary>
        /// Call this when transitioning from menu to game
        /// </summary>
        public void TransitionToGame()
        {
            // Fade out menu audio before loading game
            AudioManager.Instance?.StopMusic(fadeDuration);
            AudioManager.Instance?.StopAmbiance(fadeDuration);
        
            // Game will start its own music via WaveManager
        }
    
        /// <summary>
        /// Play button click sound
        /// </summary>
        public void OnButtonClick()
        {
            if (audioClips && audioClips.buttonClickSound)
            {
                AudioManager.Instance?.PlaySFX(audioClips.buttonClickSound);
            }
        }
    }
}

// HOW TO USE:
// 1. Create empty GameObject in Main Menu scene called "MenuAudio"
// 2. Add this script to it
// 3. Drag GameAudioClips asset into "Audio Clips" field
// 4. On "Play" button, add OnClick event → MenuAudio.TransitionToGame()
// 5. On all buttons, add OnClick event → MenuAudio.OnButtonClick()