using System;
using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    [Serializable]
    public class GameObjectPoolWithWeight
    {
        [SerializeField] GameObjectPool _gameObjectPool;
        [SerializeField] int _weight = 1;

        public GameObjectPool GameObjectPool { get { return _gameObjectPool; } }
        public int Weight { get { return _weight; } }
    }
}

