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

    [RequireComponent(typeof(Terrain))]
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private int terrainResolution = 513;
        [SerializeField] private float terrainWidth = 500f;
        [SerializeField] private float terrainLength = 500f;
        [SerializeField] private float terrainHeight = 100f;
        
        [Header("Chunk System")]
        [SerializeField] private int biomeCount = 20;
        [SerializeField] private float minBiomeDistance = 60f;
        
        [Header("City Biome (Safe Zone)")]
        [SerializeField] private Vector2 cityBiomePosition = new Vector2(250f, 250f);
        
        [Header("Biome Blending")]
        [SerializeField] private float blendRadius = 100f;
        [SerializeField] private float blendPower = 3f;
        
        [Header("Biome Definitions")]
        [SerializeField] private BiomeType[] biomes = new BiomeType[]
        {
            new BiomeType { name = "City Plains", baseHeight = 20f, noiseScale = 160f, noiseHeight = 4f, octaves = 2, debugColor = Color.green },
            new BiomeType { name = "Open Fields", baseHeight = 18f, noiseScale = 180f, noiseHeight = 3f, octaves = 2, debugColor = new Color(0.8f, 1f, 0.8f) },
            new BiomeType { name = "Grasslands", baseHeight = 22f, noiseScale = 150f, noiseHeight = 5f, octaves = 2, debugColor = new Color(0.6f, 0.9f, 0.6f) },
            new BiomeType { name = "Rolling Hills", baseHeight = 25f, noiseScale = 100f, noiseHeight = 10f, octaves = 3, debugColor = Color.yellow },
            new BiomeType { name = "Gentle Slopes", baseHeight = 23f, noiseScale = 110f, noiseHeight = 8f, octaves = 2, debugColor = new Color(0.9f, 0.9f, 0.6f) },
            new BiomeType { name = "Hill Country", baseHeight = 27f, noiseScale = 95f, noiseHeight = 12f, octaves = 3, debugColor = new Color(0.8f, 0.8f, 0.4f) },
            new BiomeType { name = "Rocky Terrain", baseHeight = 35f, noiseScale = 85f, noiseHeight = 15f, octaves = 3, debugColor = new Color(0.7f, 0.7f, 0.7f) },
            new BiomeType { name = "Highlands", baseHeight = 38f, noiseScale = 90f, noiseHeight = 18f, octaves = 3, debugColor = new Color(0.6f, 0.6f, 0.6f) },
            new BiomeType { name = "Plateau Edge", baseHeight = 40f, noiseScale = 80f, noiseHeight = 16f, octaves = 3, debugColor = new Color(0.65f, 0.5f, 0.4f) },
            new BiomeType { name = "River Valley", baseHeight = 10f, noiseScale = 120f, noiseHeight = 8f, octaves = 2, debugColor = new Color(0.4f, 0.6f, 0.8f) },
            new BiomeType { name = "Low Basin", baseHeight = 12f, noiseScale = 130f, noiseHeight = 10f, octaves = 2, debugColor = new Color(0.5f, 0.7f, 0.6f) },
            new BiomeType { name = "Canyon Floor", baseHeight = 8f, noiseScale = 100f, noiseHeight = 12f, octaves = 3, debugColor = new Color(0.7f, 0.5f, 0.4f) },
            new BiomeType { name = "Mountain Base", baseHeight = 45f, noiseScale = 95f, noiseHeight = 20f, octaves = 3, debugColor = new Color(0.8f, 0.8f, 0.9f) },
            new BiomeType { name = "Mountain Ridge", baseHeight = 55f, noiseScale = 90f, noiseHeight = 22f, octaves = 3, debugColor = Color.white },
            new BiomeType { name = "Alpine Peaks", baseHeight = 60f, noiseScale = 85f, noiseHeight = 25f, octaves = 3, debugColor = new Color(0.9f, 0.9f, 1f) },
            new BiomeType { name = "Badlands", baseHeight = 30f, noiseScale = 70f, noiseHeight = 20f, octaves = 4, debugColor = new Color(0.8f, 0.4f, 0.3f) },
            new BiomeType { name = "Broken Ground", baseHeight = 28f, noiseScale = 75f, noiseHeight = 18f, octaves = 4, debugColor = new Color(0.7f, 0.5f, 0.4f) },
            new BiomeType { name = "Craggy Hills", baseHeight = 32f, noiseScale = 80f, noiseHeight = 16f, octaves = 3, debugColor = new Color(0.6f, 0.6f, 0.5f) },
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
    
        private Terrain _terrain;
        private TerrainData _terrainData;
        private float[,] _heightMap;
        private Vector2[] _biomePositions;
        private int[] _biomeTypes;

        public void GenerateTerrain()
        {
            Random.InitState(seed);
            
            _terrain = GetComponent<Terrain>();
            _terrainData = _terrain.terrainData;
            
            _terrainData.heightmapResolution = terrainResolution;
            _terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
            
            GenerateBiomeLayout();
            BuildHeightfield();
            
            if (enableErosion) SimulateErosion();
            
            ApplyHeightsToTerrain();
        }

        private void GenerateBiomeLayout()
        {
            var positions = new List<Vector2>();
            var types = new List<int>();
            
            positions.Add(cityBiomePosition);
            types.Add(0);
            
            var attemptsRemaining = biomeCount * 25;
            while (positions.Count < biomeCount && attemptsRemaining > 0)
            {
                var x = Random.Range(terrainWidth * 0.1f, terrainWidth * 0.9f);
                var z = Random.Range(terrainLength * 0.1f, terrainLength * 0.9f);
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
            _heightMap = new float[terrainResolution, terrainResolution];
            
            for (var z = 0; z < terrainResolution; z++)
            {
                for (var x = 0; x < terrainResolution; x++)
                {
                    var worldX = (x / (float)(terrainResolution - 1)) * terrainWidth;
                    var worldZ = (z / (float)(terrainResolution - 1)) * terrainLength;
                    var pos = new Vector2(worldX, worldZ);
                    
                    _heightMap[z, x] = CalculateHeightFromBiomes(worldX, worldZ, pos);
                }
            }
        }

        private float CalculateHeightFromBiomes(float worldX, float worldZ, Vector2 pos)
        {
            var nearbyBiomes = new List<int>();
            var distances = new List<float>();
            
            for (var i = 0; i < _biomePositions.Length; i++)
            {
                var dist = Vector2.Distance(pos, _biomePositions[i]);
                if (!(dist < blendRadius)) continue;
                nearbyBiomes.Add(i);
                distances.Add(dist);
            }
            
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
                return GenerateAdvancedBiomeHeight(worldX, worldZ, _biomeTypes[closestBiome], closestBiome * 137f);
            }
            
            var totalWeight = 0f;
            var totalHeight = 0f;
            
            for (var i = 0; i < nearbyBiomes.Count; i++)
            {
                var biomeIdx = nearbyBiomes[i];
                var dist = distances[i];
                
                var normalizedDist = dist / blendRadius;
                var weight = Mathf.Pow(1f - normalizedDist, blendPower);
                
                var height = GenerateAdvancedBiomeHeight(worldX, worldZ, _biomeTypes[biomeIdx], biomeIdx * 137f);
                
                totalHeight += height * weight;
                totalWeight += weight;
            }
            
            return totalHeight / totalWeight;
        }

        private float GenerateAdvancedBiomeHeight(float x, float z, int biomeTypeIndex, float offsetSeed)
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
                warpedPos.x = Mathf.Clamp(warpedPos.x, 0, terrainWidth);
                warpedPos.y = Mathf.Clamp(warpedPos.y, 0, terrainLength);
            }
            
            height += GenerateAttenuatedNoise(
                warpedPos.x, 
                warpedPos.y, 
                biome.noiseScale, 
                biome.noiseHeight, 
                biome.octaves,
                offsetSeed
            );
            
            return height;
        }

        private Vector2 ApplyProgressiveDomainWarp(float x, float z, float offsetSeed)
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
                Mathf.Clamp(x + warpX, 0, terrainWidth),
                Mathf.Clamp(z + warpZ, 0, terrainLength)
            );
        }

        private float GenerateAttenuatedNoise(float x, float z, float scale, float heightMultiplier, int octaveCount, float offsetSeed)
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

            if (!enableOctaveAttenuation || octaveCount <= 2) return octaveValues.Sum() * heightMultiplier;
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

            return octaveValues.Sum() * heightMultiplier;
        }

        private void SimulateErosion()
        {
            var eroded = new float[terrainResolution, terrainResolution];
            
            for (var iteration = 0; iteration < erosionIterations; iteration++)
            {
                System.Array.Copy(_heightMap, eroded, _heightMap.Length);
                
                for (var z = 1; z < terrainResolution - 1; z++)
                {
                    for (var x = 1; x < terrainResolution - 1; x++)
                    {
                        var currentHeight = _heightMap[z, x];
                        var maxHeightDiff = 0f;
                        int lowestX = x, lowestZ = z;
                        
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                var nx = x + dx;
                                var nz = z + dz;
                                
                                var heightDiff = currentHeight - _heightMap[nz, nx];
                                if (!(heightDiff > maxHeightDiff)) continue;
                                maxHeightDiff = heightDiff;
                                lowestX = nx;
                                lowestZ = nz;
                            }
                        }

                        if (!(maxHeightDiff > slopeThreshold)) continue;
                        var erodeAmount = (maxHeightDiff - slopeThreshold) * erosionRate;
                        eroded[z, x] -= erodeAmount;
                        eroded[lowestZ, lowestX] += erodeAmount * 0.5f;
                    }
                }
                
                System.Array.Copy(eroded, _heightMap, _heightMap.Length);
            }
        }

        private void ApplyHeightsToTerrain()
        {
            var normalizedHeights = new float[terrainResolution, terrainResolution];
            
            for (var z = 0; z < terrainResolution; z++)
            {
                for (var x = 0; x < terrainResolution; x++)
                {
                    normalizedHeights[z, x] = Mathf.Clamp01(_heightMap[z, x] / terrainHeight);
                }
            }
            
            _terrainData.SetHeights(0, 0, normalizedHeights);
        }
    }
}