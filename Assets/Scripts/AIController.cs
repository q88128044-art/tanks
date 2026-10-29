using UnityEngine;
using UnityEngine.AI;

namespace Tanks
{
    /// <summary>
    /// Enemy tank brain with three states:
    /// Patrol - loops the waypoint route, waiting a random time at each point.
    /// Chase  - player is within chaseRange AND visible (Raycast, no shooting through walls).
    /// Attack - player within attackRange: the hull stops, the turret turns and
    ///          fires only when it points within shootAngle of the player.
    /// After losing sight of the player for loseTargetTime it returns to the
    /// nearest waypoint. Movement goes through the NavMeshAgent, so the tank
    /// never leaves the NavMesh.
    /// </summary>
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
        [SerializeField] private Vector2 _waitTimeRange = new Vector2(0.5f, 2.5f);
        [SerializeField] private float _waypointTolerance = 1.2f;

        [Header("Detection")]
        [SerializeField] private float _chaseRange = 20f;
        [SerializeField] private float _attackRange = 12f;
        [SerializeField] private float _loseTargetTime = 3f;
        [SerializeField] private LayerMask _groundMask = 1;

        [Header("Combat")]
        [SerializeField] private float _shootAngle = 10f;
        [SerializeField] private float _shootCooldown = 1.6f;
        [SerializeField] private float _turretTurnSpeed = 110f;

        [Header("References")]
        [SerializeField] private TankTurret _turret;
        [SerializeField] private WeaponController _weapon;
        [SerializeField] private Transform _model;

        [Header("Debug")]
        [SerializeField] private bool _logStateChanges = true;

        private NavMeshAgent _agent;
        private TankHealth _health;
        private Transform _player;
        private TankHealth _playerHealth;
        private int _sightMask;
        private int _currentWaypointIndex;
        private bool _waiting;
        private float _waitUntil;
        private float _lostTimer;
        private float _nextShootTime;
        private float _nextRepath;
        private Vector3 _lastKnownPosition;

        public AIState CurrentState => _currentState;
        public Transform[] Waypoints => _waypoints;
        public bool CanSeePlayer { get; private set; }
        public float ChaseRange => _chaseRange;
        public float AttackRange => _attackRange;

        public void ApplyConfig(TankConfig config)
        {
            _chaseRange = config.ChaseRange;
            _attackRange = config.AttackRange;
            _shootAngle = config.ShootAngle;
            _shootCooldown = config.ShootCooldown;
            _turretTurnSpeed = config.TurretTurnSpeed;
            _loseTargetTime = config.LoseTargetTime;
            _waitTimeRange = config.PatrolWait;
        }

        public void SetWaypoints(Transform[] waypoints)
        {
            _waypoints = waypoints;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<TankHealth>();

            if (_turret == null) _turret = GetComponentInChildren<TankTurret>();
            if (_weapon == null) _weapon = GetComponentInChildren<WeaponController>();
            if (_model == null) _model = transform;

            if (_waypoints == null || _waypoints.Length == 0)
            {
                _waypoints = FindWaypointsInScene();
            }

            // Line of sight ignores our own layer (own colliders) and projectiles.
            _sightMask = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            if (projectileLayer >= 0)
            {
                _sightMask &= ~(1 << projectileLayer);
            }

            FindPlayer();
        }

        private void Start()
        {
            _agent.stoppingDistance = 0.3f;
            _agent.autoBraking = true;
            EnterState(AIState.Patrol);
        }

        private Transform[] FindWaypointsInScene()
        {
            GameObject routeRoot = GameObject.Find("AI_Route");
            if (routeRoot == null)
            {
                return new Transform[0];
            }

            Transform[] found = new Transform[routeRoot.transform.childCount];
            for (int i = 0; i < found.Length; i++)
            {
                found[i] = routeRoot.transform.GetChild(i);
            }

            return found;
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("PlayerTank");
            }

