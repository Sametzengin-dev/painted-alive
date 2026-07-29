using System;
using PaintedAlive.Core.Prototypes;
using UnityEngine;

namespace PaintedAlive.Core.Encounters
{
    [DisallowMultipleComponent]
    public sealed class PrototypeEncounterRhythmDirector : MonoBehaviour
    {
        [Header("Existing Authoritative Systems")]
        [SerializeField] private PrototypeMatchController matchController;
        [SerializeField] private FigureProgressTracker progressTracker;
        [SerializeField] private PrototypeEncounterRhythmConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField] private PrototypeEncounterPhase currentPhase;
        [SerializeField] private int currentEncounterIndex;
        [SerializeField, Range(0f, 1f)] private float currentRouteProgress01;
        [SerializeField, Range(0f, 1f)] private float currentLocalPhaseProgress01;
        [SerializeField, Range(0f, 1f)] private float currentPressure01;
        [SerializeField] private int runNumber;
        [SerializeField] private int transitionCount;
        [SerializeField] private float lastTransitionTimeUnscaled;

        private PrototypeEncounterRhythmSnapshot currentSnapshot;

        public event Action<PrototypeEncounterRhythmSnapshot> SnapshotChanged;

        public PrototypeMatchController MatchController => matchController;
        public FigureProgressTracker ProgressTracker => progressTracker;
        public PrototypeEncounterRhythmConfig Config => config;
        public PrototypeEncounterPhase CurrentPhase => currentPhase;
        public int CurrentEncounterIndex => currentEncounterIndex;
        public float CurrentRouteProgress01 => currentRouteProgress01;
        public float CurrentLocalPhaseProgress01 => currentLocalPhaseProgress01;
        public float CurrentPressure01 => currentPressure01;
        public int RunNumber => runNumber;
        public int TransitionCount => transitionCount;
        public float LastTransitionTimeUnscaled => lastTransitionTimeUnscaled;
        public PrototypeEncounterRhythmSnapshot CurrentSnapshot => currentSnapshot;

        public void Configure(
            PrototypeMatchController authoritativeMatchController,
            FigureProgressTracker authoritativeProgressTracker,
            PrototypeEncounterRhythmConfig rhythmConfig)
        {
            matchController = authoritativeMatchController;
            progressTracker = authoritativeProgressTracker;
            config = rhythmConfig;
        }

        private void Awake()
        {
            ResolveDependencies();
            SetInactive(publishTransition: false);
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (matchController != null)
            {
                matchController.StateChanged += HandleMatchStateChanged;
            }

            if (progressTracker != null)
            {
                progressTracker.ProgressChanged += HandleProgressChanged;
            }
        }

        private void OnDisable()
        {
            if (matchController != null)
            {
                matchController.StateChanged -= HandleMatchStateChanged;
            }

            if (progressTracker != null)
            {
                progressTracker.ProgressChanged -= HandleProgressChanged;
            }
        }

        private void Start()
        {
            ValidateDependencies();

            if (matchController != null)
            {
                HandleMatchStateChanged(matchController.State);
            }
        }

        public void ResetForNewMatch()
        {
            runNumber++;
            transitionCount = 0;
            currentRouteProgress01 = 0f;
            SetInactive(publishTransition: false);
        }

        private void ResolveDependencies()
        {
            if (matchController == null)
            {
                matchController = GetComponent<PrototypeMatchController>();
            }

            if (progressTracker == null)
            {
                progressTracker = FindFirstObjectByType<FigureProgressTracker>();
            }
        }

        private void HandleMatchStateChanged(PrototypeMatchState state)
        {
            switch (state)
            {
                case PrototypeMatchState.Waiting:
                    SetInactive(publishTransition: false);
                    break;

                case PrototypeMatchState.Countdown:
                    ResetForNewMatch();
                    break;

                case PrototypeMatchState.Running:
                    EvaluateProgress(
                        progressTracker != null
                            ? progressTracker.NormalizedProgress
                            : 0f,
                        forceTransition: currentPhase == PrototypeEncounterPhase.Inactive);
                    break;

                case PrototypeMatchState.FigureEscaped:
                case PrototypeMatchState.TimeExpired:
                    TransitionToCompleted();
                    break;
            }
        }

