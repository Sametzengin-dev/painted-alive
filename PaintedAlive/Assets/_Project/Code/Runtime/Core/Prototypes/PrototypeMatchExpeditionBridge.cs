using System;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using UnityEngine;

namespace PaintedAlive.Core.Prototypes
{
    public enum PrototypeExpeditionResultReason
    {
        None = 0,
        NormalFigureExit = 1,
        TimeExpired = 2
    }

    public readonly struct PrototypeExpeditionResultSnapshot
    {
        public PrototypeExpeditionResultSnapshot(
            PrototypeMatchState matchState,
            PrototypeExpeditionResultReason reason,
            PrototypeJourneyScoreSnapshot score,
            float elapsedTime,
            float remainingTime,
            FigureClarityLevel finalClarity,
            bool stainArrivalDuringRun,
            int runNumber)
        {
            MatchState = matchState;
            Reason = reason;
            Score = score;
            ElapsedTime = elapsedTime;
            RemainingTime = remainingTime;
            FinalClarity = finalClarity;
            StainArrivalDuringRun = stainArrivalDuringRun;
            RunNumber = runNumber;
        }

        public PrototypeMatchState MatchState { get; }
        public PrototypeExpeditionResultReason Reason { get; }
        public PrototypeJourneyScoreSnapshot Score { get; }
        public float ElapsedTime { get; }
        public float RemainingTime { get; }
        public FigureClarityLevel FinalClarity { get; }
        public bool StainArrivalDuringRun { get; }
        public int RunNumber { get; }
        public bool IsCompleted =>
            MatchState == PrototypeMatchState.FigureEscaped ||
            MatchState == PrototypeMatchState.TimeExpired;
    }

