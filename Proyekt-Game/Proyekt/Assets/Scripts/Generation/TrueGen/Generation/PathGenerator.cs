using System.Collections.Generic;
using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class PathGenerator
    {
        private readonly int _seed;
        
        public PathGenerator(int seed)
        {
            _seed = seed;
        }
        
        /// <summary>
        /// Generates a path from start to end using A* with randomization
        /// </summary>
        public List<ChunkNode> GeneratePath(ChunkNode start, ChunkNode end, float randomnessFactor = 0.3f)
        {
            Random.InitState(_seed);
            
            var path = AStar(start, end, randomnessFactor);

            if (path != null && path.Count != 0) return path;
            Debug.LogError("Failed to generate path!");
            return new List<ChunkNode>();

        }
        
        /// <summary>
        /// Applies path properties to chunks
        /// </summary>
        public static void ApplyPathToChunks(List<ChunkNode> path, float pathDepth)
        {
            foreach (var chunk in path)
            {
                chunk.chunkType = ChunkType.Path;
                chunk.isBuildable = false;
                chunk.yOffset = -pathDepth;
                chunk.vertexColor = new Color(0.4f, 0.3f, 0.2f);
                chunk.TextureIndex = 1;
            }
        }
        
        private static List<ChunkNode> AStar(ChunkNode start, ChunkNode end, float randomnessFactor)
        {
            var openSet = new List<ChunkNode>();
            var closedSet = new HashSet<ChunkNode>();
            
            openSet.Add(start);
            
            while (openSet.Count > 0)
            {
                var current = openSet[0];
                
                // Find node with lowest fCost
                for (var i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < current.FCost || 
                        (Mathf.Approximately(openSet[i].FCost, current.FCost) && openSet[i].HCost < current.HCost))
                    {
                        current = openSet[i];
                    }
                }
                
                openSet.Remove(current);
                closedSet.Add(current);
                
                // Found the path
                if (current == end)
                {
                    return RetracePath(start, end);
                }
                
                // Check neighbors
                foreach (var neighbor in current.Neighbors)
                {
                    if (closedSet.Contains(neighbor))
                        continue;
                    
                    // Calculate cost with randomization for organic paths
                    var baseCost = Vector3.Distance(current.center, neighbor.center);
                    var randomFactor = Random.Range(1f - randomnessFactor, 1f + randomnessFactor);
                    var newCostToNeighbor = current.GCost + (baseCost * randomFactor);

                    if (!(newCostToNeighbor < neighbor.GCost) && openSet.Contains(neighbor)) continue;
                    neighbor.GCost = newCostToNeighbor;
                    neighbor.HCost = Vector3.Distance(neighbor.center, end.center);
                    neighbor.Parent = current;
                        
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
            
            return null; // No path found, return null
        }
        
        private static List<ChunkNode> RetracePath(ChunkNode start, ChunkNode end)
        {
            var path = new List<ChunkNode>();
            var current = end;
            
            while (current != start)
            {
                path.Add(current);
                current = current.Parent;
            }
            
            path.Add(start);
            path.Reverse();
            
            return path;
        }
    }
}