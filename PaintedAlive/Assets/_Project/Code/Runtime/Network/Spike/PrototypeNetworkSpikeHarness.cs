using System;
using System.Collections.Generic;
using System.IO;
using PaintedAlive.Core.Playtests.Validation;
using PaintedAlive.Core.Prototypes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PaintedAlive.Network.Spike
{
    [DisallowMultipleComponent]
    public sealed class PrototypeNetworkSpikeHarness : MonoBehaviour
    {
        [SerializeField] private PrototypeMatchController matchController;
        [SerializeField] private PrototypePlaytestAcceptanceGate acceptanceGate;
        [SerializeField] private PrototypeNetworkSpikeConfig config;

        private bool running;
        private string status = "Hazır";
        private string lastReportPath = string.Empty;
        private bool lastFoundationPassed;
        private int passedProfiles;
        private int totalProfiles;

        public bool Running => running;
        public bool M41Ready => acceptanceGate != null && acceptanceGate.NetworkSpikeCandidateReady;
        public string LastReportPath => lastReportPath;
        public bool LastFoundationPassed => lastFoundationPassed;

        public void Configure(
            PrototypeMatchController match,
            PrototypePlaytestAcceptanceGate gate,
            PrototypeNetworkSpikeConfig spikeConfig)
        {
            matchController = match;
            acceptanceGate = gate;
            config = spikeConfig;
        }

        private void Update()
        {
            if (Keyboard.current == null || running)
            {
                return;
            }

            bool control = Keyboard.current.leftCtrlKey.isPressed ||
                           Keyboard.current.rightCtrlKey.isPressed;
            bool shift = Keyboard.current.leftShiftKey.isPressed ||
                         Keyboard.current.rightShiftKey.isPressed;

            if (control && shift && Keyboard.current.nKey.wasPressedThisFrame)
            {
                RunAllProfiles();
            }
        }

        public void RunAllProfiles()
        {
            if (running)
            {
                return;
            }

            if (config == null)
            {
                status = "Config eksik";
                Debug.LogError("[M42] PrototypeNetworkSpikeConfig missing.", this);
                return;
            }

            if (!CanRunWithoutAffectingMatch())
            {
                status = "Maç Running/Countdown iken benchmark çalıştırılmaz";
                Debug.LogWarning(
                    "[M42] Benchmark aktif maç sırasında engellendi. " +
                    "Maçı bitir veya Waiting durumuna dön.",
                    this);
                return;
            }

            running = true;
            status = "Üç profil hesaplanıyor...";

            try
            {
                PrototypeNetworkSpikeReport report = BuildReport();
                SaveReport(report);
                lastFoundationPassed = report.allFoundationProfilesPassed;
                passedProfiles = 0;
                totalProfiles = report.profiles.Count;

                for (int i = 0; i < report.profiles.Count; i++)
                {
                    if (report.profiles[i].foundationProfilePassed)
                    {
                        passedProfiles++;
                    }
                }

                status = report.allFoundationProfilesPassed
                    ? "Ortak komut/ölçüm temeli geçti"
                    : "Temel eşiklerden biri geçmedi";

                Debug.Log(
                    "[M42] NETWORK SPIKE FOUNDATION COMPLETE\n" +
                    $"Profiles={passedProfiles}/{totalProfiles}\n" +
                    $"M41CandidateReady={report.m41NetworkSpikeCandidateReady}\n" +
                    $"FoundationPassed={report.allFoundationProfilesPassed}\n" +
                    $"Report={lastReportPath}\n" +
                    "No network SDK, match authority, movement controller or paint runtime was replaced.",
                    this);
            }
            catch (Exception exception)
            {
                status = "Benchmark başarısız: " + exception.Message;
                Debug.LogException(exception, this);
            }
            finally
            {
                running = false;
            }
        }

        public PrototypeNetworkSpikeSnapshot GetSnapshot()
        {
            return new PrototypeNetworkSpikeSnapshot(
                running,
                M41Ready,
                !string.IsNullOrEmpty(lastReportPath),
                lastFoundationPassed,
                status,
                lastReportPath,
                passedProfiles,
                totalProfiles);
        }

        private bool CanRunWithoutAffectingMatch()
        {
            if (matchController == null)
            {
                return true;
            }

            return matchController.State != PrototypeMatchState.Running &&
                   matchController.State != PrototypeMatchState.Countdown;
        }

        private PrototypeNetworkSpikeReport BuildReport()
        {
            var report = new PrototypeNetworkSpikeReport
            {
                utcCreatedAt = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                activeScene = SceneManager.GetActiveScene().name,
                fusionCandidate = config.FusionCandidate,
                fusionKccCandidate = config.FusionKccCandidate,
                fishNetCandidate = config.FishNetCandidate,
                m41GateFound = acceptanceGate != null,
                m41NetworkSpikeCandidateReady = M41Ready,
                positionQuantizationMillimeters = Mathf.RoundToInt(
                    config.PositionStepMeters * 1000f),
                strokeControlPoints = config.ControlPointsPerStroke,
                requiredStrokeCount = config.StrokeCommandCount,
                nextRequiredStep =
                    "Run the same adapter contract in isolated Fusion and FishNet branches."
            };

            report.profiles.Add(RunProfile(
                PrototypeNetworkSpikeProfileKind.Baseline,
                config.BaselineRttMilliseconds,
                config.DeterministicSeed + 11));
            report.profiles.Add(RunProfile(
                PrototypeNetworkSpikeProfileKind.Rtt100,
                config.StandardRttMilliseconds,
                config.DeterministicSeed + 23));
            report.profiles.Add(RunProfile(
                PrototypeNetworkSpikeProfileKind.Rtt150,
                config.StressRttMilliseconds,
                config.DeterministicSeed + 37));

            report.allFoundationProfilesPassed = true;
            for (int i = 0; i < report.profiles.Count; i++)
            {
                if (!report.profiles[i].foundationProfilePassed)
                {
                    report.allFoundationProfilesPassed = false;
                    break;
                }
            }

            report.adapterComparisonReady =
                report.allFoundationProfilesPassed && report.m41NetworkSpikeCandidateReady;
            return report;
        }

        private PrototypeNetworkSpikeProfileResult RunProfile(
            PrototypeNetworkSpikeProfileKind kind,
            float rttMilliseconds,
            int randomSeed)
        {
            float profileJitter = kind == PrototypeNetworkSpikeProfileKind.Baseline
                ? 0f
                : config.JitterMilliseconds;
            float profileLossPercent = kind == PrototypeNetworkSpikeProfileKind.Baseline
                ? 0f
                : config.PacketLossPercent;

            var result = new PrototypeNetworkSpikeProfileResult
            {
                profile = kind,
                configuredRttMilliseconds = rttMilliseconds,
                configuredJitterMilliseconds = profileJitter,
                configuredPacketLossPercent = profileLossPercent,
                strokeCommandsGenerated = config.StrokeCommandCount,
                strokePacketsSent = config.StrokeCommandCount,
                figureInputCommands = config.FigureInputCommandCount,
                figureInputBytes = config.FigureInputCommandCount * 9,
                figureSnapshots = config.FigureSnapshotCount,
                figureSnapshotBytes = config.FigureSnapshotCount * 20
            };

            var random = new System.Random(randomSeed);
            var deliveries = new List<SimulatedDelivery>(config.StrokeCommandCount);
            var latencies = new List<float>(config.StrokeCommandCount);
            ulong encodedHash = 14695981039346656037UL;
            ulong decodedHash = 14695981039346656037UL;
            float sendIntervalMs = 1000f / config.StrokeCommandsPerSecond;
            float oneWayBaseMs = rttMilliseconds * 0.5f;

            for (int i = 0; i < config.StrokeCommandCount; i++)
            {
                PrototypeNetworkStrokeCommand command =
                    PrototypeNetworkStrokeCommand.CreateDeterministic(
                        i,
                        config.ControlPointsPerStroke,
                        config.DeterministicSeed,
                        config.PositionStepMeters,
                        config.MaximumSurfaceExtentMeters);

                byte[] encoded = PrototypeNetworkCommandCodec.Encode(command);
                result.strokeBytesSent += encoded.Length;
                encodedHash = PrototypeNetworkCommandCodec.AppendFnv1A64(
                    encodedHash,
                    encoded);

                try
                {
                    PrototypeNetworkStrokeCommand decoded =
                        PrototypeNetworkCommandCodec.Decode(encoded);
                    byte[] reencoded = PrototypeNetworkCommandCodec.Encode(decoded);
                    decodedHash = PrototypeNetworkCommandCodec.AppendFnv1A64(
                        decodedHash,
                        reencoded);

                    if (!command.ContentEquals(decoded))
                    {
                        result.deterministicMismatches++;
                    }
                }
                catch
                {
                    result.strokeDecodeFailures++;
                }

                bool dropped = random.NextDouble() < profileLossPercent / 100.0;
                if (dropped)
                {
                    result.strokePacketsDropped++;
                    continue;
                }

                float jitter = Mathf.Lerp(
                    -profileJitter,
                    profileJitter,
                    (float)random.NextDouble());
                float latency = Mathf.Max(0f, oneWayBaseMs + jitter);
                float sendTime = i * sendIntervalMs;

                deliveries.Add(new SimulatedDelivery
                {
                    sequence = i,
                    deliveryTimeMilliseconds = sendTime + latency
                });
                latencies.Add(latency);
            }

            deliveries.Sort((a, b) =>
                a.deliveryTimeMilliseconds.CompareTo(b.deliveryTimeMilliseconds));
            int highestSequence = -1;
            for (int i = 0; i < deliveries.Count; i++)
            {
                if (deliveries[i].sequence < highestSequence)
                {
                    result.reorderedStrokePackets++;
                }
                else
                {
                    highestSequence = deliveries[i].sequence;
                }
            }

            result.strokePacketsDelivered = deliveries.Count;
            result.meanBytesPerStroke = config.StrokeCommandCount > 0
                ? result.strokeBytesSent / (float)config.StrokeCommandCount
                : 0f;
            result.deliveryRatio = result.strokePacketsSent > 0
                ? result.strokePacketsDelivered / (float)result.strokePacketsSent
                : 0f;
            result.meanOneWayLatencyMilliseconds = Mean(latencies);
            result.p95OneWayLatencyMilliseconds = Percentile(latencies, 0.95f);
            result.maximumOneWayLatencyMilliseconds = Maximum(latencies);
            result.encodedHash = encodedHash.ToString("X16");
            result.decodedHash = decodedHash.ToString("X16");
            result.deterministicRoundTripPassed =
                result.strokeDecodeFailures == 0 &&
                result.deterministicMismatches == 0 &&
                result.encodedHash == result.decodedHash;
            result.byteBudgetPassed =
                result.meanBytesPerStroke <= config.MaximumMeanBytesPerStroke;
            result.deliveryGatePassed =
                result.deliveryRatio >= config.MinimumDeliveryRatio;
            result.foundationProfilePassed =
                (!config.RequireDeterministicRoundTrip ||
                 result.deterministicRoundTripPassed) &&
                result.byteBudgetPassed &&
                result.deliveryGatePassed;

            return result;
        }

        private void SaveReport(PrototypeNetworkSpikeReport report)
        {
            string folder = Path.Combine(
                Application.persistentDataPath,
                "PlaytestTelemetry/M42_NetworkSpike");
            Directory.CreateDirectory(folder);

            string fileName =
                $"M42_Foundation_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json";
            lastReportPath = Path.Combine(folder, fileName);
            File.WriteAllText(lastReportPath, JsonUtility.ToJson(report, true));
        }

        private static float Mean(List<float> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            float sum = 0f;
            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum / values.Count;
        }

        private static float Maximum(List<float> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            float maximum = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                maximum = Mathf.Max(maximum, values[i]);
            }

            return maximum;
        }

        private static float Percentile(List<float> values, float percentile)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            var sorted = new List<float>(values);
            sorted.Sort();
            int index = Mathf.Clamp(
                Mathf.CeilToInt(percentile * sorted.Count) - 1,
                0,
                sorted.Count - 1);
            return sorted[index];
        }

        [Serializable]
        private sealed class SimulatedDelivery
        {
            public int sequence;
            public float deliveryTimeMilliseconds;
        }
    }
}
