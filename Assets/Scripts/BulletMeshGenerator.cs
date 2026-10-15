using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Detailed bullet mesh generator. Creates a realistic bullet shape
    /// (ogive nose, cylindrical body, boat-tail base) entirely in code.
    /// </summary>
    public static class BulletMeshGenerator
    {
        public static Mesh Generate(float length = 0.3f, float diameter = 0.06f, int radialSegments = 12, float ogiveRatio = 0.6f)
        {
            Mesh mesh = new Mesh { name = "BulletMesh" };

            int segments = Mathf.Max(8, radialSegments);
            float radius = diameter * 0.5f;
            float ogiveLength = length * ogiveRatio;
            float bodyLength = length - ogiveLength;

            // Vertex layout:
            // [0] tip
            // [1..segments] ogive ring
            // [segments+1..2*segments] body ring
            // [2*segments+1] base center (for flat base) or boat-tail tip

            int vertsPerRing = segments;
            int totalVerts = 1 + vertsPerRing * 2 + 1; // tip + ogive ring + body ring + base center

            Vector3[] verts = new Vector3[totalVerts];
            Vector3[] normals = new Vector3[totalVerts];
            Vector2[] uvs = new Vector2[totalVerts];

            // Tip
            verts[0] = new Vector3(0f, 0f, length * 0.5f);
            normals[0] = Vector3.forward;
            uvs[0] = new Vector2(0.5f, 1f);

            // Ogive ring (nose curve)
            for (int i = 0; i < segments; i++)
            {
                float ang = (float)i / segments * Mathf.PI * 2f;
                float noseT = (float)i / segments;
                float zPos = length * 0.5f - ogiveLength * (1f - Mathf.Cos(noseT * Mathf.PI * 0.5f));
                float rAtZ = radius * Mathf.Sin(noseT * Mathf.PI * 0.5f);

                int idx = 1 + i;
                verts[idx] = new Vector3(Mathf.Cos(ang) * rAtZ, Mathf.Sin(ang) * rAtZ, zPos);
                normals[idx] = Vector3.Normalize(new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), -1f));
                uvs[idx] = new Vector2(noseT, 0.5f);
            }

            // Body ring (cylindrical mid-section)
            float bodyStartZ = -length * 0.5f + bodyLength;
            for (int i = 0; i < segments; i++)
            {
                float ang = (float)i / segments * Mathf.PI * 2f;
                int idx = 1 + segments + i;
                verts[idx] = new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, bodyStartZ);
                normals[idx] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                uvs[idx] = new Vector2((float)i / segments, 0f);
            }

            // Base center (flat base)
            verts[totalVerts - 1] = new Vector3(0f, 0f, -length * 0.5f);
            normals[totalVerts - 1] = Vector3.back;
            uvs[totalVerts - 1] = new Vector2(0.5f, 0f);

            // Triangles
            // Tip fan (ogive)
            int triCount = segments * 2 * 3 + segments * 2 * 3; // ogive + body + base
            int[] tris = new int[triCount];
            int t = 0;

            // Ogive fan from tip
            for (int i = 0; i < segments; i++)
            {
                int a = 0;
                int b = 1 + i;
                int c = 1 + (i + 1) % segments;
                tris[t++] = a;
                tris[t++] = c;
                tris[t++] = b; // clockwise for outside
            }

            // Ogive to body quads
            for (int i = 0; i < segments; i++)
            {
                int ogiveA = 1 + i;
                int ogiveB = 1 + (i + 1) % segments;
                int bodyA = 1 + segments + i;
                int bodyB = 1 + segments + (i + 1) % segments;

                // Two triangles per quad
                tris[t++] = ogiveA;
                tris[t++] = bodyA;
                tris[t++] = ogiveB;

                tris[t++] = ogiveB;
                tris[t++] = bodyA;
                tris[t++] = bodyB;
            }

            // Body to base quads (flat base fan)
            for (int i = 0; i < segments; i++)
            {
                int bodyA = 1 + segments + i;
                int bodyB = 1 + segments + (i + 1) % segments;
                int baseCenter = totalVerts - 1;

                tris[t++] = bodyA;
                tris[t++] = baseCenter;
                tris[t++] = bodyB;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        public static Mesh GenerateSimple(float length = 0.3f, float diameter = 0.06f)
        {
            // Fallback: capsule-like simple bullet
            Mesh mesh = new Mesh { name = "BulletMeshSimple" };
            Vector3[] verts = new Vector3[4];
            verts[0] = new Vector3(0f, 0f, length * 0.5f); // tip
            verts[1] = new Vector3(diameter * 0.5f, 0f, -length * 0.5f); // base right
            verts[2] = new Vector3(-diameter * 0.5f, 0f, -length * 0.5f); // base left
            verts[3] = new Vector3(0f, diameter * 0.5f, -length * 0.5f); // base top (for volume)

            int[] tris = { 0, 2, 1, 0, 1, 3, 0, 3, 2, 1, 2, 3 };
            Vector3[] norms = new Vector3[4];
            norms[0] = Vector3.forward;
            norms[1] = Vector3.right;
            norms[2] = Vector3.left;
            norms[3] = Vector3.up;

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.normals = norms;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}