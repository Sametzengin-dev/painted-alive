using System;
using System.Collections.Generic;
using PaintedAlive.Core.Prototypes;

namespace PaintedAlive.Core.Playtests.Validation
{
    public enum PrototypeAcceptanceQuestion
    {
        AttackReadBeforeImpact = 0,
        CounterplayUnderstood = 1,
        FailureCauseUnderstood = 2,
        ControlsFeltReliable = 3,
        WouldPlayAnotherRun = 4
    }

    [Serializable]
    public sealed class PrototypeAcceptanceAnswer
    {
        public PrototypeAcceptanceQuestion question;
        public bool answered;
        public bool value;
    }

    [Serializable]
    public sealed class PrototypeAcceptanceRunReport
    {
        public string schemaVersion = "m41-prototype-acceptance-1.0.0";
        public string reviewId;
        public string sourceM40RunId;
        public int sourceM40RunNumber;
        public string utcStartedAt;
        public string utcFinishedAt;
        public string finalMatchState;
        public bool reviewCompleted;
        public string incompleteReason;

        public bool m40ReportFound;
        public string m40ReportPath;
        public bool legacyTelemetryFound;
        public string legacyTelemetryPath;

        public bool m40Accepted;
        public int distinctOutcomeCount;
        public int requiredOutcomeCount;
        public bool normalFigureExit;
        public bool stainArrivalDuringRun;
        public int finalJourneyScore;
        public float actualRunningDuration;
        public float remainingTime;

        public int strokeCount;
        public float totalStrokeLength;
        public float pigmentSpent;
        public int totalCutCount;
        public int wetCutCount;
        public int dryingCutCount;
        public int dryCutCount;
        public int roleSwitchCount;
        public float figureRoleTime;
        public float painterRoleTime;
        public float furthestProgressNormalized;
        public float blockedInputTime;
        public float longestBlockedInputSequence;
        public float blockedInputRatio;

        public List<PrototypeAcceptanceAnswer> answers = new();
        public float readabilityRatio;
        public bool replayDesired;
        public bool automatedEvidencePassed;
        public bool controlReliabilityPassed;
        public bool readabilityPassed;
        public bool runPassed;
    }

    [Serializable]
    public sealed class PrototypeAcceptanceRunSummary
    {
        public string reviewId;
        public string utcFinishedAt;
        public bool reviewCompleted;
        public bool runPassed;
        public bool m40Accepted;
        public float readabilityRatio;
        public bool replayDesired;
        public float blockedInputRatio;
        public float longestBlockedInputSequence;
        public string reportPath;
    }

    [Serializable]
    public sealed class PrototypeAcceptanceAggregateReport
    {
        public string schemaVersion = "m41-prototype-aggregate-1.0.0";
        public string utcUpdatedAt;
        public int availableCompletedRuns;
        public int evaluationWindow;
        public int evaluatedRuns;
        public int passingRuns;
        public int requiredPassingRuns;
        public int replayYesRuns;
        public float replayYesRatio;
        public float averageReadabilityRatio;
        public float averageBlockedInputRatio;
        public bool enoughRuns;
        public bool repeatedRunGatePassed;
        public bool replayGatePassed;
        public bool readabilityGatePassed;
        public bool networkSpikeCandidateReady;
        public List<PrototypeAcceptanceRunSummary> evaluatedRunSummaries = new();
    }

    public readonly struct PrototypeAcceptanceSnapshot
    {
        public PrototypeAcceptanceSnapshot(
            PrototypeMatchState matchState,
            bool collectingReports,
            bool reviewActive,
            bool reviewCompleted,
            int questionIndex,
            int questionCount,
            string questionText,
            string statusMessage,
            bool currentRunPassed,
            string runReportPath,
            bool networkSpikeCandidateReady,
            int aggregatePassingRuns,
            int aggregateEvaluatedRuns,
            int aggregateRequiredRuns)
        {
            MatchState = matchState;
            CollectingReports = collectingReports;
            ReviewActive = reviewActive;
            ReviewCompleted = reviewCompleted;
            QuestionIndex = questionIndex;
            QuestionCount = questionCount;
            QuestionText = questionText;
            StatusMessage = statusMessage;
            CurrentRunPassed = currentRunPassed;
            RunReportPath = runReportPath;
            NetworkSpikeCandidateReady = networkSpikeCandidateReady;
            AggregatePassingRuns = aggregatePassingRuns;
            AggregateEvaluatedRuns = aggregateEvaluatedRuns;
            AggregateRequiredRuns = aggregateRequiredRuns;
        }

        public PrototypeMatchState MatchState { get; }
        public bool CollectingReports { get; }
        public bool ReviewActive { get; }
        public bool ReviewCompleted { get; }
        public int QuestionIndex { get; }
        public int QuestionCount { get; }
        public string QuestionText { get; }
        public string StatusMessage { get; }
        public bool CurrentRunPassed { get; }
        public string RunReportPath { get; }
        public bool NetworkSpikeCandidateReady { get; }
        public int AggregatePassingRuns { get; }
        public int AggregateEvaluatedRuns { get; }
        public int AggregateRequiredRuns { get; }
    }
}
