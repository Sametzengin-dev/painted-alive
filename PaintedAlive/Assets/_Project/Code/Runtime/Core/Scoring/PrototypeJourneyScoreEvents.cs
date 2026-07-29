using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;

namespace PaintedAlive.Core.Scoring
{
    public enum PrototypeJourneyScoreEventType
    {
        None = 0,
        DistanceProgress = 1,
        StainSupportArrival = 2,
        NormalFigureExit = 3
    }

    public readonly struct PrototypeJourneyScoreSnapshot
    {
        public PrototypeJourneyScoreSnapshot(
            FigureClarityState figure,
            float progress01,
            int distanceScore,
            int exitBonus,
            bool stainArrivalRecorded,
            bool normalExitCompleted,
            FigureFrameExitOutcome lastExitOutcome)
        {
            Figure = figure;
            Progress01 = progress01;
            DistanceScore = distanceScore;
            ExitBonus = exitBonus;
            StainArrivalRecorded = stainArrivalRecorded;
            NormalExitCompleted = normalExitCompleted;
            LastExitOutcome = lastExitOutcome;
        }

        public FigureClarityState Figure { get; }
        public float Progress01 { get; }
        public int DistanceScore { get; }
        public int ExitBonus { get; }
        public int TotalScore => DistanceScore + ExitBonus;
        public bool StainArrivalRecorded { get; }
        public bool NormalExitCompleted { get; }
        public FigureFrameExitOutcome LastExitOutcome { get; }
    }

    public readonly struct PrototypeJourneyScoreEvent
    {
        public PrototypeJourneyScoreEvent(
            PrototypeJourneyScoreEventType eventType,
            PrototypeJourneyScoreSnapshot snapshot,
            float raisedAt)
        {
            EventType = eventType;
            Snapshot = snapshot;
            RaisedAt = raisedAt;
        }

        public PrototypeJourneyScoreEventType EventType { get; }
        public PrototypeJourneyScoreSnapshot Snapshot { get; }
        public float RaisedAt { get; }
    }

    public static class PrototypeJourneyScoreEventHub
    {
        public static event Action<PrototypeJourneyScoreEvent> EventRaised;

        public static void Publish(PrototypeJourneyScoreEvent scoreEvent)
        {
            EventRaised?.Invoke(scoreEvent);
        }
    }
}
