using System;
using System.Collections.Generic;

namespace PaintedAlive.Network.Spike
{
    public enum PrototypeNetworkSpikeProfileKind
    {
        Baseline = 0,
        Rtt100 = 1,
        Rtt150 = 2
    }

    [Serializable]
    public sealed class PrototypeNetworkSpikeProfileResult
    {
        public PrototypeNetworkSpikeProfileKind profile;
        public float configuredRttMilliseconds;
        public float configuredJitterMilliseconds;
        public float configuredPacketLossPercent;
        public int strokeCommandsGenerated;
        public int strokePacketsSent;
        public int strokePacketsDelivered;
        public int strokePacketsDropped;
        public int reorderedStrokePackets;
        public int strokeDecodeFailures;
        public int deterministicMismatches;
        public long strokeBytesSent;
        public float meanBytesPerStroke;
        public float deliveryRatio;
        public float meanOneWayLatencyMilliseconds;
        public float p95OneWayLatencyMilliseconds;
        public float maximumOneWayLatencyMilliseconds;
        public int figureInputCommands;
        public int figureInputBytes;
        public int figureSnapshots;
        public int figureSnapshotBytes;
        public string encodedHash;
        public string decodedHash;
        public bool deterministicRoundTripPassed;
        public bool byteBudgetPassed;
        public bool deliveryGatePassed;
        public bool foundationProfilePassed;
    }

    [Serializable]
    public sealed class PrototypeNetworkSpikeReport
    {
        public string schemaVersion = "m42-network-spike-foundation-1.0.0";
        public string utcCreatedAt;
        public string unityVersion;
        public string activeScene;
        public string fusionCandidate;
        public string fusionKccCandidate;
        public string fishNetCandidate;
        public bool m41GateFound;
        public bool m41NetworkSpikeCandidateReady;
        public bool transportNeutralFoundationOnly = true;
        public bool actualNetworkSdkMeasured;
        public bool actualPredictionMeasured;
        public bool actualReconnectMeasured;
        public bool actualDedicatedServerMeasured;
        public bool actualMovingPaintSurfaceMeasured;
        public bool actualSteamTransportCostMeasured;
        public int positionQuantizationMillimeters;
        public int strokeControlPoints;
        public int requiredStrokeCount;
        public List<PrototypeNetworkSpikeProfileResult> profiles = new();
        public bool allFoundationProfilesPassed;
        public bool adapterComparisonReady;
        public string nextRequiredStep;
    }

    public readonly struct PrototypeNetworkSpikeSnapshot
    {
        public PrototypeNetworkSpikeSnapshot(
            bool running,
            bool m41Ready,
            bool hasReport,
            bool foundationPassed,
            string status,
            string reportPath,
            int passedProfiles,
            int totalProfiles)
        {
            Running = running;
            M41Ready = m41Ready;
            HasReport = hasReport;
            FoundationPassed = foundationPassed;
            Status = status;
            ReportPath = reportPath;
            PassedProfiles = passedProfiles;
            TotalProfiles = totalProfiles;
        }

        public bool Running { get; }
        public bool M41Ready { get; }
        public bool HasReport { get; }
        public bool FoundationPassed { get; }
        public string Status { get; }
        public string ReportPath { get; }
        public int PassedProfiles { get; }
        public int TotalProfiles { get; }
    }
}
