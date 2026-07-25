using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    [CreateAssetMenu(
        fileName = "M29_StainSpongeCarryConfig",
        menuName = "Painted Alive/Figures/" +
            "M29 Stain Sponge Carry Config")]
    public sealed class StainSpongeCarryConfig : ScriptableObject
    {
        [Header("Boarding")]
        [SerializeField, Min(0.25f)]
        private float interactionRange = 2.25f;

        [SerializeField, Min(0f)]
        private float inputCooldown = 0.3f;

        [Header("Safe Exit")]
        [SerializeField, Min(0.25f)]
        private float exitHorizontalOffset = 1.15f;

        [SerializeField, Min(0.25f)]
        private float groundProbeHeight = 2.2f;

        [SerializeField, Min(0.25f)]
        private float groundProbeDistance = 5f;

        [SerializeField, Range(0f, 89f)]
        private float maximumExitSlope = 55f;

        [SerializeField]
        private LayerMask surfaceMask =
            Physics.DefaultRaycastLayers;

        [Header("Prototype Carrier")]
        [SerializeField, Min(0.1f)]
        private float prototypeMoveSpeed = 3.4f;

        public float InteractionRange =>
            Mathf.Max(0.25f, interactionRange);
        public float InputCooldown =>
            Mathf.Max(0f, inputCooldown);
        public float ExitHorizontalOffset =>
            Mathf.Max(0.25f, exitHorizontalOffset);
        public float GroundProbeHeight =>
            Mathf.Max(0.25f, groundProbeHeight);
        public float GroundProbeDistance =>
            Mathf.Max(0.25f, groundProbeDistance);
        public float MinimumExitUpDot =>
            Mathf.Cos(
                Mathf.Clamp(maximumExitSlope, 0f, 89f) *
                Mathf.Deg2Rad);
        public LayerMask SurfaceMask => surfaceMask;
        public float PrototypeMoveSpeed =>
            Mathf.Max(0.1f, prototypeMoveSpeed);
    }
}
