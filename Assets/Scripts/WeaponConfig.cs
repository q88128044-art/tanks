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

        [Header("VFX / SFX")]
        [SerializeField] private float _muzzleFlashSize = 0.5f;
        [SerializeField] private float _muzzleFlashDuration = 0.1f;
        [SerializeField] private Color _muzzleFlashColor = new Color(1f, 0.7f, 0.2f);
        [SerializeField] private float _trailDuration = 0.2f;
        [SerializeField] private float _impactSize = 0.3f;
        [SerializeField] private float _impactDuration = 0.15f;
        [SerializeField] private float _impactBaseFrequency = 400f;
        [SerializeField] private float _impactVolume = 0.6f;
        [SerializeField] private bool _isHeavy = false;
        [SerializeField] private float _explosionSize = 1.5f;
        [SerializeField] private float _explosionDuration = 0.3f;
        [SerializeField] private float _explosionVolume = 0.8f;
        [SerializeField] private float _cameraRecoil = 2f;

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
        public float MuzzleFlashSize => _muzzleFlashSize;
        public float MuzzleFlashDuration => _muzzleFlashDuration;
        public Color MuzzleFlashColor => _muzzleFlashColor;
        public float TrailDuration => _trailDuration;
        public float ImpactSize => _impactSize;
        public float ImpactDuration => _impactDuration;
        public float ImpactBaseFrequency => _impactBaseFrequency;
        public float ImpactVolume => _impactVolume;
        public bool IsHeavy => _isHeavy;
        public float ExplosionSize => _explosionSize;
        public float ExplosionDuration => _explosionDuration;
        public float ExplosionVolume => _explosionVolume;
        public float CameraRecoil => _cameraRecoil;
    }
}