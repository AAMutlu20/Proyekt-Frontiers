using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOS_Enemy", menuName = "Scriptable Objects/SOS_Enemy")]
public class SOS_Enemy : ScriptableObject
{
    [SerializeField] GameObject _enemyPrefab;

    [SerializeField] private float _enemySpeed = 6f;
    [SerializeField] private float _enemyHealth = 3f;
    [SerializeField] private float _damageToPlayer = 1f;
    [SerializeField] private int _coinsReward = 15;
    [SerializeField] private int _layer = 20;

    //[SerializeField] private List<AudioClip> _deathSounds = new();
    [SerializeField] private GameAudioClips _audioClips;

    public float EnemySpeed { get { return _enemySpeed; } }
    public float EnemyHealth { get { return _enemyHealth; } }
    public float DamageToPlayer { get { return _damageToPlayer; } }
    public int CoinsReward { get { return _coinsReward; } }
    public GameAudioClips AudioClips { get { return _audioClips; } }

    public GameObject EnemyPrefab { get { return _enemyPrefab; } }
    //public List<AudioClip> DeathSounds { get { List<AudioClip> deathSoundsCopy = new(_deathSounds); return deathSoundsCopy; } }

    ///// <summary>
    ///// Returns a random death sound from the list
    ///// </summary>
    ///// <returns>A random deathsound from the list. This can be null if entries in the list are null (for no death sound possibility)</returns>
    //public AudioClip GetRandomDeathsoundFromList()
    //{
    //    if(_deathSounds.Count <= 0) return null;
    //    int randomNumber = UnityEngine.Random.Range(0, _deathSounds.Count);
    //    return _deathSounds[randomNumber];
    //}
}
