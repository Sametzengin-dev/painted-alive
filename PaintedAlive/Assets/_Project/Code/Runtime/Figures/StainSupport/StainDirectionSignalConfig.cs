using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    [CreateAssetMenu(
        fileName = "StainDirectionSignalConfig",
        menuName = "Painted Alive/Figures/Stain Direction Signal Config")]
    public sealed class StainDirectionSignalConfig :
        ScriptableObject
    {
        [SerializeField, Min(1f)]
        private float signalLifetime = 6f;

        [SerializeField, Min(0f)]
        private float placementCooldown = 0.7f;

        [SerializeField, Min(1f)]
        private float maximumPlacementDistance = 18f;

        [SerializeField, Min(0.01f)]
        private float surfaceOffset = 0.035f;

        [SerializeField, Min(0.3f)]
        private float arrowLength = 1.35f;

        [SerializeField, Min(0.05f)]
        private float arrowWidth = 0.18f;

        [SerializeField, Min(0.1f)]
        private float arrowHeadLength = 0.48f;

        [SerializeField, Min(1)]
        private int maximumActiveSignals = 3;

        [SerializeField]
        private LayerMask surfaceMask = ~0;

        [SerializeField]
        private Material signalMaterial;

        public float SignalLifetime =>
            Mathf.Max(1f, signalLifetime);
        public float PlacementCooldown =>
            Mathf.Max(0f, placementCooldown);
        public float MaximumPlacementDistance =>
            Mathf.Max(1f, maximumPlacementDistance);
        public float SurfaceOffset =>
            Mathf.Max(0.01f, surfaceOffset);
        public float ArrowLength =>
            Mathf.Max(0.3f, arrowLength);
        public float ArrowWidth =>
            Mathf.Max(0.05f, arrowWidth);
        public float ArrowHeadLength =>
            Mathf.Max(0.1f, arrowHeadLength);
        public int MaximumActiveSignals =>
            Mathf.Max(1, maximumActiveSignals);
        public LayerMask SurfaceMask => surfaceMask;
        public Material SignalMaterial => signalMaterial;

        public void ConfigureMaterial(Material material)
        {
            signalMaterial = material;
        }
    }
}
