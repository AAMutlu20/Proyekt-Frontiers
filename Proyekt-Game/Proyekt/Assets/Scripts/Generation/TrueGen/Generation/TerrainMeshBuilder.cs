using System.Collections.Generic;
using System.Linq;
using Generation.TrueGen.Core;
using Generation.TrueGen.Visuals;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class TerrainMeshBuilder
    {
        // Separate lists for each submesh
        private class SubmeshData
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<int> Triangles = new();
            public readonly List<Vector2> Uvs = new();
            public readonly List<Color> Colors = new();
            public readonly List<Vector3> Normals = new();
            
            public void Clear()
            {
                Vertices.Clear();
                Triangles.Clear();
                Uvs.Clear();
                Colors.Clear();
                Normals.Clear();
            }
        }
        
        private readonly SubmeshData _buildableSubmesh = new();
        private readonly SubmeshData _pathSubmesh = new();
        private readonly SubmeshData _blockedSubmesh = new();
        private readonly SubmeshData _decorativeSubmesh = new();
        private readonly SubmeshData _wallSubmesh = new();
        
        private TerrainMaterialSet _materialSet;
        
        /// <summary>
        /// Builds mesh with submeshes for different materials
        /// </summary>
        public Mesh BuildCombinedMesh(ChunkNode[,] chunks, TerrainMaterialSet materialSet, bool addPathSides = true)
        {
            Clear();
            _materialSet = materialSet;
            
            var width = chunks.GetLength(0);
            var height = chunks.GetLength(1);
            
            // Add all chunk quads to appropriate submeshes (INCLUDING path chunks!)
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var chunk = chunks[x, y];
                    
                    // Include ALL chunks - path chunks act as base layer
                    AddChunkQuad(chunk);
                }
            }
            
            return CreateMesh();
        }
        
        private void AddChunkQuad(ChunkNode chunk)
        {
            // Select the appropriate submesh based on chunk type
            var submesh = GetSubmeshForChunkType(chunk.chunkType);
            var tiling = _materialSet.GetTilingForChunkType(chunk.chunkType);
            
            var startIndex = submesh.Vertices.Count;
            
            // Calculate rotation matrix for UVs
            var rotationRadians = chunk.TextureRotation * Mathf.Deg2Rad;
            var cosRot = Mathf.Cos(rotationRadians);
            var sinRot = Mathf.Sin(rotationRadians);
            
            // Define base UVs for a quad
            Vector2[] baseUVs = {
                new Vector2(0, 0),  // Bottom-left
                new Vector2(1, 0),  // Bottom-right
                new Vector2(1, 1),  // Top-right
                new Vector2(0, 1)   // Top-left
            };
            
            // Add 4 corners
            for (var i = 0; i < 4; i++)
            {
                var pos = chunk.worldCorners[i];
                pos.y += chunk.yOffset;
                submesh.Vertices.Add(pos);
                
                // Rotate UVs around center (0.5, 0.5) and apply tiling
                var uv = baseUVs[i];
                var centered = uv - new Vector2(0.5f, 0.5f);
                var rotated = new Vector2(
                    centered.x * cosRot - centered.y * sinRot,
                    centered.x * sinRot + centered.y * cosRot
                );
                var finalUV = (rotated + new Vector2(0.5f, 0.5f)) * tiling;
                
                submesh.Uvs.Add(finalUV);
                submesh.Colors.Add(chunk.vertexColor);
                submesh.Normals.Add(Vector3.up); // We'll recalculate later
            }
            
            // Two triangles forming the quad
            submesh.Triangles.Add(startIndex + 0);
            submesh.Triangles.Add(startIndex + 2);
            submesh.Triangles.Add(startIndex + 1);
            
            submesh.Triangles.Add(startIndex + 0);
            submesh.Triangles.Add(startIndex + 3);
            submesh.Triangles.Add(startIndex + 2);
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
                    new Color(0.3f, 0.25f, 0.2f)
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
        
        private void AddVerticalQuad(Vector3 bottom1, Vector3 bottom2, float yBottom, float yTop, Color color)
        {
            var startIndex = _wallSubmesh.Vertices.Count;
            
            var top1 = bottom1;
            top1.y = yTop;
            var top2 = bottom2;
            top2.y = yTop;
            
            bottom1.y = yBottom;
            bottom2.y = yBottom;
            
            _wallSubmesh.Vertices.Add(bottom1);
            _wallSubmesh.Vertices.Add(bottom2);
            _wallSubmesh.Vertices.Add(top2);
            _wallSubmesh.Vertices.Add(top1);
            
            _wallSubmesh.Uvs.Add(Vector2.zero);
            _wallSubmesh.Uvs.Add(Vector2.right);
            _wallSubmesh.Uvs.Add(Vector2.one);
            _wallSubmesh.Uvs.Add(Vector2.up);
            
            for (var i = 0; i < 4; i++)
            {
                _wallSubmesh.Colors.Add(color);
                _wallSubmesh.Normals.Add(Vector3.forward); // Approximate, will recalculate
            }
            
            _wallSubmesh.Triangles.Add(startIndex + 0);
            _wallSubmesh.Triangles.Add(startIndex + 2);
            _wallSubmesh.Triangles.Add(startIndex + 1);
            
            _wallSubmesh.Triangles.Add(startIndex + 0);
            _wallSubmesh.Triangles.Add(startIndex + 3);
            _wallSubmesh.Triangles.Add(startIndex + 2);
        }
        
        private SubmeshData GetSubmeshForChunkType(ChunkType type)
        {
            return type switch
            {
                ChunkType.Path => _pathSubmesh,
                ChunkType.Blocked => _blockedSubmesh,
                ChunkType.Decorative => _decorativeSubmesh,
                _ => _buildableSubmesh
            };
        }
        
        private Mesh CreateMesh()
        {
            var mesh = new Mesh
            {
                name = "ChunkTerrain",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            
            // Combine all vertices from all submeshes
            var allVertices = new List<Vector3>();
            var allUVs = new List<Vector2>();
            var allColors = new List<Color>();
            
            allVertices.AddRange(_buildableSubmesh.Vertices);
            allVertices.AddRange(_pathSubmesh.Vertices);
            allVertices.AddRange(_blockedSubmesh.Vertices);
            allVertices.AddRange(_decorativeSubmesh.Vertices);
            allVertices.AddRange(_wallSubmesh.Vertices);
            
            allUVs.AddRange(_buildableSubmesh.Uvs);
            allUVs.AddRange(_pathSubmesh.Uvs);
            allUVs.AddRange(_blockedSubmesh.Uvs);
            allUVs.AddRange(_decorativeSubmesh.Uvs);
            allUVs.AddRange(_wallSubmesh.Uvs);
            
            allColors.AddRange(_buildableSubmesh.Colors);
            allColors.AddRange(_pathSubmesh.Colors);
            allColors.AddRange(_blockedSubmesh.Colors);
            allColors.AddRange(_decorativeSubmesh.Colors);
            allColors.AddRange(_wallSubmesh.Colors);
            
            mesh.SetVertices(allVertices);
            mesh.SetUVs(0, allUVs);
            mesh.SetColors(allColors);
            
            // Set up submeshes
            mesh.subMeshCount = 5;
            
            var vertexOffset = 0;
            
            // Submesh 0: Buildable
            if (_buildableSubmesh.Triangles.Count > 0)
            {
                mesh.SetTriangles(_buildableSubmesh.Triangles, 0);
                vertexOffset += _buildableSubmesh.Vertices.Count;
            }
            
            // Submesh 1: Path
            if (_pathSubmesh.Triangles.Count > 0)
            {
                var offsetTriangles = _pathSubmesh.Triangles.Select(t => t + vertexOffset).ToList();
                mesh.SetTriangles(offsetTriangles, 1);
                vertexOffset += _pathSubmesh.Vertices.Count;
            }
            
            // Submesh 2: Blocked
            if (_blockedSubmesh.Triangles.Count > 0)
            {
                var offsetTriangles = _blockedSubmesh.Triangles.Select(t => t + vertexOffset).ToList();
                mesh.SetTriangles(offsetTriangles, 2);
                vertexOffset += _blockedSubmesh.Vertices.Count;
            }
            
            // Submesh 3: Decorative
            if (_decorativeSubmesh.Triangles.Count > 0)
            {
                var offsetTriangles = _decorativeSubmesh.Triangles.Select(t => t + vertexOffset).ToList();
                mesh.SetTriangles(offsetTriangles, 3);
                vertexOffset += _decorativeSubmesh.Vertices.Count;
            }
            
            // Submesh 4: Walls
            if (_wallSubmesh.Triangles.Count > 0)
            {
                var offsetTriangles = _wallSubmesh.Triangles.Select(t => t + vertexOffset).ToList();
                mesh.SetTriangles(offsetTriangles, 4);
            }
            
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private void Clear()
        {
            _buildableSubmesh.Clear();
            _pathSubmesh.Clear();
            _blockedSubmesh.Clear();
            _decorativeSubmesh.Clear();
            _wallSubmesh.Clear();
        }
    }
}