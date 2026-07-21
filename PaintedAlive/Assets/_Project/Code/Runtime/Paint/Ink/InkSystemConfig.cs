using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [CreateAssetMenu(
        fileName = "InkSystemConfig",
        menuName = "Painted Alive/Paint/Ink/System Config")]
    public sealed class InkSystemConfig : ScriptableObject
    {
        [Header("Budget")]
        [SerializeField, Range(1, 32)]
        private int maximumConcurrentCreatures = 8;

        [SerializeField, Range(0.025f, 0.25f)]
        private float simulationInterval = 0.05f;

        [SerializeField, Range(0.25f, 3f)]
        private float figureDiscoveryInterval = 0.75f;

        [Header("Surface")]
        [SerializeField, Min(0.25f)]
        private float initialSurfaceRadius = 0.9f;

        [SerializeField, Min(0.5f)]
        private float maximumSurfaceRadius = 2.2f;

        [SerializeField, Min(1f)]
        private float initialInkAmount = 100f;

        [SerializeField, Range(0f, 1f)]
        private float initialWetness = 1f;

        [Header("Navigation")]
        [SerializeField, Min(0.1f)]
        private float groundProbeHeight = 0.8f;

        [SerializeField, Min(0.2f)]
        private float groundProbeDistance = 1.8f;

        [SerializeField, Min(0f)]
        private float surfaceOffset = 0.08f;

        [SerializeField, Range(0.1f, 1f)]
        private float obstacleProbeRadius = 0.28f;

        [SerializeField, Min(0.2f)]
        private float obstacleProbeDistance = 0.85f;

        [SerializeField, Range(0f, 65f)]
        private float maximumWalkableSlope = 48f;

        [SerializeField, Min(0.25f)]
        private float patrolRadius = 1.8f;

        [Header("Figure Contact")]
        [SerializeField, Min(0.1f)]
        private float contactDistance = 0.72f;

        [SerializeField, Min(0f)]
        private float clarityExposurePerContact = 1.4f;

        [SerializeField, Min(0.1f)]
        private float contactCooldown = 1.1f;

        [Header("Watercolor Reaction")]
        [SerializeField, Min(0f)]
        private float surfaceExpansionPerSecond = 0.5f;

        [SerializeField, Min(0f)]
        private float creatureWaterGainPerSecond = 0.65f;

        [SerializeField, Min(0f)]
        private float creatureWaterDecayPerSecond = 0.22f;

        [SerializeField, Range(1f, 2f)]
        private float maximumWaterScale = 1.5f;

        [SerializeField, Range(0.15f, 1f)]
        private float minimumWaterControl = 0.32f;

        [SerializeField, Range(0f, 120f)]
        private float maximumWaterWobbleDegrees = 68f;

        [SerializeField, Range(0.5f, 1.2f)]
        private float maximumWaterSpeedMultiplier = 1.04f;

        public int MaximumConcurrentCreatures => maximumConcurrentCreatures;
        public float SimulationInterval => simulationInterval;
        public float FigureDiscoveryInterval => figureDiscoveryInterval;
        public float InitialSurfaceRadius => initialSurfaceRadius;
        public float MaximumSurfaceRadius => maximumSurfaceRadius;
        public float InitialInkAmount => initialInkAmount;
        public float InitialWetness => initialWetness;
        public float GroundProbeHeight => groundProbeHeight;
        public float GroundProbeDistance => groundProbeDistance;
        public float SurfaceOffset => surfaceOffset;
        public float ObstacleProbeRadius => obstacleProbeRadius;
        public float ObstacleProbeDistance => obstacleProbeDistance;
        public float MaximumWalkableSlope => maximumWalkableSlope;
        public float PatrolRadius => patrolRadius;
        public float ContactDistance => contactDistance;
        public float ClarityExposurePerContact => clarityExposurePerContact;
        public float ContactCooldown => contactCooldown;
        public float SurfaceExpansionPerSecond => surfaceExpansionPerSecond;
        public float CreatureWaterGainPerSecond => creatureWaterGainPerSecond;
        public float CreatureWaterDecayPerSecond => creatureWaterDecayPerSecond;
        public float MaximumWaterScale => maximumWaterScale;
        public float MinimumWaterControl => minimumWaterControl;
        public float MaximumWaterWobbleDegrees => maximumWaterWobbleDegrees;
        public float MaximumWaterSpeedMultiplier => maximumWaterSpeedMultiplier;

        private void OnValidate()
        {
            maximumConcurrentCreatures = Mathf.Clamp(
                maximumConcurrentCreatures,
                1,
                32);
            simulationInterval = Mathf.Clamp(
                simulationInterval,
                0.025f,
                0.25f);
            figureDiscoveryInterval = Mathf.Clamp(
                figureDiscoveryInterval,
                0.25f,
                3f);
            initialSurfaceRadius = Mathf.Max(0.25f, initialSurfaceRadius);
            maximumSurfaceRadius = Mathf.Max(
                initialSurfaceRadius,
                maximumSurfaceRadius);
            initialInkAmount = Mathf.Max(1f, initialInkAmount);
            initialWetness = Mathf.Clamp01(initialWetness);
            groundProbeHeight = Mathf.Max(0.1f, groundProbeHeight);
            groundProbeDistance = Mathf.Max(0.2f, groundProbeDistance);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            obstacleProbeRadius = Mathf.Clamp(obstacleProbeRadius, 0.1f, 1f);
            obstacleProbeDistance = Mathf.Max(0.2f, obstacleProbeDistance);
            maximumWalkableSlope = Mathf.Clamp(maximumWalkableSlope, 0f, 65f);
            patrolRadius = Mathf.Max(0.25f, patrolRadius);
            contactDistance = Mathf.Max(0.1f, contactDistance);
            clarityExposurePerContact = Mathf.Max(0f, clarityExposurePerContact);
            contactCooldown = Mathf.Max(0.1f, contactCooldown);
            surfaceExpansionPerSecond = Mathf.Max(0f, surfaceExpansionPerSecond);
            creatureWaterGainPerSecond = Mathf.Max(0f, creatureWaterGainPerSecond);
            creatureWaterDecayPerSecond = Mathf.Max(0f, creatureWaterDecayPerSecond);
            maximumWaterScale = Mathf.Clamp(maximumWaterScale, 1f, 2f);
            minimumWaterControl = Mathf.Clamp(minimumWaterControl, 0.15f, 1f);
            maximumWaterWobbleDegrees = Mathf.Clamp(
                maximumWaterWobbleDegrees,
                0f,
                120f);
            maximumWaterSpeedMultiplier = Mathf.Clamp(
                maximumWaterSpeedMultiplier,
                0.5f,
                1.2f);
        }
    }
}
