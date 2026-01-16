using UnityEngine;
using System.Collections.Generic;

namespace Generation
{
    [System.Serializable]
    public class CanyonLayer
    {
        public string name;
        public float heightStart;
        public float heightEnd;
        public float hardness; // Resistance to erosion
        public Color debugColor;
    }

    [RequireComponent(typeof(Terrain))]
    public class CanyonGenerator : MonoBehaviour
    {
        [Header("Terrain Dimensions")]
        [SerializeField] private int terrainResolution = 513;
        [SerializeField] private float terrainWidth = 400f;
        [SerializeField] private float terrainLength = 1000f;
        [SerializeField] private float terrainHeight = 150f;
        
        [Header("Canyon Configuration")]
        [SerializeField] private float canyonDepth = 90f;
        [SerializeField] private float canyonWidth = 150f;
        [SerializeField] private float canyonFloorWidth = 80f;
        [SerializeField] private float riverWidth = 10f;
        [SerializeField] private float riverDepth = 3f;
        
        [Header("Canyon Path")]
        [SerializeField] private float canyonMeander = 20f;
        [SerializeField] private float meanderFrequency = 0.008f;
        [SerializeField] private Vector2 canyonStartOffset = new Vector2(0f, 0f);
        
        [Header("Plateau")]
        [SerializeField] private float plateauHeight = 130f;
        [SerializeField] private float plateauNoiseScale = 250f;
        [SerializeField] private float plateauNoiseHeight = 10f;
        [SerializeField] private int plateauOctaves = 3;
        
        [Header("Interior Plateaus (for Castles/Structures)")]
        [SerializeField] private int interiorPlateauCount = 3;
        [SerializeField] private float interiorPlateauMinSize = 30f;
        [SerializeField] private float interiorPlateauMaxSize = 60f;
        [SerializeField] private float interiorPlateauHeight = 40f;
        [SerializeField] private float interiorPlateauHeightVariation = 15f;
        
        [Header("Canyon Floor Terraces")]
        [SerializeField] private int floorTerraceCount = 2;
        [SerializeField] private float terraceHeight = 8f;
        [SerializeField] private float terraceWidth = 15f;
        
        [Header("Side Canyons")]
        [SerializeField] private int sideCanyonCount = 12;
        [SerializeField] private float sideCanyonLength = 40f;
        [SerializeField] private float sideCanyonWidth = 25f;
        [SerializeField] private float sideCanyonDepth = 30f;
        [SerializeField] private float sideCanyonBranchChance = 0.3f;
        
        [Header("Rock Layers (Stratification)")]
        [SerializeField] private CanyonLayer[] rockLayers = new CanyonLayer[]
        {
            new CanyonLayer { name = "Kaibab Limestone", heightStart = 120f, heightEnd = 150f, hardness = 0.9f, debugColor = new Color(0.9f, 0.9f, 0.85f) },
            new CanyonLayer { name = "Toroweap Formation", heightStart = 110f, heightEnd = 120f, hardness = 0.6f, debugColor = new Color(0.85f, 0.8f, 0.7f) },
            new CanyonLayer { name = "Coconino Sandstone", heightStart = 95f, heightEnd = 110f, hardness = 0.95f, debugColor = new Color(0.95f, 0.92f, 0.85f) },
            new CanyonLayer { name = "Hermit Shale", heightStart = 85f, heightEnd = 95f, hardness = 0.4f, debugColor = new Color(0.7f, 0.4f, 0.35f) },
            new CanyonLayer { name = "Supai Group", heightStart = 60f, heightEnd = 85f, hardness = 0.7f, debugColor = new Color(0.75f, 0.45f, 0.4f) },
            new CanyonLayer { name = "Redwall Limestone", heightStart = 45f, heightEnd = 60f, hardness = 0.95f, debugColor = new Color(0.65f, 0.55f, 0.5f) },
            new CanyonLayer { name = "Muav Limestone", heightStart = 30f, heightEnd = 45f, hardness = 0.8f, debugColor = new Color(0.6f, 0.55f, 0.5f) },
            new CanyonLayer { name = "Bright Angel Shale", heightStart = 20f, heightEnd = 30f, hardness = 0.5f, debugColor = new Color(0.5f, 0.6f, 0.55f) },
            new CanyonLayer { name = "Tapeats Sandstone", heightStart = 10f, heightEnd = 20f, hardness = 0.85f, debugColor = new Color(0.55f, 0.5f, 0.45f) },
            new CanyonLayer { name = "Vishnu Schist", heightStart = 0f, heightEnd = 10f, hardness = 1.0f, debugColor = new Color(0.3f, 0.3f, 0.35f) }
        };
        
