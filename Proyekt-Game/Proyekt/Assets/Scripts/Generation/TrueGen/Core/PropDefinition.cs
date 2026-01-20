using UnityEngine;

namespace Generation.TrueGen.Core
{
    [CreateAssetMenu(fileName = "New Prop", menuName = "TrueGen/Prop Definition")]
    public class PropDefinition : ScriptableObject
    {
        public PropType propType;
        public GameObject prefab;
        public bool blocksBuilding = true;
        public Vector2 scaleRange = new(0.8f, 1.2f);
        public bool randomRotation = true;
        
        [Header("Placement Rules")]
        [Range(0f, 1f)] public float spawnChance = 0.1f;
        public bool canSpawnOnPath;
        public bool preferEdges;
    }
}