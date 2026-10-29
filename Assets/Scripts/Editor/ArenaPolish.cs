using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tanks.EditorTools
{
    /// <summary>
    /// Scene side of the polish pass: final models for greybox obstacles,
    /// surfaces, second enemy with its own route, repair kits, HUD, camera
    /// post-processing + shake, lighting, atmosphere (day / sunset + fog),
    /// battle decals, NavMesh rebake, build settings and screenshots.
    /// </summary>
    public static class ArenaPolish
    {
        public const string GameScene = "Assets/Scenes/Game.unity";
        public const string RangeScene = "Assets/Scenes/ShootingRange.unity";

        private static readonly Vector3[] HeavyRoute =
        {
            new Vector3(-7f, 0.5f, 9f),
            new Vector3(8f, 0.5f, 1f),
            new Vector3(3f, 0.5f, -5f),
            new Vector3(-9f, 0.5f, -6f)
        };

        [MenuItem("Tools/Tanks/Polish/2. Game Scene")]
        public static void PolishGameScene()
        {
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            ApplyCommon(true);
            SetupSecondEnemy();
            SetupRepairKits();
            ReplaceGreyboxWithModels();
            AssignSurfaces();
            CreateBattleDecals();
            BakeNavMesh();
            SnapRoutesToNavMesh();
            SaveActiveScene();
            Debug.Log("[Polish] Game scene polished.");
        }

        [MenuItem("Tools/Tanks/Polish/3. Shooting Range Scene")]
        public static void PolishRangeScene()
        {
            EditorSceneManager.OpenScene(RangeScene, OpenSceneMode.Single);
            ApplyCommon(false);
            ReplaceGreyboxWithModels();
            AssignSurfaces();
            BakeNavMesh();
            SaveActiveScene();
            Debug.Log("[Polish] Shooting range polished.");
        }

        [MenuItem("Tools/Tanks/Polish/4. Build Settings")]
        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameScene, true),
                new EditorBuildSettingsScene(RangeScene, true)
            };
        }

        /// <summary>Called from ArenaBuilder after a full rebuild.</summary>
        public static void ApplyAfterBuild(bool arena)
        {
            ApplyCommon(arena);
            if (arena)
            {
                SetupSecondEnemy();
                SetupRepairKits();
            }

            ReplaceGreyboxWithModels();
            AssignSurfaces();
            if (arena) CreateBattleDecals();
            BakeNavMesh();
            if (arena) SnapRoutesToNavMesh();
        }

        private static void SaveActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        #region Common

        private static void ApplyCommon(bool arena)
        {
            SetupCamera();
            SetupHud();
            SetupLightingAndAtmosphere();
        }

        private static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
            cam.farClipPlane = 200f;
        }

        private static void SetupHud()
        {
            PlayerHealthUI hud = Object.FindAnyObjectByType<PlayerHealthUI>();
            if (hud != null)
            {
                CanvasGroup group = hud.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 1f;
                hud.BuildLayout();
                EditorUtility.SetDirty(hud);
            }

            GameObject label = GameObject.Find("StatusLabel");
            TankMover mover = Object.FindAnyObjectByType<TankMover>();
            if (label != null)
            {
                Text text = label.GetComponent<Text>();
                text.text = "Танк зупинився";
                Outline outline = label.GetComponent<Outline>();
                if (outline == null) outline = label.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
                EditorUtility.SetDirty(text);

                CanvasScaler scaler = label.GetComponentInParent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                if (mover != null)
                {
                    SerializedObject so = new SerializedObject(mover);
                    so.FindProperty("_statusLabel").objectReferenceValue = text;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void SetupLightingAndAtmosphere()
        {
            Light sun = null;
            foreach (Light l in Object.FindObjectsByType<Light>())
            {
                if (l.type == LightType.Directional) { sun = l; break; }
            }

            if (sun != null)
            {
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.8f;
                sun.shadowResolution = LightShadowResolution.High;
                sun.shadowBias = 0.05f;
                sun.shadowNormalBias = 0.4f;
            }

            Volume volume = null;
            foreach (Volume v in Object.FindObjectsByType<Volume>())
            {
                if (v.isGlobal) { volume = v; break; }
            }

            if (volume == null)
            {
                volume = new GameObject("Global Volume").AddComponent<Volume>();
                volume.isGlobal = true;
            }

            GameObject atmosphereObject = GameObject.Find("Atmosphere");
            if (atmosphereObject == null) atmosphereObject = new GameObject("Atmosphere");
            AtmosphereController atmosphere = atmosphereObject.GetComponent<AtmosphereController>();
            if (atmosphere == null) atmosphere = atmosphereObject.AddComponent<AtmosphereController>();

            SerializedObject so = new SerializedObject(atmosphere);
            so.FindProperty("_sun").objectReferenceValue = sun;
            so.FindProperty("_volume").objectReferenceValue = volume;
            so.FindProperty("_dayProfile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ArenaPolishAssets.DayProfilePath);
            so.FindProperty("_sunsetProfile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ArenaPolishAssets.SunsetProfilePath);
            so.ApplyModifiedPropertiesWithoutUndo();
            atmosphere.Apply(AtmosphereController.Preset.Day);
        }

        #endregion

        #region Enemies, pickups

        private static void SetupSecondEnemy()
        {
            GameObject light = GameObject.Find("EnemyTank");
            if (light != null)
            {
                TankConfigApplier applier = light.GetComponent<TankConfigApplier>();
                if (applier != null)
                {
                    applier.SetConfig(ArenaPolishAssets.LoadTank("EnemyLightConfig"));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(applier);
                }
            }

            GameObject routeRoot = GameObject.Find("AI_Route_2");
            if (routeRoot == null) routeRoot = new GameObject("AI_Route_2");
            for (int i = routeRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(routeRoot.transform.GetChild(i).gameObject);
            }

            Transform[] waypoints = new Transform[HeavyRoute.Length];
            for (int i = 0; i < HeavyRoute.Length; i++)
            {
                GameObject wp = new GameObject("Waypoint_" + (i + 1));
                wp.transform.SetParent(routeRoot.transform, false);
                wp.transform.position = HeavyRoute[i];
                waypoints[i] = wp.transform;
            }

            GameObject heavy = GameObject.Find("EnemyTank_Heavy");
            if (heavy == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPolishAssets.EnemyPrefab);
                heavy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                heavy.name = "EnemyTank_Heavy";
            }

            heavy.tag = "Enemy";
            heavy.transform.SetPositionAndRotation(new Vector3(10f, 0.5f, 2f), Quaternion.Euler(0f, -90f, 0f));

            TankConfigApplier heavyApplier = heavy.GetComponent<TankConfigApplier>();
            heavyApplier.SetConfig(ArenaPolishAssets.LoadTank("EnemyHeavyConfig"));
            PrefabUtility.RecordPrefabInstancePropertyModifications(heavyApplier);

            AIController ai = heavy.GetComponent<AIController>();
            ai.SetWaypoints(waypoints);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ai);

            Material heavyHull = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/TankHull_EnemyHeavy.mat");
            Material enemyHull = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/TankHull_Enemy.mat");
            foreach (MeshRenderer r in heavy.GetComponentsInChildren<MeshRenderer>())
            {
                if (r.sharedMaterial == enemyHull)
                {
                    r.sharedMaterial = heavyHull;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
            }

            heavy.transform.localScale = Vector3.one * 1.1f;
        }

        private static void SetupRepairKits()
        {
            GameObject root = GameObject.Find("RepairKits");
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject("RepairKits");

            CreateRepairKit(root.transform, "RepairKit_West", new Vector3(-21f, 0.5f, 0f));
            CreateRepairKit(root.transform, "RepairKit_East", new Vector3(21f, 0.5f, -12f));
        }

        private static void CreateRepairKit(Transform parent, string name, Vector3 position)
        {
            Material kit = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/RepairKit.mat");
            Material cross = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/RepairCross.mat");

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.layer = 2;
            SphereCollider trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.3f;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Box(visual.transform, "Case", Vector3.zero, new Vector3(0.9f, 0.6f, 0.9f), kit);
            Box(visual.transform, "CrossA", new Vector3(0f, 0.31f, 0f), new Vector3(0.6f, 0.04f, 0.18f), cross);
            Box(visual.transform, "CrossB", new Vector3(0f, 0.31f, 0f), new Vector3(0.18f, 0.04f, 0.6f), cross);
            foreach (Transform t in visual.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;

            RepairPickup pickup = go.AddComponent<RepairPickup>();
            SerializedObject so = new SerializedObject(pickup);
            so.FindProperty("_visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Box(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, bool collider = false)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        #endregion

        #region Final models

        /// <summary>
        /// Swaps the plain greybox cubes for beveled concrete blocks, a sandbag
        /// wall, wooden crates and jersey barriers, keeping the same footprint.
        /// </summary>
        private static void ReplaceGreyboxWithModels()
        {
            GameObject obstacles = GameObject.Find("Obstacles");
            if (obstacles == null) return;

            Material concrete = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/ConcreteCover.mat");
            Material obstacle = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/Obstacle.mat");
            Material sandbag = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/SandbagCover.mat");
            Material crate = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/Crate.mat");

            foreach (Transform child in obstacles.transform)
            {
                if (child.Find("Model") != null) continue; // already converted
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                MeshFilter filter = child.GetComponent<MeshFilter>();
                if (renderer == null || filter == null) continue;

                Vector3 size = child.localScale;
                string n = child.name;

                // Keep the object (and its BoxCollider) but hide the greybox mesh.
                renderer.enabled = false;
                GameObject model = new GameObject("Model");
                model.transform.SetParent(child, false);
                // Undo parent scale so the model is authored in metres.
                model.transform.localScale = new Vector3(1f / size.x, 1f / size.y, 1f / size.z);

                if (n.Contains("Crate"))
                {
                    BuildCrates(model.transform, size, crate);
                }
                else if (n.Contains("LowWall"))
                {
                    BuildSandbagWall(model.transform, size, sandbag);
                }
                else if (n.Contains("LongBar"))
                {
                    BuildBarriers(model.transform, size, concrete);
                }
                else
                {
                    Mesh mesh = SaveMesh(ConcreteBlockGenerator.Generate(size.x, size.y, size.z, 0.08f),
                        "Concrete_" + n);
                    AddMesh(model.transform, "Block", mesh, n.Contains("Ramp") ? obstacle : concrete, Vector3.zero);
                }
            }
        }

        private static void BuildCrates(Transform parent, Vector3 size, Material material)
        {
            float c = Mathf.Min(size.x, size.z) * 0.5f;
            Vector3[] offsets =
            {
                new Vector3(-c * 0.5f, -size.y * 0.5f + c * 0.5f, -c * 0.5f),
                new Vector3(c * 0.5f, -size.y * 0.5f + c * 0.5f, -c * 0.5f),
                new Vector3(-c * 0.5f, -size.y * 0.5f + c * 0.5f, c * 0.5f),
                new Vector3(c * 0.5f, -size.y * 0.5f + c * 0.5f, c * 0.5f),
                new Vector3(0f, -size.y * 0.5f + c * 1.5f, 0f)
            };

            Mesh crateMesh = SaveMesh(ConcreteBlockGenerator.Generate(c * 0.96f, c * 0.96f, c * 0.96f, 0.03f), "Crate");
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject box = AddMesh(parent, "Crate_" + (i + 1), crateMesh, material, offsets[i]);
                box.transform.localRotation = Quaternion.Euler(0f, i * 17f, 0f);
            }
        }

        private static void BuildSandbagWall(Transform parent, Vector3 size, Material material)
        {
            int columns = Mathf.Max(3, Mathf.RoundToInt(size.x / 0.75f));
            int rows = Mathf.Max(2, Mathf.RoundToInt(size.y / 0.3f));
            Mesh mesh = SaveMesh(SandbagStackGenerator.GenerateWall(columns, rows, 0.12f), "SandbagWall_" + columns + "x" + rows);
            GameObject wall = AddMesh(parent, "Sandbags", mesh, material, Vector3.zero);
            Bounds b = mesh.bounds;
            Vector3 scale = new Vector3(size.x / Mathf.Max(0.01f, b.size.x), size.y / Mathf.Max(0.01f, b.size.y), size.z / Mathf.Max(0.01f, b.size.z));
            wall.transform.localScale = scale;
            wall.transform.localPosition = -Vector3.Scale(b.center, scale);
        }

        private static void BuildBarriers(Transform parent, Vector3 size, Material material)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(size.x / 2f));
            float segment = size.x / count;
            Mesh mesh = SaveMesh(ConcreteBlockGenerator.Generate(segment * 0.94f, size.y, size.z, 0.1f), "Barrier_" + count);
            for (int i = 0; i < count; i++)
            {
                float x = -size.x * 0.5f + segment * (i + 0.5f);
                GameObject b = AddMesh(parent, "Barrier_" + (i + 1), mesh, material, new Vector3(x, 0f, 0f));
                b.transform.localRotation = Quaternion.Euler(0f, Random.Range(-3f, 3f), 0f);
            }
        }

        private static GameObject AddMesh(Transform parent, string name, Mesh mesh, Material material, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            go.layer = parent.gameObject.layer;
            return go;
        }

        private static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = "Assets/Game/Meshes/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        #endregion

        #region Surfaces, decals

        private static void AssignSurfaces()
        {
            SetSurface(GameObject.Find("Ground"), SurfaceType.Dirt);
            SetChildrenSurface("Walls", SurfaceType.Concrete);
            SetChildrenSurface("Obstacles", SurfaceType.Concrete);

            GameObject obstacles = GameObject.Find("Obstacles");
            if (obstacles != null)
            {
                foreach (Transform t in obstacles.transform)
                {
                    if (t.name.Contains("Crate")) SetSurface(t.gameObject, SurfaceType.Wood);
                    if (t.name.Contains("LowWall")) SetSurface(t.gameObject, SurfaceType.Dirt);
                }
            }

            GameObject covers = GameObject.Find("Covers");
            if (covers != null)
            {
                foreach (Transform t in covers.transform)
                {
                    SetSurface(t.gameObject, t.name.Contains("Sandbag") ? SurfaceType.Dirt : SurfaceType.Concrete);
                }
            }
        }

        private static void SetChildrenSurface(string rootName, SurfaceType surface)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null) return;
            foreach (Transform t in root.transform) SetSurface(t.gameObject, surface);
        }

        private static void SetSurface(GameObject go, SurfaceType surface)
        {
            if (go == null) return;
            ImpactSurface s = go.GetComponent<ImpactSurface>();
            if (s == null) s = go.AddComponent<ImpactSurface>();
            s.Surface = surface;
            EditorUtility.SetDirty(s);
        }

        private static void CreateBattleDecals()
        {
            Material decal = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.ResourcesVfx + "/ScorchDecal.mat");
            if (decal == null) return;

            GameObject root = GameObject.Find("BattleDecals");
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject("BattleDecals");

            Vector3[] spots =
            {
                new Vector3(-5f, 0f, -12f), new Vector3(9f, 0f, 4f), new Vector3(-15f, 0f, 2f),
                new Vector3(3f, 0f, 9f), new Vector3(16f, 0f, -14f), new Vector3(-11f, 0f, 14f),
                new Vector3(0f, 0f, -3f)
            };

            Random.InitState(42);
            for (int i = 0; i < spots.Length; i++)
            {
                GameObject go = new GameObject("Scorch_" + (i + 1));
                go.transform.SetParent(root.transform, false);
                go.transform.SetPositionAndRotation(spots[i] + Vector3.up * 0.4f, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
                DecalProjector projector = go.AddComponent<DecalProjector>();
                projector.material = decal;
                float s = Random.Range(2f, 4f);
                projector.size = new Vector3(s, s, 1.2f);
                projector.pivot = Vector3.zero;
                projector.fadeFactor = 0.85f;
            }
        }

        #endregion

        #region NavMesh

        private static void BakeNavMesh()
        {
            NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                surface = new GameObject("NavMeshSurface").AddComponent<NavMeshSurface>();
            }

            int mask = Physics.DefaultRaycastLayers;
            foreach (string layer in new[] { "Enemy", "Player", "Projectile", "UI" })
            {
                int index = LayerMask.NameToLayer(layer);
                if (index >= 0) mask &= ~(1 << index);
            }

            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = mask;
            surface.BuildNavMesh();
            PersistNavMesh(surface);
            EditorUtility.SetDirty(surface);
        }

        /// <summary>Saves the baked NavMeshData as an asset next to the scene, so it survives reloads.</summary>
        private static void PersistNavMesh(NavMeshSurface surface)
        {
            if (surface.navMeshData == null) return;
            Scene scene = surface.gameObject.scene;
            if (string.IsNullOrEmpty(scene.path)) return;

            string folder = Path.Combine(Path.GetDirectoryName(scene.path), scene.name).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(scene.path).Replace('\\', '/'), scene.name);
            }

            string path = folder + "/NavMesh-" + surface.name + ".asset";
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(surface.navMeshData))) return;

            AssetDatabase.DeleteAsset(path);
            NavMeshData data = surface.navMeshData;
            AssetDatabase.CreateAsset(data, path);
            surface.navMeshData = data;
            AssetDatabase.SaveAssets();
        }

        private static void SnapRoutesToNavMesh()
        {
            foreach (string name in new[] { "AI_Route", "AI_Route_2" })
            {
                GameObject route = GameObject.Find(name);
                if (route == null) continue;
                foreach (Transform wp in route.transform)
                {
                    if (NavMesh.SamplePosition(wp.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                    {
                        wp.position = hit.position;
                    }
                    else
                    {
                        Debug.LogWarning("[Polish] Waypoint off NavMesh: " + name + "/" + wp.name);
                    }
                }
            }
        }

        #endregion

        #region Screenshots

        [MenuItem("Tools/Tanks/Polish/5. Before-After Screenshots")]
        public static void TakeScreenshots()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
            Directory.CreateDirectory(folder);

            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            AtmosphereController atmosphere = Object.FindAnyObjectByType<AtmosphereController>();
            bool hadPost = data != null && data.renderPostProcessing;
            bool fog = RenderSettings.fog;

            if (atmosphere != null) atmosphere.Apply(AtmosphereController.Preset.Day);
            if (data != null) data.renderPostProcessing = false;
            RenderSettings.fog = false;
            Capture(cam, Path.Combine(folder, "1_before_postprocessing.png"));

            if (data != null) data.renderPostProcessing = true;
            RenderSettings.fog = true;
            Capture(cam, Path.Combine(folder, "2_after_postprocessing_day.png"));

            if (atmosphere != null)
            {
                atmosphere.Apply(AtmosphereController.Preset.Sunset);
                Capture(cam, Path.Combine(folder, "3_after_postprocessing_sunset.png"));
                atmosphere.Apply(AtmosphereController.Preset.Day);
            }

            if (data != null) data.renderPostProcessing = hadPost;
            RenderSettings.fog = fog || atmosphere != null;
            Debug.Log("[Polish] Screenshots saved to " + folder);
        }

        private static void Capture(Camera cam, string path)
        {
            const int width = 1600;
            const int height = 900;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = cam.targetTexture;
            cam.targetTexture = rt;
            // First render warms up the pipeline / volume stack after a settings change.
            cam.Render();
            cam.Render();
            cam.targetTexture = previous;

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        #endregion
    }
}
