using UnityEngine;

namespace PaintedAlive.Paint.Ink.Possession
{
    [CreateAssetMenu(
        fileName = "InkPossessionConfig",
        menuName = "Painted Alive/Paint/Ink/Possession Config")]
    public sealed class InkPossessionConfig : ScriptableObject
    {
        [Header("Selection")]
        [SerializeField, Min(1f)]
        private float selectionRange = 18f;

        [SerializeField, Range(-1f, 1f)]
        private float minimumAimDot = 0.42f;

        [Header("Direct Control")]
        [SerializeField, Range(0.25f, 2f)]
        private float movementSpeedMultiplier = 1f;

        [SerializeField, Min(30f)]
        private float turnSpeedDegrees = 540f;

        [SerializeField, Range(0.01f, 0.5f)]
        private float mouseSensitivity = 0.085f;

        [SerializeField, Range(-80f, 0f)]
        private float minimumPitch = -28f;

        [SerializeField, Range(0f, 80f)]
        private float maximumPitch = 58f;

        [Header("Camera")]
        [SerializeField, Min(0.5f)]
        private float cameraDistance = 2.35f;

        [SerializeField, Min(0f)]
        private float cameraHeight = 0.82f;

        [SerializeField, Range(0.05f, 0.75f)]
        private float cameraCollisionRadius = 0.18f;

        [SerializeField, Min(1f)]
        private float cameraFollowSharpness = 18f;

        [Header("Rules")]
        [SerializeField, Min(0.05f)]
        private float toggleCooldown = 0.3f;

        [SerializeField]
        private bool forceExitWhenEyeIsLost = true;

        [SerializeField]
        private bool lockCursorWhilePossessing = true;

        public float SelectionRange => selectionRange;
        public float MinimumAimDot => minimumAimDot;
        public float MovementSpeedMultiplier => movementSpeedMultiplier;
        public float TurnSpeedDegrees => turnSpeedDegrees;
        public float MouseSensitivity => mouseSensitivity;
        public float MinimumPitch => minimumPitch;
        public float MaximumPitch => maximumPitch;
        public float CameraDistance => cameraDistance;
        public float CameraHeight => cameraHeight;
        public float CameraCollisionRadius => cameraCollisionRadius;
        public float CameraFollowSharpness => cameraFollowSharpness;
        public float ToggleCooldown => toggleCooldown;
        public bool ForceExitWhenEyeIsLost => forceExitWhenEyeIsLost;
        public bool LockCursorWhilePossessing => lockCursorWhilePossessing;

        private void OnValidate()
        {
            selectionRange = Mathf.Max(1f, selectionRange);
            minimumAimDot = Mathf.Clamp(minimumAimDot, -1f, 1f);
            movementSpeedMultiplier = Mathf.Clamp(
                movementSpeedMultiplier,
                0.25f,
                2f);
            turnSpeedDegrees = Mathf.Max(30f, turnSpeedDegrees);
            mouseSensitivity = Mathf.Clamp(
                mouseSensitivity,
                0.01f,
                0.5f);
            minimumPitch = Mathf.Clamp(minimumPitch, -80f, 0f);
            maximumPitch = Mathf.Clamp(maximumPitch, 0f, 80f);
            cameraDistance = Mathf.Max(0.5f, cameraDistance);
            cameraHeight = Mathf.Max(0f, cameraHeight);
            cameraCollisionRadius = Mathf.Clamp(
                cameraCollisionRadius,
                0.05f,
                0.75f);
            cameraFollowSharpness = Mathf.Max(1f, cameraFollowSharpness);
            toggleCooldown = Mathf.Max(0.05f, toggleCooldown);
        }
    }
}
