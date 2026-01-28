using irminNavmeshEnemyAiUnityPackage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace irminNavmeshEnemyAiUnityPackage
{
    [Serializable]
    public class Wave : MonoBehaviour
    {
        /// <summary>
        /// If true will ignore weight and just spawn all characters from the list in order.
        /// </summary>
        [SerializeField] private bool _spawnCharacterInOrder = true;
        [SerializeField] private CharactersWithWeightList CharacterWeightList = new();

        [SerializeField] private List<CharacterSpawner> _characterSpawnersForWave = new();
        [SerializeField] private List<CharacterSpawner> _freeCharacterSpawnersForWave = new();
        [SerializeField] private List<CharacterSpawner> _cooldownedCharacterSpawnersForWave = new();

        /// <summary>
        /// This makes it ignore weight and just spawn the list in an exact order. (Only way to do it now)
        /// </summary>
        [SerializeField] private bool _spawnInExactOrder = true;
        [SerializeField] private int _currentIndexForSpawningInExactOrder = 0;
        [SerializeField] private int _defeatedCharacterCounter = 0;


        [SerializeField] private bool _spawningWave = false;
        [SerializeField] private bool _allWaveCharactersSpawned = false;
        [SerializeField] private List<BaseCharacter> _currentlyActiveSpawnedCharacters = new();

        // If active will set target of character AI on spawn.
        [SerializeField] private bool _useDebugAttackDamagable;
        [SerializeField] private IDamagable _debugAttackDamagable;
        [SerializeField] private GameObject _debugAttackDamagableGameObject;


        private void Start()
        {
            _debugAttackDamagable = _debugAttackDamagableGameObject.GetComponent<IDamagable>();

            foreach (CharacterSpawner characterSpawner in _characterSpawnersForWave)
            {
                // all character spawners are free from the start.
                _freeCharacterSpawnersForWave.Add(characterSpawner);
                characterSpawner.OnCooldownTimerElapsed.AddListener(TimerCooldownElapsed);
                characterSpawner.OnCooldownStarted.AddListener(TimerCooldownStarted);
            }
        }

        private void Update()
        {
            if (_spawningWave && !_allWaveCharactersSpawned) { UpdateWave(); }
        }

        private void UpdateWave()
        {
            // If we have no free spawners we cannot spawn. We will wait.
            if (_freeCharacterSpawnersForWave.Count <= 0) return;
            if(_spawnCharacterInOrder)
            {
                BaseCharacter spawnedCharacter = _freeCharacterSpawnersForWave[0].SpawnCharacter(CharacterWeightList.CharactersWithWeight[_currentIndexForSpawningInExactOrder].CharacterPrefab);
                 AddCurrentlyActiveSpawnedCharacter(spawnedCharacter);
                if (_useDebugAttackDamagable) { spawnedCharacter.CombatAISystem.AttackDamagable(_debugAttackDamagable); }
                _currentIndexForSpawningInExactOrder++;
                if (_currentIndexForSpawningInExactOrder >= CharacterWeightList.CharactersWithWeight.Count) { _allWaveCharactersSpawned = true; }

            }
        }

        private void TimerCooldownStarted(CharacterSpawner characterSpawner)
        {
            _freeCharacterSpawnersForWave.Remove(characterSpawner);
            _cooldownedCharacterSpawnersForWave.Add(characterSpawner);
        }

        private void TimerCooldownElapsed(CharacterSpawner characterSpawner)
        {
            _cooldownedCharacterSpawnersForWave.Remove(characterSpawner);
            _freeCharacterSpawnersForWave.Add(characterSpawner);
        }

        public void StartWave()
        {
            if (_characterSpawnersForWave.Count <= 0) { Debug.LogError($"Wave {name} cannot spawn because it has no CharacterSpawners assigned. Cancelling."); return; }
            
            _currentIndexForSpawningInExactOrder = 0;
            _allWaveCharactersSpawned = false;
            _spawningWave = true;
        }

        private void AddCurrentlyActiveSpawnedCharacter(BaseCharacter pBaseCharacter)
        {
            _currentlyActiveSpawnedCharacters.Add(pBaseCharacter);
            pBaseCharacter.OnDefeat.AddListener(CharacterDefeated);
        }

        private void CharacterDefeated(BaseCharacter pCharacter)
        {
            _currentlyActiveSpawnedCharacters.Remove(pCharacter);
            _defeatedCharacterCounter++;
        }
    }
}