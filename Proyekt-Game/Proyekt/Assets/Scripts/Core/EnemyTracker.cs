using System.Collections.Generic;
using Generation.TrueGen.Manager;
using Generation.TrueGen.Systems;
using irminNavmeshEnemyAiUnityPackage;
using UnityEngine;

namespace Core
{
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
        private bool RemoveNullEnemies()
        {
            // Remove nulls by filtering
            var originalCount = _trackedEnemies.Count;
            _trackedEnemies.RemoveAll(enemy => !enemy);
            var foundNullEnemies = originalCount - _trackedEnemies.Count;
            return foundNullEnemies > 0;
        }
    }
}
