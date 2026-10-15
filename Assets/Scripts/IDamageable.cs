using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Any object that can receive damage and report its health state.
    /// Implemented by Player, Enemy tanks and Targets.
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsAlive { get; }

        void TakeDamage(float amount, Transform damageSource = null);
        void Heal(float amount);
    }
}