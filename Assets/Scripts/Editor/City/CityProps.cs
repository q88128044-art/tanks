using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tanks.EditorTools.City
{
    /// <summary>
    /// Procedural props for the city: buildings, trees, lamps, cars, containers,
    /// barriers, hedgehogs, rubble, fountain, monument, fences, bus stops...
    /// Only gameplay-relevant objects get colliders (one simple collider per prop).
    /// </summary>
    public static class CityProps
    {
        private static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();

        public static void ResetCache()
        {
            MeshCache.Clear();
        }

        #region Helpers

        public static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, string mat,
            Quaternion? rotation = null, bool collider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = CityAssets.Mat(mat);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Node(Transform parent, string name, Vector3 position, float rotY = 0f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            return go;
        }

        private static BoxCollider Box(GameObject go, Vector3 center, Vector3 size)
        {
            BoxCollider c = go.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
            return c;
        }

        private static void Surface(GameObject go, SurfaceType surface)
        {
            go.AddComponent<ImpactSurface>().Surface = surface;
        }

        private static Mesh Cached(string key, System.Func<Mesh> create)
        {
            if (MeshCache.TryGetValue(key, out Mesh m) && m != null) return m;
            string path = CityAssets.MeshFolder + "/" + key + ".asset";
            m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m == null)
            {
                m = create();
                m.name = key;
                AssetDatabase.CreateAsset(m, path);
            }

            MeshCache[key] = m;
            return m;
        }

        private static GameObject MeshObject(Transform parent, string name, Mesh mesh, string mat, Vector3 localPos, float rotY = 0f)
        {
            GameObject go = Node(parent, name, localPos, rotY);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = CityAssets.Mat(mat);
            return go;
        }

        #endregion

        #region Buildings

        /// <summary>Building whose local -Z side faces the street.</summary>
        public static GameObject Building(Transform parent, Vector3 position, float rotY, float width, float depth, int floors,
            string facade, System.Random rng, bool shop, bool damaged = false)
        {
            float w = Mathf.Max(3.5f, Mathf.Round(width / 3.5f) * 3.5f);
            float d = Mathf.Max(3f, depth);
            float h = floors * 3f;

            GameObject root = Node(parent, "Building_" + floors + "F", position, rotY);
            root.AddComponent<MeshFilter>().sharedMesh = CityAssets.Building(w, h, d);
            root.AddComponent<MeshRenderer>().sharedMaterials = new[] { CityAssets.Mat(damaged ? "FacadeBurnt" : facade), CityAssets.Mat("Roof") };
            Box(root, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d));
            Surface(root, SurfaceType.Concrete);

            // Parapet around the roof edge.
            float p = 0.5f;
            Prim(root.transform, PrimitiveType.Cube, "ParapetF", new Vector3(0f, h + p * 0.5f, -d * 0.5f + 0.15f), new Vector3(w, p, 0.3f), "Concrete");
            Prim(root.transform, PrimitiveType.Cube, "ParapetB", new Vector3(0f, h + p * 0.5f, d * 0.5f - 0.15f), new Vector3(w, p, 0.3f), "Concrete");
            Prim(root.transform, PrimitiveType.Cube, "ParapetL", new Vector3(-w * 0.5f + 0.15f, h + p * 0.5f, 0f), new Vector3(0.3f, p, d - 0.6f), "Concrete");
            Prim(root.transform, PrimitiveType.Cube, "ParapetR", new Vector3(w * 0.5f - 0.15f, h + p * 0.5f, 0f), new Vector3(0.3f, p, d - 0.6f), "Concrete");

            if (shop && !damaged)
            {
                Prim(root.transform, PrimitiveType.Cube, "Shopfront", new Vector3(0f, 1.6f, -d * 0.5f - 0.06f), new Vector3(w - 0.6f, 3.1f, 0.1f), "FacadeShop");
                Prim(root.transform, PrimitiveType.Cube, "Awning", new Vector3(0f, 3.25f, -d * 0.5f - 0.7f), new Vector3(w - 1f, 0.12f, 1.4f),
                    rng.NextDouble() < 0.5 ? "Awning" : "AwningGreen", Quaternion.Euler(-12f, 0f, 0f));
            }

            int roofProps = rng.Next(1, 4);
            for (int i = 0; i < roofProps; i++)
            {
                float x = Mathf.Lerp(-w * 0.3f, w * 0.3f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-d * 0.3f, d * 0.3f, (float)rng.NextDouble());
                int kind = rng.Next(3);
                if (kind == 0)
                    Prim(root.transform, PrimitiveType.Cube, "AC", new Vector3(x, h + 0.5f, z), new Vector3(1.4f, 1f, 1f), "MetalLight");
                else if (kind == 1)
                    Prim(root.transform, PrimitiveType.Cylinder, "WaterTank", new Vector3(x, h + 1.1f, z), new Vector3(1.6f, 1.1f, 1.6f), "Rust");
                else
                    Prim(root.transform, PrimitiveType.Cylinder, "Antenna", new Vector3(x, h + 1.5f, z), new Vector3(0.08f, 1.5f, 0.08f), "Metal");
            }

            if (damaged)
            {
                // Collapsed corner + rubble at the base.
                Prim(root.transform, PrimitiveType.Cube, "Hole", new Vector3(w * 0.25f, h - 1.2f, -d * 0.5f + 0.4f), new Vector3(w * 0.4f, 2.6f, 0.9f), "Burnt");
                Rubble(root.transform, new Vector3(w * 0.2f, 0f, -d * 0.5f - 1.5f), 2.2f, rng, false);
            }

            return root;
        }

        #endregion

        #region Nature

        public static GameObject Tree(Transform parent, Vector3 position, System.Random rng, float scale = 1f)
        {
            float s = scale * Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble());
            GameObject root = Node(parent, "Tree", position, (float)rng.NextDouble() * 360f);
            Prim(root.transform, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, 1.3f * s, 0f), new Vector3(0.32f * s, 1.3f * s, 0.32f * s), "Bark");
            string leaves = rng.NextDouble() < 0.5 ? "Leaves" : "LeavesDark";
            Prim(root.transform, PrimitiveType.Sphere, "Crown", new Vector3(0f, 3.4f * s, 0f), new Vector3(2.8f, 2.4f, 2.8f) * s, leaves);
            Prim(root.transform, PrimitiveType.Sphere, "Crown2", new Vector3(0.6f * s, 4.3f * s, 0.3f * s), new Vector3(1.9f, 1.7f, 1.9f) * s, leaves);
            Prim(root.transform, PrimitiveType.Sphere, "Crown3", new Vector3(-0.7f * s, 3.9f * s, -0.4f * s), new Vector3(1.7f, 1.5f, 1.7f) * s, leaves);
            CapsuleCollider c = root.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 1.5f * s, 0f);
            c.radius = 0.35f * s;
            c.height = 3f * s;
            Surface(root, SurfaceType.Wood);
            return root;
        }

        public static GameObject Hedge(Transform parent, Vector3 position, float rotY, float length)
        {
            GameObject h = Prim(parent, PrimitiveType.Cube, "Hedge", position + Vector3.up * 0.55f, new Vector3(length, 1.1f, 0.9f), "Hedge",
                Quaternion.Euler(0f, rotY, 0f), true);
            Surface(h, SurfaceType.Wood);
            return h;
        }

        #endregion

        #region Street furniture

        public static GameObject StreetLamp(Transform parent, Vector3 position, float rotY)
        {
            GameObject root = Node(parent, "StreetLamp", position, rotY);
            Prim(root.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 3f, 0f), new Vector3(0.14f, 3f, 0.14f), "Metal");
            Prim(root.transform, PrimitiveType.Cube, "Arm", new Vector3(0f, 5.9f, 0.7f), new Vector3(0.1f, 0.1f, 1.5f), "Metal");
            Prim(root.transform, PrimitiveType.Cube, "Head", new Vector3(0f, 5.8f, 1.4f), new Vector3(0.35f, 0.15f, 0.6f), "LampLight");
            CapsuleCollider c = root.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 3f, 0f);
            c.radius = 0.15f;
            c.height = 6f;
            Surface(root, SurfaceType.Metal);
            return root;
        }

        public static GameObject TrafficLight(Transform parent, Vector3 position, float rotY, bool green)
        {
            GameObject root = Node(parent, "TrafficLight", position, rotY);
            Prim(root.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 1.8f, 0f), new Vector3(0.12f, 1.8f, 0.12f), "Metal");
            Prim(root.transform, PrimitiveType.Cube, "Box", new Vector3(0f, 3.9f, 0f), new Vector3(0.4f, 1.1f, 0.35f), "Metal");
            Prim(root.transform, PrimitiveType.Sphere, "Lamp", new Vector3(0f, green ? 3.6f : 4.2f, -0.18f), Vector3.one * 0.22f, green ? "TrafficGreen" : "TrafficRed");
            return root;
        }

        public static GameObject Bench(Transform parent, Vector3 position, float rotY)
        {
            GameObject root = Node(parent, "Bench", position, rotY);
            Prim(root.transform, PrimitiveType.Cube, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.08f, 0.5f), "Wood");
            Prim(root.transform, PrimitiveType.Cube, "Back", new Vector3(0f, 0.8f, 0.22f), new Vector3(1.8f, 0.4f, 0.06f), "Wood");
            Prim(root.transform, PrimitiveType.Cube, "LegL", new Vector3(-0.75f, 0.22f, 0f), new Vector3(0.08f, 0.45f, 0.45f), "Metal");
            Prim(root.transform, PrimitiveType.Cube, "LegR", new Vector3(0.75f, 0.22f, 0f), new Vector3(0.08f, 0.45f, 0.45f), "Metal");
            return root;
        }

        public static GameObject BusStop(Transform parent, Vector3 position, float rotY)
        {
            GameObject root = Node(parent, "BusStop", position, rotY);
            Prim(root.transform, PrimitiveType.Cube, "Roof", new Vector3(0f, 2.6f, 0f), new Vector3(4f, 0.12f, 1.6f), "MetalLight");
            Prim(root.transform, PrimitiveType.Cube, "Back", new Vector3(0f, 1.4f, 0.7f), new Vector3(4f, 2.3f, 0.06f), "Glass");
            Prim(root.transform, PrimitiveType.Cube, "PostL", new Vector3(-1.9f, 1.3f, 0.7f), new Vector3(0.1f, 2.6f, 0.1f), "Metal");
            Prim(root.transform, PrimitiveType.Cube, "PostR", new Vector3(1.9f, 1.3f, 0.7f), new Vector3(0.1f, 2.6f, 0.1f), "Metal");
            Bench(root.transform, new Vector3(0f, 0f, 0.35f), 180f);
            Box(root, new Vector3(0f, 1.3f, 0.3f), new Vector3(4f, 2.6f, 1.2f));
            Surface(root, SurfaceType.Metal);
            return root;
        }

        public static GameObject Fence(Transform parent, Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float length = dir.magnitude;
            GameObject root = Node(parent, "Fence", (from + to) * 0.5f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
            int posts = Mathf.Max(2, Mathf.CeilToInt(length / 2.5f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float z = Mathf.Lerp(-length * 0.5f, length * 0.5f, i / (float)(posts - 1));
                Prim(root.transform, PrimitiveType.Cube, "Post", new Vector3(0f, 1f, z), new Vector3(0.1f, 2f, 0.1f), "Metal");
            }

            Prim(root.transform, PrimitiveType.Cube, "RailTop", new Vector3(0f, 1.9f, 0f), new Vector3(0.05f, 0.05f, length), "MetalLight");
            Prim(root.transform, PrimitiveType.Cube, "RailMid", new Vector3(0f, 1f, 0f), new Vector3(0.05f, 0.05f, length), "MetalLight");
            Prim(root.transform, PrimitiveType.Cube, "Mesh", new Vector3(0f, 1f, 0f), new Vector3(0.02f, 1.8f, length), "Metal");
            Box(root, new Vector3(0f, 1f, 0f), new Vector3(0.3f, 2f, length));
            Surface(root, SurfaceType.Metal);
            return root;
        }

        #endregion

        #region Vehicles & cover

        public static GameObject Car(Transform parent, Vector3 position, float rotY, string paint, bool burnt)
        {
            string body = burnt ? "Burnt" : paint;
            GameObject root = Node(parent, burnt ? "CarWreck" : "Car", position, rotY);
            Prim(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(1.85f, 0.7f, 4.3f), body);
            Prim(root.transform, PrimitiveType.Cube, "Cabin", new Vector3(0f, 1.2f, -0.25f), new Vector3(1.65f, 0.6f, 2.2f), burnt ? "Burnt" : "Glass");
            Prim(root.transform, PrimitiveType.Cube, "RoofPanel", new Vector3(0f, 1.52f, -0.25f), new Vector3(1.6f, 0.06f, 1.9f), body);
            float[] xs = { -0.85f, 0.85f };
            float[] zs = { -1.35f, 1.35f };
            foreach (float x in xs)
            foreach (float z in zs)
                Prim(root.transform, PrimitiveType.Cylinder, "Wheel", new Vector3(x, 0.35f, z), new Vector3(0.7f, 0.12f, 0.7f), burnt ? "Rust" : "Tire", Quaternion.Euler(0f, 0f, 90f));
            if (burnt) root.transform.localRotation *= Quaternion.Euler(0f, 0f, 2f);
            Box(root, new Vector3(0f, 0.8f, 0f), new Vector3(1.9f, 1.6f, 4.3f));
            Surface(root, SurfaceType.Metal);
            return root;
        }

        public static GameObject Container(Transform parent, Vector3 position, float rotY, string mat)
        {
            GameObject c = Prim(parent, PrimitiveType.Cube, "Container", position + Vector3.up * 1.3f, new Vector3(2.44f, 2.6f, 6.06f), mat,
                Quaternion.Euler(0f, rotY, 0f), true);
            Surface(c, SurfaceType.Metal);
            return c;
        }

        public static GameObject FuelTank(Transform parent, Vector3 position)
        {
            GameObject root = Node(parent, "FuelTank", position);
            GameObject tank = Prim(root.transform, PrimitiveType.Cylinder, "Tank", new Vector3(0f, 2.5f, 0f), new Vector3(5f, 2.5f, 5f), "MetalLight", null, true);
            Prim(root.transform, PrimitiveType.Cylinder, "Ring", new Vector3(0f, 4.6f, 0f), new Vector3(5.15f, 0.08f, 5.15f), "Rust");
            Prim(root.transform, PrimitiveType.Cube, "Ladder", new Vector3(0f, 2.5f, -2.55f), new Vector3(0.5f, 5f, 0.1f), "Metal");
            Surface(tank, SurfaceType.Metal);
            return root;
        }

        public static GameObject Barrier(Transform parent, Vector3 position, float rotY)
        {
            Mesh mesh = Cached("JerseyBarrier", () => ConcreteBlockGenerator.Generate(2.2f, 0.9f, 0.7f, 0.12f));
            GameObject go = MeshObject(parent, "Barrier", mesh, "Concrete", position + Vector3.up * 0.45f, rotY);
            Box(go, Vector3.zero, new Vector3(2.2f, 0.9f, 0.7f));
            Surface(go, SurfaceType.Concrete);
            return go;
        }

        public static GameObject Sandbags(Transform parent, Vector3 position, float rotY)
        {
            Mesh mesh = Cached("CitySandbags", () => SandbagStackGenerator.GenerateWall(5, 3, 0.12f));
            GameObject go = MeshObject(parent, "Sandbags", mesh, "Dirt", position, rotY);
            go.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/SandbagCover.mat");
            Box(go, mesh.bounds.center, mesh.bounds.size);
            Surface(go, SurfaceType.Dirt);
            return go;
        }

        public static GameObject Hedgehog(Transform parent, Vector3 position, float rotY)
        {
            GameObject root = Node(parent, "Hedgehog", position + Vector3.up * 0.7f, rotY);
            Prim(root.transform, PrimitiveType.Cube, "BeamA", Vector3.zero, new Vector3(2.2f, 0.16f, 0.16f), "Rust", Quaternion.Euler(0f, 0f, 45f));
            Prim(root.transform, PrimitiveType.Cube, "BeamB", Vector3.zero, new Vector3(2.2f, 0.16f, 0.16f), "Rust", Quaternion.Euler(0f, 90f, -45f));
            Prim(root.transform, PrimitiveType.Cube, "BeamC", Vector3.zero, new Vector3(0.16f, 2.2f, 0.16f), "Rust", Quaternion.Euler(45f, 0f, 0f));
            Box(root, Vector3.zero, new Vector3(1.6f, 1.4f, 1.6f));
            Surface(root, SurfaceType.Metal);
            return root;
        }

        public static GameObject Rubble(Transform parent, Vector3 position, float radius, System.Random rng, bool collider = true)
        {
            GameObject root = Node(parent, "Rubble", position);
            int count = 6 + rng.Next(5);
            for (int i = 0; i < count; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = (float)rng.NextDouble() * radius;
                float s = Mathf.Lerp(0.4f, 1.3f, (float)rng.NextDouble());
                Prim(root.transform, PrimitiveType.Cube, "Chunk", new Vector3(Mathf.Cos(a) * r, s * 0.3f, Mathf.Sin(a) * r),
                    new Vector3(s, s * 0.6f, s * Mathf.Lerp(0.6f, 1.4f, (float)rng.NextDouble())), i % 3 == 0 ? "Burnt" : "Rubble",
                    Quaternion.Euler((float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f));
            }

            if (collider)
            {
                Box(root, new Vector3(0f, 0.5f, 0f), new Vector3(radius * 1.6f, 1f, radius * 1.6f));
                Surface(root, SurfaceType.Concrete);
            }

            return root;
        }

        public static GameObject RuinWall(Transform parent, Vector3 position, float rotY, float length, float height, System.Random rng)
        {
            GameObject root = Node(parent, "RuinWall", position, rotY);
            int pieces = Mathf.Max(2, Mathf.RoundToInt(length / 2f));
            float seg = length / pieces;
            for (int i = 0; i < pieces; i++)
            {
                float h = height * Mathf.Lerp(0.35f, 1f, (float)rng.NextDouble());
                float x = -length * 0.5f + seg * (i + 0.5f);
                Prim(root.transform, PrimitiveType.Cube, "Piece", new Vector3(x, h * 0.5f, 0f), new Vector3(seg * 0.98f, h, 0.5f),
                    i % 2 == 0 ? "FacadeBurnt" : "Rubble");
            }

            Box(root, new Vector3(0f, height * 0.5f, 0f), new Vector3(length, height, 0.6f));
            Surface(root, SurfaceType.Concrete);
            return root;
        }

        #endregion

        #region Landmarks

        public static GameObject Fountain(Transform parent, Vector3 position)
        {
            GameObject root = Node(parent, "Fountain", position);
            GameObject basin = Prim(root.transform, PrimitiveType.Cylinder, "Basin", new Vector3(0f, 0.35f, 0f), new Vector3(7f, 0.35f, 7f), "Stone", null, true);
            Prim(root.transform, PrimitiveType.Cylinder, "Water", new Vector3(0f, 0.62f, 0f), new Vector3(6.5f, 0.02f, 6.5f), "Water");
            Prim(root.transform, PrimitiveType.Cylinder, "Column", new Vector3(0f, 1.3f, 0f), new Vector3(0.8f, 1f, 0.8f), "Stone");
            Prim(root.transform, PrimitiveType.Cylinder, "Bowl", new Vector3(0f, 2.3f, 0f), new Vector3(2.4f, 0.15f, 2.4f), "Stone");
            Surface(basin, SurfaceType.Concrete);
            return root;
        }

        public static GameObject Monument(Transform parent, Vector3 position)
        {
            GameObject root = Node(parent, "Monument", position);
            GameObject pedestal = Prim(root.transform, PrimitiveType.Cube, "Pedestal", new Vector3(0f, 1.2f, 0f), new Vector3(5f, 2.4f, 7f), "Stone", null, true);
            Prim(root.transform, PrimitiveType.Cube, "Step", new Vector3(0f, 0.15f, 0f), new Vector3(7f, 0.3f, 9f), "Stone");
            Surface(pedestal, SurfaceType.Concrete);

            GameObject statue = new GameObject("TankStatue");
            statue.transform.SetParent(root.transform, false);
            Material bronze = CityAssets.Mat("Bronze");
            TankModelBuilder.Build(statue, bronze, bronze, bronze, bronze);
            foreach (Collider c in statue.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            statue.transform.localPosition = new Vector3(0f, 2.4f + 0.45f, 0f);
            statue.transform.localScale = Vector3.one * 1.1f;
            Transform turret = statue.transform.Find("Turret");
            if (turret != null) turret.localRotation = Quaternion.Euler(0f, 30f, 0f);
            return root;
        }

        #endregion
    }
}
