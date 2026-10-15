using UnityEngine;
using UnityEngine.UI;

namespace Tanks
{
    /// <summary>
    /// Player health HUD (legacy uGUI). Listens to the player tank's TankHealth
    /// and drives a filled bar plus numeric text. The panel fades out while the
    /// player is at full health and fades back in on any change.
    /// </summary>
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _healthFill;
        [SerializeField] private Text _healthText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Behavior")]
        [SerializeField] private float _fadeDelay = 3f;

        private TankHealth _health;
        private float _lastChangeTime;
        private bool _isVisible = true;

        private void Awake()
        {
            _health = FindPlayerHealth();
            if (_health == null)
            {
                Debug.LogWarning("PlayerHealthUI: player TankHealth not found in scene.");
                return;
            }

            _health.HealthChanged += OnHealthChanged;
            _health.Died += OnDied;
            Refresh(_health.CurrentHealth, _health.MaxHealth);
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.HealthChanged -= OnHealthChanged;
                _health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (_health == null)
            {
                return;
            }

            if (_isVisible && _health.CurrentHealth >= _health.MaxHealth - 0.001f
                && (Time.time - _lastChangeTime) >= _fadeDelay)
            {
                SetVisible(false);
            }
        }

        private TankHealth FindPlayerHealth()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("PlayerTank");
            }

            if (player != null && player.TryGetComponent(out TankHealth tagged))
            {
                return tagged;
            }

            // Fallback: any TankHealth that is not on an Enemy-tagged object.
            TankHealth[] all = Object.FindObjectsByType<TankHealth>(FindObjectsSortMode.None);
            foreach (TankHealth candidate in all)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.CompareTag("Enemy") || candidate.name.Contains("Enemy"))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private void OnHealthChanged(float current, float max)
        {
            Refresh(current, max);
            _lastChangeTime = Time.time;
            SetVisible(true);
        }

        private void OnDied(TankHealth health)
        {
            Refresh(0f, _health.MaxHealth);
        }

        private void Refresh(float current, float max)
        {
            float pct = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (_healthFill != null)
            {
                _healthFill.fillAmount = pct;
                _healthFill.color = Color.Lerp(new Color(0.85f, 0.2f, 0.15f), new Color(0.15f, 0.75f, 0.2f), pct);
            }

            if (_healthText != null)
            {
                _healthText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible)
            {
                return;
            }

            _isVisible = visible;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }
    }
}