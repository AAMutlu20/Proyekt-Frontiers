using UnityEngine;

/// <summary>
/// Container for all game audio clips
/// Create by: Right-click → Create → Game Audio Clips
/// </summary>
[CreateAssetMenu(fileName = "GameAudioClips", menuName = "Game Audio Clips")]
public class GameAudioClips : ScriptableObject
{
    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;
    
    [Header("Ambiance")]
    public AudioClip ambianceLoop;
    
    [Header("Game Events")]
    public AudioClip waveSpawnSound;
    public AudioClip victorySound;
    public AudioClip defeatSound;
    
    [Header("UI")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    
    [Header("Tower")]
    public AudioClip towerPlaceSound;
    public AudioClip towerShootSound;
    
    [Header("Enemy")]
    public AudioClip enemyHitSound;
    public AudioClip enemyDeathSound;
}