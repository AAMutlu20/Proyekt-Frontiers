using Generation.TrueGen.Core;
using UnityEngine;

namespace Generation.TrueGen.Visuals
{
    [CreateAssetMenu(fileName = "TerrainMaterialSet", menuName = "TrueGen/Terrain Material Set")]
    public class TerrainMaterialSet : ScriptableObject
    {
        [Header("Materials by Chunk Type")]
        public Material buildableMaterial;  // Grass
        public Material pathMaterial;       // Dirt/stone
        public Material blockedMaterial;    // Rock/gravel
        public Material decorativeMaterial; // Variation
        public Material wallMaterial;       // For path sides
        
        [Header("Texture Tiling")]
        [Range(0.1f, 10f)] public float buildableTiling = 1f;
        [Range(0.1f, 10f)] public float pathTiling = 1f;
        [Range(0.1f, 10f)] public float blockedTiling = 1f;
        [Range(0.1f, 10f)] public float decorativeTiling = 1f;
        
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