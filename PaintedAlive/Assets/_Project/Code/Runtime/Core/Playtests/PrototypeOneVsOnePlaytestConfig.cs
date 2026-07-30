using UnityEngine;

namespace PaintedAlive.Core.Playtests
{
    [CreateAssetMenu(
        fileName = "PrototypeOneVsOnePlaytestConfig",
        menuName = "Painted Alive/Prototypes/One Vs One Playtest Config")]
    public sealed class PrototypeOneVsOnePlaytestConfig : ScriptableObject
    {
        [Header("Authoritative Match Expectation")]
        [SerializeField, Min(30f)] private float expectedMatchDuration = 300f;

        [Header("Acceptance")]
        [SerializeField, Range(1, 3)] private int requiredDistinctOutcomes = 3;
        [SerializeField, Range(0.05f, 0.60f)] private float earlyPassProgressThreshold = 0.28f;
        [SerializeField, Range(0.005f, 0.20f)] private float rampProgressDelta = 0.025f;
        [SerializeField, Min(0.05f)] private float evidenceScanInterval = 0.15f;

        [Header("Presentation")]
        [SerializeField] private bool showProtocolHUD = true;
        [SerializeField] private bool autoConfirmDetectedEvidence = true;
        [SerializeField] private bool writeJsonReport = true;
        [SerializeField] private bool logOutcomeChanges = true;

        public float ExpectedMatchDuration => expectedMatchDuration;
        public int RequiredDistinctOutcomes => requiredDistinctOutcomes;
        public float EarlyPassProgressThreshold => earlyPassProgressThreshold;
        public float RampProgressDelta => rampProgressDelta;
        public float EvidenceScanInterval => evidenceScanInterval;
        public bool ShowProtocolHUD => showProtocolHUD;
        public bool AutoConfirmDetectedEvidence => autoConfirmDetectedEvidence;
        public bool WriteJsonReport => writeJsonReport;
        public bool LogOutcomeChanges => logOutcomeChanges;

        private void OnValidate()
        {
            expectedMatchDuration = Mathf.Max(30f, expectedMatchDuration);
            requiredDistinctOutcomes = Mathf.Clamp(requiredDistinctOutcomes, 1, 3);
            earlyPassProgressThreshold = Mathf.Clamp(earlyPassProgressThreshold, 0.05f, 0.60f);
            rampProgressDelta = Mathf.Clamp(rampProgressDelta, 0.005f, 0.20f);
            evidenceScanInterval = Mathf.Max(0.05f, evidenceScanInterval);
        }
    }
}
