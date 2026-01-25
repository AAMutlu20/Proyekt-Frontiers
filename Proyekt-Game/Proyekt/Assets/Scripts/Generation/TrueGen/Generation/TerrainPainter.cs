using System.Collections.Generic;
using Generation.TrueGen.Core;
using Generation.TrueGen.Visuals;
using UnityEngine;

namespace Generation.TrueGen.Generation
{
    public class TerrainPainter
    {
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        private readonly TerrainData _terrainData;
        private readonly int _alphamapResolution;
        private readonly float[,,] _alphaMaps;
        
        public TerrainPainter(TerrainData terrainData)
        {
            _terrainData = terrainData;
            _alphamapResolution = terrainData.alphamapResolution;
            _alphaMaps = _terrainData.GetAlphamaps(0, 0, _alphamapResolution, _alphamapResolution);
        }
        
        /// <summary>
        /// Setup terrain texture layers
        /// </summary>
        public void SetupTextureLayers(TerrainMaterialSet materialSet)
        {
            var layers = new TerrainLayer[4];
            
            // Layer 0: Grass (buildable)
            layers[0] = CreateTerrainLayer(materialSet.buildableMaterial, materialSet.buildableTiling);
            
            // Layer 1: Path (dirt/stone)
            layers[1] = CreateTerrainLayer(materialSet.pathMaterial, materialSet.pathTiling);
            
            // Layer 2: Blocked (rock)
            layers[2] = CreateTerrainLayer(materialSet.blockedMaterial, materialSet.blockedTiling);
            
            // Layer 3: Decorative
            layers[3] = CreateTerrainLayer(materialSet.decorativeMaterial, materialSet.decorativeTiling);
            
            _terrainData.terrainLayers = layers;
            
            // Initialize with grass everywhere
            for (var y = 0; y < _alphamapResolution; y++)
            {
                for (var x = 0; x < _alphamapResolution; x++)
                {
                    _alphaMaps[y, x, 0] = 1f; // Grass
                    _alphaMaps[y, x, 1] = 0f;
                    _alphaMaps[y, x, 2] = 0f;
                    _alphaMaps[y, x, 3] = 0f;
                }
            }
        }
        
        private static TerrainLayer CreateTerrainLayer(Material material, float tiling)
        {
            var layer = new TerrainLayer();
            
            // Extract textures from material
            if (material.HasProperty(BaseMap))
                layer.diffuseTexture = material.GetTexture(BaseMap) as Texture2D;
            
            if (material.HasProperty(BumpMap))
                layer.normalMapTexture = material.GetTexture(BumpMap) as Texture2D;
            
            layer.tileSize = new Vector2(tiling * 10f, tiling * 10f); // Adjust scale
            
            return layer;
        }
        
        /// <summary>
        /// Paint path texture along waypoints
        /// </summary>
        public void PaintPath(List<ChunkNode> pathChunks, float pathWidth)
        {
            if (pathChunks == null || pathChunks.Count < 2)
                return;
            
            var splinePoints = new List<Vector3>();
            foreach (var chunk in pathChunks)
            {
                splinePoints.Add(chunk.center);
            }
            
            var smoothPoints = SmoothPathMeshGenerator.GenerateCatmullRomSpline(splinePoints, 6);
            
            var terrainSize = _terrainData.size;
            
            // Paint each segment
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
                    
                    // Convert to alphamap coords
                    var alphaX = Mathf.RoundToInt((point.x / terrainSize.x) * _alphamapResolution);
                    var alphaY = Mathf.RoundToInt((point.z / terrainSize.z) * _alphamapResolution);
                    
                    var radius = (pathWidth / 2f / terrainSize.x) * _alphamapResolution;
                    
                    PaintCircle(alphaX, alphaY, radius, 1); // Layer 1 = path
                }
            }
            
            // Apply to terrain
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
            
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var chunk = chunks[x, y];
                    
                    // Skip paths (already painted)
                    if (chunk.chunkType == ChunkType.Path)
                        continue;
                    
                    var layerIndex = chunk.chunkType switch
                    {
                        ChunkType.Blocked => 2,
                        ChunkType.Decorative => 3,
                        _ => 0 // Buildable (grass)
                    };
                    
                    // Convert chunk center to alphamap coords
                    var alphaX = Mathf.RoundToInt((chunk.center.x / terrainSize.x) * _alphamapResolution);
                    var alphaY = Mathf.RoundToInt((chunk.center.z / terrainSize.z) * _alphamapResolution);
                    
                    var radius = (5f / terrainSize.x) * _alphamapResolution; // Half chunk size
                    
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
                    
                    // Set layer weights (all must sum to 1)
                    for (var layer = 0; layer < 4; layer++)
                    {
                        if (layer == layerIndex)
                            _alphaMaps[alphaY, alphaX, layer] = falloff;
                        else
                            _alphaMaps[alphaY, alphaX, layer] *= (1f - falloff);
                    }
                    
                    // Normalize to ensure sum = 1
                    var sum = 0f;
                    for (var layer = 0; layer < 4; layer++)
                        sum += _alphaMaps[alphaY, alphaX, layer];

                    if (!(sum > 0)) continue;
                    {
                        for (var layer = 0; layer < 4; layer++)
                            _alphaMaps[alphaY, alphaX, layer] /= sum;
                    }
                }
            }
        }
    }
}