using UnityEngine;

namespace Tanks
{
    public class ImpactSurface : MonoBehaviour
    {
        [SerializeField] private SurfaceType _surface = SurfaceType.Dirt;

        public SurfaceType Surface { get { return _surface; } set { _surface = value; } }
    }
}