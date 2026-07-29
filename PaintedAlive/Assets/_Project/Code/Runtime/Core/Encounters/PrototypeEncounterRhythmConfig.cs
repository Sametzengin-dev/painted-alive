using UnityEngine;

namespace PaintedAlive.Core.Encounters
{
    [CreateAssetMenu(
        fileName = "PrototypeEncounterRhythmConfig",
        menuName = "Painted Alive/Prototypes/Encounter Rhythm Config")]
    public sealed class PrototypeEncounterRhythmConfig : ScriptableObject
    {
        [Header("Route Bands")]
        [SerializeField, Range(0.05f, 0.45f)]
        private float encounterOneEnd = 0.28f;

        [SerializeField, Range(0.06f, 0.55f)]
        private float breathOneEnd = 0.34f;

        [SerializeField, Range(0.20f, 0.75f)]
        private float encounterTwoEnd = 0.58f;

        [SerializeField, Range(0.25f, 0.82f)]
        private float breathTwoEnd = 0.66f;

        [SerializeField, Range(0.50f, 0.96f)]
        private float encounterThreeEnd = 0.90f;

        [SerializeField, Range(0.55f, 0.98f)]
        private float finalEscapeStart = 0.94f;

        [Header("Encounter Internal Rhythm")]
        [SerializeField, Range(0.05f, 0.35f)]
        private float readEnd = 0.16f;

        [SerializeField, Range(0.15f, 0.55f)]
        private float lightPressureEnd = 0.36f;

        [SerializeField, Range(0.30f, 0.75f)]
        private float toolResponseEnd = 0.58f;

        [SerializeField, Range(0.55f, 0.95f)]
        private float combinationPressureEnd = 0.82f;

        [Header("Presentation")]
        [SerializeField, Min(0.25f)]
        private float transitionBannerDuration = 2.4f;

        [SerializeField]
        private bool logTransitions = true;

        public float TransitionBannerDuration => transitionBannerDuration;
        public bool LogTransitions => logTransitions;
        public int EncounterCount => 3;

        public void Evaluate(
            float routeProgress01,
            out int encounterIndex,
            out PrototypeEncounterPhase phase,
            out float localPhaseProgress01,
            out float pressure01)
        {
            float progress = Mathf.Clamp01(routeProgress01);

            if (progress >= finalEscapeStart)
            {
                encounterIndex = 3;
                phase = PrototypeEncounterPhase.FinalEscape;
                localPhaseProgress01 = Mathf.InverseLerp(
                    finalEscapeStart,
                    1f,
                    progress);
                pressure01 = 0.85f;
                return;
            }

            if (progress < encounterOneEnd)
            {
                EvaluateEncounter(
                    progress,
                    0f,
                    encounterOneEnd,
                    1,
                    out encounterIndex,
                    out phase,
                    out localPhaseProgress01,
                    out pressure01);
                return;
            }

            if (progress < breathOneEnd)
            {
                EvaluateBreath(
                    progress,
                    encounterOneEnd,
                    breathOneEnd,
                    1,
                    out encounterIndex,
                    out phase,
                    out localPhaseProgress01,
                    out pressure01);
                return;
            }

            if (progress < encounterTwoEnd)
            {
                EvaluateEncounter(
                    progress,
                    breathOneEnd,
                    encounterTwoEnd,
                    2,
                    out encounterIndex,
                    out phase,
                    out localPhaseProgress01,
                    out pressure01);
                return;
            }

            if (progress < breathTwoEnd)
            {
                EvaluateBreath(
                    progress,
                    encounterTwoEnd,
                    breathTwoEnd,
                    2,
                    out encounterIndex,
                    out phase,
                    out localPhaseProgress01,
                    out pressure01);
                return;
            }

            if (progress < encounterThreeEnd)
            {
                EvaluateEncounter(
                    progress,
                    breathTwoEnd,
                    encounterThreeEnd,
                    3,
                    out encounterIndex,
                    out phase,
                    out localPhaseProgress01,
                    out pressure01);
                return;
            }

            EvaluateBreath(
                progress,
                encounterThreeEnd,
                finalEscapeStart,
                3,
                out encounterIndex,
                out phase,
                out localPhaseProgress01,
                out pressure01);
        }

