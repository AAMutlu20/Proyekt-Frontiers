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
        /// Generates a single-width spiral path from edge to center
        /// ENSURES path never places chunks adjacent to existing path chunks
        /// </summary>
        public List<ChunkNode> GenerateSpiralPath(ChunkNode[,] chunks, int width, int height, 
            float spiralTightness = 0.6f)
        {
            Random.InitState(_seed);
            
            var path = new List<ChunkNode>();
            var pathSet = new HashSet<ChunkNode>(); // For fast lookup
            
            // Step 1: Calculate center
            var centerX = width / 2f;
            var centerY = height / 2f;
            
            // Step 2: Pick random starting edge
            var startChunk = GetRandomEdgeChunk(chunks, width, height);
            var currentChunk = startChunk;
            
            path.Add(currentChunk);
            pathSet.Add(currentChunk);
            
            // Step 3: Calculate starting parameters
            var currentAngle = Mathf.Atan2(startChunk.gridY - centerY, startChunk.gridX - centerX);
            var startRadius = Vector2.Distance(
                new Vector2(startChunk.gridX, startChunk.gridY),
                new Vector2(centerX, centerY)
            );
            
            var currentRadius = startRadius;
            const float targetRadius = 1.5f;
            
            // Step 4: Spiral parameters
            var spiralLoops = Random.Range(2.5f, 4f);
            const float angleStep = 0.2f; // Angle increment per step
            var radiusDecrement = (startRadius - targetRadius) / (spiralLoops * Mathf.PI * 2f / angleStep);
            
            var spiralDirection = Random.value > 0.5f ? 1f : -1f; // Clockwise or counter-clockwise
            
            var stuckCounter = 0;
            const int maxStuckAttempts = 50;
            
            // Step 5: Build spiral path
            while (currentRadius > targetRadius && stuckCounter < maxStuckAttempts)
            {
                // Try to find next valid chunk
                var foundNext = false;
                var attempts = 0;
                const int maxAttempts = 8;
                
                while (!foundNext && attempts < maxAttempts)
                {
                    // Update angle
                    currentAngle += angleStep * spiralDirection;
                    
                    // Calculate next position
                    var jitterRadius = currentRadius + Random.Range(-0.3f, 0.3f);
                    var nextX = centerX + Mathf.Cos(currentAngle) * jitterRadius;
                    var nextY = centerY + Mathf.Sin(currentAngle) * jitterRadius;
                    
                    // Clamp to grid
                    nextX = Mathf.Clamp(nextX, 0, width - 1);
                    nextY = Mathf.Clamp(nextY, 0, height - 1);
                    
                    var nextChunk = chunks[Mathf.RoundToInt(nextX), Mathf.RoundToInt(nextY)];
                    
                    // Check if this chunk is valid
                    if (IsValidNextChunk(nextChunk, currentChunk, pathSet))
                    {
                        path.Add(nextChunk);
                        pathSet.Add(nextChunk);
                        currentChunk = nextChunk;
                        foundNext = true;
                        stuckCounter = 0; // Reset stuck counter
                        
                        // Gradually decrease radius
                        currentRadius -= radiusDecrement;
                    }
                    else
                    {
                        // Try different angle
                        attempts++;
                        currentAngle += 0.3f * spiralDirection;
                    }
                }

                if (foundNext) continue;
                stuckCounter++;
                // Try to unstick by changing angle more dramatically
                currentAngle += Mathf.PI * 0.25f * spiralDirection;
            }
            
            // Step 6: Connect to center if we're close
            var centerChunk = chunks[Mathf.RoundToInt(centerX), Mathf.RoundToInt(centerY)];
            if (!pathSet.Contains(centerChunk))
            {
                var finalPath = ConnectToCenter(currentChunk, centerChunk, chunks, pathSet);
                path.AddRange(finalPath);
            }
            
            Debug.Log($"✓ Generated single-width spiral path with {path.Count} chunks");
            return path;
        }
        
        /// <summary>
        /// Check if a chunk is a valid next step in the path
        /// - Must be adjacent to current chunk
        /// - Must not be already in path
        /// - Must not be adjacent to any path chunk (except current)
        /// </summary>
        private static bool IsValidNextChunk(ChunkNode candidate, ChunkNode current, HashSet<ChunkNode> pathSet)
        {
            // Already in path?
            if (pathSet.Contains(candidate))
                return false;

            // Adjacent to current?
            return IsAdjacent(current, candidate) &&
                   // CRITICAL CHECK: Is this chunk adjacent to any other path chunk?
                   // This prevents the path from widening
                   candidate.Neighbors.Where(neighbor => neighbor != current).All(neighbor => !pathSet.Contains(neighbor));
        }
        
        /// <summary>
        /// Check if two chunks are adjacent (share an edge)
        /// </summary>
        private static bool IsAdjacent(ChunkNode chunk1, ChunkNode chunk2)
        {
            var dx = Mathf.Abs(chunk1.gridX - chunk2.gridX);
            var dy = Mathf.Abs(chunk1.gridY - chunk2.gridY);
            
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }
        
        /// <summary>
        /// Connect current position to center chunk with single-width path
        /// </summary>
        private List<ChunkNode> ConnectToCenter(ChunkNode from, ChunkNode to, ChunkNode[,] chunks, HashSet<ChunkNode> pathSet)
        {
            var connection = new List<ChunkNode>();
            var current = from;
            var attempts = 0;
            const int maxAttempts = 100;
            
            while (current != to && attempts < maxAttempts)
            {
                attempts++;
                
                var dx = to.gridX - current.gridX;
                var dy = to.gridY - current.gridY;
                
                // Try to move toward target
                var potentialMoves = new List<ChunkNode>();
                
                // Horizontal movement
                if (dx != 0)
                {
                    var targetX = current.gridX + (dx > 0 ? 1 : -1);
                    if (targetX >= 0 && targetX < chunks.GetLength(0))
                        potentialMoves.Add(chunks[targetX, current.gridY]);
                }
                
                // Vertical movement
                if (dy != 0)
                {
                    var targetY = current.gridY + (dy > 0 ? 1 : -1);
                    if (targetY >= 0 && targetY < chunks.GetLength(1))
                        potentialMoves.Add(chunks[current.gridX, targetY]);
                }
                
                // Find valid move
                ChunkNode nextChunk = potentialMoves.FirstOrDefault(move => IsValidNextChunk(move, current, pathSet));

                if (nextChunk != null)
                {
                    connection.Add(nextChunk);
                    pathSet.Add(nextChunk);
                    current = nextChunk;
                }
                else
                {
                    break; // Can't continue
                }
            }
            
            return connection;
        }
        
        /// <summary>
        /// Get a random chunk on the edge of the grid (middle sections only)
        /// </summary>
        private static ChunkNode GetRandomEdgeChunk(ChunkNode[,] chunks, int width, int height)
        {
            var edge = Random.Range(0, 4); // 0=left, 1=right, 2=top, 3=bottom
            
            return edge switch
            {
                0 => chunks[0, Random.Range(height / 4, 3 * height / 4)],
                1 => chunks[width - 1, Random.Range(height / 4, 3 * height / 4)],
                2 => chunks[Random.Range(width / 4, 3 * width / 4), height - 1],
                _ => chunks[Random.Range(width / 4, 3 * width / 4), 0]
            };
        }
        
        /// <summary>
        /// Generates a path from start to end using A* with randomization (legacy method)
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
        
        /// <summary>
        /// Mark center chunks as castle area (special type)
        /// </summary>
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