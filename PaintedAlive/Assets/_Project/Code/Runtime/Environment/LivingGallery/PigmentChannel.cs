using System;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class PigmentChannel : MonoBehaviour
    {
        [SerializeField] private string channelId;
        [SerializeField] private Transform source;
        [SerializeField] private Transform destination;
        [SerializeField] private Transform diverter;
        [SerializeField] private Renderer[] flowRenderers = Array.Empty<Renderer>();
        [SerializeField, Min(0f)] private float telegraphSeconds = 1.5f;
        [SerializeField] private bool flowEnabled = true;
        [SerializeField] private bool routeModifierConfigured;

        public string ChannelId => channelId;
        public bool FlowEnabled => flowEnabled;
        public bool RouteModifierConfigured => routeModifierConfigured;

        public void Configure(
            string id,
            Transform sourceReference,
            Transform destinationReference,
            Transform diverterReference,
            Renderer[] renderers,
            float telegraph)
        {
            channelId = id ?? string.Empty;
            source = sourceReference;
            destination = destinationReference;
            diverter = diverterReference;
            flowRenderers = renderers ?? Array.Empty<Renderer>();
            telegraphSeconds = Mathf.Max(0f, telegraph);
            routeModifierConfigured = false;
            ApplyVisualState();
        }

        public void SetFlowEnabled(bool enabled)
        {
            flowEnabled = enabled;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (flowRenderers == null)
                return;
            foreach (Renderer renderer in flowRenderers)
            {
                if (renderer != null)
                    renderer.enabled = flowEnabled;
            }
        }
    }
}
