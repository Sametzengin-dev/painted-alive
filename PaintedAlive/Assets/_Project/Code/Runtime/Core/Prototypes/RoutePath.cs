using UnityEngine;

namespace PaintedAlive.Core.Prototypes
{
    public sealed class RoutePath : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;

        public float TotalLength { get; private set; }

        private void Awake()
        {
            RecalculateLength();
        }

        private void OnValidate()
        {
            RecalculateLength();
        }

        public bool EvaluateProgress(
            Vector3 worldPosition,
            out float distanceAlongPath,
            out float normalizedProgress,
            out Vector3 closestPoint)
        {
            distanceAlongPath = 0f;
            normalizedProgress = 0f;
            closestPoint = worldPosition;

            if (waypoints == null || waypoints.Length < 2)
            {
                return false;
            }

            float closestDistanceSquared = float.MaxValue;
            float accumulatedDistance = 0f;

            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] == null ||
                    waypoints[i + 1] == null)
                {
                    continue;
                }

                Vector3 segmentStart =
                    waypoints[i].position;

                Vector3 segmentEnd =
                    waypoints[i + 1].position;

                Vector3 segment =
                    segmentEnd - segmentStart;

                float segmentLength =
                    segment.magnitude;

                if (segmentLength <= 0.001f)
                {
                    continue;
                }

                float segmentT = Mathf.Clamp01(
                    Vector3.Dot(
                        worldPosition - segmentStart,
                        segment) /
                    segment.sqrMagnitude);

                Vector3 candidatePoint =
                    segmentStart + segment * segmentT;

                float candidateDistanceSquared =
                    (worldPosition - candidatePoint).sqrMagnitude;

                if (candidateDistanceSquared <
                    closestDistanceSquared)
                {
                    closestDistanceSquared =
                        candidateDistanceSquared;

                    closestPoint = candidatePoint;

                    distanceAlongPath =
                        accumulatedDistance +
                        segmentLength * segmentT;
                }

                accumulatedDistance += segmentLength;
            }

            normalizedProgress = TotalLength > 0f
                ? Mathf.Clamp01(
                    distanceAlongPath / TotalLength)
                : 0f;

            return true;
        }

        private void RecalculateLength()
        {
            TotalLength = 0f;

            if (waypoints == null)
            {
                return;
            }

            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] == null ||
                    waypoints[i + 1] == null)
                {
                    continue;
                }

                TotalLength += Vector3.Distance(
                    waypoints[i].position,
                    waypoints[i + 1].position);
            }
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                return;
            }

            Gizmos.color = new Color(
                0.85f,
                0.2f,
                0.15f,
                1f);

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(
                    waypoints[i].position,
                    0.35f);

                if (i < waypoints.Length - 1 &&
                    waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(
                        waypoints[i].position,
                        waypoints[i + 1].position);
                }
            }
        }
    }
}
