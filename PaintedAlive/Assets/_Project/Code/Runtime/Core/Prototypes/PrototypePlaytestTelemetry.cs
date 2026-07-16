using System;
using System.Collections.Generic;
using System.IO;
using PaintedAlive.Figures;
using PaintedAlive.Paint;
using PaintedAlive.Painters;
using UnityEngine;

namespace PaintedAlive.Core.Prototypes
{
    [Serializable]
    public sealed class PrototypeTelemetryEvent
    {
        public float matchTime;
        public string eventType;
        public string details;
    }

    [Serializable]
    public sealed class PrototypeTelemetrySample
    {
        public float matchTime;
        public string activeRole;
        public Vector3 figurePosition;
        public float progressNormalized;
        public float distanceTravelled;
        public float remainingDistance;
        public float pigment;
        public int activeStrokeCount;
    }

    [Serializable]
    public sealed class PrototypePlaytestReport
    {
        public string schemaVersion;
        public string sessionId;
        public string matchId;
        public string utcStartedAt;
        public string utcFinishedAt;
        public string outcome;

        public float configuredMatchDuration;
        public float actualRunningDuration;
        public float completionTime;

        public float figureRoleTime;
        public float painterRoleTime;
        public int roleSwitchCount;

        public float furthestProgressNormalized;
        public float furthestDistance;
        public float remainingDistance;

        public int strokeCount;
        public float totalStrokeLength;
        public float pigmentSpent;

        public int totalCutCount;
        public int wetCutCount;
        public int dryingCutCount;
        public int dryCutCount;

        public float blockedInputTime;
        public float longestBlockedInputSequence;

        public List<PrototypeTelemetryEvent> events = new();
        public List<PrototypeTelemetrySample> samples = new();
    }

    [DisallowMultipleComponent]
    public sealed class PrototypePlaytestTelemetry : MonoBehaviour
    {
        private const string SchemaVersion = "1.0.0";
        private const string TelemetryFolderName = "PlaytestTelemetry";

        [Header("Match")]
        [SerializeField] private PrototypeMatchController matchController;
        [SerializeField] private PrototypeRoleSwitcher roleSwitcher;
        [SerializeField] private FigureProgressTracker progressTracker;

        [Header("Figure")]
        [SerializeField] private Transform figure;
        [SerializeField] private FigureInputReader figureInputReader;

        [Header("Painter")]
        [SerializeField] private OilStrokeSystem strokeSystem;
        [SerializeField] private PainterPigmentReservoir pigmentReservoir;

        [Header("Sampling")]
        [SerializeField, Min(0.1f)] private float sampleInterval = 1f;

        [Header("Blocked Movement Detection")]
        [SerializeField, Range(0f, 1f)]
        private float movementInputThreshold = 0.25f;

        [SerializeField, Min(0.01f)]
        private float meaningfulProgressDistance = 0.2f;

        [SerializeField, Min(0f)]
        private float blockedGraceDuration = 0.75f;

        private readonly string sessionId =
            Guid.NewGuid().ToString("N");

        private PrototypePlaytestReport currentReport;

        private bool recording;
        private bool reportSaved;

        private float runningElapsed;
        private float sampleElapsed;
        private float figureRoleTime;
        private float painterRoleTime;

        private PrototypeRole previousRole;
        private int roleSwitchCount;

        private float lastMeaningfulProgress;
        private float currentBlockedSequence;
        private float totalBlockedInputTime;
        private float longestBlockedInputSequence;

        private int lastProgressMilestone;

        private string TelemetryDirectory =>
            Path.Combine(
                Application.persistentDataPath,
                TelemetryFolderName);

        private void OnEnable()
        {
            if (matchController != null)
            {
                matchController.StateChanged +=
                    HandleMatchStateChanged;
            }
        }

        private void OnDisable()
        {
            if (matchController != null)
            {
                matchController.StateChanged -=
                    HandleMatchStateChanged;
            }
        }

        private void Update()
        {
            if (!recording ||
                matchController == null ||
                matchController.State !=
                PrototypeMatchState.Running)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            runningElapsed += deltaTime;
            sampleElapsed += deltaTime;

            UpdateRoleMetrics(deltaTime);
            UpdateBlockedMovementMetric(deltaTime);
            UpdateProgressMilestones();

            if (sampleElapsed >= sampleInterval)
            {
                sampleElapsed = 0f;
                CaptureSample();
            }
        }

