using UnityEngine;
using UnityEngine.AI;

namespace Tanks
{
    /// <summary>
    /// Pushes a TankConfig into the tank's components (health, mover, AI,
    /// NavMeshAgent, weapons) before they start.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class TankConfigApplier : MonoBehaviour
    {
        [SerializeField] private TankConfig _config;

        public TankConfig Config => _config;

        private void Awake()
        {
            Apply();
        }

        public void SetConfig(TankConfig config)
        {
            _config = config;
        }

        public void Apply()
        {
            if (_config == null)
            {
                return;
            }

            if (TryGetComponent(out TankHealth health))
            {
                health.SetMaxHealth(_config.MaxHealth, true);
            }

            if (TryGetComponent(out TankMover mover))
            {
                mover.ApplyConfig(_config);
            }

            if (TryGetComponent(out AIController ai))
            {
                ai.ApplyConfig(_config);
            }

            if (TryGetComponent(out NavMeshAgent agent))
            {
                agent.speed = _config.MoveSpeed;
                agent.angularSpeed = _config.TurnSpeed;
                agent.acceleration = _config.Acceleration;
            }

            foreach (WeaponController weapon in GetComponentsInChildren<WeaponController>(true))
            {
                weapon.Configure(_config.Weapons, _config.DamageMultiplier);
            }
        }
    }
}
