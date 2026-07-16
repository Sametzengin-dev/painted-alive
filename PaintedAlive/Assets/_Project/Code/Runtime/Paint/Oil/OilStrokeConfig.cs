using UnityEngine;

namespace PaintedAlive.Paint
{
    [CreateAssetMenu(
        fileName = "OilStrokeConfig",
        menuName = "Painted Alive/Paint/Oil Stroke Config")]
    public sealed class OilStrokeConfig : ScriptableObject
    {
        [Header("Geometry")]
        [SerializeField, Min(0.05f)] private float width = 0.65f;
        [SerializeField, Min(0.05f)] private float height = 1.4f;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.025f;

        [Header("Sampling")]
        [SerializeField, Min(0.02f)]
        private float controlPointSpacing = 0.22f;

        [SerializeField, Range(1, 12)]
        private int samplesPerSegment = 5;

        [SerializeField, Min(2)]
        private int maximumControlPoints = 128;

        [Header("Drying Lifecycle")]
        [SerializeField, Min(0f)] private float wetDuration = 5f;
        [SerializeField, Min(0f)] private float dryingDuration = 3f;

        [Header("Palette Knife Response")]
        [SerializeField, Min(0.1f)]
        private float wetCutMultiplier = 1.15f;

        [SerializeField, Min(0.1f)]
        private float dryingCutMultiplier = 1f;

        [SerializeField, Min(0.1f)]
        private float dryCutMultiplier = 0.8f;

        public float Width => width;
        public float Height => height;
        public float SurfaceOffset => surfaceOffset;
        public float ControlPointSpacing => controlPointSpacing;
        public int SamplesPerSegment => samplesPerSegment;
        public int MaximumControlPoints => maximumControlPoints;
        public float WetDuration => wetDuration;
        public float DryingDuration => dryingDuration;
        public float WetCutMultiplier => wetCutMultiplier;
        public float DryingCutMultiplier => dryingCutMultiplier;
        public float DryCutMultiplier => dryCutMultiplier;

        private void OnValidate()
        {
            width = Mathf.Max(0.05f, width);
            height = Mathf.Max(0.05f, height);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            controlPointSpacing = Mathf.Max(0.02f, controlPointSpacing);
            samplesPerSegment = Mathf.Clamp(samplesPerSegment, 1, 12);
            maximumControlPoints = Mathf.Max(2, maximumControlPoints);
            wetDuration = Mathf.Max(0f, wetDuration);
            dryingDuration = Mathf.Max(0f, dryingDuration);
            wetCutMultiplier = Mathf.Max(0.1f, wetCutMultiplier);
            dryingCutMultiplier = Mathf.Max(0.1f, dryingCutMultiplier);
            dryCutMultiplier = Mathf.Max(0.1f, dryCutMultiplier);
        }
    }
}