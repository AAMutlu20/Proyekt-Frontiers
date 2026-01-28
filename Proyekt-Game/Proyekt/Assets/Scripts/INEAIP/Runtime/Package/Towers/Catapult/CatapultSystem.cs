// Original Author: Irmin Verhoeff
// Editors: -
// Description: A "catapult"s system that uses a seperate detection system to detect targets, then rotates to face them and uses dotween to animate a projectile in an arc to the target. When the animation is complete a method can execute to check for and damage damagables within blast radius.

using Audio;
using DG.Tweening;
using IrminTimerPackage.Tools;
using System;
using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class CatapultSystem : MonoBehaviour
    {
        [SerializeField] AudioClip _shootAudioClip;
        /// <summary>
        /// Detection system. Basically a seperate.
        /// </summary>
        [SerializeField] private DetectionSystem _catapultTargetDetectionSystem;
        /// <summary>
        /// Rotation system. Rotates the catapult towards a target on the y axis. This happens over time with a speed value.
        /// </summary>
        [SerializeField] private YRotationSystem _catapultYRotationSystem;
        // UNUSED ATM, but could be implemented. Factionmember component to check faction of hit targets agains faction of this system.
        // [SerializeField] private FactionMemberComponent _factionMemberComponent; 

        /// <summary>
        /// Timer for cooldown between shooting.
        /// </summary>
        [SerializeField] IrminTimer _shootCooldownTimer = new();
        /// <summary>
        /// Timer for shooting the projectile after the animation starts.
        /// </summary>
        [SerializeField] IrminTimer _shootTimer = new();

        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _height;
        [SerializeField] private float _doTweenArcAnimationDuration;

        // Easing setting for DoTween
        [SerializeField] private Ease _doTweenEase;

        //[SerializeField] private float _heightPerDistance;

        [SerializeField] private GameObject _objectToShootPrefab;
        [SerializeField] private GameObject _shotHitEffectPrefab;

        [SerializeField] private Animator _catapultAnimator;
        [SerializeField] private string _catapultSlingAnimationtriggerName;
        [SerializeField] private string _catapultRetractAnimationTriggerName;
        [SerializeField] private GameObject _currentlySpawnedShootObject;

        [SerializeField] private Transform _slingObjectParent;

        [SerializeField] PathType _pathType;

        [SerializeField] private float _explosionRadius = 10f;

        [SerializeField] private LayerMask _enemyLayerMask;

        [SerializeField] private float _damage = 2f;

        [SerializeField] private bool _useGameObjectPooling = false;
        [SerializeField] private GameObjectPool _gameObjectPool;

        //[SerializeField] private int _halfArcResolution = 3;

        [SerializeField] private bool _canShoot = false;

        private void Start()
        {
            // Event binding
            _catapultTargetDetectionSystem.OnDetectedNewGameObjectObject.AddListener(DetectedNewPossibleTarget);
            _catapultTargetDetectionSystem.OnNoLongerDetectedGameObject.AddListener(NoLongerDetectionPossibleTarget);
            _shootCooldownTimer.OnTimeElapsed += ShootCatapultWithCooldown;
            _shootTimer.OnTimeElapsed += Shoot;

        }

        private void CanShoot()
        {
            _canShoot = true;
        }

        private void Update()
        {
            // Timer updates
            _shootCooldownTimer.UpdateTimer(Time.deltaTime);
            _shootTimer.UpdateTimer(Time.deltaTime);
            if(_canShoot)
            {
                if(ShootCatapultWithCooldownWithAimCheck())
                {
                    _canShoot = false;
                }
            }
        }

        /// <summary>
        /// Does some extra checks on a possibly detected target. Then decided to fire at it or not.
        /// </summary>
        /// <param name="detectedGameObject">The possible target detected.</param>
        private void DetectedNewPossibleTarget(GameObject detectedGameObject)
        {
            if (_targetTransform == null)
            {
                bool targetWasNullBeforeThis = false;
                if (_targetTransform == null) { targetWasNullBeforeThis = true; }
                _targetTransform = detectedGameObject.transform;
                _catapultYRotationSystem.Target = detectedGameObject.transform;
                // If the timer is paused
                if (targetWasNullBeforeThis && _shootCooldownTimer.TimerActive == false)
                {
                    ShootCatapultWithCooldown();
                }
                else
                {
                    _shootCooldownTimer.ResetCurrentTime();
                    _shootCooldownTimer.StartTimer();
                }

            }
        }

        /// <summary>
        /// Checks if the target this is currently firing at left its detection collider. If so tries to get a new target from the detection system.
        /// </summary>
        /// <param name="pPossibleTarget">The possible target detected leaving the detection collider.</param>
        private void NoLongerDetectionPossibleTarget(GameObject pPossibleTarget)
        {
            if (_targetTransform == pPossibleTarget.transform)
            {
                GameObject foundNewTarget = GetFirstTargetFromDetector();
                if (foundNewTarget == null) { _targetTransform = null; _shootCooldownTimer.PauseTimer(); _catapultYRotationSystem.Target = null; }
                else
                {
                    _targetTransform = foundNewTarget.transform;
                }

            }
        }

        public bool ShootCatapultWithCooldownWithAimCheck()
        {
            if(_catapultYRotationSystem.InRotationDeadzone)
            {
                ShootCatapultWithCooldown();
                return true;
            }
            return false;
        }


        /// <summary>
        /// Will prepare the catapult to shoot.
        /// </summary>
        public void ShootCatapultWithCooldown()
        {
            if (_targetTransform == null) { _shootCooldownTimer.ResetCurrentTime(); _shootCooldownTimer.PauseTimer(); return; }
            if (_useGameObjectPooling) { _currentlySpawnedShootObject = _gameObjectPool.GetGameObjectFromPool(_slingObjectParent.transform, null); }
            else { _currentlySpawnedShootObject = Instantiate(_objectToShootPrefab); }
            _currentlySpawnedShootObject.transform.parent = _slingObjectParent;
            _currentlySpawnedShootObject.transform.localPosition = Vector3.zero;
            _currentlySpawnedShootObject.transform.rotation = _slingObjectParent.rotation;
            _catapultAnimator.SetTrigger(_catapultSlingAnimationtriggerName);
            _shootTimer.ResetCurrentTime();
            _shootTimer.StartTimer();

            _shootCooldownTimer.ResetCurrentTime();
            _shootCooldownTimer.StartTimer();
        }

        // When the animation completes it will call the event to shoot.
        public void Shoot()
        {
            if (_targetTransform == null)
            {
                if (_useGameObjectPooling) { _gameObjectPool.PoolGameObject(_currentlySpawnedShootObject); }
                else { Destroy(_currentlySpawnedShootObject); }
                Debug.Log("Enemy left range, cancelling shot.");
                _catapultYRotationSystem.Target = null;
                return;
            }

            _currentlySpawnedShootObject.transform.SetParent(null);
            MoveShootingObjectAlongDoTweenArc();
        }

        private GameObject GetFirstTargetFromDetector()
        {
            if (_catapultTargetDetectionSystem.GameObjectsInRange.Count <= 0) return null;
            return _catapultTargetDetectionSystem.GameObjectsInRange[0];
        }

        public void MoveShootingObjectAlongDoTweenArc()
        {
            Vector3 start = _slingObjectParent.position;
            Vector3 end = _targetTransform.position;

            Vector3 mid = (start + end) * 0.5f;
            mid.y += _height;

            Vector3 direction = (end - start).normalized;
            Vector3 launch1 = start + direction * 0.12f;
            Vector3 launch2 = start + direction * 0.6f;
            Vector3 launch3 = start + direction * 0.12f;

            Vector3[] path = new Vector3[]
            {
            mid,
            end,
            };

            if(_shootAudioClip != null)
            {
                AudioManager.Instance.PlaySFX(_shootAudioClip);
            }

            GameObject shootObject = _currentlySpawnedShootObject;
            _currentlySpawnedShootObject.transform.DOPath(path, _doTweenArcAnimationDuration, _pathType).SetEase(_doTweenEase).SetLookAt(0.01f).OnComplete(() =>
            {
                //ITriggerable foundITriggerable = shootObject.GetComponent<ITriggerable>();
                //if (foundITriggerable != null) { foundITriggerable.Trigger(gameObject); }
                // Replaced by:
                Vector3 origin = shootObject.transform.position;
                Collider[] hits = Physics.OverlapSphere(origin, _explosionRadius, _enemyLayerMask);
                Debug.Log($"Catapult shot. Found Damagables amount {hits.Length}");
                Instantiate(_shotHitEffectPrefab, origin, Quaternion.identity);
                foreach (Collider collider in hits)
                {
                    var foundIDamagable = collider.gameObject.GetComponentsInChildren<IDamagable>();
                    Debug.Log($"Founds {foundIDamagable.Length} in {collider.gameObject.name} [{foundIDamagable[0]}]");
                    if (foundIDamagable != null)
                    {
                        try
                        {
                            Debug.Log($"Catapult damaging for {_damage}");
                            foundIDamagable[0].Damage(_damage, null);
                            //var n = foundIDamagable[0].GetAttackTargetTransform().name;
                            //Debug.Log(n);
                        }catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                    else
                    {
                        Debug.Log("Enemy left range, cancelling shot.");
                        if (_useGameObjectPooling) { _gameObjectPool.PoolGameObject(shootObject); }
                        else { Destroy(shootObject); }
                    }
                }
                if (_catapultRetractAnimationTriggerName != "") { _catapultAnimator.SetTrigger(_catapultRetractAnimationTriggerName); }
                if (_useGameObjectPooling) { _gameObjectPool.PoolGameObject(shootObject); }
                else { Destroy(shootObject); }
            });
        }
    }
}