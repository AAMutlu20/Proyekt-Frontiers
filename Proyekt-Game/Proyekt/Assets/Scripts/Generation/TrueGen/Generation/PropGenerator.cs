using System.Collections.Generic;
using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class PropGenerator
    {
        private readonly int _seed;
        private readonly Transform _propParent;
        
        public PropGenerator(int seed, Transform parent = null)
        {
            _seed = seed;
            _propParent = parent;
        }
        
        /// <summary>
        /// Generate props with layered system using simple GameObject arrays
        /// - Grass everywhere on Buildable chunks
        /// - Mini environments (trees/rocks/bushes) based on distance to path
        /// - Empty chunks more common near path for building placement
        /// </summary>
        public void GenerateProps(
            ChunkNode[,] chunks, 
            List<ChunkNode> pathChunks,
            GameObject[] grassPrefabs,
            GameObject[] treePrefabs,
            GameObject[] rockPrefabs,
            GameObject[] bushPrefabs,
            Terrain terrain = null)
        {
            Random.InitState(_seed);
            
            var width = chunks.GetLength(0);
            var height = chunks.GetLength(1);
            
            // Combine environment props
            var environmentPrefabs = new List<GameObject>();
            if (treePrefabs != null) environmentPrefabs.AddRange(treePrefabs);
            if (rockPrefabs != null) environmentPrefabs.AddRange(rockPrefabs);
            if (bushPrefabs != null) environmentPrefabs.AddRange(bushPrefabs);
            
            var grassPlaced = 0;
            var environmentsPlaced = 0;
            
            // Calculate center position for castle exclusion
            var centerX = width / 2f;
            var centerY = height / 2f;
            const float castleExclusionRadius = 3f; // Don't place props within 3 chunks of center (castle area)
            
            // Process each chunk
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var chunk = chunks[x, y];
                    
                    // Only process Buildable chunks
                    // Skip Path and Blocked chunks
                    if (chunk.chunkType != ChunkType.Buildable)
                        continue;
                    
                    // Skip castle area (center of map)
                    var distanceFromCenter = Mathf.Sqrt(
                        Mathf.Pow(chunk.gridX - centerX, 2) + 
                        Mathf.Pow(chunk.gridY - centerY, 2)
                    );
                    
                    if (distanceFromCenter < castleExclusionRadius)
                        continue; // Skip chunks near castle
                    
                    // Calculate distance to nearest path chunk
                    var distanceToPath = GetDistanceToNearestPath(chunk, pathChunks);
                    
                    // ALWAYS place grass on Buildable chunks
                    if (grassPrefabs is { Length: > 0 })
                    {
                        var grassCount = Random.Range(2, 5); // 2-4 grass clumps per chunk
                        for (int i = 0; i < grassCount; i++)
                        {
                            var randomGrass = grassPrefabs[Random.Range(0, grassPrefabs.Length)];
                            PlaceGrass(chunk, randomGrass, terrain);
                            grassPlaced++;
                        }
                    }
                    
                    // Decide if this chunk should be empty or have a mini environment
                    var shouldBeEmpty = ShouldBeEmptyChunk(distanceToPath);
                    
                    if (!shouldBeEmpty && environmentPrefabs.Count > 0)
                    {
                        // Place mini environment (trees, rocks, bushes)
                        PlaceMiniEnvironment(chunk, environmentPrefabs, terrain);
                        environmentsPlaced++;
                    }
                }
            }
            
            Debug.Log($"✓ Placed {grassPlaced} grass clumps across {environmentsPlaced} mini environments");
        }
        
        /// <summary>
        /// Calculate distance to nearest path chunk
        /// </summary>
        private float GetDistanceToNearestPath(ChunkNode chunk, List<ChunkNode> pathChunks)
        {
            var minDistance = float.MaxValue;
            
            foreach (var pathChunk in pathChunks)
            {
                var dx = chunk.gridX - pathChunk.gridX;
                var dy = chunk.gridY - pathChunk.gridY;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (distance < minDistance)
                    minDistance = distance;
            }
            
            return minDistance;
        }
        
        /// <summary>
        /// Determine if chunk should be empty (for building) based on path distance
        /// Closer to path = more likely to be empty
        /// Far from path = less likely to be empty
        /// </summary>
        private bool ShouldBeEmptyChunk(float distanceToPath)
        {
            // Distance ranges:
            // 0-3 chunks: 70% chance empty (close to path, good for building)
            // 3-6 chunks: 40% chance empty
            // 6-10 chunks: 15% chance empty
            // 10+ chunks: 5% chance empty (far from path, mostly decorated)
            
            float emptyChance;
            
            if (distanceToPath <= 3f)
                emptyChance = 0.7f;
            else if (distanceToPath <= 6f)
                emptyChance = 0.4f;
            else if (distanceToPath <= 10f)
                emptyChance = 0.15f;
            else
                emptyChance = 0.05f;
            
            return Random.value < emptyChance;
        }
        
        /// <summary>
        /// Place grass on chunk (always happens on Buildable chunks)
        /// </summary>
        private void PlaceGrass(ChunkNode chunk, GameObject grassPrefab, Terrain terrain)
        {
            if (!grassPrefab) return;
            
            var position = chunk.center;
            
            // Random position within chunk
            var offsetX = Random.Range(-1.5f, 1.5f);
            var offsetZ = Random.Range(-1.5f, 1.5f);
            position += new Vector3(offsetX, 0, offsetZ);
            
            // Sample terrain height
            if (terrain != null)
            {
                position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            }
            else
            {
                position.y = chunk.yOffset;
            }
            
            var rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            var grass = Object.Instantiate(grassPrefab, position, rotation, _propParent);
            grass.name = $"Grass_{chunk.gridX}_{chunk.gridY}";
            
            var scale = Random.Range(0.8f, 1.2f);
            grass.transform.localScale = Vector3.one * scale;
            
            // Grass doesn't block building
        }
        
        /// <summary>
        /// Place mini environment (trees, rocks, bushes) on chunk
        /// </summary>
        private void PlaceMiniEnvironment(ChunkNode chunk, List<GameObject> environmentPrefabs, Terrain terrain)
        {
            // Randomly select 2-4 props for this mini environment
            var propCount = Random.Range(2, 5);
            
            // Track placed positions to avoid overlaps
            var placedPositions = new List<Vector3>();
            const float minDistanceBetweenProps = 2.0f; // Minimum 2 units between props
            
            var attempts = 0;
            const int maxAttempts = 20; // Try max 20 times per chunk
            
            for (int i = 0; i < propCount && attempts < maxAttempts; i++)
            {
                var randomPrefab = environmentPrefabs[Random.Range(0, environmentPrefabs.Count)];
                
                if (!randomPrefab)
                {
                    attempts++;
                    continue;
                }
                
                // Try to find a valid position
                Vector3 position = Vector3.zero;
                bool validPositionFound = false;
                
                for (int attempt = 0; attempt < 10; attempt++) // 10 attempts per prop
                {
                    position = chunk.center;
                    
                    // Random position within chunk
                    var offsetX = Random.Range(-1.5f, 1.5f);
                    var offsetZ = Random.Range(-1.5f, 1.5f);
                    position += new Vector3(offsetX, 0, offsetZ);
                    
                    // Check distance to all previously placed props
                    validPositionFound = true;
                    foreach (var placedPos in placedPositions)
                    {
                        var distance = Vector3.Distance(
                            new Vector3(position.x, 0, position.z),
                            new Vector3(placedPos.x, 0, placedPos.z)
                        );
                        
                        if (distance < minDistanceBetweenProps)
                        {
                            validPositionFound = false;
                            break;
                        }
                    }
                    
                    if (validPositionFound)
                        break;
                }
                
                // If we couldn't find a valid position after 10 tries, skip this prop
                if (!validPositionFound)
                {
                    attempts++;
                    i--; // Don't count this as a placed prop
                    continue;
                }
                
                // Sample terrain height
                if (terrain != null)
                {
                    position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
                }
                else
                {
                    position.y = chunk.yOffset;
                }
                
                var rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                var prop = Object.Instantiate(randomPrefab, position, rotation, _propParent);
                prop.name = $"Prop_{chunk.gridX}_{chunk.gridY}_{i}";
                
                var scale = Random.Range(0.7f, 1.3f);
                prop.transform.localScale = Vector3.one * scale;
                
                // Add this position to our tracking list
                placedPositions.Add(position);
                attempts++;
            }
            
            // Mark chunk as blocked (has environment, can't build)
            chunk.chunkType = ChunkType.Blocked;
            chunk.isBuildable = false;
            chunk.vertexColor = new Color(0.4f, 0.6f, 0.4f); // Greenish for decorated
        }
        
        /// <summary>
        /// Add random blockers to create interesting layouts (legacy method)
        /// </summary>
        public static void AddRandomBlockers(ChunkNode[,] chunks, List<ChunkNode> pathChunks, int seed, float blockerChance = 0.05f)
        {
            Random.InitState(seed);
            
            var width = chunks.GetLength(0);
            var height = chunks.GetLength(1);
            var pathChunkSet = new HashSet<ChunkNode>(pathChunks);
            
            var blockersAdded = 0;
            
            for (var y = 1; y < height - 1; y++) // Skip edges
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var chunk = chunks[x, y];
                    
                    // Don't block paths
                    if (pathChunkSet.Contains(chunk))
                        continue;
                    
                    // Random chance
                    if (Random.value > blockerChance)
                        continue;
                    
                    // Block the chunk
                    chunk.chunkType = ChunkType.Blocked;
                    chunk.isBuildable = false;
                    chunk.vertexColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray
                    blockersAdded++;
                }
            }
            
            Debug.Log($"✓ Added {blockersAdded} random blockers");
        }
    }
}