        [Header("Layering Effects")]
        [SerializeField] private float layerSharpness = 0.8f;
        [SerializeField] private float layerNoiseScale = 50f;
        [SerializeField] private float layerNoiseStrength = 2f;
        
        [Header("Wall Erosion")]
        [SerializeField] private float wallRoughness = 6f;
        [SerializeField] private float wallDetailScale = 35f;
        [SerializeField] private int wallOctaves = 4;
        [SerializeField] private float wallSlopeReduction = 0.6f; // Makes walls more traversible
        
        [Header("Advanced Features")]
        [SerializeField] private bool enableTerracing = true;
        [SerializeField] private float terraceInterval = 12f;
        [SerializeField] private float terraceSharpness = 0.6f;
        [SerializeField] private bool enableSlumping = true;
        [SerializeField] private float slumpingStrength = 8f;
        
        [Header("General Settings")]
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2.0f;
        [SerializeField] private int seed;
        [SerializeField] private Vector2 noiseOffset = Vector2.zero;
    
        private Terrain _terrain;
        private TerrainData _terrainData;
        private float[,] _heightMap;
        private List<Vector2> _canyonPath;
        private List<SideCanyon> _sideCanyons;
        private List<InteriorPlateau> _interiorPlateaus;

        private class SideCanyon
        {
            public Vector2 StartPoint;
            public Vector2 Direction;
            public float Length;
            public float Width;
            public float Depth;
            public bool LeftSide;
        }

        private class InteriorPlateau
        {
            public Vector2 Position;
            public float Radius;
            public float Height;
        }

        public void GenerateTerrain()
        {
            Random.InitState(seed);
            
            _terrain = GetComponent<Terrain>();
            _terrainData = _terrain.terrainData;
            
            _terrainData.heightmapResolution = terrainResolution;
            _terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
            
            GenerateCanyonPath();
            GenerateSideCanyons();
            GenerateInteriorPlateaus();
            BuildCanyonHeightfield();
            ApplyHeightsToTerrain();
        }

        private void GenerateCanyonPath()
        {
            _canyonPath = new List<Vector2>();
            
            var steps = Mathf.CeilToInt(terrainLength / 3f);
            
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var z = t * terrainLength;
                
                var meander = Mathf.Sin(z * meanderFrequency + seed) * canyonMeander;
                meander += Mathf.Sin(z * meanderFrequency * 2.3f + seed + 100f) * canyonMeander * 0.4f;
                
                var x = terrainWidth * 0.5f + meander + canyonStartOffset.x;
                
                _canyonPath.Add(new Vector2(x, z));
            }
        }

        private void GenerateSideCanyons()
        {
            _sideCanyons = new List<SideCanyon>();
            
            var pathSegments = _canyonPath.Count - 1;
            var minSpacing = pathSegments / (sideCanyonCount + 1);
            
            for (var i = 0; i < sideCanyonCount; i++)
            {
                var segmentIndex = minSpacing * (i + 1) + Random.Range(-2, 3);
                segmentIndex = Mathf.Clamp(segmentIndex, 1, pathSegments - 1);
                
                var point = _canyonPath[segmentIndex];
                var nextPoint = _canyonPath[segmentIndex + 1];
                var mainDir = (nextPoint - point).normalized;
                
                var leftSide = Random.value > 0.5f;
                var perpDir = new Vector2(-mainDir.y, mainDir.x) * (leftSide ? 1f : -1f);
                
                var length = sideCanyonLength * Random.Range(0.6f, 1.2f);
                var width = sideCanyonWidth * Random.Range(0.7f, 1.1f);
                var depth = sideCanyonDepth * Random.Range(0.6f, 1.0f);
                
                _sideCanyons.Add(new SideCanyon
                {
                    StartPoint = point,
                    Direction = perpDir,
                    Length = length,
                    Width = width,
                    Depth = depth,
                    LeftSide = leftSide
                });
            }
        }

