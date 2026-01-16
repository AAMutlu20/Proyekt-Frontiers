using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class GameObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private List<GameObject> _pooledGameObjects = new();

        public void PoolGameObject(GameObject pGameObjectToPool)
        {
            pGameObjectToPool.transform.SetParent(transform);
            pGameObjectToPool.transform.localPosition = Vector3.zero;
            pGameObjectToPool.transform.localRotation = Quaternion.identity;
            
            if(pGameObjectToPool.TryGetComponent<Rigidbody>(out Rigidbody foundRigidBody))
            {
                Debug.Log("Resetting RB Velocity");
                // If the gameobject we are trying to pool has a rigidbody, we should propably reset the velocity
                foundRigidBody.linearVelocity = Vector3.zero;
                foundRigidBody.angularVelocity = Vector3.zero;
                foundRigidBody.Sleep();
            }
            pGameObjectToPool.SetActive(false);
            _pooledGameObjects.Add(pGameObjectToPool);
        }

        public GameObject GetGameObjectFromPool(Transform pSpawnTransform, Transform pSpawnParent)
        {
            GameObject gameObjectGottenFromPool;
            if(_pooledGameObjects.Count <= 0)
            {
                // We should spawn and return a new one from the prefab.
                if (_prefab == null) Debug.LogError("GameObjectPool Prefab was null");
                gameObjectGottenFromPool = Instantiate(_prefab, pSpawnTransform.transform.position, pSpawnTransform.transform.rotation, pSpawnParent);
            }
            else
            {
                gameObjectGottenFromPool = _pooledGameObjects[0];
                gameObjectGottenFromPool.transform.position = pSpawnTransform.position;
                gameObjectGottenFromPool.transform.rotation = pSpawnTransform.rotation;
                gameObjectGottenFromPool.transform.SetParent(pSpawnParent);
                _pooledGameObjects.Remove(gameObjectGottenFromPool);
                gameObjectGottenFromPool.SetActive(true);
                
            }

            return gameObjectGottenFromPool;
        }
    }
}
