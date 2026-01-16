using Generation;
using UnityEditor;
using UnityEngine;

namespace Editor.Generation
{
    [CustomEditor(typeof(CanyonGenerator))]
    public class CanyonGenEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();
            
            GUILayout.Space(10);
            
            var generator = (CanyonGenerator)target;
            
            // Big generate button
            GUI.backgroundColor = new Color(0.7f, 0.9f, 0.7f);
            if (GUILayout.Button("Generate Grand Canyon Terrain", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "Generate Terrain", 
                    "This will replace the current terrain. Continue?", 
                    "Generate", 
                    "Cancel"))
                {
                    Undo.RecordObject(generator.GetComponent<Terrain>().terrainData, "Generate Canyon Terrain");
                    generator.GenerateTerrain();
                    EditorUtility.SetDirty(generator);
                    EditorUtility.SetDirty(generator.GetComponent<Terrain>().terrainData);
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(5);
            
            // Quick randomize seed button
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.7f);
            if (GUILayout.Button("Randomize Seed", GUILayout.Height(25)))
            {
                var so = new SerializedObject(generator);
                var seedProp = so.FindProperty("seed");
                seedProp.intValue = Random.Range(0, 100000);
                so.ApplyModifiedProperties();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            // Info section
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Generate Grand Canyon terrain with meandering river, side canyons, and stratified rock layers.\n\n" +
                "• Seed determines random variation\n" +
                "• Canyon Depth/Width control main canyon size\n" +
                "• Side Canyons add tributary valleys\n" +
                "• Terracing creates layered rock shelves", 
                MessageType.Info);
        }
    }
}