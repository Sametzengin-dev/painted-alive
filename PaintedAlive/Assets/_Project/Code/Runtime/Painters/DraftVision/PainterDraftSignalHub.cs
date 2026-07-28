using System;
using UnityEngine;

namespace PaintedAlive.Painters.DraftVision
{
    public readonly struct PainterDraftSignal
    {
        public PainterDraftSignal(
            int sourceId,
            Vector3[] worldPoints,
            float startedAt,
            float normalRevealAt)
        {
            SourceId = sourceId;
            WorldPoints = worldPoints ?? Array.Empty<Vector3>();
            StartedAt = startedAt;
            NormalRevealAt = normalRevealAt;
        }

        public int SourceId { get; }
        public Vector3[] WorldPoints { get; }
        public float StartedAt { get; }
        public float NormalRevealAt { get; }
        public bool HasRenderableGeometry => WorldPoints != null && WorldPoints.Length >= 2;
    }

    public static class PainterDraftSignalHub
    {
        public static event Action<PainterDraftSignal> DraftUpdated;
        public static event Action<int> DraftEnded;

        public static void Publish(PainterDraftSignal signal)
        {
            if (!signal.HasRenderableGeometry)
            {
                return;
            }

            DraftUpdated?.Invoke(signal);
        }

        public static void End(int sourceId)
        {
            DraftEnded?.Invoke(sourceId);
        }
    }
}
