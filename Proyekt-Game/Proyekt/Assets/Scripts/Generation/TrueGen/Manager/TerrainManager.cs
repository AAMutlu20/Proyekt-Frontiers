using Generation.TrueGen.Core;
using Generation.TrueGen.Generation;
using Generation.TrueGen.Systems;
using Generation.TrueGen.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Generation.TrueGen.Manager
{
    public class TerrainManager : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private int gridWidth = 20;
        [SerializeField] private int gridHeight = 15;
        [SerializeField] private float chunkSize = 10f;
        [SerializeField] private float distortionAmount = 2f;
        [SerializeField] private int seed = 12345;
        
        [Header("Path Settings")]
        [SerializeField] private float pathDepth = 0.3f;
        [SerializeField] private float pathRandomness = 0.2f;
        [SerializeField] private bool addPathSides = true;
        
        [Header("Props")]
        [SerializeField] private PropDefinition[] propDefinitions;
        [SerializeField] private bool generateProps = true;
        [SerializeField] private float randomBlockerChance = 0.05f;
        
        [Header("Materials")]
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Material gridOverlayMaterial;
        
        [Header("Test Prefabs")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject testBuildingPrefab;
        
        private ChunkNode[,] _chunks;
        private GameObject _terrainObject;
        private ChunkGrid _chunkGrid;

        private void Update()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame || !testBuildingPrefab) return;
            
            var placer = GetComponentInChildren<BuildingPlacement>();
            if (placer)
            {
                placer.TryPlaceBuildingAtMouse(testBuildingPrefab);
            }
        }
        
        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            // Clean up previous terrain
            if (_terrainObject)
                DestroyImmediate(_terrainObject);
            
            // Step 1: Generate chunk grid
            var chunkGen = new ChunkGenerator(seed);
            _chunks = chunkGen.GenerateDistortedGrid(gridWidth, gridHeight, chunkSize, distortionAmount);
            
            // Step 2: Select entry and exit points
            var entryChunk = _chunks[0, gridHeight / 2];
            var exitChunk = _chunks[gridWidth - 1, gridHeight / 2];
            
            // Step 3: Generate path
            var pathGen = new PathGenerator(seed + 1);
            var pathChunks = pathGen.GeneratePath(entryChunk, exitChunk, pathRandomness);
            PathGenerator.ApplyPathToChunks(pathChunks, pathDepth);
            
            // Step 3.5: Add random blockers
            if (randomBlockerChance > 0)
            {
                PropGenerator.AddRandomBlockers(_chunks, pathChunks, seed + 2, randomBlockerChance);
            }
            
            // Step 4: Build mesh
            var meshBuilder = new TerrainMeshBuilder();
            var terrainMesh = meshBuilder.BuildCombinedMesh(_chunks, addPathSides);
            
            // Step 5: Create terrain GameObject
            _terrainObject = new GameObject("GeneratedTerrain");
            _terrainObject.transform.SetParent(transform);
            _terrainObject.transform.localPosition = Vector3.zero;
            
            var mf = _terrainObject.AddComponent<MeshFilter>();
            mf.mesh = terrainMesh;
            
            var mr = _terrainObject.AddComponent<MeshRenderer>();
            mr.material = terrainMaterial ? terrainMaterial : CreateDefaultMaterial();
            
            var mc = _terrainObject.AddComponent<MeshCollider>();
            mc.sharedMesh = terrainMesh;
            
            // Step 6: Setup ChunkGrid component
            _chunkGrid = _terrainObject.AddComponent<ChunkGrid>();
            _chunkGrid.Initialize(_chunks, pathChunks);
            
            // Step 7: Add BuildingPlacement component and INITIALIZE IT
            var buildingPlacement = _terrainObject.AddComponent<BuildingPlacement>();
            buildingPlacement.Initialize(_chunkGrid); // Pass the ChunkGrid reference
            
            // Step 8: Add grid overlay and configure it
            if (gridOverlayMaterial)
            {
                var gridOverlay = _terrainObject.AddComponent<GridOverlayController>();
                gridOverlay.Initialize(gridOverlayMaterial);
            }
            
            // Step 9: Generate props
            if (generateProps && propDefinitions is { Length: > 0 })
            {
                var propGen = new PropGenerator(seed + 3, _terrainObject.transform);
                propGen.GenerateProps(_chunks, pathChunks, propDefinitions);
                
                // Rebuild mesh to reflect blocked chunks' colors
                terrainMesh = meshBuilder.BuildCombinedMesh(_chunks, addPathSides);
                mf.mesh = terrainMesh;
                mc.sharedMesh = terrainMesh;
            }
            
            Debug.Log($"✓ Generated terrain: {gridWidth}x{gridHeight} chunks, {pathChunks.Count} path chunks");
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
            
            follower.Initialize(_chunkGrid.PathChunks);
            
            Debug.Log("✓ Enemy spawned");
        }
        
        private static Material CreateDefaultMaterial()
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = Color.white
            };
            return mat;
        }
        
        public ChunkGrid GetChunkGrid() => _chunkGrid;
    }
}