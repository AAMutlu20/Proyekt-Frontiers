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
    [SerializeField] private bool _awardCoinsOnDefeat = false;

    [SerializeField] List<EnemyPathFollower> _trackedEnemies = new();

    private void Start()
    {
        _terrainManager.onEnemySpawned.AddListener(TrackEnemy);
    }

    private void TrackEnemy(EnemyPathFollower enemy)
    {
        _trackedEnemies.Add(enemy);
        enemy.onPathCompleteEvent.AddListener(EnemyCompletedPath);
        enemy.onEnemyDestroyed.AddListener(EnemyDestroyed);
    }

    private void EnemyDestroyed(int coinsToGain)
    {
        // Remove null enemies might be called before the enemy is truly destroyed
        if (_awardCoinsOnDefeat) { _economy.AwardCoins(coinsToGain); RemoveNullEnemies(); }
    }

    private void EnemyCompletedPath(EnemyPathFollower enemy)
    {
        Debug.Log($"enemy {enemy.name} reached end of path. Doing damange {enemy.DamageAtEndOfPath}");
        _healthSystem.Damage(enemy.DamageAtEndOfPath, null);
    }

    /// <summary>
    /// Removes null enemies from the tracked enemies list.
    /// </summary>
    /// <returns>If null enemies have been found and removed.</returns>
    private bool RemoveNullEnemies()
    {
        List<EnemyPathFollower> trackedEnemiesCopy = new List<EnemyPathFollower>(_trackedEnemies);
        int foundNullEnemies = 0;
        for (int i = 0; i < trackedEnemiesCopy.Count; i++)
        {
            if (_trackedEnemies[i] == null) { _trackedEnemies.RemoveAt(i); foundNullEnemies++; }
        }
        if (foundNullEnemies > 0) { return true; }
        else { return false; }
    }
}
