using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class BaseCharacter : MonoBehaviour, IDamagable
    {
        [SerializeField] IrminCombatAISystem _combatAISystem;
        [SerializeField] NavMeshModifierVolume _navMeshModifierPrefabToSpawnOnDefeat;
        [SerializeField] bool _destroyOnDefeat = true;
        [SerializeField] FactionMemberComponent _factionMemberComponent;
        [SerializeField] float _attackRadius;
        [SerializeField] private IrminBaseHealthSystem _healthSystem;


        public UnityEvent<BaseCharacter> OnDefeat;

        public IrminCombatAISystem CombatAISystem { get { return _combatAISystem; } }
        public FactionMemberComponent FactionMemberComponent { get { return _factionMemberComponent; } }

        public void Defeat()
        {
            if (_navMeshModifierPrefabToSpawnOnDefeat != null) Instantiate(_navMeshModifierPrefabToSpawnOnDefeat, transform.position, Quaternion.identity);
            OnDefeat?.Invoke(this);
            if (_destroyOnDefeat) Destroy(this.gameObject);
        }

        public Transform GetAttackTargetTransform()
        {
            return transform;
        }

        public float GetAttackRadius()
        {
            return _attackRadius;
        }

        public int GetFactionID()
        {
            return _factionMemberComponent.FactionID;
        }

        public bool Damage(float pDamage)
        {
            return _healthSystem.Damage(pDamage);
        }

        public bool IsDestroyed()
        {
            throw new System.NotImplementedException();
        }
    }

}