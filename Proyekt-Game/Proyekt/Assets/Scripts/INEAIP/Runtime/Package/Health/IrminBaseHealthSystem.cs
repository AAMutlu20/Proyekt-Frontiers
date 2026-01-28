using Audio;
using irminNavmeshEnemyAiUnityPackage;
using IrminTimerPackage.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class IrminBaseHealthSystem : MonoBehaviour, IDamagable
    {
        //[SerializeField] protected int _faction = -1;
        [SerializeField] FactionMemberComponent _factionMemberComponent;
        [SerializeField] private float _attackRadius;
        [SerializeField] protected bool _interactionBusy = false;
        [SerializeField] protected bool _destroyOnMinHealthReached = false;

        [SerializeField] protected float _maxHealth;
        [SerializeField] protected float _tempExtraMaxHealth;
        [SerializeField] protected float _minHealth = 0;
        [SerializeField] protected float _currentHealth;
        [SerializeField] protected float _tempExtraCurrentHealth;

        [SerializeField] protected bool _startAtFullHealth = true;

        [SerializeField] protected bool _invulnerable = false;

        [SerializeField] protected HealthUI _healthUI;

        [SerializeField] IrminTimer _temporaryInvulnerabltyTimer = new();

        [SerializeField] bool _usePooling;
        [SerializeField] GameObjectPool _gameObjectPool;
        
        [Header("Audio (For Player Only)")]
        [SerializeField] private GameAudioClips audioClips;
        [SerializeField] private bool isPlayer = false; // Set to true for player


        public FactionMemberComponent FactionMemberComponent { get { return _factionMemberComponent; } set { _factionMemberComponent = value; } }
        public int Faction { get { return _factionMemberComponent.FactionID; } set { _factionMemberComponent.FactionID = value; } }
        public bool DestroyOnMinHealthReached { get { return _destroyOnMinHealthReached; } set { _destroyOnMinHealthReached = value; } }

        /// <summary>
        /// First variable: Damage income.
        /// Second variable: Health after damage.
        /// </summary>
        public UnityAction<float, float> OnHealthDamaged;
        /// <summary>
        /// First variable: Healing income.
        /// Second variable: Health after healing.
        /// </summary>
        public UnityAction<float, float> OnHealthHealed;
        /// <summary>
        /// First variable: Health before change.
        /// Second variable: Health after change.
        /// </summary>
        public UnityAction<float, float> OnHealthChanged;
        /// <summary>
        /// When the health reaches the minimum health.
        /// </summary>
        public UnityAction OnMinHealthReached;
        public UnityEvent OnMinHealthReachedUnityEvent;
        /// <summary>
        /// When the health reaches the maximum health.
        /// </summary>
        public UnityAction OnMaxHealthReached;
         
        public bool DestroyAtMinHealth { get { return _destroyOnMinHealthReached; } set { _destroyOnMinHealthReached = value; } }
        public bool Invulnerable { get { return _invulnerable; } set { _invulnerable = value; } }

        protected virtual void Awake()
        {
            if (_startAtFullHealth)
            {
                SetCurrentHealthToMaxHealth();
            }
            _temporaryInvulnerabltyTimer.OnTimeElapsed += EndInvulnerability;
            if (_temporaryInvulnerabltyTimer.Time > 0)
            {
                _invulnerable = true;
                _temporaryInvulnerabltyTimer.StartTimer();
            }
        }

        private void Start()
        {
            OnMinHealthReached += InvokeOnMinHealthReachedUnityEvent;
        }

        public void ReAwaken(float pMaxHealth)
        {
            _maxHealth = pMaxHealth;
            Awake();
        }

        protected virtual void Update()
        {
            _temporaryInvulnerabltyTimer.UpdateTimer(Time.deltaTime);
        }

        private void EndInvulnerability()
        {
            _invulnerable = false;
        }

        public void ReAwaken()
        {
            // Cannot build
            //UnityEditor.PrefabUtility.RevertObjectOverride(this, UnityEditor.InteractionMode.AutomatedAction);

            if (_startAtFullHealth)
            {
                SetCurrentHealthToMaxHealth();
            }
        }

        private void SetCurrentHealthToMaxHealth()
        {
            _currentHealth = _maxHealth;
            UpdateHealthUIIfAssigned();
        }

        private float CalculateDamage(float pIncomingDamage)
        {
            float calculatedDamage = Mathf.Abs(pIncomingDamage);

            return calculatedDamage;
        }

        private float CalculateHealing(float pIncomingHealing)
        {
            float calculatedHealing = Mathf.Abs(pIncomingHealing);

            return calculatedHealing;
        }



        #region IDamagableMethods
        /// <summary>
        /// Damage this base health system.
        /// </summary>
        /// <param name="pDamageValue">The damage value to inflict. (can be altered by processes on the target)</param>
        /// <returns>If the target was defeated by the damage.</returns>
        public bool Damage(float pDamageValue, IDamagable _attackingIDamagable)
        {
            if (_invulnerable) return false;
            // Later we could use armor values or other alterations to this value.
            float healthBeforeChange = _currentHealth;
            float calculatedDamage = CalculateDamage(pDamageValue);
            // First deal damage to the extra health
            float originalTempExtraHealth = _tempExtraCurrentHealth;

            // if temp extra health is not depleted by the damage we do not need to continue dealing damage to real current health.
            if (_tempExtraCurrentHealth - calculatedDamage <= 0)
            {
                _tempExtraCurrentHealth = 0;
                if (originalTempExtraHealth > 0)
                {
                    calculatedDamage -= originalTempExtraHealth;
                }

            }
            else
            {
                _tempExtraCurrentHealth -= calculatedDamage;
                OnHealthChanged?.Invoke(healthBeforeChange, _currentHealth);
                UpdateHealthUIIfAssigned();
                return false;
            }

            if (_currentHealth - calculatedDamage <= _minHealth)
            {
                SetHealthToMin(healthBeforeChange);
                return true;
            }
            else
            {
                _currentHealth -= calculatedDamage;
                OnHealthChanged?.Invoke(healthBeforeChange, _currentHealth);
                UpdateHealthUIIfAssigned();
                return false;
            }
        }

        private void SetHealthToMin(float pHealthBeforeChange)
        {
            // NEW: Play defeat sound if this is the player
            if (isPlayer && audioClips)
            {
                // Fade out music and ambiance
                AudioManager.Instance?.StopMusic(1f);
                AudioManager.Instance?.StopAmbiance(1f);
                
                // Play defeat sound
                if (audioClips.defeatSound)
                {
                    AudioManager.Instance?.PlaySFX(audioClips.defeatSound);
                }
                
                Debug.Log("Player defeated! Playing defeat sound.");
            }
            
            // Existing code:
            _currentHealth = _minHealth;
            OnHealthChanged?.Invoke(pHealthBeforeChange, _currentHealth);
            OnMinHealthReached?.Invoke();
            if (_destroyOnMinHealthReached && !_usePooling) { Debug.Log($"OnMinHealthDebug: Destroying game object {name} because health reached minhealth."); Destroy(gameObject); }
            else if (_usePooling) { Debug.Log($"OnMinHealthDebug: Pooling game object {name} because pooling was enabled and object reached min health."); _gameObjectPool.PoolGameObject(gameObject); }
            UpdateHealthUIIfAssigned();
        }

        public void ManuallySetHealthToMin()
        {
            SetHealthToMin(_currentHealth);
        }

        public float GetHealth(float pDamageValue = 0, float pHealingValue = 0)
        {
            float calculatedDamage = CalculateDamage(pDamageValue);
            float calculatedHealing = CalculateHealing(pHealingValue);
            return _currentHealth - calculatedDamage + calculatedHealing;

        }

        /// <summary>
        /// Heal this base health system.
        /// </summary>
        /// <param name="pHealingValue">The healing value. (can be altered by processes on the target)</param>
        /// <returns>If this heals the system to max health.</returns>
        public bool Heal(float pHealingValue)
        {
            float healthBeforeChange = _currentHealth;
            float calculatedHealing = CalculateHealing(pHealingValue);
            if (_currentHealth + calculatedHealing >= _maxHealth)
            {
                _currentHealth = _maxHealth;
                OnHealthChanged?.Invoke(healthBeforeChange, _currentHealth);
                OnMaxHealthReached?.Invoke();
                UpdateHealthUIIfAssigned();
                return true;
            }
            else
            {
                _currentHealth += calculatedHealing;
                OnHealthChanged?.Invoke(healthBeforeChange, _currentHealth);
                UpdateHealthUIIfAssigned();
                return false;
            }
        }
        #endregion

        public void SetFaction(int pFaction)
        {
            _factionMemberComponent.FactionID = pFaction;
        }

        public void SetInteractionBusy(bool pInteractionBusy)
        {
            _interactionBusy = pInteractionBusy;
        }

        public void TemporarilyIncreaseMaxHealth(float pTemphealth)
        {
            _tempExtraMaxHealth += pTemphealth;
            _tempExtraCurrentHealth += pTemphealth;
            UpdateHealthUIIfAssigned();
        }

        private void UpdateHealthUIIfAssigned()
        {
            if (_healthUI == null) return;
            _healthUI.SetMaxHearts((int)_maxHealth + (int)_tempExtraMaxHealth);
            _healthUI.SetCurrentHearts((int)_currentHealth, (int)_tempExtraCurrentHealth);
        }

        public int GetFaction()
        {
            return _factionMemberComponent.FactionID;
        }

        public void SetGameObjectPooling(GameObjectPool pGameObjectPool)
        {
            _gameObjectPool = pGameObjectPool;
            if (_gameObjectPool != null) _usePooling = true;
            else { _usePooling = false; }
        }

        public void DisableGameObjectPooling()
        {
            _usePooling = false;
        }

        public Transform GetAttackTargetTransform()
        {
            throw new System.NotImplementedException();
        }

        public float GetAttackRadius()
        {
            return _attackRadius;
        }

        public int GetFactionID()
        {
            return _factionMemberComponent.FactionID;
        }

        public bool IsDestroyed()
        {
            throw new System.NotImplementedException();
        }

        private void InvokeOnMinHealthReachedUnityEvent()
        {
            OnMinHealthReachedUnityEvent?.Invoke();
        }
    }
}