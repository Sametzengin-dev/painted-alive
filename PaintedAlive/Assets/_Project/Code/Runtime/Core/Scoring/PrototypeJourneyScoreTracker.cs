using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using UnityEngine;

namespace PaintedAlive.Core.Scoring
{
    [DisallowMultipleComponent]
    public sealed class PrototypeJourneyScoreTracker : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private FigureClarityState figure;
        [SerializeField] private Transform routeStart;
        [SerializeField] private Transform routeFinish;
        [SerializeField] private PrototypeJourneyScoreConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField, Range(0f, 1f)] private float maximumProgress01;
        [SerializeField] private int distanceScore;
        [SerializeField] private int exitBonus;
        [SerializeField] private bool stainArrivalRecorded;
        [SerializeField] private bool normalExitCompleted;
        [SerializeField] private FigureFrameExitOutcome lastExitOutcome;
        [SerializeField] private int publishedEventCount;

        private float nextSampleAt;
        private int nextDistanceEventThreshold;

        public event Action<PrototypeJourneyScoreSnapshot> ScoreChanged;

        public FigureClarityState Figure => figure;
        public Transform RouteStart => routeStart;
        public Transform RouteFinish => routeFinish;
        public float MaximumProgress01 => maximumProgress01;
        public int DistanceScore => distanceScore;
        public int ExitBonus => exitBonus;
        public int TotalScore => distanceScore + exitBonus;
        public bool StainArrivalRecorded => stainArrivalRecorded;
        public bool NormalExitCompleted => normalExitCompleted;
        public FigureFrameExitOutcome LastExitOutcome => lastExitOutcome;
        public int PublishedEventCount => publishedEventCount;

        public PrototypeJourneyScoreSnapshot CurrentSnapshot =>
            new PrototypeJourneyScoreSnapshot(
                figure,
                maximumProgress01,
                distanceScore,
                exitBonus,
                stainArrivalRecorded,
                normalExitCompleted,
                lastExitOutcome);

        public void Configure(
            FigureClarityState figureState,
            Transform start,
            Transform finish,
            PrototypeJourneyScoreConfig scoreConfig)
        {
            figure = figureState;
            routeStart = start;
            routeFinish = finish;
            config = scoreConfig;
        }

        /// <summary>
        /// Clears all per-run score state when the authoritative prototype match
        /// begins a new countdown. This does not recreate anchors or config.
        /// </summary>
        public void ResetForNewMatch()
        {
            maximumProgress01 = 0f;
            distanceScore = 0;
            exitBonus = 0;
            stainArrivalRecorded = false;
            normalExitCompleted = false;
            lastExitOutcome = FigureFrameExitOutcome.None;
            publishedEventCount = 0;

            int step = config != null ? config.DistanceEventStep : 50;
            nextDistanceEventThreshold = Mathf.Max(1, step);
            nextSampleAt = Time.time;

            NotifyScoreChanged();
        }

        private void Awake()
        {
            if (figure == null)
            {
                figure = GetComponent<FigureClarityState>();
            }

            int step = config != null ? config.DistanceEventStep : 50;
            nextDistanceEventThreshold = Mathf.Max(1, step);
        }

        private void OnEnable()
        {
            FigureFrameExitRuleService.ExitEvaluated += HandleExitEvaluated;
            nextSampleAt = Time.time;
        }

        private void OnDisable()
        {
            FigureFrameExitRuleService.ExitEvaluated -= HandleExitEvaluated;
        }

        private void Start()
        {
            ValidateDependencies();
            RecalculateDistanceScore(forceNotify: true);
        }

        private void Update()
        {
            if (Time.time < nextSampleAt)
            {
                return;
            }

            nextSampleAt = Time.time +
                (config != null ? config.SampleInterval : 0.08f);

            SampleJourneyProgress();
        }

        private void SampleJourneyProgress()
        {
            if (figure == null || routeStart == null || routeFinish == null)
            {
                return;
            }

            Vector3 route = routeFinish.position - routeStart.position;
            float routeLengthSquared = route.sqrMagnitude;
            if (routeLengthSquared <= 0.0001f)
            {
                return;
            }

            Vector3 fromStart = figure.transform.position - routeStart.position;
            float projectedProgress = Vector3.Dot(fromStart, route) / routeLengthSquared;
            projectedProgress = Mathf.Clamp01(projectedProgress);

            if (projectedProgress <= maximumProgress01 + 0.0001f)
            {
                return;
            }

            maximumProgress01 = projectedProgress;
            RecalculateDistanceScore(forceNotify: false);
        }

        private void RecalculateDistanceScore(bool forceNotify)
        {
            int maximumScore = config != null
                ? config.MaximumDistanceScore
                : 1000;

            int previousDistanceScore = distanceScore;
            distanceScore = Mathf.Clamp(
                Mathf.FloorToInt(maximumProgress01 * maximumScore),
                0,
                maximumScore);

            if (!forceNotify && distanceScore == previousDistanceScore)
            {
                return;
            }

            NotifyScoreChanged();
            PublishDistanceEventsIfNeeded();
        }

        private void PublishDistanceEventsIfNeeded()
        {
            int step = config != null ? config.DistanceEventStep : 50;
            step = Mathf.Max(1, step);

            while (distanceScore >= nextDistanceEventThreshold)
            {
                PublishEvent(PrototypeJourneyScoreEventType.DistanceProgress);
                nextDistanceEventThreshold += step;
            }
        }

        private void HandleExitEvaluated(FigureFrameExitDecision decision)
        {
            if (figure == null || decision.Figure != figure)
            {
                return;
            }

            maximumProgress01 = 1f;
            lastExitOutcome = decision.Outcome;
            RecalculateDistanceScore(forceNotify: false);

            switch (decision.Outcome)
            {
                case FigureFrameExitOutcome.StainSupportArrival:
                    if (!stainArrivalRecorded)
                    {
                        stainArrivalRecorded = true;
                        PublishEvent(PrototypeJourneyScoreEventType.StainSupportArrival);
                    }

                    break;

                case FigureFrameExitOutcome.NormalFigureExit:
                    if (!normalExitCompleted)
                    {
                        normalExitCompleted = true;
                        exitBonus = Mathf.Max(0, decision.AwardedScore);
                        PublishEvent(PrototypeJourneyScoreEventType.NormalFigureExit);
                    }

                    break;
            }

            NotifyScoreChanged();

            Debug.Log(
                $"[M37] Journey Score | Figure={figure.name} | " +
                $"Distance={distanceScore} | ExitBonus={exitBonus} | " +
                $"Total={TotalScore} | Outcome={lastExitOutcome} | " +
                $"NormalExit={normalExitCompleted}",
                this);
        }

        private void PublishEvent(PrototypeJourneyScoreEventType eventType)
        {
            publishedEventCount++;
            PrototypeJourneyScoreEventHub.Publish(
                new PrototypeJourneyScoreEvent(
                    eventType,
                    CurrentSnapshot,
                    Time.time));
        }

        private void NotifyScoreChanged()
        {
            ScoreChanged?.Invoke(CurrentSnapshot);
        }

        private void ValidateDependencies()
        {
            if (figure == null)
            {
                Debug.LogError(
                    "[M37] FigureClarityState bağlantısı eksik.",
                    this);
            }

            if (routeStart == null || routeFinish == null)
            {
                Debug.LogError(
                    "[M37] Yolculuk başlangıç veya bitiş ankrajı eksik.",
                    this);
            }

            if (config == null)
            {
                Debug.LogError(
                    "[M37] PrototypeJourneyScoreConfig bağlantısı eksik.",
                    this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (routeStart == null || routeFinish == null)
            {
                return;
            }

            Gizmos.DrawLine(routeStart.position, routeFinish.position);
            Gizmos.DrawWireSphere(routeStart.position, 0.22f);
            Gizmos.DrawWireSphere(routeFinish.position, 0.22f);
        }
    }
}
