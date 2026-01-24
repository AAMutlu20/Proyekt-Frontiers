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
        private readonly List<Vector4> _uv2 = new(); // NEW - for texture index and rotation
        
        /// <summary>
        /// Builds a single mesh from all chunks with texture data
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
                        AddPathSideFaces(chunk);
                    }
                }
            }
            
            return CreateMesh();
        }
        
        private void AddChunkQuad(ChunkNode chunk)
        {
            var startIndex = _vertices.Count;
            
            // Calculate rotation matrix for UVs
            var rotationRadians = chunk.TextureIndex * Mathf.Deg2Rad;
            var cosRot = Mathf.Cos(rotationRadians);
            var sinRot = Mathf.Sin(rotationRadians);
            
            // Define base UVs for a quad (0,0) to (1,1)
            Vector2[] baseUVs = {
                new Vector2(0, 0),  // Bottom-left
                new Vector2(1, 0),  // Bottom-right
                new Vector2(1, 1),  // Top-right
                new Vector2(0, 1)   // Top-left
            };
            
            // Add 4 corners with Y offset applied
            for (var i = 0; i < 4; i++)
            {
                var pos = chunk.worldCorners[i];
                pos.y += chunk.yOffset;
                _vertices.Add(pos);
                
                // Rotate UVs around center (0.5, 0.5)
                var uv = baseUVs[i];
                var centered = uv - new Vector2(0.5f, 0.5f);
                var rotated = new Vector2(
                    centered.x * cosRot - centered.y * sinRot,
                    centered.x * sinRot + centered.y * cosRot
                );
                var finalUV = rotated + new Vector2(0.5f, 0.5f);
                
                _uvs.Add(finalUV);
                
                // Store texture index in UV2.x and extra data in other channels
                _uv2.Add(new Vector4(
                    chunk.TextureIndex,  // x: texture index (0-3)
                    0,                   // y: unused
                    0,                   // z: unused  
                    0                 // w: unused
                ));
                
                // Vertex color (can still be used for tinting)
                _colors.Add(chunk.vertexColor);
            }
            
            // Two triangles forming the quad
            _triangles.Add(startIndex + 0);
            _triangles.Add(startIndex + 2);
            _triangles.Add(startIndex + 1);
            
            _triangles.Add(startIndex + 0);
            _triangles.Add(startIndex + 3);
            _triangles.Add(startIndex + 2);
        }
        
        private void AddPathSideFaces(ChunkNode pathChunk)
        {
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
                    new Color(0.3f, 0.25f, 0.2f),
                    pathChunk.TextureIndex // Use path texture for walls
                );
            }
        }
        
        private static Vector3[] FindSharedEdge(ChunkNode chunk1, ChunkNode chunk2)
        {
            for (var i = 0; i < 4; i++)
            {
                var c1A = chunk1.worldCorners[i];
                var c1B = chunk1.worldCorners[(i + 1) % 4];
                
                for (var j = 0; j < 4; j++)
                {
                    var c2A = chunk2.worldCorners[j];
                    var c2B = chunk2.worldCorners[(j + 1) % 4];
                    
                    if ((Vector3.Distance(c1A, c2A) < 0.01f && Vector3.Distance(c1B, c2B) < 0.01f) ||
                        (Vector3.Distance(c1A, c2B) < 0.01f && Vector3.Distance(c1B, c2A) < 0.01f))
                    {
                        return new[] { c1A, c1B };
                    }
                }
            }
            
            return null;
        }
        
        private void AddVerticalQuad(Vector3 bottom1, Vector3 bottom2, float yBottom, float yTop, Color color, int textureIndex)
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
            
            // Add texture data for walls
            for (var i = 0; i < 4; i++)
            {
                _colors.Add(color);
                _uv2.Add(new Vector4(textureIndex, 0, 0, 0));
            }
            
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
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.SetUVs(0, _uvs);
            mesh.SetUVs(1, _uv2);
            mesh.SetColors(_colors);
            
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private void Clear()
        {
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _colors.Clear();
            _uv2.Clear();
        }
    }
}