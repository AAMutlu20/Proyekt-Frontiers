using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    [Serializable]
    public class CharactersWithWeightList
    {
        [SerializeField] List<CharacterWithWeight> _charactersWithWeight = new();

        public List<CharacterWithWeight> CharactersWithWeight { get { return _charactersWithWeight; } }
    }
}