using PaintedAlive.Core.Scoring;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Core.MatchFlow
{
    [DisallowMultipleComponent]
    public sealed class PrototypeExpeditionMatchFlow : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PrototypeJourneyScoreTracker scoreTracker;
        [SerializeField] private FigureClarityState figure;
        [SerializeField] private PrototypeMatchInputLock inputLock;
        [SerializeField] private PrototypeExpeditionMatchConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField] private PrototypeExpeditionMatchPhase phase;
        [SerializeField] private PrototypeExpeditionCompletionReason completionReason;
        [SerializeField] private float phaseTimeRemaining;
        [SerializeField] private float elapsedActiveTime;
        [SerializeField] private float remainingActiveTime;
        [SerializeField] private bool stainArrivalDuringRun;
        [SerializeField] private bool worldPausedByMatch;
        [SerializeField] private int phaseTransitionCount;

        private float preparationEndsAtUnscaled;
        private float activeStartedAtUnscaled;
        private float activeEndsAtUnscaled;
        private float timeScaleBeforePause = 1f;
        private PrototypeJourneyScoreSnapshot finalScoreSnapshot;
        private FigureClarityLevel finalClarity;

        public PrototypeExpeditionMatchPhase Phase => phase;
        public PrototypeExpeditionCompletionReason CompletionReason => completionReason;
        public float PhaseTimeRemaining => phaseTimeRemaining;
        public float ElapsedActiveTime => elapsedActiveTime;
        public float RemainingActiveTime => remainingActiveTime;
        public bool StainArrivalDuringRun => stainArrivalDuringRun;
        public bool WorldPausedByMatch => worldPausedByMatch;
        public int PhaseTransitionCount => phaseTransitionCount;
        public PrototypeJourneyScoreTracker ScoreTracker => scoreTracker;
        public FigureClarityState Figure => figure;
        public PrototypeExpeditionMatchConfig Config => config;

        public PrototypeExpeditionMatchSnapshot CurrentSnapshot =>
            BuildSnapshot(useFinalScore: phase == PrototypeExpeditionMatchPhase.Completed);

        public void Configure(
            PrototypeJourneyScoreTracker tracker,
            FigureClarityState figureState,
            PrototypeMatchInputLock matchInputLock,
            PrototypeExpeditionMatchConfig matchConfig)
        {
            scoreTracker = tracker;
            figure = figureState;
            inputLock = matchInputLock;
            config = matchConfig;
        }

        private void Awake()
        {
            phase = PrototypeExpeditionMatchPhase.Waiting;
            completionReason = PrototypeExpeditionCompletionReason.None;

            if (figure == null && scoreTracker != null)
            {
                figure = scoreTracker.Figure;
            }
        }

        private void OnEnable()
        {
            PrototypeJourneyScoreEventHub.EventRaised += HandleScoreEvent;
        }

        private void OnDisable()
        {
            PrototypeJourneyScoreEventHub.EventRaised -= HandleScoreEvent;
            RestoreWorldAndInputs();
        }

        private void Start()
        {
            ValidateDependencies();

            if (config == null || config.AutoStartOnPlay)
            {
                BeginPreparation();
            }
        }

        private void Update()
        {
            float now = Time.unscaledTime;

            switch (phase)
            {
                case PrototypeExpeditionMatchPhase.Preparation:
                    phaseTimeRemaining = Mathf.Max(
                        0f,
                        preparationEndsAtUnscaled - now);

                    if (phaseTimeRemaining <= 0f)
                    {
                        BeginActiveRun();
                    }

                    break;

                case PrototypeExpeditionMatchPhase.Active:
                    elapsedActiveTime = Mathf.Max(
                        0f,
                        now - activeStartedAtUnscaled);
                    remainingActiveTime = Mathf.Max(
                        0f,
                        activeEndsAtUnscaled - now);
                    phaseTimeRemaining = remainingActiveTime;

                    if (remainingActiveTime <= 0f)
                    {
                        CompleteRun(
                            PrototypeExpeditionCompletionReason.TimeExpired,
                            scoreTracker != null
                                ? scoreTracker.CurrentSnapshot
                                : default);
                    }

                    break;
            }
        }

        [ContextMenu("Begin Preparation")]
        public void BeginPreparation()
        {
            if (phase == PrototypeExpeditionMatchPhase.Active ||
                phase == PrototypeExpeditionMatchPhase.Completed)
            {
                return;
            }

            completionReason = PrototypeExpeditionCompletionReason.None;
            stainArrivalDuringRun = false;
            elapsedActiveTime = 0f;
            remainingActiveTime = config != null ? config.ActiveDuration : 300f;
            phaseTimeRemaining = config != null ? config.PreparationDuration : 4f;
            preparationEndsAtUnscaled = Time.unscaledTime + phaseTimeRemaining;

            if (inputLock != null)
            {
                inputLock.LockInputs();
            }

            if (config == null || config.PauseWorldDuringPreparation)
            {
                PauseWorld();
            }

            SetPhase(PrototypeExpeditionMatchPhase.Preparation);

            if (phaseTimeRemaining <= 0f)
            {
                BeginActiveRun();
            }
        }

        private void BeginActiveRun()
        {
            RestoreWorldAndInputs();

            activeStartedAtUnscaled = Time.unscaledTime;
            float duration = config != null ? config.ActiveDuration : 300f;
            activeEndsAtUnscaled = activeStartedAtUnscaled + duration;
            elapsedActiveTime = 0f;
            remainingActiveTime = duration;
            phaseTimeRemaining = duration;

            SetPhase(PrototypeExpeditionMatchPhase.Active);

            Debug.Log(
                $"[M38] EXPEDITION ACTIVE | Duration={duration:0.0}s | " +
                $"Figure={(figure != null ? figure.name : "MISSING")}",
                this);
        }

        private void HandleScoreEvent(PrototypeJourneyScoreEvent scoreEvent)
        {
            if (phase != PrototypeExpeditionMatchPhase.Active ||
                figure == null ||
                scoreEvent.Snapshot.Figure != figure)
            {
                return;
            }

            if (scoreEvent.EventType ==
                PrototypeJourneyScoreEventType.StainSupportArrival)
            {
                stainArrivalDuringRun = true;
                PublishCurrentState();
                return;
            }

            if (scoreEvent.EventType ==
                PrototypeJourneyScoreEventType.NormalFigureExit)
            {
                CompleteRun(
                    PrototypeExpeditionCompletionReason.NormalFigureExit,
                    scoreEvent.Snapshot);
            }
        }

        private void CompleteRun(
            PrototypeExpeditionCompletionReason reason,
            PrototypeJourneyScoreSnapshot scoreSnapshot)
        {
            if (phase != PrototypeExpeditionMatchPhase.Active)
            {
                return;
            }

            float now = Time.unscaledTime;
            elapsedActiveTime = Mathf.Max(0f, now - activeStartedAtUnscaled);
            remainingActiveTime = Mathf.Max(0f, activeEndsAtUnscaled - now);
            phaseTimeRemaining = 0f;
            completionReason = reason;
            finalScoreSnapshot = scoreSnapshot;
            finalClarity = figure != null
                ? figure.CurrentLevel
                : default;
            stainArrivalDuringRun |= scoreSnapshot.StainArrivalRecorded;

            if (inputLock != null)
            {
                inputLock.LockInputs();
            }

            if (config == null || config.PauseWorldWhenCompleted)
            {
                PauseWorld();
            }

            SetPhase(PrototypeExpeditionMatchPhase.Completed);

            Debug.Log(
                $"[M38] EXPEDITION COMPLETED | Reason={completionReason} | " +
                $"Distance={finalScoreSnapshot.DistanceScore} | " +
                $"ExitBonus={finalScoreSnapshot.ExitBonus} | " +
                $"Total={finalScoreSnapshot.TotalScore} | " +
                $"Elapsed={elapsedActiveTime:0.00}s | " +
                $"Remaining={remainingActiveTime:0.00}s | " +
                $"FinalClarity={finalClarity} | " +
                $"StainArrival={stainArrivalDuringRun}",
                this);
        }

        private void SetPhase(PrototypeExpeditionMatchPhase newPhase)
        {
            phase = newPhase;
            phaseTransitionCount++;
            PublishCurrentState();
        }

        private void PublishCurrentState()
        {
            PrototypeExpeditionMatchEventHub.Publish(CurrentSnapshot);
        }

        private PrototypeExpeditionMatchSnapshot BuildSnapshot(bool useFinalScore)
        {
            PrototypeJourneyScoreSnapshot score = useFinalScore
                ? finalScoreSnapshot
                : scoreTracker != null
                    ? scoreTracker.CurrentSnapshot
                    : default;

            FigureClarityLevel clarity = useFinalScore
                ? finalClarity
                : figure != null
                    ? figure.CurrentLevel
                    : default;

            return new PrototypeExpeditionMatchSnapshot(
                phase,
                completionReason,
                phaseTimeRemaining,
                elapsedActiveTime,
                remainingActiveTime,
                score.DistanceScore,
                score.ExitBonus,
                score.TotalScore,
                stainArrivalDuringRun || score.StainArrivalRecorded,
                score.NormalExitCompleted,
                clarity,
                Time.unscaledTime);
        }

        private void PauseWorld()
        {
            if (worldPausedByMatch)
            {
                return;
            }

            timeScaleBeforePause = Time.timeScale > 0f
                ? Time.timeScale
                : 1f;
            Time.timeScale = 0f;
            worldPausedByMatch = true;
        }

        private void RestoreWorldAndInputs()
        {
            if (worldPausedByMatch)
            {
                Time.timeScale = timeScaleBeforePause > 0f
                    ? timeScaleBeforePause
                    : 1f;
                worldPausedByMatch = false;
            }

            if (inputLock != null)
            {
                inputLock.UnlockInputs();
            }
        }

        private void ValidateDependencies()
        {
            if (scoreTracker == null)
            {
                Debug.LogError(
                    "[M38] PrototypeJourneyScoreTracker bağlantısı eksik.",
                    this);
            }

            if (figure == null)
            {
                Debug.LogError(
                    "[M38] FigureClarityState bağlantısı eksik.",
                    this);
            }

            if (inputLock == null)
            {
                Debug.LogError(
                    "[M38] PrototypeMatchInputLock bağlantısı eksik.",
                    this);
            }

            if (config == null)
            {
                Debug.LogError(
                    "[M38] PrototypeExpeditionMatchConfig bağlantısı eksik.",
                    this);
            }
        }
    }
}
