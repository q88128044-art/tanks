using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tanks
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIController : MonoBehaviour
    {
        public enum AIState
        {
            Patrol,
            Chase,
            Attack
        }

        [Header("AI")]
        [SerializeField] private AIState _currentState = AIState.Patrol;
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _chaseRange = 15f;
        [SerializeField] private float _attackRange = 8f;
        [SerializeField] private float _shootAngle = 25f;
        [SerializeField] private float _shootCooldown = 2f;
        [SerializeField] private float _loseTargetTime = 3f;
        [SerializeField] private LayerMask _groundMask = 1;

        [Header("References")]
        [SerializeField] private TankTurret _turret;
        [SerializeField] private WeaponController _weapon;
        [SerializeField] private Transform _model;

        private NavMeshAgent _agent;
        private int _currentWaypointIndex;
        private float _nextShootTime;
        private float _loseTargetTimer;
        private Transform _player;
        private bool _wasInRange;

        public AIState CurrentState => _currentState;
        public Transform[] Waypoints => _waypoints;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (_turret == null)
                _turret = GetComponentInChildren<TankTurret>();
            if (_weapon == null)
                _weapon = GetComponentInChildren<WeaponController>();
            if (_model == null)
                _model = transform;

            if (_player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj == null)
                    playerObj = GameObject.Find("PlayerTank");
                _player = playerObj?.transform;
            }
        }

        private void OnEnable()
        {
            if (_agent.isOnNavMesh)
            {
                _agent.stoppingDistance = _attackRange;
            }
        }

        private void Update()
        {
            switch (_currentState)
            {
                case AIState.Patrol:
                    UpdatePatrol();
                    break;
                case AIState.Chase:
                    UpdateChase();
                    break;
                case AIState.Attack:
                    UpdateAttack();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (_waypoints == null || _waypoints.Length == 0)
                return;

            if (CanSeePlayer())
            {
                SwitchState(AIState.Chase);
                return;
            }

            if (_agent.isOnNavMesh && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            }

            SetDestination(_waypoints[_currentWaypointIndex].position);
        }

        private void UpdateChase()
        {
            if (!CanSeePlayer())
            {
                _loseTargetTimer += Time.deltaTime;
                if (_loseTargetTimer >= _loseTargetTime)
                {
                    _loseTargetTimer = 0f;
                    SwitchState(AIState.Patrol);
                    return;
                }
            }
            else
            {
                _loseTargetTimer = 0f;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
            if (distanceToPlayer <= _attackRange)
            {
                SwitchState(AIState.Attack);
                return;
            }

            SetDestination(_player.position);
        }

        private void UpdateAttack()
        {
            _wasInRange = true;

            if (_loseTargetTimer > 0f)
                _loseTargetTimer -= Time.deltaTime;

            if (_agent.isOnNavMesh)
                _agent.isStopped = true;

            if (_turret != null && _player != null)
            {
                Vector3 directionToPlayer = _player.position - _turret.transform.position;
                directionToPlayer.y = 0f;
                if (directionToPlayer.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up);
                    _turret.transform.rotation = Quaternion.Slerp(_turret.transform.rotation, targetRotation, 5f * Time.deltaTime);
                }

                float angle = Vector3.Angle(_turret.transform.forward, directionToPlayer.normalized);
                if (angle > _shootAngle)
                {
                    return;
                }
            }

            if (!CanSeePlayer())
            {
                _loseTargetTimer += Time.deltaTime;
                if (_loseTargetTimer >= _loseTargetTime)
                {
                    _loseTargetTimer = 0f;
                    _wasInRange = false;
                    SwitchState(AIState.Patrol);
                    return;
                }
                return;
            }

            _loseTargetTimer = 0f;

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
            if (distanceToPlayer > _attackRange)
            {
                SwitchState(AIState.Chase);
                return;
            }

            if (_weapon != null && Time.time >= _nextShootTime)
            {
                _weapon.Fire();
                _nextShootTime = Time.time + _shootCooldown;
            }
        }

        private void SwitchState(AIState newState)
        {
            if (_currentState == newState)
                return;

            _loseTargetTimer = 0f;
            _currentState = newState;

            if (newState == AIState.Patrol)
            {
                if (_agent.isOnNavMesh) _agent.isStopped = false;
                SetDestination(_waypoints[_currentWaypointIndex].position);
            }
            else if (newState == AIState.Chase)
            {
                if (_agent.isOnNavMesh) _agent.isStopped = false;
            }
            else if (newState == AIState.Attack)
            {
                if (_agent.isOnNavMesh) _agent.isStopped = true;
            }

            Debug.Log(gameObject.name + " AI state: " + newState);
        }

        private void SetDestination(Vector3 target)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.SetDestination(target);
            }
        }

        private bool CanSeePlayer()
        {
            if (_player == null)
                return false;

            Vector3 origin = _model != null ? _model.position + Vector3.up * 1.5f : transform.position + Vector3.up * 1.5f;
            Vector3 direction = _player.position + Vector3.up * 1.5f - origin;
            float distance = direction.magnitude;
            direction.Normalize();

            if (Physics.Raycast(origin, direction, distance, _groundMask))
            {
                return false;
            }

            float angle = Vector3.Angle(_model.forward, direction);
            if (angle > 90f)
                return false;

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (_model == null) return;

            Gizmos.color = Color.yellow;
            Vector3 origin = _model.position + Vector3.up * 1.5f;
            if (_player != null)
            {
                Vector3 target = _player.position + Vector3.up * 1.5f;
                Vector3 direction = target - origin;
                if (!Physics.Raycast(origin, direction.normalized, direction.magnitude, _groundMask))
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawLine(origin, target);
            }

            Gizmos.color = Color.red;
            if (_turret != null)
            {
                Vector3 forward = _turret.transform.forward;
                Vector3 left = Quaternion.Euler(0, -_shootAngle, 0) * forward;
                Vector3 right = Quaternion.Euler(0, _shootAngle, 0) * forward;
                Gizmos.DrawLine(_turret.transform.position, _turret.transform.position + left * _attackRange);
                Gizmos.DrawLine(_turret.transform.position, _turret.transform.position + right * _attackRange);
            }
        }
    }
}
