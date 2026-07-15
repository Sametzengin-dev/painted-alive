using UnityEngine;

namespace PaintedAlive.Figures
{
    [CreateAssetMenu(
        fileName = "FigureMovementConfig",
        menuName = "Painted Alive/Figures/Movement Config")]
    public sealed class FigureMovementConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4.25f;
        [SerializeField, Min(0f)] private float sprintSpeed = 6.25f;
        [SerializeField, Min(0f)] private float groundAcceleration = 28f;
        [SerializeField, Min(0f)] private float groundDeceleration = 36f;
        [SerializeField, Min(0f)] private float airAcceleration = 10f;
        [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.08f;

        [Header("Jump And Gravity")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.3f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedForce = -2f;
        [SerializeField, Min(0f)] private float maximumFallSpeed = 35f;

        [Header("Input Forgiveness")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirAcceleration => airAcceleration;
        public float RotationSmoothTime => rotationSmoothTime;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float GroundedForce => groundedForce;
        public float MaximumFallSpeed => maximumFallSpeed;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;

        private void OnValidate()
        {
            walkSpeed = Mathf.Max(0f, walkSpeed);
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            groundAcceleration = Mathf.Max(0f, groundAcceleration);
            groundDeceleration = Mathf.Max(0f, groundDeceleration);
            airAcceleration = Mathf.Max(0f, airAcceleration);
            rotationSmoothTime = Mathf.Max(0.01f, rotationSmoothTime);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            gravity = Mathf.Min(-0.01f, gravity);
            groundedForce = Mathf.Min(-0.01f, groundedForce);
            maximumFallSpeed = Mathf.Max(0.1f, maximumFallSpeed);
            coyoteTime = Mathf.Max(0f, coyoteTime);
            jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
        }
    }
}
