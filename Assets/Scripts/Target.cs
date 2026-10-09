using UnityEngine;

namespace Tanks
{
    public class Target : MonoBehaviour
    {
        [SerializeField] private SurfaceType _surface = SurfaceType.Metal;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private Color _hitColor = new Color(0.85f, 0.15f, 0.12f);
        [SerializeField] private Renderer[] _renderers;

        private float _health;

         public float Health => _health;
        public float MaxHealth => _maxHealth;
        public SurfaceType Surface => _surface;

        private void Awake()
        {
            _health = _maxHealth;
            ImpactSurface surface = GetComponent<ImpactSurface>();
            if (surface == null)
            {
                surface = gameObject.AddComponent<ImpactSurface>();
            }
            surface.Surface = _surface;
        }

        public void ApplyDamage(float amount)
        {
            if (_health <= 0f)
            {
                return;
            }

            _health -= amount;

            foreach (Renderer item in _renderers)
            {
                if (item != null)
                {
                    item.material.color = _hitColor;
                }
            }

            if (_health <= 0f)
            {
                _health = 0f;
                Debug.Log(gameObject.name + " destroyed.");
            }
        }
    }
}