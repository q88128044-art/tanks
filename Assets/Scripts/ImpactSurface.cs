using UnityEngine;

namespace Tanks
{
    /// <summary>
    /// Marks what an object is made of, so impacts use the right VFX and sound.
    /// </summary>
    public class ImpactSurface : MonoBehaviour
    {
        [SerializeField] private SurfaceType _surface = SurfaceType.Dirt;

        public SurfaceType Surface { get { return _surface; } set { _surface = value; } }

        /// <summary>Surface of a hit collider: explicit ImpactSurface, then tanks/targets = Metal, then by name.</summary>
        public static SurfaceType Resolve(Collider collider)
        {
            if (collider == null)
            {
                return SurfaceType.Dirt;
            }

            ImpactSurface surface = collider.GetComponentInParent<ImpactSurface>();
            if (surface != null)
            {
                return surface.Surface;
            }

            if (collider.GetComponentInParent<IDamageable>() != null)
            {
                return SurfaceType.Metal;
            }

            string n = collider.name.ToLowerInvariant();
            if (n.Contains("ground") || n.Contains("sandbag"))
            {
                return SurfaceType.Dirt;
            }

            if (n.Contains("wood") || n.Contains("crate"))
            {
                return SurfaceType.Wood;
            }

            return SurfaceType.Concrete;
        }
    }
}
