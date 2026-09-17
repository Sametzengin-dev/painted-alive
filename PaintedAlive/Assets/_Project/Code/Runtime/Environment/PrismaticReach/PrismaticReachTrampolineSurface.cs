using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;
using UnityEngine.Events;

namespace PaintedAlive.Environment.PrismaticReach
{
    /// <summary>
    /// Reusable controller-native trampoline binding for Prismatic Reach.
    ///
    /// This component does not create or own collision geometry. Instead it
    /// references the dedicated imported sail collision colliders and registers
    /// them as trampoline contacts. FigureMotor forwards CharacterController
    /// collision hits here, preserving the existing movement/gravity system.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrismaticReachTrampolineSurface : MonoBehaviour
    {
        private sealed class MotorContactState
        {
            public int LastContactFrame = -1000;
            public bool TriggeredDuringCurrentContact;
            public float NextAllowedLaunchTime;
        }

        private static readonly Dictionary<Collider, PrismaticReachTrampolineSurface>
            ColliderRegistry = new Dictionary<Collider, PrismaticReachTrampolineSurface>();

        [Header("Authored Helpers")]
        [SerializeField] private Transform centerReference;
        [SerializeField] private Transform landingTarget;

        [Header("Existing Dedicated Collision")]
        [SerializeField] private Collider[] surfaceColliders;

        [Header("Ballistic Launch")]
        [SerializeField, Min(0.25f)] private float apexHeight = 3.0f;
        [SerializeField, Min(0f)] private float landingLift = 0.08f;

        [Header("Landing Detection")]
        [SerializeField, Min(0f)] private float minimumDownwardVelocity = 2.0f;
        [SerializeField, Range(0f, 1f)] private float minimumTopSurfaceNormalY = 0.45f;
        [SerializeField, Min(0.01f)] private float retriggerCooldown = 0.25f;

        [Header("Air Steering")]
        [SerializeField, Range(0f, 1f)] private float initialHorizontalSteeringFactor = 0.18f;
        [SerializeField, Min(0.05f)] private float steeringRestoreDuration = 0.65f;

        [Header("Optional Presentation Hook")]
        [SerializeField] private UnityEvent onBounce = new UnityEvent();

        [Header("Runtime Read Only")]
        [SerializeField] private float lastComputedFlightDuration;
        [SerializeField] private float lastComputedHorizontalSpeed;
        [SerializeField] private Vector3 lastLaunchVelocity;
        [SerializeField] private string lastBounceStatus = "Waiting for contact";

        private readonly Dictionary<FigureMotor, MotorContactState> contactStates =
            new Dictionary<FigureMotor, MotorContactState>();

        public Transform CenterReference => centerReference;
        public Transform LandingTarget => landingTarget;
        public Collider[] SurfaceColliders => surfaceColliders;
        public float ApexHeight => apexHeight;
        public float MinimumDownwardVelocity => minimumDownwardVelocity;
        public float MinimumTopSurfaceNormalY => minimumTopSurfaceNormalY;
        public float InitialHorizontalSteeringFactor => initialHorizontalSteeringFactor;
        public float SteeringRestoreDuration => steeringRestoreDuration;
        public float LastComputedFlightDuration => lastComputedFlightDuration;
        public float LastComputedHorizontalSpeed => lastComputedHorizontalSpeed;
        public Vector3 LastLaunchVelocity => lastLaunchVelocity;
        public string LastBounceStatus => lastBounceStatus;
        public UnityEvent OnBounce => onBounce;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ColliderRegistry.Clear();
        }

        private void OnEnable()
        {
            RegisterColliders();
        }

        private void OnDisable()
        {
            UnregisterColliders();
            contactStates.Clear();
        }

        private void OnValidate()
        {
            apexHeight = Mathf.Max(0.25f, apexHeight);
            landingLift = Mathf.Max(0f, landingLift);
            minimumDownwardVelocity = Mathf.Max(0f, minimumDownwardVelocity);
            minimumTopSurfaceNormalY = Mathf.Clamp01(minimumTopSurfaceNormalY);
            retriggerCooldown = Mathf.Max(0.01f, retriggerCooldown);
            initialHorizontalSteeringFactor = Mathf.Clamp01(initialHorizontalSteeringFactor);
            steeringRestoreDuration = Mathf.Max(0.05f, steeringRestoreDuration);
        }

