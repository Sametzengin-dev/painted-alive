using System;
using System.Collections.Generic;
using PaintedAlive.MatchFlow;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    [Serializable]
    public sealed class PalimpsestPersistentStateRecord
    {
        public string systemId;
        public string stateName;
        [Range(0f, 1f)] public float progress01;
        public int revision;
    }

    [Serializable]
    public sealed class PalimpsestPersistentStateSnapshot
    {
        public int worldRevision;
        public List<PalimpsestPersistentStateRecord> records =
            new List<PalimpsestPersistentStateRecord>();
    }

    /// <summary>
    /// M55.9 environment-state seam. The current project does not yet contain
    /// the production multiplayer transport, so this component deliberately
    /// does not add one. It centralizes deterministic Palimpsest state, reset,
    /// snapshot and late-join apply hooks so M56 can bind the existing state to
    /// the chosen network adapter without rewriting the environment mechanics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PalimpsestNetworkState : MonoBehaviour
    {
        [SerializeField] private PrototypeCoreMatchController matchController;
        [SerializeField] private int lastObservedResetCount = -1;
        [SerializeField] private int localWorldRevision;
        [SerializeField] private bool productionTransportBound;
        [SerializeField] private List<PalimpsestPersistentStateRecord> records =
            new List<PalimpsestPersistentStateRecord>();

        public int LocalWorldRevision => localWorldRevision;
        public bool ProductionTransportBound => productionTransportBound;
        public bool LocalPrototypeAuthority => !productionTransportBound;
        public IReadOnlyList<PalimpsestPersistentStateRecord> Records => records;

        private void Start()
        {
            ResolveMatchController();
            if (matchController != null)
                lastObservedResetCount = matchController.ResetCount;
            RefreshSnapshotRecords();
        }

        private void Update()
        {
            ResolveMatchController();
            if (matchController == null)
                return;

            if (lastObservedResetCount < 0)
            {
                lastObservedResetCount = matchController.ResetCount;
                return;
            }

            if (lastObservedResetCount == matchController.ResetCount)
                return;

            lastObservedResetCount = matchController.ResetCount;
            ResetBoundState();
        }

        public void Configure(PrototypeCoreMatchController controller)
        {
            matchController = controller;
        }

        public void SetProductionTransportBound(bool value)
        {
            productionTransportBound = value;
        }

        public void PublishState(IPalimpsestPersistentState source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.PalimpsestSystemId))
                return;

            PalimpsestPersistentStateRecord record =
                records.Find(candidate =>
                    candidate != null &&
                    string.Equals(
                        candidate.systemId,
                        source.PalimpsestSystemId,
                        StringComparison.Ordinal));

            if (record == null)
            {
                record = new PalimpsestPersistentStateRecord
                {
                    systemId = source.PalimpsestSystemId
                };
                records.Add(record);
            }

            record.stateName = source.PersistentStateName ?? string.Empty;
            record.progress01 = Mathf.Clamp01(source.PersistentProgress01);
            record.revision++;
            localWorldRevision++;
        }

        public string CaptureSnapshotJson()
        {
            RefreshSnapshotRecords();
            var snapshot = new PalimpsestPersistentStateSnapshot
            {
                worldRevision = localWorldRevision,
                records = new List<PalimpsestPersistentStateRecord>()
            };

            foreach (PalimpsestPersistentStateRecord source in records)
            {
                if (source == null)
                    continue;

                snapshot.records.Add(new PalimpsestPersistentStateRecord
                {
                    systemId = source.systemId,
                    stateName = source.stateName,
                    progress01 = source.progress01,
                    revision = source.revision
                });
            }

            return JsonUtility.ToJson(snapshot, true);
        }

        public bool ApplySnapshotJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            PalimpsestPersistentStateSnapshot snapshot;
            try
            {
                snapshot = JsonUtility.FromJson<PalimpsestPersistentStateSnapshot>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Palimpsest snapshot parse failed: " + exception.Message,
                    this);
                return false;
            }

            if (snapshot == null || snapshot.records == null)
                return false;

            MonoBehaviour[] behaviours =
                GetComponentsInChildren<MonoBehaviour>(true);

            foreach (PalimpsestPersistentStateRecord record in snapshot.records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.systemId))
                    continue;

                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (!(behaviour is IPalimpsestPersistentState persistent))
                        continue;

                    if (!string.Equals(
                            persistent.PalimpsestSystemId,
                            record.systemId,
                            StringComparison.Ordinal))
                        continue;

                    persistent.ApplyPersistentState(
                        record.stateName,
                        record.progress01);
                    break;
                }
            }

            localWorldRevision = Mathf.Max(localWorldRevision, snapshot.worldRevision);
            RefreshSnapshotRecords();
            return true;
        }

        public void ResetBoundState()
        {
            localWorldRevision++;

            MonoBehaviour[] behaviours =
                GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IPalimpsestResettable resettable)
                    resettable.ResetPalimpsestState();
            }

            RefreshSnapshotRecords();
        }

        private void RefreshSnapshotRecords()
        {
            MonoBehaviour[] behaviours =
                GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (!(behaviour is IPalimpsestPersistentState persistent))
                    continue;

                if (string.IsNullOrWhiteSpace(persistent.PalimpsestSystemId))
                    continue;

                PalimpsestPersistentStateRecord record =
                    records.Find(candidate =>
                        candidate != null &&
                        string.Equals(
                            candidate.systemId,
                            persistent.PalimpsestSystemId,
                            StringComparison.Ordinal));

                if (record == null)
                {
                    record = new PalimpsestPersistentStateRecord
                    {
                        systemId = persistent.PalimpsestSystemId
                    };
                    records.Add(record);
                }

                record.stateName = persistent.PersistentStateName ?? string.Empty;
                record.progress01 = Mathf.Clamp01(persistent.PersistentProgress01);
            }
        }

        private void ResolveMatchController()
        {
            if (matchController != null)
                return;

            PrototypeCoreMatchController[] controllers =
                UnityEngine.Object.FindObjectsByType<PrototypeCoreMatchController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (controllers != null && controllers.Length > 0)
                matchController = controllers[0];
        }
    }
}
