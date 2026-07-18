using UnityEngine;

namespace PaintedAlive.Painters
{
    [CreateAssetMenu(
        fileName = "PainterPaintMoundConfig",
        menuName = "Painted Alive/Painters/Paint Mound Config")]
    public sealed class PainterPaintMoundConfig : ScriptableObject
    {
        [Header("Charge")]
        [SerializeField, Min(0.05f)]
        private float minimumHoldDuration = 0.55f;

        [SerializeField, Min(0.1f)]
        private float maximumChargeDuration = 2f;

        [Header("Dimensions")]
        [SerializeField, Min(0.1f)]
        private float minimumRadius = 0.75f;

        [SerializeField, Min(0.1f)]
        private float maximumRadius = 1.8f;

        [SerializeField, Min(0.1f)]
        private float minimumHeight = 0.45f;

        [SerializeField, Min(0.1f)]
        private float maximumHeight = 1.35f;

        [Header("Pigment")]
        [SerializeField, Min(0f)]
        private float minimumPigmentCost = 12f;

        [SerializeField, Min(0f)]
        private float maximumPigmentCost = 30f;

        [Header("Timing")]
        [SerializeField, Min(0.05f)]
        private float growthDuration = 0.75f;

        [SerializeField, Min(0f)]
        private float wetDuration = 5f;

        [SerializeField, Min(0f)]
        private float dryingDuration = 4f;

        [SerializeField, Min(0f)]
        private float placementCooldown = 2.25f;

        [Header("Limits")]
        [SerializeField, Range(1, 8)]
        private int maximumActiveMounds = 3;

        [SerializeField, Range(1, 20)]
        private int maximumTotalMounds = 8;

        [Header("Surface And Mesh")]
        [SerializeField, Min(0f)]
        private float surfaceOffset = 0.025f;

        [SerializeField, Range(8, 16)]
        private int radialSegments = 16;

        [SerializeField, Range(3, 6)]
        private int verticalRings = 6;

        public float MinimumHoldDuration => minimumHoldDuration;
        public float MaximumChargeDuration => maximumChargeDuration;
        public float GrowthDuration => growthDuration;
        public float WetDuration => wetDuration;
        public float DryingDuration => dryingDuration;
        public float PlacementCooldown => placementCooldown;
        public int MaximumActiveMounds => maximumActiveMounds;
        public int MaximumTotalMounds => maximumTotalMounds;
        public float SurfaceOffset => surfaceOffset;
        public int RadialSegments => radialSegments;
        public int VerticalRings => verticalRings;

        public float GetChargeNormalized(float heldDuration)
        {
            return Mathf.Clamp01(
                heldDuration /
                Mathf.Max(0.1f, maximumChargeDuration));
        }

        public float GetRadius(float chargeNormalized)
        {
            return Mathf.Lerp(
                minimumRadius,
                maximumRadius,
                Mathf.Clamp01(chargeNormalized));
        }

        public float GetHeight(float chargeNormalized)
        {
            return Mathf.Lerp(
                minimumHeight,
                maximumHeight,
                Mathf.Clamp01(chargeNormalized));
        }

        public float GetPigmentCost(float chargeNormalized)
        {
            return Mathf.Lerp(
                minimumPigmentCost,
                maximumPigmentCost,
                Mathf.Clamp01(chargeNormalized));
        }

        private void OnValidate()
        {
            minimumHoldDuration =
                Mathf.Max(0.05f, minimumHoldDuration);

            maximumChargeDuration =
                Mathf.Max(
                    minimumHoldDuration,
                    maximumChargeDuration);

            minimumRadius = Mathf.Max(0.1f, minimumRadius);
            maximumRadius = Mathf.Max(minimumRadius, maximumRadius);
            minimumHeight = Mathf.Max(0.1f, minimumHeight);
            maximumHeight = Mathf.Max(minimumHeight, maximumHeight);

            minimumPigmentCost = Mathf.Max(0f, minimumPigmentCost);
            maximumPigmentCost =
                Mathf.Max(minimumPigmentCost, maximumPigmentCost);

            growthDuration = Mathf.Max(0.05f, growthDuration);
            wetDuration = Mathf.Max(0f, wetDuration);
            dryingDuration = Mathf.Max(0f, dryingDuration);
            placementCooldown = Mathf.Max(0f, placementCooldown);

            maximumActiveMounds = Mathf.Max(1, maximumActiveMounds);
            maximumTotalMounds =
                Mathf.Max(maximumActiveMounds, maximumTotalMounds);
        }
    }
}
