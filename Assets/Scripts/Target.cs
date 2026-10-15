using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Static shooting-range target. Implements IDamageable so both the player
    /// and the new TankHealth-driven tanks can be hit by the same Projectile code.
    /// </summary>
    public class Target : MonoBehaviour, IDamageable
    {
        [SerializeField] private SurfaceType _surface = SurfaceType.Metal;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private Color _hitColor = new Color(0.85f, 0.15f, 0.12f);
        [SerializeField] private Renderer[] _renderers;

        private float _health;

        public float CurrentHealth => _health;
        public float MaxHealth => _maxHealth;
        public bool IsAlive => _health > 0.001f;

        private void Awake()
        {
            _health = _maxHealth;

            ImpactSurface surface = GetComponent<ImpactSurface>();
            if (surface == null)
            {
                surface = GetComponentInParent<ImpactSurface>();
            }
            if (surface != null)
            {
                surface.Surface = _surface;
            }
        }

        public void TakeDamage(float amount, Transform damageSource = null)
        {
            if (!IsAlive) return;
            if (amount <= 0f) return;

            _health -= amount;
            if (_health < 0f) _health = 0f;

            foreach (Renderer item in _renderers)
            {
                if (item != null)
                {
                    item.material.color = _hitColor;
                }
            }

            if (_health <= 0.001f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            _health = Mathf.Min(_maxHealth, _health + amount);
        }

        private void Die()
        {
            Debug.Log("Target destroyed: " + name);
            if (VFXSystem.Instance != null)
            {
                VFXSystem.Instance.PlayExplosion(transform.position, 1.2f, 0.3f, _hitColor);
            }
            if (AudioSystem.Instance != null)
            {
                AudioSystem.Instance.PlayExplosion(transform.position, 0.5f, 1f);
            }
            Destroy(gameObject);
        }
    }
}