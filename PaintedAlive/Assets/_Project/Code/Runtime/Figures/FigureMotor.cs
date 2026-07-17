using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Figures
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FigureInputReader))]
    public sealed class FigureMotor : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private FigureMovementConfig config;

        [SerializeField]
        private Transform movementReference;

        [SerializeField]
        private FigureClarityState clarityState;

        private CharacterController characterController;
        private FigureInputReader inputReader;

        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float rotationVelocity;
        private float coyoteTimeRemaining;
        private float jumpBufferRemaining;

        private OilStrokeRuntime currentPaintSurface;

        private Vector3 currentPaintSurfaceNormal =
            Vector3.up;

        private float paintSurfaceContactRemaining;

        public Vector3 Velocity =>
            horizontalVelocity +
            Vector3.up * verticalVelocity;

        public bool IsGrounded =>
            characterController != null &&
            characterController.isGrounded;

        private void Awake()
        {
            characterController =
                GetComponent<CharacterController>();

            inputReader =
                GetComponent<FigureInputReader>();

            if (clarityState == null)
            {
                clarityState =
                    GetComponent<FigureClarityState>();
            }

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(FigureMotor)} on {name} " +
                    "requires a movement config.",
                    this);

                enabled = false;
                return;
            }

            if (movementReference == null)
            {
                Debug.LogError(
                    $"{nameof(FigureMotor)} on {name} " +
                    "requires a movement reference.",
                    this);

                enabled = false;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            UpdatePaintSurfaceContact(deltaTime);
            UpdateGroundTimers(deltaTime);
            UpdateJumpBuffer(deltaTime);
            UpdateHorizontalVelocity(deltaTime);
            UpdateVerticalVelocity(deltaTime);
            MoveCharacter(deltaTime);
            RotateCharacter(deltaTime);
        }

        public void Teleport(
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            if (characterController == null)
            {
                characterController =
                    GetComponent<CharacterController>();
            }

            bool controllerWasEnabled =
                characterController.enabled;

            characterController.enabled = false;

            transform.SetPositionAndRotation(
                worldPosition,
                worldRotation);

            characterController.enabled =
                controllerWasEnabled;

            ResetMotion();
        }

        public void ResetMotion()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            rotationVelocity = 0f;
            coyoteTimeRemaining = 0f;
            jumpBufferRemaining = 0f;

            currentPaintSurface = null;
            currentPaintSurfaceNormal = Vector3.up;
            paintSurfaceContactRemaining = 0f;
        }

        private void UpdateGroundTimers(
            float deltaTime)
        {
            if (characterController.isGrounded)
            {
                coyoteTimeRemaining =
                    config.CoyoteTime;

                if (verticalVelocity < 0f)
                {
                    verticalVelocity =
                        config.GroundedForce;
                }
            }
            else
            {
                coyoteTimeRemaining -= deltaTime;
            }
        }

        private void UpdateJumpBuffer(
            float deltaTime)
        {
            if (inputReader.JumpPressedThisFrame)
            {
                jumpBufferRemaining =
                    config.JumpBufferTime;
            }
            else
            {
                jumpBufferRemaining -= deltaTime;
            }
        }

        private void UpdateHorizontalVelocity(
            float deltaTime)
        {
            Vector2 moveInput = inputReader.Move;

            float inputMagnitude =
                Mathf.Clamp01(moveInput.magnitude);

            Vector3 cameraForward =
                Vector3.ProjectOnPlane(
                    movementReference.forward,
                    Vector3.up);

            Vector3 cameraRight =
                Vector3.ProjectOnPlane(
                    movementReference.right,
                    Vector3.up);

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 desiredDirection =
                cameraForward * moveInput.y +
                cameraRight * moveInput.x;

            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            GetPaintSurfaceModifiers(
                out float accelerationMultiplier,
                out float decelerationMultiplier,
                out float speedMultiplier,
                out float slideAcceleration);

            bool canSprint =
                clarityState == null ||
                clarityState.CanSprint;

            float maximumSpeed =
                inputReader.SprintHeld && canSprint
                    ? config.SprintSpeed
                    : config.WalkSpeed;

            if (clarityState != null)
            {
                maximumSpeed *=
                    clarityState.MovementMultiplier;
            }

            maximumSpeed *= speedMultiplier;

            Vector3 desiredVelocity =
                desiredDirection *
                (maximumSpeed * inputMagnitude);

            bool hasMovementInput =
                inputMagnitude > 0.01f;

            float acceleration;

            if (characterController.isGrounded)
            {
                acceleration = hasMovementInput
                    ? config.GroundAcceleration *
                      accelerationMultiplier
                    : config.GroundDeceleration *
                      decelerationMultiplier;
            }
            else
            {
                acceleration = hasMovementInput
                    ? config.AirAcceleration
                    : 0f;
            }

            if (acceleration > 0f)
            {
                horizontalVelocity =
                    Vector3.MoveTowards(
                        horizontalVelocity,
                        desiredVelocity,
                        acceleration * deltaTime);
            }

            if (characterController.isGrounded &&
                slideAcceleration > 0f)
            {
                Vector3 downhillDirection =
                    Vector3.ProjectOnPlane(
                        Vector3.down,
                        currentPaintSurfaceNormal);

                if (downhillDirection.sqrMagnitude >
                    0.001f)
                {
                    horizontalVelocity +=
                        downhillDirection.normalized *
                        slideAcceleration *
                        deltaTime;
                }
            }
        }

        private void UpdateVerticalVelocity(
            float deltaTime)
        {
            bool clarityAllowsJump =
                clarityState == null ||
                clarityState.CanJump;

            bool canJump =
                jumpBufferRemaining > 0f &&
                coyoteTimeRemaining > 0f &&
                clarityAllowsJump;

            if (canJump)
            {
                verticalVelocity =
                    Mathf.Sqrt(
                        config.JumpHeight *
                        -2f *
                        config.Gravity);

                jumpBufferRemaining = 0f;
                coyoteTimeRemaining = 0f;
            }

            verticalVelocity +=
                config.Gravity * deltaTime;

            verticalVelocity =
                Mathf.Max(
                    verticalVelocity,
                    -config.MaximumFallSpeed);
        }

        private void MoveCharacter(
            float deltaTime)
        {
            Vector3 motion =
                (
                    horizontalVelocity +
                    Vector3.up * verticalVelocity
                ) * deltaTime;

            CollisionFlags collisionFlags =
                characterController.Move(motion);

            if ((collisionFlags &
                 CollisionFlags.Above) != 0 &&
                verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }

            if ((collisionFlags &
                 CollisionFlags.Below) != 0 &&
                verticalVelocity < 0f)
            {
                verticalVelocity =
                    config.GroundedForce;
            }
        }

        private void RotateCharacter(
            float deltaTime)
        {
            Vector3 planarVelocity =
                Vector3.ProjectOnPlane(
                    horizontalVelocity,
                    Vector3.up);

            if (planarVelocity.sqrMagnitude < 0.01f)
            {
                return;
            }

            float targetAngle =
                Mathf.Atan2(
                    planarVelocity.x,
                    planarVelocity.z) *
                Mathf.Rad2Deg;

            float smoothedAngle =
                Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref rotationVelocity,
                    config.RotationSmoothTime,
                    Mathf.Infinity,
                    deltaTime);

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    smoothedAngle,
                    0f);
        }

        private void UpdatePaintSurfaceContact(
            float deltaTime)
        {
            if (paintSurfaceContactRemaining <= 0f)
            {
                currentPaintSurface = null;
                currentPaintSurfaceNormal = Vector3.up;
                return;
            }

            paintSurfaceContactRemaining -= deltaTime;
        }

        private void OnControllerColliderHit(
            ControllerColliderHit hit)
        {
            if (hit.normal.y < 0.35f)
            {
                return;
            }

            OilStrokeRuntime paintSurface =
                hit.collider
                    .GetComponentInParent<
                        OilStrokeRuntime>();

            if (paintSurface == null)
            {
                return;
            }

            currentPaintSurface = paintSurface;
            currentPaintSurfaceNormal = hit.normal;
            paintSurfaceContactRemaining = 0.15f;
        }

        private void GetPaintSurfaceModifiers(
            out float accelerationMultiplier,
            out float decelerationMultiplier,
            out float speedMultiplier,
            out float slideAcceleration)
        {
            accelerationMultiplier = 1f;
            decelerationMultiplier = 1f;
            speedMultiplier = 1f;
            slideAcceleration = 0f;

            if (currentPaintSurface == null)
            {
                return;
            }

            switch (currentPaintSurface.State)
            {
                case OilStrokeState.Wet:
                    accelerationMultiplier =
                        config.WetAccelerationMultiplier;

                    decelerationMultiplier =
                        config.WetDecelerationMultiplier;

                    speedMultiplier =
                        config.WetSpeedMultiplier;

                    slideAcceleration =
                        config.WetSlideAcceleration;

                    break;

                case OilStrokeState.Drying:
                    accelerationMultiplier =
                        config
                            .DryingAccelerationMultiplier;

                    decelerationMultiplier =
                        config
                            .DryingDecelerationMultiplier;

                    speedMultiplier =
                        config.DryingSpeedMultiplier;

                    slideAcceleration =
                        config.DryingSlideAcceleration;

                    break;
            }
        }
    }
}