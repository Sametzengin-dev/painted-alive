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

        public bool ValidatePlacement(
            Transform ignoredRoot,
            out string reason)
        {
            Vector3 halfExtents =
                new Vector3(
                    worldWidth * 0.5f +
                        obstructionPadding,
                    worldHeight * 0.5f +
                        obstructionPadding,
                    worldDepth * 0.5f +
                        obstructionPadding);

            Vector3 center =
                transform.position +
                transform.up *
                (
                    floorOffset +
                    worldHeight * 0.5f
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