        private void GenerateInteriorPlateaus()
        {
            _interiorPlateaus = new List<InteriorPlateau>();
            
            var pathSegments = _canyonPath.Count - 1;
            var spacing = pathSegments / (interiorPlateauCount + 1);
            
            for (var i = 0; i < interiorPlateauCount; i++)
            {
                var segmentIndex = spacing * (i + 1) + Random.Range(-1, 2);
                segmentIndex = Mathf.Clamp(segmentIndex, 0, _canyonPath.Count - 1);
                
                var centerPos = _canyonPath[segmentIndex];
                
                // Offset slightly from center for variety
                var offset = new Vector2(
                    Random.Range(-canyonFloorWidth * 0.2f, canyonFloorWidth * 0.2f),
                    Random.Range(-20f, 20f)
                );
                
                var radius = Random.Range(interiorPlateauMinSize, interiorPlateauMaxSize);
                var height = interiorPlateauHeight + Random.Range(-interiorPlateauHeightVariation, interiorPlateauHeightVariation);
                
                _interiorPlateaus.Add(new InteriorPlateau
                {
                    Position = centerPos + offset,
                    Radius = radius,
                    Height = height
                });
            }
        }

        private void BuildCanyonHeightfield()
        {
            _heightMap = new float[terrainResolution, terrainResolution];
            
            for (var z = 0; z < terrainResolution; z++)
            {
                for (var x = 0; x < terrainResolution; x++)
                {
                    var worldX = (x / (float)(terrainResolution - 1)) * terrainWidth;
                    var worldZ = (z / (float)(terrainResolution - 1)) * terrainLength;
                    
                    _heightMap[z, x] = CalculateCanyonHeight(worldX, worldZ);
                }
            }
        }

        private float CalculateCanyonHeight(float worldX, float worldZ)
        {
            var pos = new Vector2(worldX, worldZ);
            
            // Get distance to main canyon
            var distToCanyon = GetDistanceToCanyonPath(pos, out var closestPoint, out var pathDirection);
            
            // Determine if we're on inside or outside a meander bend
            var toPos = (pos - closestPoint).normalized;
            var perpDir = new Vector2(-pathDirection.y, pathDirection.x);
            var bendSide = Vector2.Dot(toPos, perpDir);
            
            // Check side canyons
            var minSideCanyonDist = float.MaxValue;
            var depth = 0f;
            var width = 0f;
            var isLeftSide = false;
            
            foreach (var sideCanyon in _sideCanyons)
            {
                var distToSide = GetDistanceToSideCanyon(pos, sideCanyon, out var sideDepth);
                if (!(distToSide < minSideCanyonDist)) continue;
                minSideCanyonDist = distToSide;
                depth = sideDepth;
                width = sideCanyon.Width;
                isLeftSide = sideCanyon.LeftSide;
            }
            
            // Check interior plateaus
            var plateauEffect = CalculateInteriorPlateauEffect(pos, distToCanyon);
            
            // Start with plateau height
            var baseHeight = GeneratePlateauHeight(worldX, worldZ);
            
            // Apply main canyon carving with meander effects
            var mainCanyonEffect = CalculateMainCanyonEffect(distToCanyon, worldX, worldZ, bendSide);
            var height = baseHeight - mainCanyonEffect;
            
            // Apply floor terraces for roads/paths
            if (distToCanyon < canyonFloorWidth * 0.5f)
            {
                height = ApplyFloorTerraces(height, distToCanyon);
            }
            
            // Apply interior plateau
            height += plateauEffect;
            
            // Apply side canyon carving
            if (minSideCanyonDist < width * 2f)
            {
                var normalizedDist = minSideCanyonDist / (width * 2f);
                var asymmetryFactor = isLeftSide ? 0.7f : 1.3f;
                var adjustedDist = Mathf.Pow(normalizedDist, asymmetryFactor);
                
                var sideFalloff = Mathf.SmoothStep(1f, 0f, adjustedDist);
                height -= depth * sideFalloff;
            }
            
            // Apply stratification
            if (enableTerracing && distToCanyon > canyonFloorWidth * 0.5f)
            {
                height = ApplyStratification(height, worldX, worldZ);
            }
            
            // Apply wall details and erosion
            height += GenerateWallDetails(worldX, worldZ, distToCanyon);
            
            // Apply slumping for softer layers
            if (enableSlumping)
            {
                height = ApplySlumping(height, distToCanyon, worldX, worldZ);
            }
            
            return Mathf.Clamp(height, 0f, terrainHeight);
        }

