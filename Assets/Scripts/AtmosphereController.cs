using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Tanks
{
    /// <summary>
    /// Two lighting moods for the level: Day and Sunset. Switches the sun
    /// (colour, intensity, angle), ambient light, fog and the post-processing
    /// profile. Press T in Play mode to toggle; works in the editor too.
    /// </summary>
    [ExecuteAlways]
    public class AtmosphereController : MonoBehaviour
    {
        public enum Preset
        {
            Day,
            Sunset
        }

        [SerializeField] private Preset _preset = Preset.Day;
        [SerializeField] private Light _sun;
        [SerializeField] private Volume _volume;
        [SerializeField] private VolumeProfile _dayProfile;
        [SerializeField] private VolumeProfile _sunsetProfile;

        [Header("Day")]
        [SerializeField] private Color _daySunColor = new Color(1f, 0.96f, 0.88f);
        [SerializeField] private float _daySunIntensity = 1.3f;
        [SerializeField] private Vector3 _daySunAngles = new Vector3(52f, -35f, 0f);
        [SerializeField] private Color _daySky = new Color(0.58f, 0.63f, 0.7f);
        [SerializeField] private Color _dayEquator = new Color(0.45f, 0.46f, 0.47f);
        [SerializeField] private Color _dayGround = new Color(0.22f, 0.2f, 0.18f);
        [SerializeField] private Color _dayFog = new Color(0.66f, 0.71f, 0.78f);
        [SerializeField] private float _dayFogDensity = 0.006f;

        [Header("Sunset")]
        [SerializeField] private Color _sunsetSunColor = new Color(1f, 0.72f, 0.5f);
        [SerializeField] private float _sunsetSunIntensity = 1.25f;
        [SerializeField] private Vector3 _sunsetSunAngles = new Vector3(24f, -70f, 0f);
        [SerializeField] private Color _sunsetSky = new Color(0.66f, 0.55f, 0.55f);
        [SerializeField] private Color _sunsetEquator = new Color(0.52f, 0.42f, 0.38f);
        [SerializeField] private Color _sunsetGround = new Color(0.22f, 0.17f, 0.15f);
        [SerializeField] private Color _sunsetFog = new Color(0.82f, 0.62f, 0.5f);
        [SerializeField] private float _sunsetFogDensity = 0.007f;

        public Preset Current => _preset;

        private void OnEnable()
        {
            Apply(_preset);
        }

        private void OnValidate()
        {
            Apply(_preset);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tKey.wasPressedThisFrame)
            {
                Apply(_preset == Preset.Day ? Preset.Sunset : Preset.Day);
                Debug.Log("Atmosphere: " + _preset);
            }
        }

        public void Apply(Preset preset)
        {
            _preset = preset;
            bool day = preset == Preset.Day;

            if (_sun != null)
            {
                _sun.color = day ? _daySunColor : _sunsetSunColor;
                _sun.intensity = day ? _daySunIntensity : _sunsetSunIntensity;
                _sun.transform.rotation = Quaternion.Euler(day ? _daySunAngles : _sunsetSunAngles);
                _sun.shadows = LightShadows.Soft;
                RenderSettings.sun = _sun;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = day ? _daySky : _sunsetSky;
            RenderSettings.ambientEquatorColor = day ? _dayEquator : _sunsetEquator;
            RenderSettings.ambientGroundColor = day ? _dayGround : _sunsetGround;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = day ? _dayFog : _sunsetFog;
            RenderSettings.fogDensity = day ? _dayFogDensity : _sunsetFogDensity;

            VolumeProfile profile = day ? _dayProfile : _sunsetProfile;
            if (_volume != null && profile != null)
            {
                _volume.sharedProfile = profile;
            }
        }
    }
}