    /// <summary>
    /// Extends the original PrototypeMatchController instead of creating a
    /// second countdown/timer/input authority. M36 evaluates the exit, M37
    /// calculates the score, and this bridge forwards only a valid normal
    /// Figure exit to the original match flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeMatchExpeditionBridge : MonoBehaviour
    {
        [Header("Authoritative Existing Match Flow")]
        [SerializeField] private PrototypeMatchController matchController;

        [Header("Current Milestone Systems")]
        [SerializeField] private PrototypeJourneyScoreTracker scoreTracker;
        [SerializeField] private FigureClarityState figure;
        [SerializeField] private PrototypeFrameExitGate frameExitGate;

        [Header("Runtime - Read Only")]
        [SerializeField] private bool stainArrivalDuringRun;
        [SerializeField] private bool normalExitForwarded;
        [SerializeField] private float runningStartedAtUnscaled;
        [SerializeField] private float lastElapsedTime;
        [SerializeField] private int runNumber;
        [SerializeField] private int normalExitForwardCount;
        [SerializeField] private int resetCount;

        private PrototypeExpeditionResultSnapshot currentSnapshot;
        private bool observedMatchStateEvent;

        public event Action<PrototypeExpeditionResultSnapshot> SnapshotChanged;

        public PrototypeMatchController MatchController => matchController;
        public PrototypeJourneyScoreTracker ScoreTracker => scoreTracker;
        public FigureClarityState Figure => figure;
        public PrototypeFrameExitGate FrameExitGate => frameExitGate;
        public bool StainArrivalDuringRun => stainArrivalDuringRun;
        public bool NormalExitForwarded => normalExitForwarded;
        public int RunNumber => runNumber;
        public int NormalExitForwardCount => normalExitForwardCount;
        public int ResetCount => resetCount;
        public PrototypeExpeditionResultSnapshot CurrentSnapshot => currentSnapshot;

        public void Configure(
            PrototypeMatchController authoritativeMatchController,
            PrototypeJourneyScoreTracker journeyScoreTracker,
            FigureClarityState figureState,
            PrototypeFrameExitGate exitGate)
        {
            matchController = authoritativeMatchController;
            scoreTracker = journeyScoreTracker;
            figure = figureState;
            frameExitGate = exitGate;
        }

        private void Awake()
        {
            ResolveDependencies();
            PublishSnapshot(PrototypeExpeditionResultReason.None);
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (matchController != null)
            {
                matchController.StateChanged += HandleMatchStateChanged;
            }

            PrototypeJourneyScoreEventHub.EventRaised += HandleJourneyScoreEvent;
        }

        private void OnDisable()
        {
            if (matchController != null)
            {
                matchController.StateChanged -= HandleMatchStateChanged;
            }

            PrototypeJourneyScoreEventHub.EventRaised -= HandleJourneyScoreEvent;
        }

        private void Start()
        {
            ValidateDependencies();

            if (matchController != null && !observedMatchStateEvent)
            {
                HandleMatchStateChanged(matchController.State);
            }
        }

        private void ResolveDependencies()
        {
            if (matchController == null)
            {
                matchController = GetComponent<PrototypeMatchController>();
            }

            if (scoreTracker == null)
            {
                scoreTracker = FindFirstObjectByType<PrototypeJourneyScoreTracker>();
            }

            if (figure == null && scoreTracker != null)
            {
                figure = scoreTracker.Figure;
            }

            if (frameExitGate == null)
            {
                frameExitGate = FindFirstObjectByType<PrototypeFrameExitGate>();
            }
        }

        private void HandleJourneyScoreEvent(PrototypeJourneyScoreEvent scoreEvent)
        {
            if (scoreTracker == null ||
                figure == null ||
                scoreEvent.Snapshot.Figure != figure)
            {
                return;
            }

            if (matchController == null ||
                matchController.State != PrototypeMatchState.Running)
            {
                return;
            }

            switch (scoreEvent.EventType)
            {
                case PrototypeJourneyScoreEventType.StainSupportArrival:
                    stainArrivalDuringRun = true;
                    PublishSnapshot(PrototypeExpeditionResultReason.None);
                    break;

                case PrototypeJourneyScoreEventType.NormalFigureExit:
                    if (normalExitForwarded)
                    {
                        return;
                    }

                    normalExitForwarded = true;
                    normalExitForwardCount++;

                    // The original match controller remains the sole owner of
                    // match completion and interaction locking.
                    matchController.NotifyFigureReachedExit();
                    break;
            }
        }

        private void HandleMatchStateChanged(PrototypeMatchState state)
        {
            observedMatchStateEvent = true;

            switch (state)
            {
                case PrototypeMatchState.Waiting:
                    PublishSnapshot(PrototypeExpeditionResultReason.None);
                    break;

                case PrototypeMatchState.Countdown:
                    ResetCurrentRun();
                    break;

                case PrototypeMatchState.Running:
                    runningStartedAtUnscaled = Time.unscaledTime;
                    lastElapsedTime = 0f;
                    PublishSnapshot(PrototypeExpeditionResultReason.None);
                    break;

                case PrototypeMatchState.FigureEscaped:
                    lastElapsedTime = CalculateElapsedTime();
                    PublishSnapshot(PrototypeExpeditionResultReason.NormalFigureExit);
                    break;

                case PrototypeMatchState.TimeExpired:
                    lastElapsedTime = CalculateElapsedTime();
                    PublishSnapshot(PrototypeExpeditionResultReason.TimeExpired);
                    break;
            }
        }

        private void ResetCurrentRun()
        {
            runNumber++;
            resetCount++;
            stainArrivalDuringRun = false;
            normalExitForwarded = false;
            runningStartedAtUnscaled = 0f;
            lastElapsedTime = 0f;

            scoreTracker?.ResetForNewMatch();
            frameExitGate?.ResetForNewMatch();

            PublishSnapshot(PrototypeExpeditionResultReason.None);

            Debug.Log(
                $"[M38.1] Existing PrototypeMatchController run reset. " +
                $"Run={runNumber}, ScoreReset={(scoreTracker != null)}, " +
                $"GateReset={(frameExitGate != null)}.",
                this);
        }

        private float CalculateElapsedTime()
        {
            if (runningStartedAtUnscaled <= 0f)
            {
                return matchController != null
                    ? Mathf.Max(0f, matchController.CompletionTime)
                    : 0f;
            }

            return Mathf.Max(0f, Time.unscaledTime - runningStartedAtUnscaled);
        }

        private void PublishSnapshot(PrototypeExpeditionResultReason reason)
        {
            PrototypeMatchState state = matchController != null
                ? matchController.State
                : PrototypeMatchState.Waiting;

            PrototypeJourneyScoreSnapshot score = scoreTracker != null
                ? scoreTracker.CurrentSnapshot
                : default;

            float elapsed = state == PrototypeMatchState.Running
                ? CalculateElapsedTime()
                : lastElapsedTime;

            float remaining = matchController != null
                ? matchController.TimeRemaining
                : 0f;

            FigureClarityLevel clarity = figure != null
                ? figure.CurrentLevel
                : default;

            currentSnapshot = new PrototypeExpeditionResultSnapshot(
                state,
                reason,
                score,
                elapsed,
                remaining,
                clarity,
                stainArrivalDuringRun,
                runNumber);

            SnapshotChanged?.Invoke(currentSnapshot);
        }

        private void ValidateDependencies()
        {
            if (matchController == null)
            {
                Debug.LogError(
                    "[M38.1] Existing PrototypeMatchController bulunamadı. " +
                    "İkinci bir match flow oluşturulmayacak.",
                    this);
            }

            if (scoreTracker == null)
            {
                Debug.LogError("[M38.1] M37 score tracker eksik.", this);
            }

            if (figure == null)
            {
                Debug.LogError("[M38.1] FigureClarityState eksik.", this);
            }

            if (frameExitGate == null)
            {
                Debug.LogError("[M38.1] M36 frame exit gate eksik.", this);
            }
        }
    }
}
