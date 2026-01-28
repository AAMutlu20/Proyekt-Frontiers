using IrminTimerPackage.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class TestingWallPiece : MonoBehaviour, IDamagable
    {
        [SerializeField] protected float _attackRadius;
        [SerializeField] protected FactionMemberComponent _factionMemberComponent;
        [SerializeField] protected IrminBaseHealthSystem _healthSystem;
        [SerializeField] protected GameObject _visualParent;
        [SerializeField] protected BoxCollider _boxCollider;
        [SerializeField] protected NavMeshObstacle _navMeshObstacle;
        [SerializeField] protected bool _destroyed = false;

        [SerializeField] protected NavMeshModifierVolume _wallCheckDifficultMovementVolume;

        [SerializeField] protected List<TestingWallPiece> _connectedWallPieces = new();
        [SerializeField] protected IrminTimer _getConnectedWallPiecedCutOffTimer = new();

        public FactionMemberComponent FactionMemberComponent { get { return _factionMemberComponent; } }

        public List<TestingWallPiece> ConnectedWallPieces { get { List<TestingWallPiece> connectedWallPiecesCopy = new(_connectedWallPieces); return connectedWallPiecesCopy; } }

        private void Awake()
        {
            _healthSystem.OnMinHealthReached += WallDestroyed;
        }

        private void Start()
        {

            Debug.Log($"CHECK: {GetAllConnectedWallPieces().Count}");
        }

        private void Update()
        {
            _getConnectedWallPiecedCutOffTimer.UpdateTimer(Time.deltaTime);
        }

        private void WallDestroyed()
        {
            _destroyed = true;
            Debug.Log($"Wall: {name} destroyed");
            //Vector3 worldCenter = _wallCheckDifficultMovementVolume.transform.TransformPoint(_wallCheckDifficultMovementVolume.center);
            //Vector3 worldSize = Vector3.Scale(_wallCheckDifficultMovementVolume.size, _wallCheckDifficultMovementVolume.transform.lossyScale);
            //Bounds createdBounds = new(worldCenter, new Vector3(4000, 4000, 4000));
            _wallCheckDifficultMovementVolume.enabled = false;
            _visualParent.SetActive(false);
            _boxCollider.enabled = false;
            _navMeshObstacle.enabled = false;
            //Destroy(this.gameObject);
            //SetAllOtherComnponents(false);
            //NavMeshSurfaceSystem.Singleton.Rebake(createdBounds);
        }

        public bool Damage(float pDamage, IDamagable _attackingIDamagable)
        {
            Debug.Log($"Wall: {name} being damaged for {pDamage}");
            return _healthSystem.Damage(pDamage, _attackingIDamagable);
        }

        public float GetAttackRadius()
        {
            return _attackRadius;
        }

        public Transform GetAttackTargetTransform()
        {
            return transform;
        }

        public int GetFactionID()
        {
            return _factionMemberComponent.FactionID;
        }

        public bool IsDestroyed()
        {
            return _destroyed;
        }

        private void SetAllOtherComnponents(bool pActive)
        {
            Component[] foundComponents = GetComponents<Component>();
            for (int i = 0; i < foundComponents.Length; i++)
            {
                if (foundComponents[i] == this) continue;
                foundComponents[i].gameObject.SetActive(pActive);
            }
        }

        public List<TestingWallPiece> GetAllConnectedWallPieces(bool pTakeIntoAccountDestroyedWallPieces = false)
        {
            List<TestingWallPiece> finishedConnectWallPieces = new();
            List<TestingWallPiece> WallPiecesToCheck = new();
            WallPiecesToCheck.AddRange(_connectedWallPieces);
            finishedConnectWallPieces.Add(this);

            _getConnectedWallPiecedCutOffTimer.ResetCurrentTime();
            _getConnectedWallPiecedCutOffTimer.StartTimer();
            
            while(WallPiecesToCheck.Count > 0)
            {
                if (_getConnectedWallPiecedCutOffTimer.Percentage >= 100) { Debug.LogError("WallPiece GetAllConnectedWallPieces ERROR: CutoffTimer Triggered."); break; }
                List<TestingWallPiece> currentlyCheckingWallPieces = new(WallPiecesToCheck);
                for (int i = 0; i < currentlyCheckingWallPieces.Count; i++)
                {
                    for (int j = 0; j < WallPiecesToCheck[i]._connectedWallPieces.Count; j++)
                    {
                        if (finishedConnectWallPieces.Contains(currentlyCheckingWallPieces[i]._connectedWallPieces[j]) || WallPiecesToCheck.Contains(WallPiecesToCheck[i]._connectedWallPieces[j]))
                        {
                            continue;
                        }
                        if (pTakeIntoAccountDestroyedWallPieces && currentlyCheckingWallPieces[i]._connectedWallPieces[j].IsDestroyed()) { continue; }
                        WallPiecesToCheck.Add(WallPiecesToCheck[i]._connectedWallPieces[j]);
                    }
                }
                finishedConnectWallPieces.AddRange(currentlyCheckingWallPieces);
                foreach (TestingWallPiece testingWallPieceDoneChecking in currentlyCheckingWallPieces)
                {
                    WallPiecesToCheck.Remove(testingWallPieceDoneChecking);
                }
            }
            return finishedConnectWallPieces;
        }

        public TestingWallPiece GetRandomConnectedAttackableWallPiece()
        {
            List<TestingWallPiece> foundWallPieces = GetAllConnectedWallPieces();
            return foundWallPieces[UnityEngine.Random.Range(0, foundWallPieces.Count)];
        }
    }
}