using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class TerrainGenerator
    {
        private readonly int _seed;
        private readonly Terrain _terrain;
        private readonly TerrainData _terrainData;
        
        public TerrainGenerator(int seed, Vector3 position, int resolution = 512)
        {
            _seed = seed;
            
            // Create TerrainData
            _terrainData = new TerrainData
            {
                heightmapResolution = resolution + 1, // Unity adds 1
                size = new Vector3(200, 10, 200), // Width, Height, Length
                baseMapResolution = 512,
                alphamapResolution = 512
            };
            
            // Create Terrain GameObject
            var terrainObj = new GameObject("ProceduralTerrain")
            {
                transform =
                {
                    position = position
                }
            };

            _terrain = terrainObj.AddComponent<Terrain>();
            _terrain.terrainData = _terrainData;
            
            var collider = terrainObj.AddComponent<TerrainCollider>();
            collider.terrainData = _terrainData;
            
            // Initialize with flat heightmap
            InitializeFlatTerrain();
        }
        
        private void InitializeFlatTerrain()
        {
            var resolution = _terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];
            
            Random.InitState(_seed);
            
            // Add subtle noise for organic feel
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    // Base height (normalized 0-1)
                    const float baseHeight = 0.5f;
                    
                    // Add Perlin noise for gentle variation
                    var noise = Mathf.PerlinNoise(
                        x * 0.05f + _seed,
                        y * 0.05f + _seed
                    );
                    
                    // Very subtle - just enough for organic look
                    heights[y, x] = baseHeight + (noise - 0.5f) * 0.02f;
                }
            }
            
            _terrainData.SetHeights(0, 0, heights);
        }
        
        public Terrain GetTerrain() => _terrain;
        public TerrainData GetTerrainData() => _terrainData;
    }
}