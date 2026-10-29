using UnityEngine;
using UnityEngine.Rendering;

namespace Tanks
{
    /// <summary>
    /// Helpers that build particle materials and procedural textures, shared by
    /// the runtime VFX system and the editor setup tool.
    /// </summary>
    public static class VFXMaterialUtil
    {
        public const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

        public static void ConfigureParticle(Material m, bool additive, Texture texture)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            if (m.HasProperty("_BaseColor"))
            {
                m.SetColor("_BaseColor", Color.white);
            }

            if (texture != null)
            {
                if (m.HasProperty("_BaseMap"))
                {
                    m.SetTexture("_BaseMap", texture);
                }

                m.mainTexture = texture;
            }
        }

        /// <summary>Soft round particle sprite (white, alpha falls off to the edge).</summary>
        public static Texture2D CreateSoftCircle(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "SoftCircle";
            tex.wrapMode = TextureWrapMode.Clamp;
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>Dark, noisy scorch mark used by battle decals.</summary>
        public static Texture2D CreateScorch(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Scorch";
            tex.wrapMode = TextureWrapMode.Clamp;
            float half = (size - 1) * 0.5f;
            float seed = 37.3f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float rays = Mathf.PerlinNoise(seed + Mathf.Cos(angle) * 2.5f, seed + Mathf.Sin(angle) * 2.5f);
                    float noise = Mathf.PerlinNoise(seed + x * 0.06f, seed + y * 0.06f);
                    float edge = 0.55f + rays * 0.45f;
                    float a = Mathf.Clamp01((edge - d) / 0.35f);
                    a *= 0.65f + noise * 0.35f;
                    float c = Mathf.Lerp(0.02f, 0.12f, noise) + (1f - Mathf.Clamp01(d * 1.4f)) * 0.03f;
                    tex.SetPixel(x, y, new Color(c, c * 0.95f, c * 0.9f, a));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
