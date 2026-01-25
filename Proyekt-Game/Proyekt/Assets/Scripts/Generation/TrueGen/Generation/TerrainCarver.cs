using System.Collections.Generic;
using System.Linq;
using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class TerrainCarver
    {
        private readonly TerrainData _terrainData;
        private readonly float[,] _heights;
        private readonly int _resolution;
        
        public TerrainCarver(TerrainData terrainData)
        {
            _terrainData = terrainData;
            _resolution = terrainData.heightmapResolution;
            _heights = terrainData.GetHeights(0, 0, _resolution, _resolution);
        }
        
        /// <summary>
        /// Carve a smooth path along waypoints
        /// </summary>
        public void CarvePath(List<ChunkNode> pathChunks, float pathWidth, float pathDepth)
        {
            if (pathChunks == null || pathChunks.Count < 2)
                return;
            
            // Generate smooth spline points
            var splinePoints = pathChunks.Select(chunk => chunk.center).ToList();

            var smoothPoints = SmoothPathMeshGenerator.GenerateCatmullRomSpline(splinePoints, 6);
            
            // Carve each segment
            for (var i = 0; i < smoothPoints.Count - 1; i++)
            {
                CarveSegment(smoothPoints[i], smoothPoints[i + 1], pathWidth, pathDepth);
            }
            
            // Apply to terrain
            _terrainData.SetHeights(0, 0, _heights);
            
            Debug.Log($"✓ Carved {smoothPoints.Count} path segments");
        }
        
        private void CarveSegment(Vector3 from, Vector3 to, float width, float depth)
        {
            var terrainSize = _terrainData.size;
            
            // Sample points along segment
            var distance = Vector3.Distance(from, to);
            var steps = Mathf.CeilToInt(distance * 2); // Dense sampling
            
            for (var i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                var point = Vector3.Lerp(from, to, t);
                
                // Convert world position to heightmap coordinates
                var hmX = Mathf.RoundToInt((point.x / terrainSize.x) * _resolution);
                var hmY = Mathf.RoundToInt((point.z / terrainSize.z) * _resolution);
                
                // Carve circular area around this point
                var radiusInHeightmap = (width / 2f / terrainSize.x) * _resolution;
                var depthNormalized = depth / terrainSize.y;
                
                CarveCircle(hmX, hmY, radiusInHeightmap, depthNormalized);
            }
        }
        
        private void CarveCircle(int centerX, int centerY, float radius, float depth)
        {
            var radiusInt = Mathf.CeilToInt(radius);
            
            for (var y = -radiusInt; y <= radiusInt; y++)
            {
                for (var x = -radiusInt; x <= radiusInt; x++)
                {
                    var hmX = centerX + x;
                    var hmY = centerY + y;
                    
                    // Bounds check
                    if (hmX < 0 || hmX >= _resolution || hmY < 0 || hmY >= _resolution)
                        continue;
                    
                    // Distance from center
                    var dist = Mathf.Sqrt(x * x + y * y);
                    
                    if (!(dist <= radius)) continue;
                    
                    // Smooth falloff (0 at edge, 1 at center)
                    var falloff = 1f - (dist / radius);
                    falloff = Mathf.SmoothStep(0f, 1f, falloff);
                    
                    // Lower terrain height
                    var targetHeight = _heights[hmY, hmX] - (depth * falloff);
                    _heights[hmY, hmX] = Mathf.Min(_heights[hmY, hmX], targetHeight);
                }
            }
        }
    }
}