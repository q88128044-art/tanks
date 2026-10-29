using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Short camera kick for shots and explosions. Runs after other camera
    /// scripts and removes its own offset every frame, so it never drifts.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(Camera))]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Main { get; private set; }

        [SerializeField] private float _maxOffset = 0.45f;
        [SerializeField] private float _maxAngle = 1.2f;
        [SerializeField] private float _decay = 3.5f;
        [SerializeField] private float _frequency = 30f;

        private float _trauma;
        private Vector3 _appliedOffset;
        private Quaternion _appliedRotation = Quaternion.identity;
        private float _seed;

        public float Trauma => _trauma;

        private void OnEnable()
        {
            _seed = Random.Range(0f, 100f);
            if (CompareTag("MainCamera") || Main == null)
            {
                Main = this;
            }
        }

        private void OnDisable()
        {
            if (Main == this)
            {
                Main = null;
            }
        }

        /// <summary>Adds shake (0..1). Small values (0.1-0.3) give a short kick.</summary>
        public static void Shake(float amount)
        {
            if (Main == null)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    return;
                }

                Main = cam.GetComponent<CameraShake>();
                if (Main == null)
                {
                    Main = cam.gameObject.AddComponent<CameraShake>();
                }
            }

            Main._trauma = Mathf.Clamp01(Main._trauma + Mathf.Max(0f, amount));
        }

        private void LateUpdate()
        {
            transform.position -= _appliedOffset;
            transform.rotation = transform.rotation * Quaternion.Inverse(_appliedRotation);
            _appliedOffset = Vector3.zero;
            _appliedRotation = Quaternion.identity;

            if (_trauma <= 0f)
            {
                return;
            }

            float strength = _trauma * _trauma;
            float t = Time.time * _frequency;
            float x = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(_seed + 10f, t) - 0.5f) * 2f;
            float r = (Mathf.PerlinNoise(_seed + 20f, t) - 0.5f) * 2f;

            _appliedOffset = transform.rotation * new Vector3(x, y, 0f) * (_maxOffset * strength);
            _appliedRotation = Quaternion.Euler(0f, 0f, r * _maxAngle * strength);
            transform.position += _appliedOffset;
            transform.rotation = transform.rotation * _appliedRotation;

            _trauma = Mathf.Max(0f, _trauma - _decay * Time.deltaTime);
        }
    }
}
