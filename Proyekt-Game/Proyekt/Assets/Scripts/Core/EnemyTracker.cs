using Generation.TrueGen.Manager;
using Generation.TrueGen.Systems;
using irminNavmeshEnemyAiUnityPackage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTracker : MonoBehaviour
{
    [SerializeField] Economy _economy;
    [SerializeField] TerrainManager _terrainManager;
    [SerializeField] IrminBaseHealthSystem _healthSystem;

    [SerializeField] List<EnemyPathFollower> _trackedEnemies = new();

    private void Start()
    {
        _terrainManager.OnEnemySpawned.AddListener(TrackEnemy);
    }

    private void TrackEnemy(EnemyPathFollower enemy)
    {
        _trackedEnemies.Add(enemy);
        enemy.OnPathCompleteEvent.AddListener(EnemyCompletedPath);
        enemy.OnEnemyDestroyed.AddListener(EnemyDestroyed);
    }

    private void EnemyDestroyed(int coinsToGain)
    {
        _economy.AwardCoins(coinsToGain);
    }

    private void EnemyCompletedPath(EnemyPathFollower enemy)
    {
        Debug.Log($"enemy {enemy.name} reached end of path. Doing damange {enemy.DamageAtEndOfPath}");
        _healthSystem.Damage(enemy.DamageAtEndOfPath, null);
    }
}
