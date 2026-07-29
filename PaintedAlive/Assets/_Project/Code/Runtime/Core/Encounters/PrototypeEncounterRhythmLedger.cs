using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Core.Encounters
{
    [Serializable]
    public struct PrototypeEncounterTransitionRecord
    {
        public int runNumber;
        public int transitionIndex;
        public int encounterIndex;
        public PrototypeEncounterPhase phase;
        public float routeProgress01;
        public float pressure01;
        public float timeUnscaled;
    }

    [DisallowMultipleComponent]
    public sealed class PrototypeEncounterRhythmLedger : MonoBehaviour
    {
        [SerializeField, Min(8)] private int maximumRecords = 64;
        [SerializeField] private List<PrototypeEncounterTransitionRecord> records = new();

        public IReadOnlyList<PrototypeEncounterTransitionRecord> Records => records;
        public int RecordCount => records.Count;
        public PrototypeEncounterTransitionRecord LastRecord =>
            records.Count > 0 ? records[records.Count - 1] : default;

        private void OnEnable()
        {
            PrototypeEncounterRhythmEventHub.Transitioned += HandleTransition;
        }

        private void OnDisable()
        {
            PrototypeEncounterRhythmEventHub.Transitioned -= HandleTransition;
        }

        public void Clear()
        {
            records.Clear();
        }

        private void HandleTransition(PrototypeEncounterRhythmSnapshot snapshot)
        {
            if (snapshot.Phase == PrototypeEncounterPhase.Inactive)
            {
                return;
            }

            records.Add(new PrototypeEncounterTransitionRecord
            {
                runNumber = snapshot.RunNumber,
                transitionIndex = snapshot.TransitionIndex,
                encounterIndex = snapshot.EncounterIndex,
                phase = snapshot.Phase,
                routeProgress01 = snapshot.RouteProgress01,
                pressure01 = snapshot.Pressure01,
                timeUnscaled = Time.unscaledTime
            });

            int capacity = Mathf.Max(8, maximumRecords);
            while (records.Count > capacity)
            {
                records.RemoveAt(0);
            }
        }
    }
}
