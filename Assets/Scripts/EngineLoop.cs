using UnityEngine;

namespace Tanks
{
    public class EngineLoop : MonoBehaviour
    {
        [Header("Audio")]
        [Range(0f, 1f)] [SerializeField] private float _idleVolume = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float _fullVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float _revVolume = 0.55f;
        [Range(0.6f, 1.4f)] [SerializeField] private float _idlePitch = 0.8f;
        [Range(0.9f, 1.3f)] [SerializeField] private float _revPitch = 1.05f;
        [Range(0.05f, 0.6f)] [SerializeField] private float _idleFrequency = 0.35f;
        [Range(0.05f, 0.6f)] [SerializeField] private float _revFrequency = 0.5f;

        [Header("Visual")]
        [Range(0f, 1f)] [SerializeField] private float _idleExhaust = 0.15f;
        [Range(0f, 1f)] [SerializeField] private float _fullExhaust = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float _revExhaust = 0.6f;

        private TankMover _mover;
        private Renderer _exhaust;
        private Color _exhaustBase = new Color(1f, 0.35f, 0.12f);
        private float _exhaustStrength;
        private float _nextTick;
        private bool _isReversing;

        private void Awake()
        {
            _mover = GetComponentInParent<TankMover>();
            Renderer[] renderers = GetComponentsInParent<Renderer>();
            foreach (Renderer item in renderers)
            {
                if (item != null && item.name.Contains("Exhaust"))
                {
                    _exhaust = item;
                }
            }
        }

        private void Update()
        {
            if (_mover == null)
            {
                return;
            }

            float speed = _mover.CurrentSpeed;
            float target = Mathf.InverseLerp(0f, _mover.MoveSpeed, speed);
            _exhaustStrength = Mathf.MoveTowards(_exhaustStrength, target, Time.deltaTime * 4f);

            if (Time.time >= _nextTick)
            {
                _nextTick = Time.time + _idleFrequency;
                PlayEngineTick(_exhaustStrength);
            }
        }

        private void PlayEngineTick(float intensity)
        {
            float volume = Mathf.Lerp(_idleVolume, _fullVolume, intensity);
            float pitch = _idlePitch;

            if (_isReversing)
            {
                volume = _revVolume;
                pitch = _revPitch;
            }

            if (_exhaust != null)
            {
                _exhaust.material.color = Color.black * (1f - _exhaustStrength * 0.75f) + _exhaustBase * (_exhaustStrength * 0.75f);
            }
        }

        public void SetReversing(bool reversing)
        {
            _isReversing = reversing;
        }
    }
}