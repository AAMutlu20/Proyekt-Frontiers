using Generation.TrueGen.Systems;
using irminNavmeshEnemyAiUnityPackage;
using UnityEngine;

namespace Audio.Bridges
{
    /// <summary>
    /// Bridges the IrminBaseHealthSystem to my audio system
    /// Subscribes to health events and plays appropriate sounds
    /// </summary>
    [RequireComponent(typeof(IrminBaseHealthSystem))]
    [RequireComponent(typeof(EnemyPathFollower))]
    public class EnemyHealthAudioBridge : MonoBehaviour
    {
        private IrminBaseHealthSystem _healthSystem;
        private EnemyPathFollower _pathFollower;
        
        private void Awake()
        {
            _healthSystem = GetComponent<IrminBaseHealthSystem>();
            _pathFollower = GetComponent<EnemyPathFollower>();
            
            // Subscribe to health events
            _healthSystem.OnHealthDamaged += OnHealthDamaged;
            _healthSystem.OnMinHealthReached += OnMinHealthReached;
        }
        
        private void OnHealthDamaged(float damageAmount, float healthAfterDamage)
        {
            // Play hit sound when enemy takes damage
            _pathFollower?.OnTakeDamage();
        }
        
        private void OnMinHealthReached()
        {
            // Play death sound when enemy dies
            _pathFollower?.OnDeath();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (!_healthSystem) return;
            _healthSystem.OnHealthDamaged -= OnHealthDamaged;
            _healthSystem.OnMinHealthReached -= OnMinHealthReached;
        }
    }
}