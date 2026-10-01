using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponConfig[] _weapons;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private int _startIndex;

        private int _selected;
        private float _nextFireTime;

        public WeaponConfig Current => _weapons[_selected];

        public int SelectedIndex => _selected;

        public float CooldownRemaining => Mathf.Max(0f, _nextFireTime - Time.time);

        private void Awake()
        {
            _selected = Mathf.Clamp(_startIndex, 0, Mathf.Max(0, _weapons.Length - 1));

            if (_muzzle == null)
            {
                _muzzle = transform;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i < _weapons.Length && i < 3; i++)
                {
                    if (keyboard.digit1Key.wasPressedThisFrame && i == 0)
                    {
                        Select(i);
                    }
                    else if (keyboard.digit2Key.wasPressedThisFrame && i == 1)
                    {
                        Select(i);
                    }
                    else if (keyboard.digit3Key.wasPressedThisFrame && i == 2)
                    {
                        Select(i);
                    }
                }
            }

            Mouse mouse = Mouse.current;
            bool firePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;

            if (!firePressed && keyboard != null)
            {
                firePressed = keyboard.spaceKey.wasPressedThisFrame;
            }

            if (firePressed)
            {
                Fire();
            }
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _weapons.Length)
            {
                return;
            }

            _selected = index;
            _nextFireTime = Time.time;
            Debug.Log("Weapon selected: " + Current.DisplayName);
        }

        public void Fire()
        {
            WeaponConfig config = Current;
            if (config == null || config.ProjectilePrefab == null)
            {
                return;
            }

            if (Time.time < _nextFireTime)
            {
                return;
            }

            if (config.LimitsActiveProjectiles && Projectile.CountActiveFor(transform.root) >= config.MaxActiveProjectiles)
            {
                Debug.Log(config.DisplayName + ": active projectile limit reached (" + config.MaxActiveProjectiles + ")");
                return;
            }

            _nextFireTime = Time.time + config.Cooldown;

            GameObject instance = Instantiate(config.ProjectilePrefab, _muzzle.position, _muzzle.rotation);
            Projectile projectile = instance.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Launch(config, transform.root, _muzzle.forward);
            }
        }
    }
}