using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tanks.EditorTools
{
    /// <summary>
    /// Asset side of the polish pass: VFX materials and textures in Resources,
    /// Decal renderer feature, weapon / tank configs, level textures,
    /// day & sunset volume profiles and the tank prefabs.
    /// </summary>
    public static class ArenaPolishAssets
    {
        public const string ConfigsFolder = "Assets/Game/Configs";
        public const string MaterialsFolder = "Assets/Game/Materials";
        public const string TexturesFolder = "Assets/Game/Textures";
        public const string PostFolder = "Assets/Game/PostProcessing";
        public const string ResourcesVfx = "Assets/Resources/VFX";
        public const string PlayerPrefab = "Assets/Game/Prefabs/PlayerTank.prefab";
        public const string EnemyPrefab = "Assets/Game/Prefabs/EnemyTank.prefab";
        public const string DayProfilePath = PostFolder + "/Volume_Day.asset";
        public const string SunsetProfilePath = PostFolder + "/Volume_Sunset.asset";

        [MenuItem("Tools/Tanks/Polish/1. Assets, Configs, Prefabs")]
        public static void RunAll()
        {
            EnsureFolders();
            EnsureVfxResources();
            EnsureRendererFeatures();
            ConfigureWeapons();
            EnsureTankConfigs();
            EnsureLevelMaterials();
            EnsureVolumeProfiles();
            UpdatePrefabs();
            ConfigureNavMeshAgentType();
            ConfigureQuality();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Polish] Assets, configs and prefabs updated.");
        }

        public static void EnsureFolders()
        {
            CreateFolder("Assets", "Resources");
            CreateFolder("Assets/Resources", "VFX");
            CreateFolder("Assets/Game", "Textures");
            CreateFolder("Assets/Game", "Configs");
            CreateFolder("Assets/Game", "PostProcessing");
        }

        private static void CreateFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        #region VFX resources

        public static void EnsureVfxResources()
        {
            Texture2D circle = SaveTexture(ResourcesVfx + "/SoftCircle.png", VFXMaterialUtil.CreateSoftCircle(64), true);
            Texture2D scorch = SaveTexture(ResourcesVfx + "/Scorch.png", VFXMaterialUtil.CreateScorch(256), true);

            Shader particle = Shader.Find(VFXMaterialUtil.ParticleShader);
            Material additive = LoadOrCreateMaterial(ResourcesVfx + "/ParticleAdditive.mat", particle);
            VFXMaterialUtil.ConfigureParticle(additive, true, circle);
            EditorUtility.SetDirty(additive);

            Material alpha = LoadOrCreateMaterial(ResourcesVfx + "/ParticleAlpha.mat", particle);
            VFXMaterialUtil.ConfigureParticle(alpha, false, circle);
            EditorUtility.SetDirty(alpha);

            Shader decalShader = Shader.Find("Shader Graphs/Decal");
            if (decalShader != null)
            {
                Material decal = LoadOrCreateMaterial(ResourcesVfx + "/ScorchDecal.mat", decalShader);
                if (decal.HasProperty("Base_Map")) decal.SetTexture("Base_Map", scorch);
                if (decal.HasProperty("_BaseMap")) decal.SetTexture("_BaseMap", scorch);
                decal.enableInstancing = true;
                EditorUtility.SetDirty(decal);
            }
            else
            {
                Debug.LogWarning("[Polish] Decal shader not found, scorch decals disabled.");
            }
        }

        public static Texture2D SaveTexture(string path, Texture2D source, bool alpha, bool repeat = false)
        {
            File.WriteAllBytes(path, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = alpha;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader && shader != null)
            {
                material.shader = shader;
            }

            return material;
        }

        #endregion

        #region Renderer features

        public static void EnsureRendererFeatures()
        {
            foreach (string path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null) continue;
                AddFeature<DecalRendererFeature>(data);
            }

            UniversalRendererData pc = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            if (pc != null)
            {
                foreach (ScriptableRendererFeature feature in pc.rendererFeatures)
                {
                    if (feature != null && feature.GetType().Name == "ScreenSpaceAmbientOcclusion")
                    {
                        feature.SetActive(true);
                        EditorUtility.SetDirty(feature);
                    }
                }
            }
        }

        private static void AddFeature<T>(ScriptableRendererData data) where T : ScriptableRendererFeature
        {
            foreach (ScriptableRendererFeature existing in data.rendererFeatures)
            {
                if (existing is T)
                {
                    existing.SetActive(true);
                    return;
                }
            }

            T feature = ScriptableObject.CreateInstance<T>();
            feature.name = typeof(T).Name.Replace("RendererFeature", string.Empty);
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

            SerializedObject so = new SerializedObject(data);
            SerializedProperty features = so.FindProperty("m_RendererFeatures");
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Configs

        public static WeaponConfig LoadWeapon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<WeaponConfig>(ConfigsFolder + "/" + name + ".asset");
        }

        public static void ConfigureWeapons()
        {
            SetWeapon("RifleConfig", "Rifle", new Color(1f, 0.88f, 0.3f), 0.35f, new Color(1f, 0.8f, 0.35f), 0.12f, 0.25f, 0.5f, false, 0f, 1.2f);
            SetWeapon("CannonConfig", "Cannon", new Color(1f, 0.5f, 0.15f), 0.6f, new Color(1f, 0.6f, 0.2f), 0.2f, 0.4f, 0.7f, false, 0f, 2.5f);
            SetWeapon("HeavyConfig", "Heavy Gun", new Color(0.8f, 0.3f, 1f), 0.95f, new Color(1f, 0.55f, 0.25f), 0.3f, 0.6f, 0.9f, true, 1.7f, 4.5f);
        }

        private static void SetWeapon(string asset, string displayName, Color tint, float flashSize, Color flashColor,
            float trail, float impactSize, float impactVolume, bool heavy, float explosionSize, float recoil)
        {
            WeaponConfig config = LoadWeapon(asset);
            if (config == null)
            {
                Debug.LogWarning("[Polish] Missing weapon config " + asset);
                return;
            }

            SerializedObject so = new SerializedObject(config);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_tint").colorValue = tint;
            so.FindProperty("_muzzleFlashSize").floatValue = flashSize;
            so.FindProperty("_muzzleFlashColor").colorValue = flashColor;
            so.FindProperty("_trailDuration").floatValue = trail;
            so.FindProperty("_impactSize").floatValue = impactSize;
            so.FindProperty("_impactVolume").floatValue = impactVolume;
            so.FindProperty("_isHeavy").boolValue = heavy;
            if (heavy)
            {
                so.FindProperty("_explosionSize").floatValue = explosionSize;
                so.FindProperty("_explosionVolume").floatValue = 1f;
            }

            so.FindProperty("_cameraRecoil").floatValue = recoil;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        public static TankConfig LoadTank(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TankConfig>(ConfigsFolder + "/" + name + ".asset");
        }

        public static void EnsureTankConfigs()
        {
            WeaponConfig rifle = LoadWeapon("RifleConfig");
            WeaponConfig cannon = LoadWeapon("CannonConfig");
            WeaponConfig heavy = LoadWeapon("HeavyConfig");

            CreateTank("PlayerTankConfig", "Player", 150f, 1f, 6.5f, 115f, 0.6f, 14f, new[] { rifle, cannon, heavy },
                0f, 0f, 0f, 0f, 0f, 0f, Vector2.zero);
            CreateTank("EnemyLightConfig", "Light Scout", 90f, 0.7f, 4.5f, 140f, 0.6f, 10f, new[] { rifle },
                22f, 14f, 8f, 1.1f, 160f, 3f, new Vector2(0.5f, 2f));
            CreateTank("EnemyHeavyConfig", "Heavy Brute", 200f, 1f, 2.6f, 80f, 0.6f, 6f, new[] { cannon },
                18f, 11f, 6f, 2.8f, 70f, 4f, new Vector2(1.5f, 3.5f));
        }

        private static void CreateTank(string asset, string displayName, float health, float damage, float move, float turn,
            float reverse, float accel, WeaponConfig[] weapons, float chase, float attack, float angle, float cooldown,
            float turretSpeed, float lose, Vector2 wait)
        {
            string path = ConfigsFolder + "/" + asset + ".asset";
            TankConfig config = AssetDatabase.LoadAssetAtPath<TankConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TankConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            SerializedObject so = new SerializedObject(config);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_maxHealth").floatValue = health;
            so.FindProperty("_damageMultiplier").floatValue = damage;
            so.FindProperty("_moveSpeed").floatValue = move;
            so.FindProperty("_turnSpeed").floatValue = turn;
            so.FindProperty("_reverseSpeedFactor").floatValue = reverse;
            so.FindProperty("_acceleration").floatValue = accel;
            SerializedProperty list = so.FindProperty("_weapons");
            list.arraySize = weapons.Length;
            for (int i = 0; i < weapons.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            }

            if (chase > 0f)
            {
                so.FindProperty("_chaseRange").floatValue = chase;
                so.FindProperty("_attackRange").floatValue = attack;
                so.FindProperty("_shootAngle").floatValue = angle;
                so.FindProperty("_shootCooldown").floatValue = cooldown;
                so.FindProperty("_turretTurnSpeed").floatValue = turretSpeed;
                so.FindProperty("_loseTargetTime").floatValue = lose;
                so.FindProperty("_patrolWait").vector2Value = wait;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        #endregion

        #region Level materials

        public static void EnsureLevelMaterials()
        {
            Texture2D ground = SaveTexture(TexturesFolder + "/Ground.png",
                NoiseTexture(256, new Color(0.30f, 0.27f, 0.22f), new Color(0.45f, 0.41f, 0.33f), 0.035f, 3), false, true);
            Texture2D concrete = SaveTexture(TexturesFolder + "/Concrete.png",
                NoiseTexture(256, new Color(0.62f, 0.61f, 0.58f), new Color(0.82f, 0.81f, 0.78f), 0.08f, 5), false, true);
            Texture2D sand = SaveTexture(TexturesFolder + "/Sandbag.png",
                NoiseTexture(128, new Color(0.62f, 0.53f, 0.35f), new Color(0.8f, 0.71f, 0.5f), 0.15f, 7), false, true);
            Texture2D wood = SaveTexture(TexturesFolder + "/Wood.png",
                WoodTexture(128), false, true);

            SetTexture("ArenaGround", ground, new Vector2(18f, 18f), new Color(1f, 1f, 1f));
            SetTexture("ArenaWall", concrete, new Vector2(12f, 1f), new Color(0.55f, 0.54f, 0.52f));
            SetTexture("Obstacle", concrete, new Vector2(1f, 1f), new Color(0.8f, 0.79f, 0.76f));
            SetTexture("ConcreteCover", concrete, new Vector2(1f, 1f), new Color(0.85f, 0.84f, 0.8f));
            SetTexture("SandbagCover", sand, new Vector2(2f, 2f), new Color(0.9f, 0.85f, 0.75f));

            Material crate = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Crate.mat");
            if (crate == null)
            {
                crate = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(crate, MaterialsFolder + "/Crate.mat");
            }

            crate.SetTexture("_BaseMap", wood);
            crate.SetColor("_BaseColor", new Color(0.85f, 0.7f, 0.5f));
            crate.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(crate);

            CreateLit("RepairKit", new Color(0.2f, 0.75f, 0.3f), new Color(0.1f, 0.6f, 0.2f));
            CreateLit("RepairCross", new Color(0.95f, 0.95f, 0.95f), new Color(0.6f, 0.6f, 0.6f));
            CreateLit("TankHull_EnemyHeavy", new Color(0.24f, 0.25f, 0.3f), Color.black);
        }

        public static Material CreateLit(string name, Color color, Color emission)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }

            m.SetColor("_BaseColor", color);
            if (emission.maxColorComponent > 0.01f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(m);
            return m;
        }

        private static void SetTexture(string material, Texture2D texture, Vector2 tiling, Color tint)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/" + material + ".mat");
            if (m == null) return;
            m.SetTexture("_BaseMap", texture);
            m.SetTextureScale("_BaseMap", tiling);
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Smoothness", 0.12f);
            EditorUtility.SetDirty(m);
        }

        private static float Fbm(float x, float y, float scale, float seed)
        {
            float value = 0f;
            float amp = 0.55f;
            float freq = scale;
            for (int o = 0; o < 4; o++)
            {
                value += Mathf.PerlinNoise(seed + x * freq, seed * 1.7f + y * freq) * amp;
                amp *= 0.5f;
                freq *= 2.1f;
            }

            return value;
        }

        /// <summary>Seamless (tileable) fractal noise texture.</summary>
        public static Texture2D NoiseTexture(int size, Color a, Color b, float scale, int seed)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            float s = 100f + seed * 13.1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = (Fbm(x, y, scale, s) * (size - x) * (size - y)
                               + Fbm(x - size, y, scale, s) * x * (size - y)
                               + Fbm(x - size, y - size, scale, s) * x * y
                               + Fbm(x, y - size, scale, s) * (size - x) * y) / (size * size);
                    n = Mathf.Clamp01((n - 0.25f) * 1.8f);
                    tex.SetPixel(x, y, Color.Lerp(a, b, n));
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D WoodTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float plank = (y % 32) < 2 ? 0.45f : 1f;
                    float grain = 0.75f + 0.25f * Mathf.Sin(x * 0.35f + Mathf.PerlinNoise(x * 0.05f, y * 0.2f) * 6f);
                    float v = plank * grain;
                    tex.SetPixel(x, y, new Color(0.55f * v, 0.38f * v, 0.22f * v));
                }
            }

            tex.Apply();
            return tex;
        }

        #endregion

        #region Volume profiles

        public static void EnsureVolumeProfiles()
        {
            BuildProfile(DayProfilePath, false);
            BuildProfile(SunsetProfilePath, true);
        }

        private static void BuildProfile(string path, bool sunset)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            Tonemapping tone = Get<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = Get<ColorAdjustments>(profile);
            color.postExposure.Override(sunset ? 0.4f : 0.25f);
            color.contrast.Override(sunset ? 18f : 12f);
            color.saturation.Override(sunset ? 12f : 8f);
            color.colorFilter.Override(sunset ? new Color(1f, 0.96f, 0.92f) : Color.white);

            WhiteBalance white = Get<WhiteBalance>(profile);
            white.temperature.Override(sunset ? 8f : 0f);
            white.tint.Override(sunset ? 4f : 0f);

            Bloom bloom = Get<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(sunset ? 0.9f : 0.6f);
            bloom.scatter.Override(0.7f);

            Vignette vignette = Get<Vignette>(profile);
            vignette.intensity.Override(sunset ? 0.3f : 0.2f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(Color.black);

            EditorUtility.SetDirty(profile);
        }

        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
            {
                component.active = true;
                return component;
            }

            component = profile.Add<T>(false);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        #endregion

        #region Prefabs

        public static void UpdatePrefabs()
        {
            UpdatePrefab(PlayerPrefab, LoadTank("PlayerTankConfig"), false);
            UpdatePrefab(EnemyPrefab, LoadTank("EnemyLightConfig"), true);
        }

        private static void UpdatePrefab(string path, TankConfig config, bool enemy)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                TankConfigApplier applier = root.GetComponent<TankConfigApplier>();
                if (applier == null) applier = root.AddComponent<TankConfigApplier>();
                applier.SetConfig(config);

                if (root.GetComponent<EngineLoop>() == null) root.AddComponent<EngineLoop>();

                TankHealth health = root.GetComponent<TankHealth>();
                if (health != null && config != null)
                {
                    SerializedObject so = new SerializedObject(health);
                    so.FindProperty("_maxHealth").floatValue = config.MaxHealth;
                    so.FindProperty("_currentHealth").floatValue = config.MaxHealth;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (!enemy)
                {
                    Rigidbody body = root.GetComponent<Rigidbody>();
                    if (body != null)
                    {
                        body.mass = 8f;
                        body.linearDamping = 0.5f;
                        body.angularDamping = 5f;
                        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                        body.interpolation = RigidbodyInterpolation.Interpolate;
                    }
                }
                else
                {
                    UnityEngine.AI.NavMeshAgent agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.radius = 1f;
                        agent.height = 1.5f;
                        agent.stoppingDistance = 0.3f;
                        agent.autoBraking = true;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        #endregion

        #region Navigation / quality

        public static void ConfigureNavMeshAgentType()
        {
            Object[] objects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
            if (objects.Length == 0) return;
            Object settingsObject = objects[0];
            SerializedObject so = new SerializedObject(settingsObject);
            SerializedProperty settings = so.FindProperty("m_Settings");
            if (settings == null || settings.arraySize == 0) return;
            SerializedProperty agent = settings.GetArrayElementAtIndex(0);
            SetFloat(agent, "agentRadius", 1f);
            SetFloat(agent, "agentHeight", 1.5f);
            SetFloat(agent, "agentClimb", 0.4f);
            SetFloat(agent, "agentSlope", 40f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(SerializedProperty parent, string name, float value)
        {
            SerializedProperty p = parent.FindPropertyRelative(name);
            if (p != null) p.floatValue = value;
        }

        public static void ConfigureQuality()
        {
            UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if (asset == null) return;
            asset.shadowDistance = 90f;
            asset.shadowCascadeCount = 2;
            EditorUtility.SetDirty(asset);
        }

        #endregion
    }
}