            _player = playerObj != null ? playerObj.transform : null;
            _playerHealth = playerObj != null ? playerObj.GetComponent<TankHealth>() : null;
        }

        private void Update()
        {
            if (_health != null && !_health.IsAlive)
            {
                enabled = false;
                return;
            }

            if (!_agent.isActiveAndEnabled)
            {
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);
                }

                return;
            }

            if (_player == null)
            {
                FindPlayer();
            }

            bool playerAlive = _player != null && (_playerHealth == null || _playerHealth.IsAlive);
            float distance = playerAlive ? FlatDistance(transform.position, _player.position) : float.MaxValue;
            CanSeePlayer = playerAlive && distance <= _chaseRange && HasLineOfSight();

            switch (_currentState)
            {
                case AIState.Patrol:
                    UpdatePatrol();
                    break;
                case AIState.Chase:
                    UpdateChase(playerAlive, distance);
                    break;
                case AIState.Attack:
                    UpdateAttack(playerAlive, distance);
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (CanSeePlayer)
            {
                SwitchState(AIState.Chase);
                return;
            }

            RelaxTurret();

            if (_waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            if (_waiting)
            {
                if (Time.time >= _waitUntil)
                {
                    _waiting = false;
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                    MoveTo(_waypoints[_currentWaypointIndex].position);
                }

                return;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= Mathf.Max(_agent.stoppingDistance, _waypointTolerance))
            {
                _waiting = true;
                _waitUntil = Time.time + Random.Range(_waitTimeRange.x, _waitTimeRange.y);
            }
        }

        private void UpdateChase(bool playerAlive, float distance)
        {
            if (!playerAlive)
            {
                SwitchState(AIState.Patrol);
                return;
            }

            if (CanSeePlayer)
            {
                _lostTimer = 0f;
                _lastKnownPosition = _player.position;
                if (distance <= _attackRange)
                {
                    SwitchState(AIState.Attack);
                    return;
                }

                AimTurret(_player.position);
            }
            else
            {
                _lostTimer += Time.deltaTime;
                if (_lostTimer >= _loseTargetTime)
                {
                    SwitchState(AIState.Patrol);
                    return;
                }

                RelaxTurret();
            }

            if (Time.time >= _nextRepath)
            {
                _nextRepath = Time.time + 0.25f;
                MoveTo(_lastKnownPosition);
            }
        }

        private void UpdateAttack(bool playerAlive, float distance)
        {
            if (!playerAlive)
            {
                SwitchState(AIState.Patrol);
                return;
            }

            StopHull();

            if (!CanSeePlayer || distance > _attackRange * 1.15f)
            {
                // Lost sight (wall) or the player drove away: chase to the last known position.
                SwitchState(AIState.Chase);
                return;
            }

            _lostTimer = 0f;
            _lastKnownPosition = _player.position;

            bool aimed = AimTurret(_player.position);
            if (aimed && Time.time >= _nextShootTime && _weapon != null && _weapon.Fire())
            {
                _nextShootTime = Time.time + _shootCooldown;
            }
        }

        private void SwitchState(AIState next)
        {
            if (next == _currentState)
            {
                return;
            }

            if (_logStateChanges)
            {
                Debug.Log($"[AI] {name}: {_currentState} -> {next}");
            }

            _currentState = next;
            EnterState(next);
        }

        private void EnterState(AIState state)
        {
            switch (state)
            {
                case AIState.Patrol:
                    _lostTimer = 0f;
                    _waiting = false;
                    if (_waypoints != null && _waypoints.Length > 0)
                    {
                        _currentWaypointIndex = NearestWaypoint();
                        MoveTo(_waypoints[_currentWaypointIndex].position);
                    }

                    break;
                case AIState.Chase:
                    _nextRepath = 0f;
                    if (_agent.isOnNavMesh) _agent.isStopped = false;
                    break;
                case AIState.Attack:
                    _lostTimer = 0f;
                    StopHull();
                    break;
            }
        }

        private void MoveTo(Vector3 destination)
        {
            if (!_agent.isOnNavMesh)
            {
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        private void StopHull()
        {
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
        }

        private int NearestWaypoint()
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;
                float d = FlatDistance(transform.position, _waypoints[i].position);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Turns the turret toward the target. Returns true when it is within shootAngle.</summary>
        private bool AimTurret(Vector3 target)
        {
            if (_turret == null)
            {
                return true;
            }

            Transform t = _turret.transform;
            Vector3 direction = target - t.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            t.rotation = Quaternion.RotateTowards(t.rotation, desired, _turretTurnSpeed * Time.deltaTime);

            Vector3 forward = t.forward;
            forward.y = 0f;
            return Vector3.Angle(forward, direction) <= _shootAngle;
        }

        private void RelaxTurret()
        {
            if (_turret == null)
            {
                return;
            }

            Transform t = _turret.transform;
            t.localRotation = Quaternion.RotateTowards(t.localRotation, Quaternion.identity, _turretTurnSpeed * 0.5f * Time.deltaTime);
        }

        private bool HasLineOfSight()
        {
            Vector3 origin = (_turret != null ? _turret.transform.position : transform.position) + Vector3.up * 0.3f;
            Vector3 target = _player.position + Vector3.up * 0.5f;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance < 0.01f)
            {
                return true;
            }

            if (Physics.Raycast(origin, direction / distance, out RaycastHit hit, distance, _sightMask, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.transform.IsChildOf(_player);
            }

            return true;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private Color StateColor()
        {
            switch (_currentState)
            {
                case AIState.Chase: return new Color(1f, 0.85f, 0.1f);
                case AIState.Attack: return new Color(1f, 0.2f, 0.15f);
                default: return new Color(0.2f, 0.9f, 0.3f);
            }
        }

        private void OnDrawGizmos()
        {
            Color stateColor = StateColor();

            // Current state marker above the tank.
            Gizmos.color = stateColor;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2.6f, 0.35f);

            Gizmos.color = new Color(stateColor.r, stateColor.g, stateColor.b, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _chaseRange);
            Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            if (_waypoints != null && _waypoints.Length > 1)
            {
                Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
                for (int i = 0; i < _waypoints.Length; i++)
                {
                    Transform a = _waypoints[i];
                    Transform b = _waypoints[(i + 1) % _waypoints.Length];
                    if (a == null || b == null) continue;
                    Gizmos.DrawLine(a.position, b.position);
                    Gizmos.DrawWireCube(a.position, Vector3.one * 0.6f);
                }
            }

            if (Application.isPlaying && _player != null && _currentState != AIState.Patrol)
            {
                Gizmos.color = CanSeePlayer ? stateColor : Color.gray;
                Gizmos.DrawLine(transform.position + Vector3.up, _player.position + Vector3.up * 0.5f);
            }
        }
    }
}
