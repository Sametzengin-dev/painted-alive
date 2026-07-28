using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    [CreateAssetMenu(
        fileName = "StainWatercolorFlowConfig",
        menuName = "Painted Alive/Figures/Stain Watercolor Flow Config")]
    public sealed class StainWatercolorFlowConfig : ScriptableObject
    {
        [Header("Flow Detection")]
        [SerializeField, Min(0.1f)] private float detectionRadius = 0.68f;
        [SerializeField, Min(0f)] private float entryConfirmationDuration = 0.07f;
        [SerializeField, Min(0f)] private float minimumRideDuration = 0.30f;
        [SerializeField, Min(0f)] private float exitGraceDuration = 0.28f;
        [SerializeField, Min(0f)] private float reentryCooldown = 0.45f;

        [Header("Current Motion")]
        [SerializeField, Min(0f)] private float fallbackFlowSpeed = 4.4f;
        [SerializeField, Min(0f)] private float minimumFlowSpeed = 2.6f;
        [SerializeField, Min(0f)] private float maximumFlowSpeed = 7.25f;
        [SerializeField, Min(0f)] private float velocityAcceleration = 18f;
        [SerializeField, Min(0.01f)] private float velocitySmoothTime = 0.18f;
        [SerializeField, Min(0f)] private float directionResponsiveness = 7.5f;
        [SerializeField, Range(0f, 1f)] private float entryTargetVelocityBlend = 0.35f;
        [SerializeField, Min(0f)] private float missingSampleDrag = 1.15f;
        [SerializeField, Min(0f)] private float steeringSpeed = 1.2f;

        [Header("Surface Attachment")]
        [SerializeField, Min(0f)] private float surfaceOffset = 0.055f;
        [SerializeField, Min(0f)] private float surfaceAdhesionSpeed = 8f;
        [SerializeField, Min(0f)] private float surfaceNormalResponsiveness = 9f;
        [SerializeField, Min(0f)] private float exitNudge = 0.07f;

        [Header("Exit Glide")]
        [SerializeField, Min(0f)] private float exitGlideDuration = 0.20f;
        [SerializeField, Range(0f, 1f)] private float exitVelocityRetention = 0.38f;

        [Header("Runtime Discovery")]
        [SerializeField, Min(0.05f)] private float adapterRefreshInterval = 0.3f;

        public float DetectionRadius => detectionRadius;
        public float EntryConfirmationDuration => entryConfirmationDuration;
        public float MinimumRideDuration => minimumRideDuration;
        public float ExitGraceDuration => exitGraceDuration;
        public float ReentryCooldown => reentryCooldown;
        public float FallbackFlowSpeed => fallbackFlowSpeed;
        public float MinimumFlowSpeed => minimumFlowSpeed;
        public float MaximumFlowSpeed => maximumFlowSpeed;
        public float VelocityAcceleration => velocityAcceleration;
        public float VelocitySmoothTime => velocitySmoothTime;
        public float DirectionResponsiveness => directionResponsiveness;
        public float EntryTargetVelocityBlend => entryTargetVelocityBlend;
        public float MissingSampleDrag => missingSampleDrag;
        public float SteeringSpeed => steeringSpeed;
        public float SurfaceOffset => surfaceOffset;
        public float SurfaceAdhesionSpeed => surfaceAdhesionSpeed;
        public float SurfaceNormalResponsiveness => surfaceNormalResponsiveness;
        public float ExitNudge => exitNudge;
        public float ExitGlideDuration => exitGlideDuration;
        public float ExitVelocityRetention => exitVelocityRetention;
        public float AdapterRefreshInterval => adapterRefreshInterval;

        private void OnValidate()
        {
            detectionRadius = Mathf.Max(0.1f, detectionRadius);
            entryConfirmationDuration = Mathf.Max(0f, entryConfirmationDuration);
            minimumRideDuration = Mathf.Max(0f, minimumRideDuration);
            exitGraceDuration = Mathf.Max(0f, exitGraceDuration);
            reentryCooldown = Mathf.Max(0f, reentryCooldown);
            fallbackFlowSpeed = Mathf.Max(0f, fallbackFlowSpeed);
            minimumFlowSpeed = Mathf.Max(0f, minimumFlowSpeed);
            maximumFlowSpeed = Mathf.Max(minimumFlowSpeed, maximumFlowSpeed);
            velocityAcceleration = Mathf.Max(0f, velocityAcceleration);
            velocitySmoothTime = Mathf.Max(0.01f, velocitySmoothTime);
            directionResponsiveness = Mathf.Max(0f, directionResponsiveness);
            entryTargetVelocityBlend = Mathf.Clamp01(entryTargetVelocityBlend);
            missingSampleDrag = Mathf.Max(0f, missingSampleDrag);
            steeringSpeed = Mathf.Max(0f, steeringSpeed);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            surfaceAdhesionSpeed = Mathf.Max(0f, surfaceAdhesionSpeed);
            surfaceNormalResponsiveness = Mathf.Max(0f, surfaceNormalResponsiveness);
            exitNudge = Mathf.Max(0f, exitNudge);
            exitGlideDuration = Mathf.Max(0f, exitGlideDuration);
            exitVelocityRetention = Mathf.Clamp01(exitVelocityRetention);
            adapterRefreshInterval = Mathf.Max(0.05f, adapterRefreshInterval);
        }
    }
}
