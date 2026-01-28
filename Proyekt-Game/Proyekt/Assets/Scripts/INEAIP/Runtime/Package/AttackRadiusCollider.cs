using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class AttackRadiusCollider : MonoBehaviour
    {
        [SerializeField] List<IDamagable> _iDamagablesInRadius = new();
        [SerializeField] List<GameObject> _iDamagableGameObjectsInRange = new();
        [SerializeField] LayerMask _layerMask;

        private void OnTriggerEnter(Collider other)
        {
            IDamagable foundIDamagable = other.GetComponent<IDamagable>();
        }

        private void OnTriggerExit(Collider other)
        {
            IDamagable foundIDamagable = other.GetComponent<IDamagable>();
        }
    }
}



