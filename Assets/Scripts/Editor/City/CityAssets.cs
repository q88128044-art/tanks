using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tanks.EditorTools.City
{
    /// <summary>
    /// Procedural textures, materials and meshes for the city map.
    /// Everything is cached as assets under Assets/Game/City.
    /// </summary>
    public static class CityAssets
    {
        public const string Root = "Assets/Game/City";
        public const string TexFolder = Root + "/Textures";
        public const string MatFolder = Root + "/Materials";
        public const string MeshFolder = Root + "/Meshes";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

        public static void Prepare()
        {
            Materials.Clear();
            Meshes.Clear();
            Folder("Assets/Game", "City");
            Folder(Root, "Textures");
            Folder(Root, "Materials");
            Folder(Root, "Meshes");

            Texture2D asphalt = Save("Asphalt", Noise(256, new Color(0.16f, 0.16f, 0.17f), new Color(0.27f, 0.27f, 0.28f), 0.09f, 11, 0.06f));
            Texture2D sidewalk = Save("Sidewalk", Tiles(256, new Color(0.56f, 0.55f, 0.52f), new Color(0.68f, 0.67f, 0.64f), 64));
            Texture2D grass = Save("Grass", Noise(256, new Color(0.2f, 0.33f, 0.14f), new Color(0.34f, 0.48f, 0.2f), 0.07f, 13, 0.03f));
            Texture2D dirt = Save("Dirt", Noise(256, new Color(0.32f, 0.27f, 0.2f), new Color(0.46f, 0.4f, 0.3f), 0.05f, 17, 0.04f));
            Texture2D roof = Save("Roof", Noise(128, new Color(0.22f, 0.22f, 0.23f), new Color(0.34f, 0.33f, 0.33f), 0.2f, 19, 0.08f));
            Texture2D brick = Save("FacadeBrick", Facade(128, new Color(0.52f, 0.27f, 0.2f), true, 23));
            Texture2D plaster = Save("FacadePlaster", Facade(128, new Color(0.78f, 0.7f, 0.56f), false, 29));
            Texture2D panel = Save("FacadePanel", Facade(128, new Color(0.62f, 0.63f, 0.64f), false, 31));
            Texture2D shop = Save("FacadeShop", Shopfront(128));
            Texture2D corrugated = Save("Corrugated", Corrugated(128, 8));
            Texture2D bark = Save("Bark", Noise(64, new Color(0.25f, 0.18f, 0.12f), new Color(0.38f, 0.28f, 0.18f), 0.3f, 37, 0.05f));
            Texture2D leaves = Save("Leaves", Noise(128, new Color(0.15f, 0.3f, 0.1f), new Color(0.32f, 0.5f, 0.18f), 0.18f, 41, 0.1f));

            Lit("Asphalt", asphalt, new Vector2(30f, 30f), Color.white, 0.2f);
            Lit("Sidewalk", sidewalk, new Vector2(15f, 15f), Color.white, 0.1f);
            Lit("Grass", grass, new Vector2(8f, 8f), Color.white, 0.05f);
            Lit("Dirt", dirt, new Vector2(6f, 6f), Color.white, 0.05f);
            Lit("Roof", roof, Vector2.one, Color.white, 0.05f);
            Lit("FacadeBrick", brick, Vector2.one, Color.white, 0.1f);
            Lit("FacadePlaster", plaster, Vector2.one, Color.white, 0.1f);
            Lit("FacadePanel", panel, Vector2.one, Color.white, 0.15f);
            Lit("FacadeShop", shop, Vector2.one, Color.white, 0.3f);
            Lit("FacadeBurnt", plaster, Vector2.one, new Color(0.3f, 0.28f, 0.26f), 0.05f);
            Lit("Warehouse", corrugated, Vector2.one, new Color(0.55f, 0.6f, 0.65f), 0.35f);
            Lit("ContainerRed", corrugated, Vector2.one, new Color(0.65f, 0.18f, 0.14f), 0.3f);
            Lit("ContainerBlue", corrugated, Vector2.one, new Color(0.16f, 0.3f, 0.58f), 0.3f);
            Lit("ContainerGreen", corrugated, Vector2.one, new Color(0.2f, 0.45f, 0.25f), 0.3f);
            Lit("ContainerOrange", corrugated, Vector2.one, new Color(0.85f, 0.45f, 0.12f), 0.3f);
            Lit("Concrete", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Textures/Concrete.png"), Vector2.one, new Color(0.8f, 0.79f, 0.76f), 0.1f);
            Lit("Rubble", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Textures/Concrete.png"), Vector2.one, new Color(0.55f, 0.53f, 0.5f), 0.05f);
            Lit("Bark", bark, Vector2.one, Color.white, 0.05f);
            Lit("Leaves", leaves, Vector2.one, Color.white, 0.1f);
            Lit("LeavesDark", leaves, Vector2.one, new Color(0.7f, 0.8f, 0.65f), 0.1f);
            Lit("Hedge", leaves, new Vector2(3f, 1f), new Color(0.75f, 0.85f, 0.7f), 0.1f);
            Lit("Metal", null, Vector2.one, new Color(0.3f, 0.31f, 0.33f), 0.55f, 0.6f);
            Lit("MetalLight", null, Vector2.one, new Color(0.62f, 0.63f, 0.65f), 0.5f, 0.5f);
            Lit("Rust", null, Vector2.one, new Color(0.35f, 0.2f, 0.12f), 0.15f);
            Lit("Tire", null, Vector2.one, new Color(0.06f, 0.06f, 0.06f), 0.2f);
            Lit("Glass", null, Vector2.one, new Color(0.12f, 0.16f, 0.2f), 0.92f, 0.2f);
            Lit("Burnt", null, Vector2.one, new Color(0.08f, 0.075f, 0.07f), 0.1f);
            Lit("CarRed", null, Vector2.one, new Color(0.6f, 0.08f, 0.07f), 0.7f, 0.3f);
            Lit("CarBlue", null, Vector2.one, new Color(0.1f, 0.2f, 0.45f), 0.7f, 0.3f);
            Lit("CarWhite", null, Vector2.one, new Color(0.82f, 0.82f, 0.8f), 0.7f, 0.3f);
            Lit("CarYellow", null, Vector2.one, new Color(0.85f, 0.65f, 0.1f), 0.7f, 0.3f);
            Lit("CarGreen", null, Vector2.one, new Color(0.18f, 0.32f, 0.2f), 0.6f, 0.3f);
            Lit("CarGrey", null, Vector2.one, new Color(0.35f, 0.36f, 0.38f), 0.6f, 0.3f);
            Lit("RoadLine", null, Vector2.one, new Color(0.85f, 0.85f, 0.8f), 0.2f);
            Lit("RoadLineYellow", null, Vector2.one, new Color(0.85f, 0.7f, 0.15f), 0.2f);
            Lit("Water", null, Vector2.one, new Color(0.12f, 0.28f, 0.35f), 0.95f);
            Lit("Bronze", null, Vector2.one, new Color(0.38f, 0.3f, 0.18f), 0.6f, 0.8f);
            Lit("Stone", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Textures/Concrete.png"), Vector2.one, new Color(0.7f, 0.68f, 0.64f), 0.2f);
            Lit("Wood", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Textures/Wood.png"), Vector2.one, new Color(0.8f, 0.65f, 0.48f), 0.15f);
            Lit("Awning", null, Vector2.one, new Color(0.55f, 0.15f, 0.12f), 0.1f);
            Lit("AwningGreen", null, Vector2.one, new Color(0.15f, 0.4f, 0.25f), 0.1f);
            Emissive("LampLight", new Color(1f, 0.85f, 0.6f), new Color(2.2f, 1.8f, 1.1f));
            Emissive("TrafficRed", new Color(0.8f, 0.1f, 0.1f), new Color(1.5f, 0.1f, 0.1f));
            Emissive("TrafficGreen", new Color(0.1f, 0.7f, 0.2f), new Color(0.1f, 1.2f, 0.3f));
            AssetDatabase.SaveAssets();
        }

        public static Material Mat(string name)
        {
            if (Materials.TryGetValue(name, out Material m) && m != null) return m;
            m = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/" + name + ".mat");
            Materials[name] = m;
            return m;
        }

        #region Materials

        private static void Folder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }

        private static Material GetOrCreate(string name)
        {
            string path = MatFolder + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }

            m.enableInstancing = true;
            Materials[name] = m;
            return m;
        }

        private static Material Lit(string name, Texture2D tex, Vector2 tiling, Color color, float smoothness, float metallic = 0f)
        {
            Material m = GetOrCreate(name);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Emissive(string name, Color color, Color emission)
        {
            Material m = Lit(name, null, Vector2.one, color, 0.5f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        #endregion

        #region Textures

        private static Texture2D Save(string name, Texture2D tex)
        {
            string path = TexFolder + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static float Fbm(float x, float y, float scale, float seed)
        {
            float v = 0f, amp = 0.55f, f = scale;
            for (int o = 0; o < 4; o++)
            {
                v += Mathf.PerlinNoise(seed + x * f, seed * 1.37f + y * f) * amp;
                amp *= 0.5f;
                f *= 2.03f;
            }

            return v;
        }

        private static float Seamless(int x, int y, int size, float scale, float seed)
        {
            return (Fbm(x, y, scale, seed) * (size - x) * (size - y)
                    + Fbm(x - size, y, scale, seed) * x * (size - y)
                    + Fbm(x - size, y - size, scale, seed) * x * y
                    + Fbm(x, y - size, scale, seed) * (size - x) * y) / (size * size);
        }

        private static Texture2D NewTex(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, true);
        }

        public static Texture2D Noise(int size, Color a, Color b, float scale, int seed, float speckle)
        {
            Texture2D tex = NewTex(size);
            System.Random rng = new System.Random(seed);
            float s = 50f + seed * 7.3f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.Clamp01((Seamless(x, y, size, scale, s) - 0.25f) * 1.8f);
                Color c = Color.Lerp(a, b, n);
                float sp = ((float)rng.NextDouble() - 0.5f) * speckle * 2f;
                c.r += sp; c.g += sp; c.b += sp;
                tex.SetPixel(x, y, c);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D Tiles(int size, Color joint, Color tile, int cell)
        {
            Texture2D tex = NewTex(size);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool line = x % cell < 2 || y % cell < 2;
                float n = Seamless(x, y, size, 0.08f, 61f);
                Color c = line ? joint : Color.Lerp(tile * 0.92f, tile * 1.05f, n);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }

            tex.Apply();
            return tex;
        }

        /// <summary>One facade cell = one window bay x one floor.</summary>
        private static Texture2D Facade(int size, Color wall, bool bricks, int seed)
        {
            Texture2D tex = NewTex(size);
            float s = seed * 3.1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;
                float n = Seamless(x, y, size, 0.12f, s);
                Color c = wall * (0.85f + n * 0.3f);

                if (bricks)
                {
                    int row = y / 8;
                    int offset = (row % 2) * 8;
                    if (y % 8 == 0 || (x + offset) % 16 == 0) c = Color.Lerp(c, new Color(0.62f, 0.6f, 0.55f), 0.6f);
                }
                else if (y < 3)
                {
                    c *= 0.82f; // floor joint
                }

                bool frame = u > 0.2f && u < 0.8f && v > 0.26f && v < 0.84f;
                bool glass = u > 0.24f && u < 0.76f && v > 0.3f && v < 0.8f;
                bool sill = u > 0.18f && u < 0.82f && v > 0.22f && v < 0.26f;
                if (sill) c = new Color(0.7f, 0.69f, 0.66f);
                if (frame) c = new Color(0.82f, 0.82f, 0.8f);
                if (glass)
                {
                    float g = Mathf.Lerp(0.1f, 0.28f, v) + (u > 0.49f && u < 0.51f ? 0.4f : 0f);
                    bool mullion = Mathf.Abs(u - 0.5f) < 0.012f || Mathf.Abs(v - 0.62f) < 0.01f;
                    c = mullion ? new Color(0.75f, 0.75f, 0.73f) : new Color(g * 0.7f, g * 0.85f, g * 1.1f);
                }

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D Shopfront(int size)
        {
            Texture2D tex = NewTex(size);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;
                Color c = new Color(0.25f, 0.24f, 0.23f);
                if (v > 0.12f && v < 0.78f && u > 0.06f && u < 0.94f)
                {
                    float g = Mathf.Lerp(0.15f, 0.35f, v);
                    c = new Color(g * 0.8f, g * 0.9f, g);
                    if (Mathf.Abs(u - 0.5f) < 0.015f) c = new Color(0.2f, 0.2f, 0.2f);
                }

                if (v > 0.82f) c = new Color(0.55f, 0.18f, 0.14f);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D Corrugated(int size, int ridges)
        {
            Texture2D tex = NewTex(size);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float ridge = 0.78f + 0.22f * Mathf.Sin(u * ridges * Mathf.PI * 2f);
                float n = Seamless(x, y, size, 0.1f, 71f);
                float v = ridge * (0.9f + n * 0.2f);
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }

            tex.Apply();
            return tex;
        }

        #endregion

        #region Meshes

        /// <summary>
        /// Box building with pivot at the bottom centre. Submesh 0 = walls (UVs in
        /// window bays / floors), submesh 1 = roof. Cached by quantised size.
        /// </summary>
        public static Mesh Building(float w, float h, float d, float bay = 3.5f, float floor = 3f)
        {
            string key = "Bld_" + w.ToString("0.0") + "x" + h.ToString("0.0") + "x" + d.ToString("0.0") + "_" + bay.ToString("0.0") + "_" + floor.ToString("0.0");
            if (Meshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
            string path = MeshFolder + "/" + key + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { Meshes[key] = existing; return existing; }

            float hx = w * 0.5f, hz = d * 0.5f;
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> walls = new List<int>();
            List<int> roof = new List<int>();

            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 dd, Vector3 normal, float uMax, float vMax, List<int> tris)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(dd);
                n.Add(normal); n.Add(normal); n.Add(normal); n.Add(normal);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(uMax, 0)); uv.Add(new Vector2(uMax, vMax)); uv.Add(new Vector2(0, vMax));
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
            }

            float fu = w / bay, su = d / bay, fv = h / floor;
            Face(new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(hx, h, -hz), new Vector3(-hx, h, -hz), Vector3.back, fu, fv, walls);
            Face(new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz), new Vector3(-hx, h, hz), new Vector3(hx, h, hz), Vector3.forward, fu, fv, walls);
            Face(new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz), new Vector3(hx, h, hz), new Vector3(hx, h, -hz), Vector3.right, su, fv, walls);
            Face(new Vector3(-hx, 0, hz), new Vector3(-hx, 0, -hz), new Vector3(-hx, h, -hz), new Vector3(-hx, h, hz), Vector3.left, su, fv, walls);
            Face(new Vector3(-hx, h, -hz), new Vector3(hx, h, -hz), new Vector3(hx, h, hz), new Vector3(-hx, h, hz), Vector3.up, w / 4f, d / 4f, roof);

            Mesh mesh = new Mesh { name = key };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(walls, 0);
            mesh.SetTriangles(roof, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, path);
            Meshes[key] = mesh;
            return mesh;
        }

        #endregion
    }
}
