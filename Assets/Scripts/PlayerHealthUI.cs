using UnityEngine;
using UnityEngine.UI;

namespace Tanks
{
    /// <summary>
    /// Player HUD: health Slider that drains smoothly, fill colour
    /// green -> yellow -> red, "current / max" text and the selected weapon
    /// with its reload state. Builds its own layout if references are missing.
    /// </summary>
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _healthFill;
        [SerializeField] private Text _healthText;
        [SerializeField] private Text _weaponText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Behavior")]
        [Tooltip("How fast the bar catches up, in bar-widths per second.")]
        [SerializeField] private float _drainSpeed = 0.6f;

        [Header("Colors")]
        [SerializeField] private Color _highColor = new Color(0.2f, 0.8f, 0.25f);
        [SerializeField] private Color _midColor = new Color(0.95f, 0.8f, 0.15f);
        [SerializeField] private Color _lowColor = new Color(0.9f, 0.18f, 0.12f);

        private TankHealth _health;
        private WeaponController _weapon;
        private float _target = 1f;
        private float _displayed = 1f;

        private void Awake()
        {
            if (_slider == null)
            {
                BuildLayout();
            }

            _health = FindPlayerHealth();
            if (_health == null)
            {
                Debug.LogWarning("PlayerHealthUI: player TankHealth not found in scene.");
                return;
            }

            foreach (WeaponController w in _health.GetComponentsInChildren<WeaponController>())
            {
                if (!w.IsAIWeapon)
                {
                    _weapon = w;
                    break;
                }
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        private void Start()
        {
            if (_health == null)
            {
                return;
            }

            _health.HealthChanged += OnHealthChanged;
            _target = _health.Normalized;
            _displayed = _target;
            Refresh(_health.CurrentHealth, _health.MaxHealth);
            ApplyBar();
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.HealthChanged -= OnHealthChanged;
            }
        }

        private void Update()
        {
            if (_health == null)
            {
                return;
            }

            float speed = _target < _displayed ? _drainSpeed : _drainSpeed * 1.5f;
            _displayed = Mathf.MoveTowards(_displayed, _target, speed * Time.deltaTime);
            ApplyBar();

            if (_weaponText != null && _weapon != null && _weapon.Current != null)
            {
                string state = !_health.IsAlive ? "—"
                    : _weapon.IsReady ? "готово"
                    : "перезарядка " + _weapon.CooldownRemaining.ToString("0.0") + " с";
                _weaponText.text = "Зброя [" + (_weapon.SelectedIndex + 1) + "/" + _weapon.WeaponCount + "]: "
                                   + _weapon.Current.DisplayName + "  •  " + state;
            }
        }

        private TankHealth FindPlayerHealth()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("PlayerTank");
            }

            return player != null ? player.GetComponent<TankHealth>() : null;
        }

        private void OnHealthChanged(float current, float max)
        {
            Refresh(current, max);
        }

        private void Refresh(float current, float max)
        {
            _target = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (_healthText != null)
            {
                _healthText.text = "HP " + Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
            }
        }

        private void ApplyBar()
        {
            if (_slider != null)
            {
                _slider.value = _displayed;
            }

            if (_healthFill != null)
            {
                _healthFill.color = Evaluate(_displayed);
            }
        }

        /// <summary>green (full) -> yellow (half) -> red (empty).</summary>
        public Color Evaluate(float pct)
        {
            return pct >= 0.5f
                ? Color.Lerp(_midColor, _highColor, (pct - 0.5f) * 2f)
                : Color.Lerp(_lowColor, _midColor, pct * 2f);
        }

        /// <summary>Creates the HUD layout (panel, slider, texts) under this canvas.</summary>
        [ContextMenu("Rebuild HUD layout")]
        public void BuildLayout()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform panel = CreateRect("HealthPanel", transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 0f);
            panel.anchoredPosition = new Vector2(24f, 24f);
            panel.sizeDelta = new Vector2(460f, 96f);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);

            RectTransform sliderRect = CreateRect("HealthSlider", panel);
            sliderRect.anchorMin = new Vector2(0f, 1f);
            sliderRect.anchorMax = new Vector2(1f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.offsetMin = new Vector2(12f, -50f);
            sliderRect.offsetMax = new Vector2(-12f, -12f);

            RectTransform background = CreateRect("Background", sliderRect);
            Stretch(background);
            Image bgImage = background.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

            RectTransform fillArea = CreateRect("Fill Area", sliderRect);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(3f, 3f);
            fillArea.offsetMax = new Vector2(-3f, -3f);

            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill);
            _healthFill = fill.gameObject.AddComponent<Image>();
            _healthFill.color = _highColor;

            _slider = sliderRect.gameObject.AddComponent<Slider>();
            _slider.fillRect = fill;
            _slider.handleRect = null;
            _slider.targetGraphic = bgImage;
            _slider.transition = Selectable.Transition.None;
            _slider.interactable = false;
            _slider.direction = Slider.Direction.LeftToRight;
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.value = 1f;
            Navigation nav = _slider.navigation;
            nav.mode = Navigation.Mode.None;
            _slider.navigation = nav;

            RectTransform hpText = CreateRect("HealthText", sliderRect);
            Stretch(hpText);
            _healthText = hpText.gameObject.AddComponent<Text>();
            _healthText.font = font;
            _healthText.fontSize = 22;
            _healthText.fontStyle = FontStyle.Bold;
            _healthText.alignment = TextAnchor.MiddleCenter;
            _healthText.color = Color.white;
            _healthText.text = "HP 150 / 150";
            Outline outline = hpText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);

            RectTransform weaponRect = CreateRect("WeaponText", panel);
            weaponRect.anchorMin = new Vector2(0f, 0f);
            weaponRect.anchorMax = new Vector2(1f, 0f);
            weaponRect.pivot = new Vector2(0.5f, 0f);
            weaponRect.offsetMin = new Vector2(14f, 8f);
            weaponRect.offsetMax = new Vector2(-14f, 38f);
            _weaponText = weaponRect.gameObject.AddComponent<Text>();
            _weaponText.font = font;
            _weaponText.fontSize = 20;
            _weaponText.alignment = TextAnchor.MiddleLeft;
            _weaponText.color = new Color(0.92f, 0.92f, 0.92f);
            _weaponText.text = "Зброя [1/3]: Rifle  •  готово";

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