        public void Configure(
            Transform newCenterReference,
            Transform newLandingTarget,
            Collider[] newSurfaceColliders,
            float newApexHeight,
            float newMinimumDownwardVelocity,
            float newMinimumTopSurfaceNormalY,
            float newInitialHorizontalSteeringFactor,
            float newSteeringRestoreDuration,
            float newRetriggerCooldown)
        {
            UnregisterColliders();

            centerReference = newCenterReference;
            landingTarget = newLandingTarget;
            surfaceColliders = newSurfaceColliders;
            apexHeight = Mathf.Max(0.25f, newApexHeight);
            minimumDownwardVelocity = Mathf.Max(0f, newMinimumDownwardVelocity);
            minimumTopSurfaceNormalY = Mathf.Clamp01(newMinimumTopSurfaceNormalY);
            initialHorizontalSteeringFactor = Mathf.Clamp01(newInitialHorizontalSteeringFactor);
            steeringRestoreDuration = Mathf.Max(0.05f, newSteeringRestoreDuration);
            retriggerCooldown = Mathf.Max(0.01f, newRetriggerCooldown);

            if (isActiveAndEnabled)
            {
                RegisterColliders();
            }
        }

        /// <summary>
        /// Called by FigureMotor from its native CharacterController collision callback.
        /// No trigger volume, Rigidbody, teleport, or second movement loop is used.
        /// </summary>
        public static bool TryHandleControllerHit(
            FigureMotor motor,
            ControllerColliderHit hit)
        {
            if (motor == null || hit == null || hit.collider == null)
            {
                return false;
            }

            if (!ColliderRegistry.TryGetValue(hit.collider, out PrismaticReachTrampolineSurface surface) ||
                surface == null ||
                !surface.isActiveAndEnabled)
            {
                return false;
            }

            return surface.TryBounce(motor, hit);
        }

        private bool TryBounce(
            FigureMotor motor,
            ControllerColliderHit hit)
        {
            MotorContactState state = GetOrCreateState(motor);

            bool continuingSameContact =
                state.LastContactFrame >= Time.frameCount - 1;

            if (!continuingSameContact)
            {
                state.TriggeredDuringCurrentContact = false;
            }

            state.LastContactFrame = Time.frameCount;

            if (state.TriggeredDuringCurrentContact)
            {
                lastBounceStatus = "Ignored: already triggered during current contact";
                return false;
            }

            if (Time.time < state.NextAllowedLaunchTime)
            {
                lastBounceStatus = "Ignored: short retrigger cooldown";
                return false;
            }

            if (!motor.isActiveAndEnabled)
            {
                lastBounceStatus = "Ignored: FigureMotor is not authoritative/active";
                return false;
            }

            if (landingTarget == null || centerReference == null)
            {
                lastBounceStatus = "Ignored: helper reference missing";
                return false;
            }

            // A grounded Figure walking onto the cloth is not considered a landing.
            // The bounce is reserved for an airborne/downward contact.
            if (motor.WasGroundedAtFrameStart)
            {
                lastBounceStatus = "Ignored: Figure was already grounded before contact";
                return false;
            }

            float downwardVelocity = -motor.VerticalVelocity;
            if (downwardVelocity < minimumDownwardVelocity)
            {
                lastBounceStatus =
                    $"Ignored: downward speed {downwardVelocity:F2} < {minimumDownwardVelocity:F2}";
                return false;
            }

            if (hit.normal.y < minimumTopSurfaceNormalY)
            {
                lastBounceStatus =
                    $"Ignored: contact normal Y {hit.normal.y:F2} < {minimumTopSurfaceNormalY:F2}";
                return false;
            }

            float gravity = motor.Gravity;
            if (gravity >= -0.01f)
            {
                lastBounceStatus = "Ignored: FigureMotor gravity is invalid";
                return false;
            }

            Vector3 start = motor.transform.position;
            Vector3 target = landingTarget.position + Vector3.up * landingLift;

            if (!TryCalculateBallisticVelocity(
                    start,
                    target,
                    gravity,
                    out Vector3 launchVelocity,
                    out float flightDuration))
            {
                lastBounceStatus = "Ignored: ballistic solution failed";
                return false;
            }

            state.TriggeredDuringCurrentContact = true;
            state.NextAllowedLaunchTime = Time.time + retriggerCooldown;

            lastLaunchVelocity = launchVelocity;
            lastComputedFlightDuration = flightDuration;
            lastComputedHorizontalSpeed =
                Vector3.ProjectOnPlane(launchVelocity, Vector3.up).magnitude;
            lastBounceStatus =
                $"BOUNCE -> {landingTarget.name} | flight={flightDuration:F2}s | " +
                $"horizontal={lastComputedHorizontalSpeed:F2}m/s";

            motor.ApplyTraversalLaunch(
                launchVelocity,
                initialHorizontalSteeringFactor,
                steeringRestoreDuration);

            onBounce?.Invoke();
            return true;
        }

