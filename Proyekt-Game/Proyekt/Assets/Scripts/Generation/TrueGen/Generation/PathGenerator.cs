using System.Collections.Generic;
using System.Linq;
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
        /// SIMPLE spiral path from castle gate to map edge
        /// Just smooth curves, no loops, no complexity
        /// </summary>
        public List<ChunkNode> GenerateSpiralPath(ChunkNode[,] chunks, int width, int height, 
            float spiralTightness = 0.6f)
        {
            Random.InitState(_seed);
            
            var centerX = width / 2f;
            var centerY = height / 2f;
            
            var path = new List<ChunkNode>();
            var pathSet = new HashSet<ChunkNode>();
            
            // GATE POSITION: Exact position of castle gate opening (south side)
            // Adjust this value to match your castle's actual gate position
            const float gateDistance = 2.5f; // Distance from center to gate opening
            var gateX = Mathf.RoundToInt(centerX);
            var gateY = Mathf.RoundToInt(centerY - gateDistance);
            gateX = Mathf.Clamp(gateX, 0, width - 1);
            gateY = Mathf.Clamp(gateY, 0, height - 1);
            var gateChunk = chunks[gateX, gateY];
            
            Debug.Log($"Gate position: ({gateX}, {gateY}) - Center: ({centerX}, {centerY})");
            
            // START spiral from just beyond the gate
            var currentRadius = gateDistance + 0.5f; // Start just past gate
            var currentAngle = -Mathf.PI / 2f; // Start pointing south (gate direction)
            
            // END: When we reach map edge
            var maxRadius = Mathf.Min(width, height) / 2f - 1f;
            
            // Spiral parameters
            var angleStep = 0.15f; // How fast we rotate (smaller = tighter spiral)
            var radiusStep = 0.08f; // How fast we move outward (smaller = more windy)
            
            // Build spiral
            while (currentRadius < maxRadius)
            {
                // Calculate position
                var x = centerX + Mathf.Cos(currentAngle) * currentRadius;
                var y = centerY + Mathf.Sin(currentAngle) * currentRadius;
                
                // Clamp to grid
                x = Mathf.Clamp(x, 0, width - 1);
                y = Mathf.Clamp(y, 0, height - 1);
                
                var chunkX = Mathf.RoundToInt(x);
                var chunkY = Mathf.RoundToInt(y);
                var chunk = chunks[chunkX, chunkY];
                
                // Add if not already in path
                if (!pathSet.Contains(chunk))
                {
                    path.Add(chunk);
                    pathSet.Add(chunk);
                }
                
                // Move along spiral
                currentAngle += angleStep;
                currentRadius += radiusStep;
            }
            
            // Reverse so enemies spawn at edge and walk to gate
            path.Reverse();
            
            // ADD GATE CHUNK at the end (where enemies finish)
            if (!pathSet.Contains(gateChunk))
            {
                path.Add(gateChunk);
            }
            
            Debug.Log($"✓ Generated simple spiral path with {path.Count} chunks");
            Debug.Log($"   Path ends at gate: ({gateX}, {gateY})");
            Debug.Log($"   Enemies spawn at edge, walk to gate");
            
            return path;
        }
        
        public List<ChunkNode> GeneratePath(ChunkNode start, ChunkNode end, float randomnessFactor = 0.3f)
        {
            Random.InitState(_seed);
            var path = AStar(start, end, randomnessFactor);
            if (path != null && path.Count != 0) return path;
            Debug.LogError("Failed to generate path!");
            return new List<ChunkNode>();
        }
        
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
        
        public static void MarkCastleArea(ChunkNode[,] chunks, int width, int height, int castleSize = 3)
        {
            var startX = (width - castleSize) / 2;
            var startY = (height - castleSize) / 2;
            
            for (var y = 0; y < castleSize; y++)
            {
                for (var x = 0; x < castleSize; x++)
                {
                    var chunkX = startX + x;
                    var chunkY = startY + y;

                    if (chunkX < 0 || chunkX >= width || chunkY < 0 || chunkY >= height) continue;
                    var chunk = chunks[chunkX, chunkY];
                    chunk.chunkType = ChunkType.Decorative;
                    chunk.isBuildable = false;
                    chunk.vertexColor = new Color(0.6f, 0.5f, 0.7f);
                    chunk.TextureIndex = 3;
                }
            }
        }
        
        private static List<ChunkNode> AStar(ChunkNode start, ChunkNode end, float randomnessFactor)
        {
            var openSet = new List<ChunkNode>();
            var closedSet = new HashSet<ChunkNode>();
            
            start.GCost = 0;
            start.HCost = Vector3.Distance(start.center, end.center);
            start.Parent = null;
            
            openSet.Add(start);
            
            while (openSet.Count > 0)
            {
                var current = openSet[0];
                
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
                
                if (current == end)
                {
                    return RetracePath(start, end);
                }
                
                foreach (var neighbor in current.Neighbors)
                {
                    if (closedSet.Contains(neighbor))
                        continue;
                    
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
            
            return null;
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