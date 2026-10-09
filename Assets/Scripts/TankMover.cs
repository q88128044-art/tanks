using UnityEngine;
using UnityEngine.UI;

namespace Tanks
{
    public class TankMover : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TankInputSource _input;
        [SerializeField] private Text _statusLabel;

        [Header("Speeds")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _turnSpeed = 100f;
        [SerializeField, Range(0f, 1f)] private float _reverseSpeedFactor = 0.6f;
        [SerializeField] private float _acceleration = 14f;

        private Rigidbody _rigidbody;
        private bool _isMoving;

        public bool IsMoving => _isMoving;

        public float MoveSpeed => _moveSpeed;
        public float ReverseSpeed => _moveSpeed * _reverseSpeedFactor;
        public float TurnSpeed => _turnSpeed;
        public float CurrentSpeed => _rigidbody != null ? _rigidbody.linearVelocity.magnitude : 0f;

        public event System.Action<bool> MovementChanged;

        private void Reset()
        {
            _input = GetComponent<TankInputSource>();
            _statusLabel = FindAnyObjectByType<Text>();
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            if (_input == null)
            {
                _input = GetComponent<TankInputSource>();
            }

            _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.centerOfMass = new Vector3(0f, -0.5f, 0f);
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = _input.Move;

            float throttle = moveInput.y;
            float steering = moveInput.x;

            float speed = throttle < 0f ? _moveSpeed * _reverseSpeedFactor : _moveSpeed;
            Vector3 acceleration = transform.forward * (throttle * speed * _acceleration);

            _rigidbody.AddForce(acceleration, ForceMode.Acceleration);

            if (Mathf.Abs(steering) > 0.001f)
            {
                Quaternion turn = Quaternion.Euler(0f, steering * _turnSpeed * Time.fixedDeltaTime, 0f);
                _rigidbody.MoveRotation(_rigidbody.rotation * turn);
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 sideways = transform.right * Vector3.Dot(velocity, transform.right);
            _rigidbody.linearVelocity = velocity - sideways;

            SetMoving(Mathf.Abs(throttle) > 0.001f || Mathf.Abs(steering) > 0.001f);
        }

        private void OnMovementChanged(bool moving)
        {
            string message = moving ? "Танк почав рух" : "Танк зупинився";
            Debug.Log(message);

            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        private void OnEnable()
        {
            MovementChanged += OnMovementChanged;
        }

        private void OnDisable()
        {
            MovementChanged -= OnMovementChanged;
        }

        private void SetMoving(bool moving)
        {
            if (_isMoving == moving)
            {
                return;
            }

            _isMoving = moving;
            MovementChanged?.Invoke(moving);
        }
    }
}