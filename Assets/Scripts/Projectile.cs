using System.Collections.Generic;
using UnityEngine;

namespace Tanks
{
    public class Projectile : MonoBehaviour
    {
        private static readonly Dictionary<int, int> ActiveByOwner = new Dictionary<int, int>();
        private static readonly List<Projectile> Active = new List<Projectile>();

        [SerializeField] private float _speed = 28f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private float _damage = 10f;

        private bool _hasHit;
        private Transform _owner;
        private Collider _collider;
        private Rigidbody _rigidbody;
        private WeaponConfig _config;
        private float _dieAt;

        public float Damage => _damage;
        public Transform Owner => _owner;
        public float Speed => _rigidbody != null ? _rigidbody.linearVelocity.magnitude : 0f;

        public static int CountActiveFor(Transform owner)
        {
            return owner != null && ActiveByOwner.TryGetValue(owner.GetInstanceID(), out int count) ? count : 0;
        }

        public static int TotalActive => Active.Count;

        public void Launch(WeaponConfig config, Transform owner, Vector3 direction)
        {
            _speed = config.ProjectileSpeed;
            _lifetime = config.ProjectileLifetime;
            _damage = config.Damage;
            _owner = owner;
            _config = config;

            transform.localScale = Vector3.one * config.ProjectileScale;
            transform.SetPositionAndRotation(transform.position, Quaternion.LookRotation(direction.normalized, Vector3.up));

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer item in renderers)
            {
                item.material.color = config.Tint;
            }

            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.time = config.TrailDuration;
                trail.startColor = config.Tint;
                trail.endColor = new Color(config.Tint.r, config.Tint.g, config.Tint.b, 0f);
                trail.startWidth = config.ProjectileScale * 0.3f;
                trail.endWidth = 0f;
            }

            IgnoreOwnerColliders(owner);
            Register();
        }

        private void Awake()
        {
            Collider[] colliders = GetComponents<Collider>();
            _collider = colliders.Length > 0 ? colliders[0] : gameObject.AddComponent<SphereCollider>();

            for (int i = 1; i < colliders.Length; i++)
            {
                Destroy(colliders[i]);
            }

            _collider.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            _rigidbody = body;
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private void Update()
        {
            transform.position += transform.forward * _speed * Time.deltaTime;

            if (Time.time >= _dieAt)
            {
                Expire();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Projectile] {name} triggered by {other.name} owner={( _owner == null ? "null" : _owner.name )}");

            // A trigger sphere can report the Hull and the TurretBody of the same
            // tank in one frame; without this guard the hit is applied twice.
            if (_hasHit)
            {
                return;
            }

            if (_owner != null && other.transform.IsChildOf(_owner))
            {
                Debug.Log($"[Projectile] {name} ignored own collider {other.name}");
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(_damage, transform);
                _hasHit = true;
            }

            PlayImpactVFX(other);

            Expire();
        }

        private void PlayImpactVFX(Collider other)
        {
            Vector3 hitPoint = transform.position;
            Quaternion hitRotation = transform.rotation;
            Color tint = _config != null ? _config.Tint : Color.white;
            float randomPitch = Random.Range(0.95f, 1.05f);

            if (VFXSystem.Instance != null)
            {
                VFXSystem.Instance.PlayImpact(hitPoint, hitRotation,
                    _config != null ? _config.ImpactSize : 0.3f,
                    _config != null ? _config.ImpactDuration : 0.15f,
                    tint);
            }

            if (AudioSystem.Instance != null && _config != null)
            {
                AudioSystem.Instance.PlayImpact(hitPoint,
                    _config.ImpactBaseFrequency,
                    _config.ImpactDuration,
                    _config.ImpactVolume,
                    randomPitch);
            }
        }

        private void IgnoreOwnerColliders(Transform owner)
        {
            if (owner == null || _collider == null)
            {
                return;
            }

            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            foreach (Collider ownerCollider in ownerColliders)
            {
                Physics.IgnoreCollision(_collider, ownerCollider, true);
            }
        }

        private void Register()
        {
            _dieAt = Time.time + _lifetime;

            Active.Add(this);

            if (_owner != null)
            {
                int id = _owner.GetInstanceID();
                ActiveByOwner.TryGetValue(id, out int count);
                ActiveByOwner[id] = count + 1;
            }
        }

        private void OnDestroy()
        {
            Active.Remove(this);

            if (_owner == null)
            {
                return;
            }

            int id = _owner.GetInstanceID();
            if (ActiveByOwner.TryGetValue(id, out int count))
            {
                if (count <= 1)
                {
                    ActiveByOwner.Remove(id);
                }
                else
                {
                    ActiveByOwner[id] = count - 1;
                }
            }
        }

        private void Expire()
        {
            // Destruction explosion on impact (heavy weapons / cannon already explode in WeaponController,
            // but every projectile leaves a small spark burst for readability).
            if (_config != null && _config.IsHeavy)
            {
                if (VFXSystem.Instance != null)
                {
                    VFXSystem.Instance.PlayExplosion(transform.position, _config.ExplosionSize * 0.5f,
                        _config.ExplosionDuration * 0.6f, _config.Tint);
                }
                if (AudioSystem.Instance != null)
                {
                    AudioSystem.Instance.PlayExplosion(transform.position, _config.ExplosionVolume * 0.5f, 1f);
                }
            }

            Destroy(gameObject);
        }
    }
}