using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Billboard health bar above an enemy tank. Shrinks smoothly, changes
    /// colour green -> yellow -> red, is hidden while health is full and
    /// disappears when the tank is destroyed.
    /// </summary>
    [RequireComponent(typeof(TankHealth))]
    public class WorldSpaceHealthBar : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Material s_material;

        [Header("Layout")]
        [SerializeField] private float _width = 1.6f;
        [SerializeField] private float _height = 0.18f;
        [SerializeField] private float _verticalOffset = 2.1f;

        [Header("Behavior")]
        [SerializeField] private bool _hideWhenFull = true;
        [SerializeField] private bool _showOnPlayer = false;
        [SerializeField] private float _drainSpeed = 0.8f;

        [Header("Colors")]
        [SerializeField] private Color _highColor = new Color(0.2f, 0.8f, 0.25f);
        [SerializeField] private Color _midColor = new Color(0.95f, 0.8f, 0.15f);
        [SerializeField] private Color _lowColor = new Color(0.9f, 0.18f, 0.12f);
        [SerializeField] private Color _backgroundColor = new Color(0.05f, 0.05f, 0.05f);

        private TankHealth _health;
        private Camera _camera;
        private Transform _root;
        private Transform _fillPivot;
        private Renderer _fillRenderer;
        private MaterialPropertyBlock _block;
        private float _target = 1f;
        private float _displayed = 1f;
        private bool _visible = true;

        private void Awake()
        {
            _health = GetComponent<TankHealth>();
            if (CompareTag("Player") && !_showOnPlayer)
            {
                enabled = false;
                return;
            }

            _block = new MaterialPropertyBlock();
            BuildBar();
        }

        private void OnEnable()
        {
            if (_health == null || _root == null)
            {
                return;
            }

            _health.HealthChanged += OnHealthChanged;
            _health.Died += OnDied;
            _target = _health.Normalized;
            _displayed = _target;
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.HealthChanged -= OnHealthChanged;
                _health.Died -= OnDied;
            }
        }

        private void BuildBar()
        {
            Transform old = transform.Find("HealthBar");
            if (old != null)
            {
                Destroy(old.gameObject);
            }

            if (s_material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                s_material = new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
                s_material.name = "HealthBar (runtime)";
            }

            _root = new GameObject("HealthBar").transform;
            _root.SetParent(transform, false);
            _root.localPosition = Vector3.up * _verticalOffset;

            CreateQuad("Background", _root, new Vector3(0f, 0f, 0.01f), new Vector3(_width + 0.08f, _height + 0.08f, 1f), _backgroundColor);

            _fillPivot = new GameObject("FillPivot").transform;
            _fillPivot.SetParent(_root, false);
            _fillPivot.localPosition = new Vector3(-_width * 0.5f, 0f, 0f);
            _fillRenderer = CreateQuad("Fill", _fillPivot, new Vector3(_width * 0.5f, 0f, 0f), new Vector3(_width, _height, 1f), _highColor);
        }

        private Renderer CreateQuad(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = position;
            quad.transform.localScale = scale;
            quad.layer = 2; // Ignore Raycast

            Renderer r = quad.GetComponent<Renderer>();
            r.sharedMaterial = s_material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            SetColor(r, color);
            return r;
        }

        private void SetColor(Renderer r, Color color)
        {
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            r.SetPropertyBlock(_block);
        }

        private void OnHealthChanged(float current, float max)
        {
            _target = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        private void OnDied(TankHealth health)
        {
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (_root == null || _health == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _displayed = Mathf.MoveTowards(_displayed, _target, _drainSpeed * Time.deltaTime);
            _fillPivot.localScale = new Vector3(Mathf.Max(0.0001f, _displayed), 1f, 1f);
            SetColor(_fillRenderer, Evaluate(_displayed));

            bool full = _target >= 0.999f && _displayed >= 0.999f;
            SetVisible(_health.IsAlive && !(_hideWhenFull && full));

            if (_visible && _camera != null)
            {
                _root.rotation = _camera.transform.rotation;
            }
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible || _root == null)
            {
                return;
            }

            _visible = visible;
            _root.gameObject.SetActive(visible);
        }

        private Color Evaluate(float pct)
        {
            return pct >= 0.5f
                ? Color.Lerp(_midColor, _highColor, (pct - 0.5f) * 2f)
                : Color.Lerp(_lowColor, _midColor, pct * 2f);
        }
    }
}
