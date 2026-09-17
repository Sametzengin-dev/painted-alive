using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    [DisallowMultipleComponent]
    public sealed class PalimpsestRespawnCoordinator : MonoBehaviour,
        IPalimpsestResettable
    {
        [SerializeField] private PalimpsestRespawnPoint[] respawnPoints;
        [SerializeField] private PalimpsestClearanceVolume[] unsafeMechanismVolumes;
        [SerializeField] private LayerMask environmentCollisionMask;
        [SerializeField, Min(0f)] private float verticalOffset = 0.08f;
        [SerializeField, Min(0.05f)] private float sameFigureCooldown = 0.35f;
        [SerializeField] private int respawnRevision;

        private readonly Dictionary<FigureMotor, PalimpsestRespawnPoint> lastRegionalRespawn =
            new Dictionary<FigureMotor, PalimpsestRespawnPoint>();
        private readonly Dictionary<FigureMotor, float> nextAllowedRespawnTime =
            new Dictionary<FigureMotor, float>();

        public int RespawnRevision => respawnRevision;
        public int RespawnPointCount => respawnPoints != null ? respawnPoints.Length : 0;

        public void Configure(
            PalimpsestRespawnPoint[] points,
            PalimpsestClearanceVolume[] unsafeVolumes,
            LayerMask collisionMask)
        {
            respawnPoints = points ?? System.Array.Empty<PalimpsestRespawnPoint>();
            unsafeMechanismVolumes = unsafeVolumes ?? System.Array.Empty<PalimpsestClearanceVolume>();
            environmentCollisionMask = collisionMask;
        }

        public void NotifyRegionEntered(
            FigureMotor figure,
            PalimpsestRespawnPoint regionalPoint)
        {
            if (figure == null || regionalPoint == null)
                return;
            lastRegionalRespawn[figure] = regionalPoint;
        }

        public bool TryRespawn(
            FigureMotor figure,
            PalimpsestRespawnPoint preferredPoint)
        {
            if (figure == null)
                return false;

            if (nextAllowedRespawnTime.TryGetValue(figure, out float allowedAt) &&
                Time.time < allowedAt)
                return false;

            PalimpsestRespawnPoint candidate = preferredPoint;
            if (candidate == null)
                lastRegionalRespawn.TryGetValue(figure, out candidate);

            if (!TryResolveSafePose(figure, candidate, out Vector3 position, out Quaternion rotation))
            {
                candidate = FindNearestSafePoint(figure);
                if (!TryResolveSafePose(figure, candidate, out position, out rotation))
                    return false;
            }

            nextAllowedRespawnTime[figure] = Time.time + sameFigureCooldown;
            figure.Teleport(position, rotation);
            respawnRevision++;
            return true;
        }

        private PalimpsestRespawnPoint FindNearestSafePoint(FigureMotor figure)
        {
            if (respawnPoints == null || figure == null)
                return null;

            PalimpsestRespawnPoint best = null;
            float bestDistance = float.PositiveInfinity;

            foreach (PalimpsestRespawnPoint point in respawnPoints)
            {
                if (point == null)
                    continue;

                if (!TryResolveSafePose(figure, point, out _, out _))
                    continue;

                float distance = (point.transform.position - figure.transform.position).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                best = point;
                bestDistance = distance;
            }

            return best;
        }

        private bool TryResolveSafePose(
            FigureMotor figure,
            PalimpsestRespawnPoint point,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (figure == null || point == null)
                return false;

            rotation = point.transform.rotation;
            Vector3 up = rotation * Vector3.up;
            Vector3 basePosition = point.transform.position + up * verticalOffset;

            if (TryPose(figure, basePosition, rotation))
            {
                position = basePosition;
                return true;
            }

            const int samples = 8;
            float radius = Mathf.Max(0.15f, point.SafeRadius * 0.75f);
            for (int index = 0; index < samples; index++)
            {
                float angle = (360f / samples) * index;
                Vector3 offset = Quaternion.AngleAxis(angle, up) * (rotation * Vector3.forward) * radius;
                Vector3 sample = basePosition + offset;
                if (!TryPose(figure, sample, rotation))
                    continue;

                position = sample;
                return true;
            }

            return false;
        }

        private bool TryPose(
            FigureMotor figure,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            foreach (PalimpsestClearanceVolume unsafeVolume in unsafeMechanismVolumes)
            {
                if (unsafeVolume != null && unsafeVolume.ContainsPoint(worldPosition))
                    return false;
            }

            CharacterController controller = figure.GetComponent<CharacterController>();
            if (controller == null)
                return true;

            Vector3 up = worldRotation * Vector3.up;
            Vector3 center = worldPosition + worldRotation * controller.center + up * 0.05f;
            float radius = Mathf.Max(0.05f, controller.radius * 0.78f);
            float halfHeight = Mathf.Max(radius, controller.height * 0.5f - radius);
            Vector3 p1 = center + up * halfHeight;
            Vector3 p2 = center - up * halfHeight;

            int mask = environmentCollisionMask.value;
            if (mask == 0)
                return true;

            return !Physics.CheckCapsule(
                p1,
                p2,
                radius,
                mask,
                QueryTriggerInteraction.Ignore);
        }

        public void ResetPalimpsestState()
        {
            lastRegionalRespawn.Clear();
            nextAllowedRespawnTime.Clear();
        }
    }
}
