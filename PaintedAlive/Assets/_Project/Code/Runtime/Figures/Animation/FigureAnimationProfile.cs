using UnityEngine;

namespace PaintedAlive.Figures.Animation
{
    [CreateAssetMenu(
        fileName = "FigureAnimationProfile",
        menuName = "Painted Alive/Figures/Animation Profile")]
    public sealed class FigureAnimationProfile :
        ScriptableObject
    {
        [Header("Locomotion Mapping")]
        [SerializeField, Min(0.1f)]
        private float locomotionReferenceSpeed = 6.25f;

        [SerializeField, Min(0f)]
        private float movingSpeedThreshold = 0.10f;

        [SerializeField, Min(0f)]
        private float sprintSpeedThreshold = 4.90f;

        [SerializeField, Min(0f)]
        private float jumpVerticalThreshold = 0.20f;

        [SerializeField, Min(0f)]
        private float landingMinimumFallSpeed = 0.75f;

        [SerializeField, Min(0f)]
        private float floatParameterDampTime = 0.08f;

        [Header("Animator Float Parameters")]
        [SerializeField]
        private string speedParameter = "Speed";

        [SerializeField]
        private string speed01Parameter = "Speed01";

        [SerializeField]
        private string forwardSpeedParameter = "ForwardSpeed";

        [SerializeField]
        private string sideSpeedParameter = "SideSpeed";

        [SerializeField]
        private string verticalSpeedParameter = "VerticalSpeed";

        [Header("Animator Bool Parameters")]
        [SerializeField]
        private string groundedParameter = "Grounded";

        [SerializeField]
        private string movingParameter = "Moving";

        [SerializeField]
        private string sprintingParameter = "Sprinting";

        [Header("Animator Trigger Parameters")]
        [SerializeField]
        private string jumpTriggerParameter = "Jump";

        [SerializeField]
        private string landTriggerParameter = "Land";

        public float LocomotionReferenceSpeed =>
            locomotionReferenceSpeed;

        public float MovingSpeedThreshold =>
            movingSpeedThreshold;

        public float SprintSpeedThreshold =>
            sprintSpeedThreshold;

        public float JumpVerticalThreshold =>
            jumpVerticalThreshold;

        public float LandingMinimumFallSpeed =>
            landingMinimumFallSpeed;

        public float FloatParameterDampTime =>
            floatParameterDampTime;

        public string SpeedParameter =>
            speedParameter;

        public string Speed01Parameter =>
            speed01Parameter;

        public string ForwardSpeedParameter =>
            forwardSpeedParameter;

        public string SideSpeedParameter =>
            sideSpeedParameter;

        public string VerticalSpeedParameter =>
            verticalSpeedParameter;

        public string GroundedParameter =>
            groundedParameter;

        public string MovingParameter =>
            movingParameter;

        public string SprintingParameter =>
            sprintingParameter;

        public string JumpTriggerParameter =>
            jumpTriggerParameter;

        public string LandTriggerParameter =>
            landTriggerParameter;

        private void OnValidate()
        {
            locomotionReferenceSpeed =
                Mathf.Max(
                    0.1f,
                    locomotionReferenceSpeed);

            movingSpeedThreshold =
                Mathf.Max(
                    0f,
                    movingSpeedThreshold);

            sprintSpeedThreshold =
                Mathf.Max(
                    movingSpeedThreshold,
                    sprintSpeedThreshold);

            jumpVerticalThreshold =
                Mathf.Max(
                    0f,
                    jumpVerticalThreshold);

            landingMinimumFallSpeed =
                Mathf.Max(
                    0f,
                    landingMinimumFallSpeed);

            floatParameterDampTime =
                Mathf.Max(
                    0f,
                    floatParameterDampTime);
        }
    }
}
