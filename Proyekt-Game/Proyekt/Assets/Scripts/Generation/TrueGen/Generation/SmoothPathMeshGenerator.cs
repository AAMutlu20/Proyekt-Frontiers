using System.Collections.Generic;
using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public abstract class SmoothPathMeshGenerator
    {
        /// <summary>
        /// Generate a smooth ribbon mesh along the path using Catmull-Rom splines
        /// </summary>
        public static Mesh GenerateSmoothPathMesh(List<ChunkNode> pathChunks, float pathWidth = 0.8f, 
            float pathDepth = 0.3f, int segmentsPerChunk = 4)
        {
            if (pathChunks == null || pathChunks.Count < 2)
            {
                Debug.LogWarning("Need at least 2 path chunks to generate smooth path");
                return null;
            }
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            
            // Step 1: Create spline points from chunk centers
            var splinePoints = new List<Vector3>();
            foreach (var chunk in pathChunks) // Please do not turn this into a LINQ expression. It breaks things.
            {
                var point = chunk.center;
                point.y = -pathDepth + 0.05f; // Slightly above to prevent z-fighting
                splinePoints.Add(point);
            }
            
            // Step 2: Generate smooth interpolated points along the spline
            var smoothPoints = GenerateCatmullRomSpline(splinePoints, segmentsPerChunk);
            
            Debug.Log($"Generated {smoothPoints.Count} smooth points from {splinePoints.Count} control points");
            
            // Step 3: Generate ribbon mesh along the smooth path
            for (var i = 0; i < smoothPoints.Count; i++)
            {
                var point = smoothPoints[i];
                
                // Calculate tangent (direction along path)
                var forward = i < smoothPoints.Count - 1 ? (smoothPoints[i + 1] - point).normalized : (point - smoothPoints[i - 1]).normalized;

                // Calculate right direction (perpendicular to path)
                var right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Create two vertices for the ribbon (left and right edges)
                var leftVertex = point - right * (pathWidth / 2f);
                var rightVertex = point + right * (pathWidth / 2f);
                
                vertices.Add(leftVertex);
                vertices.Add(rightVertex);
                
                // UVs - tile along the path
                var uvV = (float)i / (smoothPoints.Count - 1);
                uvs.Add(new Vector2(0, uvV * 5f)); // Multiply for texture tiling
                uvs.Add(new Vector2(1, uvV * 5f));
                
                // Normals - all pointing up
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                
                // Create triangles connecting to previous segment
                if (i <= 0) continue;
                var baseIndex = (i - 1) * 2;
                    
                // First triangle (CCW winding)
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                    
                // Second triangle (CCW winding)
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }
            
            // Create mesh
            var mesh = new Mesh
            {
                name = "SmoothPath",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            
            Debug.Log($"✓ Generated smooth path: {vertices.Count} verts, {triangles.Count / 3} tris");
            
            return mesh;
        }
        
        /// <summary>
        /// Generate smooth curve using Catmull-Rom spline interpolation
        /// </summary>
        public static List<Vector3> GenerateCatmullRomSpline(List<Vector3> controlPoints, int segmentsPerPoint)
        {
            if (controlPoints.Count < 2)
                return controlPoints;
            
            var smoothPoints = new List<Vector3>();
            
            for (var i = 0; i < controlPoints.Count - 1; i++)
            {
                // Get 4 control points (with clamping at ends)
                var p0 = i > 0 ? controlPoints[i - 1] : controlPoints[i];
                var p1 = controlPoints[i];
                var p2 = controlPoints[i + 1];
                var p3 = i < controlPoints.Count - 2 ? controlPoints[i + 2] : controlPoints[i + 1];
                
                // Generate interpolated points between p1 and p2
                for (var j = 0; j < segmentsPerPoint; j++)
                {
                    var t = (float)j / segmentsPerPoint;
                    var point = CalculateCatmullRomPoint(p0, p1, p2, p3, t);
                    smoothPoints.Add(point);
                }
            }
            
            // Add final point
            smoothPoints.Add(controlPoints[^1]);
            
            return smoothPoints;
        }
        
        /// <summary>
        /// Calculate point on Catmull-Rom spline
        /// </summary>
        private static Vector3 CalculateCatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            
            var result = 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
            
            return result;
        }
        
        /// <summary>
        /// Generate smooth side walls for the path
        /// </summary>
        public static Mesh GenerateSmoothPathWalls(List<ChunkNode> pathChunks, float pathWidth = 0.8f, 
            float pathDepth = 0.3f, int segmentsPerChunk = 4)
        {
            if (pathChunks == null || pathChunks.Count < 2)
                return null;
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            
            // Create spline points
            var splinePoints = new List<Vector3>();
            foreach (var chunk in pathChunks)
            {
                splinePoints.Add(chunk.center);
            }
            
            var smoothPoints = GenerateCatmullRomSpline(splinePoints, segmentsPerChunk);
            
            for (var i = 0; i < smoothPoints.Count; i++)
            {
                var point = smoothPoints[i];
                
                // Calculate forward direction
                var forward = i < smoothPoints.Count - 1 ? (smoothPoints[i + 1] - point).normalized : (point - smoothPoints[i - 1]).normalized;
                
                var right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Create wall vertices for both sides
                // Left wall
                var leftBottom = point - right * (pathWidth / 2f);
                leftBottom.y = -pathDepth;
                var leftTop = leftBottom;
                leftTop.y = 0.01f;
                
                // Right wall
                var rightBottom = point + right * (pathWidth / 2f);
                rightBottom.y = -pathDepth;
                var rightTop = rightBottom;
                rightTop.y = 0.01f;
                
                // Add left wall vertices
                var leftBaseIndex = vertices.Count;
                vertices.Add(leftBottom);
                vertices.Add(leftTop);
                
                var leftNormal = -right; // Normal points outward (left)
                normals.Add(leftNormal);
                normals.Add(leftNormal);
                
                var uvV = (float)i / (smoothPoints.Count - 1);
                uvs.Add(new Vector2(0, uvV));
                uvs.Add(new Vector2(1, uvV));
                
                // Add right wall vertices
                var rightBaseIndex = vertices.Count;
                vertices.Add(rightBottom);
                vertices.Add(rightTop);
                
                var rightNormal = right; // Normal points outward (right)
                normals.Add(rightNormal);
                normals.Add(rightNormal);
                
                uvs.Add(new Vector2(0, uvV));
                uvs.Add(new Vector2(1, uvV));
                
                // Create wall triangles
                if (i > 0)
                {
                    var prevLeftBase = leftBaseIndex - 4;
                    var prevRightBase = rightBaseIndex - 4;
                    
                    // Left wall triangles
                    triangles.Add(prevLeftBase);
                    triangles.Add(leftBaseIndex);
                    triangles.Add(prevLeftBase + 1);
                    
                    triangles.Add(prevLeftBase + 1);
                    triangles.Add(leftBaseIndex);
                    triangles.Add(leftBaseIndex + 1);
                    
                    // Right wall triangles
                    triangles.Add(prevRightBase);
                    triangles.Add(prevRightBase + 1);
                    triangles.Add(rightBaseIndex);
                    
                    triangles.Add(rightBaseIndex);
                    triangles.Add(prevRightBase + 1);
                    triangles.Add(rightBaseIndex + 1);
                }
            }
            
            var mesh = new Mesh
            {
                name = "SmoothPathWalls",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            
            Debug.Log($"✓ Generated smooth walls: {vertices.Count} verts, {triangles.Count / 3} tris");
            
            return mesh;
        }
    }
}