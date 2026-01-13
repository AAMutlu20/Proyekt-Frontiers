using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Generation
{
    [System.Serializable]
    public class BiomeType
    {
        public string name;
        public float baseHeight;
        public float noiseScale;
        public float noiseHeight;
        public int octaves;
        public Color debugColor;
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private int terrainSize = 500;
        [SerializeField] private float cellSize = 1f;
        
        [Header("Chunk System")]
        [SerializeField] private int biomeCount = 20; // More biomes
        [SerializeField] private float minBiomeDistance = 60f; // Closer together
        
        [Header("City Biome (Safe Zone)")]
        [SerializeField] private Vector2 cityBiomePosition = new Vector2(250f, 250f);
        
        [Header("Biome Blending")]
        [SerializeField] private float blendRadius = 100f; // Consider all biomes within this radius
        [SerializeField] private float blendPower = 3f; // Higher = sharper but smooth transitions
        
        [Header("Biome Definitions")]
        [SerializeField] private BiomeType[] biomes = new BiomeType[]
        {
            // Flat buildable biomes
            new BiomeType { name = "City Plains", baseHeight = 20f, noiseScale = 160f, noiseHeight = 4f, octaves = 2, debugColor = Color.green },
            new BiomeType { name = "Open Fields", baseHeight = 18f, noiseScale = 180f, noiseHeight = 3f, octaves = 2, debugColor = new Color(0.8f, 1f, 0.8f) },
            new BiomeType { name = "Grasslands", baseHeight = 22f, noiseScale = 150f, noiseHeight = 5f, octaves = 2, debugColor = new Color(0.6f, 0.9f, 0.6f) },
            
            // Gentle hills
            new BiomeType { name = "Rolling Hills", baseHeight = 25f, noiseScale = 100f, noiseHeight = 10f, octaves = 3, debugColor = Color.yellow },
            new BiomeType { name = "Gentle Slopes", baseHeight = 23f, noiseScale = 110f, noiseHeight = 8f, octaves = 2, debugColor = new Color(0.9f, 0.9f, 0.6f) },
            new BiomeType { name = "Hill Country", baseHeight = 27f, noiseScale = 95f, noiseHeight = 12f, octaves = 3, debugColor = new Color(0.8f, 0.8f, 0.4f) },
            
            // Medium elevation
            new BiomeType { name = "Rocky Terrain", baseHeight = 35f, noiseScale = 85f, noiseHeight = 15f, octaves = 3, debugColor = new Color(0.7f, 0.7f, 0.7f) },
            new BiomeType { name = "Highlands", baseHeight = 38f, noiseScale = 90f, noiseHeight = 18f, octaves = 3, debugColor = new Color(0.6f, 0.6f, 0.6f) },
            new BiomeType { name = "Plateau Edge", baseHeight = 40f, noiseScale = 80f, noiseHeight = 16f, octaves = 3, debugColor = new Color(0.65f, 0.5f, 0.4f) },
            
            // Valleys and low areas
            new BiomeType { name = "River Valley", baseHeight = 10f, noiseScale = 120f, noiseHeight = 8f, octaves = 2, debugColor = new Color(0.4f, 0.6f, 0.8f) },
            new BiomeType { name = "Low Basin", baseHeight = 12f, noiseScale = 130f, noiseHeight = 10f, octaves = 2, debugColor = new Color(0.5f, 0.7f, 0.6f) },
            new BiomeType { name = "Canyon Floor", baseHeight = 8f, noiseScale = 100f, noiseHeight = 12f, octaves = 3, debugColor = new Color(0.7f, 0.5f, 0.4f) },
            
            // Dramatic elevation
            new BiomeType { name = "Mountain Base", baseHeight = 45f, noiseScale = 95f, noiseHeight = 20f, octaves = 3, debugColor = new Color(0.8f, 0.8f, 0.9f) },
            new BiomeType { name = "Mountain Ridge", baseHeight = 55f, noiseScale = 90f, noiseHeight = 22f, octaves = 3, debugColor = Color.white },
            new BiomeType { name = "Alpine Peaks", baseHeight = 60f, noiseScale = 85f, noiseHeight = 25f, octaves = 3, debugColor = new Color(0.9f, 0.9f, 1f) },
            
            // Rough terrain
            new BiomeType { name = "Badlands", baseHeight = 30f, noiseScale = 70f, noiseHeight = 20f, octaves = 4, debugColor = new Color(0.8f, 0.4f, 0.3f) },
            new BiomeType { name = "Broken Ground", baseHeight = 28f, noiseScale = 75f, noiseHeight = 18f, octaves = 4, debugColor = new Color(0.7f, 0.5f, 0.4f) },
            new BiomeType { name = "Craggy Hills", baseHeight = 32f, noiseScale = 80f, noiseHeight = 16f, octaves = 3, debugColor = new Color(0.6f, 0.6f, 0.5f) },
            
            // Unique features
            new BiomeType { name = "Mesa", baseHeight = 42f, noiseScale = 140f, noiseHeight = 8f, octaves = 2, debugColor = new Color(0.9f, 0.6f, 0.4f) },
            new BiomeType { name = "Volcanic", baseHeight = 48f, noiseScale = 85f, noiseHeight = 24f, octaves = 3, debugColor = new Color(0.3f, 0.3f, 0.3f) }
        };
        
        [Header("Advanced Noise")]
        [SerializeField] private bool enableDependentNoise = true;
        [SerializeField] private float dependentNoiseStrength = 12f;
        [SerializeField] private float dependentNoiseScale = 130f;
        
        [Header("Octave Attenuation")]
        [SerializeField] private bool enableOctaveAttenuation = true;
        [SerializeField] private float attenuationStrength = 0.5f;
        
        [Header("Domain Warping")]
        [SerializeField] private float warpStrength = 25f;
        [SerializeField] private float warpScale = 110f;
        
        [Header("Erosion")]
        [SerializeField] private bool enableErosion = true;
        [SerializeField] private int erosionIterations = 3;
        [SerializeField] private float erosionRate = 0.15f;
        [SerializeField] private float slopeThreshold = 1.8f;
        
        [Header("General Settings")]
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2.2f;
        [SerializeField] private int seed = 0;
        [SerializeField] private Vector2 noiseOffset = Vector2.zero;
    
        private Mesh _mesh;
        private Vector3[] _vertices;
        private int[] _triangles;
        private float[,] _heightMap;
        private Vector2[] _biomePositions;
        private int[] _biomeTypes;

        public void GenerateTerrain()
        {
            Random.InitState(seed);
            
            _mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            GetComponent<MeshFilter>().mesh = _mesh;
            
            GenerateBiomeLayout();
            BuildHeightfield();
            
            if (enableErosion) SimulateErosion();
            
            CreateMeshFromHeightMap();
            UpdateMesh();
        }

        private void GenerateBiomeLayout()
        {
            var positions = new List<Vector2>();
            var types = new List<int>();
            
            positions.Add(cityBiomePosition);
            types.Add(0); // City Plains
            
            var attemptsRemaining = biomeCount * 25;
            while (positions.Count < biomeCount && attemptsRemaining > 0)
            {
                var x = Random.Range(terrainSize * 0.1f, terrainSize * 0.9f);
                var z = Random.Range(terrainSize * 0.1f, terrainSize * 0.9f);
                var candidate = new Vector2(x, z);
                
                var validPosition = positions.All(pos => !(Vector2.Distance(candidate, pos) < minBiomeDistance));

                if (validPosition)
                {
                    positions.Add(candidate);
                    types.Add(Random.Range(1, biomes.Length));
                }
                
                attemptsRemaining--;
            }
            
            _biomePositions = positions.ToArray();
            _biomeTypes = types.ToArray();
        }

        private void BuildHeightfield()
        {
            _heightMap = new float[terrainSize + 1, terrainSize + 1];
            
            for (var z = 0; z <= terrainSize; z++)
            {
                for (var x = 0; x <= terrainSize; x++)
                {
                    var pos = new Vector2(x, z);
                    _heightMap[x, z] = CalculateHeightFromBiomes(x, z, pos);
                }
            }
        }

        private float CalculateHeightFromBiomes(int x, int z, Vector2 pos)
        {
            // Collect ALL biomes within blend radius
            var nearbyBiomes = new List<int>();
            var distances = new List<float>();
            
            for (var i = 0; i < _biomePositions.Length; i++)
            {
                var dist = Vector2.Distance(pos, _biomePositions[i]);
                if (!(dist < blendRadius)) continue;
                nearbyBiomes.Add(i);
                distances.Add(dist);
            }
            
            // If no biomes in range, use closest
            if (nearbyBiomes.Count == 0)
            {
                var closestBiome = 0;
                var closestDist = float.MaxValue;
                for (var i = 0; i < _biomePositions.Length; i++)
                {
                    var dist = Vector2.Distance(pos, _biomePositions[i]);
                    if (!(dist < closestDist)) continue;
                    closestDist = dist;
                    closestBiome = i;
                }
                return GenerateAdvancedBiomeHeight(x, z, _biomeTypes[closestBiome], closestBiome * 137f);
            }
            
            // Calculate smooth blend weights using exponential falloff
            var totalWeight = 0f;
            var totalHeight = 0f;
            
            for (var i = 0; i < nearbyBiomes.Count; i++)
            {
                var biomeIdx = nearbyBiomes[i];
                var dist = distances[i];
                
                // Exponential falloff for ultra-smooth blending
                var normalizedDist = dist / blendRadius;
                var weight = Mathf.Pow(1f - normalizedDist, blendPower);
                
                // Generate height for this biome
                var height = GenerateAdvancedBiomeHeight(x, z, _biomeTypes[biomeIdx], biomeIdx * 137f);
                
                totalHeight += height * weight;
                totalWeight += weight;
            }
            
            return totalHeight / totalWeight;
        }

        private float GenerateAdvancedBiomeHeight(int x, int z, int biomeTypeIndex, float offsetSeed)
        {
            var biome = biomes[biomeTypeIndex];
            
            var warpedPos = ApplyProgressiveDomainWarp(x, z, offsetSeed);
            
            var height = biome.baseHeight;
            
            if (enableDependentNoise)
            {
                var dependentSample = Mathf.PerlinNoise(
                    (warpedPos.x + offsetSeed) / dependentNoiseScale,
                    (warpedPos.y + offsetSeed) / dependentNoiseScale
                );
                
                warpedPos.x += dependentSample * dependentNoiseStrength;
                warpedPos.y += dependentSample * dependentNoiseStrength;
                warpedPos.x = Mathf.Clamp(warpedPos.x, 0, terrainSize);
                warpedPos.y = Mathf.Clamp(warpedPos.y, 0, terrainSize);
            }
            
            height += GenerateAttenuatedNoise(
                (int)warpedPos.x, 
                (int)warpedPos.y, 
                biome.noiseScale, 
                biome.noiseHeight, 
                biome.octaves,
                offsetSeed
            );
            
            return height;
        }

        private Vector2 ApplyProgressiveDomainWarp(int x, int z, float offsetSeed)
        {
            var warpX = Mathf.PerlinNoise(
                (x + noiseOffset.x + offsetSeed) / warpScale, 
                (z + noiseOffset.y + offsetSeed) / warpScale
            );
            var warpZ = Mathf.PerlinNoise(
                (x + noiseOffset.x + offsetSeed + 1000f) / warpScale, 
                (z + noiseOffset.y + offsetSeed + 1000f) / warpScale
            );
            
            warpX = (warpX * 2f - 1f) * warpStrength;
            warpZ = (warpZ * 2f - 1f) * warpStrength;
            
            return new Vector2(
                Mathf.Clamp(x + warpX, 0, terrainSize),
                Mathf.Clamp(z + warpZ, 0, terrainSize)
            );
        }

        private float GenerateAttenuatedNoise(int x, int z, float scale, float heightMultiplier, int octaveCount, float offsetSeed)
        {
            if (scale <= 0.001f) return 0;
            
            var octaveValues = new float[octaveCount];
            var amplitude = 1f;
            var frequency = 1f;
            
            for (var i = 0; i < octaveCount; i++)
            {
                var sampleX = (x + noiseOffset.x + offsetSeed) / scale * frequency;
                var sampleZ = (z + noiseOffset.y + offsetSeed) / scale * frequency;
                
                var perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
                perlinValue = perlinValue * 2f - 1f;
                
                octaveValues[i] = perlinValue * amplitude;
                
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            if (enableOctaveAttenuation && octaveCount > 2)
            {
                var lowFreqValue = octaveValues[0];
                
                for (var i = 2; i < octaveCount; i++)
                {
                    var attenuationFactor = Mathf.Lerp(
                        1f - attenuationStrength, 
                        1f, 
                        (lowFreqValue + 1f) * 0.5f
                    );
                    octaveValues[i] *= attenuationFactor;
                }
            }
            
            var finalHeight = octaveValues.Sum();

            return finalHeight * heightMultiplier;
        }

        private void SimulateErosion()
        {
            var eroded = new float[terrainSize + 1, terrainSize + 1];
            
            for (var iteration = 0; iteration < erosionIterations; iteration++)
            {
                System.Array.Copy(_heightMap, eroded, _heightMap.Length);
                
                for (var z = 1; z < terrainSize; z++)
                {
                    for (var x = 1; x < terrainSize; x++)
                    {
                        var currentHeight = _heightMap[x, z];
                        var maxHeightDiff = 0f;
                        int lowestX = x, lowestZ = z;
                        
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                var nx = x + dx;
                                var nz = z + dz;
                                
                                if (nx < 0 || nx > terrainSize || nz < 0 || nz > terrainSize) continue;
                                
                                var heightDiff = currentHeight - _heightMap[nx, nz];
                                if (!(heightDiff > maxHeightDiff)) continue;
                                maxHeightDiff = heightDiff;
                                lowestX = nx;
                                lowestZ = nz;
                            }
                        }

                        if (!(maxHeightDiff > slopeThreshold)) continue;
                        var erodeAmount = (maxHeightDiff - slopeThreshold) * erosionRate;
                        eroded[x, z] -= erodeAmount;
                        eroded[lowestX, lowestZ] += erodeAmount * 0.5f;
                    }
                }
                
                System.Array.Copy(eroded, _heightMap, _heightMap.Length);
            }
        }

        private void CreateMeshFromHeightMap()
        {
            var vertexCount = (terrainSize + 1) * (terrainSize + 1);
            _vertices = new Vector3[vertexCount];
        
            var vertIndex = 0;
            for (var z = 0; z <= terrainSize; z++)
            {
                for (var x = 0; x <= terrainSize; x++)
                {
                    _vertices[vertIndex] = new Vector3(x * cellSize, _heightMap[x, z], z * cellSize);
                    vertIndex++;
                }
            }
        
            var quadCount = terrainSize * terrainSize;
            _triangles = new int[quadCount * 6];
        
            var triIndex = 0;
            for (var z = 0; z < terrainSize; z++)
            {
                for (var x = 0; x < terrainSize; x++)
                {
                    var bottomLeft = z * (terrainSize + 1) + x;
                    var bottomRight = bottomLeft + 1;
                    var topLeft = bottomLeft + (terrainSize + 1);
                    var topRight = topLeft + 1;
                
                    _triangles[triIndex++] = bottomLeft;
                    _triangles[triIndex++] = topLeft;
                    _triangles[triIndex++] = bottomRight;
                    _triangles[triIndex++] = bottomRight;
                    _triangles[triIndex++] = topLeft;
                    _triangles[triIndex++] = topRight;
                }
            }
        }

        private void UpdateMesh()
        {
            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.RecalculateNormals();
        }
    }
}