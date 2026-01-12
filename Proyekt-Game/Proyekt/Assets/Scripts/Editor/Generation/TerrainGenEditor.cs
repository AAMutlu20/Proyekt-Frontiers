using Generation;
using UnityEditor;
using UnityEngine;

namespace Editor.Generation
{
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainGenEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            TerrainGenerator generator = (TerrainGenerator)target;
        
            GUILayout.Space(10);
        
            if (GUILayout.Button("Generate Terrain", GUILayout.Height(40)))
            {
                generator.GenerateTerrain();
            }
        }
    }
}