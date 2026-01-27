using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Generation.TrueGen.Core
{
    [System.Serializable]
    public class WaveDefinition
    {
        [Header("Wave Info")]
        public int waveNumber = 1;
        public int enemyCount = 3;
        public float spawnInterval = 0.5f; // Time between each enemy spawn
        public float delayBeforeWave = 3f; // Delay before this wave starts
        
        [Header("Enemy Settings")]
        public GameObject enemyPrefab;
        public bool _useSpecificEnemies = false;
        public List<SOS_Enemy> _specificEnemies = new();
        public float enemySpeed = 5f;
        public float enemyHealth = 2f;
        public float damageToPlayer = 1f;
        public int coinsReward = 10;
        
        [Header("Wave Rewards")]
        public int goldReward = 100;
    }
}