        private void HandleMatchStateChanged(
            PrototypeMatchState state)
        {
            switch (state)
            {
                case PrototypeMatchState.Countdown:
                    BeginReport();
                    AddEvent(
                        "countdown_started",
                        "Match countdown started.");
                    break;

                case PrototypeMatchState.Running:
                    if (!recording)
                    {
                        BeginReport();
                    }

                    AddEvent(
                        "match_started",
                        "Prototype match entered running state.");
                    break;

                case PrototypeMatchState.FigureEscaped:
                    FinalizeReport("figure_escaped");
                    break;

                case PrototypeMatchState.TimeExpired:
                    FinalizeReport("time_expired");
                    break;
            }
        }

        private void BeginReport()
        {
            currentReport = new PrototypePlaytestReport
            {
                schemaVersion = SchemaVersion,
                sessionId = sessionId,
                matchId = Guid.NewGuid().ToString("N"),
                utcStartedAt = DateTime.UtcNow.ToString("O")
            };

            recording = true;
            reportSaved = false;

            runningElapsed = 0f;
            sampleElapsed = 0f;
            figureRoleTime = 0f;
            painterRoleTime = 0f;
            roleSwitchCount = 0;

            lastMeaningfulProgress = 0f;
            currentBlockedSequence = 0f;
            totalBlockedInputTime = 0f;
            longestBlockedInputSequence = 0f;
            lastProgressMilestone = 0;

            previousRole = roleSwitcher != null
                ? roleSwitcher.CurrentRole
                : PrototypeRole.Figure;

            currentReport.configuredMatchDuration =
                matchController != null
                    ? matchController.TimeRemaining
                    : 0f;

            CaptureSample();
        }

        private void UpdateRoleMetrics(float deltaTime)
        {
            PrototypeRole currentRole =
                roleSwitcher != null
                    ? roleSwitcher.CurrentRole
                    : PrototypeRole.Figure;

            if (currentRole != previousRole)
            {
                roleSwitchCount++;

                AddEvent(
                    "role_changed",
                    $"{previousRole} -> {currentRole}");

                previousRole = currentRole;
                currentBlockedSequence = 0f;
            }

            if (currentRole == PrototypeRole.Figure)
            {
                figureRoleTime += deltaTime;
            }
            else
            {
                painterRoleTime += deltaTime;
            }
        }

        private void UpdateBlockedMovementMetric(float deltaTime)
        {
            if (roleSwitcher == null ||
                roleSwitcher.CurrentRole != PrototypeRole.Figure ||
                figureInputReader == null ||
                progressTracker == null)
            {
                currentBlockedSequence = 0f;
                return;
            }

            bool attemptingMovement =
                figureInputReader.Move.magnitude >=
                movementInputThreshold;

            float currentProgress =
                progressTracker.FurthestDistance;

            bool madeMeaningfulProgress =
                currentProgress >=
                lastMeaningfulProgress +
                meaningfulProgressDistance;

            if (madeMeaningfulProgress)
            {
                lastMeaningfulProgress = currentProgress;
                currentBlockedSequence = 0f;
                return;
            }

            if (!attemptingMovement)
            {
                currentBlockedSequence = 0f;
                return;
            }

            currentBlockedSequence += deltaTime;

            longestBlockedInputSequence = Mathf.Max(
                longestBlockedInputSequence,
                currentBlockedSequence);

            if (currentBlockedSequence >= blockedGraceDuration)
            {
                totalBlockedInputTime += deltaTime;
            }
        }

        private void UpdateProgressMilestones()
        {
            if (progressTracker == null)
            {
                return;
            }

            int milestone = Mathf.FloorToInt(
                progressTracker.NormalizedProgress * 4f);

            milestone = Mathf.Clamp(milestone, 0, 4);

            if (milestone <= lastProgressMilestone)
            {
                return;
            }

            lastProgressMilestone = milestone;

            AddEvent(
                "progress_milestone",
                $"{milestone * 25}%");
        }

