using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tanks
{
    /// <summary>
    /// Plays all one-shot sounds (shots, impacts, explosions, pickups) through a
    /// fixed pool of AudioSources, so no temporary audio objects are created.
    /// Clips are synthesised procedurally. Created automatically for every scene.
    /// </summary>
    public class AudioSystem : MonoBehaviour
    {
        public static AudioSystem Instance { get; private set; }

        private const int SampleRate = 44100;

        [Header("Mix")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 0.85f;
        [Range(4, 32)] [SerializeField] private int _voices = 20;
        [Range(0f, 1f)] [SerializeField] private float _spatialBlend = 0.55f;

        [Header("Shot pitch randomisation")]
        [SerializeField] private float _minShotPitch = 0.95f;
        [SerializeField] private float _maxShotPitch = 1.05f;

        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private readonly Dictionary<WeaponConfig, AudioClip> _shotClips = new Dictionary<WeaponConfig, AudioClip>();
        private int _next;

        private AudioClip _impactDirt;
        private AudioClip _impactMetal;
        private AudioClip _impactConcrete;
        private AudioClip _impactWood;
        private AudioClip _explosionSmall;
        private AudioClip _explosionLarge;
        private AudioClip _pickup;

        private static AudioClip s_engineLoop;

        /// <summary>Seamless engine loop shared by every tank.</summary>
        public static AudioClip EngineLoopClip
        {
            get
            {
                if (s_engineLoop == null)
                {
                    s_engineLoop = SynthesizeEngineLoop();
                }

                return s_engineLoop;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance == null)
            {
                new GameObject("AudioSystem").AddComponent<AudioSystem>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            for (int i = 0; i < _voices; i++)
            {
                GameObject go = new GameObject("Voice_" + i);
                go.transform.SetParent(transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = _spatialBlend;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 12f;
                source.maxDistance = 90f;
                source.dopplerLevel = 0f;
                _sources.Add(source);
            }

            _impactDirt = SynthesizeImpactDirt();
            _impactMetal = SynthesizeImpactMetal();
            _impactConcrete = SynthesizeImpactConcrete();
            _impactWood = SynthesizeImpactWood();
            _explosionSmall = SynthesizeExplosion(0.9f, 0.12f, "explosion_small");
            _explosionLarge = SynthesizeExplosion(1.8f, 0.05f, "explosion_large");
            _pickup = SynthesizePickup();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region Public API

        /// <summary>Weapon shot with a random pitch in [0.95, 1.05].</summary>
        public void PlayShot(Vector3 position, WeaponConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (!_shotClips.TryGetValue(config, out AudioClip clip) || clip == null)
            {
                float weight = Mathf.InverseLerp(1f, 2.8f, config.ProjectileScale);
                clip = SynthesizeShot(Mathf.Lerp(150f, 48f, weight), Mathf.Lerp(0.28f, 0.9f, weight), "shot_" + config.name);
                _shotClips[config] = clip;
            }

            float pitch = Random.Range(_minShotPitch, _maxShotPitch);
            float volume = Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(1f, 2.8f, config.ProjectileScale));
            Play(clip, position, volume, pitch);
        }

        public void PlayImpact(Vector3 position, SurfaceType surface, float volume)
        {
            AudioClip clip;
            switch (surface)
            {
                case SurfaceType.Metal: clip = _impactMetal; break;
                case SurfaceType.Concrete: clip = _impactConcrete; break;
                case SurfaceType.Wood: clip = _impactWood; break;
                default: clip = _impactDirt; break;
            }

            Play(clip, position, Mathf.Clamp01(volume), Random.Range(0.92f, 1.08f));
        }

        public void PlayExplosion(Vector3 position, bool large, float volume)
        {
            Play(large ? _explosionLarge : _explosionSmall, position, Mathf.Clamp01(volume), Random.Range(0.95f, 1.05f));
        }

        public void PlayPickup(Vector3 position)
        {
            Play(_pickup, position, 0.6f, 1f);
        }

        #endregion

        private void Play(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (clip == null || _sources.Count == 0)
            {
                return;
            }

            AudioSource source = null;
            for (int i = 0; i < _sources.Count; i++)
            {
                AudioSource candidate = _sources[(_next + i) % _sources.Count];
                if (!candidate.isPlaying)
                {
                    source = candidate;
                    _next = (_next + i + 1) % _sources.Count;
                    break;
                }
            }

            if (source == null)
            {
                source = _sources[_next];
                _next = (_next + 1) % _sources.Count;
            }

            source.transform.position = position;
            source.clip = clip;
            source.volume = volume * _masterVolume;
            source.pitch = pitch;
            source.Play();
        }

        #region Synthesis

        private static float Noise(System.Random rng)
        {
            return (float)rng.NextDouble() * 2f - 1f;
        }

        private static AudioClip MakeClip(string name, float[] data, bool normalize = true)
        {
            if (normalize)
            {
                float peak = 0.0001f;
                for (int i = 0; i < data.Length; i++)
                {
                    peak = Mathf.Max(peak, Mathf.Abs(data[i]));
                }

                float gain = 0.9f / peak;
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] *= gain;
                }
            }

            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeShot(float boom, float duration, string name)
        {
            System.Random rng = new System.Random(name.GetHashCode());
            int n = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[n];
            float low = 0f;
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float x = Noise(rng);
                low += 0.12f * (x - low);
                float freq = boom * (1f - 0.45f * t / duration);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float crack = x * Mathf.Exp(-t * 45f) * 0.7f;
                float body = Mathf.Sin(phase) * Mathf.Exp(-t * 5f / duration) * 0.9f;
                float rumble = low * Mathf.Exp(-t * 4f / duration) * 1.6f;
                float attack = Mathf.Clamp01(t / 0.002f);
                data[i] = (crack + body + rumble) * attack;
            }

            return MakeClip(name, data);
        }

        private static AudioClip SynthesizeImpactMetal()
        {
            System.Random rng = new System.Random(11);
            int n = Mathf.CeilToInt(SampleRate * 0.45f);
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float ring = Mathf.Sin(2f * Mathf.PI * 910f * t) * 0.5f
                             + Mathf.Sin(2f * Mathf.PI * 1430f * t) * 0.35f
                             + Mathf.Sin(2f * Mathf.PI * 2260f * t) * 0.2f;
                data[i] = ring * Mathf.Exp(-t * 11f) + Noise(rng) * Mathf.Exp(-t * 90f) * 0.8f;
            }

            return MakeClip("impact_metal", data);
        }

        private static AudioClip SynthesizeImpactDirt()
        {
            System.Random rng = new System.Random(12);
            int n = Mathf.CeilToInt(SampleRate * 0.35f);
            float[] data = new float[n];
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                low += 0.07f * (Noise(rng) - low);
                data[i] = low * Mathf.Exp(-t * 13f) * 2.5f + Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 22f) * 0.6f;
            }

            return MakeClip("impact_dirt", data);
        }

        private static AudioClip SynthesizeImpactConcrete()
        {
            System.Random rng = new System.Random(13);
            int n = Mathf.CeilToInt(SampleRate * 0.3f);
            float[] data = new float[n];
            float band = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                band += 0.3f * (Noise(rng) - band);
                data[i] = band * Mathf.Exp(-t * 20f) * 1.5f + Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 28f) * 0.5f;
            }

            return MakeClip("impact_concrete", data);
        }

        private static AudioClip SynthesizeImpactWood()
        {
            System.Random rng = new System.Random(14);
            int n = Mathf.CeilToInt(SampleRate * 0.3f);
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float knock = Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 510f * t) * 0.3f;
                data[i] = knock * Mathf.Exp(-t * 25f) + Noise(rng) * Mathf.Exp(-t * 60f) * 0.4f;
            }

            return MakeClip("impact_wood", data);
        }

        private static AudioClip SynthesizeExplosion(float duration, float lowpass, string name)
        {
            System.Random rng = new System.Random(name.GetHashCode());
            int n = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[n];
            float low = 0f;
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                low += lowpass * (Noise(rng) - low);
                phase += 2f * Mathf.PI * Mathf.Lerp(55f, 28f, t / duration) / SampleRate;
                float env = Mathf.Clamp01(t / 0.006f) * Mathf.Exp(-t * 3.2f / duration);
                data[i] = (low * 3f + Mathf.Sin(phase) * 0.6f + Noise(rng) * Mathf.Exp(-t * 30f) * 0.5f) * env;
            }

            return MakeClip(name, data);
        }

        private static AudioClip SynthesizePickup()
        {
            int n = Mathf.CeilToInt(SampleRate * 0.36f);
            float[] data = new float[n];
            float[] notes = { 660f, 880f, 1320f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                int note = Mathf.Min(2, (int)(t / 0.12f));
                float local = t - note * 0.12f;
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[note] * t) * Mathf.Exp(-local * 14f) * 0.6f;
            }

            return MakeClip("pickup", data);
        }

        /// <summary>
        /// One second of integer-frequency harmonics, so the clip loops without a click.
        /// </summary>
        private static AudioClip SynthesizeEngineLoop()
        {
            int n = SampleRate;
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float pistons = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 12f * t);
                float hum = Mathf.Sin(2f * Mathf.PI * 36f * t) * 0.55f
                            + Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.3f
                            + Mathf.Sin(2f * Mathf.PI * 108f * t) * 0.15f
                            + Mathf.Sin(2f * Mathf.PI * 181f * t) * 0.06f;
                float rattle = Mathf.Sin(2f * Mathf.PI * 240f * t) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 24f * t)), 8f) * 0.25f;
                data[i] = (hum * pistons + rattle) * 0.6f;
            }

            return MakeClip("engine_loop", data);
        }

        #endregion
    }
}
