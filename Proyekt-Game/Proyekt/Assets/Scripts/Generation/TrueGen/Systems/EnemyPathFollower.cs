using System.Collections.Generic;
using Generation.TrueGen.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Generation.TrueGen.Systems
{
    public class EnemyPathFollower : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float waypointReachedDistance = 0.2f;
        [SerializeField] private float damageAtEndOfPath = 1;
        [SerializeField] private int coinsGainedAtDefeat = 1;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo;
        
        private List<Vector3> _waypoints;
        private int _currentWaypointIndex;

        public float DamageAtEndOfPath => damageAtEndOfPath;
        public int CoinsGainAtDefeat => coinsGainedAtDefeat;

        public UnityEvent<EnemyPathFollower> onPathCompleteEvent = new();
        public UnityEvent<EnemyPathFollower> onEnemyKilled = new();
        public UnityEvent<int> onEnemyDestroyed = new();
        
        /// <summary>
        /// Initialize enemy with path chunks
        /// </summary>
        public void Initialize(List<ChunkNode> pathChunks)
        {
            _waypoints = new List<Vector3>();
            
            if (pathChunks == null || pathChunks.Count == 0)
            {
                Debug.LogError("EnemyPathFollower: No path chunks provided!");
                return;
            }
            
            foreach (var chunk in pathChunks)
            {
                var waypoint = chunk.center;
                waypoint.y = chunk.yOffset; // Follow path height
                _waypoints.Add(waypoint);
            }
            
            if (_waypoints.Count > 0)
            {
                transform.position = _waypoints[0];
                if (showDebugInfo)
                    Debug.Log($"✓ Enemy initialized with {_waypoints.Count} waypoints at position {transform.position}");
            }
            else
            {
                Debug.LogError("EnemyPathFollower: Failed to create waypoints!");
            }
        }
        
        /// <summary>
        /// Set enemy movement speed (used by wave system)
        /// </summary>
        public void SetSpeed(float speed)
        {
            moveSpeed = speed;
        }
        
        /// <summary>
        /// Set damage dealt when reaching end
        /// </summary>
        public void SetDamage(float damage)
        {
            damageAtEndOfPath = damage;
        }
        
        /// <summary>
        /// Set coins gained on defeat
        /// </summary>
        public void SetCoinsReward(int coins)
        {
            coinsGainedAtDefeat = coins;
        }
        
        private void Update()
        {
            if (_waypoints == null || _waypoints.Count == 0)
            {
                if (showDebugInfo)
                    Debug.LogWarning("EnemyPathFollower: No waypoints to follow!");
                return;
            }
            
            if (_currentWaypointIndex >= _waypoints.Count)
            {
                OnPathComplete();
                return;
            }
            
            MoveTowardsCurrentWaypoint();
        }
        
        private void MoveTowardsCurrentWaypoint()
        {
            var targetWaypoint = _waypoints[_currentWaypointIndex];
            
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWaypoint,
                moveSpeed * Time.deltaTime
            );
            
            // Look at target
            var direction = (targetWaypoint - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // Check if reached waypoint
            var distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint);
            if (!(distanceToWaypoint < waypointReachedDistance)) return;
            _currentWaypointIndex++;
            if (showDebugInfo)
                Debug.Log($"✓ Reached waypoint {_currentWaypointIndex - 1}, moving to next");
        }
        
        private void OnPathComplete()
        {
            if (showDebugInfo)
                Debug.Log("✓ Enemy reached end of path!");
            
            // Invoke event before destroying
            onPathCompleteEvent?.Invoke(this);
            Destroy(gameObject);
        }
        
        private void OnDrawGizmos()
        {
            if (_waypoints == null || _waypoints.Count == 0)
                return;
            
            // Draw entire path
            Gizmos.color = Color.red;
            for (var i = 0; i < _waypoints.Count - 1; i++)
            {
                Gizmos.DrawLine(_waypoints[i], _waypoints[i + 1]);
            }
            
            // Draw all waypoints as spheres
            Gizmos.color = Color.yellow;
            foreach (var waypoint in _waypoints)
            {
                Gizmos.DrawSphere(waypoint, 0.3f);
            }
            
            // Draw current waypoint in different color
            if (_currentWaypointIndex >= _waypoints.Count) return;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_waypoints[_currentWaypointIndex], 0.5f);
                
            // Draw line from enemy to current waypoint
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _waypoints[_currentWaypointIndex]);
        }
        
        // Public method to check status
        public void PrintDebugInfo()
        {
            Debug.Log($"Enemy at {transform.position}");
            Debug.Log($"Waypoint {_currentWaypointIndex}/{_waypoints?.Count ?? 0}");
            Debug.Log($"Speed: {moveSpeed}, Has waypoints: {_waypoints is { Count: > 0 }}");
        }

        private void OnDestroy()
        {
            // Check if enemy was killed before reaching the end
            if (_waypoints == null || _currentWaypointIndex >= _waypoints.Count) return;
            // Enemy was killed by towers
            onEnemyKilled?.Invoke(this);
            onEnemyDestroyed?.Invoke(coinsGainedAtDefeat);  // Only give coins if killed
        }
    }
}