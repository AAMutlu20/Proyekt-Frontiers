using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Generation.TrueGen.Generation
{
    public class TerrainGenerator
    {
        private readonly int _seed;
        private Terrain _terrain;
        private TerrainData _terrainData;
        
        public TerrainGenerator(int seed, Vector3 position, int resolution = 512)
        {
            _seed = seed;
            
            // Create TerrainData
            _terrainData = new TerrainData
            {
                heightmapResolution = resolution + 1,
                size = new Vector3(200, 10, 200),
                baseMapResolution = 512,
                alphamapResolution = 512
            };
            
            // CRITICAL: Save TerrainData as asset in Editor mode
            #if UNITY_EDITOR
            SaveTerrainDataAsAsset();
            #endif
            
            // Initialize with flat heightmap
            InitializeFlatTerrain();
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Save TerrainData as an actual asset (REQUIRED for Unity Terrain)
        /// </summary>
        private void SaveTerrainDataAsAsset()
        {
            var folderPath = "Assets/GeneratedTerrainData";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "GeneratedTerrainData");
            }
            
            var assetPath = $"{folderPath}/TerrainData_{System.Guid.NewGuid()}.asset";
            
            AssetDatabase.CreateAsset(_terrainData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"✓ Saved TerrainData asset: {assetPath}");
        }
        #endif
        
        private void InitializeFlatTerrain()
        {
            var resolution = _terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];
            
            Random.InitState(_seed);
            
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var baseHeight = 0.5f;
                    var noise = Mathf.PerlinNoise(x * 0.05f + _seed, y * 0.05f + _seed);
                    heights[y, x] = baseHeight + (noise - 0.5f) * 0.02f;
                }
            }
            
            _terrainData.SetHeights(0, 0, heights);
            
            #if UNITY_EDITOR
            EditorUtility.SetDirty(_terrainData);
            AssetDatabase.SaveAssets();
            #endif
        }
        
        /// <summary>
        /// Create the actual terrain GameObject AFTER all data is set up
        /// </summary>
        public Terrain CreateTerrainObject(Vector3 position)
        {
            Debug.Log("Creating Terrain GameObject with asset-backed data...");
            
            var terrainObj = new GameObject("ProceduralTerrain");
            terrainObj.transform.position = position;
            
            _terrain = terrainObj.AddComponent<Terrain>();
            _terrain.terrainData = _terrainData;
            
            var collider = terrainObj.AddComponent<TerrainCollider>();
            collider.terrainData = _terrainData;
            
            _terrain.Flush();
            
            #if UNITY_EDITOR
            EditorUtility.SetDirty(_terrain);
            EditorUtility.SetDirty(terrainObj);
            #endif
            
            Debug.Log("✓ Terrain created successfully with asset-backed data");
            
            return _terrain;
        }
        
        public Terrain GetTerrain() => _terrain;
        public TerrainData GetTerrainData() => _terrainData;
    }
}