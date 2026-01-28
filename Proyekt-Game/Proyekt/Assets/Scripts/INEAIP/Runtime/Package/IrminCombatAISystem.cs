using IrminStaticUtilities;
using IrminStaticUtilities.Tools;
using IrminTimerPackage.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class IrminCombatAISystem : MonoBehaviour
    {

        [SerializeField] Animator _combatAttackAnimator;
        [SerializeField] string _triggerToCallOnAttack;
        [SerializeField] IrminTimer _attackCooldownTimer = new();
        [SerializeField] float _damage;

        [SerializeField] bool _targetingObstacle = false;

        [SerializeField] IrminTimer _attackPathCheckTimer = new();
        [SerializeField] GameObject _debugPathPrefab;

        [SerializeField] DetectionSystem _damagablesInWeaponRangeDetectionSystem;
        [SerializeField] FactionMemberComponent _factionMemberComponent;
        [SerializeField] IrminBaseHealthSystem _healthSystem;

        [SerializeField] protected bool _canLeadOtherCharacters;
        [SerializeField] protected List<IrminCombatAISystem> _combatAisUnderCommand = new();
        [SerializeField] protected EFormationMode _formationMode;
        [SerializeField] protected NavMeshAgent _navMeshAgent;
        [SerializeField] protected bool _agentMovingToTarget = false;
        [SerializeField] protected bool _agentReachedTarget = false;
        [SerializeField] protected NavMeshAgent _wallCheckNavMeshAgent;
        [SerializeField] protected NavMeshPath _wallCheckNavMeshAgentPath;
        [SerializeField] protected int _navMeshAreaMask = NavMesh.AllAreas;
        [SerializeField] protected float _targetPointMaxDistanceFromNavMesh = 20.0f;
        [SerializeField] protected LayerMask _damagablePathObstaclesLayerMask;
        [SerializeField] protected bool _useNavmeshAgentRadiusAsPathCheckRadius = false;
        [SerializeField] protected float _pathCheckRadius;

        [SerializeField] protected Transform _transformToRotate;
        [SerializeField] protected bool _inRotationDeadzone = false;
        [SerializeField] protected float _rotationSpeedPerSecond = 1.0f;
        [SerializeField] protected bool _faceTarget = false;
        [SerializeField] protected float _targetGroundRotationAngleDegrees;
        [SerializeField] protected float _rotationAngleDeadZone = 5.0f;
        [SerializeField] private float _minRot = 0.0f;
        [SerializeField] private float _maxRot = 0.0f;

        [SerializeField] private bool _chargeWall = false;
        [SerializeField] private bool _keepChargingWalls = false;
        [SerializeField] private TestingWallPiece _wallToCharge;

        [SerializeField] private IDamagable _attackingDamagable;
        [SerializeField] private GameObject _debugAttackingDamagable;
        [SerializeField] private IDamagable _attackingObstacle;
        [SerializeField] private GameObject _debugAttackingObstacle;
        [SerializeField] private bool _debug = false;

        
        // I can use this to check if the interface is null:
        //IrminStaticUtilities.Tools.UnityObjectAliveUtility()

        public UnityEvent OnEnteredRotationDeadzone;
        public UnityEvent OnExitedRotationDeadzone;
        //public UnityEvent OnReachedNavMeshTargetForAttack;

        public FactionMemberComponent FactionMemberComponent { get { return _factionMemberComponent; } }
        public bool CanLeadOtherCharacters { get { return _canLeadOtherCharacters; } }
        public EFormationMode FormationMode { get { return _formationMode; } }

        private void Awake()
        {
            _wallCheckNavMeshAgentPath = new();
            _wallCheckNavMeshAgent.updatePosition = false;
            _wallCheckNavMeshAgent.updateRotation = false;

            // Chat GPT Told me I could stop the bumping from warping the wall checker navmesh agent with this
            _wallCheckNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            _wallCheckNavMeshAgent.avoidancePriority = 99;
        }

        private void Start()
        {
            // Reserving Timer was bugged
            //IrminTimerControl.Singleton.ReserveTimer(gameObject, _attackPathCheckTimerTime, out int timerKey, out IrminTimer reservedIrminTimer);
            //_attackPathCheckTimer = reservedIrminTimer;
            _attackPathCheckTimer.OnTimeElapsed += RestartAttackPathCheckTimer;
            _attackCooldownTimer.OnTimeElapsed += Attack;
            if(_chargeWall && _wallToCharge != null)
            {
                AttackDamagable(_wallToCharge.GetRandomConnectedAttackableWallPiece());
            }
        }

        private void Update()
        {
            _attackPathCheckTimer.UpdateTimer(Time.deltaTime);
            _attackCooldownTimer.UpdateTimer(Time.deltaTime);
            // TODO: make this also with with the rotation functionality of IrminCharacters.
            // If IrminCharacterMode is active, set the rotation target of the IrminCharacter instead.
            Debug.Log(ReachedTarget());
            if (ReachedTarget())
            {
                IDamagable attackTarget = GetAttackTarget();
                if (_faceTarget && attackTarget != null)
                {

                    SetTargetRotationMinMaxRot(attackTarget.GetAttackTargetTransform());
                    UpdateMovementRotation();
                }
                
                if (_agentMovingToTarget)
                {
                    // We reached the target and are now staying close to it.
                    _agentMovingToTarget = false;
                    // We disable rotation update to be able to do so manually.
                    _navMeshAgent.updateRotation = false;
                    _agentReachedTarget = true;
                    
                    Debug.Log($"Get attack target went to update and was null? {attackTarget == null}");
                    Debug.Log($"Get attack target {attackTarget.GetAttackTargetTransform().name} went to update and was detected in range? {_damagablesInWeaponRangeDetectionSystem.GameObjectsInRange.Contains(attackTarget.GetAttackTargetTransform().gameObject)}");
                    if (attackTarget != null && _damagablesInWeaponRangeDetectionSystem.GameObjectsInRange.Contains(attackTarget.GetAttackTargetTransform().gameObject))
                    {
                        _attackCooldownTimer.ResetCurrentTime();
                        _attackCooldownTimer.StartTimer();
                    }

                    

                }
                if(_debug) Debug.Log("Reached Target");
                
            }
            else
            {
                // We are no longer close enough to the target to attack
                if(_agentReachedTarget)
                {
                    _agentReachedTarget = false;
                    _agentMovingToTarget = true;
                    _navMeshAgent.updateRotation = true;
                }
            }
        }

        private void Attack()
        {
            IDamagable foundAttackable = GetAttackTarget();
            if (foundAttackable.IsDestroyed()) 
            { 
                foundAttackable = null; 
            }
            if (foundAttackable == _attackingDamagable && _targetingObstacle)
            {
                _targetingObstacle = false;
                AttackDamagable(_attackingDamagable);
                return;
            }

            if(foundAttackable != null)
            {

                if (_damagablesInWeaponRangeDetectionSystem.GameObjectsInRange.Contains(foundAttackable.GetAttackTargetTransform().gameObject))
                {
                    // We are still in range, attack again.
                    _combatAttackAnimator.SetTrigger(_triggerToCallOnAttack);
                    bool _defeated = foundAttackable.Damage(_damage, _attackingDamagable);
                    _attackCooldownTimer.ResetCurrentTime();
                    _attackCooldownTimer.StartTimer();

                    if(_defeated || foundAttackable.IsDestroyed())
                    {
                        // set attack target to null, this could be the obstacle
                        foundAttackable = null;
                        // If main target is not null attack it.
                        if (_attackingDamagable != null) AttackDamagable(_attackingDamagable);
                    }
                }
            }

        }

        private IDamagable GetAttackTarget()
        {
            if(_targetingObstacle)
            {
                Debug.Log("Get attack target returned obstacle");
                return _attackingObstacle;
            }
            else if(!IrminStaticUtilities.Tools.UnityObjectAliveUtility.IsInterfaceObjectDestroyed(_attackingDamagable) && _attackingDamagable != null)
            {
                Debug.Log("Get attack target returned attack damagable");
                return _attackingDamagable;
            }
            else
            {
                Debug.Log("Get attack target returned null");
                return null;
            }

        }

        private bool ReachedTarget()
        {
            return !_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance;
        }
        private void RestartAttackPathCheckTimer()
        {
            // If we are not tracking an obstacle go for the target
            if (_attackingObstacle == null || IrminStaticUtilities.Tools.UnityObjectAliveUtility.IsInterfaceObjectDestroyed(_attackingObstacle))
            {
                // If the target is null, ignore
                if (IrminStaticUtilities.Tools.UnityObjectAliveUtility.IsInterfaceObjectDestroyed(_attackingDamagable)) { return; }

                _navMeshAgent.SetDestination(_attackingDamagable.GetAttackTargetTransform().position);
                _wallCheckNavMeshAgent.Warp(transform.position);
                if (_wallCheckNavMeshAgent.CalculatePath(_attackingDamagable.GetAttackTargetTransform().position, _wallCheckNavMeshAgentPath)) { Debug.Log($"CheckForObstacleAlongNavMeshPath DEBUG: Path check for attack damagable succeeded"); }
                else { Debug.Log($"CheckForObstacleAlongNavMeshPath DEBUG: Path check for attack damagable failed"); }
            }
            else
            {

                _navMeshAgent.SetDestination(_attackingObstacle.GetAttackTargetTransform().position);
                _wallCheckNavMeshAgent.Warp(transform.position);
                if (_wallCheckNavMeshAgent.CalculatePath(_attackingObstacle.GetAttackTargetTransform().position, _wallCheckNavMeshAgentPath)) { Debug.Log($"CheckForObstacleAlongNavMeshPath DEBUG: Path check for attack damagable succeeded"); }
                else { Debug.Log($"CheckForObstacleAlongNavMeshPath DEBUG: Path check for attack damagable failed"); }
            }


            CheckForObstacleAlongNavMeshPath(_wallCheckNavMeshAgentPath);
            _attackPathCheckTimer.ResetCurrentTime();
            _attackPathCheckTimer.StartTimer();
        }

        
        protected bool IsLeadingOtherCharacters()
        {
            return _combatAisUnderCommand.Count > 0;
        }

        protected List<IrminCombatAISystem> GetCombatAIsUnderCommand()
        {
            List<IrminCombatAISystem> combatAIsUnderCommandCopy = new(_combatAisUnderCommand);
            return combatAIsUnderCommandCopy;
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamagable foundDestructable = other.GetComponent<IDamagable>();
            if (foundDestructable != null)
            {

            }
        }

        private bool CheckForObstacleFromPoint(Vector3 pPoint, Vector3 pTarget)
        {
            Vector3 start = pPoint;
            Vector3 end = pTarget;

            List<RaycastHit> hits = Physics.SphereCastAll(start, _useNavmeshAgentRadiusAsPathCheckRadius ? _navMeshAgent.radius : _pathCheckRadius, (end - start).normalized, Vector3.Distance(start, end), _damagablePathObstaclesLayerMask).ToList<RaycastHit>();

            bool hitSomething = hits.Count > 0;
            //Physics.CapsuleCast(start, end, _useNavmeshAgentRadiusAsPathCheckRadius ? _navMeshAgent.radius : _pathCheckRadius, (end - start).normalized, out RaycastHit hitObstacle, Vector3.Distance(start, end), _damagablePathObstaclesLayerMask);
            if (hitSomething)
            {
                for (int j = 0; j < hits.Count; j++)
                {
                    Debug.Log($"CheckForObstacleAlongNavMeshPath DEBUG: Hit something {hits[j].collider.name}");
                    IDamagable foundAttackable = hits[j].collider.gameObject.GetComponent<IDamagable>();
                    // If the damagables faction ID is my faction and it is not the non assigned faction (!<= -1) I should not consider to attack it.
                    //if (foundAttackable.GetFactionID() == _factionMemberComponent.FactionID)
                    //{
                    //    Debug.Log($"CheckForObstacleAlongNavMeshPath DEBUG: Hit something {hits[j].collider.name} was friendly faction, continue.");
                    //    continue;
                    //}



                    if (foundAttackable == null) { Debug.LogError($"Found obstacle {hits[j].transform.name} on path without destuctable script attached"); }
                    if (foundAttackable.IsDestroyed()) { foundAttackable = null; }
                    if (_factionMemberComponent.FactionID != foundAttackable.GetFactionID() && foundAttackable.GetFactionID()! <= -1)
                    {
                        // If the found damagable is the target, this is no obstacle.
                        if (foundAttackable == _attackingDamagable) return false;
                        _targetingObstacle = true;
                        _attackingObstacle = foundAttackable;
                        _debugAttackingObstacle = foundAttackable.GetAttackTargetTransform().gameObject;
                        _targetingObstacle = true;
                        _navMeshAgent.destination = foundAttackable.GetAttackTargetTransform().position;
                        // Get weapon range and divide by 2 to be sure we stop within weapons range.
                        _navMeshAgent.stoppingDistance = foundAttackable.GetAttackRadius(); /*+ (_damagablesInWeaponRangeDetectionSystem.GetRadius() / 2);*/

                        // We only have to attack the first thing we hit, we dont have to cast any more spheres to check for obstacles.
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckForObstacleAlongNavMeshPath(NavMeshPath pNavMeshPath)
        {
            //return;
            Debug.Log("CheckForObstacleAlongNavMeshPath DEBUG: Starting");
            // We start at one because we want to check betwee a start and end position, so we need two positions. (0 and 1 in the first case then 1 and 2 etc......)
            Vector3 lastDirection = Vector3.zero;
            for (int i = 1; i < pNavMeshPath.corners.Length; i++)
            {
                Debug.Log("CheckForObstacleAlongNavMeshPath DEBUG: Checking a corner");
                 Vector3 start = pNavMeshPath.corners[i - 1];
                Vector3 end = pNavMeshPath.corners[i];
                GameObject startDebugFrefab = Instantiate(_debugPathPrefab, start, Quaternion.Euler((end - start).normalized));
                startDebugFrefab.name = $"start debug prefab index {i - 1} with obstacle {IrminStaticUtilities.Tools.UnityObjectAliveUtility.IsInterfaceObjectDestroyed(_attackingObstacle)} {_attackingObstacle == null}";
                lastDirection = (end - start).normalized;
                GameObject endDebugFrefab = Instantiate(_debugPathPrefab, end, Quaternion.Euler(lastDirection));
                endDebugFrefab.name = $"end debug prefab index {i} with obstacle {IrminStaticUtilities.Tools.UnityObjectAliveUtility.IsInterfaceObjectDestroyed(_attackingObstacle)} {_attackingObstacle == null}";
                Debug.DrawLine(start, end, Color.red, 10000f);

                List<RaycastHit> hits = Physics.SphereCastAll(start, _useNavmeshAgentRadiusAsPathCheckRadius ? _navMeshAgent.radius : _pathCheckRadius, (end - start).normalized, Vector3.Distance(start, end), _damagablePathObstaclesLayerMask).ToList<RaycastHit>();
                
                bool hitSomething = hits.Count > 0;
                //Physics.CapsuleCast(start, end, _useNavmeshAgentRadiusAsPathCheckRadius ? _navMeshAgent.radius : _pathCheckRadius, (end - start).normalized, out RaycastHit hitObstacle, Vector3.Distance(start, end), _damagablePathObstaclesLayerMask);
                if (hitSomething)
                {
                    for (int j = 0; j < hits.Count; j++)
                    {
                        Debug.Log($"CheckForObstacleAlongNavMeshPath DEBUG: Hit something {hits[j].collider.name}");
                        IDamagable foundAttackable = hits[j].collider.gameObject.GetComponent<IDamagable>();
                        // If the damagables faction ID is my faction and it is not the non assigned faction (!<= -1) I should not consider to attack it.
                        //if (foundAttackable.GetFactionID() == _factionMemberComponent.FactionID)
                        //{
                        //    Debug.Log($"CheckForObstacleAlongNavMeshPath DEBUG: Hit something {hits[j].collider.name} was friendly faction, continue.");
                        //    continue;
                        //}
                        

                        
                        if (foundAttackable == null) { Debug.LogError($"Found obstacle {hits[j].transform.name} on path without destuctable script attached"); }
                        if (foundAttackable.IsDestroyed()) { foundAttackable = null; }
                        if (_factionMemberComponent.FactionID != foundAttackable.GetFactionID() && foundAttackable.GetFactionID() !<= -1 )
                        {
                            // If the found damagable is the target, this is no obstacle.
                            if (foundAttackable == _attackingDamagable) return false;
                            _targetingObstacle = true;
                            _attackingObstacle = foundAttackable;
                            _debugAttackingObstacle = foundAttackable.GetAttackTargetTransform().gameObject;
                            _targetingObstacle = true;
                            _navMeshAgent.destination = foundAttackable.GetAttackTargetTransform().position;
                            // Get weapon range and divide by 2 to be sure we stop within weapons range.
                            _navMeshAgent.stoppingDistance = foundAttackable.GetAttackRadius(); /*+ (_damagablesInWeaponRangeDetectionSystem.GetRadius() / 2);*/

                            // We only have to attack the first thing we hit, we dont have to cast any more spheres to check for obstacles.
                            return true;
                        }
                    }
                }
            }
            // No obstacle in path was found
            return false;
        }

        private void UpdateMovementRotation()
        {
            Debug.Log("Updating rotation");
            if (EulerRotationUtility.CheckIfBetweenAngles(_transformToRotate.rotation.eulerAngles.y, _minRot, _maxRot))
            {
                if (!_inRotationDeadzone)
                {
                    _inRotationDeadzone = true;
                    OnEnteredRotationDeadzone?.Invoke();
                }
                //Debug.Log("Player Character within deadzone");
                return;
            }
            else
            {
                if (_inRotationDeadzone)
                {
                    _inRotationDeadzone = false;
                    OnExitedRotationDeadzone?.Invoke();
                }
            }

            if (EulerRotationUtility.RotateBackOrForward(transform.rotation.eulerAngles.y, _targetGroundRotationAngleDegrees))
            {
                _transformToRotate.Rotate(0, _rotationSpeedPerSecond * Time.deltaTime, 0);
            }
            else
            {
                _transformToRotate.Rotate(0, -_rotationSpeedPerSecond * Time.deltaTime, 0);
            }
        }

        private void UpdateMinMaxRot()
        {
            _minRot = EulerRotationUtility.ConvertTo360DegreeAngle(_targetGroundRotationAngleDegrees - (_rotationAngleDeadZone / 2));
            _maxRot = EulerRotationUtility.ConvertTo360DegreeAngle(_targetGroundRotationAngleDegrees + (_rotationAngleDeadZone / 2));
        }

        public void SetTargetRotationMinMaxRot(Transform pTransform)
        {
            _targetGroundRotationAngleDegrees = GetLookAtRotation(_transformToRotate, pTransform).eulerAngles.y;
            UpdateMinMaxRot();
        }

        
        Quaternion GetLookAtRotation(Transform from, Transform to)
        {
            Vector3 direction = to.position - from.position;
            return Quaternion.LookRotation(direction, Vector3.up);
        }

        public void DebugAttackSelectedDamagable()
        {
            IDamagable foundIDamagable = _debugAttackingDamagable.GetComponent<IDamagable>();
            if (foundIDamagable == null) { Debug.LogError($"DebugAttackSelectedDamagable ERROR: Damagable invalid."); return; }
            AttackDamagable(foundIDamagable);
        }

        public bool AttackDamagable(IDamagable pIDamagable)
        {
            _targetingObstacle = false;
            _attackingObstacle = null;
            _debugAttackingObstacle = null;
            if (IrminStaticUtilities.Tools.UnityObjectAliveUtility.IsInterfaceObjectDestroyed(pIDamagable)) { Debug.LogWarning($"{name} Tried to set attacking IDamagable to null interface."); return false; }
            bool isCloseEnoughToNavmesh = NavMesh.SamplePosition(pIDamagable.GetAttackTargetTransform().position, out NavMeshHit navMeshHit, _targetPointMaxDistanceFromNavMesh, _navMeshAreaMask);
            if (!isCloseEnoughToNavmesh) { Debug.LogWarning($"{name} Tried to set attacking IDamagable {pIDamagable.GetAttackTargetTransform().name} but its attack target transform was not close enough to the NavMesh."); return false; }
            if (!SetNavMeshAgentDestination(navMeshHit.position)) { return false; }
            _navMeshAgent.stoppingDistance = pIDamagable.GetAttackRadius(); /*+ (_damagablesInWeaponRangeDetectionSystem.GetRadius() / 2);*/
            _attackingDamagable = pIDamagable;
            _debugAttackingDamagable = pIDamagable.GetAttackTargetTransform().gameObject;

            _attackPathCheckTimer.StartTimer();
            return true;
        }

        private bool SetNavMeshAgentDestination(Vector3 pDestination)
        {
            _navMeshAgent.updateRotation = true;
            NavMeshPath calculatedPath = new NavMeshPath();
           _navMeshAgent.CalculatePath(pDestination, calculatedPath);
            if (calculatedPath.status == NavMeshPathStatus.PathPartial) 
            { 
                Debug.Log("Partial"); 
                if(CheckForObstacleFromPoint(calculatedPath.corners[calculatedPath.corners.Length - 1], pDestination))
                {
                    _agentMovingToTarget = true;
                    return true;
                }
                else
                {
                    Debug.LogWarning($"{name} Could not find obstacle blocking partial route.");
                }
            }
            if (!_navMeshAgent.SetDestination(pDestination)) 
            {
                Debug.LogError("Cannot Reach Destination.");
                return false; 
            }
            // We enable possibly disable update rotation variable.
            _navMeshAgent.updateRotation = true;
            _agentMovingToTarget = true;
            return true;
        }

        private void StopAttackingDamagable()
        {
            if (_attackingDamagable == null) return;
            _attackPathCheckTimer.ResetCurrentTime();
            _attackPathCheckTimer.PauseTimer();
            _attackingDamagable = null;
            _debugAttackingDamagable = null;
        }
    }
}