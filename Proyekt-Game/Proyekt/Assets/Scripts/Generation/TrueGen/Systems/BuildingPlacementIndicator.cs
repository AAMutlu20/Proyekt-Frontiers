using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Systems
{
    public class BuildingPlacementIndicator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float indicatorSize = 1.5f;
        [SerializeField] private float hoverHeight = 6f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.2f;
        
        [Header("Colors")]
        [SerializeField] private Color validColor = new Color(0, 1, 0, 0.6f); // Green
        [SerializeField] private Color invalidColor = new Color(1, 0, 0, 0.6f); // Red
        
        private GameObject _indicatorObject;
        private Material _indicatorMaterial;
        private BuildingPlacement _buildingPlacement;
        private float _baseScale;
        private bool _isActive;
        
        private void Awake()
        {
            CreateIndicator();
            _baseScale = indicatorSize;
            _isActive = false;
            _indicatorObject.SetActive(false);
        }
        
        private void Start()
        {
            _buildingPlacement = GetComponent<BuildingPlacement>();
        }
        
        private void Update()
        {
            if (!_isActive || !_buildingPlacement)
            {
                if (_indicatorObject.activeSelf)
                    _indicatorObject.SetActive(false);
                return;
            }
            
            // Get chunk under mouse
            var chunk = _buildingPlacement.GetChunkUnderMouse();
            
            if (chunk == null)
            {
                _indicatorObject.SetActive(false);
                return;
            }
            
            // Show indicator
            if (!_indicatorObject.activeSelf)
                _indicatorObject.SetActive(true);
            
            // Position at chunk center
            var position = chunk.center;
            position.y += hoverHeight;
            _indicatorObject.transform.position = position;
            
            // Determine if placement is valid
            var canBuild = chunk.chunkType == ChunkType.Buildable && !chunk.isOccupied;
            
            // Update color
            _indicatorMaterial.color = canBuild ? validColor : invalidColor;
            _indicatorMaterial.SetColor("_EmissionColor", canBuild ? validColor * 2f : invalidColor * 2f);
            
            // Pulse effect
            var pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            var scale = _baseScale + pulse;
            _indicatorObject.transform.localScale = Vector3.one * scale;
        }
        
        /// <summary>
        /// Create the visual indicator object
        /// </summary>
        private void CreateIndicator()
        {
            _indicatorObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _indicatorObject.name = "BuildingPlacementIndicator";
            _indicatorObject.transform.SetParent(transform);
            
            // Remove collider (we don't want it to interfere with raycasts)
            Destroy(_indicatorObject.GetComponent<Collider>());
            
            // Create material
            _indicatorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _indicatorMaterial.SetFloat("_Surface", 1); // Transparent
            _indicatorMaterial.SetFloat("_Blend", 0); // Alpha blend
            _indicatorMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _indicatorMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            _indicatorMaterial.SetFloat("_SrcBlend", 1);
            _indicatorMaterial.SetFloat("_DstBlend", 10);
            _indicatorMaterial.SetFloat("_ZWrite", 0);
            _indicatorMaterial.renderQueue = 3000;
            
            // Enable emission
            _indicatorMaterial.EnableKeyword("_EMISSION");
            _indicatorMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            
            _indicatorObject.GetComponent<Renderer>().material = _indicatorMaterial;
            _indicatorObject.transform.localScale = Vector3.one * indicatorSize;
        }
        
        /// <summary>
        /// Show the indicator (called when building is selected)
        /// </summary>
        public void ShowIndicator()
        {
            _isActive = true;
        }
        
        /// <summary>
        /// Hide the indicator (called when building is deselected)
        /// </summary>
        public void HideIndicator()
        {
            _isActive = false;
            if (_indicatorObject)
                _indicatorObject.SetActive(false);
        }
    }
}