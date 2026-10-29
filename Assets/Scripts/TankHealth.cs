using System;
using UnityEngine;
using UnityEngine.AI;

namespace Tanks
{
    /// <summary>
    /// Health of a tank (player or enemy). Receives damage through IDamageable,
    /// blinks the tank's materials on hit, can be repaired with RestoreHealth,
    /// and dies exactly once: big explosion + sound, all driving / shooting /
    /// AI components are switched off and the hull is left as a smoking wreck.
    /// </summary>
    public class TankHealth : MonoBehaviour, IDamageable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth;

        [Header("Regeneration")]
        [SerializeField] private bool _regenerate = false;
        [SerializeField] private float _regenRate = 2f;
        [SerializeField] private float _regenDelay = 5f;

        [Header("Hit feedback")]
        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.25f, 0.18f);
        [SerializeField] private float _hitFlashDuration = 0.24f;
        [SerializeField] private float _blinkInterval = 0.06f;

        [Header("Death")]
        [SerializeField] private float _wreckDarkness = 0.25f;
        [Tooltip("Enemy wrecks are removed after this time. 0 = keep forever. The player's wreck always stays.")]
        [SerializeField] private float _wreckLifetime = 12f;

        private Renderer[] _renderers;
        private Color[] _baseColors;
        private MaterialPropertyBlock _block;
        private float _flashStart = -10f;
        private bool _flashing;
        private float _lastDamageTime = float.NegativeInfinity;
        private bool _isDead;

        public event Action<float, float> HealthChanged;
        public event Action<TankHealth> Died;
        public event Action<float, float> TookDamage;
        public event Action<float> Restored;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float Normalized => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
        public bool IsAlive => !_isDead && _currentHealth > 0.001f;
        public bool IsDead => _isDead;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _block = new MaterialPropertyBlock();
            CacheRenderers();
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
            Renderer[] all = GetComponentsInChildren<Renderer>(true);
            System.Collections.Generic.List<Renderer> list = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in all)
            {
                if (r is MeshRenderer && !IsUnderHealthBar(r.transform))
                {
                    list.Add(r);
                }
            }

            _renderers = list.ToArray();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material shared = _renderers[i].sharedMaterial;
                _baseColors[i] = shared != null && shared.HasProperty(BaseColorId) ? shared.GetColor(BaseColorId) : Color.white;
            }
        }

        private bool IsUnderHealthBar(Transform t)
        {
            while (t != null && t != transform)
            {
                if (t.name == "HealthBar")
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        private void UpdateHitFlash()
        {
            if (!_flashing || _renderers == null)
            {
                return;
            }

            float elapsed = Time.time - _flashStart;
            if (elapsed >= _hitFlashDuration)
            {
                _flashing = false;
                ApplyTint(_isDead ? _wreckDarkness : 1f, null);
                return;
            }

            bool on = ((int)(elapsed / _blinkInterval)) % 2 == 0;
            ApplyTint(1f, on ? _hitFlashColor : (Color?)null);
        }

        private void ApplyTint(float brightness, Color? overrideColor)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null)
                {
                    continue;
                }

                if (overrideColor == null && Mathf.Approximately(brightness, 1f))
                {
                    r.SetPropertyBlock(null);
                    continue;
                }

                Color c = overrideColor ?? _baseColors[i] * brightness;
                c.a = 1f;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(_block);
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
            _flashStart = Time.time;
            _flashing = true;

            string source = damageSource == null ? "?" : damageSource.root.name;
            Debug.Log($"[TankHealth] {name} отримав {amount:0.#} шкоди від {source} -> {_currentHealth:0.#}/{_maxHealth:0.#}");

            TookDamage?.Invoke(amount, _currentHealth);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0.001f)
            {
                Die();
            }
        }

        /// <summary>Repairs the tank. Returns false if it is dead or already at full health.</summary>
        public bool RestoreHealth(float amount)
        {
            if (!IsAlive || amount <= 0f || _currentHealth >= _maxHealth - 0.001f)
            {
                return false;
            }

            float before = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            Debug.Log($"[TankHealth] {name} відремонтовано +{_currentHealth - before:0.#} -> {_currentHealth:0.#}/{_maxHealth:0.#}");
            Restored?.Invoke(_currentHealth - before);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            return true;
        }

        public void Heal(float amount)
        {
            RestoreHealth(amount);
        }

        public void SetMaxHealth(float maxHealth, bool refill = true)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);
            if (refill || _currentHealth > _maxHealth)
            {
                _currentHealth = _maxHealth;
            }

            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            _currentHealth = 0f;
            Debug.Log($"[TankHealth] {name} знищено");

            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            Died?.Invoke(this);

            if (VFXSystem.Instance != null)
            {
                VFXSystem.Instance.PlayDestruction(transform.position);
                VFXSystem.Instance.AttachWreckSmoke(transform);
            }

            if (AudioSystem.Instance != null)
            {
                AudioSystem.Instance.PlayExplosion(transform.position, true, 1f);
            }

            DisableTank();

            _flashing = false;
            ApplyTint(_wreckDarkness, null);

            if (!CompareTag("Player") && _wreckLifetime > 0f)
            {
                Destroy(gameObject, _wreckLifetime);
            }
        }

        private void DisableTank()
        {
            foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour is AIController || behaviour is TankMover || behaviour is WeaponController
                    || behaviour is TankTurret || behaviour is EngineLoop || behaviour is TankInputSource)
                {
                    behaviour.enabled = false;
                }
            }

            if (TryGetComponent(out NavMeshAgent agent))
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                agent.enabled = false;
            }

            if (TryGetComponent(out Rigidbody body))
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
            }

            Transform turret = transform.Find("Turret");
            if (turret != null)
            {
                turret.localPosition += Vector3.up * 0.2f;
                turret.localRotation *= Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-35f, 35f), UnityEngine.Random.Range(-10f, 10f));
            }
        }
    }
}
