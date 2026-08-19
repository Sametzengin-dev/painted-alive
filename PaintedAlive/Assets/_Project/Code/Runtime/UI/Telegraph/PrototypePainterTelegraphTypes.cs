using System;
using UnityEngine;

namespace PaintedAlive.UI.Telegraph
{
    public enum PrototypePainterTelegraphPhase
    {
        None,
        Drafting,
        Armed,
        Blocked
    }

    public enum PrototypePainterTelegraphResolution
    {
        None,
        Committed,
        Cancelled
    }

    public readonly struct PrototypePainterTelegraphSignal
    {
        public PrototypePainterTelegraphSignal(
            int sourceId,
            Vector3[] worldPoints,
            string shapeName,
            PrototypePainterTelegraphPhase phase,
            float normalizedProgress,
            float remainingSeconds,
            bool canAfford,
            float estimatedPigmentCost,
            float startedAt,
            float publishedAt)
        {
            SourceId = sourceId;
            WorldPoints = worldPoints ?? Array.Empty<Vector3>();
            ShapeName = shapeName ?? string.Empty;
            Phase = phase;
            NormalizedProgress = Mathf.Clamp01(normalizedProgress);
            RemainingSeconds = Mathf.Max(0f, remainingSeconds);
            CanAfford = canAfford;
            EstimatedPigmentCost = Mathf.Max(0f, estimatedPigmentCost);
            StartedAt = startedAt;
            PublishedAt = publishedAt;
        }

        public int SourceId { get; }
        public Vector3[] WorldPoints { get; }
        public string ShapeName { get; }
        public PrototypePainterTelegraphPhase Phase { get; }
        public float NormalizedProgress { get; }
        public float RemainingSeconds { get; }
        public bool CanAfford { get; }
        public float EstimatedPigmentCost { get; }
        public float StartedAt { get; }
        public float PublishedAt { get; }

        public bool HasRenderableGeometry =>
            WorldPoints != null &&
            WorldPoints.Length >= 2;
    }

    public readonly struct PrototypePainterTelegraphOutcome
    {
        public PrototypePainterTelegraphOutcome(
            int sourceId,
            Vector3[] worldPoints,
            string shapeName,
            PrototypePainterTelegraphResolution resolution,
            string reason,
            float resolvedAt)
        {
            SourceId = sourceId;
            WorldPoints = worldPoints ?? Array.Empty<Vector3>();
            ShapeName = shapeName ?? string.Empty;
            Resolution = resolution;
            Reason = reason ?? string.Empty;
            ResolvedAt = resolvedAt;
        }

        public int SourceId { get; }
        public Vector3[] WorldPoints { get; }
        public string ShapeName { get; }
        public PrototypePainterTelegraphResolution Resolution { get; }
        public string Reason { get; }
        public float ResolvedAt { get; }
    }
}
