using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures.StainRestoration
{
    [DisallowMultipleComponent]
    public sealed class StainRestorationSurface :
        MonoBehaviour
    {
        private static readonly List<StainRestorationSurface>
            ActiveSurfaceList =
                new List<StainRestorationSurface>();

        [SerializeField]
        private Transform restorationPoint;

        [SerializeField]
        private bool restorationEnabled = true;

        public static IReadOnlyList<StainRestorationSurface>
            ActiveSurfaces => ActiveSurfaceList;
        public Vector3 RestorationPosition =>
            restorationPoint != null
                ? restorationPoint.position
                : transform.position;
        public bool CanRestore =>
            restorationEnabled && isActiveAndEnabled;

        private void OnEnable()
        {
            if (!ActiveSurfaceList.Contains(this))
            {
                ActiveSurfaceList.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveSurfaceList.Remove(this);
        }

        public void Configure(
            Transform point,
            bool enabledForRestoration)
        {
            restorationPoint = point;
            restorationEnabled = enabledForRestoration;
        }
    }
}
