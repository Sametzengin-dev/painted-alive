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

        public float Width => width;
        public float Height => height;
        public float SurfaceOffset => surfaceOffset;
        public float ControlPointSpacing => controlPointSpacing;
        public int SamplesPerSegment => samplesPerSegment;
        public int MaximumControlPoints => maximumControlPoints;

        private void OnValidate()
        {
            width = Mathf.Max(0.05f, width);
            height = Mathf.Max(0.05f, height);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            controlPointSpacing = Mathf.Max(0.02f, controlPointSpacing);
            samplesPerSegment = Mathf.Clamp(samplesPerSegment, 1, 12);
            maximumControlPoints = Mathf.Max(2, maximumControlPoints);
        }
    }
}
