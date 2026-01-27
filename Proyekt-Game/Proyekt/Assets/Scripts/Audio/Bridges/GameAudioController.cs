using UnityEngine;

namespace Audio.Bridges
{
    /// <summary>
    /// Handles game-wide audio like music transitions and ambiance
    /// Add this to GameManager or the like
    /// </summary>
    public class GameAudioController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioLibrary audioLibrary;
        
        [Header("Settings")]
        [SerializeField] private bool playMenuMusicOnStart;
        [SerializeField] private bool playAmbianceInGame = true;
        
        private AudioSource _currentMusic;
        private AudioSource _currentAmbiance;
        
        private void Start()
        {
            if (playMenuMusicOnStart && audioLibrary && audioLibrary.menuMusic)
            {
                PlayMenuMusic();
            }
        }
        
        /// <summary>
        /// Call when entering main menu
        /// </summary>
        public void PlayMenuMusic()
        {
            StopCurrentMusic();
            
            if (audioLibrary && audioLibrary.menuMusic)
            {
                _currentMusic = AudioManager.Instance?.PlayMusic(audioLibrary.menuMusic);
            }
        }
        
        /// <summary>
        /// Call when starting gameplay
        /// </summary>
        public void PlayGameMusic()
        {
            StopCurrentMusic();
            
            if (audioLibrary && audioLibrary.gameMusic)
            {
                _currentMusic = AudioManager.Instance?.PlayMusic(audioLibrary.gameMusic);
            }
            
            if (playAmbianceInGame && audioLibrary && audioLibrary.levelAmbiance)
            {
                _currentAmbiance = AudioManager.Instance?.PlayAmbiance(audioLibrary.levelAmbiance);
            }
        }
        
        /// <summary>
        /// Call on game over (lose)
        /// </summary>
        public void OnGameLose()
        {
            StopCurrentMusic();
            
            if (audioLibrary && audioLibrary.gameLose)
            {
                AudioManager.Instance?.PlaySound(audioLibrary.gameLose);
            }
        }
        
        /// <summary>
        /// Call on game victory
        /// </summary>
        public void OnGameWin()
        {
            StopCurrentMusic();
            
            if (audioLibrary && audioLibrary.gameWin)
            {
                AudioManager.Instance?.PlaySound(audioLibrary.gameWin);
            }
        }
        
        private void StopCurrentMusic()
        {
            if (_currentMusic)
            {
                AudioManager.Instance?.StopSound(_currentMusic);
                _currentMusic = null;
            }

            if (!_currentAmbiance) return;
            AudioManager.Instance?.StopSound(_currentAmbiance);
            _currentAmbiance = null;
        }
    }
}