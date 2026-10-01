using Tanks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tanks.EditorTools
{
    public static class ArenaBuilder
    {
        private const string RootFolder = "Assets/Game";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string ConfigsFolder = RootFolder + "/Configs";
        private const string ScenesFolder = "Assets/Scenes";
        private const string ScenePath = ScenesFolder + "/Game.unity";
        private const string RangeScenePath = ScenesFolder + "/ShootingRange.unity";
        private const string PrefabPath = PrefabsFolder + "/PlayerTank.prefab";

        private static readonly Vector3 TankSpawn = new Vector3(0f, 0.5f, -8f);

        [MenuItem("Tools/Tanks/Build Arena")]
        public static void BuildArena()
        {
            EnsureFolders();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material groundMaterial = CreateMaterial("ArenaGround", new Color(0.36f, 0.34f, 0.31f));
            Material obstacleMaterial = CreateMaterial("Obstacle", new Color(0.55f, 0.53f, 0.49f));
            Material wallMaterial = CreateMaterial("ArenaWall", new Color(0.28f, 0.27f, 0.26f));
            Material hullMaterial = CreateMaterial("TankHull", new Color(0.20f, 0.36f, 0.22f));
            Material trackMaterial = CreateMaterial("TankTrack", new Color(0.13f, 0.13f, 0.14f));
            Material barrelMaterial = CreateMaterial("TankBarrel", new Color(0.35f, 0.36f, 0.38f));

            WeaponConfig[] weaponConfigs = CreateWeaponConfigs();
            GameObject tankPrefab = BuildTankPrefab(hullMaterial, trackMaterial, barrelMaterial);
            CreateInstance(tankPrefab, TankSpawn, Quaternion.identity, "PlayerTank");

            CreateGround(groundMaterial);
            CreateWalls(wallMaterial);
            CreateObstacles(obstacleMaterial);
            CreateLighting();
            CreateCamera();
            CreateStatusLabel();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Arena built and saved to " + ScenePath + ". PlayerTank prefab: " + PrefabPath);
        }

        [MenuItem("Tools/Tanks/Build Shooting Range")]
        public static void BuildShootingRange()
        {
            EnsureFolders();

            WeaponConfig[] weaponConfigs = CreateWeaponConfigs();

            Material groundMaterial = CreateMaterial("ArenaGround", new Color(0.36f, 0.34f, 0.31f));
            Material obstacleMaterial = CreateMaterial("Obstacle", new Color(0.55f, 0.53f, 0.49f));
            Material wallMaterial = CreateMaterial("ArenaWall", new Color(0.28f, 0.27f, 0.26f));
            Material hullMaterial = CreateMaterial("TankHull", new Color(0.20f, 0.36f, 0.22f));
            Material trackMaterial = CreateMaterial("TankTrack", new Color(0.13f, 0.13f, 0.14f));
            Material barrelMaterial = CreateMaterial("TankBarrel", new Color(0.35f, 0.36f, 0.38f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject tankPrefab = BuildTankPrefab(hullMaterial, trackMaterial, barrelMaterial);
            CreateInstance(tankPrefab, TankSpawn, Quaternion.identity, "PlayerTank");

            CreateGround(groundMaterial);
            CreateWalls(wallMaterial);
            CreateObstacles(obstacleMaterial);
            CreateLighting();
            CreateCamera();
            CreateStatusLabel();
            CreateTargets();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RangeScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Shooting range built and saved to " + RangeScenePath + ". Weapon configs: " + weaponConfigs.Length);
        }

        private static void CreateTargets()
        {
            Material targetMaterial = CreateMaterial("Target", new Color(0.85f, 0.20f, 0.20f));
            Material postMaterial = CreateMaterial("TargetPost", new Color(0.30f, 0.30f, 0.32f));

            GameObject targets = new GameObject("Targets");

            Vector3[] positions =
            {
                new Vector3(-10f, 1.2f, 6f),
                new Vector3(-4f, 1.2f, 11f),
                new Vector3(3f, 1.2f, 14f),
                new Vector3(10f, 1.2f, 9f),
                new Vector3(7f, 1.2f, 2f),
                new Vector3(-8f, 1.2f, 16f)
            };

            float[] health = { 60f, 100f, 140f, 80f, 120f, 100f };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateTarget(targets.transform, "Target" + (i + 1), positions[i], targetMaterial, postMaterial, health[i]);
            }
        }

        private static void CreateTarget(Transform parent, string name, Vector3 position, Material panel, Material post, float health)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            GameObject plate = CreateBox(root.transform, "Plate", new Vector3(0f, 0f, 0f), new Vector3(2.2f, 2.2f, 0.2f), panel, true);
            CreateBox(root.transform, "Post", new Vector3(0f, -position.y, 0f), new Vector3(0.18f, position.y * 2f, 0.18f), post, true);

            Target target = root.AddComponent<Target>();
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty("_maxHealth").floatValue = health;
            serialized.FindProperty("_renderers").arraySize = 1;
            serialized.FindProperty("_renderers").GetArrayElementAtIndex(0).objectReferenceValue = plate.GetComponent<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Game");
            CreateFolderIfMissing(RootFolder, "Materials");
            CreateFolderIfMissing(RootFolder, "Prefabs");
            CreateFolderIfMissing(RootFolder, "Configs");
            CreateFolderIfMissing("Assets", "Scenes");
        }

        private static WeaponConfig[] CreateWeaponConfigs()
        {
            WeaponConfig rifle = CreateWeaponConfig("RifleConfig", "Rifle", PrimitiveType.Sphere, new Color(0.95f, 0.85f, 0.25f), 10f, 0.35f, 32f, 2.5f, 0, 1f);
            WeaponConfig cannon = CreateWeaponConfig("CannonConfig", "Cannon", PrimitiveType.Cube, new Color(0.90f, 0.45f, 0.15f), 30f, 1.1f, 22f, 4f, 6, 1.8f);
            WeaponConfig heavy = CreateWeaponConfig("HeavyConfig", "Heavy Gun", PrimitiveType.Capsule, new Color(0.75f, 0.20f, 0.85f), 65f, 1.8f, 18f, 5f, 3, 2.8f);

            return new[] { rifle, cannon, heavy };
        }

        private static WeaponConfig CreateWeaponConfig(string assetName, string displayName, PrimitiveType shape, Color tint, float damage, float cooldown, float speed, float lifetime, int maxActive, float scale)
        {
            string prefabPath = PrefabsFolder + "/Projectile_" + assetName.Replace("Config", string.Empty) + ".prefab";
            GameObject prefab = CreateProjectilePrefab(prefabPath, shape);

            string path = ConfigsFolder + "/" + assetName + ".asset";
            WeaponConfig config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);

            bool created = config == null;
            if (created)
            {
                config = ScriptableObject.CreateInstance<WeaponConfig>();
            }

            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_projectilePrefab").objectReferenceValue = prefab;
            serialized.FindProperty("_projectileScale").floatValue = scale;
            serialized.FindProperty("_projectileSpeed").floatValue = speed;
            serialized.FindProperty("_tint").colorValue = tint;
            serialized.FindProperty("_damage").floatValue = damage;
            serialized.FindProperty("_cooldown").floatValue = cooldown;
            serialized.FindProperty("_projectileLifetime").floatValue = lifetime;
            serialized.FindProperty("_maxActiveProjectiles").intValue = maxActive;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (created)
            {
                AssetDatabase.CreateAsset(config, path);
            }
            else
            {
                EditorUtility.SetDirty(config);
            }

            return config;
        }

        private static GameObject CreateProjectilePrefab(string path, PrimitiveType shape)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject projectile = GameObject.CreatePrimitive(shape);
            projectile.name = "Projectile";

            SphereCollider sphere = projectile.AddComponent<SphereCollider>();
            sphere.isTrigger = true;

            projectile.AddComponent<Projectile>();

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = Color.white;
            string materialPath = MaterialsFolder + "/Projectile.mat";
            Material stored = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (stored == null)
            {
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                Object.DestroyImmediate(material);
            }

            AssetDatabase.SaveAssets();
            projectile.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, path);
            Object.DestroyImmediate(projectile);

            return prefab;
        }

        private static void AssignWeapons(WeaponController controller, Transform muzzle)
        {
            CreateWeaponConfigs();

            string[] assetNames = { "RifleConfig", "CannonConfig", "HeavyConfig" };

            SerializedObject serialized = new SerializedObject(controller);

            SerializedProperty weapons = serialized.FindProperty("_weapons");
            weapons.arraySize = assetNames.Length;
            for (int i = 0; i < assetNames.Length; i++)
            {
                weapons.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<WeaponConfig>(ConfigsFolder + "/" + assetNames[i] + ".asset");
            }

            serialized.FindProperty("_muzzle").objectReferenceValue = muzzle;
            serialized.FindProperty("_startIndex").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildTankPrefab(Material hull, Material track, Material barrel)
        {
            GameObject root = new GameObject("PlayerTank");
            root.layer = LayerMask.NameToLayer("Player");

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 8f;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CreateBox(root.transform, "Hull", new Vector3(0f, 0f, 0f), new Vector3(1.4f, 0.55f, 2f), hull, true);

            CreateBox(root.transform, "TrackLeft", new Vector3(-0.82f, -0.18f, 0f), new Vector3(0.35f, 0.5f, 2.1f), track, true);
            CreateBox(root.transform, "TrackRight", new Vector3(0.82f, -0.18f, 0f), new Vector3(0.35f, 0.5f, 2.1f), track, true);

            GameObject turret = new GameObject("Turret");
            turret.transform.SetParent(root.transform, false);
            turret.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            CreateBox(turret.transform, "TurretBody", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 0.4f, 1.1f), hull, true);
            CreateBox(turret.transform, "Barrel", new Vector3(0f, 0.08f, 1.1f), new Vector3(0.16f, 0.16f, 1.8f), barrel, true);

            CreateBox(turret.transform, "Muzzle", new Vector3(0f, 0.08f, 2.05f), new Vector3(0.28f, 0.28f, 0.2f), barrel, true);

            root.AddComponent<TankInputSource>();
            root.AddComponent<TankMover>();

            turret.AddComponent<TankTurret>();

            Transform muzzle = turret.transform.Find("Muzzle");
            WeaponController weapons = turret.AddComponent<WeaponController>();
            AssignWeapons(weapons, muzzle);

            string path = PrefabsFolder + "/PlayerTank.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, bool collider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;

            box.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (!collider)
            {
                Object.DestroyImmediate(box.GetComponent<Collider>());
            }

            return box;
        }

        private static GameObject CreateInstance(GameObject prefab, Vector3 position, Quaternion rotation, string name)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(prefab);
            }

            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static void CreateGround(Material material)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);

            ground.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateWalls(Material material)
        {
            const float half = 30f;
            const float thickness = 1f;
            const float height = 4f;

            GameObject walls = new GameObject("Walls");

            CreateWall(walls.transform, "WallNorth", new Vector3(0f, height * 0.5f, half), new Vector3(half * 2f + thickness, height, thickness), material);
            CreateWall(walls.transform, "WallSouth", new Vector3(0f, height * 0.5f, -half), new Vector3(half * 2f + thickness, height, thickness), material);
            CreateWall(walls.transform, "WallEast", new Vector3(half, height * 0.5f, 0f), new Vector3(thickness, height, half * 2f), material);
            CreateWall(walls.transform, "WallWest", new Vector3(-half, height * 0.5f, 0f), new Vector3(thickness, height, half * 2f), material);
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject wall = CreateBox(parent, name, position, scale, material, true);
            wall.isStatic = true;
        }

        private static void CreateObstacles(Material material)
        {
            GameObject obstacles = new GameObject("Obstacles");

            CreateBox(obstacles.transform, "ObstacleBlockA", new Vector3(-10f, 1f, 4f), new Vector3(4f, 2f, 2f), material, true).transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            CreateBox(obstacles.transform, "ObstacleBlockB", new Vector3(8f, 1.5f, -6f), new Vector3(3f, 3f, 6f), material, true).transform.rotation = Quaternion.Euler(0f, -15f, 0f);
            CreateBox(obstacles.transform, "ObstacleRamp", new Vector3(0f, 0.25f, 12f), new Vector3(5f, 0.5f, 3f), material, true).transform.rotation = Quaternion.Euler(-8f, 0f, 0f);
            CreateBox(obstacles.transform, "ObstaclePillarA", new Vector3(-4f, 2f, -2f), new Vector3(1.5f, 4f, 1.5f), material, true);
            CreateBox(obstacles.transform, "ObstaclePillarB", new Vector3(5f, 2f, 8f), new Vector3(1.5f, 4f, 1.5f), material, true);
            CreateBox(obstacles.transform, "ObstacleLowWall", new Vector3(-14f, 0.75f, -10f), new Vector3(6f, 1.5f, 1f), material, true).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            CreateBox(obstacles.transform, "ObstacleCrateCluster", new Vector3(12f, 0.75f, 12f), new Vector3(2f, 1.5f, 2f), material, true);
            CreateBox(obstacles.transform, "ObstacleLongBar", new Vector3(2f, 0.5f, -14f), new Vector3(12f, 1f, 1f), material, true).transform.rotation = Quaternion.Euler(0f, 30f, 0f);
        }

        private static void CreateLighting()
        {
            GameObject light = new GameObject("Directional Light");
            Light directional = light.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.1f;
            directional.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.43f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.18f);
        }

        private static void CreateCamera()
        {
            GameObject camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";

            Camera cam = camera.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;

            camera.AddComponent<AudioListener>();
            camera.transform.position = new Vector3(0f, 26f, -26f);
            camera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        }

        private static void CreateStatusLabel()
        {
            GameObject canvasObject = new GameObject("HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();

            GameObject labelObject = new GameObject("StatusLabel");
            labelObject.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = labelObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(420f, 56f);

            Text label = labelObject.AddComponent<Text>();
            label.font = LoadUiFont();
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.text = "Танк зупинився";

            TankMover mover = Object.FindAnyObjectByType<TankMover>();
            if (mover != null)
            {
                SerializedObject serialized = new SerializedObject(mover);
                serialized.FindProperty("_statusLabel").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Font LoadUiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}