        private bool TryCalculateBallisticVelocity(
            Vector3 start,
            Vector3 target,
            float gravity,
            out Vector3 launchVelocity,
            out float flightDuration)
        {
            launchVelocity = Vector3.zero;
            flightDuration = 0f;

            float gravityMagnitude = -gravity;
            if (gravityMagnitude <= 0.01f)
            {
                return false;
            }

            float apexY = Mathf.Max(start.y, target.y) + apexHeight;
            float riseHeight = Mathf.Max(0.05f, apexY - start.y);
            float fallHeight = Mathf.Max(0f, apexY - target.y);

            float verticalLaunchSpeed =
                Mathf.Sqrt(2f * gravityMagnitude * riseHeight);

            float timeToApex = verticalLaunchSpeed / gravityMagnitude;
            float timeFromApex =
                Mathf.Sqrt(2f * fallHeight / gravityMagnitude);

            flightDuration = timeToApex + timeFromApex;
            if (flightDuration <= 0.05f ||
                float.IsNaN(flightDuration) ||
                float.IsInfinity(flightDuration))
            {
                return false;
            }

            Vector3 planarDelta =
                Vector3.ProjectOnPlane(target - start, Vector3.up);

            Vector3 horizontalVelocity =
                planarDelta / flightDuration;

            launchVelocity =
                horizontalVelocity +
                Vector3.up * verticalLaunchSpeed;

            return
                IsFinite(launchVelocity.x) &&
                IsFinite(launchVelocity.y) &&
                IsFinite(launchVelocity.z);
        }

        private MotorContactState GetOrCreateState(FigureMotor motor)
        {
            if (!contactStates.TryGetValue(motor, out MotorContactState state))
            {
                state = new MotorContactState();
                contactStates.Add(motor, state);
            }

            return state;
        }

        private void RegisterColliders()
        {
            if (surfaceColliders == null)
            {
                return;
            }

            for (int i = 0; i < surfaceColliders.Length; i++)
            {
                Collider collider = surfaceColliders[i];
                if (collider == null)
                {
                    continue;
                }

                if (ColliderRegistry.TryGetValue(
                        collider,
                        out PrismaticReachTrampolineSurface existing) &&
                    existing != null &&
                    existing != this)
                {
                    Debug.LogWarning(
                        $"Trampoline collider '{collider.name}' is already registered by " +
                        $"'{existing.name}'. Keeping the existing binding.",
                        collider);
                    continue;
                }

                ColliderRegistry[collider] = this;
            }
        }

        private void UnregisterColliders()
        {
            if (surfaceColliders == null)
            {
                return;
            }

            for (int i = 0; i < surfaceColliders.Length; i++)
            {
                Collider collider = surfaceColliders[i];
                if (collider == null)
                {
                    continue;
                }

                if (ColliderRegistry.TryGetValue(
                        collider,
                        out PrismaticReachTrampolineSurface existing) &&
                    existing == this)
                {
                    ColliderRegistry.Remove(collider);
                }
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (centerReference != null)
            {
                Gizmos.DrawWireSphere(centerReference.position, 0.18f);
            }

            if (landingTarget != null)
            {
                Gizmos.DrawWireSphere(landingTarget.position, 0.24f);
            }

            if (centerReference != null && landingTarget != null)
            {
                Gizmos.DrawLine(centerReference.position, landingTarget.position);
            }
        }
#endif
    }
}
