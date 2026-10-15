using System.Collections.Generic;
using Tanks;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tanks.EditorTools
{
    /// <summary>
    /// Procedurally builds the Tanks arena: detailed tank prefabs for the player
    /// and one enemy, greybox geometry, two cover types, a perimeter AI patrol
    /// route, player HUD, a global URP post-processing volume, lighting,
    /// and a baked NavMeshSurface.
    /// </summary>
    public static class ArenaBuilder
    {
        private const string RootFolder = "Assets/Game";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string ConfigsFolder = RootFolder + "/Configs";
        private const string MeshesFolder = RootFolder + "/Meshes";
        private const string PostProcessFolder = RootFolder + "/PostProcessing";
        private const string ScenesFolder = "Assets/Scenes";

        private const string ScenePath = ScenesFolder + "/Game.unity";
        private const string RangeScenePath = ScenesFolder + "/ShootingRange.unity";
        private const string PlayerPrefabPath = PrefabsFolder + "/PlayerTank.prefab";
        private const string EnemyPrefabPath = PrefabsFolder + "/EnemyTank.prefab";
        private const string GlobalVolumeProfilePath = PostProcessFolder + "/GlobalVolumeProfile.asset";

        private static readonly Vector3 PlayerSpawn = new Vector3(0f, 0.5f, -8f);
        private static readonly Vector3 EnemySpawn = new Vector3(-6f, 0.5f, 0f);

        /// <summary>
        /// Perimeter patrol loop. The map is extended with covers, so the AI route
        /// is a fresh outer loop that never crosses an obstacle, cover or wall.
        /// </summary>
        private static readonly Vector3[] AiPatrolRoute =
        {
            new Vector3(0f, 0.5f, -21f),
            new Vector3(-23f, 0.5f, -17f),
            new Vector3(-23f, 0.5f, 17f),
            new Vector3(23f, 0.5f, 17f),
            new Vector3(23f, 0.5f, -17f)
        };

        /// <summary>
        /// Distance from the tank pivot (hull centre) down to the track belt
        /// bottom, used as the NavMeshAgent baseOffset so the vehicle stands on
        /// the navmesh instead of hovering above it.
        /// </summary>
        private const float TrackBottomOffset = 0.41f;

        /// <summary>
        /// Layers the NavMesh is baked from: everything except the tank layers,
        /// so a tank never becomes part of the walkable surface.
        /// </summary>
        private static readonly int LevelNavigationMask = BuildLevelNavigationMask();

        private static int BuildLevelNavigationMask()
        {
            int mask = ~0;
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                mask &= ~(1 << enemyLayer);
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                mask &= ~(1 << playerLayer);
            }

            int projectileLayer = LayerMask.NameToLayer("Projectile");
            if (projectileLayer >= 0)
            {
                mask &= ~(1 << projectileLayer);
            }

            return mask;
        }

        [MenuItem("Tools/Tanks/Build Arena")]
        public static void BuildArena()
        {
            EnsureFolders();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MaterialSet set = CreateMaterials();
            CreateWeaponConfigs(set.Bullet);

            GameObject playerPrefab = BuildTankPrefab(set, false);
            CreateInstance(playerPrefab, PlayerSpawn, Quaternion.identity, "PlayerTank");

            GameObject enemyPrefab = BuildTankPrefab(set, true);
            GameObject enemy = CreateInstance(enemyPrefab, EnemySpawn, Quaternion.identity, "EnemyTank");
            enemy.tag = "Enemy";

            CreateGround(set.Ground);
            CreateWalls(set.Wall);
            CreateObstacles(set.Obstacle);
            CreateCovers(set);
            CreatePlayerHealthHud();
            CreateGlobalVolume();
            CreateLighting();
            CreateCamera();
            CreateStatusLabel();

            Transform[] waypoints = CreateAiPatrolRoute();
            AssignAiRoute(enemy, waypoints);
            BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Arena rebuilt: " + ScenePath + " | enemy prefab: " + EnemyPrefabPath);
        }

        [MenuItem("Tools/Tanks/Build Shooting Range")]
        public static void BuildShootingRange()
        {
            EnsureFolders();

            MaterialSet set = CreateMaterials();
            CreateWeaponConfigs(set.Bullet);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject playerPrefab = BuildTankPrefab(set, false);
            CreateInstance(playerPrefab, PlayerSpawn, Quaternion.identity, "PlayerTank");

            CreateGround(set.Ground);
            CreateWalls(set.Wall);
            CreateObstacles(set.Obstacle);
            CreateGlobalVolume();
            CreateLighting();
            CreateCamera();
            CreateStatusLabel();
            CreatePlayerHealthHud();
            CreateTargets(set);
            BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RangeScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Shooting range rebuilt: " + RangeScenePath);
        }

        private struct MaterialSet
        {
            public Material Ground;
            public Material Obstacle;
            public Material Wall;
            public Material Hull;
            public Material Track;
            public Material Barrel;
            public Material Detail;
            public Material EnemyHull;
            public Material EnemyTrack;
            public Material EnemyBarrel;
            public Material EnemyDetail;
            public Material Concrete;
            public Material Sandbag;
            public Material Bullet;
        }

        private static MaterialSet CreateMaterials()
        {
            MaterialSet set = new MaterialSet();
            set.Ground = CreateMaterial("ArenaGround", new Color(0.36f, 0.34f, 0.31f));
            set.Obstacle = CreateMaterial("Obstacle", new Color(0.55f, 0.53f, 0.49f));
            set.Wall = CreateMaterial("ArenaWall", new Color(0.28f, 0.27f, 0.26f));
            set.Hull = CreateMaterial("TankHull", new Color(0.20f, 0.36f, 0.22f));
            set.Track = CreateMaterial("TankTrack", new Color(0.13f, 0.13f, 0.14f));
            set.Barrel = CreateMaterial("TankBarrel", new Color(0.35f, 0.36f, 0.38f));
            set.Detail = CreateMaterial("TankDetail", new Color(0.42f, 0.43f, 0.45f));
            set.EnemyHull = CreateMaterial("TankHull_Enemy", new Color(0.36f, 0.18f, 0.16f));
            set.EnemyTrack = CreateMaterial("TankTrack_Enemy", new Color(0.11f, 0.10f, 0.10f));
            set.EnemyBarrel = CreateMaterial("TankBarrel_Enemy", new Color(0.26f, 0.24f, 0.22f));
            set.EnemyDetail = CreateMaterial("TankDetail_Enemy", new Color(0.30f, 0.28f, 0.26f));
            set.Concrete = CreateMaterial("ConcreteCover", new Color(0.52f, 0.51f, 0.48f));
            set.Sandbag = CreateMaterial("SandbagCover", new Color(0.42f, 0.36f, 0.22f));
            set.Bullet = CreateMaterial("Bullet", new Color(0.85f, 0.66f, 0.25f));
            return set;
        }

        private static void CreateTargets(MaterialSet set)
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
                GameObject root = new GameObject("Target" + (i + 1));
                root.transform.SetParent(targets.transform, false);
                root.transform.position = positions[i];

                GameObject plate = CreateBox(root.transform, "Plate", Vector3.zero, new Vector3(2.2f, 2.2f, 0.2f), targetMaterial, true);
                CreateBox(root.transform, "Post", new Vector3(0f, -positions[i].y, 0f), new Vector3(0.18f, positions[i].y * 2f, 0.18f), postMaterial, true);

                Target target = root.AddComponent<Target>();
                SerializedObject serialized = new SerializedObject(target);
                serialized.FindProperty("_maxHealth").floatValue = health[i];
                SerializedProperty renderers = serialized.FindProperty("_renderers");
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = plate.GetComponent<Renderer>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Game");
            CreateFolderIfMissing(RootFolder, "Materials");
            CreateFolderIfMissing(RootFolder, "Prefabs");
            CreateFolderIfMissing(RootFolder, "Configs");
            CreateFolderIfMissing(RootFolder, "Meshes");
            CreateFolderIfMissing(RootFolder, "PostProcessing");
            CreateFolderIfMissing("Assets", "Scenes");
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        #region Weapons

        private static void CreateWeaponConfigs(Material bulletMaterial)
        {
            CreateWeaponConfig("RifleConfig", "Rifle", bulletMaterial, new Color(0.95f, 0.85f, 0.25f), 10f, 0.35f, 32f, 2.5f, 0, 1f);
            CreateWeaponConfig("CannonConfig", "Cannon", bulletMaterial, new Color(0.90f, 0.45f, 0.15f), 30f, 1.1f, 22f, 4f, 6, 1.8f);
            CreateWeaponConfig("HeavyConfig", "Heavy Gun", bulletMaterial, new Color(0.75f, 0.20f, 0.85f), 65f, 1.8f, 18f, 5f, 3, 2.8f);
        }

        private static WeaponConfig CreateWeaponConfig(string assetName, string displayName, Material bulletMaterial,
            Color tint, float damage, float cooldown, float speed, float lifetime, int maxActive, float scale)
        {
            string prefabPath = PrefabsFolder + "/Projectile_" + assetName.Replace("Config", string.Empty) + ".prefab";
            GameObject prefab = CreateProjectilePrefab(prefabPath, bulletMaterial, scale);

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

        /// <summary>
        /// Builds a detailed bullet prefab: ogive nose, cylindrical body,
        /// boat-tail base, plus a trail. Saved mesh is shared between weapons.
        /// </summary>
        private static GameObject CreateProjectilePrefab(string path, Material material, float scale)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            GameObject projectile = new GameObject("Projectile");

            Mesh bulletMesh = BulletMeshGenerator.Generate(0.3f * scale, 0.06f * scale, 14, 0.6f);
            Mesh shared = SaveMesh(bulletMesh, "BulletMesh");

            MeshFilter filter = projectile.AddComponent<MeshFilter>();
            filter.sharedMesh = shared;

            MeshRenderer renderer = projectile.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            SphereCollider projectileCollider = projectile.AddComponent<SphereCollider>();
            projectileCollider.radius = 0.1f * scale;
            projectileCollider.isTrigger = true;

            int projectileLayer = LayerMask.NameToLayer("Projectile");
            if (projectileLayer >= 0)
            {
                projectile.layer = projectileLayer;
            }

            Rigidbody body = projectile.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.07f * scale;
            trail.endWidth = 0f;
            trail.numCapVertices = 4;
            trail.material = material;

            // An empty child marker used by gameplay code to spawn impact VFX
            // slightly ahead of the visual nose.
            GameObject nose = new GameObject("Nose");
            nose.transform.SetParent(projectile.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0f, 0.15f * scale);

            projectile.AddComponent<Projectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, path);
            Object.DestroyImmediate(projectile);
            return prefab;
        }

        private static Mesh SaveMesh(Mesh mesh, string assetName)
        {
            string path = MeshesFolder + "/" + assetName + ".asset";
            Mesh stored = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (stored != null)
            {
                Object.DestroyImmediate(mesh);
                return stored;
            }

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        #endregion

        #region Materials

        /// <summary>
        /// Applies a layer to a transform and all of its descendants.
        /// </summary>
        private static void SetLayerRecursively(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform child in transform)
            {
                SetLayerRecursively(child, layer);
            }
        }

        /// <summary>
        /// Loads an existing URP/Lit material from the project or creates one,
        /// then applies the base color. Materials are shared across scenes.
        /// </summary>
        private static Material CreateMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        #endregion

        #region Tanks

        private static GameObject BuildTankPrefab(MaterialSet set, bool isEnemy)
        {
            Material hull = isEnemy ? set.EnemyHull : set.Hull;
            Material track = isEnemy ? set.EnemyTrack : set.Track;
            Material barrel = isEnemy ? set.EnemyBarrel : set.Barrel;
            Material detail = isEnemy ? set.EnemyDetail : set.Detail;

            string rootName = isEnemy ? "EnemyTank" : "PlayerTank";
            GameObject root = new GameObject(rootName);

            string layerName = isEnemy ? "Enemy" : "Player";
            int layer = LayerMask.NameToLayer(layerName);

            TankModelBuilder.Build(root, hull, track, barrel, detail);

            // The model parts are created on the Default layer, so the whole
            // hierarchy must be moved explicitly. Otherwise the tank colliders
            // stay on Default and get baked into the NavMesh.
            if (layer >= 0)
            {
                SetLayerRecursively(root.transform, layer);
            }

            TankHealth health = root.AddComponent<TankHealth>();
            SerializedObject healthSerialized = new SerializedObject(health);
            healthSerialized.FindProperty("_maxHealth").floatValue = isEnemy ? 120f : 150f;
            healthSerialized.FindProperty("_currentHealth").floatValue = isEnemy ? 120f : 150f;
            healthSerialized.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<WorldSpaceHealthBar>();

            Transform turret = root.transform.Find("Turret");
            Transform muzzle = turret != null ? turret.Find("Muzzle") : null;

            if (isEnemy)
            {
                UnityEngine.AI.NavMeshAgent agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed = 3.5f;
                agent.angularSpeed = 120f;
                agent.acceleration = 8f;
                agent.stoppingDistance = 9f;
                agent.radius = 0.9f;
                agent.height = 1.2f;
                agent.autoBraking = true;

                // The tank pivot sits at hull centre; lift it so the track belts
                // rest on the navmesh surface rather than hover above it.
                agent.baseOffset = TrackBottomOffset;

                TankTurret turretComponent = turret.gameObject.AddComponent<TankTurret>();
                SerializedObject turretSerialized = new SerializedObject(turretComponent);
                turretSerialized.FindProperty("_turnSpeed").floatValue = 6f;
                turretSerialized.ApplyModifiedPropertiesWithoutUndo();

                WeaponController weapons = turret.gameObject.AddComponent<WeaponController>();
                AssignWeapons(weapons, muzzle, true);

                AIController ai = root.AddComponent<AIController>();
                SerializedObject aiSerialized = new SerializedObject(ai);
                aiSerialized.FindProperty("_turret").objectReferenceValue = turretComponent;
                aiSerialized.FindProperty("_weapon").objectReferenceValue = weapons;
                aiSerialized.FindProperty("_model").objectReferenceValue = root.transform;
                aiSerialized.FindProperty("_chaseRange").floatValue = 20f;
                aiSerialized.FindProperty("_attackRange").floatValue = 12f;
                aiSerialized.FindProperty("_shootAngle").floatValue = 10f;
                aiSerialized.FindProperty("_shootCooldown").floatValue = 2.2f;
                aiSerialized.FindProperty("_loseTargetTime").floatValue = 3f;
                aiSerialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject enemyPrefab = PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
                Object.DestroyImmediate(root);
                return enemyPrefab;
            }

            root.tag = "Player";

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 8f;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            root.AddComponent<TankInputSource>();
            root.AddComponent<TankMover>();

            TankTurret playerTurret = turret.gameObject.AddComponent<TankTurret>();
            SerializedObject playerTurretSerialized = new SerializedObject(playerTurret);
            playerTurretSerialized.FindProperty("_turnSpeed").floatValue = 12f;
            playerTurretSerialized.ApplyModifiedPropertiesWithoutUndo();

            WeaponController playerWeapons = turret.gameObject.AddComponent<WeaponController>();
            AssignWeapons(playerWeapons, muzzle, false);

            GameObject playerPrefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return playerPrefab;
        }

        private static void AssignWeapons(WeaponController controller, Transform muzzle, bool isAI)
        {
            string[] assetNames = isAI
                ? new[] { "CannonConfig" }
                : new[] { "RifleConfig", "CannonConfig", "HeavyConfig" };

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
            serialized.FindProperty("_isAIWeapon").boolValue = isAI;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Level geometry

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

        private static void CreateGround(Material material)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(ground, UnityEditor.StaticEditorFlags.NavigationStatic);

            // A primitive Plane comes with a non-convex MeshCollider, which the
            // player's dynamic Rigidbody ignores (and logs a warning). A box
            // collider is exact here and lets the tank rest on the floor.
            MeshCollider meshCollider = ground.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                Object.DestroyImmediate(meshCollider);
            }

            BoxCollider box = ground.AddComponent<BoxCollider>();
            box.size = new Vector3(100f, 0.1f, 100f);
            box.center = new Vector3(0f, -0.05f, 0f);
        }

        private static void CreateWalls(Material material)
        {
            const float half = 30f;
            const float thickness = 1f;
            const float height = 4f;

            GameObject walls = new GameObject("Walls");

            CreateStaticBox(walls.transform, "WallNorth", new Vector3(0f, height * 0.5f, half), new Vector3(half * 2f + thickness, height, thickness), material);
            CreateStaticBox(walls.transform, "WallSouth", new Vector3(0f, height * 0.5f, -half), new Vector3(half * 2f + thickness, height, thickness), material);
            CreateStaticBox(walls.transform, "WallEast", new Vector3(half, height * 0.5f, 0f), new Vector3(thickness, height, half * 2f), material);
            CreateStaticBox(walls.transform, "WallWest", new Vector3(-half, height * 0.5f, 0f), new Vector3(thickness, height, half * 2f), material);
        }

        private static void CreateStaticBox(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject box = CreateBox(parent, name, localPosition, scale, material, true);
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(box, UnityEditor.StaticEditorFlags.NavigationStatic);
        }

        private static void CreateObstacles(Material material)
        {
            GameObject obstacles = new GameObject("Obstacles");

            CreateObstacle(obstacles.transform, "ObstacleBlockA", new Vector3(-10f, 1f, 4f), new Vector3(4f, 2f, 2f), Quaternion.Euler(0f, 20f, 0f), material);
            CreateObstacle(obstacles.transform, "ObstacleBlockB", new Vector3(8f, 1.5f, -6f), new Vector3(3f, 3f, 6f), Quaternion.Euler(0f, -15f, 0f), material);
            CreateObstacle(obstacles.transform, "ObstacleRamp", new Vector3(0f, 0.25f, 12f), new Vector3(5f, 0.5f, 3f), Quaternion.Euler(-8f, 0f, 0f), material);
            CreateObstacle(obstacles.transform, "ObstaclePillarA", new Vector3(-4f, 2f, -2f), new Vector3(1.5f, 4f, 1.5f), Quaternion.identity, material);
            CreateObstacle(obstacles.transform, "ObstaclePillarB", new Vector3(5f, 2f, 8f), new Vector3(1.5f, 4f, 1.5f), Quaternion.identity, material);
            CreateObstacle(obstacles.transform, "ObstacleLowWall", new Vector3(-14f, 0.75f, -10f), new Vector3(6f, 1.5f, 1f), Quaternion.Euler(0f, 45f, 0f), material);
            CreateObstacle(obstacles.transform, "ObstacleCrateCluster", new Vector3(12f, 0.75f, 12f), new Vector3(2f, 1.5f, 2f), Quaternion.identity, material);
            CreateObstacle(obstacles.transform, "ObstacleLongBar", new Vector3(2f, 0.5f, -14f), new Vector3(12f, 1f, 1f), Quaternion.Euler(0f, 30f, 0f), material);
        }

        private static void CreateObstacle(Transform parent, string name, Vector3 localPosition, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject box = CreateBox(parent, name, localPosition, scale, material, true);
            box.transform.localRotation = rotation;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(box, UnityEditor.StaticEditorFlags.NavigationStatic);
        }

        /// <summary>
        /// Two cover types built from generated meshes: reinforced concrete blocks
        /// and stacked sandbags. Both are NavigationStatic so the rebaked NavMesh
        /// routes around them.
        /// </summary>
        private static void CreateCovers(MaterialSet set)
        {
            Mesh concreteMesh = SaveMesh(ConcreteBlockGenerator.Generate(2.4f, 1.6f, 1.2f, 0.06f), "ConcreteCover");
            Mesh concreteTallMesh = SaveMesh(ConcreteBlockGenerator.Generate(1.4f, 2.6f, 1.4f, 0.04f), "ConcreteCover_Tall");
            Mesh sandbagMesh = SaveMesh(SandbagStackGenerator.GenerateWall(8, 3, 0.12f), "SandbagWall");

            GameObject covers = new GameObject("Covers");

            GameObject concreteA = CreateCover(covers.transform, "Cover_Concrete_A", concreteMesh, set.Concrete, new Vector3(-8f, 0.8f, -11f));
            concreteA.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);

            CreateCover(covers.transform, "Cover_Concrete_B", concreteMesh, set.Concrete, new Vector3(10f, 0.8f, 11f));

            GameObject concreteC = CreateCover(covers.transform, "Cover_Concrete_C", concreteMesh, set.Concrete, new Vector3(-17f, 0.8f, 7f));
            concreteC.transform.localRotation = Quaternion.Euler(0f, 70f, 0f);

            GameObject concreteD = CreateCover(covers.transform, "Cover_Concrete_D", concreteMesh, set.Concrete, new Vector3(14f, 0.8f, -9f));
            concreteD.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);

            CreateCover(covers.transform, "Cover_Concrete_Tall_A", concreteTallMesh, set.Concrete, new Vector3(-2f, 1.3f, 18f));
            CreateCover(covers.transform, "Cover_Concrete_Tall_B", concreteTallMesh, set.Concrete, new Vector3(17f, 1.3f, 1f));

            CreateCover(covers.transform, "Cover_Sandbags_A", sandbagMesh, set.Sandbag, new Vector3(6f, 0f, 5f));

            GameObject sandbagsB = CreateCover(covers.transform, "Cover_Sandbags_B", sandbagMesh, set.Sandbag, new Vector3(-13f, 0f, -3f));
            sandbagsB.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
        }

        private static GameObject CreateCover(Transform parent, string name, Mesh mesh, Material material, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            // Convex primitive collider: the sandbag mesh is concave, and a
            // MeshCollider on it would be ignored by the player's Rigidbody.
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;

            UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.StaticEditorFlags.NavigationStatic);
            return go;
        }

        #endregion

        #region HUD

        /// <summary>
        /// Legacy uGUI HUD: nameplate, filled health bar and health text.
        /// </summary>
        private static void CreatePlayerHealthHud()
        {
            GameObject hudObject = new GameObject("PlayerHealthHUD");
            Canvas canvas = hudObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = hudObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            hudObject.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("HealthPanel");
            panel.transform.SetParent(hudObject.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -104f);
            panelRect.sizeDelta = new Vector2(420f, 48f);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject fill = new GameObject("HealthFill");
            fill.transform.SetParent(panel.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);

            Image fillImage = fill.AddComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
            fillImage.color = new Color(0.15f, 0.75f, 0.2f);

            GameObject text = new GameObject("HealthText");
            text.transform.SetParent(panel.transform, false);
            RectTransform textRect = text.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text label = text.AddComponent<Text>();
            label.font = LoadUiFont();
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "150 / 150";

            GameObject nameplate = new GameObject("Nameplate");
            nameplate.transform.SetParent(hudObject.transform, false);
            RectTransform nameplateRect = nameplate.AddComponent<RectTransform>();
            nameplateRect.anchorMin = new Vector2(0f, 1f);
            nameplateRect.anchorMax = new Vector2(0f, 1f);
            nameplateRect.pivot = new Vector2(0f, 1f);
            nameplateRect.anchoredPosition = new Vector2(24f, -72f);
            nameplateRect.sizeDelta = new Vector2(420f, 32f);

            GameObject nameplateText = new GameObject("NameLabel");
            nameplateText.transform.SetParent(nameplate.transform, false);
            RectTransform nameLabelRect = nameplateText.AddComponent<RectTransform>();
            nameLabelRect.anchorMin = Vector2.zero;
            nameLabelRect.anchorMax = Vector2.one;
            nameLabelRect.offsetMin = Vector2.zero;
            nameLabelRect.offsetMax = Vector2.zero;

            Text nameLabel = nameplateText.AddComponent<Text>();
            nameLabel.font = LoadUiFont();
            nameLabel.fontSize = 24;
            nameLabel.alignment = TextAnchor.MiddleLeft;
            nameLabel.color = new Color(0.75f, 0.85f, 1f);
            nameLabel.text = "PLAYER TANK";

            CanvasGroup canvasGroup = hudObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            PlayerHealthUI ui = hudObject.AddComponent<PlayerHealthUI>();
            SerializedObject uiSerialized = new SerializedObject(ui);
            uiSerialized.FindProperty("_healthFill").objectReferenceValue = fillImage;
            uiSerialized.FindProperty("_healthText").objectReferenceValue = label;
            uiSerialized.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            uiSerialized.ApplyModifiedPropertiesWithoutUndo();
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
            label.text = "РўР°РЅРє Р·СѓРїРёРЅРёРІСЃСЏ";

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

        #endregion

        #region AI route, lighting, camera, volume, navmesh

        private static Transform[] CreateAiPatrolRoute()
        {
            GameObject routeRoot = new GameObject("AI_Route");
            Transform[] waypoints = new Transform[AiPatrolRoute.Length];

            for (int i = 0; i < AiPatrolRoute.Length; i++)
            {
                GameObject waypoint = new GameObject("Waypoint_" + (i + 1));
                waypoint.transform.SetParent(routeRoot.transform, false);
                waypoint.transform.position = AiPatrolRoute[i];
                waypoints[i] = waypoint.transform;
            }

            return waypoints;
        }

        /// <summary>
        /// Scene-object references must never be stored on a prefab asset, so the
        /// route lives on the scene instance only. AIController.FindWaypointsInScene()
        /// resolves the "AI_Route" group at runtime as a fallback.
        /// </summary>
        private static void AssignAiRoute(GameObject enemy, Transform[] waypoints)
        {
            if (enemy == null || waypoints == null)
            {
                return;
            }

            AIController ai = enemy.GetComponent<AIController>();
            if (ai == null)
            {
                Debug.LogWarning("AssignAiRoute: AIController missing on " + enemy.name);
                return;
            }

            SerializedObject serialized = new SerializedObject(ai);
            SerializedProperty waypointsProperty = serialized.FindProperty("_waypoints");
            waypointsProperty.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLighting()
        {
            GameObject light = new GameObject("Directional Light");
            Light directional = light.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.2f;
            directional.color = new Color(1f, 0.96f, 0.88f);
            directional.shadows = LightShadows.Soft;
            directional.shadowStrength = 0.85f;
            directional.shadowBias = 0.02f;
            directional.shadowNormalBias = 0.2f;
            directional.shadowNearPlane = 0.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.43f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.18f);
            RenderSettings.fog = false;
            RenderSettings.sun = directional;
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

        /// <summary>
        /// Global (scene-independent) URP volume with Tonemapping,
        /// Color Adjustments, Bloom and Vignette. Overrides are registered as
        /// sub-assets of the profile asset so the values survive serialization.
        /// </summary>
        private static void CreateGlobalVolume()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GlobalVolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, GlobalVolumeProfilePath);
            }

            ConfigureTonemapping(GetOrAddOverride<Tonemapping>(profile));
            ConfigureColorAdjustments(GetOrAddOverride<ColorAdjustments>(profile));
            ConfigureBloom(GetOrAddOverride<Bloom>(profile));
            ConfigureVignette(GetOrAddOverride<Vignette>(profile));

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            GameObject volumeObject = new GameObject("Global Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.weight = 1f;
            volume.profile = profile;
        }

        /// <summary>
        /// Returns the override for T on the profile, adding it as a sub-asset
        /// of the profile asset so its values are persisted. Existing overrides
        /// are reused so repeated builds never destroy assets.
        /// </summary>
        private static T GetOrAddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing) && existing != null)
            {
                existing.hideFlags = HideFlags.None;
                existing.active = true;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            T component = profile.Add<T>();
            component.active = true;
            component.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(component);
            return component;
        }

        private static void ConfigureTonemapping(Tonemapping v)
        {
            v.mode.Override(TonemappingMode.Neutral);
        }

        private static void ConfigureColorAdjustments(ColorAdjustments v)
        {
            v.postExposure.Override(0.1f);
            v.contrast.Override(8f);
            v.colorFilter.Override(Color.white);
            v.hueShift.Override(0f);
            v.saturation.Override(10f);
        }

        private static void ConfigureBloom(Bloom v)
        {
            v.threshold.Override(1f);
            v.intensity.Override(0.5f);
            v.scatter.Override(0.7f);
            v.tint.Override(new Color(1f, 0.95f, 0.9f));
        }

        private static void ConfigureVignette(Vignette v)
        {
            v.color.Override(Color.black);
            v.center.Override(new Vector2(0.5f, 0.5f));
            v.intensity.Override(0.25f);
            v.smoothness.Override(0.4f);
            v.rounded.Override(false);
        }

private static void BuildNavMesh()
        {
            // Legacy NavMesh (Unity.AI) — the NavMeshAgent on EnemyTank uses
            // this API. Only NavigationStatic geometry contributes; the tanks
            // live on the Player/Enemy layers and are excluded so the surface
            // stays at ground level instead of being built on top of them.
            NavMeshSettings settings = NavMeshAgentSettings.LoadFromSettings(NavMeshAgentSettings.defaultAgentTypeID);
            NavMeshData data = UnityEngine.AI.NavMesh.Bake(settings, new Vector3(200f, 20f, 200f));
            UnityEngine.AI.NavMesh.AddNavMeshData(data);
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

        #endregion
    }
}
