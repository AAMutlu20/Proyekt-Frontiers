using System.Collections.Generic;
using Generation.TrueGen.Core;
using Generation.TrueGen.Visuals;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class TerrainPainter
    {
        private readonly TerrainData _terrainData;
        private readonly int _alphamapResolution;
        private float[,,] _alphaMaps;
        
        public TerrainPainter(TerrainData terrainData)
        {
            _terrainData = terrainData;
            _alphamapResolution = terrainData.alphamapResolution;
        }
        
        /// <summary>
        /// Setup terrain texture layers using pre-made TerrainLayer assets
        /// </summary>
        public void SetupTextureLayers(TerrainMaterialSet materialSet)
        {
            Debug.Log("=== Setting up Terrain Layers ===");
            
            // Validate that layers are assigned
            if (!materialSet.grassLayer || !materialSet.pathLayer || 
                !materialSet.blockedLayer || !materialSet.decorativeLayer)
            {
                Debug.LogError("❌ TerrainLayers not assigned in MaterialSet! Please assign all 4 layers.");
                return;
            }
            
            // Use pre-made layers directly
            var layers = new TerrainLayer[4];
            layers[0] = materialSet.grassLayer;
            layers[1] = materialSet.pathLayer;
            layers[2] = materialSet.blockedLayer;
            layers[3] = materialSet.decorativeLayer;
            
            // Set layers to terrain
            _terrainData.terrainLayers = layers;
            
            Debug.Log($"✓ Terrain now has {_terrainData.terrainLayers.Length} layers");
            
            // Get alphamaps
            _alphaMaps = _terrainData.GetAlphamaps(0, 0, _alphamapResolution, _alphamapResolution);
            
            // Initialize with grass everywhere
            for (var y = 0; y < _alphamapResolution; y++)
            {
                for (var x = 0; x < _alphamapResolution; x++)
                {
                    _alphaMaps[y, x, 0] = 1f; // Grass
                    _alphaMaps[y, x, 1] = 0f; // Path
                    _alphaMaps[y, x, 2] = 0f; // Blocked
                    _alphaMaps[y, x, 3] = 0f; // Decorative
                }
            }
            
            _terrainData.SetAlphamaps(0, 0, _alphaMaps);
            
            Debug.Log("✓ Terrain layers configured and initialized");
        }
        
        /// <summary>
        /// Paint path texture along waypoints
        /// </summary>
        public void PaintPath(List<ChunkNode> pathChunks, float pathWidth)
        {
            if (pathChunks == null || pathChunks.Count < 2) return;
            
            Debug.Log($"Painting path with {pathChunks.Count} chunks...");
            
            _alphaMaps = _terrainData.GetAlphamaps(0, 0, _alphamapResolution, _alphamapResolution);
            
            var splinePoints = new List<Vector3>();
            foreach (var chunk in pathChunks)
                splinePoints.Add(chunk.center);
            
            var smoothPoints = SmoothPathMeshGenerator.GenerateCatmullRomSpline(splinePoints, 6);
            var terrainSize = _terrainData.size;
            
            for (var i = 0; i < smoothPoints.Count - 1; i++)
            {
                var from = smoothPoints[i];
                var to = smoothPoints[i + 1];
                var distance = Vector3.Distance(from, to);
                var steps = Mathf.CeilToInt(distance * 2);
                
                for (var j = 0; j <= steps; j++)
                {
                    var t = (float)j / steps;
                    var point = Vector3.Lerp(from, to, t);
                    
                    var alphaX = Mathf.RoundToInt((point.x / terrainSize.x) * _alphamapResolution);
                    var alphaY = Mathf.RoundToInt((point.z / terrainSize.z) * _alphamapResolution);
                    var radius = (pathWidth / 2f / terrainSize.x) * _alphamapResolution;
                    
                    PaintCircle(alphaX, alphaY, radius, 1); // Layer 1 = path
                }
            }
            
            _terrainData.SetAlphamaps(0, 0, _alphaMaps);
            Debug.Log("✓ Painted path textures");
        }
        
        /// <summary>
        /// Paint decorative/blocked chunks
        /// </summary>
        public void PaintChunkTypes(ChunkNode[,] chunks)
        {
            var width = chunks.GetLength(0);
            var height = chunks.GetLength(1);
            var terrainSize = _terrainData.size;
            
            Debug.Log($"Painting chunk types on {width}x{height} grid...");
            
            _alphaMaps = _terrainData.GetAlphamaps(0, 0, _alphamapResolution, _alphamapResolution);
            
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var chunk = chunks[x, y];
                    if (chunk.chunkType == ChunkType.Path) continue;
                    
                    var layerIndex = chunk.chunkType switch
                    {
                        ChunkType.Blocked => 2,
                        ChunkType.Decorative => 3,
                        _ => 0
                    };
                    
                    var alphaX = Mathf.RoundToInt((chunk.center.x / terrainSize.x) * _alphamapResolution);
                    var alphaY = Mathf.RoundToInt((chunk.center.z / terrainSize.z) * _alphamapResolution);
                    var radius = (5f / terrainSize.x) * _alphamapResolution;
                    
                    PaintCircle(alphaX, alphaY, radius, layerIndex);
                }
            }
            
            _terrainData.SetAlphamaps(0, 0, _alphaMaps);
            Debug.Log("✓ Painted chunk type textures");
        }
        
        private void PaintCircle(int centerX, int centerY, float radius, int layerIndex)
        {
            var radiusInt = Mathf.CeilToInt(radius);
            
            for (var y = -radiusInt; y <= radiusInt; y++)
            {
                for (var x = -radiusInt; x <= radiusInt; x++)
                {
                    var alphaX = centerX + x;
                    var alphaY = centerY + y;
                    
                    if (alphaX < 0 || alphaX >= _alphamapResolution || 
                        alphaY < 0 || alphaY >= _alphamapResolution)
                        continue;
                    
                    var dist = Mathf.Sqrt(x * x + y * y);
                    if (!(dist <= radius)) continue;
                    
                    var falloff = 1f - (dist / radius);
                    falloff = Mathf.SmoothStep(0f, 1f, falloff);
                    
                    for (var layer = 0; layer < 4; layer++)
                    {
                        if (layer == layerIndex)
                            _alphaMaps[alphaY, alphaX, layer] = falloff;
                        else
                            _alphaMaps[alphaY, alphaX, layer] *= (1f - falloff);
                    }
                    
                    var sum = 0f;
                    for (var layer = 0; layer < 4; layer++)
                        sum += _alphaMaps[alphaY, alphaX, layer];
                    
                    if (sum > 0)
                    {
                        for (var layer = 0; layer < 4; layer++)
                            _alphaMaps[alphaY, alphaX, layer] /= sum;
                    }
                }
            }
        }
    }
}