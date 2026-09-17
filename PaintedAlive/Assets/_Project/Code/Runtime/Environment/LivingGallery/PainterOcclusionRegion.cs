using System;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class PainterOcclusionRegion : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private Transform[] opaqueBlockers = Array.Empty<Transform>();

        public string BindingId => bindingId;
        public Transform[] OpaqueBlockers => opaqueBlockers;

        public void Configure(string newBindingId, Transform[] blockers)
        {
            bindingId = newBindingId ?? string.Empty;
            opaqueBlockers = blockers ?? Array.Empty<Transform>();
        }
    }
}
