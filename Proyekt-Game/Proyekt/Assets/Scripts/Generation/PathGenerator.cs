using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    public class PathGenerator : MonoBehaviour
    {
        [Header("Path Settings")]
        public Terrain terrain;
        public Transform startPoint;
        public Transform endPoint;
        public int numberOfSwitchbacks = 3;
        public float pathWidth = 4f;
        public float switchbackOffset = 10f; // How far each zigzag goes
    
        [Header("Terrain Painting")]
        public int pathTextureIndex = 4; // Index of RockyTrail or SmallRocks texture
        public float textureBlendStrength = 0.8f;
    
        [Header("Steps")]
        public bool createSteps = true;
        public float stepHeight = 0.3f;
        public float stepDepth = 2f;
    
        [Header("Cobblestone Meshes (Optional)")]
        public GameObject cobblestonePrefab;
        public float cobblestoneSpacing = 1.5f;
        public bool useMeshCobblestones = true;
    
        [Header("Waypoints")]
        public Transform waypointParent;
    
        private readonly List<Vector3> _pathPoints = new();

        private void Start()
        {
            GenerateSwitchbackPath();
        }

        public void GenerateSwitchbackPath()
        {
            if (terrain == null || startPoint == null || endPoint == null)
            {
                Debug.LogError("Please assign Terrain, Start Point, and End Point!");
                return;
            }
            
            _pathPoints.Clear();
            
            // Calculate switchback points
            var start = startPoint.position;
            var end = endPoint.position;
            
            // Calculate the forward direction (ignoring Y for horizontal movement)
            var horizontalStart = new Vector3(start.x, 0, start.z);
            var horizontalEnd = new Vector3(end.x, 0, end.z);
            var forwardDirection = (horizontalEnd - horizontalStart).normalized;
            
            // Calculate perpendicular direction for zigzagging
            var sideDirection = Vector3.Cross(Vector3.up, forwardDirection);
            
            var totalHeight = end.y - start.y;
            var horizontalDistance = Vector3.Distance(horizontalStart, horizontalEnd);
            
            var heightPerSection = totalHeight / (numberOfSwitchbacks + 1);
            var forwardPerSection = horizontalDistance / (numberOfSwitchbacks + 1);
            
            // Add start point
            _pathPoints.Add(start);
            
            var currentPos = start;
            var goingRight = true;
            
            for (var i = 0; i <= numberOfSwitchbacks; i++)
            {
                if (i < numberOfSwitchbacks)
                {
                    // Move forward toward the end
                    currentPos += forwardDirection * forwardPerSection;
                    
                    // Move up in height
                    currentPos.y += heightPerSection;
                    
                    // Zigzag to the side
                    currentPos += sideDirection * (switchbackOffset * (goingRight ? 1 : -1));
                    
                    _pathPoints.Add(currentPos);
                    
                    // Create the switchback turn - move to opposite side
                    goingRight = !goingRight;
                    currentPos += sideDirection * (switchbackOffset * 2 * (goingRight ? 1 : -1));
                    
                    _pathPoints.Add(currentPos);
                }
                else
                {
                    // Final straight segment to castle
                    _pathPoints.Add(end);
                }
            }
            
            // Paint terrain along path
            PaintPathOnTerrain();
            
            // Create steps if enabled
            if (createSteps)
            {
                CreateTerrainSteps();
            }
            
            // Place cobblestone meshes
            if (useMeshCobblestones && cobblestonePrefab)
            {
                PlaceCobblestones();
            }
            
            // Generate waypoints for enemies
            GenerateWaypoints();
            
            Debug.Log($"✓ Generated switchback path with {_pathPoints.Count} points");
        }
    
        private void PaintPathOnTerrain()
        {
            var terrainData = terrain.terrainData;
            var alphamapWidth = terrainData.alphamapWidth;
            var alphamapHeight = terrainData.alphamapHeight;
        
            var alphamaps = terrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
        
            for (var i = 0; i < _pathPoints.Count - 1; i++)
            {
                var start = _pathPoints[i];
                var end = _pathPoints[i + 1];
            
                // Interpolate along path segment
                var steps = Mathf.CeilToInt(Vector3.Distance(start, end) * 2);
            
                for (var j = 0; j <= steps; j++)
                {
                    var t = j / (float)steps;
                    var worldPos = Vector3.Lerp(start, end, t);
                
                    // Paint circular area around point
                    PaintCircularArea(alphamaps, worldPos, pathWidth, alphamapWidth, alphamapHeight);
                }
            }
        
            terrainData.SetAlphamaps(0, 0, alphamaps);
        }
    
        private void PaintCircularArea(float[,,] alphamaps, Vector3 worldPos, float radius, int alphamapWidth, int alphamapHeight)
        {
            // Convert world position to terrain coordinates
            var terrainPos = worldPos - terrain.transform.position;
            var normalizedPos = new Vector3(
                terrainPos.x / terrain.terrainData.size.x,
                0,
                terrainPos.z / terrain.terrainData.size.z
            );
        
            var centerX = Mathf.Clamp((int)(normalizedPos.x * alphamapWidth), 0, alphamapWidth - 1);
            var centerY = Mathf.Clamp((int)(normalizedPos.z * alphamapHeight), 0, alphamapHeight - 1);
        
            var radiusInAlphamap = Mathf.CeilToInt(radius / terrain.terrainData.size.x * alphamapWidth);
        
            for (var y = -radiusInAlphamap; y <= radiusInAlphamap; y++)
            {
                for (var x = -radiusInAlphamap; x <= radiusInAlphamap; x++)
                {
                    var alphaX = Mathf.Clamp(centerX + x, 0, alphamapWidth - 1);
                    var alphaY = Mathf.Clamp(centerY + y, 0, alphamapHeight - 1);
                
                    var distance = Mathf.Sqrt(x * x + y * y);
                    if (!(distance <= radiusInAlphamap)) continue;
                    var strength = 1f - (distance / radiusInAlphamap);
                    strength *= textureBlendStrength;
                    
                    // Reduce other textures
                    var totalOther = 0f;
                    for (var t = 0; t < alphamaps.GetLength(2); t++)
                    {
                        if (t == pathTextureIndex) continue;
                        alphamaps[alphaY, alphaX, t] *= (1f - strength);
                        totalOther += alphamaps[alphaY, alphaX, t];
                    }
                    
                    // Increase path texture
                    alphamaps[alphaY, alphaX, pathTextureIndex] = strength + (1f - strength) * alphamaps[alphaY, alphaX, pathTextureIndex];
                    
                    // Normalize
                    var sum = totalOther + alphamaps[alphaY, alphaX, pathTextureIndex];
                    if (!(sum > 0)) continue;
                    {
                        for (var t = 0; t < alphamaps.GetLength(2); t++)
                        {
                            alphamaps[alphaY, alphaX, t] /= sum;
                        }
                    }
                }
            }
        }
    
        private void CreateTerrainSteps()
        {
            var terrainData = terrain.terrainData;
            var heightmapResolution = terrainData.heightmapResolution;
            var heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
        
            for (var i = 0; i < _pathPoints.Count - 1; i++)
            {
                var start = _pathPoints[i];
                var end = _pathPoints[i + 1];
            
                var distance = Vector3.Distance(start, end);
                var numSteps = Mathf.CeilToInt(distance / stepDepth);
            
                for (var s = 0; s < numSteps; s++)
                {
                    var t = s / (float)numSteps;
                    var stepPos = Vector3.Lerp(start, end, t);
                    var stepHeightValue = Mathf.Lerp(start.y, end.y, t);
                
                    // Flatten area for step
                    FlattenCircularArea(heights, stepPos, stepHeightValue, pathWidth * 0.5f, heightmapResolution);
                }
            }
        
            terrainData.SetHeights(0, 0, heights);
        }

        private void FlattenCircularArea(float[,] heights, Vector3 worldPos, float targetHeight, float radius, int heightmapResolution)
        {
            var terrainPos = worldPos - terrain.transform.position;
            var normalizedPos = new Vector3(
                terrainPos.x / terrain.terrainData.size.x,
                0,
                terrainPos.z / terrain.terrainData.size.z
            );
        
            var centerX = Mathf.Clamp((int)(normalizedPos.x * heightmapResolution), 0, heightmapResolution - 1);
            var centerY = Mathf.Clamp((int)(normalizedPos.z * heightmapResolution), 0, heightmapResolution - 1);
        
            var radiusInHeightmap = Mathf.CeilToInt(radius / terrain.terrainData.size.x * heightmapResolution);
            var normalizedHeight = targetHeight / terrain.terrainData.size.y;
        
            for (var y = -radiusInHeightmap; y <= radiusInHeightmap; y++)
            {
                for (var x = -radiusInHeightmap; x <= radiusInHeightmap; x++)
                {
                    var hX = Mathf.Clamp(centerX + x, 0, heightmapResolution - 1);
                    var hY = Mathf.Clamp(centerY + y, 0, heightmapResolution - 1);
                
                    var distance = Mathf.Sqrt(x * x + y * y);
                    if (!(distance <= radiusInHeightmap)) continue;
                    var blend = 1f - (distance / radiusInHeightmap);
                    heights[hY, hX] = Mathf.Lerp(heights[hY, hX], normalizedHeight, blend * 0.8f);
                }
            }
        }
    
        private void PlaceCobblestones()
        {
            // Remove old cobblestones if they exist
            var oldContainer = transform.Find("Cobblestones");
            if (oldContainer != null)
            {
                DestroyImmediate(oldContainer.gameObject);
            }
            
            var cobblestoneContainer = new GameObject("Cobblestones")
            {
                transform =
                {
                    parent = transform
                }
            };

            for (var i = 0; i < _pathPoints.Count - 1; i++)
            {
                var start = _pathPoints[i];
                var end = _pathPoints[i + 1];
            
                var distance = Vector3.Distance(start, end);
                var numCobbles = Mathf.CeilToInt(distance / cobblestoneSpacing);
            
                for (var j = 0; j <= numCobbles; j++)
                {
                    var t = j / (float)numCobbles;
                    var pos = Vector3.Lerp(start, end, t);
                
                    // Sample terrain height
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y + 0.01f;
                
                    var cobble = Instantiate(cobblestonePrefab, pos, Quaternion.identity, cobblestoneContainer.transform);
                
                    // Random rotation
                    cobble.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                
                    // Random scale variation
                    var scale = Random.Range(0.9f, 1.1f);
                    cobble.transform.localScale *= scale;
                }
            }
        }

        private void GenerateWaypoints()
        {
            if (!waypointParent)
            {
                var wpParent = new GameObject("Waypoints");
                waypointParent = wpParent.transform;
                waypointParent.parent = transform;
            }
        
            // Clear existing waypoints
            while (waypointParent.childCount > 0)
            {
                DestroyImmediate(waypointParent.GetChild(0).gameObject);
            }
        
            for (var i = 0; i < _pathPoints.Count; i++)
            {
                var wp = new GameObject($"Waypoint_{i:D3}")
                {
                    transform =
                    {
                        position = _pathPoints[i],
                        parent = waypointParent
                    }
                };

                // Visual indicator
                wp.AddComponent<SphereCollider>().isTrigger = true;
            }
            
            Debug.Log($"✓ Created {_pathPoints.Count} waypoints for enemy navigation");
        }
        
        public void ClearPath()
        {
            // Remove waypoints
            if (waypointParent)
            {
                DestroyImmediate(waypointParent.gameObject);
            }
            
            // Remove cobblestones
            var cobbleContainer = transform.Find("Cobblestones");
            if (cobbleContainer)
            {
                DestroyImmediate(cobbleContainer.gameObject);
            }
            
            Debug.Log("Path cleared. Note: Terrain changes are permanent - use Ctrl+Z to undo terrain modifications.");
        }
    
        // Call from inspector or code to regenerate
        [ContextMenu("Regenerate Path")]
        public void RegeneratePath()
        {
            GenerateSwitchbackPath();
        }
    }
}