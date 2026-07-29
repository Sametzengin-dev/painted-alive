using System;
using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Core.Scoring
{
    [DisallowMultipleComponent]
    public sealed class PrototypeJourneyScoreLedger : MonoBehaviour
    {
        [Serializable]
        private sealed class LedgerEntry
        {
            public string eventType;
            public float time;
            public int distanceScore;
            public int exitBonus;
            public int totalScore;
            public string clarityLevel;
        }

        [Header("Dependencies")]
        [SerializeField] private FigureClarityState figure;

        [Header("Runtime - Read Only")]
        [SerializeField, Min(4)] private int maximumEntries = 24;
        [SerializeField] private int recordedEventCount;
        [SerializeField] private string lastEvent = "None";
        [SerializeField] private List<LedgerEntry> recentEntries = new();

        public int RecordedEventCount => recordedEventCount;
        public string LastEvent => lastEvent;

        public void Configure(FigureClarityState figureState)
        {
            figure = figureState;
        }

        private void Awake()
        {
            if (figure == null)
            {
                figure = GetComponent<FigureClarityState>();
            }
        }

        private void OnEnable()
        {
            PrototypeJourneyScoreEventHub.EventRaised += HandleEventRaised;
        }

        private void OnDisable()
        {
            PrototypeJourneyScoreEventHub.EventRaised -= HandleEventRaised;
        }

        private void HandleEventRaised(PrototypeJourneyScoreEvent scoreEvent)
        {
            if (figure == null || scoreEvent.Snapshot.Figure != figure)
            {
                return;
            }

            FigureClarityLevel clarity = figure.CurrentLevel;
            var entry = new LedgerEntry
            {
                eventType = scoreEvent.EventType.ToString(),
                time = scoreEvent.RaisedAt,
                distanceScore = scoreEvent.Snapshot.DistanceScore,
                exitBonus = scoreEvent.Snapshot.ExitBonus,
                totalScore = scoreEvent.Snapshot.TotalScore,
                clarityLevel = clarity.ToString()
            };

            recentEntries.Add(entry);
            while (recentEntries.Count > Mathf.Max(4, maximumEntries))
            {
                recentEntries.RemoveAt(0);
            }

            recordedEventCount++;
            lastEvent =
                $"{entry.eventType} | Distance={entry.distanceScore} | " +
                $"Exit={entry.exitBonus} | Total={entry.totalScore} | " +
                $"Clarity={entry.clarityLevel}";
        }
    }
}
