using System;
using UnityEngine;

namespace PaintedAlive.Painters.Masterpiece
{
    public enum PrototypeMasterpieceVisualPolicyMode
    {
        Original,
        SafeSilhouette,
        ReadyPuppetFallback
    }

    [Serializable]
    public sealed class PrototypeMasterpieceLocalReportRecord
    {
        [SerializeField] private int snapshotHash;
        [SerializeField] private long reportedAtUtcTicks;
        [SerializeField] private string reasonCode;
        [SerializeField]
        private PrototypeMasterpieceVisualPolicyMode modeAtReport;

        public int SnapshotHash => snapshotHash;
        public long ReportedAtUtcTicks => reportedAtUtcTicks;
        public string ReasonCode => reasonCode;
        public PrototypeMasterpieceVisualPolicyMode ModeAtReport =>
            modeAtReport;

        public PrototypeMasterpieceLocalReportRecord(
            int configuredSnapshotHash,
            string configuredReasonCode,
            PrototypeMasterpieceVisualPolicyMode configuredMode)
        {
            snapshotHash = configuredSnapshotHash;
            reportedAtUtcTicks =
                DateTime.UtcNow.Ticks;

            reasonCode =
                string.IsNullOrWhiteSpace(
                    configuredReasonCode)
                    ? "LOCAL_VISUAL_REPORT"
                    : configuredReasonCode;

            modeAtReport = configuredMode;
        }
    }
}
