using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Visuals
{
    [CreateAssetMenu(fileName = "TerrainMaterialSet", menuName = "TrueGen/Terrain Material Set")]
    public class TerrainMaterialSet : ScriptableObject
    {
        [Header("Materials by Chunk Type (for Mesh Mode)")]
        public Material buildableMaterial;
        public Material pathMaterial;
        public Material blockedMaterial;
        public Material decorativeMaterial;
        public Material wallMaterial;
        
        [Header("Texture Tiling (for Mesh Mode)")]
        [Range(0.1f, 10f)] public float buildableTiling = 1f;
        [Range(0.1f, 10f)] public float pathTiling = 1f;
        [Range(0.1f, 10f)] public float blockedTiling = 1f;
        [Range(0.1f, 10f)] public float decorativeTiling = 1f;
        
        [Header("Terrain Layers (for Terrain Mode)")]
        public TerrainLayer grassLayer;
        public TerrainLayer pathLayer;
        public TerrainLayer blockedLayer;
        public TerrainLayer decorativeLayer;
        
        public Material GetMaterialForChunkType(ChunkType type)
        {
            return type switch
            {
                ChunkType.Path => pathMaterial,
                ChunkType.Blocked => blockedMaterial,
                ChunkType.Decorative => decorativeMaterial,
                _ => buildableMaterial
            };
        }
        
        public float GetTilingForChunkType(ChunkType type)
        {
            return type switch
            {
                ChunkType.Path => pathTiling,
                ChunkType.Blocked => blockedTiling,
                ChunkType.Decorative => decorativeTiling,
                _ => buildableTiling
            };
        }
    }
}