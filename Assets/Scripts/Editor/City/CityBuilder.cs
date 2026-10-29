using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tanks.EditorTools.City
{
    /// <summary>
    /// Builds the "City" battle map (180 x 180 m): a 4 x 4 grid of city blocks
    /// (dense housing, ruins, parks, parking, industrial yards) separated by
    /// streets, a central monument, street furniture, a skyline outside the
    /// walls, six enemies on their own patrol routes and repair kits.
    /// Menu: Tools/Tanks/Build City.
    /// </summary>
    public static class CityBuilder
    {
        public const string ScenePath = "Assets/Scenes/City.unity";
        private const float Half = 90f;
        private const float BlockHalf = 14f;
        private static readonly float[] BlockCenters = { -60f, -20f, 20f, 60f };
        private static readonly float[] RoadCenters = { -82f, -40f, 0f, 40f, 82f };

        private enum BlockType { Dense, Ruins, Park, Parking, Industrial }

        private static readonly BlockType[,] Layout =
        {
            // [i (x), j (z)]
            { BlockType.Industrial, BlockType.Dense, BlockType.Park, BlockType.Dense },
            { BlockType.Dense, BlockType.Ruins, BlockType.Dense, BlockType.Dense },
            { BlockType.Parking, BlockType.Dense, BlockType.Park, BlockType.Ruins },
            { BlockType.Industrial, BlockType.Dense, BlockType.Ruins, BlockType.Dense }
        };

        private static readonly string[] Facades = { "FacadeBrick", "FacadePlaster", "FacadePanel" };
        private static readonly string[] Paints = { "CarRed", "CarBlue", "CarWhite", "CarYellow", "CarGreen", "CarGrey" };
        private static readonly string[] ContainerMats = { "ContainerRed", "ContainerBlue", "ContainerGreen", "ContainerOrange" };

        [MenuItem("Tools/Tanks/Build City")]
        public static void BuildCity()
        {
            BuildGeometry();
            Finish();
        }

        public static void BuildGeometry()
        {
            CityAssets.Prepare();
            CityProps.ResetCache();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            System.Random rng = new System.Random(2024);

            Transform city = new GameObject("City").transform;
            CreateGround(city);

            Transform blocks = new GameObject("Blocks").transform;
            blocks.SetParent(city, false);
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                Vector3 c = new Vector3(BlockCenters[i], 0f, BlockCenters[j]);
                Transform block = CityProps.Node(blocks, "Block_" + i + "_" + j + "_" + Layout[i, j], c).transform;
                bool nearCenter = (i == 1 || i == 2) && (j == 1 || j == 2);
                Vector3 toCenter = new Vector3(-Mathf.Sign(c.x), 0f, -Mathf.Sign(c.z));
                switch (Layout[i, j])
                {
                    case BlockType.Dense: DenseBlock(block, rng, nearCenter ? toCenter : Vector3.zero); break;
                    case BlockType.Ruins: RuinsBlock(block, rng); break;
                    case BlockType.Park: ParkBlock(block, rng, i == 2 && j == 2); break;
                    case BlockType.Parking: ParkingBlock(block, rng); break;
                    case BlockType.Industrial: IndustrialBlock(block, rng); break;
                }
            }

            CreateStreets(city, rng);
            CreateBoundary(city, rng);
            CreateSkyline(city, rng);

            foreach (Transform t in city.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }

            AssetDatabase.SaveAssets();
        }

        public static void Finish()
        {
            CreateLightingCameraHud();
            SpawnTanks();
            CreateRepairKits();
            ArenaPolish.SetupCamera();
            ArenaPolish.SetupHud();
            ArenaPolish.SetupLightingAndAtmosphere();

            Camera cam = Camera.main;
            cam.fieldOfView = 55f;
            cam.farClipPlane = 260f;

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            ArenaPolish.BakeNavMesh();
            SnapRoutes();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (s.path != ScenePath) scenes.Add(s);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[City] City map built: " + ScenePath);
        }

        #region Ground & streets

        private static GameObject Plate(Transform parent, string name, Vector3 center, Vector2 size, string mat, float y)
        {
            return CityProps.Prim(parent, PrimitiveType.Quad, name, new Vector3(center.x, y, center.z), new Vector3(size.x, size.y, 1f), mat,
                Quaternion.Euler(90f, 0f, 0f));
        }

        private static void CreateGround(Transform city)
        {
            GameObject ground = CityProps.Prim(city, PrimitiveType.Cube, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(280f, 1f, 280f), "Dirt", null, true);
            ground.GetComponent<MeshRenderer>().sharedMaterial = CityAssets.Mat("Dirt");
            ground.AddComponent<ImpactSurface>().Surface = SurfaceType.Dirt;
            Plate(city, "Asphalt", Vector3.zero, new Vector2(Half * 2f, Half * 2f), "Asphalt", 0.005f);
        }

        private static void BlockBase(Transform block, string mat)
        {
            Plate(block, "Base", Vector3.zero, new Vector2(30f, 30f), mat, 0.012f);
            float h = 0.15f;
            CityProps.Prim(block, PrimitiveType.Cube, "CurbS", new Vector3(0f, h * 0.5f, -15f), new Vector3(30.3f, h, 0.3f), "Concrete");
            CityProps.Prim(block, PrimitiveType.Cube, "CurbN", new Vector3(0f, h * 0.5f, 15f), new Vector3(30.3f, h, 0.3f), "Concrete");
            CityProps.Prim(block, PrimitiveType.Cube, "CurbW", new Vector3(-15f, h * 0.5f, 0f), new Vector3(0.3f, h, 30f), "Concrete");
            CityProps.Prim(block, PrimitiveType.Cube, "CurbE", new Vector3(15f, h * 0.5f, 0f), new Vector3(0.3f, h, 30f), "Concrete");
        }

        private static void CreateStreets(Transform city, System.Random rng)
        {
            Transform streets = new GameObject("Streets").transform;
            streets.SetParent(city, false);
            Transform marks = new GameObject("Markings").transform;
            marks.SetParent(streets, false);

            // Dashed centre lines, skipping intersections and the central plaza.
            foreach (float line in RoadCenters)
            {
                bool outer = Mathf.Abs(line) > 60f;
                for (float t = -86f; t <= 86f; t += 6f)
                {
                    bool atIntersection = false;
                    foreach (float cross in RoadCenters)
                    {
                        float halfWidth = Mathf.Abs(cross) > 60f ? 9f : 7.5f;
                        if (Mathf.Abs(t - cross) < halfWidth) atIntersection = true;
                    }

                    if (atIntersection) continue;
                    string mat = outer ? "RoadLineYellow" : "RoadLine";
                    Plate(marks, "Dash", new Vector3(line, 0f, t), new Vector2(0.18f, 3f), mat, 0.02f);
                    Plate(marks, "Dash", new Vector3(t, 0f, line), new Vector2(3f, 0.18f), mat, 0.02f);
                }
            }

            // Zebra crossings around the inner intersections.
            float[] inner = { -40f, 0f, 40f };
            foreach (float x in inner)
            foreach (float z in inner)
            {
                if (x == 0f && z == 0f) continue;
                for (int s = 0; s < 6; s++)
                {
                    float o = -3.75f + s * 1.5f;
                    Plate(marks, "Zebra", new Vector3(x + o, 0f, z - 7.6f), new Vector2(0.7f, 2.6f), "RoadLine", 0.02f);
                    Plate(marks, "Zebra", new Vector3(x + o, 0f, z + 7.6f), new Vector2(0.7f, 2.6f), "RoadLine", 0.02f);
                    Plate(marks, "Zebra", new Vector3(x - 7.6f, 0f, z + o), new Vector2(2.6f, 0.7f), "RoadLine", 0.02f);
                    Plate(marks, "Zebra", new Vector3(x + 7.6f, 0f, z + o), new Vector2(2.6f, 0.7f), "RoadLine", 0.02f);
                }

                CityProps.TrafficLight(streets, new Vector3(x - 6.6f, 0f, z - 6.6f), 0f, rng.NextDouble() < 0.5);
                CityProps.TrafficLight(streets, new Vector3(x + 6.6f, 0f, z + 6.6f), 180f, rng.NextDouble() < 0.5);
            }

            // Central roundabout with the tank monument.
            CityProps.Prim(streets, PrimitiveType.Cylinder, "PlazaRing", new Vector3(0f, 0.08f, 0f), new Vector3(11f, 0.08f, 11f), "Stone");
            CityProps.Prim(streets, PrimitiveType.Cylinder, "PlazaGrass", new Vector3(0f, 0.12f, 0f), new Vector3(10f, 0.05f, 10f), "Grass");
            GameObject monument = CityProps.Monument(streets, Vector3.zero);
            monument.transform.localScale = Vector3.one * 0.8f;

            // Abandoned / burnt vehicles and barricades on the streets.
            Vector3[] wrecks =
            {
                new Vector3(-37f, 0f, -62f), new Vector3(43f, 0f, 22f), new Vector3(-20f, 0f, 37.5f), new Vector3(63f, 0f, -42f),
                new Vector3(2.5f, 0f, -24f), new Vector3(-84f, 0f, 18f), new Vector3(30f, 0f, 84f), new Vector3(85f, 0f, -70f)
            };
            for (int i = 0; i < wrecks.Length; i++)
            {
                bool alongZ = Mathf.Abs(Mathf.Round(wrecks[i].x / 40f) * 40f - wrecks[i].x) < 4f || Mathf.Abs(Mathf.Abs(wrecks[i].x) - 82f) < 4f;
                CityProps.Car(streets, wrecks[i], (alongZ ? 0f : 90f) + (float)rng.NextDouble() * 30f - 15f, Paints[rng.Next(Paints.Length)], i % 2 == 0);
            }

            Barricade(streets, new Vector3(-40f, 0f, 20f), 0f, rng);
            Barricade(streets, new Vector3(20f, 0f, -40f), 90f, rng);
            Barricade(streets, new Vector3(40f, 0f, 60f), 0f, rng);
            Barricade(streets, new Vector3(-60f, 0f, 40f), 90f, rng);

            // Outer ring: trees and lamps along the wall, bus stops.
            for (float t = -80f; t <= 80f; t += 12f)
            {
                CityProps.Tree(streets, new Vector3(t, 0f, 87.5f), rng);
                CityProps.Tree(streets, new Vector3(t + 6f, 0f, -87.5f), rng);
                CityProps.Tree(streets, new Vector3(87.5f, 0f, t + 3f), rng);
                CityProps.Tree(streets, new Vector3(-87.5f, 0f, t + 9f), rng);
            }

            CityProps.BusStop(streets, new Vector3(-20f, 0f, -88f), 180f);
            CityProps.BusStop(streets, new Vector3(25f, 0f, 88f), 0f);
            CityProps.BusStop(streets, new Vector3(88f, 0f, 30f), 90f);
        }

        private static void Barricade(Transform parent, Vector3 center, float rotY, System.Random rng)
        {
            Transform root = CityProps.Node(parent, "Barricade", center, rotY).transform;
            CityProps.Barrier(root, new Vector3(-3.5f, 0f, 0f), 0f);
            CityProps.Barrier(root, new Vector3(3.5f, 0f, 0f), 0f);
            CityProps.Hedgehog(root, new Vector3(0f, 0f, 2.5f), (float)rng.NextDouble() * 90f);
            CityProps.Sandbags(root, new Vector3(-1.5f, 0f, -2f), 0f);
        }

        private static void CreateBoundary(Transform city, System.Random rng)
        {
            Transform walls = new GameObject("Boundary").transform;
            walls.SetParent(city, false);
            float h = 5f;
            Wall(walls, "WallN", new Vector3(0f, h * 0.5f, Half), new Vector3(Half * 2f + 1f, h, 1f));
            Wall(walls, "WallS", new Vector3(0f, h * 0.5f, -Half), new Vector3(Half * 2f + 1f, h, 1f));
            Wall(walls, "WallE", new Vector3(Half, h * 0.5f, 0f), new Vector3(1f, h, Half * 2f));
            Wall(walls, "WallW", new Vector3(-Half, h * 0.5f, 0f), new Vector3(1f, h, Half * 2f));
        }

        private static void Wall(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject w = CityProps.Prim(parent, PrimitiveType.Cube, name, pos, size, "Concrete", null, true);
            w.AddComponent<ImpactSurface>().Surface = SurfaceType.Concrete;
        }

        /// <summary>Tall background buildings outside the walls (visual only, no colliders, no shadows).</summary>
        private static void CreateSkyline(Transform city, System.Random rng)
        {
            Transform sky = new GameObject("Skyline").transform;
            sky.SetParent(city, false);
            for (int side = 0; side < 4; side++)
            {
                for (float t = -120f; t <= 120f; t += 14f)
                {
                    float dist = 100f + (float)rng.NextDouble() * 18f;
                    Vector3 pos;
                    float rot;
                    switch (side)
                    {
                        case 0: pos = new Vector3(t, 0f, dist); rot = 180f; break;
                        case 1: pos = new Vector3(t, 0f, -dist); rot = 0f; break;
                        case 2: pos = new Vector3(dist, 0f, t); rot = -90f; break;
                        default: pos = new Vector3(-dist, 0f, t); rot = 90f; break;
                    }

                    GameObject b = CityProps.Building(sky, pos, rot, 10f + (float)rng.NextDouble() * 4f, 10f, 5 + rng.Next(12),
                        Facades[rng.Next(Facades.Length)], rng, false);
                    foreach (Collider c in b.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                    foreach (ImpactSurface s in b.GetComponentsInChildren<ImpactSurface>()) Object.DestroyImmediate(s);
                    foreach (MeshRenderer r in b.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        #endregion

        #region Blocks

        private static void DenseBlock(Transform block, System.Random rng, Vector3 openCorner)
        {
            BlockBase(block, "Sidewalk");
            const float depth = 9f;

            // Two random sides get an alley into the courtyard.
            bool[] alley = new bool[4];
            int first = rng.Next(4);
            alley[first] = true;
            alley[(first + 1 + rng.Next(3)) % 4] = true;

            // South (0) / North (1): full-width rows.
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? -BlockHalf + depth * 0.5f : BlockHalf - depth * 0.5f;
                float rot = side == 0 ? 0f : 180f;
                float[][] segments = alley[side]
                    ? new[] { new[] { -14f, -3f }, new[] { 3f, 14f } }
                    : new[] { new[] { -14f, 0f }, new[] { 0f, 14f } };
                foreach (float[] seg in segments)
                {
                    float mid = (seg[0] + seg[1]) * 0.5f;
                    if (IsOpenCorner(new Vector3(mid, 0f, z), openCorner)) continue;
                    PlaceBuilding(block, new Vector3(mid, 0f, z), rot, seg[1] - seg[0] - 0.6f, depth, rng);
                }
            }

            // West (2) / East (3): middle segment between the rows.
            for (int side = 2; side < 4; side++)
            {
                if (alley[side]) continue;
                float x = side == 2 ? -BlockHalf + depth * 0.5f : BlockHalf - depth * 0.5f;
                float rot = side == 2 ? 90f : -90f;
                if (IsOpenCorner(new Vector3(x, 0f, 0f), openCorner)) continue;
                PlaceBuilding(block, new Vector3(x, 0f, 0f), rot, 10f - 0.6f, depth, rng);
            }

            // Courtyard.
            int kind = rng.Next(3);
            if (kind == 0)
            {
                CityProps.Car(block, new Vector3(-2f, 0f, 1f), 90f + (float)rng.NextDouble() * 20f, Paints[rng.Next(Paints.Length)], rng.NextDouble() < 0.3);
                CityProps.Tree(block, new Vector3(2.5f, 0f, -2f), rng, 0.8f);
            }
            else if (kind == 1)
            {
                CityProps.Tree(block, new Vector3(-2.5f, 0f, -2f), rng, 0.8f);
                CityProps.Tree(block, new Vector3(2.5f, 0f, 2.5f), rng, 0.8f);
                CityProps.Bench(block, new Vector3(0f, 0f, -0.5f), 0f);
            }
            else
            {
                CityProps.Sandbags(block, new Vector3(-1.5f, 0f, 0f), 0f);
                CityProps.Rubble(block, new Vector3(2.5f, 0f, 2f), 1.4f, rng);
            }

            // Street lamps on the outer sidewalk.
            CityProps.StreetLamp(block, new Vector3(-7f, 0f, -14.6f), 180f);
            CityProps.StreetLamp(block, new Vector3(7f, 0f, 14.6f), 0f);
            CityProps.StreetLamp(block, new Vector3(-14.6f, 0f, 7f), -90f);
            CityProps.StreetLamp(block, new Vector3(14.6f, 0f, -7f), 90f);
        }

        private static bool IsOpenCorner(Vector3 local, Vector3 openCorner)
        {
            if (openCorner == Vector3.zero) return false;
            return Vector3.Dot(new Vector3(Mathf.Sign(local.x), 0f, Mathf.Sign(local.z)), openCorner) > 1.5f
                   && Mathf.Abs(local.x) > 4f && Mathf.Abs(local.z) > 4f;
        }

        private static void PlaceBuilding(Transform block, Vector3 local, float rot, float width, float depth, System.Random rng)
        {
            int floors = 2 + rng.Next(6);
            bool damaged = rng.NextDouble() < 0.15;
            CityProps.Building(block, local, rot, width, depth, floors, Facades[rng.Next(Facades.Length)], rng, rng.NextDouble() < 0.55, damaged);
        }

        private static void RuinsBlock(Transform block, System.Random rng)
        {
            BlockBase(block, "Dirt");
            CityProps.Building(block, new Vector3(-8f, 0f, 8f), 180f, 10f, 9f, 2 + rng.Next(2), Facades[rng.Next(3)], rng, false, true);

            // Shell of a collapsed building (walls with gaps).
            CityProps.RuinWall(block, new Vector3(6f, 0f, -11f), 0f, 12f, 4f, rng);
            CityProps.RuinWall(block, new Vector3(11.5f, 0f, -4f), 90f, 8f, 3.5f, rng);
            CityProps.RuinWall(block, new Vector3(-11f, 0f, -7f), 90f, 9f, 3f, rng);
            CityProps.RuinWall(block, new Vector3(2f, 0f, 11f), 0f, 7f, 2.5f, rng);

            CityProps.Rubble(block, new Vector3(4f, 0f, -5f), 2.2f, rng);
            CityProps.Rubble(block, new Vector3(-4f, 0f, -2f), 1.8f, rng);
            CityProps.Rubble(block, new Vector3(9f, 0f, 6f), 2f, rng);
            CityProps.Car(block, new Vector3(-6f, 0f, -10f), 30f, "CarGrey", true);
            CityProps.Hedgehog(block, new Vector3(0f, 0f, 4f), 20f);
            CityProps.Hedgehog(block, new Vector3(-9f, 0f, 2f), 60f);
            CityProps.Sandbags(block, new Vector3(5f, 0f, 2f), 90f);
            CityProps.Sandbags(block, new Vector3(-2f, 0f, 9f), 0f);
            CityProps.Tree(block, new Vector3(10f, 0f, 11f), rng, 0.7f);
        }

        private static void ParkBlock(Transform block, System.Random rng, bool central)
        {
            BlockBase(block, "Sidewalk");
            Plate(block, "Lawn", Vector3.zero, new Vector2(27f, 27f), "Grass", 0.016f);
            Plate(block, "PathX", Vector3.zero, new Vector2(27f, 3f), "Sidewalk", 0.02f);
            Plate(block, "PathZ", Vector3.zero, new Vector2(3f, 27f), "Sidewalk", 0.02f);

            if (central) CityProps.Fountain(block, Vector3.zero);
            else
            {
                GameObject m = CityProps.Monument(block, Vector3.zero);
                m.transform.localScale = Vector3.one * 0.6f;
            }

            for (int q = 0; q < 4; q++)
            {
                float sx = q % 2 == 0 ? 1f : -1f;
                float sz = q < 2 ? 1f : -1f;
                int trees = 4 + rng.Next(3);
                for (int t = 0; t < trees; t++)
                {
                    Vector3 p = new Vector3(sx * Mathf.Lerp(4f, 12.5f, (float)rng.NextDouble()), 0f, sz * Mathf.Lerp(4f, 12.5f, (float)rng.NextDouble()));
                    CityProps.Tree(block, p, rng);
                }

                CityProps.Bench(block, new Vector3(sx * 6f, 0f, sz * 2.3f), sz > 0 ? 180f : 0f);
            }

            // Hedge border with gaps where the paths leave the park.
            CityProps.Hedge(block, new Vector3(-8.5f, 0f, -14f), 0f, 10f);
            CityProps.Hedge(block, new Vector3(8.5f, 0f, -14f), 0f, 10f);
            CityProps.Hedge(block, new Vector3(-8.5f, 0f, 14f), 0f, 10f);
            CityProps.Hedge(block, new Vector3(8.5f, 0f, 14f), 0f, 10f);
            CityProps.Hedge(block, new Vector3(-14f, 0f, -8.5f), 90f, 10f);
            CityProps.Hedge(block, new Vector3(-14f, 0f, 8.5f), 90f, 10f);
            CityProps.Hedge(block, new Vector3(14f, 0f, -8.5f), 90f, 10f);
            CityProps.Hedge(block, new Vector3(14f, 0f, 8.5f), 90f, 10f);

            CityProps.StreetLamp(block, new Vector3(2.2f, 0f, 7f), 90f);
            CityProps.StreetLamp(block, new Vector3(-2.2f, 0f, -7f), -90f);
        }

        private static void ParkingBlock(Transform block, System.Random rng)
        {
            BlockBase(block, "Sidewalk");
            GameObject lot = Plate(block, "Lot", Vector3.zero, new Vector2(27f, 27f), "Asphalt", 0.016f);
            lot.GetComponent<MeshRenderer>().sharedMaterial = CityAssets.Mat("Asphalt");

            for (int row = 0; row < 2; row++)
            {
                float z = row == 0 ? -8f : 8f;
                for (int s = 0; s < 9; s++)
                {
                    float x = -11.7f + s * 2.9f;
                    Plate(block, "Stall", new Vector3(x - 1.45f, 0f, z), new Vector2(0.12f, 5f), "RoadLine", 0.022f);
                    if (rng.NextDouble() < 0.6)
                    {
                        CityProps.Car(block, new Vector3(x, 0f, z), (row == 0 ? 0f : 180f) + (float)rng.NextDouble() * 6f - 3f,
                            Paints[rng.Next(Paints.Length)], rng.NextDouble() < 0.2);
                    }
                }
            }

            CityProps.Building(block, new Vector3(10f, 0f, 0f), -90f, 7f, 4f, 1, "FacadePlaster", rng, true);
            CityProps.StreetLamp(block, new Vector3(-6f, 0f, 0f), 90f);
            CityProps.StreetLamp(block, new Vector3(4f, 0f, 0f), -90f);
            CityProps.Barrier(block, new Vector3(-13f, 0f, 2f), 90f);
        }

        private static void IndustrialBlock(Transform block, System.Random rng)
        {
            BlockBase(block, "Dirt");

            GameObject warehouse = CityProps.Node(block, "Warehouse", new Vector3(-4f, 0f, 6f), 180f);
            warehouse.AddComponent<MeshFilter>().sharedMesh = CityAssets.Building(16f, 7f, 11f, 2f, 7f);
            warehouse.AddComponent<MeshRenderer>().sharedMaterials = new[] { CityAssets.Mat("Warehouse"), CityAssets.Mat("Roof") };
            BoxCollider wc = warehouse.AddComponent<BoxCollider>();
            wc.center = new Vector3(0f, 3.5f, 0f);
            wc.size = new Vector3(16f, 7f, 11f);
            warehouse.AddComponent<ImpactSurface>().Surface = SurfaceType.Metal;
            CityProps.Prim(warehouse.transform, PrimitiveType.Cube, "Door", new Vector3(0f, 2.2f, -5.56f), new Vector3(5f, 4.4f, 0.1f), "Metal");

            CityProps.Container(block, new Vector3(6f, 0f, -9f), 90f, ContainerMats[rng.Next(4)]);
            CityProps.Container(block, new Vector3(6f, 2.6f, -9f), 92f, ContainerMats[rng.Next(4)]);
            CityProps.Container(block, new Vector3(-6f, 0f, -9f), 90f, ContainerMats[rng.Next(4)]);
            CityProps.Container(block, new Vector3(-6f, 0f, -6f), 88f, ContainerMats[rng.Next(4)]);
            CityProps.Container(block, new Vector3(9.5f, 0f, 3f), 0f, ContainerMats[rng.Next(4)]);
            CityProps.FuelTank(block, new Vector3(9f, 0f, 10f));

            // Fence with two gates.
            CityProps.Fence(block, new Vector3(-14f, 0f, -14f), new Vector3(-4f, 0f, -14f));
            CityProps.Fence(block, new Vector3(4f, 0f, -14f), new Vector3(14f, 0f, -14f));
            CityProps.Fence(block, new Vector3(14f, 0f, -14f), new Vector3(14f, 0f, -4f));
            CityProps.Fence(block, new Vector3(14f, 0f, 4f), new Vector3(14f, 0f, 14f));
            CityProps.Fence(block, new Vector3(-14f, 0f, -14f), new Vector3(-14f, 0f, 14f));

            CityProps.StreetLamp(block, new Vector3(0f, 0f, -12f), 180f);
            for (int i = 0; i < 3; i++)
            {
                GameObject crate = CityProps.Prim(block, PrimitiveType.Cube, "Crate", new Vector3(2f + i * 1.3f, 0.55f, -3f), Vector3.one * 1.1f, "Wood",
                    Quaternion.Euler(0f, i * 23f, 0f), true);
                crate.AddComponent<ImpactSurface>().Surface = SurfaceType.Wood;
            }
        }

        #endregion

        #region Lighting, tanks, kits

        private static void CreateLightingCameraHud()
        {
            GameObject sun = new GameObject("Directional Light");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            sun.AddComponent<UniversalAdditionalLightData>();

            GameObject volume = new GameObject("Global Volume");
            Volume v = volume.AddComponent<Volume>();
            v.isGlobal = true;
            v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ArenaPolishAssets.DayProfilePath);

            GameObject camObject = new GameObject("Main Camera");
            camObject.tag = "MainCamera";
            camObject.AddComponent<Camera>();
            camObject.AddComponent<AudioListener>();
            camObject.AddComponent<TopDownCameraFollow>();
            camObject.transform.position = new Vector3(0f, 24f, -100f);
            camObject.transform.rotation = Quaternion.Euler(56f, 0f, 0f);

            GameObject hud = new GameObject("PlayerHealthHUD");
            Canvas canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hud.AddComponent<CanvasGroup>();
            hud.AddComponent<PlayerHealthUI>();

            GameObject status = new GameObject("HUD");
            Canvas statusCanvas = status.AddComponent<Canvas>();
            statusCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            status.AddComponent<CanvasScaler>();
            GameObject label = new GameObject("StatusLabel", typeof(RectTransform));
            label.transform.SetParent(status.transform, false);
            RectTransform rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(420f, 56f);
            Text text = label.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.color = Color.white;
        }

        private static readonly Vector3[][] EnemyRoutes =
        {
            new[] { new Vector3(-82f, 0f, -40f), new Vector3(-40f, 0f, -40f), new Vector3(-40f, 0f, 0f), new Vector3(-82f, 0f, 0f) },
            new[] { new Vector3(40f, 0f, -40f), new Vector3(82f, 0f, -40f), new Vector3(82f, 0f, 0f), new Vector3(40f, 0f, 0f) },
            new[] { new Vector3(-40f, 0f, 40f), new Vector3(-40f, 0f, 82f), new Vector3(40f, 0f, 82f), new Vector3(40f, 0f, 40f) },
            new[] { new Vector3(-82f, 0f, 0f), new Vector3(-82f, 0f, 82f), new Vector3(-40f, 0f, 82f), new Vector3(-40f, 0f, 0f) },
            new[] { new Vector3(40f, 0f, 0f), new Vector3(82f, 0f, 0f), new Vector3(82f, 0f, 82f), new Vector3(40f, 0f, 82f) },
            new[] { new Vector3(-40f, 0f, -40f), new Vector3(40f, 0f, -40f), new Vector3(40f, 0f, 40f), new Vector3(-40f, 0f, 40f) }
        };

        private static readonly bool[] EnemyHeavy = { false, true, false, true, false, true };

        private static void SpawnTanks()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPolishAssets.PlayerPrefab);
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "PlayerTank";
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, -82f), Quaternion.identity);

            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPolishAssets.EnemyPrefab);
            TankConfig light = ArenaPolishAssets.LoadTank("EnemyLightConfig");
            TankConfig heavy = ArenaPolishAssets.LoadTank("EnemyHeavyConfig");
            Material heavyHull = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/TankHull_EnemyHeavy.mat");
            Material enemyHull = AssetDatabase.LoadAssetAtPath<Material>(ArenaPolishAssets.MaterialsFolder + "/TankHull_Enemy.mat");

            for (int i = 0; i < EnemyRoutes.Length; i++)
            {
                bool isHeavy = EnemyHeavy[i];
                Transform route = new GameObject("AI_Route_" + (i + 1)).transform;
                Transform[] points = new Transform[EnemyRoutes[i].Length];
                for (int p = 0; p < points.Length; p++)
                {
                    points[p] = new GameObject("Waypoint_" + (p + 1)).transform;
                    points[p].SetParent(route, false);
                    points[p].position = EnemyRoutes[i][p];
                }

                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
                enemy.name = "Enemy_" + (i + 1) + (isHeavy ? "_Heavy" : "_Light");
                enemy.tag = "Enemy";
                Vector3 spawn = Vector3.Lerp(EnemyRoutes[i][0], EnemyRoutes[i][1], 0.25f) + Vector3.up * 0.5f;
                Vector3 dir = EnemyRoutes[i][1] - EnemyRoutes[i][0];
                enemy.transform.SetPositionAndRotation(spawn, Quaternion.LookRotation(dir.normalized, Vector3.up));

                TankConfigApplier applier = enemy.GetComponent<TankConfigApplier>();
                applier.SetConfig(isHeavy ? heavy : light);
                PrefabUtility.RecordPrefabInstancePropertyModifications(applier);

                AIController ai = enemy.GetComponent<AIController>();
                ai.SetWaypoints(points);
                PrefabUtility.RecordPrefabInstancePropertyModifications(ai);

                if (isHeavy)
                {
                    foreach (MeshRenderer r in enemy.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (r.sharedMaterial == enemyHull)
                        {
                            r.sharedMaterial = heavyHull;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                        }
                    }

                    enemy.transform.localScale = Vector3.one * 1.1f;
                }
            }
        }

        private static void CreateRepairKits()
        {
            Transform root = new GameObject("RepairKits").transform;
            Vector3[] spots =
            {
                new Vector3(0f, 0.5f, -60f), new Vector3(-60f, 0.5f, 0f), new Vector3(60f, 0.5f, 0f),
                new Vector3(0f, 0.5f, 60f), new Vector3(-82f, 0.5f, 60f), new Vector3(82f, 0.5f, -60f)
            };
            for (int i = 0; i < spots.Length; i++)
            {
                ArenaPolish.CreateRepairKit(root, "RepairKit_" + (i + 1), spots[i]);
            }
        }

        private static void SnapRoutes()
        {
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!go.name.StartsWith("AI_Route")) continue;
                foreach (Transform wp in go.transform)
                {
                    if (NavMesh.SamplePosition(wp.position, out NavMeshHit hit, 5f, NavMesh.AllAreas)) wp.position = hit.position;
                    else Debug.LogWarning("[City] Waypoint off NavMesh: " + go.name + "/" + wp.name);
                }
            }
        }

        #endregion
    }
}
