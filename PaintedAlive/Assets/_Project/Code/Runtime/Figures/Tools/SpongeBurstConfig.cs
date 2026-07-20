using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [CreateAssetMenu(
        fileName = "SpongeBurstConfig",
        menuName =
            "Painted Alive/Figures/Tools/Sponge Burst Config")]
    public sealed class SpongeBurstConfig : ScriptableObject
    {
        [Header("Burst Requirement")]
        [SerializeField, Range(0.5f, 1f)]
        private float minimumFillToBurst = 0.9f;

        [SerializeField, Min(0.1f)]
        private float stableImpactThreshold = 9f;

        [SerializeField, Min(0.1f)]
        private float unstableImpactThreshold = 5.5f;

        [SerializeField, Min(0f)]
        private float burstCooldown = 1.25f;

        [Header("Area Paint")]
        [SerializeField, Min(0.1f)]
        private float burstRadius = 3.6f;

        [SerializeField, Min(0f)]
        private float baseClarityExposure = 22f;

        [SerializeField, Range(0f, 2f)]
        private float instabilityExposureScale = 0.55f;

        [Header("Recoverable Spill")]
        [SerializeField, Range(1, 12)]
        private int puddleCount = 7;

        [SerializeField, Range(0f, 1f)]
        private float recoverablePaintFraction = 0.75f;

        [SerializeField, Min(0f)]
        private float minimumPuddleDistance = 0.65f;

        [SerializeField, Min(0.1f)]
        private float maximumPuddleDistance = 2.4f;

        [SerializeField, Min(0.5f)]
        private float surfaceProbeHeight = 1.8f;

        [SerializeField, Min(0.5f)]
        private float surfaceProbeDistance = 4.5f;

        [SerializeField, Min(0f)]
        private float surfaceOffset = 0.035f;

        public float MinimumFillToBurst => minimumFillToBurst;
        public float StableImpactThreshold => stableImpactThreshold;
        public float UnstableImpactThreshold =>
            unstableImpactThreshold;
        public float BurstCooldown => burstCooldown;
        public float BurstRadius => burstRadius;
        public float BaseClarityExposure => baseClarityExposure;
        public float InstabilityExposureScale =>
            instabilityExposureScale;
        public int PuddleCount => puddleCount;
        public float RecoverablePaintFraction =>
            recoverablePaintFraction;
        public float MinimumPuddleDistance =>
            minimumPuddleDistance;
        public float MaximumPuddleDistance =>
            maximumPuddleDistance;
        public float SurfaceProbeHeight => surfaceProbeHeight;
        public float SurfaceProbeDistance => surfaceProbeDistance;
        public float SurfaceOffset => surfaceOffset;

        public float GetRequiredImpact(float instability)
        {
            return Mathf.Lerp(
                stableImpactThreshold,
                unstableImpactThreshold,
                Mathf.Clamp01(instability));
        }

        private void OnValidate()
        {
            minimumFillToBurst =
                Mathf.Clamp(minimumFillToBurst, 0.5f, 1f);
            stableImpactThreshold =
                Mathf.Max(0.1f, stableImpactThreshold);
            unstableImpactThreshold =
                Mathf.Clamp(
                    unstableImpactThreshold,
                    0.1f,
                    stableImpactThreshold);
            burstCooldown = Mathf.Max(0f, burstCooldown);
            burstRadius = Mathf.Max(0.1f, burstRadius);
            baseClarityExposure =
                Mathf.Max(0f, baseClarityExposure);
            instabilityExposureScale =
                Mathf.Clamp(instabilityExposureScale, 0f, 2f);
            puddleCount = Mathf.Clamp(puddleCount, 1, 12);
            recoverablePaintFraction =
                Mathf.Clamp01(recoverablePaintFraction);
            minimumPuddleDistance =
                Mathf.Max(0f, minimumPuddleDistance);
            maximumPuddleDistance =
                Mathf.Max(
                    minimumPuddleDistance + 0.1f,
                    maximumPuddleDistance);
            surfaceProbeHeight =
                Mathf.Max(0.5f, surfaceProbeHeight);
            surfaceProbeDistance =
                Mathf.Max(0.5f, surfaceProbeDistance);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
        }
    }
}
