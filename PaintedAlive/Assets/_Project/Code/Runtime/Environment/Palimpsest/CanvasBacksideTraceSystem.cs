using System;
using System.Collections.Generic;
using System.Linq;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    /// <summary>
    /// Keeps exact Figure transforms private to the gameplay side and exposes only
    /// a deliberately coarse trace around the authored A/B/C trace helpers.
    /// Supports multiple Figures without ever publishing their individual world
    /// positions through this presentation seam.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CanvasBacksideTraceSystem : MonoBehaviour,
        IPalimpsestResettable
    {
        [SerializeField] private Transform[] traceHelpers;
        [SerializeField, Min(0.1f)] private float uncertaintyRadius = 3f;
        [SerializeField, Min(0.05f)] private float updateInterval = 0.35f;
        [SerializeField] private Vector3 approximateWorldPosition;
        [SerializeField] private bool hasApproximateTrace;
        [SerializeField] private int traceRevision;
        [SerializeField] private int trackedFigureCount;

        private readonly HashSet<FigureMotor> trackedFigures =
            new HashSet<FigureMotor>();
        private float nextUpdateAt;

        public event Action<Vector3> ApproximateTraceUpdated;

        public bool HasApproximateTrace => hasApproximateTrace;
        public Vector3 ApproximateWorldPosition => approximateWorldPosition;
        public float UncertaintyRadius => uncertaintyRadius;
        public int TraceRevision => traceRevision;
        public int TrackedFigureCount => trackedFigureCount;

        public void Configure(Transform[] helpers, float radius)
        {
            traceHelpers = helpers ?? Array.Empty<Transform>();
            uncertaintyRadius = Mathf.Max(0.1f, radius);
        }

        public void BeginTracking(FigureMotor figure)
        {
            if (figure == null)
                return;

            trackedFigures.Add(figure);
            trackedFigureCount = trackedFigures.Count;
            nextUpdateAt = 0f;
            UpdateApproximateTrace(force: true);
        }

        public void StopTracking(FigureMotor figure)
        {
            if (figure != null)
                trackedFigures.Remove(figure);

            RemoveDestroyedFigures();
            trackedFigureCount = trackedFigures.Count;
            if (trackedFigureCount > 0)
            {
                nextUpdateAt = 0f;
                UpdateApproximateTrace(force: true);
                return;
            }

            hasApproximateTrace = false;
            approximateWorldPosition = Vector3.zero;
        }

        private void Update()
        {
            RemoveDestroyedFigures();
            trackedFigureCount = trackedFigures.Count;

            if (trackedFigureCount == 0)
            {
                hasApproximateTrace = false;
                return;
            }

            if (Time.time < nextUpdateAt)
                return;

            UpdateApproximateTrace(force: false);
        }

        private void UpdateApproximateTrace(bool force)
        {
            RemoveDestroyedFigures();
            if (trackedFigures.Count == 0)
                return;

            if (!force && Time.time < nextUpdateAt)
                return;

            nextUpdateAt = Time.time + updateInterval;

            // Exact positions are used only internally to choose a coarse authored
            // cue region. The public event/property never exposes any Figure's
            // transform and is intentionally jittered within the 3 m handoff radius.
            Vector3 aggregatePosition = Vector3.zero;
            int count = 0;
            int deterministicIdMix = 17;
            foreach (FigureMotor figure in trackedFigures
                         .Where(candidate => candidate != null)
                         .OrderBy(candidate => candidate.GetInstanceID()))
            {
                aggregatePosition += figure.transform.position;
                deterministicIdMix = unchecked(
                    deterministicIdMix * 31 + figure.GetInstanceID());
                count++;
            }

            if (count == 0)
                return;

            aggregatePosition /= count;
            Transform anchor = FindNearestTraceAnchor(aggregatePosition);
            Vector3 basePoint = anchor != null
                ? anchor.position
                : transform.position;

            int bucket = Mathf.FloorToInt(
                Time.time / Mathf.Max(0.05f, updateInterval));
            int seed = unchecked(
                deterministicIdMix * 486187739 +
                bucket * 16777619 +
                traceRevision * 31);

            var random = new System.Random(seed);
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float radius = Mathf.Sqrt((float)random.NextDouble()) * uncertaintyRadius;
            Vector3 jitter = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            approximateWorldPosition = basePoint + jitter;
            hasApproximateTrace = true;
            traceRevision++;
            ApproximateTraceUpdated?.Invoke(approximateWorldPosition);
        }

        private Transform FindNearestTraceAnchor(Vector3 aggregatePosition)
        {
            Transform best = null;
            float bestDistance = float.PositiveInfinity;

            if (traceHelpers == null)
                return null;

            foreach (Transform helper in traceHelpers)
            {
                if (helper == null)
                    continue;

                float distance = (helper.position - aggregatePosition).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                best = helper;
                bestDistance = distance;
            }

            return best;
        }

        private void RemoveDestroyedFigures()
        {
            trackedFigures.RemoveWhere(figure => figure == null);
        }

        public void ResetPalimpsestState()
        {
            trackedFigures.Clear();
            trackedFigureCount = 0;
            hasApproximateTrace = false;
            approximateWorldPosition = Vector3.zero;
            nextUpdateAt = 0f;
            traceRevision = 0;
        }
    }
}
