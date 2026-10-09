using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks
{
    public class AudioSystem : MonoBehaviour
    {
        public static AudioSystem Instance { get; private set; }

        [Header("Pool")]
        [Range(1, 32)] [SerializeField] private int _poolSize = 12;
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 0.9f;

        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(transform.root);
        }

        private void Start()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject go = new GameObject("AudioSource_" + i);
                go.transform.SetParent(transform, false);
                AudioSource source = go.GetComponent<AudioSource>();
                if (source == null)
                {
                    source = go.AddComponent<AudioSource>();
                }
                source.spatialize = false;
                source.playOnAwake = false;
                _sources.Add(source);
            }
        }

        public void PlayShot(Vector3 position, float baseFrequency, float duration, float volume, float pitch)
        {
            AudioClip clip = GetOrGenerate("shot_" + baseFrequency.ToString("F0") + "_" + duration.ToString("F2"), () => SynthesizeShot(baseFrequency, duration));
            PlayAt(position, clip, volume * _masterVolume, pitch);
        }

        public void PlayImpact(Vector3 position, float frequency, float duration, float volume, float pitch)
        {
            AudioClip clip = GetOrGenerate("impact_" + frequency.ToString("F0") + "_" + duration.ToString("F2"), () => SynthesizeImpact(frequency, duration));
            PlayAt(position, clip, volume * _masterVolume, pitch);
        }

        public void PlayEngine(float frequency, float duration, float volume)
        {
            AudioClip clip = GetOrGenerate("engine_" + frequency.ToString("F2"), () => SynthesizeEngine(frequency, duration));
            foreach (AudioSource source in _sources)
            {
                if (source == null) continue;
                if (!source.isPlaying)
                {
                    source.spatialize = false;
                    source.clip = clip;
                    source.volume = volume * _masterVolume;
                    source.pitch = 1f;
                    source.Play();
                    return;
                }
            }
        }

        public void PlayExplosion(Vector3 position, float volume, float pitch)
        {
            AudioClip clip = GetOrGenerate("explosion", () => SynthesizeExplosion(1.2f));
            PlayAt(position, clip, volume * _masterVolume, pitch);
        }

        private void PlayAt(Vector3 position, AudioClip clip, float volume, float pitch)
        {
            foreach (AudioSource source in _sources)
            {
                if (source == null) continue;
                if (!source.isPlaying)
                {
                    source.transform.position = position;
                    source.spatialize = true;
                    source.clip = clip;
                    source.volume = volume;
                    source.pitch = pitch;
                    source.Play();
                    return;
                }
            }
        }

        private AudioClip GetOrGenerate(string key, Func<AudioClip> generator)
        {
            if (_cache.TryGetValue(key, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            AudioClip clip = generator();
            _cache[key] = clip;
            return clip;
        }

        private static AudioClip SynthesizeShot(float baseFrequency, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Pow(1f - t / duration, 2f);
                float noise = (float)UnityEngine.Random.value * 2f - 1f;
                data[i] = noise * env * 0.9f;
            }

            return MakeClip("shot", data, sampleRate);
        }

        private static AudioClip SynthesizeImpact(float frequency, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 18f);
                float osc = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f;
                data[i] = (osc + (UnityEngine.Random.value * 2f - 1f) * 0.3f) * env * 0.8f;
            }

            return MakeClip("impact", data, sampleRate);
        }

        private static AudioClip SynthesizeEngine(float frequency, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration) * 0.6f;
                float hum = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f;
                float rattle = Mathf.Sin(2f * Mathf.PI * frequency * 2.3f * t) * 0.15f;
                data[i] = (hum + rattle) * env * 0.5f;
            }

            return MakeClip("engine", data, sampleRate);
        }

        private static AudioClip SynthesizeExplosion(float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 4f);
                float noise = (float)UnityEngine.Random.value * 2f - 1f;
                float rumble = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.5f;
                data[i] = (noise * 0.7f + rumble) * env;
            }

            return MakeClip("explosion", data, sampleRate);
        }

        private static AudioClip MakeClip(string name, float[] data, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}