using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// World-space health bar that follows a TankHealth owner and always faces the camera.
    /// Used by enemies so the player can see their remaining health in-scene.
    /// </summary>
    [RequireComponent(typeof(TankHealth))]
    public class WorldSpaceHealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _barRoot;
        [SerializeField] private Renderer _fill;

        [Header("Layout")]
        [SerializeField] private float _width = 1.3f;
        [SerializeField] private float _height = 0.2f;
        [SerializeField] private float _verticalOffset = 1.7f;
        [SerializeField] private float _hideWhenFullDelay = 1f;

        [Header("Colors")]
        [SerializeField] private Color _fullColor = new Color(0.15f, 0.75f, 0.2f);
        [SerializeField] private Color _lowColor = new Color(0.85f, 0.2f, 0.15f);
        [SerializeField] private Color _flashColor = new Color(1f, 1f, 1f);

        private TankHealth _health;
        private Camera _camera;
        private float _lastDamageTime;
        private bool _isVisible = true;
        private float _fillAmount = 1f;

        private void Awake()
        {
            _health = GetComponent<TankHealth>();
            _camera = Camera.main;
            EnsureBar();
        }

        private void OnEnable()
        {
            _health.HealthChanged += OnHealthChanged;
            _health.Died += OnDied;
            Refresh(_health.CurrentHealth, _health.MaxHealth);
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.HealthChanged -= OnHealthChanged;
                _health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera != null && _barRoot != null)
            {
                Vector3 dir = _barRoot.position - _camera.transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    _barRoot.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }

            bool atFull = _health != null && _health.CurrentHealth >= _health.MaxHealth - 0.001f;
            if (_isVisible && atFull && _fillAmount >= 0.999f && (Time.time - _lastDamageTime) >= _hideWhenFullDelay)
            {
                SetVisible(false);
            }
            else if (!_isVisible)
            {
                SetVisible(true);
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            Refresh(current, max);
            _lastDamageTime = Time.time;

            if (_fill != null)
            {
                _fill.material.color = _flashColor;
                CancelInvoke(nameof(ResetFlashColor));
                Invoke(nameof(ResetFlashColor), 0.12f);
            }

            SetVisible(true);
        }

        private void OnDied(TankHealth health)
        {
            SetVisible(false);
        }

        private void Refresh(float current, float max)
        {
            _fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (_fill != null)
            {
                _fill.transform.localScale = new Vector3(_fillAmount, 1f, 1f);
                _fill.transform.localPosition = new Vector3(-_width * 0.5f + _width * _fillAmount * 0.5f, 0f, 0.02f);
                _fill.material.color = Color.Lerp(_lowColor, _fullColor, _fillAmount);
            }
        }

        private void ResetFlashColor()
        {
            if (_fill != null)
            {
                _fill.material.color = Color.Lerp(_lowColor, _fullColor, _fillAmount);
            }
        }

        private void EnsureBar()
        {
            if (_barRoot != null && _fill != null)
            {
                return;
            }

            GameObject root = new GameObject("HealthBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, _verticalOffset, 0f);
            _barRoot = root.transform;

            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Background";
            bg.transform.SetParent(root.transform, false);
            bg.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bg.transform.localPosition = new Vector3(0f, 0f, 0f);
            bg.transform.localScale = new Vector3(_width, _height, 1f);
            bg.GetComponent<Renderer>().material.color = new Color(0f, 0f, 0f, 0.7f);
            Object.Destroy(bg.GetComponent<Collider>());

            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Fill";
            fill.transform.SetParent(root.transform, false);
            fill.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            fill.transform.localScale = new Vector3(_width, _height, 1f);
            fill.GetComponent<Renderer>().material.color = _fullColor;
            Object.Destroy(fill.GetComponent<Collider>());

            _fill = fill.GetComponent<Renderer>();

            Refresh(_health.CurrentHealth, _health.MaxHealth);
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible)
            {
                return;
            }

            _isVisible = visible;
            if (_barRoot != null)
            {
                _barRoot.gameObject.SetActive(visible);
            }
        }
    }
}