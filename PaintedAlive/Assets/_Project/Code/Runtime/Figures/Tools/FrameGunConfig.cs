using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [CreateAssetMenu(
        fileName = "FrameGunConfig",
        menuName = "Painted Alive/Figures/Tools/Frame Gun Config")]
    public sealed class FrameGunConfig : ScriptableObject
    {
        [Header("Anchor Shot")]
        [SerializeField, Min(1f)]
        private float maximumAimDistance = 24f;

        [SerializeField, Min(0f)]
        private float castRadius = 0.08f;

        [SerializeField, Min(0f)]
        private float minimumAnchorDistance = 1.25f;

        [Header("Rope Length")]
        [SerializeField, Min(0.1f)]
        private float minimumRopeLength = 1.5f;

        [SerializeField, Min(1f)]
        private float maximumRopeLength = 24f;

        [SerializeField, Range(0.9f, 1.25f)]
        private float initialLengthMultiplier = 1.02f;

        [SerializeField, Min(0f)]
        private float slackTolerance = 0.12f;

        [Header("Physical Constraint")]
        [SerializeField, Min(0f)]
        private float springAcceleration = 20f;

        [SerializeField, Min(0f)]
        private float radialDamping = 5.5f;

        [SerializeField, Min(0f)]
        private float maximumPullAcceleration = 32f;

        [SerializeField, Min(0.1f)]
        private float maximumStretchBeforeBreak = 5f;

        [SerializeField, Min(0f)]
        private float dynamicBodyReactionForceScale = 0.25f;

        [Header("Rope Visual")]
        [SerializeField, Range(4, 64)]
        private int ropeSegments = 18;

        [SerializeField, Min(0.005f)]
        private float ropeWidth = 0.035f;

        [SerializeField, Min(0f)]
        private float maximumVisualSag = 1.2f;

        public float MaximumAimDistance => maximumAimDistance;
        public float CastRadius => castRadius;
        public float MinimumAnchorDistance => minimumAnchorDistance;
        public float MinimumRopeLength => minimumRopeLength;
        public float MaximumRopeLength => maximumRopeLength;
        public float InitialLengthMultiplier => initialLengthMultiplier;
        public float SlackTolerance => slackTolerance;
        public float SpringAcceleration => springAcceleration;
        public float RadialDamping => radialDamping;
        public float MaximumPullAcceleration => maximumPullAcceleration;
        public float MaximumStretchBeforeBreak =>
            maximumStretchBeforeBreak;
        public float DynamicBodyReactionForceScale =>
            dynamicBodyReactionForceScale;
        public int RopeSegments => ropeSegments;
        public float RopeWidth => ropeWidth;
        public float MaximumVisualSag => maximumVisualSag;

        private void OnValidate()
        {
            maximumAimDistance =
                Mathf.Max(1f, maximumAimDistance);

            castRadius = Mathf.Max(0f, castRadius);
            minimumAnchorDistance =
                Mathf.Max(0f, minimumAnchorDistance);

            minimumRopeLength =
                Mathf.Max(0.1f, minimumRopeLength);

            maximumRopeLength =
                Mathf.Max(minimumRopeLength, maximumRopeLength);

            initialLengthMultiplier =
                Mathf.Clamp(initialLengthMultiplier, 0.9f, 1.25f);

            slackTolerance = Mathf.Max(0f, slackTolerance);
            springAcceleration = Mathf.Max(0f, springAcceleration);
            radialDamping = Mathf.Max(0f, radialDamping);

            maximumPullAcceleration =
                Mathf.Max(0f, maximumPullAcceleration);

            maximumStretchBeforeBreak =
                Mathf.Max(0.1f, maximumStretchBeforeBreak);

            dynamicBodyReactionForceScale =
                Mathf.Max(0f, dynamicBodyReactionForceScale);

            ropeSegments = Mathf.Clamp(ropeSegments, 4, 64);
            ropeWidth = Mathf.Max(0.005f, ropeWidth);
            maximumVisualSag =
                Mathf.Max(0f, maximumVisualSag);
        }
    }
}
