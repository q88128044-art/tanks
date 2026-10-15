using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Assembles a detailed tank model from primitives: hull with glacis plate,
    /// fenders, engine deck, exhausts, headlights, turret with cupola, hatch,
    /// mantlet, barrel with muzzle brake, machine gun, antenna and full track
    /// assemblies with road wheels and sprockets.
    /// Replaces the plain greybox cubes.
    /// </summary>
    public static class TankModelBuilder
    {
        public static void Build(GameObject root, Material hull, Material track, Material barrel, Material detail)
        {
            BuildHull(root.transform, hull, detail);
            BuildTracks(root.transform, track, detail);

            GameObject turret = new GameObject("Turret");
            turret.transform.SetParent(root.transform, false);
            turret.transform.localPosition = new Vector3(0f, 0.45f, 0f);

            BuildTurret(turret.transform, hull, barrel, detail);
        }

        private static void BuildHull(Transform parent, Material hull, Material detail)
        {
            Part(parent, "Hull", PrimitiveType.Cube,
                new Vector3(0f, -0.05f, 0f), new Vector3(1.5f, 0.42f, 2.2f), hull, true);

            Part(parent, "UpperHull", PrimitiveType.Cube,
                new Vector3(0f, 0.2f, -0.15f), new Vector3(1.4f, 0.3f, 1.7f), hull, false);

            GameObject glacis = Part(parent, "Glacis", PrimitiveType.Cube,
                new Vector3(0f, 0.3f, 1.12f), new Vector3(1.44f, 0.08f, 1.05f), hull, false);
            glacis.transform.localRotation = Quaternion.Euler(-52f, 0f, 0f);

            Part(parent, "FenderLeft", PrimitiveType.Cube,
                new Vector3(-0.86f, -0.2f, 0f), new Vector3(0.16f, 0.06f, 2.2f), hull, false);
            Part(parent, "FenderRight", PrimitiveType.Cube,
                new Vector3(0.86f, -0.2f, 0f), new Vector3(0.16f, 0.06f, 2.2f), hull, false);

            Part(parent, "EngineDeck", PrimitiveType.Cube,
                new Vector3(0f, 0.36f, -1.0f), new Vector3(1.15f, 0.18f, 0.55f), hull, false);

            Part(parent, "EngineGrille", PrimitiveType.Cube,
                new Vector3(0f, 0.38f, -1.32f), new Vector3(1.0f, 0.16f, 0.1f), detail, false);

            Part(parent, "ExhaustLeft", PrimitiveType.Cylinder,
                new Vector3(-0.5f, 0.44f, -1.25f), new Vector3(0.16f, 0.07f, 0.16f), detail, false);
            Part(parent, "ExhaustRight", PrimitiveType.Cylinder,
                new Vector3(0.5f, 0.44f, -1.25f), new Vector3(0.16f, 0.07f, 0.16f), detail, false);

            Part(parent, "TowHookLeft", PrimitiveType.Cube,
                new Vector3(-0.3f, -0.14f, 1.12f), new Vector3(0.08f, 0.06f, 0.12f), detail, false);
            Part(parent, "TowHookRight", PrimitiveType.Cube,
                new Vector3(0.3f, -0.14f, 1.12f), new Vector3(0.08f, 0.06f, 0.12f), detail, false);

            Part(parent, "HeadlightLeft", PrimitiveType.Sphere,
                new Vector3(-0.62f, 0.24f, 1.1f), new Vector3(0.12f, 0.12f, 0.12f), detail, false);
            Part(parent, "HeadlightRight", PrimitiveType.Sphere,
                new Vector3(0.62f, 0.24f, 1.1f), new Vector3(0.12f, 0.12f, 0.12f), detail, false);

            Part(parent, "SpareTrackLeft", PrimitiveType.Cube,
                new Vector3(-0.7f, 0.48f, -0.95f), new Vector3(0.06f, 0.22f, 0.7f), hull, false);
            Part(parent, "SpareTrackRight", PrimitiveType.Cube,
                new Vector3(0.7f, 0.48f, -0.95f), new Vector3(0.06f, 0.22f, 0.7f), hull, false);
        }

        private static void BuildTurret(Transform parent, Material hull, Material barrel, Material detail)
        {
            GameObject body = Part(parent, "TurretBody", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0f), new Vector3(1.15f, 0.38f, 1.15f), hull, true);

            // Beveled cheeks give the turret a less boxy silhouette.
            GameObject cheekLeft = Part(parent, "TurretCheekLeft", PrimitiveType.Cube,
                new Vector3(-0.5f, -0.06f, 0.02f), new Vector3(0.3f, 0.3f, 1.05f), hull, false);
            cheekLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            GameObject cheekRight = Part(parent, "TurretCheekRight", PrimitiveType.Cube,
                new Vector3(0.5f, -0.06f, 0.02f), new Vector3(0.3f, 0.3f, 1.05f), hull, false);
            cheekRight.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);

            Part(parent, "TurretRoof", PrimitiveType.Cube,
                new Vector3(0f, 0.21f, -0.12f), new Vector3(0.92f, 0.1f, 0.9f), hull, false);

            Part(parent, "TurretRing", PrimitiveType.Cylinder,
                new Vector3(0f, -0.19f, 0f), new Vector3(1.22f, 0.05f, 1.22f), detail, false);

            Part(parent, "Mantlet", PrimitiveType.Cube,
                new Vector3(0f, 0.05f, 0.48f), new Vector3(0.44f, 0.32f, 0.34f), barrel, false);

            GameObject barrelTube = Part(parent, "Barrel", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 1.35f), new Vector3(0.13f, 0.95f, 0.13f), barrel, false);
            barrelTube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Part(parent, "MuzzleBrake", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 2.35f), new Vector3(0.2f, 0.11f, 0.2f), barrel, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Part(parent, "BarrelJacket", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 2.0f), new Vector3(0.17f, 0.16f, 0.17f), detail, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Part(parent, "Cupola", PrimitiveType.Cylinder,
                new Vector3(0.3f, 0.33f, -0.28f), new Vector3(0.3f, 0.07f, 0.3f), detail, false);
            Part(parent, "CupolaGunSight", PrimitiveType.Sphere,
                new Vector3(0.3f, 0.42f, -0.28f), new Vector3(0.1f, 0.1f, 0.1f), detail, false);

            Part(parent, "Hatch", PrimitiveType.Cube,
                new Vector3(-0.3f, 0.29f, -0.16f), new Vector3(0.34f, 0.05f, 0.34f), hull, false);

            Part(parent, "MachineGun", PrimitiveType.Cube,
                new Vector3(-0.45f, 0.3f, 0.3f), new Vector3(0.06f, 0.06f, 0.5f), detail, false);

            Part(parent, "AmmoBox", PrimitiveType.Cube,
                new Vector3(-0.42f, 0.06f, -0.5f), new Vector3(0.16f, 0.2f, 0.22f), detail, false);

            Part(parent, "Antenna", PrimitiveType.Cylinder,
                new Vector3(-0.46f, 0.62f, -0.55f), new Vector3(0.02f, 0.45f, 0.02f), detail, false);

            Part(parent, "AntennaBase", PrimitiveType.Cylinder,
                new Vector3(-0.46f, 0.24f, -0.55f), new Vector3(0.07f, 0.03f, 0.07f), detail, false);

            Part(parent, "SmokeLauncherLeft", PrimitiveType.Cylinder,
                new Vector3(-0.42f, 0.18f, 0.2f), new Vector3(0.08f, 0.1f, 0.08f), detail, false);
            Part(parent, "SmokeLauncherRight", PrimitiveType.Cylinder,
                new Vector3(0.42f, 0.18f, 0.2f), new Vector3(0.08f, 0.1f, 0.08f), detail, false);

            // Empty marker at the end of the barrel where projectiles spawn.
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(parent, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.05f, 2.55f);
        }

        private static void BuildTracks(Transform parent, Material track, Material detail)
        {
            BuildTrackAssembly(parent, "TrackLeft", -0.88f, track, detail);
            BuildTrackAssembly(parent, "TrackRight", 0.88f, track, detail);
        }

        private static void BuildTrackAssembly(Transform parent, string name, float x, Material track, Material detail)
        {
            GameObject assembly = new GameObject(name);
            assembly.transform.SetParent(parent, false);
            assembly.transform.localPosition = new Vector3(x, 0f, 0f);

            Part(assembly.transform, name + "_Belt", PrimitiveType.Cube,
                new Vector3(0f, -0.18f, 0f), new Vector3(0.3f, 0.46f, 2.7f), track, true);

            Part(assembly.transform, name + "_SideSkirt", PrimitiveType.Cube,
                new Vector3(x > 0f ? 0.12f : -0.12f, 0.06f, 0f), new Vector3(0.04f, 0.4f, 2.4f), track, false);

            // Road wheels, sprocket and idler share the belt bottom (-0.41) so
            // nothing pokes through the ground when the tank rests on the navmesh.
            float wheelBottom = -0.41f;
            float[] wheelZ = { -0.85f, -0.42f, 0f, 0.42f, 0.85f };
            foreach (float z in wheelZ)
            {
                Part(assembly.transform, name + "_Wheel" + z.ToString("F2"), PrimitiveType.Cylinder,
                    new Vector3(0f, wheelBottom + 0.42f, z), new Vector3(0.42f, 0.04f, 0.42f), detail, false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            Part(assembly.transform, name + "_Sprocket", PrimitiveType.Cylinder,
                new Vector3(0f, wheelBottom + 0.5f, -1.32f), new Vector3(0.5f, 0.05f, 0.5f), detail, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            Part(assembly.transform, name + "_Idler", PrimitiveType.Cylinder,
                new Vector3(0f, wheelBottom + 0.46f, 1.32f), new Vector3(0.46f, 0.05f, 0.46f), detail, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            Part(assembly.transform, name + "_ReturnRoller", PrimitiveType.Cylinder,
                new Vector3(0f, 0.06f, 0f), new Vector3(1.25f, 0.15f, 0.075f), detail, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 scale, Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;

            if (go.TryGetComponent(out Renderer r))
            {
                r.sharedMaterial = material;
            }

            if (!collider && go.TryGetComponent(out Collider c))
            {
                Object.DestroyImmediate(c);
            }

            return go;
        }
    }
}