        private void HandleProgressChanged(
            float normalizedProgress,
            float distance,
            float remaining)
        {
            if (matchController == null ||
                matchController.State != PrototypeMatchState.Running)
            {
                return;
            }

            EvaluateProgress(normalizedProgress, forceTransition: false);
        }

        private void EvaluateProgress(float normalizedProgress, bool forceTransition)
        {
            if (config == null)
            {
                return;
            }

            currentRouteProgress01 = Mathf.Max(
                currentRouteProgress01,
                Mathf.Clamp01(normalizedProgress));

            config.Evaluate(
                currentRouteProgress01,
                out int encounterIndex,
                out PrototypeEncounterPhase phase,
                out float localPhaseProgress,
                out float pressure);

            bool changed =
                forceTransition ||
                phase != currentPhase ||
                encounterIndex != currentEncounterIndex;

            currentEncounterIndex = encounterIndex;
            currentPhase = phase;
            currentLocalPhaseProgress01 = localPhaseProgress;
            currentPressure01 = pressure;

            if (changed)
            {
                transitionCount++;
                lastTransitionTimeUnscaled = Time.unscaledTime;
            }

            currentSnapshot = new PrototypeEncounterRhythmSnapshot(
                currentPhase,
                currentEncounterIndex,
                currentRouteProgress01,
                currentLocalPhaseProgress01,
                currentPressure01,
                runNumber,
                transitionCount,
                lastTransitionTimeUnscaled);

            SnapshotChanged?.Invoke(currentSnapshot);

            if (!changed)
            {
                return;
            }

            PrototypeEncounterRhythmEventHub.Publish(currentSnapshot);

            if (config.LogTransitions)
            {
                Debug.Log(
                    $"[M39] Encounter transition | Run={runNumber} | " +
                    $"Encounter={currentEncounterIndex} | Phase={currentPhase} | " +
                    $"Progress={currentRouteProgress01:P0} | Pressure={currentPressure01:0.00}",
                    this);
            }
        }

        private void TransitionToCompleted()
        {
            currentPhase = PrototypeEncounterPhase.Completed;
            currentLocalPhaseProgress01 = 1f;
            currentPressure01 = 0f;
            transitionCount++;
            lastTransitionTimeUnscaled = Time.unscaledTime;

            currentSnapshot = new PrototypeEncounterRhythmSnapshot(
                currentPhase,
                currentEncounterIndex,
                currentRouteProgress01,
                currentLocalPhaseProgress01,
                currentPressure01,
                runNumber,
                transitionCount,
                lastTransitionTimeUnscaled);

            SnapshotChanged?.Invoke(currentSnapshot);
            PrototypeEncounterRhythmEventHub.Publish(currentSnapshot);
        }

        private void SetInactive(bool publishTransition)
        {
            currentPhase = PrototypeEncounterPhase.Inactive;
            currentEncounterIndex = 0;
            currentLocalPhaseProgress01 = 0f;
            currentPressure01 = 0f;
            lastTransitionTimeUnscaled = Time.unscaledTime;

            currentSnapshot = new PrototypeEncounterRhythmSnapshot(
                currentPhase,
                currentEncounterIndex,
                currentRouteProgress01,
                currentLocalPhaseProgress01,
                currentPressure01,
                runNumber,
                transitionCount,
                lastTransitionTimeUnscaled);

            SnapshotChanged?.Invoke(currentSnapshot);

            if (publishTransition)
            {
                PrototypeEncounterRhythmEventHub.Publish(currentSnapshot);
            }
        }

        private void ValidateDependencies()
        {
            if (matchController == null)
            {
                Debug.LogError(
                    "[M39] PrototypeMatchController bağlantısı eksik.",
                    this);
            }

            if (progressTracker == null)
            {
                Debug.LogError(
                    "[M39] FigureProgressTracker bağlantısı eksik.",
                    this);
            }

            if (config == null)
            {
                Debug.LogError(
                    "[M39] PrototypeEncounterRhythmConfig bağlantısı eksik.",
                    this);
            }
        }
    }
}
