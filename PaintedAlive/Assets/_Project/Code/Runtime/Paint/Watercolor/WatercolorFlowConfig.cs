using UnityEngine;

namespace PaintedAlive.Paint.Watercolor
{
    [CreateAssetMenu(
        fileName = "WatercolorFlowConfig",
        menuName = "Painted Alive/Paint/Watercolor Flow Config")]
    public sealed class WatercolorFlowConfig : ScriptableObject
    {
        [Header("Flow Geometry")]
        [SerializeField, Min(1f)]
        private float maximumFlowLength = 9f;

        [SerializeField, Range(0.2f, 1f)]
        private float nodeSpacing = 0.45f;

        [SerializeField, Min(0.1f)]
        private float growthSpeed = 1.5f;

        [SerializeField, Min(0.25f)]
        private float surfaceWidth = 2.2f;

        [SerializeField, Range(0f, 1f)]
        private float slopeSteering = 0.82f;

        [SerializeField, Range(4, 64)]
        private int maximumNodeCount = 28;

        [Header("Surface Probe")]
        [SerializeField, Min(0.1f)]
        private float probeHeight = 1.4f;

        [SerializeField, Min(0.2f)]
        private float probeDistance = 3.2f;

        [SerializeField, Min(0f)]
        private float surfaceOffset = 0.025f;

        [Header("Paint Storage")]
        [SerializeField, Min(1f)]
        private float initialAmount = 100f;

        [SerializeField, Range(0f, 1f)]
        private float absorptionInstability = 0.08f;

        [Header("Gameplay")]
        [SerializeField, Min(0f)]
        private float figureFlowAcceleration = 7.5f;

        [SerializeField, Min(0f)]
        private float rigidbodyFlowAcceleration = 11f;

        [SerializeField, Min(0f)]
        private float clarityExposurePerSecond = 1.8f;

        [SerializeField, Min(0.1f)]
        private float contactHeightTolerance = 0.85f;

        public float MaximumFlowLength => maximumFlowLength;
        public float NodeSpacing => nodeSpacing;
        public float GrowthSpeed => growthSpeed;
        public float SurfaceWidth => surfaceWidth;
        public float SlopeSteering => slopeSteering;
        public int MaximumNodeCount => maximumNodeCount;
        public float ProbeHeight => probeHeight;
        public float ProbeDistance => probeDistance;
        public float SurfaceOffset => surfaceOffset;
        public float InitialAmount => initialAmount;
        public float AbsorptionInstability => absorptionInstability;
        public float FigureFlowAcceleration => figureFlowAcceleration;
        public float RigidbodyFlowAcceleration => rigidbodyFlowAcceleration;
        public float ClarityExposurePerSecond => clarityExposurePerSecond;
        public float ContactHeightTolerance => contactHeightTolerance;

        private void OnValidate()
        {
            maximumFlowLength = Mathf.Max(1f, maximumFlowLength);
            nodeSpacing = Mathf.Clamp(nodeSpacing, 0.2f, 1f);
            growthSpeed = Mathf.Max(0.1f, growthSpeed);
            surfaceWidth = Mathf.Max(0.25f, surfaceWidth);
            slopeSteering = Mathf.Clamp01(slopeSteering);
            maximumNodeCount = Mathf.Clamp(maximumNodeCount, 4, 64);
            probeHeight = Mathf.Max(0.1f, probeHeight);
            probeDistance = Mathf.Max(0.2f, probeDistance);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            initialAmount = Mathf.Max(1f, initialAmount);
            absorptionInstability = Mathf.Clamp01(absorptionInstability);
            figureFlowAcceleration = Mathf.Max(0f, figureFlowAcceleration);
            rigidbodyFlowAcceleration = Mathf.Max(0f, rigidbodyFlowAcceleration);
            clarityExposurePerSecond = Mathf.Max(0f, clarityExposurePerSecond);
            contactHeightTolerance = Mathf.Max(0.1f, contactHeightTolerance);
        }
    }
}
