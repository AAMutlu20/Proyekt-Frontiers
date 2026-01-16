using irminNavmeshEnemyAiUnityPackage;
using IrminTimerPackage.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CharacterSpawner : MonoBehaviour
{
    [SerializeField] List<BaseCharacter> _spawnedCharacters = new();
    
    //[SerializeField] List<CharacterWithWeight> _charactersWithWeight = new();

    [SerializeField] List<BaseCharacter> _baseCharactersObstructing = new();

    [SerializeField] private float _obstructionSafetyDistance = 5;

    
    [SerializeField] private IrminTimer _spawnCooldownTimer = new();
    public UnityEvent<CharacterSpawner> OnCooldownTimerElapsed;
    public UnityEvent<CharacterSpawner> OnCooldownStarted;

    private void Start()
    {
        _spawnCooldownTimer.OnTimeElapsed += CooldownTimerElapsed;
    }

    private void Update()
    {
        _spawnCooldownTimer.UpdateTimer(Time.deltaTime);
    }

    private void CooldownTimerElapsed()
    {
        OnCooldownTimerElapsed?.Invoke(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        BaseCharacter foundBaseCharacter = collision.gameObject.GetComponent<BaseCharacter>();
        if(foundBaseCharacter != null)
        {
            _baseCharactersObstructing.Add(foundBaseCharacter);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        BaseCharacter foundBaseCharacter = collision.gameObject.GetComponent<BaseCharacter>();
        if (foundBaseCharacter != null)
        {
            _baseCharactersObstructing.Remove(foundBaseCharacter);
        }
    }

    public bool IsObstructed()
    {
        for (int i = 0; i < _baseCharactersObstructing.Count; i++)
        {
            if (_baseCharactersObstructing[i] == null || Vector3.Distance(_baseCharactersObstructing[i].transform.position, transform.position) >= _obstructionSafetyDistance ) { _baseCharactersObstructing.RemoveAt(i); }
        }
        if (_baseCharactersObstructing.Count <= 0) return false;
        else { return true; }
    }

    public BaseCharacter SpawnCharacter(BaseCharacter pBaseCharacterPrefab)
    {
        BaseCharacter spawnedCharacter = Instantiate(pBaseCharacterPrefab, transform.position, transform.rotation);
        StartCooldown();
        return spawnedCharacter;
        
    }

    private void StartCooldown()
    {
        _spawnCooldownTimer.ResetCurrentTime();
        _spawnCooldownTimer.StartTimer();
        OnCooldownStarted?.Invoke(this);
    }
}
