using UnityEngine;
using UnityEngine.AI;

namespace Tanks
{
    /// <summary>
    /// Looping engine sound whose pitch and volume follow the tank's speed
    /// (works for the player's Rigidbody and for NavMeshAgent enemies), plus
    /// exhaust smoke that thickens under load.
    /// </summary>
    public class EngineLoop : MonoBehaviour
    {
        [Header("Audio")]
        [Range(0f, 1f)] [SerializeField] private float _idleVolume = 0.12f;
        [Range(0f, 1f)] [SerializeField] private float _fullVolume = 0.45f;
        [Range(0.3f, 2f)] [SerializeField] private float _idlePitch = 0.7f;
        [Range(0.3f, 2.5f)] [SerializeField] private float _fullPitch = 1.4f;
        [SerializeField] private float _response = 3f;
        [Range(0f, 1f)] [SerializeField] private float _spatialBlend = 0.6f;

        [Header("Exhaust")]
        [SerializeField] private bool _exhaustSmoke = true;
        [SerializeField] private float _idleSmokeRate = 3f;
        [SerializeField] private float _fullSmokeRate = 22f;

        private AudioSource _source;
        private Rigidbody _body;
        private NavMeshAgent _agent;
        private TankMover _mover;
        private ParticleSystem[] _smoke = new ParticleSystem[0];
        private float _load;

        public float Load => _load;

        private void Awake()
        {
            _body = GetComponentInParent<Rigidbody>();
            _agent = GetComponentInParent<NavMeshAgent>();
            _mover = GetComponentInParent<TankMover>();

            _source = GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }

            _source.clip = AudioSystem.EngineLoopClip;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = _spatialBlend;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = 10f;
            _source.maxDistance = 70f;
            _source.dopplerLevel = 0f;
            _source.volume = _idleVolume;
            _source.pitch = _idlePitch;
        }

        private void Start()
        {
            _source.time = Random.Range(0f, _source.clip.length * 0.9f);
            _source.Play();

            if (_exhaustSmoke && VFXSystem.Instance != null)
            {
                System.Collections.Generic.List<ParticleSystem> list = new System.Collections.Generic.List<ParticleSystem>();
                foreach (Transform child in transform.root.GetComponentsInChildren<Transform>())
                {
                    if (child.name.StartsWith("Exhaust"))
                    {
                        list.Add(VFXSystem.Instance.CreateExhaust(child));
                    }
                }

                _smoke = list.ToArray();
            }
        }

        private void OnDisable()
        {
            if (_source != null)
            {
                _source.Stop();
            }

            foreach (ParticleSystem ps in _smoke)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void OnEnable()
        {
            if (_source != null && _source.clip != null && !_source.isPlaying && Time.timeSinceLevelLoad > 0.1f)
            {
                _source.Play();
            }
        }

        private void Update()
        {
            float speed = 0f;
            float maxSpeed = 6f;

            if (_agent != null && _agent.enabled)
            {
                speed = _agent.velocity.magnitude;
                maxSpeed = Mathf.Max(0.1f, _agent.speed);
            }
            else if (_body != null)
            {
                Vector3 v = _body.linearVelocity;
                v.y = 0f;
                speed = v.magnitude;
                maxSpeed = _mover != null ? Mathf.Max(0.1f, _mover.MoveSpeed) : 6f;
            }

            float target = Mathf.Clamp01(speed / maxSpeed);
            if (_mover != null && (Mathf.Abs(_mover.Throttle) > 0.01f || Mathf.Abs(_mover.Steering) > 0.01f))
            {
                target = Mathf.Max(target, 0.25f);
            }

            _load = Mathf.MoveTowards(_load, target, _response * Time.deltaTime);
            _source.pitch = Mathf.Lerp(_idlePitch, _fullPitch, _load);
            _source.volume = Mathf.Lerp(_idleVolume, _fullVolume, _load);

            float rate = Mathf.Lerp(_idleSmokeRate, _fullSmokeRate, _load);
            foreach (ParticleSystem ps in _smoke)
            {
                if (ps == null) continue;
                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = rate;
                if (!ps.isPlaying) ps.Play();
            }
        }
    }
}
