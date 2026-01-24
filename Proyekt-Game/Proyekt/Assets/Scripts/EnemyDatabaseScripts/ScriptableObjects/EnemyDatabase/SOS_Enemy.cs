using UnityEngine;

[CreateAssetMenu(fileName = "SOS_Enemy", menuName = "Scriptable Objects/SOS_Enemy")]
public class SOS_Enemy : ScriptableObject
{
    [SerializeField] GameObject _enemyPrefab;

    [SerializeField] private float _enemySpeed = 6f;
    [SerializeField] private float _enemyHealth = 3f;
    [SerializeField] private float _damageToPlayer = 1f;
    [SerializeField] private int _coinsReward = 15;

    public float EnemySpeed { get { return _enemySpeed; } }
    public float EnemyHealth { get { return _enemyHealth; } }
    public float DamageToPlayer {  get { return _damageToPlayer; } }
    public int CoinsReward { get { return _coinsReward; } }

    public GameObject EnemyPrefab { get { return _enemyPrefab; } }
}
