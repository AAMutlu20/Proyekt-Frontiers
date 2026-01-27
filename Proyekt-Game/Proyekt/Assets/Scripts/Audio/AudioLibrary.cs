using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Music")]
        public AudioClipData menuMusic;
        public AudioClipData gameMusic;
        
        [Header("Ambiance")]
        public AudioClipData levelAmbiance;
        
        [Header("SFX - Enemies")]
        public AudioClipData enemyHit;
        public AudioClipData enemyDeath;
        public AudioClipData[] enemyWalkSounds; // Can have multiple variations
        
        [Header("SFX - Towers")]
        public AudioClipData towerShoot;
        public AudioClipData towerPlace;
        public AudioClipData towerUpgrade;
        
        [Header("SFX - UI")]
        public AudioClipData buttonClick;
        public AudioClipData buttonHover;
        public AudioClipData invalidAction;
        
        [Header("SFX - Game Events")]
        public AudioClipData waveStart;
        public AudioClipData waveComplete;
        public AudioClipData gameWin;
        public AudioClipData gameLose;
    }
}