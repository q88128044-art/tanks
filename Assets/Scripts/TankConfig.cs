using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// All tunable stats of one tank type: health, damage, movement, weapons
    /// and (for enemies) AI behaviour. Applied by TankConfigApplier.
    /// </summary>
    [CreateAssetMenu(fileName = "TankConfig", menuName = "Tanks/Tank Config")]
    public class TankConfig : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private string _displayName = "Tank";

        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;

        [Header("Damage")]
        [Tooltip("Shell damage = WeaponConfig.Damage x DamageMultiplier.")]
        [SerializeField] private float _damageMultiplier = 1f;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _turnSpeed = 110f;
        [Range(0f, 1f)] [SerializeField] private float _reverseSpeedFactor = 0.6f;
        [SerializeField] private float _acceleration = 12f;

        [Header("Weapons")]
        [SerializeField] private WeaponConfig[] _weapons;

        [Header("AI (enemies only)")]
        [SerializeField] private float _chaseRange = 20f;
        [SerializeField] private float _attackRange = 12f;
        [SerializeField] private float _shootAngle = 10f;
        [SerializeField] private float _shootCooldown = 1.6f;
        [SerializeField] private float _turretTurnSpeed = 110f;
        [SerializeField] private float _loseTargetTime = 3f;
        [SerializeField] private Vector2 _patrolWait = new Vector2(0.5f, 2.5f);

        public string DisplayName => _displayName;
        public float MaxHealth => _maxHealth;
        public float DamageMultiplier => _damageMultiplier;
        public float MoveSpeed => _moveSpeed;
        public float TurnSpeed => _turnSpeed;
        public float ReverseSpeedFactor => _reverseSpeedFactor;
        public float Acceleration => _acceleration;
        public WeaponConfig[] Weapons => _weapons;
        public float ChaseRange => _chaseRange;
        public float AttackRange => _attackRange;
        public float ShootAngle => _shootAngle;
        public float ShootCooldown => _shootCooldown;
        public float TurretTurnSpeed => _turretTurnSpeed;
        public float LoseTargetTime => _loseTargetTime;
        public Vector2 PatrolWait => _patrolWait;
    }
}
