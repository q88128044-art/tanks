using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks
{
    /// <summary>
    /// Player tank movement: W/S drive the hull forward/back (reverse is
    /// limited to a fraction of forward speed), A/D rotate the hull.
    /// Speed is clamped, sideways sliding is removed and X/Z rotation is
    /// frozen so the tank never flips. Reports when it starts/stops moving.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TankMover : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TankInputSource _input;
        [SerializeField] private Text _statusLabel;

        [Header("Speeds")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _turnSpeed = 110f;
        [SerializeField, Range(0f, 1f)] private float _reverseSpeedFactor = 0.6f;
        [SerializeField] private float _acceleration = 12f;
        [SerializeField] private float _braking = 18f;

        [Header("Messages")]
        [SerializeField] private string _startMessage = "Танк почав рух";
        [SerializeField] private string _stopMessage = "Танк зупинився";

        private Rigidbody _rigidbody;
        private bool _isMoving;

        public bool IsMoving => _isMoving;
        public float MoveSpeed => _moveSpeed;
        public float ReverseSpeed => _moveSpeed * _reverseSpeedFactor;
        public float TurnSpeed => _turnSpeed;
        public float Throttle { get; private set; }
        public float Steering { get; private set; }

        public float CurrentSpeed
        {
            get
            {
                if (_rigidbody == null)
                {
                    return 0f;
                }

                Vector3 v = _rigidbody.linearVelocity;
                v.y = 0f;
                return v.magnitude;
            }
        }

        public event Action<bool> MovementChanged;

        public void ApplyConfig(TankConfig config)
        {
            _moveSpeed = config.MoveSpeed;
            _turnSpeed = config.TurnSpeed;
            _reverseSpeedFactor = config.ReverseSpeedFactor;
            _acceleration = config.Acceleration;
        }

        private void Reset()
        {
            _input = GetComponent<TankInputSource>();
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

        private void OnEnable()
        {
            MovementChanged += OnMovementChanged;
        }

        private void OnDisable()
        {
            MovementChanged -= OnMovementChanged;
            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
            }

            SetMoving(false);
        }

        private void FixedUpdate()
        {
            if (_input == null)
            {
                return;
            }

            Vector2 move = _input.Move;
            float throttle = Mathf.Clamp(move.y, -1f, 1f);
            float steering = Mathf.Clamp(move.x, -1f, 1f);
            Throttle = throttle;
            Steering = steering;

            // Reverse speed is a fraction (60% by default) of forward speed.
            float maxSpeed = throttle >= 0f ? _moveSpeed : _moveSpeed * _reverseSpeedFactor;
            float targetSpeed = throttle * maxSpeed;

            Vector3 velocity = _rigidbody.linearVelocity;
            float currentSpeed = Vector3.Dot(velocity, transform.forward);
            float rate = Mathf.Abs(throttle) > 0.01f && Mathf.Sign(targetSpeed) == Mathf.Sign(currentSpeed + targetSpeed * 0.01f)
                ? _acceleration
                : _braking;
            float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

            // Keep gravity, remove sideways drift.
            Vector3 planar = transform.forward * newSpeed;
            _rigidbody.linearVelocity = new Vector3(planar.x, velocity.y, planar.z);

            if (Mathf.Abs(steering) > 0.01f)
            {
                Quaternion turn = Quaternion.Euler(0f, steering * _turnSpeed * Time.fixedDeltaTime, 0f);
                _rigidbody.MoveRotation(_rigidbody.rotation * turn);
            }

            _rigidbody.angularVelocity = Vector3.zero;

            SetMoving(Mathf.Abs(newSpeed) > 0.15f || Mathf.Abs(steering) > 0.01f);
        }

        private void OnMovementChanged(bool moving)
        {
            string message = moving ? _startMessage : _stopMessage;
            Debug.Log(message);

            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
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
