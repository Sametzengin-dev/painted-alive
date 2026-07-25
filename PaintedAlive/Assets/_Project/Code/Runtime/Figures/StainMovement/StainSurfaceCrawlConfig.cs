using UnityEngine;

namespace PaintedAlive.Figures.StainMovement
{
    [CreateAssetMenu(
        fileName = "M28_StainSurfaceCrawlConfig",
        menuName = "Painted Alive/Figures/Stain Surface Crawl Config")]
    public sealed class StainSurfaceCrawlConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0.25f)]
        private float crawlSpeed = 3.1f;

        [SerializeField, Min(30f)]
        private float turnSpeedDegrees = 540f;

        [SerializeField, Min(0.1f)]
        private float detachedFallSpeed = 4.5f;

        [Header("Surface Detection")]
        [SerializeField]
        private LayerMask surfaceMask = Physics.DefaultRaycastLayers;

        [SerializeField, Range(0.03f, 0.45f)]
        private float probeRadius = 0.18f;

        [SerializeField, Min(0.1f)]
        private float surfaceProbeDistance = 0.8f;

        [SerializeField, Min(0.1f)]
        private float transitionProbeDistance = 0.7f;

        [SerializeField, Min(0.01f)]
        private float surfaceGap = 0.06f;

        [SerializeField, Min(0.1f)]
        private float surfaceSnapSpeed = 8f;

        [SerializeField, Range(0f, 89f)]
        private float minimumTransitionAngle = 18f;

        [SerializeField, Range(0.05f, 1f)]
        private float lostSurfaceGrace = 0.28f;

        [Header("Detach")]
        [SerializeField, Range(0.05f, 1f)]
        private float detachReattachDelay = 0.22f;

        public float CrawlSpeed => crawlSpeed;
        public float TurnSpeedDegrees => turnSpeedDegrees;
        public float DetachedFallSpeed => detachedFallSpeed;
        public LayerMask SurfaceMask => surfaceMask;
        public float ProbeRadius => probeRadius;
        public float SurfaceProbeDistance => surfaceProbeDistance;
        public float TransitionProbeDistance => transitionProbeDistance;
        public float SurfaceGap => surfaceGap;
        public float SurfaceSnapSpeed => surfaceSnapSpeed;
        public float MinimumTransitionAngle => minimumTransitionAngle;
        public float LostSurfaceGrace => lostSurfaceGrace;
        public float DetachReattachDelay => detachReattachDelay;

        private void OnValidate()
        {
            crawlSpeed = Mathf.Max(0.25f, crawlSpeed);
            turnSpeedDegrees = Mathf.Max(30f, turnSpeedDegrees);
            detachedFallSpeed = Mathf.Max(0.1f, detachedFallSpeed);
            probeRadius = Mathf.Clamp(probeRadius, 0.03f, 0.45f);
            surfaceProbeDistance =
                Mathf.Max(0.1f, surfaceProbeDistance);
            transitionProbeDistance =
                Mathf.Max(0.1f, transitionProbeDistance);
            surfaceGap = Mathf.Max(0.01f, surfaceGap);
            surfaceSnapSpeed = Mathf.Max(0.1f, surfaceSnapSpeed);
            minimumTransitionAngle =
                Mathf.Clamp(minimumTransitionAngle, 0f, 89f);
            lostSurfaceGrace =
                Mathf.Clamp(lostSurfaceGrace, 0.05f, 1f);
            detachReattachDelay =
                Mathf.Clamp(detachReattachDelay, 0.05f, 1f);
        }
    }
}
