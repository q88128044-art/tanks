using System;
using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Generic health component shared by the player tank, enemy tanks and targets.
    /// Fires events so HUD, world-space health bars and destruction VFX can react.
    /// </summary>
    public class TankHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth;

        [Header("Regeneration")]
        [SerializeField] private bool _regenerate = false;
        [SerializeField] private float _regenRate = 2f;
        [SerializeField] private float _regenDelay = 5f;

        [Header("Feedback")]
        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.25f, 0.18f);
        [SerializeField] private float _hitFlashDuration = 0.15f;

        private Renderer[] _renderers;
        private Color[] _originalColors;
        private float _flashUntil;
        private float _lastDamageTime = float.NegativeInfinity;

        public event Action<float, float> HealthChanged;
        public event Action<TankHealth> Died;
        public event Action<float, float> TookDamage;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsAlive => _currentHealth > 0.001f;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            CacheRenderers();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                return;
            }
        }

        private void OnEnable()
        {
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Update()
        {
            if (_regenerate && IsAlive && Time.time - _lastDamageTime >= _regenDelay && _currentHealth < _maxHealth)
            {
                _currentHealth = Mathf.Min(_maxHealth, _currentHealth + _regenRate * Time.deltaTime);
                HealthChanged?.Invoke(_currentHealth, _maxHealth);
            }

            UpdateHitFlash();
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && _renderers[i].material != null)
                {
                    _originalColors[i] = _renderers[i].material.color;
                }
            }
        }

        private void UpdateHitFlash()
        {
            if (_renderers == null)
            {
                return;
            }

            bool flashing = Time.time < _flashUntil;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null || r.material == null)
                {
                    continue;
                }

                r.material.color = flashing ? _hitFlashColor : _originalColors[i];
            }
        }

        public void TakeDamage(float amount, Transform damageSource = null)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            _lastDamageTime = Time.time;
            _flashUntil = Time.time + _hitFlashDuration;

            Debug.Log($"[TankHealth] {name} took {amount} dmg from {(damageSource == null ? "null" : damageSource.root.name)} -> {_currentHealth}/{_maxHealth}");

            TookDamage?.Invoke(amount, _currentHealth);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0.001f)
            {
                Die(damageSource);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Die(Transform damageSource)
        {
            _currentHealth = 0f;
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            Died?.Invoke(this);

            if (VFXSystem.Instance != null)
            {
                VFXSystem.Instance.PlayExplosion(transform.position, 2.5f, 0.8f, new Color(0.9f, 0.45f, 0.15f));
            }

            if (AudioSystem.Instance != null)
            {
                AudioSystem.Instance.PlayExplosion(transform.position, 1f, 1f);
            }

            Destroy(gameObject);
        }

        public void SetMaxHealth(float maxHealth, bool refill = true)
        {
            _maxHealth = maxHealth;
            if (refill || _currentHealth > maxHealth)
            {
                _currentHealth = maxHealth;
            }
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}