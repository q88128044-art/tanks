using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks
{
    /// <summary>
    /// Fires the selected WeaponConfig from the muzzle. Player: LMB / Space to
    /// fire (hold for auto-fire limited by cooldown), 1/2/3 to switch weapons.
    /// AI weapons are fired by AIController through Fire().
    /// Each shot plays a muzzle flash, a shot sound with random pitch and, for
    /// the player, a short camera kick.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponConfig[] _weapons;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private int _startIndex;
        [SerializeField] private bool _isAIWeapon;
        [Tooltip("Set from TankConfig: shell damage = WeaponConfig.Damage x this value.")]
        [SerializeField] private float _damageMultiplier = 1f;

        private int _selected;
        private float _nextFireTime;

        public event Action<WeaponConfig> WeaponChanged;

        public WeaponConfig Current => _weapons != null && _weapons.Length > 0 ? _weapons[_selected] : null;
        public int SelectedIndex => _selected;
        public int WeaponCount => _weapons != null ? _weapons.Length : 0;
        public float CooldownRemaining => Mathf.Max(0f, _nextFireTime - Time.time);
        public float CooldownNormalized => Current != null && Current.Cooldown > 0f ? Mathf.Clamp01(CooldownRemaining / Current.Cooldown) : 0f;
        public bool IsReady => Current != null && Time.time >= _nextFireTime;
        public bool IsAIWeapon => _isAIWeapon;
        public float DamageMultiplier => _damageMultiplier;
        public Transform Muzzle => _muzzle;

        public void Configure(WeaponConfig[] weapons, float damageMultiplier)
        {
            if (weapons != null && weapons.Length > 0)
            {
                _weapons = weapons;
            }

            _damageMultiplier = damageMultiplier;
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, WeaponCount - 1));
        }

        private void Awake()
        {
            _selected = Mathf.Clamp(_startIndex, 0, Mathf.Max(0, WeaponCount - 1));
            if (_muzzle == null)
            {
                _muzzle = transform;
            }
        }

        private void Update()
        {
            if (_isAIWeapon)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) Select(0);
                else if (keyboard.digit2Key.wasPressedThisFrame) Select(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) Select(2);
            }

            Mouse mouse = Mouse.current;
            bool firePressed = mouse != null && mouse.leftButton.isPressed;
            if (!firePressed && keyboard != null)
            {
                firePressed = keyboard.spaceKey.isPressed;
            }

            if (firePressed)
            {
                Fire();
            }
        }

        public void Select(int index)
        {
            if (index < 0 || index >= WeaponCount || index == _selected)
            {
                return;
            }

            _selected = index;
            _nextFireTime = Mathf.Max(_nextFireTime, Time.time + 0.15f);
            Debug.Log("Weapon selected: " + Current.DisplayName);
            WeaponChanged?.Invoke(Current);
        }

        /// <summary>Fires if the cooldown and active-projectile limit allow it. Returns true when a shell was fired.</summary>
        public bool Fire()
        {
            WeaponConfig config = Current;
            if (config == null || config.ProjectilePrefab == null || !isActiveAndEnabled)
            {
                return false;
            }

            if (Time.time < _nextFireTime)
            {
                return false;
            }

            if (config.LimitsActiveProjectiles && Projectile.CountActiveFor(transform.root) >= config.MaxActiveProjectiles)
            {
                return false;
            }

            _nextFireTime = Time.time + config.Cooldown;

            Vector3 direction = _muzzle.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.root.forward;
            }

            direction.Normalize();
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject instance = Instantiate(config.ProjectilePrefab, _muzzle.position, rotation);
            instance.name = "Shell_" + config.DisplayName;
            Projectile projectile = instance.GetComponent<Projectile>();
            if (projectile == null)
            {
                projectile = instance.AddComponent<Projectile>();
            }

            projectile.Launch(config, transform.root, direction, _damageMultiplier);

            if (VFXSystem.Instance != null)
            {
                VFXSystem.Instance.PlayMuzzleFlash(_muzzle.position, rotation, config.MuzzleFlashSize, config.MuzzleFlashColor);
            }

            if (AudioSystem.Instance != null)
            {
                AudioSystem.Instance.PlayShot(_muzzle.position, config);
            }

            if (!_isAIWeapon)
            {
                CameraShake.Shake(config.CameraRecoil * 0.1f);
            }

            return true;
        }
    }
}