        private float CalculateInteriorPlateauEffect(Vector2 pos, float distToCanyon)
        {
            // Only apply inside the canyon floor
            if (distToCanyon > canyonFloorWidth * 0.5f) return 0f;
            
            var maxEffect = 0f;
            
            foreach (var plateau in _interiorPlateaus)
            {
                var distToPlateau = Vector2.Distance(pos, plateau.Position);
                
                if (distToPlateau > plateau.Radius * 1.5f) continue;
                
                // Create a smooth mesa-like plateau
                float effect;
                if (distToPlateau < plateau.Radius * 0.7f)
                {
                    // Flat top
                    effect = plateau.Height;
                }
                else
                {
                    // Smooth falloff at edges
                    var edgeDist = (distToPlateau - plateau.Radius * 0.7f) / (plateau.Radius * 0.8f);
                    effect = plateau.Height * Mathf.SmoothStep(1f, 0f, edgeDist);
                }
                
                maxEffect = Mathf.Max(maxEffect, effect);
            }
            
            return maxEffect;
        }

        private float ApplyFloorTerraces(float height, float distToCanyon)
        {
            // Create stepped terraces on the canyon floor for roads/paths
            for (var i = 1; i <= floorTerraceCount; i++)
            {
                var terraceStart = (canyonFloorWidth * 0.5f) * (i / (float)(floorTerraceCount + 1));
                var terraceEnd = terraceStart + terraceWidth;
                
                if (distToCanyon >= terraceStart && distToCanyon < terraceEnd)
                {
                    var terraceT = (distToCanyon - terraceStart) / terraceWidth;
                    var smoothT = Mathf.SmoothStep(0f, 1f, terraceT);
                    
                    var targetHeight = plateauHeight - canyonDepth + (i * terraceHeight);
                    height = Mathf.Lerp(height, targetHeight, 1f - smoothT);
                    break;
                }
            }
            
            return height;
        }

        private float GetDistanceToCanyonPath(Vector2 pos, out Vector2 closestPoint, out Vector2 direction)
        {
            var minDist = float.MaxValue;
            closestPoint = _canyonPath[0];
            direction = Vector2.right;
            
            for (var i = 0; i < _canyonPath.Count - 1; i++)
            {
                var a = _canyonPath[i];
                var b = _canyonPath[i + 1];
                
                var ab = b - a;
                var ap = pos - a;
                var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab));
                var projection = a + ab * t;
                
