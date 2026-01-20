using System.Linq;
using Generation.TrueGen.Core;
using Generation.TrueGen.Systems;
using UnityEngine;

namespace Generation.TrueGen.Debugging
{
    [RequireComponent(typeof(ChunkGrid))]
    public class ChunkDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization")]
        [SerializeField] private bool showChunkBoundaries = true;
        [SerializeField] private bool showChunkCenters;
        [SerializeField] private bool showNeighborConnections;
        [SerializeField] private bool showGridCoordinates;
        [SerializeField] private bool showPathOnly;
        
        [Header("Colors")]
        [SerializeField] private Color buildableColor = Color.green;
        [SerializeField] private Color pathColor = Color.yellow;
        [SerializeField] private Color blockedColor = Color.red;
        [SerializeField] private Color neighborLineColor = Color.cyan;
        
        private ChunkGrid _chunkGrid;
        
        private void OnDrawGizmos()
        {
            if (!_chunkGrid)
                _chunkGrid = GetComponent<ChunkGrid>();
            
            if (!_chunkGrid || _chunkGrid.PathChunks == null)
                return;
            
            foreach (var chunk in _chunkGrid.PathChunks.Where(chunk => !showPathOnly || chunk.chunkType == ChunkType.Path))
            {
                if (showChunkBoundaries)
                    DrawChunkBoundary(chunk);
                
                if (showChunkCenters)
                    DrawChunkCenter(chunk);
                
                if (showNeighborConnections)
                    DrawNeighborConnections(chunk);
                
                if (showGridCoordinates)
                    DrawGridCoordinates(chunk);
            }
        }
        
        private void DrawChunkBoundary(ChunkNode chunk)
        {
            var color = GetChunkColor(chunk);
            Gizmos.color = color;
            
            for (var i = 0; i < 4; i++)
            {
                var start = chunk.GetWorldCorner(i);
                var end = chunk.GetWorldCorner((i + 1) % 4);
                Gizmos.DrawLine(start, end);
            }
        }
        
        private void DrawChunkCenter(ChunkNode chunk)
        {
            Gizmos.color = GetChunkColor(chunk);
            var center = chunk.center;
            center.y = chunk.yOffset;
            Gizmos.DrawSphere(center, 0.3f);
        }
        
        private void DrawNeighborConnections(ChunkNode chunk)
        {
            Gizmos.color = neighborLineColor;
            var from = chunk.center;
            from.y = chunk.yOffset;
            
            foreach (var neighbor in chunk.Neighbors)
            {
                var to = neighbor.center;
                to.y = neighbor.yOffset;
                Gizmos.DrawLine(from, to);
            }
        }
        
        private static void DrawGridCoordinates(ChunkNode chunk)
        {
            #if UNITY_EDITOR
            var labelPos = chunk.center;
            labelPos.y = chunk.yOffset + 0.5f;
            UnityEditor.Handles.Label(labelPos, $"({chunk.gridX},{chunk.gridY})");
            #endif
        }
        
        private Color GetChunkColor(ChunkNode chunk)
        {
            return chunk.chunkType switch
            {
                ChunkType.Path => pathColor,
                ChunkType.Blocked => blockedColor,
                _ => chunk.isBuildable ? buildableColor : blockedColor
            };
        }
    }
}