        private void CaptureSample()
        {
            if (currentReport == null)
            {
                return;
            }

            currentReport.samples.Add(
                new PrototypeTelemetrySample
                {
                    matchTime = runningElapsed,

                    activeRole = roleSwitcher != null
                        ? roleSwitcher.CurrentRole.ToString()
                        : PrototypeRole.Figure.ToString(),

                    figurePosition = figure != null
                        ? figure.position
                        : Vector3.zero,

                    progressNormalized =
                        progressTracker != null
                            ? progressTracker.NormalizedProgress
                            : 0f,

                    distanceTravelled =
                        progressTracker != null
                            ? progressTracker.FurthestDistance
                            : 0f,

                    remainingDistance =
                        progressTracker != null
                            ? progressTracker.RemainingDistance
                            : 0f,

                    pigment =
                        pigmentReservoir != null
                            ? pigmentReservoir.Current
                            : 0f,

                    activeStrokeCount =
                        strokeSystem != null
                            ? strokeSystem.Strokes.Count
                            : 0
                });
        }

        private void FinalizeReport(string outcome)
        {
            if (!recording ||
                reportSaved ||
                currentReport == null)
            {
                return;
            }

            CaptureSample();

            currentReport.utcFinishedAt =
                DateTime.UtcNow.ToString("O");

            currentReport.outcome = outcome;
            currentReport.actualRunningDuration = runningElapsed;

            currentReport.completionTime =
                matchController != null
                    ? matchController.CompletionTime
                    : runningElapsed;

            currentReport.figureRoleTime = figureRoleTime;
            currentReport.painterRoleTime = painterRoleTime;
            currentReport.roleSwitchCount = roleSwitchCount;

            if (progressTracker != null)
            {
                currentReport.furthestProgressNormalized =
                    progressTracker.NormalizedProgress;

                currentReport.furthestDistance =
                    progressTracker.FurthestDistance;

                currentReport.remainingDistance =
                    progressTracker.RemainingDistance;
            }

            if (pigmentReservoir != null)
            {
                currentReport.pigmentSpent =
                    pigmentReservoir.TotalSpentThisMatch;
            }

            CollectStrokeMetrics();

            currentReport.blockedInputTime =
                totalBlockedInputTime;

            currentReport.longestBlockedInputSequence =
                longestBlockedInputSequence;

            AddEvent(
                "match_finished",
                outcome);

            SaveReport();

            reportSaved = true;
            recording = false;
        }

        private void CollectStrokeMetrics()
        {
            if (strokeSystem == null)
            {
                return;
            }

            foreach (OilStrokeRuntime stroke in
                     strokeSystem.Strokes)
            {
                if (stroke == null)
                {
                    continue;
                }

                currentReport.strokeCount++;
                currentReport.totalStrokeLength +=
                    stroke.OriginalLength;

                currentReport.totalCutCount +=
                    stroke.CutCount;

                currentReport.wetCutCount +=
                    stroke.WetCutCount;

                currentReport.dryingCutCount +=
                    stroke.DryingCutCount;

                currentReport.dryCutCount +=
                    stroke.DryCutCount;
            }
        }

        private void AddEvent(
            string eventType,
            string details)
        {
            if (currentReport == null)
            {
                return;
            }

            currentReport.events.Add(
                new PrototypeTelemetryEvent
                {
                    matchTime = runningElapsed,
                    eventType = eventType,
                    details = details
                });
        }

        private void SaveReport()
        {
            Directory.CreateDirectory(
                TelemetryDirectory);

            string timestamp =
                DateTime.UtcNow.ToString(
                    "yyyyMMdd_HHmmss");

            string shortMatchId =
                currentReport.matchId.Substring(0, 8);

            string fileName =
                $"painted_alive_{timestamp}_{shortMatchId}.json";

            string fullPath =
                Path.Combine(
                    TelemetryDirectory,
                    fileName);

            string json = JsonUtility.ToJson(
                currentReport,
                true);

            File.WriteAllText(
                fullPath,
                json);

            Debug.Log(
                $"[Playtest Telemetry] Report saved:\n{fullPath}",
                this);
        }

        [ContextMenu("Open Telemetry Folder")]
        public void OpenTelemetryFolder()
        {
            Directory.CreateDirectory(
                TelemetryDirectory);

            string normalizedPath =
                TelemetryDirectory.Replace("\\", "/");

            Application.OpenURL(
                $"file:///{normalizedPath}");
        }

        private void OnApplicationQuit()
        {
            if (recording && !reportSaved)
            {
                FinalizeReport("application_closed");
            }
        }
    }
}
