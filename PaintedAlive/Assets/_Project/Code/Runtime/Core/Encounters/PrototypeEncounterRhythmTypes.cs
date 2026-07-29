using System;

namespace PaintedAlive.Core.Encounters
{
    public enum PrototypeEncounterPhase
    {
        Inactive = 0,
        Read = 1,
        LightPressure = 2,
        ToolResponse = 3,
        CombinationPressure = 4,
        RescueAndEscape = 5,
        Breath = 6,
        FinalEscape = 7,
        Completed = 8
    }

    public readonly struct PrototypeEncounterRhythmSnapshot
    {
        public PrototypeEncounterRhythmSnapshot(
            PrototypeEncounterPhase phase,
            int encounterIndex,
            float routeProgress01,
            float localPhaseProgress01,
            float pressure01,
            int runNumber,
            int transitionIndex,
            float transitionTimeUnscaled)
        {
            Phase = phase;
            EncounterIndex = encounterIndex;
            RouteProgress01 = routeProgress01;
            LocalPhaseProgress01 = localPhaseProgress01;
            Pressure01 = pressure01;
            RunNumber = runNumber;
            TransitionIndex = transitionIndex;
            TransitionTimeUnscaled = transitionTimeUnscaled;
        }

        public PrototypeEncounterPhase Phase { get; }
        public int EncounterIndex { get; }
        public float RouteProgress01 { get; }
        public float LocalPhaseProgress01 { get; }
        public float Pressure01 { get; }
        public int RunNumber { get; }
        public int TransitionIndex { get; }
        public float TransitionTimeUnscaled { get; }
        public bool IsPressurePhase =>
            Phase == PrototypeEncounterPhase.LightPressure ||
            Phase == PrototypeEncounterPhase.ToolResponse ||
            Phase == PrototypeEncounterPhase.CombinationPressure ||
            Phase == PrototypeEncounterPhase.RescueAndEscape ||
            Phase == PrototypeEncounterPhase.FinalEscape;
    }

    public static class PrototypeEncounterRhythmEventHub
    {
        public static event Action<PrototypeEncounterRhythmSnapshot> Transitioned;

        internal static void Publish(PrototypeEncounterRhythmSnapshot snapshot)
        {
            Transitioned?.Invoke(snapshot);
        }
    }
}
