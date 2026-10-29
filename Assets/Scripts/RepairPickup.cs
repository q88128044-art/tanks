using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Repair kit: when a tank drives into it, calls TankHealth.RestoreHealth
    /// and respawns after a delay. Ignored while the tank is at full health.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class RepairPickup : MonoBehaviour
    {
        [SerializeField] private float _restoreAmount = 50f;
        [SerializeField] private float _respawnTime = 15f;
        [SerializeField] private bool _playerOnly = true;
        [SerializeField] private float _spinSpeed = 90f;
        [SerializeField] private float _bobHeight = 0.2f;
        [SerializeField] private Transform _visual;
        [SerializeField] private Color _color = new Color(0.25f, 0.95f, 0.4f);

        private bool _available = true;
        private float _respawnAt;
        private Vector3 _visualBase;

        private void Reset()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.3f;
        }

        private void Awake()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            if (_visual == null)
            {
                _visual = transform.Find("Visual");
            }

            if (_visual != null)
            {
                _visualBase = _visual.localPosition;
            }
        }

        private void Update()
        {
            if (!_available)
            {
                if (Time.time >= _respawnAt)
                {
                    SetAvailable(true);
                }

                return;
            }

            if (_visual != null)
            {
                _visual.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
                _visual.localPosition = _visualBase + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * _bobHeight);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPickup(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryPickup(other);
        }

        private void TryPickup(Collider other)
        {
            if (!_available)
            {
                return;
            }

            TankHealth health = other.GetComponentInParent<TankHealth>();
            if (health == null || (_playerOnly && !health.CompareTag("Player")))
            {
                return;
            }

            if (!health.RestoreHealth(_restoreAmount))
            {
                return;
            }

            if (VFXSystem.Instance != null)
            {
                VFXSystem.Instance.PlayPickup(transform.position + Vector3.up * 0.6f, _color);
            }

            if (AudioSystem.Instance != null)
            {
                AudioSystem.Instance.PlayPickup(transform.position);
            }

            SetAvailable(false);
            _respawnAt = Time.time + _respawnTime;
        }

        private void SetAvailable(bool available)
        {
            _available = available;
            if (_visual != null)
            {
                _visual.gameObject.SetActive(available);
            }
        }
    }
}
