using System.Linq;
using UnityEngine;

namespace Generation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private int terrainSize = 500;
        [SerializeField] private float cellSize = 1f;
    
        [Header("Base Heightfield")]
        [SerializeField] private float baseScale = 120f;
        [SerializeField] private float baseHeight = 20f;
        [SerializeField] private int baseOctaves = 3;
        
        [Header("Elevation Feature (Central Hill)")]
        [SerializeField] private Vector2 elevationCenter = new Vector2(250f, 280f);
        [SerializeField] private float elevationAmount = 18f;
        [SerializeField] private float elevationRadius = 90f;
        [SerializeField] private float elevationFalloff = 70f;
        
        [Header("Domain Warping (Organic Shapes)")]
        [SerializeField] private float warpStrength = 25f;
        [SerializeField] private float warpScale = 80f;
        
        [Header("Valley Flattening")]
        [SerializeField] private bool enableValleyFlattening = true;
        [SerializeField] private float valleyFloorHeight = 2f; // Height threshold for valleys
        [SerializeField] private float valleyFlattenStrength = 0.8f; // How flat (0-1)
        
        [Header("Peak Flattening")]
        [SerializeField] private bool enablePeakFlattening = true;
        [SerializeField] private float peakFlattenThreshold = 30f; // Height above which to flatten
        [SerializeField] private float peakFlattenStrength = 0.7f; // How flat (0-1)
        
        [Header("Erosion Simulation")]
        [SerializeField] private bool enableErosion = true;
        [SerializeField] private int erosionIterations = 5;
        [SerializeField] private float erosionRate = 0.3f;
        [SerializeField] private float slopeThreshold = 1.5f;
        
        [Header("Collapse (Scooping)")]
        [SerializeField] private bool enableCollapse = true;
        [SerializeField] private int collapseCount = 8;
        [SerializeField] private float collapseDepth = 8f;
        [SerializeField] private float collapseRadius = 30f;
        
        [Header("Rock Features")]
        [SerializeField] private bool enableRocks = true;
        [SerializeField] private int rockCount = 12;
        [SerializeField] private float rockHeight = 6f;
        [SerializeField] private float rockRadius = 15f;
        
        [Header("General Settings")]
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2.0f;
        [SerializeField] private int seed = 0;
        [SerializeField] private Vector2 noiseOffset = Vector2.zero;
    
        private Mesh _mesh;
        private Vector3[] _vertices;
        private int[] _triangles;
        private float[,] _heightMap;
        private Vector2[] _collapsePositions;
        private Vector2[] _rockPositions;

        public void GenerateTerrain()
        {
            Random.InitState(seed);
            
            _mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            GetComponent<MeshFilter>().mesh = _mesh;
            
            GenerateFeaturePositions();
            BuildHeightfield();
            
            if (enableErosion)
            {
                SimulateErosion();
            }
            
            ApplyFlattening();
            
            CreateMeshFromHeightMap();
            UpdateMesh();
        }

        private void GenerateFeaturePositions()
        {
            if (enableCollapse)
            {
                _collapsePositions = new Vector2[collapseCount];
                for (var i = 0; i < collapseCount; i++)
                {
                    var x = Random.Range(terrainSize * 0.2f, terrainSize * 0.8f);
                    var z = Random.Range(terrainSize * 0.2f, terrainSize * 0.8f);
                    _collapsePositions[i] = new Vector2(x, z);
                }
            }

            if (!enableRocks) return;
            {
                _rockPositions = new Vector2[rockCount];
                for (var i = 0; i < rockCount; i++)
                {
                    var x = Random.Range(terrainSize * 0.15f, terrainSize * 0.85f);
                    var z = Random.Range(terrainSize * 0.15f, terrainSize * 0.85f);
                    _rockPositions[i] = new Vector2(x, z);
                }
            }
        }

        private void BuildHeightfield()
        {
            _heightMap = new float[terrainSize + 1, terrainSize + 1];
            
            for (var z = 0; z <= terrainSize; z++)
            {
                for (var x = 0; x <= terrainSize; x++)
                {
                    var height = 0f;
                    
                    // Apply domain warping to position
                    var warpedPos = ApplyDomainWarp(x, z);
                    
                    // Base heightfield (accumulated)
                    height += GenerateNoise((int)warpedPos.x, (int)warpedPos.y, baseScale, baseHeight, baseOctaves);
                    
                    // Elevation feature (accumulated)
                    var pos = new Vector2(x, z);
                    var distToElevation = Vector2.Distance(pos, elevationCenter);
                    
                    if (distToElevation < elevationRadius)
                    {
                        height += elevationAmount;
                    }
                    else if (distToElevation < elevationRadius + elevationFalloff)
                    {
                        var falloff = (distToElevation - elevationRadius) / elevationFalloff;
                        falloff = Mathf.SmoothStep(0, 1, falloff);
                        height += elevationAmount * (1f - falloff);
                    }
                    
                    // Collapse features (subtract material)
                    if (enableCollapse && _collapsePositions != null)
                    {
                        height = (from collapsePos in _collapsePositions select Vector2.Distance(pos, collapsePos) into distToCollapse where distToCollapse < collapseRadius select 1f - (distToCollapse / collapseRadius) into collapseInfluence select Mathf.Pow(collapseInfluence, 1.5f)).Aggregate(height, (current, collapseInfluence) => current - collapseDepth * collapseInfluence);
                    }
                    
                    // Rock features (accumulated)
                    if (enableRocks && _rockPositions != null)
                    {
                        height += (from rockPos in _rockPositions select Vector2.Distance(pos, rockPos) into distToRock where distToRock < rockRadius select 1f - (distToRock / rockRadius) into rockInfluence select Mathf.Pow(rockInfluence, 1.2f) into rockInfluence let rockAdd = rockHeight * rockInfluence let embedding = Mathf.Lerp(0.7f, 1f, rockInfluence) select rockAdd * embedding).Sum();
                    }
                    
                    _heightMap[x, z] = height;
                }
            }
        }

        private Vector2 ApplyDomainWarp(int x, int z)
        {
            var warpX = Mathf.PerlinNoise((x + noiseOffset.x) / warpScale, (z + noiseOffset.y) / warpScale);
            var warpZ = Mathf.PerlinNoise((x + noiseOffset.x + 1000f) / warpScale, (z + noiseOffset.y + 1000f) / warpScale);
            
            warpX = (warpX * 2f - 1f) * warpStrength;
            warpZ = (warpZ * 2f - 1f) * warpStrength;
            
            return new Vector2(
                Mathf.Clamp(x + warpX, 0, terrainSize),
                Mathf.Clamp(z + warpZ, 0, terrainSize)
            );
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
                        var lowestX = x;
                        var lowestZ = z;
                        
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

        private void ApplyFlattening()
        {
            var flattened = new float[terrainSize + 1, terrainSize + 1];
            System.Array.Copy(_heightMap, flattened, _heightMap.Length);
            
            for (var z = 1; z < terrainSize; z++)
            {
                for (var x = 1; x < terrainSize; x++)
                {
                    var height = _heightMap[x, z];
                    
                    // Flatten valley floors
                    if (enableValleyFlattening && height < valleyFloorHeight)
                    {
                        // Average with neighbors to create flat valley floor
                        var sum = _heightMap[x, z];
                        var count = 1;
                        
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                var nx = x + dx;
                                var nz = z + dz;

                                if (nx < 0 || nx > terrainSize || nz < 0 || nz > terrainSize) continue;
                                if (!(_heightMap[nx, nz] < valleyFloorHeight)) continue;
                                sum += _heightMap[nx, nz];
                                count++;
                            }
                        }
                        
                        var avgHeight = sum / count;
                        flattened[x, z] = Mathf.Lerp(height, avgHeight, valleyFlattenStrength);
                    }
                    
                    // Flatten mountain peaks
                    if (!enablePeakFlattening || !(height > peakFlattenThreshold)) continue;
                    {
                        var sum = _heightMap[x, z];
                        var count = 1;
                        
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                var nx = x + dx;
                                var nz = z + dz;

                                if (nx < 0 || nx > terrainSize || nz < 0 || nz > terrainSize) continue;
                                if (!(_heightMap[nx, nz] > peakFlattenThreshold)) continue;
                                sum += _heightMap[nx, nz];
                                count++;
                            }
                        }
                        
                        var avgHeight = sum / count;
                        flattened[x, z] = Mathf.Lerp(height, avgHeight, peakFlattenStrength);
                    }
                }
            }
            
            System.Array.Copy(flattened, _heightMap, _heightMap.Length);
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
                    var xPos = x * cellSize;
                    var zPos = z * cellSize;
                    var height = _heightMap[x, z];
                
                    _vertices[vertIndex] = new Vector3(xPos, height, zPos);
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
                
                    _triangles[triIndex] = bottomLeft;
                    _triangles[triIndex + 1] = topLeft;
                    _triangles[triIndex + 2] = bottomRight;
                
                    _triangles[triIndex + 3] = bottomRight;
                    _triangles[triIndex + 4] = topLeft;
                    _triangles[triIndex + 5] = topRight;
                
                    triIndex += 6;
                }
            }
        }
        
        private float GenerateNoise(int x, int z, float scale, float heightMultiplier, int octaveCount)
        {
            if (scale <= 0.001f) return 0;
            
            float height = 0;
            float amplitude = 1;
            float frequency = 1;
        
            for (var i = 0; i < octaveCount; i++)
            {
                var sampleX = (x + noiseOffset.x) / scale * frequency;
                var sampleZ = (z + noiseOffset.y) / scale * frequency;
            
                var perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
                perlinValue = perlinValue * 2 - 1;
            
                height += perlinValue * amplitude;
            
                amplitude *= persistence;
                frequency *= lacunarity;
            }
        
            return height * heightMultiplier;
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