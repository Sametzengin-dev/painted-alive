using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    [CreateAssetMenu(
        fileName = "StainGripImprintConfig",
        menuName = "Painted Alive/Figures/Stain Grip Imprint Config")]
    public sealed class StainGripImprintConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)]
        private float imprintDuration = 0.8f;

        [SerializeField, Min(1f)]
        private float markLifetime = 8f;

        [SerializeField, Min(0.2f)]
        private float surfaceProbeDistance = 1.35f;

        [SerializeField, Min(0.2f)]
        private float markDiameter = 1.35f;

        [SerializeField, Min(0.05f)]
        private float platformThickness = 0.14f;

        [SerializeField, Min(0.2f)]
        private float wallLedgeDepth = 0.62f;

        [SerializeField, Min(1)]
        private int maximumActiveMarks = 2;

        [SerializeField]
        private LayerMask surfaceMask = ~0;

        [SerializeField]
        private Material markMaterial;

        [SerializeField]
        private Material supportMaterial;

        public float ImprintDuration =>
            Mathf.Max(0.1f, imprintDuration);
        public float MarkLifetime =>
            Mathf.Max(1f, markLifetime);
        public float SurfaceProbeDistance =>
            Mathf.Max(0.2f, surfaceProbeDistance);
        public float MarkDiameter =>
            Mathf.Max(0.2f, markDiameter);
        public float PlatformThickness =>
            Mathf.Max(0.05f, platformThickness);
        public float WallLedgeDepth =>
            Mathf.Max(0.2f, wallLedgeDepth);
        public int MaximumActiveMarks =>
            Mathf.Max(1, maximumActiveMarks);
        public LayerMask SurfaceMask => surfaceMask;
        public Material MarkMaterial => markMaterial;
        public Material SupportMaterial => supportMaterial;

        public void ConfigureMaterials(
            Material targetMarkMaterial,
            Material targetSupportMaterial)
        {
            markMaterial = targetMarkMaterial;
            supportMaterial = targetSupportMaterial;
        }
    }
}
