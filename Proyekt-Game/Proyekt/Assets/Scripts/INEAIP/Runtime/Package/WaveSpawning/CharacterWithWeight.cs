using System;
using UnityEngine;


namespace irminNavmeshEnemyAiUnityPackage
{
    [Serializable]
    public class CharacterWithWeight
    {
        [SerializeField] private BaseCharacter _characterPrefab;
        [SerializeField] private int _weight = 1;

        public BaseCharacter CharacterPrefab {  get { return _characterPrefab; } }
        public int Weight { get { return _weight; } }

        public CharacterWithWeight(int pWeight = 1)
        {
            _weight = pWeight;
        }
    }
}

