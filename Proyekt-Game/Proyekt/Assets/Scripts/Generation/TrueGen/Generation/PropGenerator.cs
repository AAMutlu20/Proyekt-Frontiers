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
        /// Generate props across the terrain
        /// </summary>
        public void GenerateProps(ChunkNode[,] chunks, List<ChunkNode> pathChunks, PropDefinition[] propDefinitions)
        {
            Random.InitState(_seed);
            
            if (propDefinitions == null || propDefinitions.Length == 0)
            {
                Debug.LogWarning("No prop definitions provided");
                return;
            }
            
            var width = chunks.GetLength(0);
            var height = chunks.GetLength(1);
            var pathChunkSet = new HashSet<ChunkNode>(pathChunks);
            
            var propsPlaced = 0;
            
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var chunk = chunks[x, y];
                    
                    // Skip path chunks unless prop allows it
                    var isPath = pathChunkSet.Contains(chunk);
                    
                    // Try to place a prop
                    foreach (var propDef in propDefinitions)
                    {
                        if (isPath && !propDef.canSpawnOnPath)
                            continue;
                        
                        if (!propDef)
                            continue;
                        
                        // Edge preference check
                        if (propDef.preferEdges && !IsEdgeChunk(x, y, width, height))
                        {
                            if (Random.value > 0.3f) // 70% less likely if not on edge
                                continue;
                        }
                        
                        // Spawn chance
                        if (Random.value > propDef.spawnChance)
                            continue;
                        
                        // Place the prop
                        PlaceProp(chunk, propDef);
                        propsPlaced++;
                        
                        // Only one prop per chunk
                        break;
                    }
                }
            }
            
            Debug.Log($"✓ Placed {propsPlaced} props across terrain");
        }
        
        private void PlaceProp(ChunkNode chunk, PropDefinition propDef)
        {
            if (!propDef.prefab)
            {
                Debug.LogWarning($"Prop {propDef.name} has no prefab assigned");
                return;
            }
            
            // Calculate position (center with slight offset)
            var position = chunk.center;
            position.y = chunk.yOffset;
            
            // Add small random offset within chunk
            var offsetX = Random.Range(-1f, 1f);
            var offsetZ = Random.Range(-1f, 1f);
            position += new Vector3(offsetX, 0, offsetZ);
            
            // Random rotation
            var rotation = Quaternion.identity;
            if (propDef.randomRotation)
            {
                rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }
            
            // Instantiate prop
            var prop = Object.Instantiate(propDef.prefab, position, rotation, _propParent);
            prop.name = $"{propDef.propType}_{chunk.gridX}_{chunk.gridY}";
            
            // Random scale
            var scale = Random.Range(propDef.scaleRange.x, propDef.scaleRange.y);
            prop.transform.localScale = Vector3.one * scale;
            
            // Mark chunk as blocked if needed
            if (propDef.blocksBuilding)
            {
                chunk.chunkType = ChunkType.Blocked;
                chunk.isBuildable = false;
                chunk.vertexColor = new Color(0.4f, 0.4f, 0.35f);
                chunk.TextureIndex = 2;
            }
            else
            {
                chunk.chunkType = ChunkType.Decorative;
                chunk.TextureIndex = 3;
            }
        }
        
        private static bool IsEdgeChunk(int x, int y, int width, int height)
        {
            return x == 0 || x == width - 1 || y == 0 || y == height - 1;
        }
        
        /// <summary>
        /// Add random blockers to create interesting layouts
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