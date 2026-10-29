using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Short light flash used by muzzle flashes and explosions: fades the
    /// intensity to zero over the given duration.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFade : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.1f;

        private Light _light;
        private float _startIntensity;
        private float _startTime;

        public void Init(float intensity, float range, Color color, float duration)
        {
            _light = GetComponent<Light>();
            _light.type = LightType.Point;
            _light.color = color;
            _light.range = range;
            _light.intensity = intensity;
            _light.shadows = LightShadows.None;
            _startIntensity = intensity;
            _duration = Mathf.Max(0.01f, duration);
            _startTime = Time.time;
        }

        private void Update()
        {
            if (_light == null)
            {
                return;
            }

            float t = (Time.time - _startTime) / _duration;
            _light.intensity = Mathf.Lerp(_startIntensity, 0f, t);
            if (t >= 1f)
            {
                _light.enabled = false;
                enabled = false;
            }
        }
    }
}
