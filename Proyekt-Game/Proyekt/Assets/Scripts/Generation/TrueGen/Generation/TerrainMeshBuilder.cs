using System.Collections.Generic;
using System.Linq;
using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class TerrainMeshBuilder
    {
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<Color> _colors = new();
        
        /// <summary>
        /// Builds a single mesh from all chunks
        /// </summary>
        public Mesh BuildCombinedMesh(ChunkNode[,] chunks, bool addPathSides = true)
        {
            Clear();
            
            var width = chunks.GetLength(0);
            var height = chunks.GetLength(1);
            
            // Add all chunk quads
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var chunk = chunks[x, y];
                    AddChunkQuad(chunk);
                    
                    // Add side faces for path edges
                    if (addPathSides && chunk.chunkType == ChunkType.Path)
                    {
                        AddPathSideFaces(chunk);  // Removed unused 'chunks' parameter
                    }
                }
            }
            
            return CreateMesh();
        }
        
        private void AddChunkQuad(ChunkNode chunk)
        {
            var startIndex = _vertices.Count;
            
            // Add 4 corners with Y offset applied
            for (var i = 0; i < 4; i++)
            {
                var pos = chunk.worldCorners[i];
                pos.y += chunk.yOffset;
                _vertices.Add(pos);
                
                // UVs for texture tiling (based on world position)
                _uvs.Add(new Vector2(pos.x * 0.1f, pos.z * 0.1f));
                
                // Vertex color
                _colors.Add(chunk.vertexColor);
            }
            
            // Two triangles forming the quad
            // Triangle 1: 0, 2, 1
            _triangles.Add(startIndex + 0);
            _triangles.Add(startIndex + 2);
            _triangles.Add(startIndex + 1);
            
            // Triangle 2: 0, 3, 2
            _triangles.Add(startIndex + 0);
            _triangles.Add(startIndex + 3);
            _triangles.Add(startIndex + 2);
        }
        
        private void AddPathSideFaces(ChunkNode pathChunk)
        {
            // For each neighbor, check if it's NOT a path
            // If so, add a vertical wall between them
            foreach (var sharedEdge in from neighbor in pathChunk.Neighbors 
                     where neighbor.chunkType != ChunkType.Path 
                     select FindSharedEdge(pathChunk, neighbor) into sharedEdge 
                     where sharedEdge != null 
                     select sharedEdge)
            {
                AddVerticalQuad(
                    sharedEdge[0], 
                    sharedEdge[1], 
                    pathChunk.yOffset, 
                    0f, 
                    new Color(0.3f, 0.25f, 0.2f) // Darker brown for walls
                );
            }
        }
        
        private static Vector3[] FindSharedEdge(ChunkNode chunk1, ChunkNode chunk2)
        {
            // Check all edges of chunk1 against edges of chunk2
            for (var i = 0; i < 4; i++)
            {
                var c1A = chunk1.worldCorners[i];
                var c1B = chunk1.worldCorners[(i + 1) % 4];
                
                for (var j = 0; j < 4; j++)
                {
                    var c2A = chunk2.worldCorners[j];
                    var c2B = chunk2.worldCorners[(j + 1) % 4];
                    
                    // Check if edges match (allowing for reversed direction)
                    if ((Vector3.Distance(c1A, c2A) < 0.01f && Vector3.Distance(c1B, c2B) < 0.01f) ||
                        (Vector3.Distance(c1A, c2B) < 0.01f && Vector3.Distance(c1B, c2A) < 0.01f))
                    {
                        return new[] { c1A, c1B };
                    }
                }
            }
            
            return null;
        }
        
        private void AddVerticalQuad(Vector3 bottom1, Vector3 bottom2, float yBottom, float yTop, Color color)
        {
            var startIndex = _vertices.Count;
            
            var top1 = bottom1;
            top1.y = yTop;
            var top2 = bottom2;
            top2.y = yTop;
            
            bottom1.y = yBottom;
            bottom2.y = yBottom;
            
            _vertices.Add(bottom1);
            _vertices.Add(bottom2);
            _vertices.Add(top2);
            _vertices.Add(top1);
            
            _uvs.Add(Vector2.zero);
            _uvs.Add(Vector2.right);
            _uvs.Add(Vector2.one);
            _uvs.Add(Vector2.up);
            
            for (var i = 0; i < 4; i++)
                _colors.Add(color);
            
            // Two triangles
            _triangles.Add(startIndex + 0);
            _triangles.Add(startIndex + 2);
            _triangles.Add(startIndex + 1);
            
            _triangles.Add(startIndex + 0);
            _triangles.Add(startIndex + 3);
            _triangles.Add(startIndex + 2);
        }
        
        private Mesh CreateMesh()
        {
            var mesh = new Mesh
            {
                name = "ChunkTerrain",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 // Support large meshes
            };

            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.SetUVs(0, _uvs);
            mesh.SetColors(_colors);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private void Clear()
        {
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _colors.Clear();
        }
    }
}