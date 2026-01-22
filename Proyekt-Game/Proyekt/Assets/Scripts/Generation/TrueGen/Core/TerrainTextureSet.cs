using UnityEngine;

namespace Generation.TrueGen.Core
{
    [CreateAssetMenu(fileName = "TerrainTextureSet", menuName = "TrueGen/Terrain Texture Set")]
    public class TerrainTextureSet : ScriptableObject
    {
        [System.Serializable]
        public class TextureSlot
        {
            public string name;
            public Texture2D albedoMap;
            public Texture2D normalMap;
            [Range(0.1f, 10f)] public float tiling = 1f;
        }
        
        [Header("Chunk Type Textures")]
        public TextureSlot buildableTexture;  // Grass
        public TextureSlot pathTexture;       // Dirt/stone
        public TextureSlot blockedTexture;    // Rock/gravel
        public TextureSlot decorativeTexture; // Variation
        
        public TextureSlot GetTextureForChunkType(ChunkType type)
        {
            return type switch
            {
                ChunkType.Path => pathTexture,
                ChunkType.Blocked => blockedTexture,
                ChunkType.Decorative => decorativeTexture,
                _ => buildableTexture
            };
        }
    }
}