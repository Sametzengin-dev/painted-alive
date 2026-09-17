using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CanvasBacksideRegion : MonoBehaviour,
        IPalimpsestPersistentState,
        IPalimpsestResettable
    {
        [SerializeField] private string systemId = "BACKSIDE_WORKS";
        [SerializeField] private BoxCollider regionTrigger;
        [SerializeField] private Transform entry;
        [SerializeField] private Transform exit;
        [SerializeField] private CanvasBacksideTraceSystem traceSystem;
        [SerializeField] private PalimpsestNetworkState networkState;
        [SerializeField] private int occupancyCount;

        private readonly HashSet<FigureMotor> occupants =
            new HashSet<FigureMotor>();

        public string PalimpsestSystemId => systemId;
        public string PersistentStateName =>
            occupancyCount > 0 ? "BACKSIDE_OCCUPIED" : "FRONT";
        public float PersistentProgress01 => occupancyCount > 0 ? 1f : 0f;
        public int OccupancyCount => occupancyCount;

        public void Configure(
            string id,
            BoxCollider trigger,
            Transform entryReference,
            Transform exitReference,
            CanvasBacksideTraceSystem trace,
            PalimpsestNetworkState stateHub)
        {
            systemId = id ?? "BACKSIDE_WORKS";
            regionTrigger = trigger;
            entry = entryReference;
            exit = exitReference;
            traceSystem = trace;
            networkState = stateHub;
            if (regionTrigger != null)
                regionTrigger.isTrigger = true;
        }

        private void Awake()
        {
            if (regionTrigger == null)
                regionTrigger = GetComponent<BoxCollider>();
            if (regionTrigger != null)
                regionTrigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            FigureMotor figure = other != null
                ? other.GetComponentInParent<FigureMotor>()
                : null;
            if (figure == null || !occupants.Add(figure))
                return;

            occupancyCount = occupants.Count;
            traceSystem?.BeginTracking(figure);
            networkState?.PublishState(this);
        }

        private void OnTriggerExit(Collider other)
        {
            FigureMotor figure = other != null
                ? other.GetComponentInParent<FigureMotor>()
                : null;
            if (figure == null || !occupants.Remove(figure))
                return;

            occupancyCount = occupants.Count;
            traceSystem?.StopTracking(figure);
            networkState?.PublishState(this);
        }

        public void ApplyPersistentState(string stateName, float progress01)
        {
            // Occupancy is character-derived authority. A late-join snapshot may
            // expose the coarse occupied/not-occupied state, but it must never
            // fabricate an exact Figure transform.
            occupancyCount = string.Equals(
                stateName,
                "BACKSIDE_OCCUPIED",
                System.StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;
        }

        public void ResetPalimpsestState()
        {
            occupants.Clear();
            occupancyCount = 0;
            traceSystem?.ResetPalimpsestState();
        }
    }
}
