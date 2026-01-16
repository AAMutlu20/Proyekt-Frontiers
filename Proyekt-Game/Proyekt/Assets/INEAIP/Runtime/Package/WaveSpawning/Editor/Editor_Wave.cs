using irminNavmeshEnemyAiUnityPackage;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Wave))]
public class Editor_Wave : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Wave wave = (Wave)target;
        // Start the wave when the button is pressed
        if (GUILayout.Button("StartWave"))
        {
            wave.StartWave();
        }
    }
}
