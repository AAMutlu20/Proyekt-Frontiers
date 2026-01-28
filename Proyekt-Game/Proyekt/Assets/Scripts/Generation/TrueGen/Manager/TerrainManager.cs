using Generation.TrueGen.Core;
using Generation.TrueGen.Generation;
using Generation.TrueGen.Systems;
using Generation.TrueGen.Visuals;
using irminNavmeshEnemyAiUnityPackage;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Generation.TrueGen.Manager
{
    public class TerrainManager : MonoBehaviour
    {
        [Header("Runtime Settings")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private float generationDelay;
        
        [Header("Terrain")]
        [SerializeField] private Terrain existingTerrain;
        
        [Header("Generation Settings")]
        [SerializeField] private int gridSize = 20;
        [SerializeField] private float chunkSize = 10f;
        [SerializeField] private float distortionAmount = 2f;
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool randomizeSeed = true;
        
        [Header("Path Settings")]
        [SerializeField] private float pathDepth = 0.3f;
        [SerializeField] private float spiralTightness = 0.6f;
        
        [Header("Props")]
        [SerializeField] private GameObject[] grassPrefabs;
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private GameObject[] rockPrefabs;
        [SerializeField] private GameObject[] bushPrefabs;
        [SerializeField] private bool generateProps;
        
        [Header("Materials")]
        [SerializeField] private TerrainMaterialSet materialSet;
        
        [Header("Wave System")]
        [SerializeField] private bool enableWaveSystem = true;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int enemyLayer = 20;
        [SerializeField] private WaveDefinition[] waveDefinitionsToPassOn;
        
        [Header("Building System")]
        [SerializeField] private SOS_BuildingDatabase buildingDatabase;
        [SerializeField] private int selectedBuildIndex;
        [SerializeField] private GameObject selectedBuildingPrefab;
        
        [Header("Economy")]
        [SerializeField] private Economy economyRef;
        
        private ChunkNode[,] _chunks;
        private GameObject _terrainObject;
        private ChunkGrid _chunkGrid;
        private WaveManager _waveManager;
        private Terrain _terrain;
        private int _lastUsedSeed; // Store the actual seed used for generation

        public UnityEvent<EnemyPathFollower> onEnemySpawned;
        public UnityEvent onAllWavesCompleted;
        public UnityEvent onTerrainGenerated;

        private void Start()
        {
            Time.timeScale = 1f;
            if (!generateOnStart) return;
            
            if (generationDelay > 0)
            {
                Invoke(nameof(GenerateTerrain), generationDelay);
            }
            else
            {
                GenerateTerrain();
            }
        }

        private void Update()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame || EventSystem.current.IsPointerOverGameObject()) 
                return;
            
            var placer = GetComponentInChildren<BuildingPlacement>();
            if (!placer) return;
            
            var buildingToPlace = selectedBuildingPrefab;
            var buildingCost = 0;
            SOS_Building buildingToPlaceSOS = null;
            if (buildingDatabase)
            {
                if (selectedBuildIndex < 0 || selectedBuildIndex > buildingDatabase.GetBuildingCount() - 1) 
                { 
                    Debug.Log("Cannot try to build tower because selected index is invalid.");
                    return; 
                }
                buildingToPlaceSOS = buildingDatabase.GetBuilding(selectedBuildIndex);
                buildingToPlace = buildingToPlaceSOS.Building;
                buildingCost = buildingDatabase.GetBuilding(selectedBuildIndex).BuildingCost;
            }
            
            if (!buildingToPlace) return;
            
            if (economyRef && buildingCost > 0)
            {
                if (economyRef.CanAfford(buildingCost))
                {
                    if (!placer.TryPlaceBuildingAtMouse(buildingToPlace)) return;
                    economyRef.withDrag(buildingCost);
                    buildingToPlaceSOS.IncreasePriceOfBuilding();
                }
                else
                {
                    Debug.Log("Cannot afford building");
                }
            }
            else
            {
                placer.TryPlaceBuildingAtMouse(buildingToPlace);
            }
        }
        
        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            if (!materialSet)
            {
                Debug.LogError("TerrainMaterialSet not assigned!");
                return;
            }
            
            if (!existingTerrain)
            {
                Debug.LogError("❌ No terrain assigned! Please assign an existing Terrain in the inspector.");
                return;
            }
            
            // CLEANUP: Remove all old props before generating new terrain
            CleanupOldGeneration();
            
            var actualSeed = randomizeSeed ? Random.Range(0, 999999) : seed;
            _lastUsedSeed = actualSeed; // Store for reference
            Debug.Log($"🌍 Generating UNITY TERRAIN with seed: {actualSeed}");
            
            _terrain = existingTerrain;
            var terrainData = _terrain.terrainData;
            _terrainObject = _terrain.gameObject;
            
            var expectedSize = new Vector3(gridSize * chunkSize, 10, gridSize * chunkSize);
            if (Vector3.Distance(terrainData.size, expectedSize) > 0.1f)
            {
                Debug.LogWarning($"⚠ Terrain size mismatch! Expected {expectedSize}, got {terrainData.size}. Adjusting...");
                terrainData.size = expectedSize;
            }
            
            // STEP 0: Initialize terrain heightmap (flat)
            InitializeTerrainHeightmap(terrainData, actualSeed);
            
            // Step 1: Generate logical ChunkGrid
            var chunkGen = new ChunkGenerator(actualSeed);
            _chunks = chunkGen.GenerateDistortedGrid(gridSize, gridSize, chunkSize, distortionAmount);
            
            // Step 2: Generate path waypoints
            var pathGen = new PathGenerator(actualSeed + 1);
            var pathChunks = pathGen.GenerateSpiralPath(_chunks, gridSize, gridSize, spiralTightness);
            
            // Step 3: Mark castle area
            PathGenerator.MarkCastleArea(_chunks, gridSize, gridSize, castleSize: 3);
            PathGenerator.ApplyPathToChunks(pathChunks, pathDepth);
            
            // Step 3.5: Add hills to PERIPHERAL areas (far from path)
            AddPeripheralHills(terrainData, pathChunks, chunkSize * 0.7f, actualSeed);
            
            // Step 4: Setup texture layers
            var painter = new TerrainPainter(terrainData);
            painter.SetupTextureLayers(materialSet);
            
            // Step 5: Carve path into terrain
            var carver = new TerrainCarver(terrainData);
            carver.CarvePath(pathChunks, chunkSize * 0.7f, pathDepth);
            
            // Step 6: Paint textures
            painter.PaintPath(pathChunks, chunkSize * 0.7f);
            painter.PaintChunkTypes(_chunks);
            
            // Step 7: Refresh terrain
            _terrain.Flush();
            
            Debug.Log("✓ Terrain heightmap and textures applied");
            
            // Step 8: Place props
            if (generateProps)
            {
                var propGen = new PropGenerator(actualSeed + 3, transform);
                propGen.GenerateProps(_chunks, pathChunks, grassPrefabs, treePrefabs, rockPrefabs, bushPrefabs, _terrain);
            }
            
            // Step 9: Setup ChunkGrid
            _chunkGrid = _terrainObject.GetComponent<ChunkGrid>();
            if (!_chunkGrid)
                _chunkGrid = _terrainObject.AddComponent<ChunkGrid>();
            _chunkGrid.Initialize(_chunks, pathChunks);
            
            // Step 10: Building placement
            var buildingPlacement = _terrainObject.GetComponent<BuildingPlacement>();
            if (!buildingPlacement)
                buildingPlacement = _terrainObject.AddComponent<BuildingPlacement>();
            buildingPlacement.Initialize(_chunkGrid);
            
            // Step 10.5: Building placement indicator
            var placementIndicator = _terrainObject.GetComponent<BuildingPlacementIndicator>();
            if (!placementIndicator)
                placementIndicator = _terrainObject.AddComponent<BuildingPlacementIndicator>();
            
            // Step 11: Wave Manager
            if (enableWaveSystem)
            {
                _waveManager = _terrainObject.GetComponent<WaveManager>();
                if (!_waveManager)
                    _waveManager = _terrainObject.AddComponent<WaveManager>();
                if (waveDefinitionsToPassOn.Length > 0) { _waveManager.WaveDefinitions = waveDefinitionsToPassOn; }
                _waveManager.onAllWavesCompleted.RemoveAllListeners();
                _waveManager.onAllWavesCompleted.AddListener(AllWavesCompleted);
                _waveManager.Initialize(_chunkGrid, economyRef, enemyPrefab, enemyLayer);
                _waveManager.onEnemySpawned.AddListener((enemy) => onEnemySpawned?.Invoke(enemy));
                
                Debug.Log("✓ Wave system initialized");
            }
            
            Debug.Log($"✓ Generated TERRAIN: {gridSize}x{gridSize} grid with {pathChunks.Count} waypoints");
            Debug.Log($"🎲 Seed used: {actualSeed} (Copy this to regenerate the same terrain!)");
            
            onTerrainGenerated?.Invoke();
        }
        
        /// <summary>
        /// Initialize terrain with flat base heightmap
        /// </summary>
        private void InitializeTerrainHeightmap(TerrainData terrainData, int seed)
        {
            var resolution = terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];
            
            Random.InitState(seed);
            
            Debug.Log($"Initializing terrain heightmap: {resolution}x{resolution}");
            
            // Start with flat base at middle height
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    heights[y, x] = 0.5f; // Middle height
                }
            }
            
            terrainData.SetHeights(0, 0, heights);
            
            Debug.Log("✓ Terrain heightmap initialized (flat base)");
        }
        
        /// <summary>
        /// Add subtle hills to peripheral areas FAR from the path
        /// Path and nearby areas stay FLAT
        /// </summary>
        private void AddPeripheralHills(TerrainData terrainData, System.Collections.Generic.List<ChunkNode> pathChunks, float pathWidth, int seed)
        {
            var resolution = terrainData.heightmapResolution;
            var heights = terrainData.GetHeights(0, 0, resolution, resolution);
            var terrainSize = terrainData.size;
            
            Random.InitState(seed + 100);
            
            Debug.Log("Adding subtle hills to peripheral areas (path stays flat)...");
            
            // Generate smooth spline points along path
            var splinePoints = new System.Collections.Generic.List<Vector3>();
            foreach (var chunk in pathChunks)
                splinePoints.Add(chunk.center);
            
            var smoothPoints = SmoothPathMeshGenerator.GenerateCatmullRomSpline(splinePoints, 6);
            
            // For each heightmap point, find distance to nearest path point
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    // Convert heightmap coords to world position
                    var worldX = (x / (float)resolution) * terrainSize.x;
                    var worldZ = (y / (float)resolution) * terrainSize.z;
                    var worldPos = new Vector3(worldX, 0, worldZ);
                    
                    // Find distance to nearest path point
                    var minDistToPath = float.MaxValue;
                    foreach (var pathPoint in smoothPoints)
                    {
                        var dist = Vector3.Distance(new Vector3(worldPos.x, 0, worldPos.z), 
                                                   new Vector3(pathPoint.x, 0, pathPoint.z));
                        if (dist < minDistToPath)
                            minDistToPath = dist;
                    }
                    
                    // Define zones
                    var flatZoneRadius = pathWidth / 2f + 10f; // Path + 10 units = totally flat
                    var transitionZoneRadius = flatZoneRadius + 15f; // 15 units of smooth transition
                    
                    // If we're in the flat zone, skip this point entirely
                    if (minDistToPath < flatZoneRadius)
                        continue;
                    
                    // Calculate influence (0 = flat zone, 1 = far from path)
                    float influence;
                    if (minDistToPath < transitionZoneRadius)
                    {
                        // Transition zone - smooth ramp up
                        var t = (minDistToPath - flatZoneRadius) / (transitionZoneRadius - flatZoneRadius);
                        influence = Mathf.SmoothStep(0f, 1f, t);
                    }
                    else
                    {
                        // Far from path - full influence
                        influence = 1f;
                    }
                    
                    // Generate SMOOTH hills using Perlin noise
                    var noiseValue = Mathf.PerlinNoise(
                        x * 0.03f + seed,
                        y * 0.03f + seed
                    );
                    
                    // Apply subtle height variation (gentle hills)
                    var variation = (noiseValue - 0.5f) * 0.04f; // ±2% height variation
                    heights[y, x] += variation * influence;
                    
                    // Clamp to valid range
                    heights[y, x] = Mathf.Clamp01(heights[y, x]);
                }
            }
            
            terrainData.SetHeights(0, 0, heights);
            
            Debug.Log("✓ Added subtle hills to peripheral areas (path stays flat)");
        }

        private void AllWavesCompleted()
        {
            onAllWavesCompleted?.Invoke();
        }
        
        /// <summary>
        /// Clean up old generation before creating new terrain
        /// </summary>
        private void CleanupOldGeneration()
        {
            Debug.Log("🧹 Cleaning up old generation...");
            
            // Find and destroy all old props (children of TerrainManager)
            var propsToDestroy = new System.Collections.Generic.List<GameObject>();
            
            foreach (Transform child in transform)
            {
                // Don't destroy the terrain itself
                if (child.gameObject == _terrainObject)
                    continue;
                
                // Destroy all prop objects (Grass, Tree, Rock, Bush, Prop)
                if (child.name.StartsWith("Grass_") || 
                    child.name.StartsWith("Tree_") || 
                    child.name.StartsWith("Rock_") || 
                    child.name.StartsWith("Bush_") || 
                    child.name.StartsWith("Prop_"))
                {
                    propsToDestroy.Add(child.gameObject);
                }
            }
            
            // Destroy all collected props
            foreach (var prop in propsToDestroy)
            {
                if (Application.isPlaying)
                    Destroy(prop);
                else
                    DestroyImmediate(prop);
            }
            
            if (propsToDestroy.Count > 0)
                Debug.Log($"✓ Destroyed {propsToDestroy.Count} old props");
            
            // Hide placement indicator if it exists
            if (_terrainObject)
            {
                var indicator = _terrainObject.GetComponent<BuildingPlacementIndicator>();
                if (indicator)
                    indicator.HideIndicator();
            }
        }

        [ContextMenu("Spawn Test Enemy")]
        public void SpawnTestEnemy()
        {
            if (!_chunkGrid || _chunkGrid.PathChunks == null)
            {
                Debug.LogError("Generate terrain first!");
                return;
            }
    
            if (!enemyPrefab)
            {
                Debug.LogError("Assign enemy prefab!");
                return;
            }
    
            var enemy = Instantiate(enemyPrefab);
            var follower = enemy.GetComponent<EnemyPathFollower>();
    
            if (!follower)
                follower = enemy.AddComponent<EnemyPathFollower>();
    
            var followerHealthSystem = enemy.AddComponent<IrminBaseHealthSystem>();
            followerHealthSystem.DestroyAtMinHealth = true;
    
            var enemyFactionMemberComponent = enemy.AddComponent<FactionMemberComponent>();
            followerHealthSystem.FactionMemberComponent = enemyFactionMemberComponent;
            followerHealthSystem.Faction = 1;
    
            var enemyRigidBody = enemy.AddComponent<Rigidbody>();
            enemyRigidBody.useGravity = false;
            enemyRigidBody.isKinematic = true;
            

            followerHealthSystem.ReAwaken(2);
    
            follower.Initialize(_chunkGrid.PathChunks, _terrain);
            follower.gameObject.layer = enemyLayer;
            onEnemySpawned?.Invoke(follower);
    
            Debug.Log("✓ Enemy spawned");
        }
        
        [ContextMenu("Regenerate Terrain (New Seed)")]
        public void RegenerateTerrain()
        {
            randomizeSeed = true;
            GenerateTerrain();
        }
        
        [ContextMenu("Regenerate Terrain (Same Seed)")]
        public void RegenerateWithSameSeed()
        {
            if (_lastUsedSeed == 0)
            {
                Debug.LogWarning("No terrain has been generated yet!");
                return;
            }
            
            randomizeSeed = false;
            seed = _lastUsedSeed;
            GenerateTerrain();
        }
        
        [ContextMenu("Copy Current Seed to Clipboard")]
        public void CopySeedToClipboard()
        {
            if (_lastUsedSeed == 0)
            {
                Debug.LogWarning("No terrain has been generated yet!");
                return;
            }
            
            GUIUtility.systemCopyBuffer = _lastUsedSeed.ToString();
            Debug.Log($"📋 Copied seed to clipboard: {_lastUsedSeed}");
        }

        public void SelectBuilding(int pIndex)
        {
            if (!buildingDatabase || pIndex < 0 || pIndex >= buildingDatabase.GetBuildingCount())
            {
                Debug.LogError($"Invalid building index: {pIndex}");
                return;
            }
            
            Debug.Log($"Selected building of index {pIndex} with name {buildingDatabase.GetBuilding(pIndex).Building.name}");
            
            selectedBuildingPrefab = buildingDatabase.GetBuilding(pIndex).Building;
            selectedBuildIndex = pIndex;
            
            // Show placement indicator
            var indicator = _terrainObject?.GetComponent<BuildingPlacementIndicator>();
            if (indicator)
                indicator.ShowIndicator();
        }
        
        /// <summary>
        /// Deselect current building and hide indicator
        /// </summary>
        public void DeselectBuilding()
        {
            selectedBuildingPrefab = null;
            selectedBuildIndex = -1;
            
            // Hide placement indicator
            var indicator = _terrainObject?.GetComponent<BuildingPlacementIndicator>();
            if (indicator)
                indicator.HideIndicator();
        }
        
        public ChunkGrid GetChunkGrid() => _chunkGrid;
        public WaveManager GetWaveManager() => _waveManager;
        public int GetCurrentSeed() => _lastUsedSeed; // Return the actual seed that was used
        public Terrain GetTerrain() => _terrain;
    }
}