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
        [SerializeField] private float terrainLength = 2000f;
        [SerializeField] private float terrainHeight = 250f;
        
        [Header("Canyon Configuration")]
        [SerializeField] private float canyonDepth = 125f;
        [SerializeField] private float canyonWidth = 150f;
        [SerializeField] private float canyonFloorWidth = 80f;
        [SerializeField] private float riverWidth = 10f;
        [SerializeField] private float riverDepth = 3f;
        
        [Header("Canyon Path")]
        [SerializeField] private float canyonMeander = 20f;
        [SerializeField] private float meanderFrequency = 0.008f;
        [SerializeField] private Vector2 canyonStartOffset = new(0f, 0f);
        
        [Header("End Zone (Canyon Closes)")]
        [SerializeField] private bool enableEndZone = true;
        [SerializeField] private float endZoneRadius = 150f; // How far before end to start closing
        [SerializeField] private float endZoneWallThickness = 60f; // Thickness of the closing wall
        [SerializeField] private float endZoneTransitionLength = 200f; // Transition zone length
        
        [Header("Plateau")]
        [SerializeField] private float plateauHeight = 130f;
        [SerializeField] private float plateauNoiseScale = 300f;
        [SerializeField] private float plateauNoiseHeight = 6f;
        [SerializeField] private int plateauOctaves = 2;
        
        [Header("Interior Plateaus (for Castles/Structures)")]
        [SerializeField] private int interiorPlateauCount;
        [SerializeField] private float interiorPlateauMinSize = 20f;
        [SerializeField] private float interiorPlateauMaxSize = 35f;
        [SerializeField] private float interiorPlateauHeight = 15f;
        [SerializeField] private float interiorPlateauHeightVariation = 5f;
        
        [Header("Canyon Floor Terraces")]
        [SerializeField] private int floorTerraceCount;
        [SerializeField] private float terraceHeight = 8f;
        [SerializeField] private float terraceWidth = 15f;
        
        [Header("Side Canyons")]
        [SerializeField] private int sideCanyonCount = 12;
        [SerializeField] private float sideCanyonLength = 40f;
        [SerializeField] private float sideCanyonWidth = 25f;
        [SerializeField] private float sideCanyonDepth = 30f;
        [SerializeField] private float sideCanyonBranchChance = 0.3f;
        
        [Header("Rock Layers (Stratification)")]
        [SerializeField] private CanyonLayer[] rockLayers = {
            new() { name = "Kaibab Limestone", heightStart = 120f, heightEnd = 150f, hardness = 0.9f, debugColor = new Color(0.9f, 0.9f, 0.85f) },
            new() { name = "Toroweap Formation", heightStart = 110f, heightEnd = 120f, hardness = 0.6f, debugColor = new Color(0.85f, 0.8f, 0.7f) },
            new() { name = "Coconino Sandstone", heightStart = 95f, heightEnd = 110f, hardness = 0.95f, debugColor = new Color(0.95f, 0.92f, 0.85f) },
            new() { name = "Hermit Shale", heightStart = 85f, heightEnd = 95f, hardness = 0.4f, debugColor = new Color(0.7f, 0.4f, 0.35f) },
            new() { name = "Supai Group", heightStart = 60f, heightEnd = 85f, hardness = 0.7f, debugColor = new Color(0.75f, 0.45f, 0.4f) },
            new() { name = "Redwall Limestone", heightStart = 45f, heightEnd = 60f, hardness = 0.95f, debugColor = new Color(0.65f, 0.55f, 0.5f) },
            new() { name = "Muav Limestone", heightStart = 30f, heightEnd = 45f, hardness = 0.8f, debugColor = new Color(0.6f, 0.55f, 0.5f) },
            new() { name = "Bright Angel Shale", heightStart = 20f, heightEnd = 30f, hardness = 0.5f, debugColor = new Color(0.5f, 0.6f, 0.55f) },
            new() { name = "Tapeats Sandstone", heightStart = 10f, heightEnd = 20f, hardness = 0.85f, debugColor = new Color(0.55f, 0.5f, 0.45f) },
            new() { name = "Vishnu Schist", heightStart = 0f, heightEnd = 10f, hardness = 1.0f, debugColor = new Color(0.3f, 0.3f, 0.35f) }
        };
        
        [Header("Layering Effects")]
        [SerializeField] private float layerSharpness = 0.8f;
        [SerializeField] private float layerNoiseScale = 100f;
        [SerializeField] private float layerNoiseStrength = 0.5f;
        
        [Header("Wall Erosion")]
        [SerializeField] private float wallRoughness = 2f;
        [SerializeField] private float wallDetailScale = 80f;
        [SerializeField] private int wallOctaves = 3;
        [SerializeField] private float wallSlopeReduction = 0.6f;
        
        [Header("Advanced Features")]
        [SerializeField] private bool enableTerracing = true;
        [SerializeField] private float terraceInterval = 20f;
        [SerializeField] private float terraceSharpness = 0.3f;
        [SerializeField] private bool enableSlumping = true;
        [SerializeField] private float slumpingStrength = 4f;
        
        [Header("Gameplay Features")]
        [SerializeField] private bool enableGameplayFlattening = true;
        [SerializeField] private float floorFlatness = 0.95f;
        [SerializeField] private int buildableZoneCount = 8;
        [SerializeField] private float buildableZoneRadius = 25f;
        [SerializeField] private float buildableZoneHeight = 2f;
        [SerializeField] private bool addAccessRamps = true;
        [SerializeField] private float rampWidth = 30f;
        [SerializeField] private int rampCount = 6;
        
        [Header("General Settings")]
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2.0f;
        [SerializeField] private int seed;
        [SerializeField] private Vector2 noiseOffset = Vector2.zero;
    
        private Terrain _terrain;
        private TerrainData _terrainData;
        private float[,] _heightMap;
        private List<Vector2> _canyonPath;
        private List<Vector2> _riverPath;
        private List<SideCanyon> _sideCanyons;
        private List<InteriorPlateau> _interiorPlateaus;
        private List<Vector2> _buildableZones;
        private List<RampData> _accessRamps;
        private Vector2 _endZoneCenter;

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

        private class RampData
        {
            public Vector2 StartPoint;
            public Vector2 EndPoint;
            public float Width;
        }

        public void GenerateTerrain()
        {
            Random.InitState(seed);
            
            _terrain = GetComponent<Terrain>();
            _terrainData = _terrain.terrainData;
            
            _terrainData.heightmapResolution = terrainResolution;
            _terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
            
            GenerateCanyonPath();
            GenerateRiverPath();
            CalculateEndZoneCenter(); // Calculate this early so other features can avoid the end zone
            GenerateSideCanyons();
            GenerateInteriorPlateaus();
            GenerateBuildableZones();
            GenerateAccessRamps();
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
                
                // Add curving toward center at the end if end zone is enabled
                if (enableEndZone)
                {
                    var endZoneStartZ = terrainLength - endZoneRadius - 200f;
                    
                    if (z > endZoneStartZ)
                    {
                        var curveT = (z - endZoneStartZ) / 200f;
                        curveT = Mathf.Clamp01(curveT);
                        curveT = Mathf.SmoothStep(0f, 1f, curveT);
                        
                        // Curve back toward center
                        var targetX = terrainWidth * 0.5f;
                        x = Mathf.Lerp(x, targetX, curveT * 0.7f);
                    }
                }
                
                _canyonPath.Add(new Vector2(x, z));
            }
        }

        private void GenerateRiverPath()
        {
            _riverPath = new List<Vector2>();
            
            var steps = Mathf.CeilToInt(terrainLength / 2f);
            
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var z = t * terrainLength;
                
                var riverMeander = Mathf.Sin(z * meanderFrequency * 3f + seed + 200f) * (canyonFloorWidth * 0.25f);
                riverMeander += Mathf.Sin(z * meanderFrequency * 5.5f + seed + 300f) * (canyonFloorWidth * 0.12f);
                
                var x = terrainWidth * 0.5f + riverMeander;
                
                _riverPath.Add(new Vector2(x, z));
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
                
                // Don't add side canyons near the end zone
                if (enableEndZone && _endZoneCenter != Vector2.zero)
                {
                    var distToEndZone = Vector2.Distance(point, _endZoneCenter);
                    if (distToEndZone < endZoneRadius + 100f)
                        continue;
                }
                
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
                
                // Don't add plateaus near the end zone
                if (enableEndZone && _endZoneCenter != Vector2.zero)
                {
                    var distToEndZone = Vector2.Distance(centerPos, _endZoneCenter);
                    if (distToEndZone < endZoneRadius + 100f)
                        continue;
                }
                
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

        private void GenerateBuildableZones()
        {
            _buildableZones = new List<Vector2>();
            
            var pathSegments = _canyonPath.Count - 1;
            var spacing = pathSegments / (buildableZoneCount + 1);
            
            for (var i = 0; i < buildableZoneCount; i++)
            {
                var segmentIndex = spacing * (i + 1);
                segmentIndex = Mathf.Clamp(segmentIndex, 0, _canyonPath.Count - 1);
                
                var centerPos = _canyonPath[segmentIndex];
                
                // Don't add buildable zones near the end zone
                if (enableEndZone && _endZoneCenter != Vector2.zero)
                {
                    var distToEndZone = Vector2.Distance(centerPos, _endZoneCenter);
                    if (distToEndZone < endZoneRadius + 100f)
                        continue;
                }
                
                var side = (i % 2 == 0) ? 1f : -1f;
                var offset = new Vector2(
                    side * (canyonFloorWidth * 0.3f),
                    Random.Range(-15f, 15f)
                );
                
                _buildableZones.Add(centerPos + offset);
            }
        }

        private void GenerateAccessRamps()
        {
            _accessRamps = new List<RampData>();
            
            var pathSegments = _canyonPath.Count - 1;
            var spacing = pathSegments / (rampCount + 1);
            
            for (var i = 0; i < rampCount; i++)
            {
                var segmentIndex = spacing * (i + 1);
                segmentIndex = Mathf.Clamp(segmentIndex, 1, pathSegments - 1);
                
                var floorPoint = _canyonPath[segmentIndex];
                var nextPoint = _canyonPath[segmentIndex + 1];
                var mainDir = (nextPoint - floorPoint).normalized;
                
                // Don't add ramps near the end zone
                if (enableEndZone && _endZoneCenter != Vector2.zero)
                {
                    var distToEndZone = Vector2.Distance(floorPoint, _endZoneCenter);
                    if (distToEndZone < endZoneRadius + 100f)
                        continue;
                }
                
                var leftSide = (i % 2 == 0);
                var perpDir = new Vector2(-mainDir.y, mainDir.x) * (leftSide ? 1f : -1f);
                
                var rampStart = floorPoint + perpDir * (canyonFloorWidth * 0.4f);
                var rampEnd = floorPoint + perpDir * (canyonWidth * 0.7f);
                
                _accessRamps.Add(new RampData
                {
                    StartPoint = rampStart,
                    EndPoint = rampEnd,
                    Width = rampWidth
                });
            }
        }

        private void CalculateEndZoneCenter()
        {
            if (!enableEndZone || _canyonPath.Count == 0)
            {
                _endZoneCenter = Vector2.zero;
                return;
            }
            
            // Position at the very end of the canyon with some buffer
            _endZoneCenter = _canyonPath[^1];
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
            
            // Declare ALL shared variables once at the top
            float distToCanyon;
            Vector2 closestPoint;
            Vector2 pathDirection;
            float distToRiver;
            Vector2 toPos;
            Vector2 perpDir;
            float bendSide;
            float baseHeight;
            float mainCanyonEffect;

            // Regular canyon generation
            distToCanyon = GetDistanceToCanyonPath(pos, out closestPoint, out pathDirection);
            distToRiver = GetDistanceToRiverPath(pos);
            
            // Check if we're past the end of the canyon - create closing wall
            if (enableEndZone && _endZoneCenter != Vector2.zero)
            {
                var distPastEnd = worldZ - _endZoneCenter.y;
                
                if (distPastEnd > 0f && distPastEnd < 80f)
                {
                    // We're in the end wall zone
                    var wallT = distPastEnd / 80f;
                    
                    // Get canyon calculation for smooth blending
                    toPos = (pos - closestPoint).normalized;
                    perpDir = new Vector2(-pathDirection.y, pathDirection.x);
                    bendSide = Vector2.Dot(toPos, perpDir);
                    
                    baseHeight = GeneratePlateauHeight(worldX, worldZ);
                    mainCanyonEffect = CalculateMainCanyonEffect(distToCanyon, distToRiver, bendSide, worldZ);
                    var canyonHeight = baseHeight - mainCanyonEffect;
                    
                    // Blend from canyon floor to plateau height
                    var targetHeight = Mathf.Lerp(plateauHeight - canyonDepth, plateauHeight, Mathf.SmoothStep(0f, 1f, wallT));
                    
                    // Blend with canyon walls smoothly
                    if (distToCanyon < canyonWidth * 0.5f)
                    {
                        return Mathf.Lerp(canyonHeight, targetHeight, Mathf.SmoothStep(0f, 1f, wallT));
                    }
                    else
                    {
                        return targetHeight;
                    }
                }
                else if (distPastEnd >= 80f)
                {
                    // Past the wall - just plateau
                    return plateauHeight;
                }
            }
            
            toPos = (pos - closestPoint).normalized;
            perpDir = new Vector2(-pathDirection.y, pathDirection.x);
            bendSide = Vector2.Dot(toPos, perpDir);
            
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
            
            var plateauEffect = CalculateInteriorPlateauEffect(pos, distToCanyon);
            baseHeight = GeneratePlateauHeight(worldX, worldZ);
            mainCanyonEffect = CalculateMainCanyonEffect(distToCanyon, distToRiver, bendSide, worldZ);
            var height = baseHeight - mainCanyonEffect;
            
            height = ApplyGameplayFlattening(height, distToCanyon, distToRiver);
            
            if (distToCanyon < canyonFloorWidth * 0.5f)
            {
                height = ApplyFloorTerraces(height, distToCanyon);
            }
            
            if (distToCanyon < canyonFloorWidth * 0.5f)
            {
                height += CalculateBuildableZoneEffect(pos);
            }
            
            height += plateauEffect;
            height = ApplyAccessRamp(height, pos);
            
            if (minSideCanyonDist < width * 2f && distToCanyon > canyonFloorWidth * 0.5f)
            {
                var normalizedDist = minSideCanyonDist / (width * 2f);
                var asymmetryFactor = isLeftSide ? 0.7f : 1.3f;
                var adjustedDist = Mathf.Pow(normalizedDist, asymmetryFactor);
                
                var sideFalloff = Mathf.SmoothStep(1f, 0f, adjustedDist);
                height -= depth * sideFalloff;
            }
            
            if (enableTerracing && distToCanyon > canyonFloorWidth * 0.5f)
            {
                height = ApplyStratification(height, worldX, worldZ);
            }
            
            height += GenerateWallDetails(worldX, worldZ, distToCanyon);
            
            if (enableSlumping)
            {
                height = ApplySlumping(height, distToCanyon, worldX, worldZ);
            }
            
            return Mathf.Clamp(height, 0f, terrainHeight);
        }

        private float CalculateInteriorPlateauEffect(Vector2 pos, float distToCanyon)
        {
            if (distToCanyon > canyonFloorWidth * 0.5f) return 0f;
            
            var maxEffect = 0f;
            
            foreach (var plateau in _interiorPlateaus)
            {
                var distToPlateau = Vector2.Distance(pos, plateau.Position);
                
                if (distToPlateau > plateau.Radius * 2.0f) continue;
                
                float effect;
                if (distToPlateau < plateau.Radius * 0.5f)
                {
                    effect = plateau.Height;
                }
                else
                {
                    var edgeDist = (distToPlateau - plateau.Radius * 0.5f) / (plateau.Radius * 1.5f);
                    effect = plateau.Height * Mathf.SmoothStep(1f, 0f, Mathf.Pow(edgeDist, 0.5f));
                }
                
                maxEffect = Mathf.Max(maxEffect, effect);
            }
            
            return maxEffect;
        }

        private float ApplyFloorTerraces(float height, float distToCanyon)
        {
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

        private float CalculateBuildableZoneEffect(Vector2 pos)
        {
            var maxEffect = 0f;
            
            foreach (var zone in _buildableZones)
            {
                var distToZone = Vector2.Distance(pos, zone);
                
                if (distToZone > buildableZoneRadius * 1.5f) continue;
                
                float effect;
                if (distToZone < buildableZoneRadius * 0.7f)
                {
                    effect = buildableZoneHeight;
                }
                else
                {
                    var edgeDist = (distToZone - buildableZoneRadius * 0.7f) / (buildableZoneRadius * 0.8f);
                    effect = buildableZoneHeight * Mathf.SmoothStep(1f, 0f, edgeDist);
                }
                
                maxEffect = Mathf.Max(maxEffect, effect);
            }
            
            return maxEffect;
        }

        private float ApplyAccessRamp(float height, Vector2 pos)
        {
            if (!addAccessRamps) return height;
            
            foreach (var ramp in _accessRamps)
            {
                var rampDir = (ramp.EndPoint - ramp.StartPoint).normalized;
                var rampLength = Vector2.Distance(ramp.StartPoint, ramp.EndPoint);
                
                var toPoint = pos - ramp.StartPoint;
                var alongRamp = Vector2.Dot(toPoint, rampDir);
                
                if (alongRamp < 0f || alongRamp > rampLength) continue;
                
                var projection = ramp.StartPoint + rampDir * alongRamp;
                var perpDist = Vector2.Distance(pos, projection);
                
                if (perpDist > ramp.Width * 0.5f) continue;
                
                var rampProgress = alongRamp / rampLength;
                var floorHeight = plateauHeight - canyonDepth;
                var wallHeight = plateauHeight - (canyonDepth * 0.3f);
                
                var rampHeight = Mathf.Lerp(floorHeight, wallHeight, Mathf.SmoothStep(0f, 1f, rampProgress));
                var edgeFactor = Mathf.SmoothStep(0f, 1f, 1f - (perpDist / (ramp.Width * 0.5f)));
                
                return Mathf.Lerp(height, rampHeight, edgeFactor);
            }
            
            return height;
        }

        private float ApplyGameplayFlattening(float height, float distToCanyon, float distToRiver)
        {
            if (!enableGameplayFlattening) return height;
            
            if (distToCanyon > canyonFloorWidth * 0.5f) return height;
            
            var flatFloorHeight = plateauHeight - canyonDepth;
            
            if (distToRiver < riverWidth * 2f) return height;
            
            return Mathf.Lerp(height, flatFloorHeight, floorFlatness);
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

        private float GetDistanceToRiverPath(Vector2 pos)
        {
            var minDist = float.MaxValue;
            
            for (var i = 0; i < _riverPath.Count - 1; i++)
            {
                var a = _riverPath[i];
                var b = _riverPath[i + 1];
                
                var ab = b - a;
                var ap = pos - a;
                var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab));
                var projection = a + ab * t;
                
                var dist = Vector2.Distance(pos, projection);
                if (dist < minDist)
                {
                    minDist = dist;
                }
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

        private float CalculateMainCanyonEffect(float distToCanyon, float distToRiver, float bendSide, float worldZ)
        {
            // Check if we're near the end zone and should narrow/curve the canyon
            float effectiveWidth = canyonWidth;
            float effectiveFloorWidth = canyonFloorWidth;
            
            if (enableEndZone && _endZoneCenter != Vector2.zero)
            {
                var endZoneStart = _endZoneCenter.y - endZoneRadius - 150f;
                var endZoneTransition = 200f;
                
                // If we're in the narrowing zone
                if (worldZ > endZoneStart)
                {
                    var narrowingT = (worldZ - endZoneStart) / endZoneTransition;
                    narrowingT = Mathf.Clamp01(narrowingT);
                    narrowingT = Mathf.SmoothStep(0f, 1f, narrowingT);
                    
                    // Narrow the canyon smoothly to create the closing effect
                    var targetWidth = canyonWidth * 0.25f;
                    var targetFloorWidth = canyonFloorWidth * 0.35f;
                    
                    effectiveWidth = Mathf.Lerp(canyonWidth, targetWidth, narrowingT);
                    effectiveFloorWidth = Mathf.Lerp(canyonFloorWidth, targetFloorWidth, narrowingT);
                }
            }
            
            if (distToCanyon > effectiveWidth * 2f) return 0f;
            
            float carvingDepth;
            
            if (distToCanyon < effectiveFloorWidth * 0.5f)
            {
                carvingDepth = canyonDepth;
                
                if (distToRiver < riverWidth * 1.5f)
                {
                    var riverT = Mathf.Clamp01(distToRiver / (riverWidth * 1.5f));
                    var riverDepthEffect = riverDepth * Mathf.SmoothStep(1f, 0f, riverT);
                    carvingDepth += riverDepthEffect;
                    
                    var bankEffect = Mathf.Sin(riverT * Mathf.PI) * 1.2f;
                    carvingDepth -= bankEffect;
                }
            }
            else
            {
                var wallStart = effectiveFloorWidth * 0.5f;
                var wallEnd = effectiveWidth;
                var t = (distToCanyon - wallStart) / (wallEnd - wallStart);
                
                var currentDepth = canyonDepth * (1f - t);
                var layer = GetLayerAtHeight(plateauHeight - currentDepth);
                
                var erosionFactor = 1f - (layer.hardness * 0.15f);
                var meanderEffect = 1f - (bendSide * 0.08f);
                
                var slopePower = 1.2f + (wallSlopeReduction * 0.8f);
                carvingDepth = canyonDepth * Mathf.Pow(1f - t, slopePower) * erosionFactor * meanderEffect;
                
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
            var layerNoise = Mathf.PerlinNoise(
                (x + seed) / layerNoiseScale,
                (z + seed) / layerNoiseScale
            ) * layerNoiseStrength;
            
            var adjustedHeight = height + layerNoise;
            
            var terraceLevel = Mathf.Floor(adjustedHeight / terraceInterval);
            var terraceBase = terraceLevel * terraceInterval;
            var withinTerrace = (adjustedHeight - terraceBase) / terraceInterval;
            
            var layer = GetLayerAtHeight(height);
            var sharpness = terraceSharpness * layer.hardness;
            
            var smoothedWithin = Mathf.Pow(withinTerrace, 1f + sharpness * 2f);
            
            return terraceBase + smoothedWithin * terraceInterval;
        }

        private float GenerateWallDetails(float x, float z, float distToCanyon)
        {
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
            
            var edgeFactor = Mathf.SmoothStep(0f, 1f, 
                (distToCanyon - canyonFloorWidth * 0.5f) / (canyonWidth * 0.5f));
            edgeFactor *= Mathf.SmoothStep(1f, 0f, 
                (distToCanyon - canyonWidth) / (canyonWidth * 0.5f));
            
            return detail * edgeFactor;
        }

        private float ApplySlumping(float height, float distToCanyon, float x, float z)
        {
            if (distToCanyon < canyonFloorWidth * 0.5f || distToCanyon > canyonWidth * 1.5f)
            {
                return height;
            }
            
            var layer = GetLayerAtHeight(height);
            var slumpFactor = (1f - layer.hardness);
            
            if (slumpFactor < 0.3f) return height;
            
            var slumpNoise = Mathf.PerlinNoise(
                (x + seed + 5000f) / (wallDetailScale * 2f),
                (z + seed + 5000f) / (wallDetailScale * 2f)
            );
            
            var slump = slumpNoise * slumpingStrength * slumpFactor;
            
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

        private void OnDrawGizmosSelected()
        {
            if (_canyonPath == null || _canyonPath.Count == 0) return;
            
            // Main canyon path (red)
            Gizmos.color = Color.red;
            for (var i = 0; i < _canyonPath.Count - 1; i++)
            {
                var start = new Vector3(_canyonPath[i].x, 50f, _canyonPath[i].y);
                var end = new Vector3(_canyonPath[i + 1].x, 50f, _canyonPath[i + 1].y);
                Gizmos.DrawLine(start, end);
            }
            
            // River path (green)
            if (_riverPath is { Count: > 0 })
            {
                Gizmos.color = Color.green;
                for (var i = 0; i < _riverPath.Count - 1; i++)
                {
                    var start = new Vector3(_riverPath[i].x, 50f, _riverPath[i].y);
                    var end = new Vector3(_riverPath[i + 1].x, 50f, _riverPath[i + 1].y);
                    Gizmos.DrawLine(start, end);
                }
            }
            
            // Side canyons (yellow)
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
            
            // Interior plateaus (cyan)
            if (_interiorPlateaus != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var plateau in _interiorPlateaus)
                {
                    var center = new Vector3(plateau.Position.x, 50f, plateau.Position.y);
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
            
            // Buildable zones (magenta)
            if (_buildableZones != null)
            {
                Gizmos.color = Color.magenta;
                foreach (var zone in _buildableZones)
                {
                    var center = new Vector3(zone.x, 50f, zone.y);
                    for (var i = 0; i < 16; i++)
                    {
                        var angle1 = i * 22.5f * Mathf.Deg2Rad;
                        var angle2 = (i + 1) * 22.5f * Mathf.Deg2Rad;
                        
                        var p1 = center + new Vector3(Mathf.Cos(angle1), 0f, Mathf.Sin(angle1)) * buildableZoneRadius;
                        var p2 = center + new Vector3(Mathf.Cos(angle2), 0f, Mathf.Sin(angle2)) * buildableZoneRadius;
                        
                        Gizmos.DrawLine(p1, p2);
                    }
                }
            }
            
            // Access ramps (white)
            if (_accessRamps != null)
            {
                Gizmos.color = Color.white;
                foreach (var ramp in _accessRamps)
                {
                    var start = new Vector3(ramp.StartPoint.x, 50f, ramp.StartPoint.y);
                    var end = new Vector3(ramp.EndPoint.x, 50f, ramp.EndPoint.y);
                    Gizmos.DrawLine(start, end);
                }
            }
            
            // End zone closing point (blue)
            if (enableEndZone && _endZoneCenter != Vector2.zero)
            {
                var center = new Vector3(_endZoneCenter.x, 50f, _endZoneCenter.y);
                
                // Draw the end point
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(center, 20f);
                
                // Draw the narrowing zone
                Gizmos.color = Color.cyan;
                var startZ = _endZoneCenter.y - endZoneRadius - 150f;
                var startPoint = new Vector3(terrainWidth * 0.5f, 50f, startZ);
                Gizmos.DrawLine(startPoint, center);
            }
        }
    }
}