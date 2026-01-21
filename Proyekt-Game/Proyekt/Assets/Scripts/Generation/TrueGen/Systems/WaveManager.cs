using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generation.TrueGen.Core;
using irminNavmeshEnemyAiUnityPackage;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Generation.TrueGen.Systems
{
    public class WaveManager : MonoBehaviour
    {
        [Header("Wave Configuration")]
        [SerializeField] private WaveDefinition[] waves;
        [SerializeField] private bool autoStartWaves = true;
        [SerializeField] private float timeBetweenWaves = 5f;
        
        [Header("References")]
        [SerializeField] private GameObject defaultEnemyPrefab;
        [SerializeField] private int enemyLayer = 20;
        
        [Header("Economy")]
        [SerializeField] private Economy economyRef;
        
        private ChunkGrid _chunkGrid;
        private List<EnemyPathFollower> _activeEnemies = new();
        private int _currentWaveIndex = 0;
        private bool _waveInProgress = false;
        private int _totalEnemiesSpawned = 0;
        
        public int CurrentWave => _currentWaveIndex + 1;
        public int TotalWaves => waves.Length;
        public bool IsWaveActive => _waveInProgress;
        public int ActiveEnemyCount => _activeEnemies.Count;
        
        // Events
        public UnityEvent<int> OnWaveStarted = new();
        public UnityEvent<int> OnWaveCompleted = new();
        public UnityEvent<EnemyPathFollower> OnEnemySpawned = new();
        public UnityEvent OnAllWavesCompleted = new();
        
        public void Initialize(ChunkGrid chunkGrid, Economy economy, GameObject enemyPrefab, int layer)
        {
            _chunkGrid = chunkGrid;
            economyRef = economy;
            defaultEnemyPrefab = enemyPrefab;
            enemyLayer = layer;
            
            if (_chunkGrid == null)
            {
                Debug.LogError("WaveManager: ChunkGrid is required!");
                enabled = false;
                return;
            }
            
            if (defaultEnemyPrefab == null)
            {
                Debug.LogError("WaveManager: No enemy prefab assigned!");
                enabled = false;
                return;
            }
            
            if (economyRef == null)
            {
                Debug.LogWarning("WaveManager: No economy assigned! Rewards won't work.");
            }
            
            // Create default waves if none defined
            if (waves == null || waves.Length == 0)
            {
                CreateDefaultWaves();
            }
            
            if (autoStartWaves)
            {
                StartCoroutine(WaveSequence());
            }
        }
        
        private void Update()
        {
            // DEBUG - Press K to see active enemy count
            if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            {
                Debug.Log($"=== Wave Debug ===");
                Debug.Log($"Current Wave: {_currentWaveIndex + 1}/{waves.Length}");
                Debug.Log($"Active Enemies: {_activeEnemies.Count}");
                Debug.Log($"Wave In Progress: {_waveInProgress}");
                
                // Check for null enemies
                int nullCount = _activeEnemies.Count(e => e == null);
                if (nullCount > 0)
                {
                    Debug.LogWarning($"Found {nullCount} null enemies in list! Cleaning up...");
                    _activeEnemies.RemoveAll(e => e == null);
                    Debug.Log($"After cleanup: {_activeEnemies.Count} active enemies");
                }
            }
        }
        
        private void CreateDefaultWaves()
        {
            waves = new WaveDefinition[3];
            
            // Wave 1: 3 enemies
            waves[0] = new WaveDefinition
            {
                waveNumber = 1,
                enemyCount = 3,
                spawnInterval = 1f,
                delayBeforeWave = 2f,
                enemyPrefab = defaultEnemyPrefab,
                enemySpeed = 5f,
                enemyHealth = 2f,
                damageToPlayer = 1f,
                coinsReward = 10,
                goldReward = 100
            };
            
            // Wave 2: 5 enemies
            waves[1] = new WaveDefinition
            {
                waveNumber = 2,
                enemyCount = 5,
                spawnInterval = 0.8f,
                delayBeforeWave = 3f,
                enemyPrefab = defaultEnemyPrefab,
                enemySpeed = 6f,
                enemyHealth = 3f,
                damageToPlayer = 1f,
                coinsReward = 15,
                goldReward = 150
            };
            
            // Wave 3: 10 enemies
            waves[2] = new WaveDefinition
            {
                waveNumber = 3,
                enemyCount = 10,
                spawnInterval = 0.5f,
                delayBeforeWave = 3f,
                enemyPrefab = defaultEnemyPrefab,
                enemySpeed = 7f,
                enemyHealth = 5f,
                damageToPlayer = 2f,
                coinsReward = 20,
                goldReward = 200
            };
        }
        
        private IEnumerator WaveSequence()
        {
            Debug.Log("=== Wave System Started ===");
            
            while (_currentWaveIndex < waves.Length)
            {
                var wave = waves[_currentWaveIndex];
                
                // Wait before wave
                Debug.Log($"Wave {wave.waveNumber} starting in {wave.delayBeforeWave} seconds...");
                yield return new WaitForSeconds(wave.delayBeforeWave);
                
                // Notify wave started
                OnWaveStarted?.Invoke(wave.waveNumber);
                
                // Spawn wave
                yield return StartCoroutine(SpawnWave(wave));
                
                // Wait for all enemies to be defeated
                Debug.Log($"Wave {wave.waveNumber} spawned! Waiting for enemies to be defeated...");
                yield return new WaitUntil(() => _activeEnemies.Count == 0);
                
                // Wave completed - give gold reward
                if (economyRef)
                {
                    economyRef.AwardCoins(wave.goldReward);
                    Debug.Log($"✓ Wave {wave.waveNumber} completed! Reward: {wave.goldReward} gold");
                }
                
                OnWaveCompleted?.Invoke(wave.waveNumber);
                
                _currentWaveIndex++;
                
                // Wait between waves
                if (_currentWaveIndex < waves.Length)
                {
                    yield return new WaitForSeconds(timeBetweenWaves);
                }
            }
            
            Debug.Log("=== All Waves Completed! Victory! ===");
            OnAllWavesCompleted?.Invoke();
        }
        
        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            _waveInProgress = true;
            
            Debug.Log($">>> Spawning Wave {wave.waveNumber}: {wave.enemyCount} enemies");
            
            for (int i = 0; i < wave.enemyCount; i++)
            {
                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnInterval);
            }
            
            _waveInProgress = false;
        }
        
        private void SpawnEnemy(WaveDefinition wave)
        {
            if (_chunkGrid == null || _chunkGrid.PathChunks == null || _chunkGrid.PathChunks.Count == 0)
            {
                Debug.LogError("Cannot spawn enemy: No path available!");
                return;
            }
            
            // Use wave's prefab, or fall back to default
            var prefab = wave.enemyPrefab != null ? wave.enemyPrefab : defaultEnemyPrefab;
            
            if (prefab == null)
            {
                Debug.LogError("No enemy prefab assigned!");
                return;
            }
            
            // Spawn at first path chunk
            var spawnChunk = _chunkGrid.PathChunks[0];
            var spawnPos = spawnChunk.center;
            spawnPos.y = spawnChunk.yOffset;
            
            var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemy.name = $"Enemy_Wave{wave.waveNumber}_{_totalEnemiesSpawned + 1}";
            
            // Setup path follower
            var follower = enemy.GetComponent<EnemyPathFollower>();
            if (follower == null)
            {
                follower = enemy.AddComponent<EnemyPathFollower>();
            }
            
            // Setup health system
            var healthSystem = enemy.GetComponent<IrminBaseHealthSystem>();
            if (healthSystem == null)
            {
                healthSystem = enemy.AddComponent<IrminBaseHealthSystem>();
            }
            healthSystem.DestroyAtMinHealth = true;
            
            // Setup faction
            var factionComponent = enemy.GetComponent<FactionMemberComponent>();
            if (factionComponent == null)
            {
                factionComponent = enemy.AddComponent<FactionMemberComponent>();
            }
            healthSystem.FactionMemberComponent = factionComponent;
            healthSystem.Faction = 1;
            
            // Setup rigidbody
            var rb = enemy.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = enemy.AddComponent<Rigidbody>();
            }
            rb.useGravity = false;
            rb.isKinematic = true;
            
            // Initialize with wave settings
            healthSystem.ReAwaken(wave.enemyHealth);
            follower.Initialize(_chunkGrid.PathChunks);
            follower.SetSpeed(wave.enemySpeed);
            follower.SetDamage(wave.damageToPlayer);
            follower.SetCoinsReward(wave.coinsReward);
            follower.gameObject.layer = enemyLayer;
            
            // Subscribe to events
            follower.OnPathCompleteEvent.AddListener(OnEnemyReachedEnd);
            follower.OnEnemyKilled.AddListener(OnEnemyKilledByTower); // HANDLES TOWER KILLS
            follower.OnEnemyDestroyed.AddListener(OnEnemyDestroyedHandler); // HANDLES COINS
            
            _activeEnemies.Add(follower);
            _totalEnemiesSpawned++;
            
            // Notify that enemy spawned
            OnEnemySpawned?.Invoke(follower);
            
            Debug.Log($"Spawned enemy {_totalEnemiesSpawned} from wave {wave.waveNumber}");
        }
        
        private void OnEnemyReachedEnd(EnemyPathFollower enemy)
        {
            if (_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Remove(enemy);
                
                // Deal damage to player
                if (economyRef != null)
                {
                    economyRef.withDrag((int)enemy.DamageAtEndOfPath);
                    Debug.Log($"Enemy reached end! Player health: -{enemy.DamageAtEndOfPath} ({_activeEnemies.Count} enemies remaining)");
                }
            }
        }
        
        private void OnEnemyKilledByTower(EnemyPathFollower enemy)
        {
            // Remove from active list when killed by tower
            if (_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Remove(enemy);
                Debug.Log($"Enemy killed by tower! ({_activeEnemies.Count} enemies remaining)");
            }
        }
        
        private void OnEnemyDestroyedHandler(int coinsReward)
        {
            // Give coins when enemy is killed
            if (economyRef != null)
            {
                economyRef.AwardCoins(coinsReward);
                Debug.Log($"Enemy defeated! +{coinsReward} coins");
            }
        }
        
        // Manual control methods
        [ContextMenu("Start Next Wave")]
        public void StartNextWave()
        {
            if (_waveInProgress)
            {
                Debug.LogWarning("Wave already in progress!");
                return;
            }
            
            if (_currentWaveIndex >= waves.Length)
            {
                Debug.LogWarning("No more waves!");
                return;
            }
            
            StartCoroutine(SpawnWave(waves[_currentWaveIndex]));
            _currentWaveIndex++;
        }
        
        [ContextMenu("Skip Current Wave")]
        public void SkipCurrentWave()
        {
            // Clear all active enemies
            foreach (var enemy in new List<EnemyPathFollower>(_activeEnemies))
            {
                if (enemy != null)
                    Destroy(enemy.gameObject);
            }
            _activeEnemies.Clear();
        }
        
        public void SetAutoStart(bool enabled)
        {
            autoStartWaves = enabled;
        }
    }
}