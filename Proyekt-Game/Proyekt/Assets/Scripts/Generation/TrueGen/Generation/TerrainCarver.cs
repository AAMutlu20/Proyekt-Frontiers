using System.Collections.Generic;
using System.Linq;
using Generation.TrueGen.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        
        public void CarvePath(List<ChunkNode> pathChunks, float pathWidth, float pathDepth)
        {
            if (pathChunks == null || pathChunks.Count < 2)
                return;
            
            var splinePoints = pathChunks.Select(chunk => chunk.center).ToList();
            var smoothPoints = SmoothPathMeshGenerator.GenerateCatmullRomSpline(splinePoints, 6);
            
            for (var i = 0; i < smoothPoints.Count - 1; i++)
            {
                CarveSegment(smoothPoints[i], smoothPoints[i + 1], pathWidth, pathDepth);
            }
            
            _terrainData.SetHeights(0, 0, _heights);
            
            #if UNITY_EDITOR
            EditorUtility.SetDirty(_terrainData);
            AssetDatabase.SaveAssets();
            #endif
            
            Debug.Log($"✓ Carved {smoothPoints.Count} path segments");
        }
        
        private void CarveSegment(Vector3 from, Vector3 to, float width, float depth)
        {
            var terrainSize = _terrainData.size;
            var distance = Vector3.Distance(from, to);
            var steps = Mathf.CeilToInt(distance * 2);
            
            for (var i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                var point = Vector3.Lerp(from, to, t);
                
                var hmX = Mathf.RoundToInt((point.x / terrainSize.x) * _resolution);
                var hmY = Mathf.RoundToInt((point.z / terrainSize.z) * _resolution);
                
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
                    
                    if (hmX < 0 || hmX >= _resolution || hmY < 0 || hmY >= _resolution)
                        continue;
                    
                    var dist = Mathf.Sqrt(x * x + y * y);
                    if (!(dist <= radius)) continue;
                    
                    var falloff = 1f - (dist / radius);
                    falloff = Mathf.SmoothStep(0f, 1f, falloff);
                    
                    var targetHeight = _heights[hmY, hmX] - (depth * falloff);
                    _heights[hmY, hmX] = Mathf.Min(_heights[hmY, hmX], targetHeight);
                }
            }
        }
    }
}