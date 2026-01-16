using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    [CustomEditor(typeof(IrminCombatAISystem))]
    public class Editor_IrminCombatAISystem : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10);
            IrminCombatAISystem irminCombatAISystem = (IrminCombatAISystem)target;

            if (GUILayout.Button("Try to Attack Selected Target"))
            {
                irminCombatAISystem.DebugAttackSelectedDamagable();
            }
        }
    }
}