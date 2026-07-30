using UnityEngine;

namespace PaintedAlive.Network.Spike
{
    [CreateAssetMenu(
        fileName = "PrototypeNetworkSpikeConfig",
        menuName = "Painted Alive/Network/Prototype Network Spike Config")]
    public sealed class PrototypeNetworkSpikeConfig : ScriptableObject
    {
        [Header("Candidate Versions - Recorded For This Spike")]
        [SerializeField] private string fusionCandidate = "Fusion 2.1.1 / Host-Server";
        [SerializeField] private string fusionKccCandidate = "Advanced KCC 2.1.0";
        [SerializeField] private string fishNetCandidate = "FishNet 4.7.2R";

        [Header("Deterministic Command Workload")]
        [SerializeField, Min(1)] private int deterministicSeed = 42142;
        [SerializeField, Min(1)] private int strokeCommandCount = 100;
        [SerializeField, Range(2, 64)] private int controlPointsPerStroke = 12;
        [SerializeField, Min(1f)] private float strokeCommandsPerSecond = 20f;
        [SerializeField, Min(1)] private int figureInputCommandCount = 300;
        [SerializeField, Min(1)] private int figureSnapshotCount = 100;

        [Header("Simulated Link")]
        [SerializeField, Min(0f)] private float jitterMilliseconds = 8f;
        [SerializeField, Range(0f, 20f)] private float packetLossPercent = 0.5f;
        [SerializeField, Min(0f)] private float baselineRttMilliseconds = 0f;
        [SerializeField, Min(0f)] private float standardRttMilliseconds = 100f;
        [SerializeField, Min(0f)] private float stressRttMilliseconds = 150f;

        [Header("Quantization")]
        [SerializeField, Min(0.001f)] private float positionStepMeters = 0.01f;
        [SerializeField, Min(1f)] private float maximumSurfaceExtentMeters = 32f;

        [Header("Foundation Acceptance - Prototype Hypothesis")]
        [SerializeField, Min(32)] private int maximumMeanBytesPerStroke = 160;
        [SerializeField, Range(0f, 1f)] private float minimumDeliveryRatio = 0.95f;
        [SerializeField] private bool requireDeterministicRoundTrip = true;

        public string FusionCandidate => fusionCandidate;
        public string FusionKccCandidate => fusionKccCandidate;
        public string FishNetCandidate => fishNetCandidate;
        public int DeterministicSeed => deterministicSeed;
        public int StrokeCommandCount => strokeCommandCount;
        public int ControlPointsPerStroke => controlPointsPerStroke;
        public float StrokeCommandsPerSecond => strokeCommandsPerSecond;
        public int FigureInputCommandCount => figureInputCommandCount;
        public int FigureSnapshotCount => figureSnapshotCount;
        public float JitterMilliseconds => jitterMilliseconds;
        public float PacketLossPercent => packetLossPercent;
        public float BaselineRttMilliseconds => baselineRttMilliseconds;
        public float StandardRttMilliseconds => standardRttMilliseconds;
        public float StressRttMilliseconds => stressRttMilliseconds;
        public float PositionStepMeters => positionStepMeters;
        public float MaximumSurfaceExtentMeters => maximumSurfaceExtentMeters;
        public int MaximumMeanBytesPerStroke => maximumMeanBytesPerStroke;
        public float MinimumDeliveryRatio => minimumDeliveryRatio;
        public bool RequireDeterministicRoundTrip => requireDeterministicRoundTrip;

        private void OnValidate()
        {
            deterministicSeed = Mathf.Max(1, deterministicSeed);
            strokeCommandCount = Mathf.Max(1, strokeCommandCount);
            controlPointsPerStroke = Mathf.Clamp(controlPointsPerStroke, 2, 64);
            strokeCommandsPerSecond = Mathf.Max(1f, strokeCommandsPerSecond);
            figureInputCommandCount = Mathf.Max(1, figureInputCommandCount);
            figureSnapshotCount = Mathf.Max(1, figureSnapshotCount);
            jitterMilliseconds = Mathf.Max(0f, jitterMilliseconds);
            packetLossPercent = Mathf.Clamp(packetLossPercent, 0f, 20f);
            baselineRttMilliseconds = Mathf.Max(0f, baselineRttMilliseconds);
            standardRttMilliseconds = Mathf.Max(0f, standardRttMilliseconds);
            stressRttMilliseconds = Mathf.Max(0f, stressRttMilliseconds);
            positionStepMeters = Mathf.Max(0.001f, positionStepMeters);
            maximumSurfaceExtentMeters = Mathf.Max(1f, maximumSurfaceExtentMeters);
            maximumMeanBytesPerStroke = Mathf.Max(32, maximumMeanBytesPerStroke);
            minimumDeliveryRatio = Mathf.Clamp01(minimumDeliveryRatio);
        }
    }
}
