using System;
using PaintedAlive.Core.RoleAuthority;
using UnityEngine;

namespace PaintedAlive.Figures
{
    /// <summary>
    /// Local playtest safety net. Records recent grounded Figure poses and
    /// returns the Figure to the newest still-valid nearby pose after a real
    /// void fall. It does not reset the match and does not replace authored
    /// map FallZone/Respawn systems.
    /// </summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class FigureVoidRecoveryController : MonoBehaviour
    {
        [Serializable]
        private struct SafePose
        {
            public Vector3 position;
            public Quaternion rotation;
            public float capturedAt;
        }

        private const int HitBufferSize = 32;

        [SerializeField] private FigureMotor figureMotor;
        [SerializeField, Min(0.05f)] private float safePoseSampleInterval = 0.18f;
        [SerializeField, Min(0.1f)] private float minimumAirborneSeconds = 0.9f;
        [SerializeField, Min(2f)] private float minimumVerticalDrop = 9f;
        [SerializeField, Min(2f)] private float landingProbeDistance = 38f;
        [SerializeField, Range(10f, 89f)] private float maximumLandingSlope = 62f;
        [SerializeField, Min(0.01f)] private float respawnVerticalOffset = 0.12f;
        [SerializeField, Min(1)] private int safePoseHistorySize = 18;
        [SerializeField, Min(0.1f)] private float recoveryCooldown = 0.75f;

        [Header("Runtime - Read Only")]
        [SerializeField] private int recoveryCount;
        [SerializeField] private float airborneSeconds;
        [SerializeField] private string lastRecoveryReason = "Not triggered";
        [SerializeField] private Vector3 mostRecentSafePosition;

        private SafePose[] history;
        private int historyWriteIndex;
        private int historyCount;
        private float nextSafeSampleAt;
        private float nextRecoveryAllowedAt;
        private readonly RaycastHit[] hits = new RaycastHit[HitBufferSize];

        public int RecoveryCount => recoveryCount;
        public string LastRecoveryReason => lastRecoveryReason;
        public Vector3 MostRecentSafePosition => mostRecentSafePosition;

        public void Configure(FigureMotor motor)
        {
            figureMotor = motor;
            EnsureHistory();
        }

        private void Awake()
        {
            figureMotor ??= GetComponent<FigureMotor>();
            EnsureHistory();
        }

        private void OnValidate()
        {
            safePoseSampleInterval = Mathf.Max(0.05f, safePoseSampleInterval);
            minimumAirborneSeconds = Mathf.Max(0.1f, minimumAirborneSeconds);
            minimumVerticalDrop = Mathf.Max(2f, minimumVerticalDrop);
            landingProbeDistance = Mathf.Max(2f, landingProbeDistance);
            safePoseHistorySize = Mathf.Clamp(safePoseHistorySize, 4, 64);
            recoveryCooldown = Mathf.Max(0.1f, recoveryCooldown);
        }

        private void Update()
        {
            if (figureMotor == null ||
                !Application.isPlaying)
            {
                return;
            }

            if (PrototypeRoleAuthorityResolver.IsPainter(out _))
            {
                airborneSeconds = 0f;
                return;
            }

            if (figureMotor.IsGrounded)
            {
                airborneSeconds = 0f;

                if (Time.time >= nextSafeSampleAt)
                {
                    CaptureSafePose();
                    nextSafeSampleAt =
                        Time.time + safePoseSampleInterval;
                }

                return;
            }

            airborneSeconds += Time.deltaTime;

            if (Time.time < nextRecoveryAllowedAt ||
                historyCount == 0 ||
                airborneSeconds < minimumAirborneSeconds ||
                figureMotor.Velocity.y > -1.5f)
            {
                return;
            }

            SafePose newest = GetHistoryPose(0);
            float verticalDrop =
                newest.position.y - transform.position.y;

            if (verticalDrop < minimumVerticalDrop)
                return;

            if (HasPotentialLandingBelow(
                    transform.position,
                    landingProbeDistance))
            {
                return;
            }

            if (!TryResolveRecoveryPose(
                    out Vector3 recoveryPosition,
                    out Quaternion recoveryRotation))
            {
                lastRecoveryReason =
                    "Void detected but no recent grounded pose remained valid";
                return;
            }

            figureMotor.Teleport(
                recoveryPosition,
                recoveryRotation);

            recoveryCount++;
            airborneSeconds = 0f;
            nextRecoveryAllowedAt =
                Time.time + recoveryCooldown;
            nextSafeSampleAt =
                Time.time + safePoseSampleInterval;

            lastRecoveryReason =
                "Recovered to recent grounded pose near the fall";
        }

        private void EnsureHistory()
        {
            int requested = Mathf.Clamp(
                safePoseHistorySize,
                4,
                64);

            if (history != null &&
                history.Length == requested)
            {
                return;
            }

            history = new SafePose[requested];
            historyWriteIndex = 0;
            historyCount = 0;
        }

        private void CaptureSafePose()
        {
            EnsureHistory();

            SafePose pose = new SafePose
            {
                position = transform.position,
                rotation = Quaternion.Euler(
                    0f,
                    transform.eulerAngles.y,
                    0f),
                capturedAt = Time.time
            };

            history[historyWriteIndex] = pose;
            historyWriteIndex =
                (historyWriteIndex + 1) % history.Length;
            historyCount =
                Mathf.Min(historyCount + 1, history.Length);
            mostRecentSafePosition = pose.position;
        }

        private SafePose GetHistoryPose(int newestOffset)
        {
            int index =
                historyWriteIndex - 1 - newestOffset;

            while (index < 0)
                index += history.Length;

            return history[index % history.Length];
        }

        private bool TryResolveRecoveryPose(
            out Vector3 position,
            out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;

            for (int offset = 0;
                 offset < historyCount;
                 offset++)
            {
                SafePose pose = GetHistoryPose(offset);

                if (!TrySnapPoseToGround(
                        pose,
                        out Vector3 groundedPosition))
                {
                    continue;
                }

                if (!IsCapsulePoseFree(
                        groundedPosition,
                        pose.rotation))
                {
                    continue;
                }

                position =
                    groundedPosition +
                    Vector3.up * respawnVerticalOffset;
                rotation = pose.rotation;
                return true;
            }

            return false;
        }

        private bool TrySnapPoseToGround(
            SafePose pose,
            out Vector3 groundedPosition)
        {
            groundedPosition = default;

            Vector3 origin =
                pose.position + Vector3.up * 1.5f;

            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                hits,
                4f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            RaycastHit best = default;
            float nearest = float.PositiveInfinity;

            for (int index = 0; index < count; index++)
            {
                RaycastHit hit = hits[index];

                if (!IsValidEnvironmentHit(hit) ||
                    hit.distance >= nearest ||
                    Vector3.Angle(hit.normal, Vector3.up) >
                        maximumLandingSlope)
                {
                    continue;
                }

                nearest = hit.distance;
                best = hit;
            }

            if (best.collider == null)
                return false;

            groundedPosition =
                new Vector3(
                    pose.position.x,
                    best.point.y,
                    pose.position.z);

            return true;
        }

        private bool HasPotentialLandingBelow(
            Vector3 worldPosition,
            float distance)
        {
            Vector3 origin =
                worldPosition + Vector3.up * 0.25f;

            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                hits,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < count; index++)
            {
                RaycastHit hit = hits[index];

                if (!IsValidEnvironmentHit(hit))
                    continue;

                if (Vector3.Angle(hit.normal, Vector3.up) <=
                    maximumLandingSlope)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsValidEnvironmentHit(RaycastHit hit)
        {
            if (hit.collider == null ||
                hit.collider.isTrigger)
            {
                return false;
            }

            FigureMotor hitFigure =
                hit.collider.GetComponentInParent<FigureMotor>();

            return hitFigure == null ||
                   hitFigure != figureMotor;
        }

        private bool IsCapsulePoseFree(
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            CharacterController controller =
                figureMotor != null
                    ? figureMotor.GetComponent<CharacterController>()
                    : null;

            if (controller == null)
                return true;

            Vector3 up = worldRotation * Vector3.up;
            Vector3 center =
                worldPosition +
                worldRotation * controller.center +
                up * 0.12f;

            float radius =
                Mathf.Max(
                    0.05f,
                    controller.radius * 0.70f);

            float halfHeight =
                Mathf.Max(
                    radius,
                    controller.height * 0.5f -
                    radius -
                    0.08f);

            Vector3 p0 = center + up * halfHeight;
            Vector3 p1 = center - up * halfHeight;

            Collider[] overlaps = Physics.OverlapCapsule(
                p0,
                p1,
                radius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider overlap = overlaps[index];

                if (overlap == null ||
                    overlap.isTrigger)
                {
                    continue;
                }

                FigureMotor owner =
                    overlap.GetComponentInParent<FigureMotor>();

                if (owner == figureMotor)
                    continue;

                // A tiny overlap with the support surface is acceptable.
                Bounds bounds = overlap.bounds;
                if (bounds.max.y <= worldPosition.y + 0.22f)
                    continue;

                return false;
            }

            return true;
        }
    }
}
