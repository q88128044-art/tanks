using UnityEngine;

namespace Tanks
{
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "Tanks/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private string _displayName = "Weapon";

        [Header("Projectile")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _projectileScale = 1f;
        [SerializeField] private float _projectileSpeed = 28f;
        [SerializeField] private Color _tint = Color.white;

        [Header("Damage")]
        [SerializeField] private float _damage = 10f;

        [Header("Firing")]
        [SerializeField] private float _cooldown = 0.5f;
        [SerializeField] private float _projectileLifetime = 3f;
        [SerializeField] private int _maxActiveProjectiles = 0;

        public string DisplayName => _displayName;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileScale => _projectileScale;
        public float ProjectileSpeed => _projectileSpeed;
        public Color Tint => _tint;
        public float Damage => _damage;
        public float Cooldown => _cooldown;
        public float ProjectileLifetime => _projectileLifetime;
        public int MaxActiveProjectiles => _maxActiveProjectiles;
        public bool LimitsActiveProjectiles => _maxActiveProjectiles > 0;
    }
}