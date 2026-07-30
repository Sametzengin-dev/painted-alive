using UnityEngine;

namespace PaintedAlive.Core.Playtests.Validation
{
    [CreateAssetMenu(
        fileName = "PrototypePlaytestAcceptanceConfig",
        menuName = "Painted Alive/Playtests/Prototype Acceptance Config")]
    public sealed class PrototypePlaytestAcceptanceConfig : ScriptableObject
    {
        [Header("Report Collection")]
        [SerializeField, Min(0f)] private float reportCollectionDelay = 0.25f;
        [SerializeField, Min(0.25f)] private float reportWaitTimeout = 3f;

        [Header("Control Reliability - Prototype Hypothesis")]
        [SerializeField, Range(0f, 1f)] private float maximumBlockedInputRatio = 0.20f;
        [SerializeField, Min(0f)] private float maximumLongestBlockedSequence = 6f;

        [Header("Repeated Run Gate - Prototype Hypothesis")]
        [SerializeField, Min(1)] private int evaluationWindow = 3;
        [SerializeField, Min(1)] private int requiredPassingRuns = 3;
        [SerializeField, Range(0f, 1f)] private float minimumReplayYesRatio = 0.67f;
        [SerializeField, Range(0f, 1f)] private float minimumAverageReadabilityRatio = 0.80f;

        public float ReportCollectionDelay => reportCollectionDelay;
        public float ReportWaitTimeout => reportWaitTimeout;
        public float MaximumBlockedInputRatio => maximumBlockedInputRatio;
        public float MaximumLongestBlockedSequence => maximumLongestBlockedSequence;
        public int EvaluationWindow => evaluationWindow;
        public int RequiredPassingRuns => Mathf.Min(requiredPassingRuns, evaluationWindow);
        public float MinimumReplayYesRatio => minimumReplayYesRatio;
        public float MinimumAverageReadabilityRatio => minimumAverageReadabilityRatio;

        private void OnValidate()
        {
            reportCollectionDelay = Mathf.Max(0f, reportCollectionDelay);
            reportWaitTimeout = Mathf.Max(0.25f, reportWaitTimeout);
            maximumBlockedInputRatio = Mathf.Clamp01(maximumBlockedInputRatio);
            maximumLongestBlockedSequence = Mathf.Max(0f, maximumLongestBlockedSequence);
            evaluationWindow = Mathf.Max(1, evaluationWindow);
            requiredPassingRuns = Mathf.Clamp(requiredPassingRuns, 1, evaluationWindow);
            minimumReplayYesRatio = Mathf.Clamp01(minimumReplayYesRatio);
            minimumAverageReadabilityRatio = Mathf.Clamp01(minimumAverageReadabilityRatio);
        }
    }
}
