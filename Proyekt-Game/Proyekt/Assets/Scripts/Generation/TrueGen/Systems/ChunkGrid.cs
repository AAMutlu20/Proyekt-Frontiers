using System.Collections.Generic;
using System.Linq;
using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Systems
{
    public class ChunkGrid : MonoBehaviour
    {
        public ChunkNode[,] Chunks { get; private set; }
        public List<ChunkNode> PathChunks { get; private set; }

        private int Width { get; set; }
        private int Height { get; set; }
        
        public void Initialize(ChunkNode[,] chunkGrid, List<ChunkNode> path)
        {
            Chunks = chunkGrid;
            PathChunks = path;
            Width = chunkGrid.GetLength(0);
            Height = chunkGrid.GetLength(1);
        }
        
        /// <summary>
        /// Gets the chunk at a world position (with raycast hit point)
        /// </summary>
        public ChunkNode GetChunkAtWorldPosition(Vector3 worldPos)
        {
            // ADD NULL CHECK
            if (Chunks == null)
            {
                return null;
            }
            
            return Chunks.Cast<ChunkNode>().FirstOrDefault(chunk => IsPointInQuad(worldPos, chunk.worldCorners));
        }
        
        /// <summary>
        /// Gets chunk by grid coordinates
        /// </summary>
        private ChunkNode GetChunk(int x, int y)
        {
            // ADD NULL CHECK
            if (Chunks == null || x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            
            return Chunks[x, y];
        }
        
        /// <summary>
        /// Gets all chunks in a rectangular footprint
        /// </summary>
        public List<ChunkNode> GetChunksInFootprint(int originX, int originY, int footprintWidth, int footprintHeight)
        {
            var result = new List<ChunkNode>();
            
            // ADD NULL CHECK
            if (Chunks == null)
                return result;
            
            for (var y = 0; y < footprintHeight; y++)
            {
                for (var x = 0; x < footprintWidth; x++)
                {
                    var chunk = GetChunk(originX + x, originY + y);
                    if (chunk != null)
                        result.Add(chunk);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Check if all chunks in footprint are buildable
        /// </summary>
        public bool CanBuildAt(int originX, int originY, int footprintWidth, int footprintHeight)
        {
            // ADD NULL CHECK
            if (Chunks == null)
                return false;
            
            var chunksInFp = GetChunksInFootprint(originX, originY, footprintWidth, footprintHeight);
            
            return chunksInFp.Count == footprintWidth * footprintHeight &&
                   chunksInFp.All(chunk => chunk.isBuildable && !chunk.isOccupied);
        }
        
        /// <summary>
        /// Point-in-quad test (2D, ignoring Y)
        /// </summary>
        private static bool IsPointInQuad(Vector3 point, Vector3[] quad)
        {
            // Use cross product method to check if point is on same side of all edges
            var p = new Vector2(point.x, point.z);
            
            for (var i = 0; i < 4; i++)
            {
                var a = new Vector2(quad[i].x, quad[i].z);
                var b = new Vector2(quad[(i + 1) % 4].x, quad[(i + 1) % 4].z);
                
                var edge = b - a;
                var toPoint = p - a;
                
                var cross = edge.x * toPoint.y - edge.y * toPoint.x;
                
                if (cross < 0)
                    return false;
            }
            
            return true;
        }
    }
}