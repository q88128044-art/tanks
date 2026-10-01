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

        private Collider _collider;
        private Transform _owner;
        private float _dieAt;

        public float Damage => _damage;
        public Transform Owner => _owner;

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

            transform.localScale = Vector3.one * config.ProjectileScale;
            transform.SetPositionAndRotation(transform.position, Quaternion.LookRotation(direction.normalized, Vector3.up));

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer item in renderers)
            {
                item.material.color = config.Tint;
            }

            IgnoreOwnerColliders(owner);
            Register();
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
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
            if (_owner != null && other.transform.IsChildOf(_owner))
            {
                return;
            }

            Target target = other.GetComponentInParent<Target>();
            if (target != null)
            {
                target.ApplyDamage(_damage);
            }

            Expire();
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
            Destroy(gameObject);
        }
    }
}