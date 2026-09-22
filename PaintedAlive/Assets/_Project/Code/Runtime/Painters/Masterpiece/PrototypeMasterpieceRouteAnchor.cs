using UnityEngine;

namespace PaintedAlive.Painters.Masterpiece
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceRouteAnchor :
        MonoBehaviour
    {
        [Header("Deployment Envelope")]
        [SerializeField, Min(0.5f)] private float worldWidth = 2.8f;
        [SerializeField, Min(0.5f)] private float worldHeight = 3.4f;
        [SerializeField, Min(0.1f)] private float worldDepth = 0.65f;
        [SerializeField, Min(0f)] private float floorOffset = 0.08f;

        [Header("Placement Validation")]
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField, Min(0f)] private float obstructionPadding = 0.12f;
        [SerializeField, Min(0f)] private float groundIgnoreHeight = 0.24f;

        [Header("Dynamic Deployment Placement")]
        [SerializeField, Min(2f)] private float preferredReferenceDistance = 3.8f;
        [SerializeField, Min(0f)] private float referenceFloorLift = 0.05f;

        [Header("Runtime Read Only")]
        [SerializeField] private bool lastPlacementClear;
        [SerializeField] private int lastObstructionCount;
        [SerializeField] private string lastValidation = "Not checked";

        public float WorldWidth => worldWidth;
        public float WorldHeight => worldHeight;
        public float WorldDepth => worldDepth;
        public float FloorOffset => floorOffset;
        public bool LastPlacementClear => lastPlacementClear;
        public int LastObstructionCount => lastObstructionCount;
        public string LastValidation => lastValidation;

        public Vector3 DeploymentPosition =>
            transform.position;

        public Quaternion DeploymentRotation =>
            transform.rotation;

        public bool TryPrepareNearReference(
            Transform placementReference,
            Transform ignoredRoot,
            Vector3 deploymentScale,
            out string reason)
        {
            if (placementReference == null)
            {
                return ValidatePlacement(
                    ignoredRoot,
                    deploymentScale,
                    out reason);
            }

            Vector3 originalPosition = transform.position;
            Quaternion originalRotation = transform.rotation;

            Vector3 forward = Vector3.ProjectOnPlane(
                placementReference.forward,
                Vector3.up).normalized;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3[] directions =
            {
                forward,
                (forward + right).normalized,
                (forward - right).normalized,
                right,
                -right,
                -forward
            };

            float preferred = Mathf.Max(2f, preferredReferenceDistance);
            float[] distances = { preferred, preferred + 1.4f };

            for (int distanceIndex = 0;
                 distanceIndex < distances.Length;
                 distanceIndex++)
            {
                for (int directionIndex = 0;
                     directionIndex < directions.Length;
                     directionIndex++)
                {
                    Vector3 direction = directions[directionIndex];
                    Vector3 candidate =
                        placementReference.position +
                        direction * distances[distanceIndex];

                    candidate.y =
                        placementReference.position.y +
                        referenceFloorLift;

                    transform.SetPositionAndRotation(
                        candidate,
                        Quaternion.LookRotation(-direction, Vector3.up));

                    if (ValidatePlacement(
                            ignoredRoot,
                            deploymentScale,
                            out _))
                    {
                        reason =
                            $"Oyuncuya yakın güvenli deploy alanı hazır ({distances[distanceIndex]:F1} m).";
                        lastValidation = reason;
                        return true;
                    }
                }
            }

            transform.SetPositionAndRotation(
                originalPosition,
                originalRotation);

            bool authoredClear = ValidatePlacement(
                ignoredRoot,
                deploymentScale,
                out string authoredReason);

            reason = authoredClear
                ? "Yakın alan dolu; authored route anchor kullanılacak."
                : $"Yakın ve authored deploy alanları dolu. {authoredReason}";

            lastValidation = reason;
            return authoredClear;
        }

        public bool ValidatePlacement(
            Transform ignoredRoot,
            out string reason)
        {
            return ValidatePlacement(
                ignoredRoot,
                Vector3.one,
                out reason);
        }

        public bool ValidatePlacement(
            Transform ignoredRoot,
            Vector3 deploymentScale,
            out string reason)
        {
            deploymentScale = new Vector3(
                Mathf.Max(0.1f, deploymentScale.x),
                Mathf.Max(0.1f, deploymentScale.y),
                Mathf.Max(0.1f, deploymentScale.z));

            Vector3 halfExtents =
                new Vector3(
                    worldWidth * deploymentScale.x * 0.5f +
                        obstructionPadding,
                    worldHeight * deploymentScale.y * 0.5f +
                        obstructionPadding,
                    worldDepth * deploymentScale.z * 0.5f +
                        obstructionPadding);

            Vector3 center =
                transform.position +
                transform.up *
                (
                    floorOffset * deploymentScale.y +
                    worldHeight * deploymentScale.y * 0.5f
                );

            Collider[] overlaps =
                Physics.OverlapBox(
                    center,
                    halfExtents,
                    transform.rotation,
                    obstructionMask,
                    QueryTriggerInteraction.Ignore);

            int obstructionCount = 0;
            string firstObstruction =
                string.Empty;

            for (int index = 0;
                 index < overlaps.Length;
                 index++)
            {
                Collider candidate =
                    overlaps[index];

                if (candidate == null ||
                    !candidate.enabled)
                {
                    continue;
                }

                if (ignoredRoot != null &&
                    candidate.transform.IsChildOf(
                        ignoredRoot))
                {
                    continue;
                }

                if (candidate.GetComponentInParent<
                        PrototypeMasterpieceWorldInstance>() != null)
                {
                    continue;
                }

                // Ground and very low floor trim are allowed. The deployment
                // envelope begins at the floor plane, so those colliders
                // would otherwise always count as an obstruction.
                if (candidate.bounds.max.y <=
                    transform.position.y +
                    groundIgnoreHeight)
                {
                    continue;
                }

                obstructionCount++;

                if (string.IsNullOrWhiteSpace(
                        firstObstruction))
                {
                    firstObstruction =
                        candidate.gameObject.name;
                }
            }

            lastObstructionCount =
                obstructionCount;

            lastPlacementClear =
                obstructionCount == 0;

            if (lastPlacementClear)
            {
                reason = "Route anchor clear.";
                lastValidation = reason;
                return true;
            }

            reason =
                $"Route anchor blocked by {obstructionCount} object(s). " +
                $"First: {firstObstruction}";

            lastValidation = reason;
            return false;
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 previousMatrix =
                Gizmos.matrix;

            Color previousColor =
                Gizmos.color;

            Gizmos.matrix =
                Matrix4x4.TRS(
                    transform.position +
                    transform.up *
                    (
                        floorOffset +
                        worldHeight * 0.5f
                    ),
                    transform.rotation,
                    Vector3.one);

            Gizmos.color =
                lastPlacementClear
                    ? new Color(
                        0.10f,
                        0.82f,
                        0.58f,
                        0.32f)
                    : new Color(
                        1f,
                        0.54f,
                        0.06f,
                        0.34f);

            Gizmos.DrawCube(
                Vector3.zero,
                new Vector3(
                    worldWidth,
                    worldHeight,
                    worldDepth));

            Gizmos.color =
                new Color(
                    1f,
                    0.78f,
                    0.16f,
                    0.95f);

            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(
                    worldWidth,
                    worldHeight,
                    worldDepth));

            Gizmos.DrawLine(
                Vector3.zero,
                Vector3.forward *
                Mathf.Max(
                    0.8f,
                    worldDepth * 2f));

            Gizmos.matrix =
                previousMatrix;

            Gizmos.color =
                previousColor;
        }
    }
}
