using UnityEngine;

namespace PaintedAlive.Paint.Ink.StainHijack
{
    [CreateAssetMenu(
        fileName = "M26_StainCreatureHijackConfig",
        menuName = "Painted Alive/Paint/Ink/Stain Creature Hijack Config")]
    public sealed class InkStainHijackConfig : ScriptableObject
    {
        [Header("Entry")]
        [SerializeField, Min(0.5f)]
        private float interactionRange = 4.25f;

        [SerializeField, Min(0.02f)]
        private float aimAssistRadius = 0.18f;

        [SerializeField, Min(0.1f)]
        private float entryHoldDuration = 0.45f;

        [SerializeField, Range(1, 16)]
        private int maximumComplexity = 3;

        [SerializeField]
        private LayerMask targetMask = Physics.DefaultRaycastLayers;

        [Header("Hijack")]
        [SerializeField, Min(1f)]
        private float maximumHijackDuration = 3.75f;

        [SerializeField, Range(0.25f, 2f)]
        private float movementSpeedMultiplier = 1.05f;

        [SerializeField, Min(30f)]
        private float turnSpeedDegrees = 540f;

        [SerializeField, Range(0.01f, 0.5f)]
        private float mouseSensitivity = 0.085f;

        [Header("Camera")]
        [SerializeField, Min(0.5f)]
        private float cameraDistance = 2.35f;

        [SerializeField, Min(0f)]
        private float cameraHeight = 0.75f;

        [SerializeField, Range(-80f, 0f)]
        private float minimumPitch = -25f;

        [SerializeField, Range(0f, 80f)]
        private float maximumPitch = 55f;

        [SerializeField, Min(1f)]
        private float cameraFollowSharpness = 18f;

        [SerializeField, Range(0.05f, 0.75f)]
        private float cameraCollisionRadius = 0.18f;

        [Header("Navigation")]
        [SerializeField]
        private LayerMask navigationMask = Physics.DefaultRaycastLayers;

        [SerializeField, Min(0.25f)]
        private float groundProbeHeight = 1.8f;

        [SerializeField, Min(0.25f)]
        private float groundProbeDistance = 3.5f;

        [SerializeField, Min(0.01f)]
        private float surfaceOffset = 0.08f;

        [SerializeField, Range(0f, 80f)]
        private float maximumWalkableSlope = 52f;

        [SerializeField, Min(0.05f)]
        private float obstacleProbeRadius = 0.22f;

        [SerializeField, Min(0.1f)]
        private float obstacleProbeDistance = 0.72f;

        public float InteractionRange => interactionRange;
        public float AimAssistRadius => aimAssistRadius;
        public float EntryHoldDuration => entryHoldDuration;
        public int MaximumComplexity => maximumComplexity;
        public LayerMask TargetMask => targetMask;
        public float MaximumHijackDuration => maximumHijackDuration;
        public float MovementSpeedMultiplier => movementSpeedMultiplier;
        public float TurnSpeedDegrees => turnSpeedDegrees;
        public float MouseSensitivity => mouseSensitivity;
        public float CameraDistance => cameraDistance;
        public float CameraHeight => cameraHeight;
        public float MinimumPitch => minimumPitch;
        public float MaximumPitch => maximumPitch;
        public float CameraFollowSharpness => cameraFollowSharpness;
        public float CameraCollisionRadius => cameraCollisionRadius;
        public LayerMask NavigationMask => navigationMask;
        public float GroundProbeHeight => groundProbeHeight;
        public float GroundProbeDistance => groundProbeDistance;
        public float SurfaceOffset => surfaceOffset;
        public float MaximumWalkableSlope => maximumWalkableSlope;
        public float ObstacleProbeRadius => obstacleProbeRadius;
        public float ObstacleProbeDistance => obstacleProbeDistance;

        private void OnValidate()
        {
            interactionRange = Mathf.Max(0.5f, interactionRange);
            aimAssistRadius = Mathf.Max(0.02f, aimAssistRadius);
            entryHoldDuration = Mathf.Max(0.1f, entryHoldDuration);
            maximumComplexity = Mathf.Clamp(maximumComplexity, 1, 16);
            maximumHijackDuration =
                Mathf.Max(1f, maximumHijackDuration);
            movementSpeedMultiplier = Mathf.Clamp(
                movementSpeedMultiplier,
                0.25f,
                2f);
            turnSpeedDegrees = Mathf.Max(30f, turnSpeedDegrees);
            mouseSensitivity = Mathf.Clamp(
                mouseSensitivity,
                0.01f,
                0.5f);
            cameraDistance = Mathf.Max(0.5f, cameraDistance);
            cameraHeight = Mathf.Max(0f, cameraHeight);
            minimumPitch = Mathf.Clamp(minimumPitch, -80f, 0f);
            maximumPitch = Mathf.Clamp(maximumPitch, 0f, 80f);
            cameraFollowSharpness =
                Mathf.Max(1f, cameraFollowSharpness);
            cameraCollisionRadius = Mathf.Clamp(
                cameraCollisionRadius,
                0.05f,
                0.75f);
            groundProbeHeight = Mathf.Max(0.25f, groundProbeHeight);
            groundProbeDistance =
                Mathf.Max(0.25f, groundProbeDistance);
            surfaceOffset = Mathf.Max(0.01f, surfaceOffset);
            maximumWalkableSlope =
                Mathf.Clamp(maximumWalkableSlope, 0f, 80f);
            obstacleProbeRadius =
                Mathf.Max(0.05f, obstacleProbeRadius);
            obstacleProbeDistance =
                Mathf.Max(0.1f, obstacleProbeDistance);
        }
    }
}
