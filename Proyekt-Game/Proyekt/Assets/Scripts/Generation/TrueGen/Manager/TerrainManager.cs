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
        
        [Header("Terrain Mode")]
        [SerializeField] private bool useUnityTerrain = true; // Toggle between systems
        [SerializeField] private int terrainResolution = 512;
        
        [Header("Generation Settings")]
        [SerializeField] private int gridSize = 20;
        [SerializeField] private float chunkSize = 10f;
        [SerializeField] private float distortionAmount = 2f;
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool randomizeSeed = true;
        
        [Header("Path Settings")]
        [SerializeField] private float pathDepth = 0.3f;
        [SerializeField] private float spiralTightness = 0.6f;
        [SerializeField] private bool addPathSides = true;
        
        [Header("Props")]
        [SerializeField] private PropDefinition[] propDefinitions;
        [SerializeField] private bool generateProps;
        
        [Header("Materials")]
        [SerializeField] private TerrainMaterialSet materialSet;
        [SerializeField] private Material gridOverlayMaterial;
        
        [Header("Wave System")]
        [SerializeField] private bool enableWaveSystem = true;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int enemyLayer = 20;
        
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
        private Terrain _terrain; // NEW - Store terrain reference

        public UnityEvent<EnemyPathFollower> onEnemySpawned;
        public UnityEvent onAllWavesCompleted;
        public UnityEvent onTerrainGenerated;

        private void Start()
        {
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
            
            if (buildingDatabase)
            {
                if (selectedBuildIndex < 0 || selectedBuildIndex > buildingDatabase.GetBuildingCount() - 1) 
                { 
                    Debug.Log("Cannot try to build tower because selected index is invalid.");
                    return; 
                }
                buildingToPlace = buildingDatabase.GetBuilding(selectedBuildIndex).Building;
                buildingCost = buildingDatabase.GetBuilding(selectedBuildIndex).BuildingCost;
            }
            
            if (!buildingToPlace) return;
            
            if (economyRef && buildingCost > 0)
            {
                if (economyRef.CanAfford(buildingCost))
                {
                    economyRef.withDrag(buildingCost);
                    placer.TryPlaceBuildingAtMouse(buildingToPlace);
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
            if (_terrainObject)
                DestroyImmediate(_terrainObject);
            
            if (!materialSet)
            {
                Debug.LogError("TerrainMaterialSet not assigned!");
                return;
            }
            
            // Route to appropriate generation method
            if (useUnityTerrain)
            {
                GenerateTerrainMode();
            }
            else
            {
                GenerateMeshMode();
            }
        }

        /// <summary>
        /// Generate terrain using Unity Terrain system
        /// </summary>
        private void GenerateTerrainMode()
        {
            var actualSeed = randomizeSeed ? Random.Range(0, 999999) : seed;
            Debug.Log($"🌍 Generating UNITY TERRAIN with seed: {actualSeed}");
            
            // Step 1: Create TerrainData (but not the GameObject yet)
            var terrainGen = new TerrainGenerator(actualSeed, transform.position, terrainResolution);
            var terrainData = terrainGen.GetTerrainData();
            
            // Step 2: Generate logical ChunkGrid (invisible, regular grid for gameplay)
            var chunkGen = new ChunkGenerator(actualSeed);
            _chunks = chunkGen.GenerateDistortedGrid(gridSize, gridSize, chunkSize, distortionAmount);
            
            // Step 3: Generate path waypoints
            var pathGen = new PathGenerator(actualSeed + 1);
            var pathChunks = pathGen.GenerateSpiralPath(_chunks, gridSize, gridSize, spiralTightness);
            
            // Step 4: Mark castle area
            PathGenerator.MarkCastleArea(_chunks, gridSize, gridSize, castleSize: 3);
            PathGenerator.ApplyPathToChunks(pathChunks, pathDepth);
            
            // Step 5: Carve path into terrain
            var carver = new TerrainCarver(terrainData);
            carver.CarvePath(pathChunks, chunkSize * 0.7f, pathDepth);
            
            // Step 6: Paint textures
            var painter = new TerrainPainter(terrainData);
            painter.SetupTextureLayers(materialSet);
            painter.PaintPath(pathChunks, chunkSize * 0.7f);
            painter.PaintChunkTypes(_chunks);
            
            // Step 7: NOW create the actual terrain GameObject with all data configured
            _terrain = terrainGen.CreateTerrainObject(transform.position);
            _terrain.transform.SetParent(transform);
            _terrainObject = _terrain.gameObject;
            
            // Step 8: Place props on terrain
            if (generateProps && propDefinitions is { Length: > 0 })
            {
                var propGen = new PropGenerator(actualSeed + 3, transform);
                propGen.GenerateProps(_chunks, pathChunks, propDefinitions, _terrain);
            }
            
            // Step 9: Setup ChunkGrid component
            _chunkGrid = _terrainObject.AddComponent<ChunkGrid>();
            _chunkGrid.Initialize(_chunks, pathChunks);
            
            // Step 10: Building placement (works on logical grid)
            var buildingPlacement = _terrainObject.AddComponent<BuildingPlacement>();
            buildingPlacement.Initialize(_chunkGrid);
            
            // Step 11: Grid overlay (skip for terrain mode)
            // Grid overlay is designed for mesh mode only
            
            // Step 12: Wave Manager (unchanged)
            if (enableWaveSystem)
            {
                _waveManager = _terrainObject.AddComponent<WaveManager>();
                _waveManager.onAllWavesCompleted.AddListener(AllWavesCompleted);
                _waveManager.Initialize(_chunkGrid, economyRef, enemyPrefab, enemyLayer);
                _waveManager.onEnemySpawned.AddListener((enemy) => onEnemySpawned?.Invoke(enemy));
                Debug.Log("✓ Wave system initialized");
            }
            
            Debug.Log($"✓ Generated TERRAIN: {gridSize}x{gridSize} grid with {pathChunks.Count} waypoints");
            
            onTerrainGenerated?.Invoke();
        }

        /// <summary>
        /// Generate terrain using custom mesh system
        /// </summary>
        private void GenerateMeshMode()
        {
            var actualSeed = randomizeSeed ? Random.Range(0, 999999) : seed;
            Debug.Log($"🎨 Generating MESH TERRAIN with seed: {actualSeed}");
            
            // Step 1: Generate SQUARE chunk grid
            var chunkGen = new ChunkGenerator(actualSeed);
            _chunks = chunkGen.GenerateDistortedGrid(gridSize, gridSize, chunkSize, distortionAmount);
            
            // Step 2: Generate spiral path
            var pathGen = new PathGenerator(actualSeed + 1);
            var pathChunks = pathGen.GenerateSpiralPath(_chunks, gridSize, gridSize, spiralTightness);
            
            // Step 3: Mark castle area
            PathGenerator.MarkCastleArea(_chunks, gridSize, gridSize, castleSize: 3);
            
            // Step 4: Apply path properties to chunks (for collision/gameplay)
            PathGenerator.ApplyPathToChunks(pathChunks, pathDepth);
            
            // Step 5: Generate props
            if (generateProps && propDefinitions is { Length: > 0 })
            {
                var propGen = new PropGenerator(actualSeed + 3, transform);
                propGen.GenerateProps(_chunks, pathChunks, propDefinitions, null); // No terrain
            }
            
            // Step 6: Build main terrain mesh
            var meshBuilder = new TerrainMeshBuilder();
            var terrainMesh = meshBuilder.BuildCombinedMesh(_chunks, materialSet, false);
            
            // Step 7: Create main terrain GameObject
            _terrainObject = new GameObject("GeneratedTerrain");
            _terrainObject.transform.SetParent(transform);
            _terrainObject.transform.localPosition = Vector3.zero;
            
            var mf = _terrainObject.AddComponent<MeshFilter>();
            mf.mesh = terrainMesh;
            
            var mr = _terrainObject.AddComponent<MeshRenderer>();
            
            var materials = new Material[5];
            materials[0] = materialSet.buildableMaterial;
            materials[1] = materialSet.pathMaterial;
            materials[2] = materialSet.blockedMaterial;
            materials[3] = materialSet.decorativeMaterial;
            materials[4] = materialSet.wallMaterial;
            
            mr.materials = materials;
            
            var mc = _terrainObject.AddComponent<MeshCollider>();
            mc.sharedMesh = terrainMesh;
            
            // Step 8: Generate a smooth path as a separate mesh
            var smoothPathObj = new GameObject("SmoothPath");
            smoothPathObj.transform.SetParent(_terrainObject.transform);
            smoothPathObj.transform.localPosition = Vector3.zero;
            
            var pathMesh = SmoothPathMeshGenerator.GenerateSmoothPathMesh(
                pathChunks, 
                pathWidth: chunkSize * 0.7f,
                pathDepth: pathDepth,
                segmentsPerChunk: 6
            );
            
            var pathMf = smoothPathObj.AddComponent<MeshFilter>();
            pathMf.mesh = pathMesh;
            
            var pathMr = smoothPathObj.AddComponent<MeshRenderer>();
            pathMr.material = materialSet.pathMaterial;
            pathMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            // Add smooth path walls
            if (addPathSides)
            {
                var wallsObj = new GameObject("PathWalls");
                wallsObj.transform.SetParent(_terrainObject.transform);
                wallsObj.transform.localPosition = Vector3.zero;
                
                var wallsMesh = SmoothPathMeshGenerator.GenerateSmoothPathWalls(
                    pathChunks,
                    pathWidth: chunkSize * 0.7f,
                    pathDepth: pathDepth,
                    segmentsPerChunk: 6
                );
                
                var wallsMf = wallsObj.AddComponent<MeshFilter>();
                wallsMf.mesh = wallsMesh;
                
                var wallsMr = wallsObj.AddComponent<MeshRenderer>();
                wallsMr.material = materialSet.wallMaterial;
                wallsMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            
            var pathCollider = smoothPathObj.AddComponent<MeshCollider>();
            pathCollider.sharedMesh = pathMesh;
            pathCollider.convex = false;
            
            // Step 9: Setup ChunkGrid component
            _chunkGrid = _terrainObject.AddComponent<ChunkGrid>();
            _chunkGrid.Initialize(_chunks, pathChunks);
            
            // Step 10: Add BuildingPlacement component
            var buildingPlacement = _terrainObject.AddComponent<BuildingPlacement>();
            buildingPlacement.Initialize(_chunkGrid);
            
            // Step 11: Add grid overlay
            if (gridOverlayMaterial)
            {
                var gridOverlay = _terrainObject.AddComponent<GridOverlayController>();
                gridOverlay.Initialize(gridOverlayMaterial);
            }
            
            // Step 12: Add Wave Manager
            if (enableWaveSystem)
            {
                _waveManager = _terrainObject.AddComponent<WaveManager>();
                _waveManager.onAllWavesCompleted.AddListener(AllWavesCompleted);
                _waveManager.Initialize(_chunkGrid, economyRef, enemyPrefab, enemyLayer);
                _waveManager.onEnemySpawned.AddListener((enemy) => onEnemySpawned?.Invoke(enemy));
                Debug.Log("✓ Wave system initialized");
            }
            
            Debug.Log($"✓ Generated MESH: {gridSize}x{gridSize} terrain with {pathChunks.Count} path chunks");
            
            onTerrainGenerated?.Invoke();
        }

        private void AllWavesCompleted()
        {
            onAllWavesCompleted?.Invoke();
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
            
            follower.Initialize(_chunkGrid.PathChunks);
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
        }
        
        public ChunkGrid GetChunkGrid() => _chunkGrid;
        public WaveManager GetWaveManager() => _waveManager;
        public int GetCurrentSeed() => seed;
        public Terrain GetTerrain() => _terrain;
    }
}