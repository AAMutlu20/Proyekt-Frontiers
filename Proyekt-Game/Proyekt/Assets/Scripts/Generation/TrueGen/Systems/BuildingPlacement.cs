using Generation.TrueGen.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Generation.TrueGen.Systems
{
    public class BuildingPlacement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChunkGrid chunkGrid;
        [SerializeField] private Camera mainCamera;
        
        [Header("Settings")]
        [SerializeField] private LayerMask terrainLayer = -1;
        
        private Terrain _terrain;
        
        /// <summary>
        /// Initialize with chunk grid (for runtime setup)
        /// </summary>
        public void Initialize(ChunkGrid grid)
        {
            chunkGrid = grid;
            
            // Check if we're in terrain mode
            _terrain = GetComponent<Terrain>();
            
            if (_terrain != null)
            {
                Debug.Log("BuildingPlacement: Terrain mode detected");
            }
        }
        
        private void Start()
        {
            if (!mainCamera)
                mainCamera = Camera.main;
            
            if (!chunkGrid)
                chunkGrid = GetComponent<ChunkGrid>();
            
            if (_terrain == null)
                _terrain = GetComponent<Terrain>();
        }
        
        /// <summary>
        /// Try to place a building at mouse position
        /// </summary>
        public bool TryPlaceBuildingAtMouse(GameObject buildingPrefab, int footprintWidth = 1, int footprintHeight = 1)
        {
            var mousePos = Mouse.current.position.ReadValue();
            var ray = mainCamera.ScreenPointToRay(mousePos);
            
            return Physics.Raycast(ray, out var hit, 1000f, terrainLayer) &&
                   TryPlaceBuildingAtWorldPos(buildingPrefab, hit.point, footprintWidth, footprintHeight);
        }
        
        /// <summary>
        /// Try to place a building at a world position
        /// </summary>
        private bool TryPlaceBuildingAtWorldPos(GameObject buildingPrefab, Vector3 worldPos, 
            int footprintWidth = 1, int footprintHeight = 1)
        {
            var chunk = chunkGrid.GetChunkAtWorldPosition(worldPos);
            
            if (chunk == null)
            {
                Debug.Log("No chunk found at position");
                
                return false;
            }
            
            if (!chunkGrid.CanBuildAt(chunk.gridX, chunk.gridY, footprintWidth, footprintHeight))
            {
                Debug.Log("Cannot build at this location");
                
                return false;
            }
            
            // Calculate placement position
            var placementPos = chunk.center;
            
            // Sample terrain height if in terrain mode
            if (_terrain != null)
            {
                placementPos.y = _terrain.SampleHeight(placementPos) + _terrain.transform.position.y;
            }
            else
            {
                // Mesh mode - use chunk's yOffset (should be 0 for buildable chunks)
                placementPos.y = chunk.yOffset;
            }
            
            // Place building
            var building = Instantiate(buildingPrefab);
            building.transform.position = placementPos;
            
            // Mark chunks as occupied
            var occupiedChunks = chunkGrid.GetChunksInFootprint(
                chunk.gridX, chunk.gridY, footprintWidth, footprintHeight
            );
            
            foreach (var occupiedChunk in occupiedChunks)
            {
                occupiedChunk.isOccupied = true;
            }
            
            Debug.Log($"Building placed at chunk ({chunk.gridX}, {chunk.gridY}) at height {placementPos.y}");
            return true;
        }
        
        /// <summary>
        /// Get the chunk under the mouse cursor
        /// </summary>
        public ChunkNode GetChunkUnderMouse()
        {
            if (!chunkGrid)
            {
                Debug.LogWarning("BuildingPlacement: ChunkGrid is null!");
                return null;
            }
            
            var mousePos = Mouse.current.position.ReadValue();
            var ray = mainCamera.ScreenPointToRay(mousePos);
            
            return Physics.Raycast(ray, out var hit, 1000f, terrainLayer) ?
                chunkGrid.GetChunkAtWorldPosition(hit.point) : null;
        }
    }
}