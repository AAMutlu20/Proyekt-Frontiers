using DG.Tweening;
using IrminTimerPackage.Tools;
using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class CatapultSystem : MonoBehaviour
    {
        [SerializeField] private DetectionSystem _catapultTargetDetectionSystem;
        [SerializeField] private YRotationSystem _catapultYRotationSystem;
        // UNUSED ATM, but could be implemented
        // [SerializeField] private FactionMemberComponent _factionMemberComponent; 

        [SerializeField] IrminTimer _shootCooldownTimer = new();
        [SerializeField] IrminTimer _shootTimer = new();

        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _height;
        [SerializeField] private float _animationDuration;

        [SerializeField] private Ease _doTweenEase;

        //[SerializeField] private float _heightPerDistance;

        [SerializeField] private GameObject _objectToShootPrefab;
        [SerializeField] private GameObject _shotHitEffectPrefab;

        [SerializeField] private Animator _catapultAnimator;
        [SerializeField] private string _catapultSlingAnimationtriggerName;
        [SerializeField] private GameObject _currentlySpawnedShootObject;

        [SerializeField] private Transform _slingObjectParent;

        [SerializeField] PathType _pathType;

        [SerializeField] private float _explosionRadius = 10f;

        [SerializeField] private LayerMask _enemyLayerMask;

        [SerializeField] private float _damage = 2f;

        //[SerializeField] private int _halfArcResolution = 3;

        private void Start()
        {
            _catapultTargetDetectionSystem.OnDetectedNewGameObjectObject.AddListener(DetectedNewPossibleTarget);
            _catapultTargetDetectionSystem.OnNoLongerDetectedGameObject.AddListener(NoLongerDetectionPossibleTarget);
            _shootCooldownTimer.OnTimeElapsed += ShootCatapultWithCooldown;
            _shootTimer.OnTimeElapsed += Shoot;

        }



        private void Update()
        {
            _shootCooldownTimer.UpdateTimer(Time.deltaTime);
            _shootTimer.UpdateTimer(Time.deltaTime);
        }

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

        private void NoLongerDetectionPossibleTarget(GameObject pPossibleTarget)
        {
            if (_targetTransform == pPossibleTarget.transform)
            {
                GameObject foundNewTarget = GetFirstTargetFromDetector();
                if (foundNewTarget == null) { _targetTransform = null; _shootCooldownTimer.PauseTimer(); }
                else
                {
                    _targetTransform = foundNewTarget.transform;
                }

            }
        }

        public void ShootCatapultWithCooldown()
        {
            if (_targetTransform == null) { _shootCooldownTimer.ResetCurrentTime(); _shootCooldownTimer.PauseTimer(); return; }
            _currentlySpawnedShootObject = Instantiate(_objectToShootPrefab);
            _currentlySpawnedShootObject.transform.parent = _slingObjectParent;
            _currentlySpawnedShootObject.transform.localPosition = Vector3.zero;
            _currentlySpawnedShootObject.transform.rotation = _slingObjectParent.rotation;
            _catapultAnimator.SetTrigger(_catapultSlingAnimationtriggerName);
            _shootTimer.ResetCurrentTime();
            _shootTimer.StartTimer();
            //Shoot();

            _shootCooldownTimer.ResetCurrentTime();
            _shootCooldownTimer.StartTimer();
        }

        // When the animation completes it will call the event to shoot.
        public void Shoot()
        {
            if (_targetTransform == null) return;
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

            GameObject shootObject = _currentlySpawnedShootObject;
            _currentlySpawnedShootObject.transform.DOPath(path, _animationDuration, _pathType).SetEase(_doTweenEase).SetLookAt(0.01f).OnComplete(() =>
            {
                //ITriggerable foundITriggerable = shootObject.GetComponent<ITriggerable>();
                //if (foundITriggerable != null) { foundITriggerable.Trigger(gameObject); }
                // Replaced by:
                Vector3 origin = shootObject.transform.position;
                Collider[] hits = Physics.OverlapSphere(origin, _explosionRadius, _enemyLayerMask);
                Instantiate(_shotHitEffectPrefab, origin, Quaternion.identity);
                foreach (Collider collider in hits)
                {
                    IDamagable foundIDamagable = collider.GetComponent<IDamagable>();
                    if (foundIDamagable != null)
                    {
                        Debug.Log($"Catapult damaging {foundIDamagable.GetAttackTargetTransform().name} for {_damage}");
                        foundIDamagable.Damage(_damage, null);

                    }
                }
                Destroy(shootObject);
            });
        }
    }
}