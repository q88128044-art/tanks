using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks
{
    [CreateAssetMenu(fileName = "SFXData", menuName = "Tanks/SFX Data")]
    public class SFXData : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private SurfaceType _surface = SurfaceType.Dirt;

        [Header("Impact")]
        [Range(0f, 1f)] [SerializeField] private float _volume = 0.8f;
        [Range(-3f, 3f)] [SerializeField] private float _pitch = 1f;
        [Range(0.01f, 2f)] [SerializeField] private float _duration = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float _lowPassCutoff = 8000f;

        [Header("Visual")]
        [Range(0f, 5f)] [SerializeField] private float _flashSize = 1.2f;
        [Range(0f, 3f)] [SerializeField] private float _flashDuration = 0.12f;
        [Range(0f, 1f)] [SerializeField] private float _sparkIntensity = 0.8f;

        public SurfaceType Surface => _surface;
        public float Volume => _volume;
        public float Pitch => _pitch;
        public float Duration => _duration;
        public float LowPassCutoff => _lowPassCutoff;
        public float FlashSize => _flashSize;
        public float FlashDuration => _flashDuration;
        public float SparkIntensity => _sparkIntensity;
    }
}