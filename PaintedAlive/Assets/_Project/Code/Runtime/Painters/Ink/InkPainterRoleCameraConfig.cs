using UnityEngine;

namespace PaintedAlive.Painters.Ink
{
    [CreateAssetMenu(
        fileName = "InkPainterRoleCameraConfig",
        menuName = "Painted Alive/Painters/Ink Painter Role Camera Config")]
    public sealed class InkPainterRoleCameraConfig : ScriptableObject
    {
        [Header("Free Look")]
        [SerializeField, Min(0.01f)]
        private float mouseSensitivity = 0.12f;

        [SerializeField, Range(-89f, 0f)]
        private float minimumPitch = -80f;

        [SerializeField, Range(0f, 89f)]
        private float maximumPitch = 80f;

        [Header("Work Camera Movement")]
        [SerializeField, Min(0.1f)]
        private float movementSpeed = 9f;

        [SerializeField, Min(1f)]
        private float boostMultiplier = 2.2f;

        [SerializeField, Min(2f)]
        private float maximumWorkRadius = 35f;

        [SerializeField, Min(0.5f)]
        private float minimumHeightFromFigure = 1.5f;

        [SerializeField, Min(1f)]
        private float maximumHeightFromFigure = 18f;

        [Header("Reframe")]
        [SerializeField, Min(1f)]
        private float reframeDistance = 9f;

        [SerializeField, Min(0f)]
        private float reframeHeight = 5f;

        [SerializeField, Min(1f)]
        private float normalFieldOfView = 64f;

        [SerializeField, Min(1f)]
        private float planningFieldOfView = 74f;

        [SerializeField, Min(0.1f)]
        private float fieldOfViewSharpness = 8f;

        public float MouseSensitivity => mouseSensitivity;
        public float MinimumPitch => minimumPitch;
        public float MaximumPitch => maximumPitch;
        public float MovementSpeed => movementSpeed;
        public float BoostMultiplier => boostMultiplier;
        public float MaximumWorkRadius => maximumWorkRadius;
        public float MinimumHeightFromFigure => minimumHeightFromFigure;
        public float MaximumHeightFromFigure => maximumHeightFromFigure;
        public float ReframeDistance => reframeDistance;
        public float ReframeHeight => reframeHeight;
        public float NormalFieldOfView => normalFieldOfView;
        public float PlanningFieldOfView => planningFieldOfView;
        public float FieldOfViewSharpness => fieldOfViewSharpness;

        private void OnValidate()
        {
            mouseSensitivity = Mathf.Max(0.01f, mouseSensitivity);
            maximumPitch = Mathf.Max(0f, maximumPitch);
            minimumPitch = Mathf.Min(0f, minimumPitch);
            movementSpeed = Mathf.Max(0.1f, movementSpeed);
            boostMultiplier = Mathf.Max(1f, boostMultiplier);
            maximumWorkRadius = Mathf.Max(2f, maximumWorkRadius);
            minimumHeightFromFigure =
                Mathf.Max(0.5f, minimumHeightFromFigure);
            maximumHeightFromFigure = Mathf.Max(
                minimumHeightFromFigure,
                maximumHeightFromFigure);
            reframeDistance = Mathf.Max(1f, reframeDistance);
            normalFieldOfView = Mathf.Clamp(normalFieldOfView, 20f, 110f);
            planningFieldOfView = Mathf.Clamp(
                planningFieldOfView,
                normalFieldOfView,
                120f);
            fieldOfViewSharpness = Mathf.Max(0.1f, fieldOfViewSharpness);
        }
    }
}
