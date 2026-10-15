using System.Collections.Generic;
using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Procedural sandbag stack generator. Creates a curved row of
    /// individual sandbag meshes with organic shape variation.
    /// </summary>
    public static class SandbagStackGenerator
    {
        public static Mesh Generate(int count = 6, float bagLength = 0.5f, float bagHeight = 0.3f, float bagWidth = 0.2f, float curveRadius = 0f)
        {
            Mesh mesh = new Mesh { name = "SandbagStackMesh" };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int vertStart = 0;

            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                float angle = curveRadius > 0f ? (t - 0.5f) * 0.6f : 0f; // slight curve

                Vector3 center = new Vector3(
                    Mathf.Sin(angle) * curveRadius,
                    bagHeight * 0.5f + i * bagHeight * 0.85f, // slight overlap
                    Mathf.Cos(angle) * curveRadius
                );

                Quaternion rot = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                AddBag(verts, normals, uvs, tris, ref vertStart, center, rot, bagLength, bagHeight, bagWidth);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        private static void AddBag(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris,
            ref int vertStart, Vector3 center, Quaternion rotation, float len, float h, float w)
        {
            float hl = len * 0.5f;
            float hh = h * 0.5f;
            float hw = w * 0.5f;

            // 8 corners of a rounded box (pillow shape)
            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(-hl, -hh, -hw);
            corners[1] = new Vector3(hl, -hh, -hw);
            corners[2] = new Vector3(hl, hh, -hw);
            corners[3] = new Vector3(-hl, hh, -hw);
            corners[4] = new Vector3(-hl, -hh, hw);
            corners[5] = new Vector3(hl, -hh, hw);
            corners[6] = new Vector3(hl, hh, hw);
            corners[7] = new Vector3(-hl, hh, hw);

            // Apply rounding (pillow effect) by pulling corners inward
            float round = Mathf.Min(len, h, w) * 0.15f;
            for (int c = 0; c < 8; c++)
            {
                Vector3 v = corners[c];
                Vector3 sign = new Vector3(
                    v.x >= 0 ? 1 : -1,
                    v.y >= 0 ? 1 : -1,
                    v.z >= 0 ? 1 : -1
                );
                corners[c] = v - sign * round;
            }

            // Transform to world
            for (int c = 0; c < 8; c++)
            {
                corners[c] = center + rotation * corners[c];
            }

            // 6 faces, 4 verts each = 24 verts
            // Face order: -X, +X, -Y, +Y, -Z, +Z
            int[][] faces = new int[6][]
            {
                new int[] { 0, 3, 7, 4 }, // -X
                new int[] { 1, 5, 6, 2 }, // +X
                new int[] { 0, 4, 5, 1 }, // -Y
                new int[] { 3, 2, 6, 7 }, // +Y
                new int[] { 0, 1, 2, 3 }, // -Z
                new int[] { 4, 7, 6, 5 }  // +Z
            };

            Vector3[] faceNormals = new Vector3[]
            {
                Vector3.left, Vector3.right, Vector3.down, Vector3.up, Vector3.back, Vector3.forward
            };

            for (int f = 0; f < 6; f++)
            {
                int baseIdx = vertStart;
                for (int fc = 0; fc < 4; fc++)
                {
                    verts.Add(corners[faces[f][fc]]);
                    normals.Add(rotation * faceNormals[f]);
                    uvs.Add(new Vector2(fc % 2, fc / 2));
                }

                // Two triangles per face
                tris.Add(baseIdx);
                tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 1);
                tris.Add(baseIdx);
                tris.Add(baseIdx + 3);
                tris.Add(baseIdx + 2);

                vertStart += 4;
            }
        }

        public static Mesh GenerateWall(int lengthBags = 8, int heightBags = 3, float stagger = 0.1f)
        {
            // Stacked wall: multiple rows with stagger
            Mesh mesh = new Mesh { name = "SandbagWallMesh" };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int vertStart = 0;
            float bagLength = 0.5f;
            float bagHeight = 0.3f;
            float bagWidth = 0.2f;

            for (int row = 0; row < heightBags; row++)
            {
                for (int i = 0; i < lengthBags; i++)
                {
                    Vector3 center = new Vector3(
                        i * bagLength * 0.9f + (row % 2 == 0 ? 0f : stagger),
                        bagHeight * 0.5f + row * bagHeight * 0.85f,
                        0f
                    );

                    Quaternion rot = Quaternion.identity;
                    AddBag(verts, normals, uvs, tris, ref vertStart, center, rot, bagLength, bagHeight, bagWidth);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}