        private void EvaluateEncounter(
            float routeProgress,
            float encounterStart,
            float encounterEnd,
            int index,
            out int encounterIndex,
            out PrototypeEncounterPhase phase,
            out float localPhaseProgress01,
            out float pressure01)
        {
            encounterIndex = index;
            float local = Mathf.InverseLerp(
                encounterStart,
                encounterEnd,
                routeProgress);

            if (local < readEnd)
            {
                phase = PrototypeEncounterPhase.Read;
                localPhaseProgress01 = Mathf.InverseLerp(0f, readEnd, local);
                pressure01 = 0.10f;
                return;
            }

            if (local < lightPressureEnd)
            {
                phase = PrototypeEncounterPhase.LightPressure;
                localPhaseProgress01 = Mathf.InverseLerp(
                    readEnd,
                    lightPressureEnd,
                    local);
                pressure01 = 0.35f;
                return;
            }

            if (local < toolResponseEnd)
            {
                phase = PrototypeEncounterPhase.ToolResponse;
                localPhaseProgress01 = Mathf.InverseLerp(
                    lightPressureEnd,
                    toolResponseEnd,
                    local);
                pressure01 = 0.55f;
                return;
            }

            if (local < combinationPressureEnd)
            {
                phase = PrototypeEncounterPhase.CombinationPressure;
                localPhaseProgress01 = Mathf.InverseLerp(
                    toolResponseEnd,
                    combinationPressureEnd,
                    local);
                pressure01 = 1f;
                return;
            }

            phase = PrototypeEncounterPhase.RescueAndEscape;
            localPhaseProgress01 = Mathf.InverseLerp(
                combinationPressureEnd,
                1f,
                local);
            pressure01 = 0.72f;
        }

        private static void EvaluateBreath(
            float routeProgress,
            float breathStart,
            float breathEnd,
            int index,
            out int encounterIndex,
            out PrototypeEncounterPhase phase,
            out float localPhaseProgress01,
            out float pressure01)
        {
            encounterIndex = index;
            phase = PrototypeEncounterPhase.Breath;
            localPhaseProgress01 = Mathf.InverseLerp(
                breathStart,
                breathEnd,
                routeProgress);
            pressure01 = 0f;
        }

        private void OnValidate()
        {
            encounterOneEnd = Mathf.Clamp(encounterOneEnd, 0.05f, 0.45f);
            breathOneEnd = Mathf.Clamp(
                breathOneEnd,
                encounterOneEnd + 0.01f,
                0.55f);
            encounterTwoEnd = Mathf.Clamp(
                encounterTwoEnd,
                breathOneEnd + 0.03f,
                0.75f);
            breathTwoEnd = Mathf.Clamp(
                breathTwoEnd,
                encounterTwoEnd + 0.01f,
                0.82f);
            encounterThreeEnd = Mathf.Clamp(
                encounterThreeEnd,
                breathTwoEnd + 0.03f,
                0.96f);
            finalEscapeStart = Mathf.Clamp(
                finalEscapeStart,
                encounterThreeEnd + 0.01f,
                0.99f);

            readEnd = Mathf.Clamp(readEnd, 0.05f, 0.35f);
            lightPressureEnd = Mathf.Clamp(
                lightPressureEnd,
                readEnd + 0.02f,
                0.55f);
            toolResponseEnd = Mathf.Clamp(
                toolResponseEnd,
                lightPressureEnd + 0.02f,
                0.75f);
            combinationPressureEnd = Mathf.Clamp(
                combinationPressureEnd,
                toolResponseEnd + 0.02f,
                0.95f);

            transitionBannerDuration = Mathf.Max(
                0.25f,
                transitionBannerDuration);
        }
    }
}
