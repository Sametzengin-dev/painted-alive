using UnityEngine;

namespace PaintedAlive.Paint.Watercolor
{
    [CreateAssetMenu(
        fileName = "WatercolorReactionConfig",
        menuName = "Painted Alive/Paint/Watercolor Reaction Config")]
    public sealed class WatercolorReactionConfig : ScriptableObject
    {
        [Header("Oil Paint Reaction")]
        [SerializeField, Min(0f)]
        private float oilDriftSpeed = 0.8f;

        [SerializeField, Min(0f)]
        private float oilThinningPerSecond = 0.24f;

        [SerializeField, Range(0.1f, 1f)]
        private float minimumOilHeightScale = 0.42f;

        [SerializeField, Min(0.1f)]
        private float maximumOilDisplacement = 1.8f;

        [Header("Coordinator")]
        [SerializeField, Min(0.1f)]
        private float discoveryInterval = 0.5f;

        public float OilDriftSpeed => oilDriftSpeed;
        public float OilThinningPerSecond => oilThinningPerSecond;
        public float MinimumOilHeightScale => minimumOilHeightScale;
        public float MaximumOilDisplacement => maximumOilDisplacement;
        public float DiscoveryInterval => discoveryInterval;

        private void OnValidate()
        {
            oilDriftSpeed = Mathf.Max(0f, oilDriftSpeed);
            oilThinningPerSecond =
                Mathf.Max(0f, oilThinningPerSecond);
            minimumOilHeightScale =
                Mathf.Clamp(minimumOilHeightScale, 0.1f, 1f);
            maximumOilDisplacement =
                Mathf.Max(0.1f, maximumOilDisplacement);
            discoveryInterval = Mathf.Max(0.1f, discoveryInterval);
        }
    }
}
