using Generation.TrueGen.Core;
using Generation.TrueGen.Systems;
using UnityEngine;
using UnityEngine.InputSystem; // ADD THIS

namespace Generation.TrueGen.Visuals
{
    [RequireComponent(typeof(ChunkGrid))]
    public class GridOverlayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Material gridOverlayMaterial;
        
        [Header("Colors")]
        [SerializeField] private Color validPlacementColor = new(0, 1, 0, 0.3f);
        [SerializeField] private Color invalidPlacementColor = new(1, 0, 0, 0.3f);
        [SerializeField] private Color neutralColor = new(1, 1, 1, 0.2f);
        
        [Header("Settings")]
        [SerializeField] private bool showGridOnHover = true;
        
        private ChunkGrid _chunkGrid;
        private BuildingPlacement _buildingPlacement;
        private GameObject _gridOverlay;
        private MeshRenderer _gridRenderer;
        private bool _isPlacementMode;
        private ChunkNode _hoveredChunk;
        
        private static readonly int GridColorProperty = Shader.PropertyToID("_GridColor");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Surface = Shader.PropertyToID("_Surface");
        private static readonly int Blend = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

        /// <summary>
        /// Initialize with custom material (useful for runtime creation)
        /// </summary>
        public void Initialize(Material material)
        {
            gridOverlayMaterial = material;
        }

        private void Start()
        {
            _chunkGrid = GetComponent<ChunkGrid>();
            _buildingPlacement = GetComponent<BuildingPlacement>();
            
            if (!gridOverlayMaterial)
            {
                Debug.LogWarning("Grid overlay material not assigned. Creating default.");
                CreateDefaultGridMaterial();
            }
            
            CreateGridOverlay();
        }
        
        private void Update()
        {
            if (!showGridOnHover) return;
    
            UpdateHoveredChunk();
            UpdateGridColor();
    
            // DEBUG
            if (Keyboard.current == null || !Keyboard.current.gKey.wasPressedThisFrame) return;
            Debug.Log($"Grid Debug:");
            Debug.Log($"- BuildingPlacement: {_buildingPlacement}");
            Debug.Log($"- ChunkGrid: {_chunkGrid}");
            Debug.Log($"- Hovered Chunk: {_hoveredChunk != null}");
            Debug.Log($"- Grid Overlay Active: {_gridOverlay && _gridOverlay.activeSelf}");
        }
        
        private void CreateGridOverlay()
        {
            // Create a duplicate of the terrain mesh for overlay
            var terrainMesh = GetComponent<MeshFilter>().sharedMesh;
            
            _gridOverlay = new GameObject("GridOverlay");
            _gridOverlay.transform.SetParent(transform);
            _gridOverlay.transform.localPosition = Vector3.up * 0.01f; // Slight offset to avoid z-fighting
            _gridOverlay.transform.localRotation = Quaternion.identity;
            _gridOverlay.transform.localScale = Vector3.one;
            
            var mf = _gridOverlay.AddComponent<MeshFilter>();
            mf.mesh = terrainMesh;
            
            _gridRenderer = _gridOverlay.AddComponent<MeshRenderer>();
            _gridRenderer.material = gridOverlayMaterial;
            _gridRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            // Start hidden
            _gridOverlay.SetActive(false);
        }
        
        private void UpdateHoveredChunk()
        {
            if (!_buildingPlacement || !_chunkGrid || _chunkGrid.Chunks == null)
                return;
    
            _hoveredChunk = _buildingPlacement.GetChunkUnderMouse();
    
            // Show/hide grid based on hover
            if (_gridOverlay)
            {
                _gridOverlay.SetActive(_hoveredChunk != null || _isPlacementMode);
            }
        }

        private void UpdateGridColor()
        {
            if (!_gridRenderer || !_chunkGrid || _chunkGrid.Chunks == null)
            {
                SetGridColor(neutralColor);
                return;
            }
    
            if (_hoveredChunk == null)
            {
                SetGridColor(neutralColor);
                return;
            }
    
            // Check if placement is valid
            var canBuild = _chunkGrid.CanBuildAt(_hoveredChunk.gridX, _hoveredChunk.gridY, 1, 1);
            var color = canBuild ? validPlacementColor : invalidPlacementColor;
    
            SetGridColor(color);
        }
        
        private void SetGridColor(Color color)
        {
            if (_gridRenderer && _gridRenderer.material)
            {
                _gridRenderer.material.SetColor(GridColorProperty, color);
            }
        }
        
        public void SetPlacementMode(bool active)
        {
            _isPlacementMode = active;
            if (_gridOverlay)
            {
                _gridOverlay.SetActive(active);
            }
        }
        
        private void CreateDefaultGridMaterial()
        {
            gridOverlayMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            gridOverlayMaterial.SetColor(BaseColor, new Color(1, 0, 0, 0.8f));
            gridOverlayMaterial.SetFloat(Surface, 1); // Transparent
            gridOverlayMaterial.SetFloat(Blend, 0); // Alpha blend
    
            // Enable transparency
            gridOverlayMaterial.SetOverrideTag("RenderType", "Transparent");
            gridOverlayMaterial.SetInt(SrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            gridOverlayMaterial.SetInt(DstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            gridOverlayMaterial.SetInt(ZWrite, 0);
            gridOverlayMaterial.renderQueue = 3000;
        }
    }
}