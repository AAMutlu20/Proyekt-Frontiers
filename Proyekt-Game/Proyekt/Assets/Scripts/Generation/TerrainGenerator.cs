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
                for (int i = 0; i < collapseCount; i++)
                {
                    float x = Random.Range(terrainSize * 0.2f, terrainSize * 0.8f);
                    float z = Random.Range(terrainSize * 0.2f, terrainSize * 0.8f);
                    _collapsePositions[i] = new Vector2(x, z);
                }
            }
            
            if (enableRocks)
            {
                _rockPositions = new Vector2[rockCount];
                for (int i = 0; i < rockCount; i++)
                {
                    float x = Random.Range(terrainSize * 0.15f, terrainSize * 0.85f);
                    float z = Random.Range(terrainSize * 0.15f, terrainSize * 0.85f);
                    _rockPositions[i] = new Vector2(x, z);
                }
            }
        }

        private void BuildHeightfield()
        {
            _heightMap = new float[terrainSize + 1, terrainSize + 1];
            
            for (int z = 0; z <= terrainSize; z++)
            {
                for (int x = 0; x <= terrainSize; x++)
                {
                    float height = 0f;
                    
                    // Apply domain warping to position
                    Vector2 warpedPos = ApplyDomainWarp(x, z);
                    
                    // Base heightfield (accumulated)
                    height += GenerateNoise((int)warpedPos.x, (int)warpedPos.y, baseScale, baseHeight, baseOctaves);
                    
                    // Elevation feature (accumulated)
                    Vector2 pos = new Vector2(x, z);
                    float distToElevation = Vector2.Distance(pos, elevationCenter);
                    
                    if (distToElevation < elevationRadius)
                    {
                        height += elevationAmount;
                    }
                    else if (distToElevation < elevationRadius + elevationFalloff)
                    {
                        float falloff = (distToElevation - elevationRadius) / elevationFalloff;
                        falloff = Mathf.SmoothStep(0, 1, falloff);
                        height += elevationAmount * (1f - falloff);
                    }
                    
                    // Collapse features (subtract material)
                    if (enableCollapse && _collapsePositions != null)
                    {
                        foreach (var collapsePos in _collapsePositions)
                        {
                            float distToCollapse = Vector2.Distance(pos, collapsePos);
                            if (distToCollapse < collapseRadius)
                            {
                                float collapseInfluence = 1f - (distToCollapse / collapseRadius);
                                collapseInfluence = Mathf.Pow(collapseInfluence, 1.5f);
                                height -= collapseDepth * collapseInfluence;
                            }
                        }
                    }
                    
                    // Rock features (accumulated)
                    if (enableRocks && _rockPositions != null)
                    {
                        foreach (var rockPos in _rockPositions)
                        {
                            float distToRock = Vector2.Distance(pos, rockPos);
                            if (distToRock < rockRadius)
                            {
                                float rockInfluence = 1f - (distToRock / rockRadius);
                                rockInfluence = Mathf.Pow(rockInfluence, 1.2f);
                                
                                float rockAdd = rockHeight * rockInfluence;
                                float embedding = Mathf.Lerp(0.7f, 1f, rockInfluence);
                                height += rockAdd * embedding;
                            }
                        }
                    }
                    
                    _heightMap[x, z] = height;
                }
            }
        }

        private Vector2 ApplyDomainWarp(int x, int z)
        {
            float warpX = Mathf.PerlinNoise((x + noiseOffset.x) / warpScale, (z + noiseOffset.y) / warpScale);
            float warpZ = Mathf.PerlinNoise((x + noiseOffset.x + 1000f) / warpScale, (z + noiseOffset.y + 1000f) / warpScale);
            
            warpX = (warpX * 2f - 1f) * warpStrength;
            warpZ = (warpZ * 2f - 1f) * warpStrength;
            
            return new Vector2(
                Mathf.Clamp(x + warpX, 0, terrainSize),
                Mathf.Clamp(z + warpZ, 0, terrainSize)
            );
        }

        private void SimulateErosion()
        {
            float[,] eroded = new float[terrainSize + 1, terrainSize + 1];
            
            for (int iteration = 0; iteration < erosionIterations; iteration++)
            {
                System.Array.Copy(_heightMap, eroded, _heightMap.Length);
                
                for (int z = 1; z < terrainSize; z++)
                {
                    for (int x = 1; x < terrainSize; x++)
                    {
                        float currentHeight = _heightMap[x, z];
                        
                        float maxHeightDiff = 0f;
                        int lowestX = x;
                        int lowestZ = z;
                        
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                int nx = x + dx;
                                int nz = z + dz;
                                
                                if (nx >= 0 && nx <= terrainSize && nz >= 0 && nz <= terrainSize)
                                {
                                    float heightDiff = currentHeight - _heightMap[nx, nz];
                                    if (heightDiff > maxHeightDiff)
                                    {
                                        maxHeightDiff = heightDiff;
                                        lowestX = nx;
                                        lowestZ = nz;
                                    }
                                }
                            }
                        }
                        
                        if (maxHeightDiff > slopeThreshold)
                        {
                            float erodeAmount = (maxHeightDiff - slopeThreshold) * erosionRate;
                            eroded[x, z] -= erodeAmount;
                            eroded[lowestX, lowestZ] += erodeAmount * 0.5f;
                        }
                    }
                }
                
                System.Array.Copy(eroded, _heightMap, _heightMap.Length);
            }
        }

        private void ApplyFlattening()
        {
            float[,] flattened = new float[terrainSize + 1, terrainSize + 1];
            System.Array.Copy(_heightMap, flattened, _heightMap.Length);
            
            for (int z = 1; z < terrainSize; z++)
            {
                for (int x = 1; x < terrainSize; x++)
                {
                    float height = _heightMap[x, z];
                    
                    // Flatten valley floors (low areas)
                    if (enableValleyFlattening && height < valleyFloorHeight)
                    {
                        // Average with neighbors to create flat valley floor
                        float sum = _heightMap[x, z];
                        int count = 1;
                        
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                int nx = x + dx;
                                int nz = z + dz;
                                
                                if (nx >= 0 && nx <= terrainSize && nz >= 0 && nz <= terrainSize)
                                {
                                    if (_heightMap[nx, nz] < valleyFloorHeight)
                                    {
                                        sum += _heightMap[nx, nz];
                                        count++;
                                    }
                                }
                            }
                        }
                        
                        float avgHeight = sum / count;
                        flattened[x, z] = Mathf.Lerp(height, avgHeight, valleyFlattenStrength);
                    }
                    
                    // Flatten mountain peaks (high areas)
                    if (enablePeakFlattening && height > peakFlattenThreshold)
                    {
                        float sum = _heightMap[x, z];
                        int count = 1;
                        
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                int nx = x + dx;
                                int nz = z + dz;
                                
                                if (nx >= 0 && nx <= terrainSize && nz >= 0 && nz <= terrainSize)
                                {
                                    if (_heightMap[nx, nz] > peakFlattenThreshold)
                                    {
                                        sum += _heightMap[nx, nz];
                                        count++;
                                    }
                                }
                            }
                        }
                        
                        float avgHeight = sum / count;
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
                    float height = _heightMap[x, z];
                
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