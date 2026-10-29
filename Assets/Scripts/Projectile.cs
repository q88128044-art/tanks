using System.Collections.Generic;
using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Shell fired by a WeaponController. Every stat (speed, lifetime, damage,
    /// scale, tint, trail) comes from the WeaponConfig. Movement is swept with a
    /// sphere cast each frame, so fast shells never tunnel through thin objects,
    /// and the owner's colliders are always ignored. Damage is applied through
    /// IDamageable.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private static readonly Dictionary<Transform, int> ActiveByOwner = new Dictionary<Transform, int>();
        private static readonly List<Projectile> Active = new List<Projectile>();
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private float _speed = 28f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _radius = 0.1f;

        private WeaponConfig _config;
        private Transform _owner;
        private Transform _ownerKey;
        private bool _registered;
        private bool _done;
        private float _dieAt;
        private int _mask;

        public float Damage => _damage;
        public Transform Owner => _owner;
        public WeaponConfig Config => _config;
        public static int TotalActive => Active.Count;

        public static int CountActiveFor(Transform owner)
        {
            return owner != null && ActiveByOwner.TryGetValue(owner, out int count) ? count : 0;
        }

        public void Launch(WeaponConfig config, Transform owner, Vector3 direction, float damageMultiplier = 1f)
        {
            _config = config;
            _owner = owner;
            _speed = config.ProjectileSpeed;
            _lifetime = config.ProjectileLifetime;
            _damage = config.Damage * Mathf.Max(0f, damageMultiplier);
            _radius = 0.08f * config.ProjectileScale;
            _dieAt = Time.time + _lifetime;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.forward;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.localScale = Vector3.one * config.ProjectileScale;

            int projectileLayer = LayerMask.NameToLayer("Projectile");
            _mask = Physics.DefaultRaycastLayers;
            if (projectileLayer >= 0)
            {
                _mask &= ~(1 << projectileLayer);
            }

            // Collision is handled by the sweep below; the prefab collider would
            // only make shells trigger on each other.
            foreach (Collider own in GetComponentsInChildren<Collider>())
            {
                own.enabled = false;
            }

            ApplyLook(config);
            Register();
        }

        private void ApplyLook(WeaponConfig config)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (MeshRenderer item in GetComponentsInChildren<MeshRenderer>())
            {
                item.GetPropertyBlock(block);
                block.SetColor(BaseColorId, config.Tint);
                item.SetPropertyBlock(block);
            }

            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }

            if (VFXSystem.Instance != null && VFXSystem.Instance.AdditiveMaterial != null)
            {
                trail.sharedMaterial = VFXSystem.Instance.AdditiveMaterial;
            }

            Color head = Color.Lerp(config.Tint, Color.white, 0.35f);
            trail.time = Mathf.Max(0.05f, config.TrailDuration);
            trail.startColor = head;
            trail.endColor = new Color(config.Tint.r, config.Tint.g, config.Tint.b, 0f);
            trail.startWidth = 0.14f * config.ProjectileScale;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.1f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.Clear();
            trail.emitting = true;
        }

        private void Update()
        {
            if (_done)
            {
                return;
            }

            if (_config == null)
            {
                // Spawned without Launch (e.g. dragged into the scene): just expire.
                if (Time.time >= _dieAt + _lifetime)
                {
                    Destroy(gameObject);
                }

                return;
            }

            float step = _speed * Time.deltaTime;
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            int count = Physics.SphereCastNonAlloc(origin, _radius, forward, HitBuffer, step, _mask, QueryTriggerInteraction.Ignore);
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = HitBuffer[i];
                if (hit.collider == null || (_owner != null && hit.collider.transform.IsChildOf(_owner)))
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = i;
                }
            }

            if (best >= 0)
            {
                RaycastHit hit = HitBuffer[best];
                bool overlapping = hit.distance <= 0f && hit.point == Vector3.zero;
                Vector3 point = overlapping ? origin : hit.point;
                Vector3 normal = overlapping ? -forward : hit.normal;
                OnHit(hit.collider, point, normal);
                return;
            }

            transform.position = origin + forward * step;

            if (Time.time >= _dieAt)
            {
                Destroy(gameObject);
            }
        }

        private void OnHit(Collider other, Vector3 point, Vector3 normal)
        {
            _done = true;
            transform.position = point;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive && !IsFriendly(damageable))
            {
                damageable.TakeDamage(_damage, _owner != null ? _owner : transform);
            }

            SurfaceType surface = ImpactSurface.Resolve(other);

            if (_config.IsHeavy)
            {
                if (VFXSystem.Instance != null)
                {
                    VFXSystem.Instance.PlayExplosion(point + normal * 0.2f, _config.ExplosionSize, true);
                }

                if (AudioSystem.Instance != null)
                {
                    AudioSystem.Instance.PlayExplosion(point, true, _config.ExplosionVolume);
                }
            }
            else
            {
                if (VFXSystem.Instance != null)
                {
                    VFXSystem.Instance.PlayImpact(point, normal, surface, _config.ImpactSize);
                }

                if (AudioSystem.Instance != null)
                {
                    AudioSystem.Instance.PlayImpact(point, surface, _config.ImpactVolume);
                }
            }

            Destroy(gameObject);
        }

        /// <summary>Enemies do not damage each other.</summary>
        private bool IsFriendly(IDamageable damageable)
        {
            Component target = damageable as Component;
            return _owner != null && target != null && _owner.CompareTag("Enemy") && target.CompareTag("Enemy");
        }

        private void Register()
        {
            if (_registered)
            {
                return;
            }

            _registered = true;
            Active.Add(this);
            if (_owner != null)
            {
                _ownerKey = _owner;
                ActiveByOwner.TryGetValue(_ownerKey, out int count);
                ActiveByOwner[_ownerKey] = count + 1;
            }
        }

        private void OnDestroy()
        {
            if (!_registered)
            {
                return;
            }

            Active.Remove(this);
            if (ReferenceEquals(_ownerKey, null))
            {
                return;
            }

            if (ActiveByOwner.TryGetValue(_ownerKey, out int count))
            {
                if (count <= 1)
                {
                    ActiveByOwner.Remove(_ownerKey);
                }
                else
                {
                    ActiveByOwner[_ownerKey] = count - 1;
                }
            }
        }
    }
}
