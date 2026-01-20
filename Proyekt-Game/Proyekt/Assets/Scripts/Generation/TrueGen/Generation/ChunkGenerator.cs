using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class ChunkGenerator
    {
        private readonly int _seed;
        
        public ChunkGenerator(int seed)
        {
            _seed = seed;
        }
        
        /// <summary>
        /// Generates an irregular grid of chunks using distorted quad layout
        /// </summary>
        public ChunkNode[,] GenerateDistortedGrid(int width, int height, float chunkSize, float distortionAmount)
        {
            Random.InitState(_seed);
            var grid = new ChunkNode[width, height];
            
            // Step 1: Generate vertex positions with jitter
            var vertices = GenerateVertexGrid(width, height, chunkSize, distortionAmount);
            
            // Step 2: Create chunks from vertices
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    grid[x, y] = CreateChunkFromVertices(x, y, width, vertices);
                }
            }
            
            // Step 3: Build neighbor connections
            BuildNeighborGraph(grid, width, height);
            
            return grid;
        }
        
        private static Vector3[,] GenerateVertexGrid(int width, int height, float chunkSize, float distortionAmount)
        {
            var vertices = new Vector3[width + 1, height + 1];
            
            for (var y = 0; y <= height; y++)
            {
                for (var x = 0; x <= width; x++)
                {
                    // Base position on regular grid
                    var basePos = new Vector3(x * chunkSize, 0, y * chunkSize);
                    
                    // Add jitter to interior vertices only (keep edges aligned)
                    var isEdge = x == 0 || x == width || y == 0 || y == height;
                    
                    if (!isEdge)
                    {
                        var jitterX = Random.Range(-distortionAmount, distortionAmount);
                        var jitterZ = Random.Range(-distortionAmount, distortionAmount);
                        basePos += new Vector3(jitterX, 0, jitterZ);
                    }
                    
                    vertices[x, y] = basePos;
                }
            }
            
            return vertices;
        }
        
        private static ChunkNode CreateChunkFromVertices(int x, int y, int width, Vector3[,] vertices)
        {
            var chunk = new ChunkNode
            {
                ID = y * width + x,
                gridX = x,
                gridY = y,
                // ReSharper disable once RedundantExplicitArraySize
                worldCorners = new Vector3[4] // May be redundant to specify array size, but I still want to specify it
                {
                    vertices[x, y],         // 0: bottom-left
                    vertices[x + 1, y],     // 1: bottom-right
                    vertices[x + 1, y + 1], // 2: top-right
                    vertices[x, y + 1]      // 3: top-left
                },
                chunkType = ChunkType.Buildable
            };
            
            // Calculate center
            chunk.center = CalculateQuadCenter(chunk.worldCorners);
            
            // Default color (can be changed later)
            chunk.vertexColor = new Color(0.5f, 0.7f, 0.3f); // Grass is green
            
            return chunk;
        }
        
        private static Vector3 CalculateQuadCenter(Vector3[] corners)
        {
            return (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
        }
        
        private static void BuildNeighborGraph(ChunkNode[,] grid, int width, int height)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var chunk = grid[x, y];
                    chunk.Neighbors.Clear();
                    
                    // Add 4 cardinal neighbors (no diagonals because I don't wanna)
                    if (x > 0) 
                        chunk.Neighbors.Add(grid[x - 1, y]); // Left
                    if (x < width - 1) 
                        chunk.Neighbors.Add(grid[x + 1, y]); // Right
                    if (y > 0) 
                        chunk.Neighbors.Add(grid[x, y - 1]); // Down
                    if (y < height - 1) 
                        chunk.Neighbors.Add(grid[x, y + 1]); // Up
                }
            }
        }
    }
}