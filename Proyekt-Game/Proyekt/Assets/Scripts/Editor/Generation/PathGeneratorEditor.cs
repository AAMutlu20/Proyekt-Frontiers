using Generation;
using UnityEditor;
using UnityEngine;

namespace Editor.Generation
{
    [CustomEditor(typeof(PathGenerator))]
    public class PathGenetratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            var generator = (PathGenerator)target;
        
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Path Generation", EditorStyles.boldLabel);
        
            if (GUILayout.Button("Generate Switchback Path", GUILayout.Height(40)))
            {
                Undo.RegisterCompleteObjectUndo(generator.terrain.terrainData, "Generate Switchback Path");
                generator.GenerateSwitchbackPath();
                EditorUtility.SetDirty(generator.terrain.terrainData);
            }
        
            EditorGUILayout.Space();
        
            if (GUILayout.Button("Clear Path", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear Path", 
                        "This will attempt to reset the terrain. Continue?", "Yes", "Cancel"))
                {
                    generator.ClearPath();
                }
            }
        
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "1. Assign terrain, start point, and end point (castle)\n" +
                "2. Set the path texture index (check terrain layers)\n" +
                "3. Click 'Generate Switchback Path'\n" +
                "4. Waypoints will be created for enemy navigation", 
                MessageType.Info);
        }
    }
}