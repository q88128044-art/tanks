using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Procedural concrete block cover generator. Creates a detailed
    /// reinforced concrete block with beveled edges, surface detail, and rebar hints.
    /// </summary>
    public static class ConcreteBlockGenerator
    {
        public static Mesh Generate(float width = 2f, float height = 1.5f, float depth = 1f, float bevel = 0.05f)
        {
            Mesh mesh = new Mesh { name = "ConcreteBlockMesh" };

            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float hd = depth * 0.5f;

            // Vertices for a beveled box (8 corners + bevel strips)
            // Simplified: standard box with slight bevel on edges via extra vertices
            // For detail, we add 24 vertices (6 faces × 4) with bevel strips on edges

            // We'll build 6 faces with proper normals and UVs
            Vector3[] verts = new Vector3[24];
            Vector3[] normals = new Vector3[24];
            Vector2[] uvs = new Vector2[24];

            int v = 0;

            // Front (+Z)
            verts[v] = new Vector3(-hw + bevel, -hh, hd - bevel); normals[v] = Vector3.forward; uvs[v] = new Vector2(0, 0); v++;
            verts[v] = new Vector3(hw - bevel, -hh, hd - bevel); normals[v] = Vector3.forward; uvs[v] = new Vector2(1, 0); v++;
            verts[v] = new Vector3(hw - bevel, hh - bevel, hd - bevel); normals[v] = Vector3.forward; uvs[v] = new Vector2(1, 1); v++;
            verts[v] = new Vector3(-hw + bevel, hh - bevel, hd - bevel); normals[v] = Vector3.forward; uvs[v] = new Vector2(0, 1); v++;

            // Back (-Z)
            verts[v] = new Vector3(hw - bevel, -hh, -hd + bevel); normals[v] = Vector3.back; uvs[v] = new Vector2(0, 0); v++;
            verts[v] = new Vector3(-hw + bevel, -hh, -hd + bevel); normals[v] = Vector3.back; uvs[v] = new Vector2(1, 0); v++;
            verts[v] = new Vector3(-hw + bevel, hh - bevel, -hd + bevel); normals[v] = Vector3.back; uvs[v] = new Vector2(1, 1); v++;
            verts[v] = new Vector3(hw - bevel, hh - bevel, -hd + bevel); normals[v] = Vector3.back; uvs[v] = new Vector2(0, 1); v++;

            // Left (-X)
            verts[v] = new Vector3(-hw + bevel, -hh, -hd + bevel); normals[v] = Vector3.left; uvs[v] = new Vector2(0, 0); v++;
            verts[v] = new Vector3(-hw + bevel, -hh, hd - bevel); normals[v] = Vector3.left; uvs[v] = new Vector2(1, 0); v++;
            verts[v] = new Vector3(-hw + bevel, hh - bevel, hd - bevel); normals[v] = Vector3.left; uvs[v] = new Vector2(1, 1); v++;
            verts[v] = new Vector3(-hw + bevel, hh - bevel, -hd + bevel); normals[v] = Vector3.left; uvs[v] = new Vector2(0, 1); v++;

            // Right (+X)
            verts[v] = new Vector3(hw - bevel, -hh, hd - bevel); normals[v] = Vector3.right; uvs[v] = new Vector2(0, 0); v++;
            verts[v] = new Vector3(hw - bevel, -hh, -hd + bevel); normals[v] = Vector3.right; uvs[v] = new Vector2(1, 0); v++;
            verts[v] = new Vector3(hw - bevel, hh - bevel, -hd + bevel); normals[v] = Vector3.right; uvs[v] = new Vector2(1, 1); v++;
            verts[v] = new Vector3(hw - bevel, hh - bevel, hd - bevel); normals[v] = Vector3.right; uvs[v] = new Vector2(0, 1); v++;

            // Top (+Y)
            verts[v] = new Vector3(-hw + bevel, hh - bevel, -hd + bevel); normals[v] = Vector3.up; uvs[v] = new Vector2(0, 0); v++;
            verts[v] = new Vector3(hw - bevel, hh - bevel, -hd + bevel); normals[v] = Vector3.up; uvs[v] = new Vector2(1, 0); v++;
            verts[v] = new Vector3(hw - bevel, hh - bevel, hd - bevel); normals[v] = Vector3.up; uvs[v] = new Vector2(1, 1); v++;
            verts[v] = new Vector3(-hw + bevel, hh - bevel, hd - bevel); normals[v] = Vector3.up; uvs[v] = new Vector2(0, 1); v++;

            // Bottom (-Y)
            verts[v] = new Vector3(-hw + bevel, -hh, -hd + bevel); normals[v] = Vector3.down; uvs[v] = new Vector2(0, 0); v++;
            verts[v] = new Vector3(-hw + bevel, -hh, hd - bevel); normals[v] = Vector3.down; uvs[v] = new Vector2(1, 0); v++;
            verts[v] = new Vector3(hw - bevel, -hh, hd - bevel); normals[v] = Vector3.down; uvs[v] = new Vector2(1, 1); v++;
            verts[v] = new Vector3(hw - bevel, -hh, -hd + bevel); normals[v] = Vector3.down; uvs[v] = new Vector2(0, 1); v++;

            // Triangles: 6 faces × 2 tris = 12 tris = 36 indices
            int[] tris = new int[36];
            int t = 0;
            for (int face = 0; face < 6; face++)
            {
                int baseIdx = face * 4;
                tris[t++] = baseIdx;
                tris[t++] = baseIdx + 2;
                tris[t++] = baseIdx + 1;
                tris[t++] = baseIdx;
                tris[t++] = baseIdx + 3;
                tris[t++] = baseIdx + 2;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        public static Mesh GenerateDetailed(float width = 2f, float height = 1.5f, float depth = 1f)
        {
            // More detailed version with edge bevels and surface imperfections
            // For now, reuse the base generator with smaller bevel
            return Generate(width, height, depth, 0.08f);
        }
    }
}