                var dist = Vector2.Distance(pos, projection);
                if (!(dist < minDist)) continue;
                minDist = dist;
                closestPoint = projection;
                direction = ab.normalized;
            }
            
            return minDist;
        }

        private static float GetDistanceToSideCanyon(Vector2 pos, SideCanyon canyon, out float depth)
        {
            var toPoint = pos - canyon.StartPoint;
            var alongCanyon = Vector2.Dot(toPoint, canyon.Direction);
            
            depth = canyon.Depth;
            
            if (alongCanyon < 0f || alongCanyon > canyon.Length)
            {
                return float.MaxValue;
            }
            
            var projection = canyon.StartPoint + canyon.Direction * alongCanyon;
            var perpDistance = Vector2.Distance(pos, projection);
            
            // Taper depth along the side canyon
            var depthFactor = 1f - (alongCanyon / canyon.Length);
            depth *= depthFactor;
            
            return perpDistance;
        }

        private float GeneratePlateauHeight(float x, float z)
        {
            var height = plateauHeight;
            
            var amplitude = 1f;
            var frequency = 1f;
            
            for (var i = 0; i < plateauOctaves; i++)
            {
                var sampleX = (x + noiseOffset.x + seed) / plateauNoiseScale * frequency;
                var sampleZ = (z + noiseOffset.y + seed) / plateauNoiseScale * frequency;
                
                var noise = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                height += noise * plateauNoiseHeight * amplitude;
                
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return height;
        }

        private float CalculateMainCanyonEffect(float distToCanyon, float x, float z, float bendSide)
        {
            if (distToCanyon > canyonWidth * 2f) return 0f;
            
            float carvingDepth;
            
            // Add positional variation along the canyon length
            var lengthVariation = Mathf.PerlinNoise(
                (z + seed) / 120f,
                (x + seed) / 120f
            ) * 0.15f + 0.925f;
            
            // River channel at the bottom
            if (distToCanyon < riverWidth * 0.5f)
            {
                carvingDepth = (canyonDepth + riverDepth) * lengthVariation;
            }
            // Wide flat canyon floor
            else if (distToCanyon < canyonFloorWidth * 0.5f)
            {
                var t = (distToCanyon - riverWidth * 0.5f) / (canyonFloorWidth * 0.5f - riverWidth * 0.5f);
                // Keep floor mostly flat with subtle variation
                carvingDepth = Mathf.Lerp(canyonDepth + riverDepth, canyonDepth, Mathf.Pow(t, 0.5f)) * lengthVariation;
            }
            // Canyon walls - gentler slopes for traversibility
            else
            {
                var wallStart = canyonFloorWidth * 0.5f;
                var wallEnd = canyonWidth;
                var t = (distToCanyon - wallStart) / (wallEnd - wallStart);
                
                // Get the layer at this depth to determine hardness
                var currentDepth = canyonDepth * (1f - t);
                var layer = GetLayerAtHeight(plateauHeight - currentDepth);
                
                // Harder layers erode less, creating shelves
                var erosionFactor = 1f - (layer.hardness * 0.3f);
                
                // Meander effect
                var meanderEffect = 1f - (bendSide * 0.15f);
                
                // Gentler slope for traversibility
                var slopePower = 1.2f + (wallSlopeReduction * 0.8f);
                carvingDepth = canyonDepth * Mathf.Pow(1f - t, slopePower) * erosionFactor * lengthVariation * meanderEffect;
                
                if (carvingDepth < 0f) carvingDepth = 0f;
            }
            
            return carvingDepth;
        }

        private CanyonLayer GetLayerAtHeight(float height)
        {
            foreach (var layer in rockLayers)
            {
                if (height >= layer.heightStart && height < layer.heightEnd)
                {
                    return layer;
                }
            }
            return rockLayers[0];
        }

        private float ApplyStratification(float height, float x, float z)
        {
            // Add subtle variation to layer boundaries
            var layerNoise = Mathf.PerlinNoise(
                (x + seed) / layerNoiseScale,
                (z + seed) / layerNoiseScale
            ) * layerNoiseStrength;
            
            var adjustedHeight = height + layerNoise;
            
            // Create terracing effect
            var terraceLevel = Mathf.Floor(adjustedHeight / terraceInterval);
            var terraceBase = terraceLevel * terraceInterval;
            var withinTerrace = (adjustedHeight - terraceBase) / terraceInterval;
            
            // Sharp transitions for hard layers, softer for soft layers
            var layer = GetLayerAtHeight(height);
            var sharpness = terraceSharpness * layer.hardness;
            
            var smoothedWithin = Mathf.Pow(withinTerrace, 1f + sharpness * 2f);
            
            return terraceBase + smoothedWithin * terraceInterval;
        }

        private float GenerateWallDetails(float x, float z, float distToCanyon)
        {
            // Only apply detail to canyon walls
            if (distToCanyon < canyonFloorWidth * 0.5f || distToCanyon > canyonWidth * 1.5f)
            {
                return 0f;
            }
            
            var detail = 0f;
            var amplitude = wallRoughness;
            var frequency = 1f;
            
            for (var i = 0; i < wallOctaves; i++)
            {
                var sampleX = (x + seed + i * 1000f) / wallDetailScale * frequency;
                var sampleZ = (z + seed + i * 1000f) / wallDetailScale * frequency;
                
                var noise = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                detail += noise * amplitude;
                
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            // Reduce detail at edges
            var edgeFactor = Mathf.SmoothStep(0f, 1f, 
                (distToCanyon - canyonFloorWidth * 0.5f) / (canyonWidth * 0.5f));
            edgeFactor *= Mathf.SmoothStep(1f, 0f, 
                (distToCanyon - canyonWidth) / (canyonWidth * 0.5f));
            
            return detail * edgeFactor;
        }

        private float ApplySlumping(float height, float distToCanyon, float x, float z)
        {
            // Check if we're on a wall
            if (distToCanyon < canyonFloorWidth * 0.5f || distToCanyon > canyonWidth * 1.5f)
            {
                return height;
            }
            
            var layer = GetLayerAtHeight(height);
            
            // Soft layers slump more
            var slumpFactor = (1f - layer.hardness);
            
            if (slumpFactor < 0.3f) return height;
            
            var slumpNoise = Mathf.PerlinNoise(
                (x + seed + 5000f) / (wallDetailScale * 2f),
                (z + seed + 5000f) / (wallDetailScale * 2f)
            );
            
            var slump = slumpNoise * slumpingStrength * slumpFactor;
            
            // Slumping creates talus slopes at the base
            var wallPosition = (distToCanyon - canyonFloorWidth * 0.5f) / (canyonWidth * 0.5f);
            var talusEffect = Mathf.Exp(-wallPosition * 3f);
            
            return height - slump * talusEffect;
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

        // Editor helper to visualize canyon path
        private void OnDrawGizmosSelected()
        {
            if (_canyonPath == null || _canyonPath.Count == 0) return;
            
            // Main canyon path
            Gizmos.color = Color.red;
            for (var i = 0; i < _canyonPath.Count - 1; i++)
            {
                var start = new Vector3(_canyonPath[i].x, 50f, _canyonPath[i].y);
                var end = new Vector3(_canyonPath[i + 1].x, 50f, _canyonPath[i + 1].y);
                Gizmos.DrawLine(start, end);
            }
            
            // Side canyons
            if (_sideCanyons != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var sideCanyon in _sideCanyons)
                {
                    var start = new Vector3(sideCanyon.StartPoint.x, 50f, sideCanyon.StartPoint.y);
                    var end = start + new Vector3(sideCanyon.Direction.x, 0f, sideCanyon.Direction.y) * sideCanyon.Length;
                    Gizmos.DrawLine(start, end);
                }
            }
            
            // Interior plateaus
            if (_interiorPlateaus == null) return;
            {
                Gizmos.color = Color.cyan;
                foreach (var plateau in _interiorPlateaus)
                {
                    var center = new Vector3(plateau.Position.x, 50f, plateau.Position.y);
                    // Draw a circle to show plateau location
                    for (var i = 0; i < 16; i++)
                    {
                        var angle1 = i * 22.5f * Mathf.Deg2Rad;
                        var angle2 = (i + 1) * 22.5f * Mathf.Deg2Rad;
                        
                        var p1 = center + new Vector3(Mathf.Cos(angle1), 0f, Mathf.Sin(angle1)) * plateau.Radius;
                        var p2 = center + new Vector3(Mathf.Cos(angle2), 0f, Mathf.Sin(angle2)) * plateau.Radius;
                        
                        Gizmos.DrawLine(p1, p2);
                    }
                }
            }
        }
    }
}