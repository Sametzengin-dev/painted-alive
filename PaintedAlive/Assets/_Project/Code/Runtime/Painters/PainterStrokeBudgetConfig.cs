using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Painters
{
    [CreateAssetMenu(
        fileName = "PainterStrokeBudgetConfig",
        menuName = "Painted Alive/Painters/Stroke Budget Config")]
    public sealed class PainterStrokeBudgetConfig : ScriptableObject
    {
        [Header("Pressure Capacity")]
        [SerializeField, Min(1f)]
        private float maximumPressure = 12f;

        [SerializeField, Range(1, 10)]
        private int maximumActiveStrokes = 3;

        [Header("Active Stroke Pressure")]
        [SerializeField, Min(0f)]
        private float wallPressure = 4f;

        [SerializeField, Min(0f)]
        private float rampPressure = 3.5f;

        [Header("Dry Stroke Pressure")]
        [SerializeField, Min(0f)]
        private float dryStrokePressure = 0.5f;

        [Header("Telegraph Duration")]
        [SerializeField, Min(0f)]
        private float wallTelegraphDuration = 1.4f;

        [SerializeField, Min(0f)]
        private float rampTelegraphDuration = 0.9f;

        [Header("Pigment Surcharge")]
        [SerializeField, Min(0f)]
        private float wallPigmentSurcharge = 9f;

        [SerializeField, Min(0f)]
        private float rampPigmentSurcharge = 7f;

        [Header("Global Stroke Cooldown")]
        [SerializeField, Min(0f)]
        private float strokeCooldown = 0.35f;

        public float MaximumPressure => maximumPressure;
        public int MaximumActiveStrokes => maximumActiveStrokes;
        public float DryStrokePressure => dryStrokePressure;
        public float StrokeCooldown => strokeCooldown;

        public float GetActivePressure(OilStrokeShape shape)
        {
            return shape switch
            {
                OilStrokeShape.Wall => wallPressure,
                OilStrokeShape.Ramp => rampPressure,
                _ => wallPressure
            };
        }

        public float GetTelegraphDuration(OilStrokeShape shape)
        {
            return shape switch
            {
                OilStrokeShape.Wall => wallTelegraphDuration,
                OilStrokeShape.Ramp => rampTelegraphDuration,
                _ => wallTelegraphDuration
            };
        }

        public float GetPigmentSurcharge(OilStrokeShape shape)
        {
            return shape switch
            {
                OilStrokeShape.Wall => wallPigmentSurcharge,
                OilStrokeShape.Ramp => rampPigmentSurcharge,
                _ => wallPigmentSurcharge
            };
        }

        private void OnValidate()
        {
            maximumPressure = Mathf.Max(1f, maximumPressure);
            maximumActiveStrokes = Mathf.Max(1, maximumActiveStrokes);

            wallPressure = Mathf.Max(0f, wallPressure);
            rampPressure = Mathf.Max(0f, rampPressure);
            dryStrokePressure = Mathf.Max(0f, dryStrokePressure);

            wallTelegraphDuration =
                Mathf.Max(0f, wallTelegraphDuration);

            rampTelegraphDuration =
                Mathf.Max(0f, rampTelegraphDuration);

            wallPigmentSurcharge =
                Mathf.Max(0f, wallPigmentSurcharge);

            rampPigmentSurcharge =
                Mathf.Max(0f, rampPigmentSurcharge);

            strokeCooldown = Mathf.Max(0f, strokeCooldown);
        }
    }
}
