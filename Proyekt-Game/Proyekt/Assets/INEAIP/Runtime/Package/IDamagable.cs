using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    public interface IDamagable
    {
        public Transform GetAttackTargetTransform();
        public float GetAttackRadius();

        public int GetFactionID();
        public bool Damage(float pDamage);
        public bool IsDestroyed();
    }
}