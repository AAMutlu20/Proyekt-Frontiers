using UnityEngine;
using UnityEditor;
using irminNavmeshEnemyAiUnityPackage;
using UnityEngine.UIElements;

[CustomEditor(typeof(CatapultSystem))]
public class Editor_CatapultSystem : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CatapultSystem catapultSystem = (CatapultSystem)target;

        if (GUILayout.Button("Debug Shoot"))
        {
            catapultSystem.ShootCatapultWithCooldown();
        }
    }
}
