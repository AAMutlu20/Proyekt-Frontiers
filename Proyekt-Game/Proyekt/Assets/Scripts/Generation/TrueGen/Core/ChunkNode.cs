using System.Collections.Generic;
using UnityEngine;

namespace Generation.TrueGen.Core
{
    [System.Serializable]
    public class ChunkNode
    {
        // Identity
        public int ID { get; set; }
        public int gridX;
        public int gridY;
        
        // Spatial data
        public Vector3[] worldCorners; // Make chunk corners be 4
        public Vector3 center;
        
        // Gameplay data
        public ChunkType chunkType;
        public bool isBuildable = true;
        public bool isOccupied;
        
        // Visual data
        public float yOffset;
        public Color vertexColor = Color.white;
        
        // Graph data - DON'T SERIALIZE PLEASE (causes circular reference)
        [System.NonSerialized]
        public List<ChunkNode> Neighbors = new();
        
        // Pathfinding data for A*
        [System.NonSerialized]
        public float GCost;
        [System.NonSerialized]
        public float HCost;
        [System.NonSerialized]
        public ChunkNode Parent;
        
        // Texture data
        [System.NonSerialized]
        public int TextureIndex; // 0-3 for the 4 texture slots
        [System.NonSerialized]
        public float TextureRotation; // 0, 90, 180, or 270 degrees
        
        public float FCost => GCost + HCost;

        // Helper method
        public Vector3 GetWorldCorner(int index)
        {
            var corner = worldCorners[index];
            corner.y += yOffset;
            return corner;
        }
    }
}