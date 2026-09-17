using System;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class RotatingFrameGateBindingMetadata : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private RotatingFrameGate gate;
        [SerializeField] private Transform openReference;
        [SerializeField] private Transform closedReference;
        [SerializeField] private Transform[] nearbyRoutes = Array.Empty<Transform>();
        [SerializeField] private bool neutralPivotBasis = true;

        public string BindingId => bindingId;
        public Transform OpenReference => openReference;
        public Transform ClosedReference => closedReference;
        public Transform[] NearbyRoutes => nearbyRoutes;
        public bool NeutralPivotBasis => neutralPivotBasis;
        public bool IsResolved => gate != null && openReference != null &&
                                  closedReference != null &&
                                  nearbyRoutes != null && nearbyRoutes.Length > 0;

        public void Configure(
            string id,
            RotatingFrameGate targetGate,
            Transform open,
            Transform closed,
            Transform[] routes)
        {
            bindingId = id ?? string.Empty;
            gate = targetGate;
            openReference = open;
            closedReference = closed;
            nearbyRoutes = routes ?? Array.Empty<Transform>();
            neutralPivotBasis = true;
        }
    }
}
