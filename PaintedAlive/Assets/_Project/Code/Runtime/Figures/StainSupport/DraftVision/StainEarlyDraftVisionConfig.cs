using UnityEngine;

namespace PaintedAlive.Figures.StainSupport.DraftVision
{
    [CreateAssetMenu(
        fileName = "StainEarlyDraftVisionConfig",
        menuName = "Painted Alive/Figures/Stain Early Draft Vision Config")]
    public sealed class StainEarlyDraftVisionConfig : ScriptableObject
    {
        [Header("Information Advantage")]
        [SerializeField, Min(0.05f)] private float earlyLeadDuration = 0.65f;
        [SerializeField, Min(1f)] private float maximumVisibleDistance = 24f;
        [SerializeField, Range(1, 6)] private int maximumActiveDrafts = 3;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.20f;

        [Header("Visual Language")]
        [SerializeField] private Material draftMaterial;
        [SerializeField] private Color draftColor = new(0.12f, 0.95f, 0.88f, 0.46f);
        [SerializeField, Min(0.005f)] private float lineWidth = 0.085f;
        [SerializeField, Min(0f)] private float worldLift = 0.025f;
        [SerializeField, Min(0f)] private float pulseFrequency = 5.5f;
        [SerializeField, Range(0f, 1f)] private float minimumPulseAlpha = 0.48f;

        [Header("Relay")]
        [SerializeField, Range(5f, 60f)] private float relayUpdatesPerSecond = 20f;
        [SerializeField, Min(0f)] private float minimumPointChange = 0.01f;

        [Header("Debug")]
        [SerializeField, Min(0.2f)] private float debugDrawDuration = 0.55f;
        [SerializeField, Range(3, 24)] private int debugPointCount = 9;
        [SerializeField, Min(1f)] private float debugDraftLength = 5.5f;

        public float EarlyLeadDuration => earlyLeadDuration;
        public float MaximumVisibleDistance => maximumVisibleDistance;
        public int MaximumActiveDrafts => maximumActiveDrafts;
        public float FadeOutDuration => fadeOutDuration;
        public Material DraftMaterial => draftMaterial;
        public Color DraftColor => draftColor;
        public float LineWidth => lineWidth;
        public float WorldLift => worldLift;
        public float PulseFrequency => pulseFrequency;
        public float MinimumPulseAlpha => minimumPulseAlpha;
        public float RelayPublishInterval => 1f / Mathf.Max(5f, relayUpdatesPerSecond);
        public float MinimumPointChange => minimumPointChange;
        public float DebugDrawDuration => debugDrawDuration;
        public int DebugPointCount => debugPointCount;
        public float DebugDraftLength => debugDraftLength;

        public void SetDraftMaterial(Material material)
        {
            draftMaterial = material;
        }

        private void OnValidate()
        {
            earlyLeadDuration = Mathf.Max(0.05f, earlyLeadDuration);
            maximumVisibleDistance = Mathf.Max(1f, maximumVisibleDistance);
            maximumActiveDrafts = Mathf.Clamp(maximumActiveDrafts, 1, 6);
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
            lineWidth = Mathf.Max(0.005f, lineWidth);
            worldLift = Mathf.Max(0f, worldLift);
            pulseFrequency = Mathf.Max(0f, pulseFrequency);
            minimumPulseAlpha = Mathf.Clamp01(minimumPulseAlpha);
            relayUpdatesPerSecond = Mathf.Clamp(relayUpdatesPerSecond, 5f, 60f);
            minimumPointChange = Mathf.Max(0f, minimumPointChange);
            debugDrawDuration = Mathf.Max(0.2f, debugDrawDuration);
            debugPointCount = Mathf.Clamp(debugPointCount, 3, 24);
            debugDraftLength = Mathf.Max(1f, debugDraftLength);
        }
    }
}
