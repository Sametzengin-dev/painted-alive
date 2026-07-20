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

        [Header("Surface Grip")]
        [SerializeField, Range(0f, 1f)]
        private float wetOilGrip = 0.22f;

        [SerializeField, Range(0f, 1f)]
        private float dryingOilGrip = 0.62f;

        [SerializeField, Range(0f, 1f)]
        private float dryOilGrip = 1f;

        [SerializeField, Range(0f, 1f)]
        private float fixedOilGrip = 1f;

        [SerializeField, Range(0f, 0.5f)]
        private float slipStartTensionBias = 0.08f;

        [SerializeField, Min(0f)]
        private float minimumSlipSpeed = 0.12f;

        [SerializeField, Min(0f)]
        private float maximumSlipSpeed = 2.2f;

        [SerializeField, Min(0.05f)]
        private float surfaceProbeDistance = 0.35f;

        [SerializeField, Min(0.1f)]
        private float maximumContinuousSlipDistance = 5f;

        [Header("Structural Rope Load")]
        [SerializeField, Range(0f, 1f)]
        private float minimumDamageTension = 0.55f;

        [SerializeField, Min(0f)]
        private float tensionDamagePerSecond = 0.18f;

        [SerializeField, Range(0f, 1f)]
        private float wetBrittleness = 0.08f;

        [SerializeField, Range(0f, 1f)]
        private float dryingBrittleness = 0.45f;

        [SerializeField, Min(1f)]
        private float maximumFixativeBrittlenessMultiplier = 2.25f;

        [Header("Rope Visual")]
        [SerializeField, Range(4, 64)]
        private int ropeSegments = 18;

        [SerializeField, Min(0.005f)]
        private float ropeWidth = 0.035f;

        [SerializeField, Min(0f)]
        private float maximumVisualSag = 1.2f;

        [SerializeField]
        private Color slackRopeColor =
            new Color(0.32f, 0.13f, 0.04f, 1f);

        [SerializeField]
        private Color tensionRopeColor =
            new Color(1f, 0.48f, 0.06f, 1f);

        [SerializeField]
        private Color criticalRopeColor =
            new Color(0.95f, 0.06f, 0.025f, 1f);

        [SerializeField]
        private Color slidingRopeColor =
            new Color(1f, 0.82f, 0.08f, 1f);

        [SerializeField, HideInInspector]
        private int surfaceAnchorVersion;

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
        public float WetOilGrip => wetOilGrip;
        public float DryingOilGrip => dryingOilGrip;
        public float DryOilGrip => dryOilGrip;
        public float FixedOilGrip => fixedOilGrip;
        public float SlipStartTensionBias => slipStartTensionBias;
        public float MinimumSlipSpeed => minimumSlipSpeed;
        public float MaximumSlipSpeed => maximumSlipSpeed;
        public float SurfaceProbeDistance => surfaceProbeDistance;
        public float MaximumContinuousSlipDistance =>
            maximumContinuousSlipDistance;
        public float MinimumDamageTension => minimumDamageTension;
        public float TensionDamagePerSecond => tensionDamagePerSecond;
        public float WetBrittleness => wetBrittleness;
        public float DryingBrittleness => dryingBrittleness;
        public float MaximumFixativeBrittlenessMultiplier =>
            maximumFixativeBrittlenessMultiplier;
        public int RopeSegments => ropeSegments;
        public float RopeWidth => ropeWidth;
        public float MaximumVisualSag => maximumVisualSag;
        public Color SlackRopeColor => slackRopeColor;
        public Color TensionRopeColor => tensionRopeColor;
        public Color CriticalRopeColor => criticalRopeColor;
        public Color SlidingRopeColor => slidingRopeColor;

        public void EnsureSurfaceAnchorDefaults()
        {
            if (surfaceAnchorVersion >= 1)
            {
                return;
            }

            wetOilGrip = 0.22f;
            dryingOilGrip = 0.62f;
            dryOilGrip = 1f;
            fixedOilGrip = 1f;
            slipStartTensionBias = 0.08f;
            minimumSlipSpeed = 0.12f;
            maximumSlipSpeed = 2.2f;
            surfaceProbeDistance = 0.35f;
            maximumContinuousSlipDistance = 5f;
            minimumDamageTension = 0.55f;
            tensionDamagePerSecond = 0.18f;
            wetBrittleness = 0.08f;
            dryingBrittleness = 0.45f;
            maximumFixativeBrittlenessMultiplier = 2.25f;
            slackRopeColor =
                new Color(0.32f, 0.13f, 0.04f, 1f);
            tensionRopeColor =
                new Color(1f, 0.48f, 0.06f, 1f);
            criticalRopeColor =
                new Color(0.95f, 0.06f, 0.025f, 1f);
            slidingRopeColor =
                new Color(1f, 0.82f, 0.08f, 1f);
            surfaceAnchorVersion = 1;
        }

        private void OnValidate()
        {
            maximumAimDistance = Mathf.Max(1f, maximumAimDistance);
            castRadius = Mathf.Max(0f, castRadius);
            minimumAnchorDistance =
                Mathf.Max(0f, minimumAnchorDistance);
            minimumRopeLength = Mathf.Max(0.1f, minimumRopeLength);
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

            wetOilGrip = Mathf.Clamp01(wetOilGrip);
            dryingOilGrip = Mathf.Clamp01(dryingOilGrip);
            dryOilGrip = Mathf.Clamp01(dryOilGrip);
            fixedOilGrip = Mathf.Clamp01(fixedOilGrip);
            slipStartTensionBias =
                Mathf.Clamp(slipStartTensionBias, 0f, 0.5f);
            minimumSlipSpeed = Mathf.Max(0f, minimumSlipSpeed);
            maximumSlipSpeed =
                Mathf.Max(minimumSlipSpeed, maximumSlipSpeed);
            surfaceProbeDistance =
                Mathf.Max(0.05f, surfaceProbeDistance);
            maximumContinuousSlipDistance =
                Mathf.Max(0.1f, maximumContinuousSlipDistance);

            minimumDamageTension =
                Mathf.Clamp01(minimumDamageTension);
            tensionDamagePerSecond =
                Mathf.Max(0f, tensionDamagePerSecond);
            wetBrittleness = Mathf.Clamp01(wetBrittleness);
            dryingBrittleness =
                Mathf.Clamp01(dryingBrittleness);
            maximumFixativeBrittlenessMultiplier =
                Mathf.Max(1f, maximumFixativeBrittlenessMultiplier);

            ropeSegments = Mathf.Clamp(ropeSegments, 4, 64);
            ropeWidth = Mathf.Max(0.005f, ropeWidth);
            maximumVisualSag = Mathf.Max(0f, maximumVisualSag);
        }
    }
}
