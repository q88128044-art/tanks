using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Shooting-range target. Takes damage through IDamageable, blinks white on
    /// every hit and shifts its colour toward hitColor as it loses health,
    /// then breaks with a small explosion.
    /// </summary>
    public class Target : MonoBehaviour, IDamageable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private SurfaceType _surface = SurfaceType.Metal;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private Color _hitColor = new Color(0.95f, 0.85f, 0.15f);
        [SerializeField] private Renderer[] _renderers;

        private float _health;
        private Color[] _baseColors;
        private MaterialPropertyBlock _block;
        private float _flashUntil;

        public float CurrentHealth => _health;
        public float MaxHealth => _maxHealth;
        public bool IsAlive => _health > 0.001f;

        private void Awake()
        {
            _health = _maxHealth;
            _block = new MaterialPropertyBlock();

            ImpactSurface surface = GetComponent<ImpactSurface>();
            if (surface == null)
            {
                surface = gameObject.AddComponent<ImpactSurface>();
            }

            surface.Surface = _surface;

            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>();
            }

            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material m = _renderers[i] != null ? _renderers[i].sharedMaterial : null;
                _baseColors[i] = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white;
            }
        }

        private void Update()
        {
            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                ApplyColor(false);
            }
        }

        public void TakeDamage(float amount, Transform damageSource = null)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            Debug.Log($"[Target] {name}: -{amount:0.#} HP -> {_health:0.#}/{_maxHealth:0.#}");

            _flashUntil = Time.time + 0.08f;
            ApplyColor(true);

            if (_health <= 0.001f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            _health = Mathf.Min(_maxHealth, _health + amount);
            ApplyColor(false);
        }

        private void ApplyColor(bool flash)
        {
            float damage = 1f - _health / _maxHealth;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;
                Color c = flash ? Color.white : Color.Lerp(_baseColors[i], _hitColor, Mathf.Clamp01(0.35f + damage));
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(_block);
            }
        }

        private void Die()
        {
            Debug.Log("Target destroyed: " + name);
            if (VFXSystem.Instance != null)
            {
                VFXSystem.Instance.PlayExplosion(transform.position, 1f, false);
            }

            if (AudioSystem.Instance != null)
            {
                AudioSystem.Instance.PlayExplosion(transform.position, false, 0.6f);
            }

            Destroy(gameObject);
        }
    }
}
