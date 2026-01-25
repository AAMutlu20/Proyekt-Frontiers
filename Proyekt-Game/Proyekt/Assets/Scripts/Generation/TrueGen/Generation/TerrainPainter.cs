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
            // DON'T get alphamaps here - wait until after layers are set!
        }
        
        /// <summary>
        /// Setup terrain texture layers
        /// </summary>
        public void SetupTextureLayers(TerrainMaterialSet materialSet)
        {
            Debug.Log("=== Setting up Terrain Layers ===");
            
            var layers = new TerrainLayer[4];
            
            // Layer 0: Grass (buildable)
            Debug.Log("Creating Grass layer...");
            layers[0] = CreateTerrainLayerFromMaterial(
                "Grass", 
                materialSet.buildableMaterial,
                materialSet.buildableTiling,
                new Color(0.5f, 0.7f, 0.3f) // Green fallback
            );
            
            // Layer 1: Path (dirt/stone)
            Debug.Log("Creating Path layer...");
            layers[1] = CreateTerrainLayerFromMaterial(
                "Path",
                materialSet.pathMaterial,
                materialSet.pathTiling,
                new Color(0.6f, 0.5f, 0.4f) // Brown fallback
            );
            
            // Layer 2: Blocked (rock)
            Debug.Log("Creating Blocked layer...");
            layers[2] = CreateTerrainLayerFromMaterial(
                "Blocked",
                materialSet.blockedMaterial,
                materialSet.blockedTiling,
                new Color(0.4f, 0.4f, 0.4f) // Gray fallback
            );
            
            // Layer 3: Decorative
            Debug.Log("Creating Decorative layer...");
            layers[3] = CreateTerrainLayerFromMaterial(
                "Decorative",
                materialSet.decorativeMaterial,
                materialSet.decorativeTiling,
                new Color(0.6f, 0.5f, 0.7f) // Purple fallback
            );
            
            // Verify all layers are valid
            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    Debug.LogError($"Layer {i} is NULL!");
                    layers[i] = CreateFallbackLayer($"Fallback_{i}", Color.magenta);
                }
                else if (layers[i].diffuseTexture == null)
                {
                    Debug.LogError($"Layer {i} has NULL diffuse texture!");
                    layers[i].diffuseTexture = CreateSolidColorTexture(Color.magenta, 64);
                }
                else
                {
                    Debug.Log($"✓ Layer {i} ({layers[i].name}) valid: {layers[i].diffuseTexture.width}x{layers[i].diffuseTexture.height}");
                }
            }
            
            // SET LAYERS FIRST!
            _terrainData.terrainLayers = layers;
            
            Debug.Log($"Terrain now has {_terrainData.terrainLayers.Length} layers");
            
            // NOW get alphamaps with correct dimensions
            _alphaMaps = _terrainData.GetAlphamaps(0, 0, _alphamapResolution, _alphamapResolution);
            
            Debug.Log($"Alphamap dimensions: {_alphaMaps.GetLength(0)} x {_alphaMaps.GetLength(1)} x {_alphaMaps.GetLength(2)}");
            
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
            
            _terrainData.SetAlphamaps(0, 0, _alphaMaps);
            
            Debug.Log("✓ Terrain layers configured and alpha maps initialized");
        }
        
        /// <summary>
        /// Create terrain layer from material with fallback color
        /// </summary>
        private TerrainLayer CreateTerrainLayerFromMaterial(string name, Material material, float tiling, Color fallbackColor)
        {
            Texture2D diffuseTexture = null;
            Texture2D normalTexture = null;
            
            if (material != null)
            {
                Debug.Log($"  Material: {material.name}, Shader: {material.shader.name}");
                
                // Try to extract textures
                diffuseTexture = ExtractTextureFromMaterial(material);
                normalTexture = ExtractNormalMapFromMaterial(material);
                
                if (diffuseTexture != null)
                {
                    Debug.Log($"  ✓ Found diffuse texture: {diffuseTexture.name} ({diffuseTexture.width}x{diffuseTexture.height})");
                }
                else
                {
                    Debug.LogWarning($"  ⚠ No diffuse texture found in material {material.name}");
                }
            }
            else
            {
                Debug.LogError($"  ✗ Material is NULL for {name}");
            }
            
            // Create fallback if no texture found
            if (diffuseTexture == null)
            {
                Debug.Log($"  Creating fallback texture with color {fallbackColor}");
                diffuseTexture = CreateSolidColorTexture(fallbackColor, 256);
            }
            
            var layer = new TerrainLayer
            {
                name = name,
                diffuseTexture = diffuseTexture,
                normalMapTexture = normalTexture,
                tileSize = new Vector2(tiling * 10f, tiling * 10f),
                tileOffset = Vector2.zero,
                metallic = 0f,
                smoothness = 0f,
                specular = Color.black
            };
            
            return layer;
        }
        
        /// <summary>
        /// Extract diffuse texture from material
        /// </summary>
        private Texture2D ExtractTextureFromMaterial(Material material)
        {
            if (material == null) return null;
            
            // Check all common texture property names
            string[] texturePropertyNames = { "_BaseMap", "_MainTex", "_BaseColorMap", "_AlbedoMap" };
            
            foreach (var propName in texturePropertyNames)
            {
                if (material.HasProperty(propName))
                {
                    var texture = material.GetTexture(propName) as Texture2D;
                    if (texture != null)
                    {
                        Debug.Log($"    Found texture at property: {propName}");
                        return texture;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Extract normal map from material
        /// </summary>
        private Texture2D ExtractNormalMapFromMaterial(Material material)
        {
            if (material == null) return null;
            
            string[] normalPropertyNames = { "_BumpMap", "_NormalMap" };
            
            foreach (var propName in normalPropertyNames)
            {
                if (material.HasProperty(propName))
                {
                    var texture = material.GetTexture(propName) as Texture2D;
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Create a solid color texture
        /// </summary>
        private Texture2D CreateSolidColorTexture(Color color, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"GeneratedTexture_{color}",
                wrapMode = TextureWrapMode.Repeat
            };
            
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            Debug.Log($"    Created {size}x{size} texture with color {color}");
            
            return texture;
        }
        
        /// <summary>
        /// Create emergency fallback layer
        /// </summary>
        private TerrainLayer CreateFallbackLayer(string name, Color color)
        {
            return new TerrainLayer
            {
                name = name,
                diffuseTexture = CreateSolidColorTexture(color, 64),
                tileSize = new Vector2(10f, 10f),
                tileOffset = Vector2.zero,
                metallic = 0f,
                smoothness = 0f,
                specular = Color.black
            };
        }
        
        /// <summary>
        /// Paint path texture along waypoints
        /// </summary>
        public void PaintPath(List<ChunkNode> pathChunks, float pathWidth)
        {
            if (pathChunks == null || pathChunks.Count < 2)
                return;
            
            Debug.Log($"Painting path with {pathChunks.Count} chunks...");
            
            // Refresh alphamaps
            _alphaMaps = _terrainData.GetAlphamaps(0, 0, _alphamapResolution, _alphamapResolution);
            
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
            
            Debug.Log($"Painting chunk types on {width}x{height} grid...");
            
            // Refresh alphamaps
            _alphaMaps = _terrainData.GetAlphamaps(0, 0, _alphamapResolution, _alphamapResolution);
            
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