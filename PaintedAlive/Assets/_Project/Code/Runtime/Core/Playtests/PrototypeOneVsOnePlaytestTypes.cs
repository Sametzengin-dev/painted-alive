using System;
using System.Collections.Generic;
using PaintedAlive.Core.Encounters;
using PaintedAlive.Core.Prototypes;

namespace PaintedAlive.Core.Playtests
{
    public enum PrototypeOneVsOneOutcomeKind
    {
        EarlyPassOrAvoidance = 0,
        PaletteKnifeCut = 1,
        FixativeBreakOrRampUse = 2
    }

    public enum PrototypeOneVsOneOutcomeStatus
    {
        Pending = 0,
        Passed = 1,
        Failed = 2
    }

    [Serializable]
    public sealed class PrototypeOneVsOneOutcomeRecord
    {
        public PrototypeOneVsOneOutcomeKind outcome;
        public PrototypeOneVsOneOutcomeStatus status;
        public string evidence;
        public float matchTime;
        public int encounterIndex;
    }

    [Serializable]
    public sealed class PrototypeOneVsOneRunReport
    {
        public string schemaVersion = "m40-one-vs-one-1.0.0";
        public string runId;
        public int runNumber;
        public string utcStartedAt;
        public string utcFinishedAt;
        public string finalMatchState;
        public bool accepted;
        public int requiredOutcomeCount;
        public int passedOutcomeCount;
        public float configuredDuration;
        public float actualRunningDuration;
        public float remainingTime;
        public int finalJourneyScore;
        public bool normalFigureExit;
        public bool stainArrivalDuringRun;
        public bool legacyTelemetryPresent;
        public int strokeCount;
        public int cutCount;
        public int rampCount;
        public int encounterTransitionCount;
        public List<string> visitedPhases = new();
        public List<PrototypeOneVsOneOutcomeRecord> outcomes = new();
    }

    public readonly struct PrototypeOneVsOnePlaytestSnapshot
    {
        public PrototypeOneVsOnePlaytestSnapshot(
            PrototypeMatchState matchState,
            int runNumber,
            int currentOutcomeIndex,
            int passedOutcomeCount,
            int requiredOutcomeCount,
            bool accepted,
            float runningElapsed,
            string statusMessage,
            string reportPath)
        {
            MatchState = matchState;
            RunNumber = runNumber;
            CurrentOutcomeIndex = currentOutcomeIndex;
            PassedOutcomeCount = passedOutcomeCount;
            RequiredOutcomeCount = requiredOutcomeCount;
            Accepted = accepted;
            RunningElapsed = runningElapsed;
            StatusMessage = statusMessage;
            ReportPath = reportPath;
        }

        public PrototypeMatchState MatchState { get; }
        public int RunNumber { get; }
        public int CurrentOutcomeIndex { get; }
        public int PassedOutcomeCount { get; }
        public int RequiredOutcomeCount { get; }
        public bool Accepted { get; }
        public float RunningElapsed { get; }
        public string StatusMessage { get; }
        public string ReportPath { get; }
    }
}
