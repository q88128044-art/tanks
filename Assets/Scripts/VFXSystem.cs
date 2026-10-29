using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Tanks
{
    /// <summary>
    /// Spawns short-lived particle effects: muzzle flashes, surface-specific
    /// impacts (dirt / metal / concrete / wood), small and large explosions,
    /// scorch decals, exhaust and wreck smoke.
    /// Every one-shot effect destroys itself after its lifetime, and decals are
    /// capped, so temporary objects never accumulate in the scene.
    /// The system is created automatically for every loaded scene.
    /// </summary>
    public class VFXSystem : MonoBehaviour
    {
        public static VFXSystem Instance { get; private set; }

        [Header("Decals")]
        [SerializeField] private int _maxDecals = 24;
        [SerializeField] private float _decalLifetime = 25f;

        private Material _additive;
        private Material _alpha;
        private Material _decal;
        private Transform _root;
        private readonly Queue<GameObject> _decals = new Queue<GameObject>();

        /// <summary>Number of one-shot effects currently alive (for debugging).</summary>
        public int ActiveEffects => _root != null ? _root.childCount : 0;
        public int ActiveDecals => _decals.Count;
        public Material AdditiveMaterial => _additive;

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
                new GameObject("VFXSystem").AddComponent<VFXSystem>();
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
            _root = new GameObject("Effects").transform;
            _root.SetParent(transform, false);

            Texture2D circle = null;
            _additive = LoadOrCreate("VFX/ParticleAdditive", true, ref circle);
            _alpha = LoadOrCreate("VFX/ParticleAlpha", false, ref circle);
            _decal = Resources.Load<Material>("VFX/ScorchDecal");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private static Material LoadOrCreate(string path, bool additive, ref Texture2D circle)
        {
            Material material = Resources.Load<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(VFXMaterialUtil.ParticleShader);
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (circle == null)
            {
                circle = VFXMaterialUtil.CreateSoftCircle(64);
            }

            material = new Material(shader);
            VFXMaterialUtil.ConfigureParticle(material, additive, circle);
            return material;
        }

        #region Public API

        public void PlayMuzzleFlash(Vector3 position, Quaternion rotation, float size, Color color)
        {
            Transform fx = CreateContainer("FX_MuzzleFlash", position, rotation, 0.8f);
            Spawn(fx, new Burst
            {
                Name = "Flash", Material = _additive, Count = 3, Lifetime = new Vector2(0.05f, 0.08f),
                Speed = new Vector2(0f, 0.5f), Size = new Vector2(size * 1.2f, size * 1.8f),
                ColorA = color, ColorB = Color.Lerp(color, Color.white, 0.5f), Shape = ParticleSystemShapeType.Sphere,
                Radius = 0.02f, EndSize = 1.4f
            });
            Spawn(fx, new Burst
            {
                Name = "Sparks", Material = _additive, Count = 8, Lifetime = new Vector2(0.08f, 0.18f),
                Speed = new Vector2(6f, 12f), Size = new Vector2(0.04f, 0.08f), ColorA = color, ColorB = Color.white,
                Shape = ParticleSystemShapeType.Cone, Angle = 12f, Radius = 0.02f, Stretch = true, EndSize = 0.3f
            });
            Spawn(fx, new Burst
            {
                Name = "Smoke", Material = _alpha, Count = 4, Lifetime = new Vector2(0.4f, 0.7f),
                Speed = new Vector2(0.6f, 1.6f), Size = new Vector2(size * 0.6f, size * 1.1f),
                ColorA = new Color(0.55f, 0.55f, 0.55f, 0.35f), ColorB = new Color(0.4f, 0.4f, 0.4f, 0.25f),
                Shape = ParticleSystemShapeType.Cone, Angle = 18f, Radius = 0.05f, EndSize = 2.2f, Drag = 2f
            });
            AddLight(fx, Color.Lerp(color, Color.white, 0.3f), 4f, 4f + size * 4f, 0.07f);
        }

        public void PlayImpact(Vector3 position, Vector3 normal, SurfaceType surface, float size)
        {
            if (normal.sqrMagnitude < 0.001f)
            {
                normal = Vector3.up;
            }

            Quaternion rotation = Quaternion.LookRotation(normal);
            Transform fx = CreateContainer("FX_Impact_" + surface, position, rotation, 1.6f);
            float s = Mathf.Max(0.2f, size) / 0.3f;

            switch (surface)
            {
                case SurfaceType.Metal:
                    Spawn(fx, new Burst
                    {
                        Name = "Sparks", Material = _additive, Count = 18, Lifetime = new Vector2(0.2f, 0.45f),
                        Speed = new Vector2(4f, 10f), Size = new Vector2(0.04f, 0.08f),
                        ColorA = new Color(1f, 0.85f, 0.4f), ColorB = new Color(1f, 0.55f, 0.15f), Gravity = 1.2f,
                        Shape = ParticleSystemShapeType.Cone, Angle = 55f, Radius = 0.05f, Stretch = true, EndSize = 0.4f
                    });
                    Spawn(fx, new Burst
                    {
                        Name = "Flash", Material = _additive, Count = 1, Lifetime = new Vector2(0.06f, 0.08f),
                        Speed = Vector2.zero, Size = new Vector2(0.7f * s, 0.9f * s), ColorA = new Color(1f, 0.9f, 0.6f),
                        ColorB = Color.white, Radius = 0.01f, EndSize = 1.5f
                    });
                    AddLight(fx, new Color(1f, 0.75f, 0.4f), 3f, 4f, 0.08f);
                    break;

                case SurfaceType.Concrete:
                    Spawn(fx, new Burst
                    {
                        Name = "Dust", Material = _alpha, Count = 8, Lifetime = new Vector2(0.5f, 0.9f),
                        Speed = new Vector2(1f, 2.5f), Size = new Vector2(0.4f * s, 0.8f * s),
                        ColorA = new Color(0.62f, 0.6f, 0.56f, 0.55f), ColorB = new Color(0.5f, 0.49f, 0.46f, 0.45f),
                        Shape = ParticleSystemShapeType.Cone, Angle = 35f, Radius = 0.1f, EndSize = 2f, Drag = 3f
                    });
                    Spawn(fx, new Burst
                    {
                        Name = "Chips", Material = _alpha, Count = 10, Lifetime = new Vector2(0.4f, 0.7f),
                        Speed = new Vector2(3f, 6f), Size = new Vector2(0.05f, 0.1f),
                        ColorA = new Color(0.45f, 0.44f, 0.42f), ColorB = new Color(0.7f, 0.68f, 0.64f), Gravity = 1.5f,
                        Shape = ParticleSystemShapeType.Cone, Angle = 45f, Radius = 0.05f, EndSize = 1f
                    });
                    Spawn(fx, new Burst
                    {
                        Name = "Sparks", Material = _additive, Count = 6, Lifetime = new Vector2(0.1f, 0.25f),
                        Speed = new Vector2(4f, 8f), Size = new Vector2(0.03f, 0.06f), ColorA = new Color(1f, 0.8f, 0.4f),
                        ColorB = new Color(1f, 0.6f, 0.2f), Gravity = 1f, Shape = ParticleSystemShapeType.Cone,
                        Angle = 50f, Radius = 0.05f, Stretch = true, EndSize = 0.4f
                    });
                    break;

                case SurfaceType.Wood:
                    Spawn(fx, new Burst
                    {
                        Name = "Splinters", Material = _alpha, Count = 12, Lifetime = new Vector2(0.4f, 0.8f),
                        Speed = new Vector2(3f, 6f), Size = new Vector2(0.06f, 0.12f),
                        ColorA = new Color(0.55f, 0.38f, 0.2f), ColorB = new Color(0.75f, 0.58f, 0.35f), Gravity = 1.4f,
                        Shape = ParticleSystemShapeType.Cone, Angle = 45f, Radius = 0.05f, Stretch = true, EndSize = 1f
                    });
                    Spawn(fx, new Burst
                    {
                        Name = "Dust", Material = _alpha, Count = 5, Lifetime = new Vector2(0.4f, 0.7f),
                        Speed = new Vector2(0.8f, 1.8f), Size = new Vector2(0.3f * s, 0.6f * s),
                        ColorA = new Color(0.6f, 0.5f, 0.35f, 0.45f), ColorB = new Color(0.5f, 0.42f, 0.3f, 0.35f),
                        Shape = ParticleSystemShapeType.Cone, Angle = 30f, Radius = 0.1f, EndSize = 2f, Drag = 3f
                    });
                    break;

                default:
                    Spawn(fx, new Burst
                    {
                        Name = "DirtPuff", Material = _alpha, Count = 10, Lifetime = new Vector2(0.6f, 1.1f),
                        Speed = new Vector2(1f, 3f), Size = new Vector2(0.5f * s, 1f * s),
                        ColorA = new Color(0.42f, 0.33f, 0.22f, 0.6f), ColorB = new Color(0.34f, 0.27f, 0.18f, 0.5f),
                        Gravity = 0.15f, Shape = ParticleSystemShapeType.Cone, Angle = 30f, Radius = 0.15f,
                        EndSize = 2.2f, Drag = 2.5f
                    });
                    Spawn(fx, new Burst
                    {
                        Name = "Clods", Material = _alpha, Count = 14, Lifetime = new Vector2(0.5f, 0.9f),
                        Speed = new Vector2(3f, 7f), Size = new Vector2(0.06f, 0.14f),
                        ColorA = new Color(0.25f, 0.19f, 0.12f), ColorB = new Color(0.36f, 0.28f, 0.18f), Gravity = 2f,
                        Shape = ParticleSystemShapeType.Cone, Angle = 35f, Radius = 0.08f, EndSize = 1f
                    });
                    break;
            }
        }

        /// <summary>Explosion. "large" adds debris, a bigger fireball, a ground scorch decal and camera shake.</summary>
        public void PlayExplosion(Vector3 position, float size, bool large)
        {
            size = Mathf.Max(0.3f, size);
            Transform fx = CreateContainer(large ? "FX_ExplosionLarge" : "FX_Explosion", position, Quaternion.identity, large ? 3.5f : 2.2f);
            Spawn(fx, new Burst
            {
                Name = "Fireball", Material = _additive, Count = large ? 18 : 10, Lifetime = new Vector2(0.25f, 0.55f),
                Speed = new Vector2(0.8f * size, 2.6f * size), Size = new Vector2(0.7f * size, 1.4f * size),
                ColorA = new Color(1f, 0.55f, 0.15f), ColorB = new Color(1f, 0.85f, 0.4f),
                Shape = ParticleSystemShapeType.Sphere, Radius = 0.25f * size, EndSize = 1.8f, Drag = 3f
            });
            Spawn(fx, new Burst
            {
                Name = "Smoke", Material = _alpha, Count = large ? 16 : 8, Lifetime = new Vector2(1.2f, 2.2f),
                Speed = new Vector2(0.4f * size, 1.5f * size), Size = new Vector2(0.9f * size, 1.8f * size),
                ColorA = new Color(0.18f, 0.17f, 0.16f, 0.5f), ColorB = new Color(0.3f, 0.29f, 0.27f, 0.4f),
                Gravity = -0.05f, Shape = ParticleSystemShapeType.Hemisphere, Radius = 0.3f * size, EndSize = 2.4f, Drag = 1.5f
            });
            Spawn(fx, new Burst
            {
                Name = "Sparks", Material = _additive, Count = large ? 40 : 16, Lifetime = new Vector2(0.3f, 0.8f),
                Speed = new Vector2(5f, large ? 15f : 9f), Size = new Vector2(0.05f, 0.1f),
                ColorA = new Color(1f, 0.8f, 0.35f), ColorB = new Color(1f, 0.5f, 0.1f), Gravity = 1.2f,
                Shape = ParticleSystemShapeType.Hemisphere, Radius = 0.2f, Stretch = true, EndSize = 0.3f
            });

            if (large)
            {
                Spawn(fx, new Burst
                {
                    Name = "Debris", Material = _alpha, Count = 18, Lifetime = new Vector2(0.7f, 1.3f),
                    Speed = new Vector2(4f, 9f), Size = new Vector2(0.1f, 0.22f),
                    ColorA = new Color(0.12f, 0.12f, 0.12f), ColorB = new Color(0.3f, 0.26f, 0.2f), Gravity = 2.2f,
                    Shape = ParticleSystemShapeType.Hemisphere, Radius = 0.3f, EndSize = 1f
                });
                Spawn(fx, new Burst
                {
                    Name = "Shockwave", Material = _additive, Count = 1, Lifetime = new Vector2(0.18f, 0.18f),
                    Speed = Vector2.zero, Size = new Vector2(2.5f * size, 2.5f * size),
                    ColorA = new Color(1f, 0.7f, 0.4f, 0.6f), ColorB = new Color(1f, 0.7f, 0.4f, 0.6f),
                    Radius = 0.01f, EndSize = 2.2f
                });
                CameraShake.Shake(0.35f);
            }

            AddLight(fx, new Color(1f, 0.6f, 0.25f), large ? 10f : 5f, (large ? 9f : 5f) * size, large ? 0.4f : 0.2f);
            SpawnScorch(position, (large ? 1.6f : 0.9f) * size);
        }

        /// <summary>Big explosion when a tank is destroyed.</summary>
        public void PlayDestruction(Vector3 position)
        {
            PlayExplosion(position + Vector3.up * 0.5f, 2.2f, true);
        }

        public void PlayPickup(Vector3 position, Color color)
        {
            Transform fx = CreateContainer("FX_Pickup", position, Quaternion.identity, 1.5f);
            Spawn(fx, new Burst
            {
                Name = "Glow", Material = _additive, Count = 20, Lifetime = new Vector2(0.4f, 0.8f),
                Speed = new Vector2(1f, 3f), Size = new Vector2(0.15f, 0.3f), ColorA = color, ColorB = Color.white,
                Gravity = -0.3f, Shape = ParticleSystemShapeType.Sphere, Radius = 0.5f, EndSize = 0.2f
            });
            AddLight(fx, color, 4f, 5f, 0.3f);
        }

        /// <summary>Scorch mark on the ground below the given point (requires the Decal renderer feature).</summary>
        public void SpawnScorch(Vector3 position, float size)
        {
            if (_decal == null)
            {
                return;
            }

            if (!Physics.Raycast(position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            if (hit.collider.GetComponentInParent<IDamageable>() != null)
            {
                return;
            }

            GameObject go = new GameObject("Decal_Scorch");
            go.transform.SetPositionAndRotation(hit.point + Vector3.up * 0.4f, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
            DecalProjector projector = go.AddComponent<DecalProjector>();
            projector.material = _decal;
            float s = Mathf.Clamp(size * 2f, 1f, 6f);
            projector.size = new Vector3(s, s, 1.2f);
            projector.pivot = Vector3.zero;
            projector.fadeFactor = 0.9f;
            go.transform.SetParent(transform, true);

            _decals.Enqueue(go);
            Destroy(go, _decalLifetime);
            while (_decals.Count > _maxDecals)
            {
                GameObject oldest = _decals.Dequeue();
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }
        }

        /// <summary>Continuous exhaust smoke whose rate is driven by the engine.</summary>
        public ParticleSystem CreateExhaust(Transform parent)
        {
            ParticleSystem ps = Spawn(parent, new Burst
            {
                Name = "ExhaustSmoke", Material = _alpha, Count = 0, Lifetime = new Vector2(0.6f, 1.1f),
                Speed = new Vector2(0.4f, 1f), Size = new Vector2(0.15f, 0.3f),
                ColorA = new Color(0.25f, 0.25f, 0.25f, 0.35f), ColorB = new Color(0.4f, 0.4f, 0.4f, 0.25f),
                Gravity = -0.05f, Shape = ParticleSystemShapeType.Cone, Angle = 15f, Radius = 0.03f, EndSize = 3f,
                Loop = true, MaxParticles = 80
            });
            ps.transform.localRotation = Quaternion.Euler(-60f, 180f, 0f);
            return ps;
        }

        /// <summary>Thick looping smoke over a destroyed tank (destroyed together with the wreck).</summary>
        public void AttachWreckSmoke(Transform parent)
        {
            ParticleSystem smoke = Spawn(parent, new Burst
            {
                Name = "WreckSmoke", Material = _alpha, Count = 0, Lifetime = new Vector2(1.8f, 2.8f),
                Speed = new Vector2(0.8f, 1.6f), Size = new Vector2(0.6f, 1.2f),
                ColorA = new Color(0.1f, 0.1f, 0.1f, 0.45f), ColorB = new Color(0.22f, 0.21f, 0.2f, 0.35f),
                Gravity = -0.08f, Shape = ParticleSystemShapeType.Cone, Angle = 10f, Radius = 0.4f, EndSize = 3f,
                Loop = true, MaxParticles = 120
            });
            smoke.transform.localPosition = Vector3.up * 0.8f;
            smoke.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.EmissionModule emission = smoke.emission;
            emission.rateOverTime = 10f;

            ParticleSystem fire = Spawn(parent, new Burst
            {
                Name = "WreckFire", Material = _additive, Count = 0, Lifetime = new Vector2(0.3f, 0.6f),
                Speed = new Vector2(0.5f, 1.2f), Size = new Vector2(0.3f, 0.6f),
                ColorA = new Color(1f, 0.5f, 0.15f), ColorB = new Color(1f, 0.8f, 0.3f),
                Shape = ParticleSystemShapeType.Cone, Angle = 8f, Radius = 0.3f, EndSize = 0.3f,
                Loop = true, MaxParticles = 40
            });
            fire.transform.localPosition = Vector3.up * 0.7f;
            fire.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.EmissionModule fireEmission = fire.emission;
            fireEmission.rateOverTime = 18f;
        }

        #endregion

        #region Particle construction

        private sealed class Burst
        {
            public string Name = "Burst";
            public Material Material;
            public int Count = 10;
            public Vector2 Lifetime = new Vector2(0.2f, 0.4f);
            public Vector2 Speed = new Vector2(1f, 3f);
            public Vector2 Size = new Vector2(0.2f, 0.4f);
            public Color ColorA = Color.white;
            public Color ColorB = Color.white;
            public float Gravity;
            public ParticleSystemShapeType Shape = ParticleSystemShapeType.Sphere;
            public float Angle = 25f;
            public float Radius = 0.1f;
            public float EndSize = 1f;
            public bool Stretch;
            public float Drag;
            public bool Loop;
            public int MaxParticles;
        }

        private Transform CreateContainer(string name, Vector3 position, Quaternion rotation, float lifetime)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root != null ? _root : transform, false);
            go.transform.SetPositionAndRotation(position, rotation);
            Destroy(go, lifetime);
            return go.transform;
        }

        private static void AddLight(Transform parent, Color color, float intensity, float range, float duration)
        {
            GameObject go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.AddComponent<Light>();
            go.AddComponent<LightFade>().Init(intensity, range, color, duration);
        }

        private ParticleSystem Spawn(Transform parent, Burst b)
        {
            GameObject go = new GameObject(b.Name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = b.Loop ? 1f : 0.1f;
            main.loop = b.Loop;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(b.Lifetime.x, b.Lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(b.Speed.x, b.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(b.Size.x, b.Size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(b.ColorA, b.ColorB);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = b.Gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = b.MaxParticles > 0 ? b.MaxParticles : Mathf.Max(1, b.Count);
            main.stopAction = ParticleSystemStopAction.None;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            if (b.Count > 0)
            {
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)b.Count) });
            }

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = b.Shape;
            shape.angle = b.Angle;
            shape.radius = Mathf.Max(0.001f, b.Radius);

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, b.EndSize));

            if (b.Drag > 0f)
            {
                ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.limit = 1000f;
                limit.dampen = 0f;
                limit.drag = b.Drag;
            }

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = b.Material;
            renderer.renderMode = b.Stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (b.Stretch)
            {
                renderer.velocityScale = 0.04f;
                renderer.lengthScale = 2f;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
            return ps;
        }

        #endregion
    }
}
