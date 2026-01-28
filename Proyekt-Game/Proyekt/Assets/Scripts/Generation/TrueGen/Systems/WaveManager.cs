using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
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
        [SerializeField] private SOS_EnemyDatabase _enemyDatabase;
        [SerializeField] private int enemyLayer = 20;
        
        [Header("Economy")]
        [SerializeField] private Economy economyRef;
        
        [Header("Audio")]
        [SerializeField] private GameAudioClips audioClips;
        
        private ChunkGrid _chunkGrid;
        private readonly List<EnemyPathFollower> _activeEnemies = new();
        private int _currentWaveIndex;
        private int _totalEnemiesSpawned;
        
        public int CurrentWave => _currentWaveIndex + 1;
        public int TotalWaves => waves.Length;
        private bool IsWaveActive { get; set; }
        public int ActiveEnemyCount => _activeEnemies.Count;
        
        public UnityEvent<int> onWaveStarted = new();
        public UnityEvent<int> onWaveCompleted = new();
        public UnityEvent<EnemyPathFollower> onEnemySpawned = new();
        public UnityEvent onAllWavesCompleted = new();

        public WaveDefinition[] WaveDefinitions {
            set => waves = value;
        }
        
        public void Initialize(ChunkGrid chunkGrid, Economy economy, GameObject enemyPrefab, int layer)
        {
            if (DatabaseAccessor.Singleton) { _enemyDatabase = DatabaseAccessor.Singleton.GeneralEnemyDatabase; }

            _chunkGrid = chunkGrid;
            economyRef = economy;
            defaultEnemyPrefab = enemyPrefab;
            enemyLayer = layer;
            
            if (!_chunkGrid)
            {
                Debug.LogError("WaveManager: ChunkGrid is required!");
                enabled = false;
                return;
            }
            
            if (!defaultEnemyPrefab)
            {
                Debug.LogError("WaveManager: No enemy prefab assigned!");
                enabled = false;
                return;
            }
            
            if (!economyRef)
            {
                Debug.LogWarning("WaveManager: No economy assigned! Rewards won't work.");
            }
            
            if (waves == null || waves.Length == 0)
            {
                CreateDefaultWaves();
            }
            
            if (autoStartWaves)
            {
                StartCoroutine(WaveSequence());
            }

            UI_WaveCounter.Singleton.SetWaveText(_currentWaveIndex, waves.Length);
            
            // Initialize audio - start game music and ambiance with fade in
            if (!audioClips) return;
            if (audioClips.gameMusic)
                AudioManager.Instance?.PlayMusic(audioClips.gameMusic, 0.5f, 2f);
                
            if (audioClips.ambianceLoop)
                AudioManager.Instance?.PlayAmbiance(audioClips.ambianceLoop, 0.3f, 2f);
        }
        
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.kKey.wasPressedThisFrame) return;
            Debug.Log($"=== Wave Debug ===");
            Debug.Log($"Current Wave: {_currentWaveIndex + 1}/{waves.Length}");
            Debug.Log($"Active Enemies: {_activeEnemies.Count}");
            Debug.Log($"Wave In Progress: {IsWaveActive}");
                
            var nullCount = _activeEnemies.Count(e => !e);
            if (nullCount <= 0) return;
            {
                Debug.LogWarning($"Found {nullCount} null enemies in list! Cleaning up...");
                _activeEnemies.RemoveAll(e => !e);
                Debug.Log($"After cleanup: {_activeEnemies.Count} active enemies");
            }
        }
        
        private void CreateDefaultWaves()
        {
            if (waves != null) return;
            waves = new WaveDefinition[3];
            
            waves[0] = new WaveDefinition
            {
                waveNumber = 1,
                enemyCount = 3,
                spawnInterval = 1f,
                delayBeforeWave = 2f,
                enemyPrefab = _enemyDatabase.GetEnemy(1).EnemyPrefab,
                enemySpeed = 5f,
                enemyHealth = 2f,
                damageToPlayer = 1f,
                coinsReward = 10,
                goldReward = 100
            };
            
            waves[1] = new WaveDefinition
            {
                waveNumber = 2,
                enemyCount = 5,
                spawnInterval = 0.8f,
                delayBeforeWave = 3f,
                enemyPrefab = _enemyDatabase.GetEnemy(0).EnemyPrefab,
                enemySpeed = _enemyDatabase.GetEnemy(0).EnemySpeed,
                enemyHealth = _enemyDatabase.GetEnemy(0).EnemyHealth,
                damageToPlayer = _enemyDatabase.GetEnemy(0).DamageToPlayer,
                coinsReward = _enemyDatabase.GetEnemy(0).CoinsReward,
                goldReward = 150
            };
            
            waves[2] = new WaveDefinition
            {
                waveNumber = 3,
                enemyCount = 10,
                spawnInterval = 0.5f,
                delayBeforeWave = 3f,
                enemyPrefab = _enemyDatabase.GetEnemy(2).EnemyPrefab,
                enemySpeed = _enemyDatabase.GetEnemy(2).EnemySpeed,
                enemyHealth = _enemyDatabase.GetEnemy(2).EnemyHealth,
                damageToPlayer = _enemyDatabase.GetEnemy(2).DamageToPlayer,
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
                
                Debug.Log($"Wave {wave.waveNumber} starting in {wave.delayBeforeWave} seconds...");
                yield return new WaitForSeconds(wave.delayBeforeWave);
                
                onWaveStarted?.Invoke(wave.waveNumber);
                
                // Play wave spawn sound
                if (audioClips && audioClips.waveSpawnSound)
                {
                    AudioManager.Instance?.PlaySFX(audioClips.waveSpawnSound);
                }
                
                yield return StartCoroutine(SpawnWave(wave));
                
                Debug.Log($"Wave {wave.waveNumber} spawned! Waiting for enemies to be defeated...");
                yield return new WaitUntil(() => _activeEnemies.Count == 0);
                
                if (economyRef)
                {
                    economyRef.AwardCoins(wave.goldReward);
                    Debug.Log($"✓ Wave {wave.waveNumber} completed! Reward: {wave.goldReward} gold");
                }
                
                onWaveCompleted?.Invoke(wave.waveNumber);
                
                _currentWaveIndex++;
                
                if (_currentWaveIndex < waves.Length)
                {
                    yield return new WaitForSeconds(timeBetweenWaves);
                }
            }
            
            Debug.Log("=== All Waves Completed! Victory! ===");
            
            // Play victory sound and fade out music/ambiance
            if (audioClips)
            {
                AudioManager.Instance?.StopMusic(1f);
                AudioManager.Instance?.StopAmbiance(1f);
                
                if (audioClips.victorySound)
                    AudioManager.Instance?.PlaySFX(audioClips.victorySound);
            }
            
            onAllWavesCompleted?.Invoke();
        }
        
        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            IsWaveActive = true;
            
            Debug.Log($">>> Spawning Wave {wave.waveNumber}: {wave.enemyCount} enemies");
            UI_WaveCounter.Singleton.SetWaveText(_currentWaveIndex, waves.Length);
            
            for (var i = 0; i < (wave._useSpecificEnemies ? wave._specificEnemies.Count : wave.enemyCount); i++)
            {
                if(wave._useSpecificEnemies)
                {
                    SpawnEnemy(wave, i);
                }
                else
                {
                    SpawnEnemy(wave);
                }
                    
                yield return new WaitForSeconds(wave.spawnInterval);
            }
            
            IsWaveActive = false;
        }
        
        private void SpawnEnemy(WaveDefinition wave, int pSpecificEnemy = -1)
        {
            if (!_chunkGrid || _chunkGrid.PathChunks == null || _chunkGrid.PathChunks.Count == 0)
            {
                Debug.LogError("Cannot spawn enemy: No path available!");
                return;
            }
            
            var prefab = wave._useSpecificEnemies && pSpecificEnemy >= 0 ? wave._specificEnemies[pSpecificEnemy].EnemyPrefab : ( wave.enemyPrefab ? wave.enemyPrefab : defaultEnemyPrefab);
            
            if (!prefab)
            {
                Debug.LogError("No enemy prefab assigned!");
                return;
            }
            
            var spawnChunk = _chunkGrid.PathChunks[0];
            var spawnPos = spawnChunk.center;
            
            var terrain = GetComponent<Terrain>();
            if (terrain != null)
            {
                spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
            }
            else
            {
                spawnPos.y = spawnChunk.yOffset;
            }
            
            var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemy.name = $"Enemy_Wave{wave.waveNumber}_{_totalEnemiesSpawned + 1}";
            
            var follower = enemy.GetComponent<EnemyPathFollower>();
            if (!follower)
            {
                follower = enemy.AddComponent<EnemyPathFollower>();
            }
            
            var healthSystem = enemy.GetComponent<IrminBaseHealthSystem>();
            if (!healthSystem)
            {
                healthSystem = enemy.AddComponent<IrminBaseHealthSystem>();
            }
            healthSystem.DestroyAtMinHealth = true;
            
            var factionComponent = enemy.GetComponent<FactionMemberComponent>();
            if (!factionComponent)
            {
                factionComponent = enemy.AddComponent<FactionMemberComponent>();
            }
            healthSystem.FactionMemberComponent = factionComponent;
            healthSystem.Faction = 1;
            
            var rb = enemy.GetComponent<Rigidbody>();
            if (!rb)
            {
                rb = enemy.AddComponent<Rigidbody>();
            }
            rb.useGravity = false;
            rb.isKinematic = true;
            
            healthSystem.ReAwaken(wave.enemyHealth);
            follower.Initialize(_chunkGrid.PathChunks, terrain);
            if (wave._useSpecificEnemies)
            {
                follower.SetSpeed(wave._specificEnemies[pSpecificEnemy].EnemySpeed);
                follower.SetDamage(wave._specificEnemies[pSpecificEnemy].DamageToPlayer);
                follower.SetCoinsReward(wave._specificEnemies[pSpecificEnemy].CoinsReward);
                healthSystem.AudioClips = wave._specificEnemies[pSpecificEnemy].AudioClips;
            }
            else
            {
                follower.SetSpeed(wave.enemySpeed);
                follower.SetDamage(wave.damageToPlayer);
                follower.SetCoinsReward(wave.coinsReward);
            }
            follower.gameObject.layer = enemyLayer;
            
            follower.onPathCompleteEvent.AddListener(OnEnemyReachedEnd);
            follower.onEnemyKilled.AddListener(OnEnemyKilledByTower);
            follower.onEnemyDestroyed.AddListener(OnEnemyDestroyedHandler);
            
            _activeEnemies.Add(follower);
            _totalEnemiesSpawned++;
            
            onEnemySpawned?.Invoke(follower);
            
            Debug.Log($"Spawned enemy {_totalEnemiesSpawned} from wave {wave.waveNumber}");
        }
        
        private void OnEnemyReachedEnd(EnemyPathFollower enemy)
        {
            if (!_activeEnemies.Contains(enemy)) return;
            _activeEnemies.Remove(enemy);
                
            if (!economyRef) return;
            economyRef.withDrag((int)enemy.DamageAtEndOfPath);
            Debug.Log($"Enemy reached end! Player health: -{enemy.DamageAtEndOfPath} ({_activeEnemies.Count} enemies remaining)");
        }
        
        private void OnEnemyKilledByTower(EnemyPathFollower enemy)
        {
            if (!_activeEnemies.Contains(enemy)) return;
            _activeEnemies.Remove(enemy);
            Debug.Log($"Enemy killed by tower! ({_activeEnemies.Count} enemies remaining)");
        }
        
        private void OnEnemyDestroyedHandler(int coinsReward)
        {
            if (!economyRef) return;
            economyRef.AwardCoins(coinsReward);
            Debug.Log($"Enemy defeated! +{coinsReward} coins");
        }
        
        [ContextMenu("Start Next Wave")]
        public void StartNextWave()
        {
            UI_WaveCounter.Singleton.SetWaveText(_currentWaveIndex, waves.Length);
            if (IsWaveActive)
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
            foreach (var enemy in new List<EnemyPathFollower>(_activeEnemies).Where(enemy => enemy))
            {
                Destroy(enemy.gameObject);
            }

            _activeEnemies.Clear();
        }
        
        public void SetAutoStart(bool isEnabled)
        {
            autoStartWaves = isEnabled;
        }
    }
}