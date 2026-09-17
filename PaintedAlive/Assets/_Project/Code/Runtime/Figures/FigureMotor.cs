using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Environment.PrismaticReach;
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

        [Header("External Motion Safety")]
        [SerializeField, Min(0.01f)]
        private float externalVelocityDamping = 7f;

        [SerializeField, Min(0f)]
        private float maximumExternalHorizontalSpeed = 4.5f;

        [SerializeField, Min(0f)]
        private float maximumExternalUpwardSpeed = 2.4f;

        [SerializeField, Min(0f)]
        private float maximumExternalDownwardSpeed = 1.5f;

        [Header("Equipment Load - Runtime")]
        [SerializeField, Range(0.1f, 1f)]
        private float equipmentMovementMultiplier = 1f;

        [Header("Environment Surface - Runtime")]
        [SerializeField, Range(0.1f, 1f)]
        private float environmentMovementMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float environmentMovementMultiplierRemaining;

        private CharacterController characterController;
        private FigureInputReader inputReader;

        private Vector3 horizontalVelocity;
        private Vector3 externalVelocity;
        private float verticalVelocity;
        private float rotationVelocity;
        private float coyoteTimeRemaining;
        private float jumpBufferRemaining;

        private bool wasGroundedAtFrameStart;
        private bool traversalLaunchControlActive;
        private float traversalAirControlInitial = 1f;
        private float traversalAirControlMultiplier = 1f;
        private float traversalAirControlElapsed;
        private float traversalAirControlDuration;

        private OilStrokeRuntime currentPaintSurface;

        private Vector3 currentPaintSurfaceNormal =
            Vector3.up;

        private float paintSurfaceContactRemaining;

        public Vector3 Velocity =>
            horizontalVelocity +
            Vector3.up * verticalVelocity +
            externalVelocity;

        public bool IsGrounded =>
            characterController != null &&
            characterController.isGrounded;

        public float VerticalVelocity =>
            verticalVelocity;

        public float Gravity =>
            config != null
                ? config.Gravity
                : 0f;

        public bool WasGroundedAtFrameStart =>
            wasGroundedAtFrameStart;

        public bool TraversalLaunchControlActive =>
            traversalLaunchControlActive;

        public float TraversalAirControlMultiplier =>
            traversalAirControlMultiplier;

        public float EquipmentMovementMultiplier =>
            equipmentMovementMultiplier;

        public float EnvironmentMovementMultiplier =>
            environmentMovementMultiplier;

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

            wasGroundedAtFrameStart =
                characterController != null &&
                characterController.isGrounded;

            UpdatePaintSurfaceContact(deltaTime);
            UpdateEnvironmentMovementMultiplier(deltaTime);
            UpdateGroundTimers(deltaTime);
            UpdateJumpBuffer(deltaTime);
            UpdateTraversalLaunchControl(deltaTime);
            UpdateHorizontalVelocity(deltaTime);
            UpdateVerticalVelocity(deltaTime);
            UpdateExternalVelocity(deltaTime);
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

        /// <summary>
        /// Applies an authoritative/shared network pose while preserving the
        /// existing CharacterController ownership model. Used only by the M56
        /// friend-test network foundation on peers which are not currently the
        /// active Figure, and for hard reconciliation on the Figure peer.
        /// </summary>
        public void ApplyNetworkPose(
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 networkVelocity)
        {
            Teleport(
                worldPosition,
                worldRotation);

            horizontalVelocity = Vector3.ProjectOnPlane(
                networkVelocity,
                Vector3.up);
            verticalVelocity = networkVelocity.y;
            externalVelocity = Vector3.zero;
        }

        public void ResetMotion()
        {
            horizontalVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            rotationVelocity = 0f;
            coyoteTimeRemaining = 0f;
            jumpBufferRemaining = 0f;
            wasGroundedAtFrameStart = false;
            traversalLaunchControlActive = false;
            traversalAirControlInitial = 1f;
            traversalAirControlMultiplier = 1f;
            traversalAirControlElapsed = 0f;
            traversalAirControlDuration = 0f;

            currentPaintSurface = null;
            currentPaintSurfaceNormal = Vector3.up;
            paintSurfaceContactRemaining = 0f;

            environmentMovementMultiplier = 1f;
            environmentMovementMultiplierRemaining = 0f;
        }

        /// <summary>
        /// Applies a controller-native traversal launch without replacing the
        /// existing CharacterController movement loop. Gravity, camera and
        /// animation continue to use the normal FigureMotor state.
        /// </summary>
        public void ApplyTraversalLaunch(
            Vector3 launchVelocity,
            float initialHorizontalSteeringFactor,
            float steeringRestoreDuration)
        {
            if (!isActiveAndEnabled || config == null)
            {
                return;
            }

            horizontalVelocity =
                Vector3.ProjectOnPlane(
                    launchVelocity,
                    Vector3.up);

            verticalVelocity =
                launchVelocity.y;

            // A traversal launch owns the intended ballistic velocity.
            // Old knockback/external motion must not bend the authored arc.
            externalVelocity = Vector3.zero;

            coyoteTimeRemaining = 0f;
            jumpBufferRemaining = 0f;

            traversalAirControlInitial =
                Mathf.Clamp01(
                    initialHorizontalSteeringFactor);

            traversalAirControlMultiplier =
                traversalAirControlInitial;

            traversalAirControlElapsed = 0f;
            traversalAirControlDuration =
                Mathf.Max(
                    0.05f,
                    steeringRestoreDuration);

            traversalLaunchControlActive = true;
        }

        public void AddExternalImpulse(
            Vector3 velocityChange)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Vector3 candidateVelocity =
                externalVelocity + velocityChange;

            Vector3 horizontalCandidate =
                Vector3.ProjectOnPlane(
                    candidateVelocity,
                    Vector3.up);

            horizontalCandidate =
                Vector3.ClampMagnitude(
                    horizontalCandidate,
                    maximumExternalHorizontalSpeed);

            float verticalCandidate =
                Mathf.Clamp(
                    candidateVelocity.y,
                    -maximumExternalDownwardSpeed,
                    maximumExternalUpwardSpeed);

            externalVelocity =
                horizontalCandidate +
                Vector3.up * verticalCandidate;
        }

        public void SetEquipmentMovementMultiplier(
            float multiplier)
        {
            equipmentMovementMultiplier =
                Mathf.Clamp(multiplier, 0.1f, 1f);
        }

        /// <summary>
        /// Refreshes a short-lived movement multiplier supplied by a physical
        /// environment surface. Living Gallery Drying Paper calls this from the
        /// CharacterController hit callback, so the modifier is intentionally
        /// time-buffered rather than stored as permanent surface ownership.
        /// </summary>
        public void RefreshEnvironmentMovementMultiplier(
            float multiplier,
            float holdSeconds)
        {
            environmentMovementMultiplier =
                Mathf.Clamp(multiplier, 0.1f, 1f);

            environmentMovementMultiplierRemaining =
                Mathf.Max(
                    environmentMovementMultiplierRemaining,
                    Mathf.Max(0f, holdSeconds));
        }

        private void UpdateEnvironmentMovementMultiplier(
            float deltaTime)
        {
            if (environmentMovementMultiplierRemaining <= 0f)
            {
                environmentMovementMultiplier = 1f;
                environmentMovementMultiplierRemaining = 0f;
                return;
            }

            environmentMovementMultiplierRemaining =
                Mathf.Max(
                    0f,
                    environmentMovementMultiplierRemaining -
                    Mathf.Max(0f, deltaTime));

            if (environmentMovementMultiplierRemaining <= 0f)
            {
                environmentMovementMultiplier = 1f;
            }
        }

        private void UpdateTraversalLaunchControl(
            float deltaTime)
        {
            if (!traversalLaunchControlActive)
            {
                traversalAirControlMultiplier = 1f;
                return;
            }

            traversalAirControlElapsed += deltaTime;

            float t = Mathf.Clamp01(
                traversalAirControlElapsed /
                Mathf.Max(
                    0.05f,
                    traversalAirControlDuration));

            // SmoothStep avoids a visible input-authority snap while the
            // ballistic launch hands normal air steering back to the player.
            float smoothT =
                t * t * (3f - 2f * t);

            traversalAirControlMultiplier =
                Mathf.Lerp(
                    traversalAirControlInitial,
                    1f,
                    smoothT);

            if (t >= 1f)
            {
                traversalLaunchControlActive = false;
                traversalAirControlMultiplier = 1f;
            }
        }

        private void UpdateExternalVelocity(
            float deltaTime)
        {
            if (externalVelocity.sqrMagnitude <
                0.0001f)
            {
                externalVelocity = Vector3.zero;
                return;
            }

            float decay =
                Mathf.Exp(
                    -externalVelocityDamping *
                    deltaTime);

            externalVelocity *= decay;
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

            maximumSpeed *= equipmentMovementMultiplier;
            maximumSpeed *= environmentMovementMultiplier;

            maximumSpeed *= speedMultiplier;

            Vector3 desiredVelocity =
                desiredDirection *
                (maximumSpeed * inputMagnitude);

            bool hasMovementInput =
                inputMagnitude > 0.01f;

            float acceleration;

            bool useAirMovement =
                !characterController.isGrounded ||
                traversalLaunchControlActive;

            if (!useAirMovement)
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
                    ? config.AirAcceleration *
                      traversalAirControlMultiplier
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
                !traversalLaunchControlActive &&
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
                !traversalLaunchControlActive &&
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
                    Vector3.up * verticalVelocity +
                    externalVelocity
                ) * deltaTime;

            CollisionFlags collisionFlags =
                characterController.Move(motion);

            if ((collisionFlags &
                 CollisionFlags.Above) != 0 &&
                verticalVelocity > 0f)
            {
                verticalVelocity = 0f;

                if (externalVelocity.y > 0f)
                {
                    externalVelocity.y = 0f;
                }
            }

            if ((collisionFlags &
                 CollisionFlags.Below) != 0 &&
                verticalVelocity < 0f)
            {
                verticalVelocity =
                    config.GroundedForce;

                if (externalVelocity.y < 0f)
                {
                    externalVelocity.y = 0f;
                }
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
            // Trampoline contact is evaluated from the existing
            // CharacterController collision callback so side/underside
            // contacts can be rejected using the real contact normal.
            PrismaticReachTrampolineSurface
                .TryHandleControllerHit(
                    this,
                    hit);

            // Living Gallery wet/dry paper supplies a short-lived speed
            // multiplier through the same controller-native contact path.
            DryingPaperSurface
                .TryHandleControllerHit(
                    this,
                    hit);

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

        private void OnValidate()
        {
            externalVelocityDamping =
                Mathf.Max(
                    0.01f,
                    externalVelocityDamping);

            maximumExternalHorizontalSpeed =
                Mathf.Max(
                    0f,
                    maximumExternalHorizontalSpeed);

            maximumExternalUpwardSpeed =
                Mathf.Max(
                    0f,
                    maximumExternalUpwardSpeed);

            maximumExternalDownwardSpeed =
                Mathf.Max(
                    0f,
                    maximumExternalDownwardSpeed);

            equipmentMovementMultiplier =
                Mathf.Clamp(
                    equipmentMovementMultiplier,
                    0.1f,
                    1f);

            environmentMovementMultiplier =
                Mathf.Clamp(
                    environmentMovementMultiplier,
                    0.1f,
                    1f);

            environmentMovementMultiplierRemaining =
                Mathf.Max(
                    0f,
                    environmentMovementMultiplierRemaining);
        }
    }
}
