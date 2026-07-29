using System;
using PaintedAlive.Figures;

namespace PaintedAlive.Core.MatchFlow
{
    public enum PrototypeExpeditionMatchPhase
    {
        Waiting = 0,
        Preparation = 1,
        Active = 2,
        Completed = 3
    }

    public enum PrototypeExpeditionCompletionReason
    {
        None = 0,
        NormalFigureExit = 1,
        TimeExpired = 2
    }

    public readonly struct PrototypeExpeditionMatchSnapshot
    {
        public PrototypeExpeditionMatchSnapshot(
            PrototypeExpeditionMatchPhase phase,
            PrototypeExpeditionCompletionReason completionReason,
            float phaseTimeRemaining,
            float elapsedActiveTime,
            float remainingActiveTime,
            int distanceScore,
            int exitBonus,
            int totalScore,
            bool stainArrivalRecorded,
            bool normalExitCompleted,
            FigureClarityLevel finalClarity,
            float raisedAt)
        {
            Phase = phase;
            CompletionReason = completionReason;
            PhaseTimeRemaining = phaseTimeRemaining;
            ElapsedActiveTime = elapsedActiveTime;
            RemainingActiveTime = remainingActiveTime;
            DistanceScore = distanceScore;
            ExitBonus = exitBonus;
            TotalScore = totalScore;
            StainArrivalRecorded = stainArrivalRecorded;
            NormalExitCompleted = normalExitCompleted;
            FinalClarity = finalClarity;
            RaisedAt = raisedAt;
        }

        public PrototypeExpeditionMatchPhase Phase { get; }
        public PrototypeExpeditionCompletionReason CompletionReason { get; }
        public float PhaseTimeRemaining { get; }
        public float ElapsedActiveTime { get; }
        public float RemainingActiveTime { get; }
        public int DistanceScore { get; }
        public int ExitBonus { get; }
        public int TotalScore { get; }
        public bool StainArrivalRecorded { get; }
        public bool NormalExitCompleted { get; }
        public FigureClarityLevel FinalClarity { get; }
        public float RaisedAt { get; }
    }

    public static class PrototypeExpeditionMatchEventHub
    {
        public static event Action<PrototypeExpeditionMatchSnapshot> StateChanged;

        public static void Publish(PrototypeExpeditionMatchSnapshot snapshot)
        {
            StateChanged?.Invoke(snapshot);
        }
    }
}
