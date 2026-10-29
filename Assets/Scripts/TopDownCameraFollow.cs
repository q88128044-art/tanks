using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks
{
    /// <summary>
    /// Top-down follow camera with a fixed angle (does not rotate with the
    /// tank, so mouse aiming stays intuitive). Mouse wheel zooms.
    /// Looks slightly ahead toward the cursor for a better view of the fight.
    /// </summary>
    public class TopDownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _height = 24f;
        [SerializeField] private float _distance = 16f;
        [SerializeField] private float _smoothTime = 0.18f;
        [SerializeField] private Vector2 _zoomRange = new Vector2(0.6f, 1.6f);
        [SerializeField] private float _zoomStep = 0.1f;
        [SerializeField] private float _lookAhead = 4f;

        private Vector3 _velocity;
        private Vector3 _smoothed;
        private float _zoom = 1f;
        private bool _initialized;

        public Transform Target { get => _target; set => _target = value; }

        private void Start()
        {
            if (_target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            Vector3 ahead = Vector3.zero;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _zoom = Mathf.Clamp(_zoom - Mathf.Sign(scroll) * _zoomStep, _zoomRange.x, _zoomRange.y);
                }

                Vector2 p = mouse.position.ReadValue();
                Vector2 centered = new Vector2(p.x / Mathf.Max(1, Screen.width) - 0.5f, p.y / Mathf.Max(1, Screen.height) - 0.5f);
                centered = Vector2.ClampMagnitude(centered * 2f, 1f);
                ahead = new Vector3(centered.x, 0f, centered.y) * _lookAhead;
            }

            Vector3 focus = _target.position + ahead;
            Vector3 offset = new Vector3(0f, _height, -_distance) * _zoom;

            if (!_initialized)
            {
                _smoothed = focus;
                _initialized = true;
            }

            _smoothed = Vector3.SmoothDamp(_smoothed, focus, ref _velocity, _smoothTime);
            transform.position = _smoothed + offset;
            transform.rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
        }
    }
}
