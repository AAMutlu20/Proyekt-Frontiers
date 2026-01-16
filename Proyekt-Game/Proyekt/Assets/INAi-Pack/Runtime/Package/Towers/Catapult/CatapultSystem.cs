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

        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _height;
        [SerializeField] private float _animationDuration;

        [SerializeField] private Ease _doTweenEase;

        //[SerializeField] private float _heightPerDistance;

        [SerializeField] private GameObject _objectToShootPrefab;

        [SerializeField] private Animator _catapultAnimator;
        [SerializeField] private string _catapultSlingAnimationtriggerName;
        [SerializeField] private GameObject _currentlySpawnedShootObject;

        [SerializeField] private Transform _slingObjectParent;

        [SerializeField] PathType _pathType;

        //[SerializeField] private int _halfArcResolution = 3;

        private void Start()
        {
            _catapultTargetDetectionSystem.OnDetectedNewGameObjectObject.AddListener(DetectedNewPossibleTarget);
            _shootCooldownTimer.OnTimeElapsed += ShootCatapultWithCooldown;
        }

        private void Update()
        {
            _shootCooldownTimer.UpdateTimer(Time.deltaTime);
        }

        private void DetectedNewPossibleTarget(GameObject detectedGameObject)
        {
            if (_targetTransform == null)
            {
                _targetTransform = detectedGameObject.transform;
                _catapultYRotationSystem.Target = detectedGameObject.transform;
                // If the timer is paused
                _shootCooldownTimer.ResetCurrentTime();
                _shootCooldownTimer.StartTimer();
            }
        }

        public void ShootCatapultWithCooldown()
        {
            _currentlySpawnedShootObject = Instantiate(_objectToShootPrefab);
            _currentlySpawnedShootObject.transform.parent = _slingObjectParent;
            _currentlySpawnedShootObject.transform.localPosition = Vector3.zero;
            _currentlySpawnedShootObject.transform.rotation = _slingObjectParent.rotation;
            _catapultAnimator.SetTrigger(_catapultSlingAnimationtriggerName);
            //Shoot();

            _shootCooldownTimer.ResetCurrentTime();
            _shootCooldownTimer.StartTimer();
        }

        // When the animation completes it will call the event to shoot.
        public void Shoot()
        {
            _currentlySpawnedShootObject.transform.SetParent(null);
            MoveShootingObjectAlongDoTweenArc();
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
                ITriggerable foundITriggerable = shootObject.GetComponent<ITriggerable>();
                if (foundITriggerable != null) { foundITriggerable.Trigger(gameObject); }
            });
        }
    }
}