using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Painters
{
    [CreateAssetMenu(
        fileName = "PainterStrokePressureConfig",
        menuName = "Painted Alive/Painters/Stroke Pressure Config")]
    public sealed class PainterStrokePressureConfig : ScriptableObject
    {
        [Header("Draw Speed")]
        [SerializeField, Min(0.05f)]
        private float slowDrawSpeed = 0.75f;

        [SerializeField, Min(0.1f)]
        private float fastDrawSpeed = 6f;

        [SerializeField, Min(0.05f)]
        private float defaultDrawSpeed = 2.5f;

        [Header("Sampling")]
        [SerializeField, Min(0.02f)]
        private float maximumSegmentSampleDuration = 0.75f;

        [Header("Pressure Curves")]
        [SerializeField]
        private AnimationCurve widthByPressure =
            AnimationCurve.Linear(0f, 0.65f, 1f, 1.35f);

        [SerializeField]
        private AnimationCurve heightByPressure =
            AnimationCurve.Linear(0f, 0.70f, 1f, 1.25f);

        [SerializeField]
        private AnimationCurve pigmentByPressure =
            AnimationCurve.Linear(0f, 0.70f, 1f, 1.40f);

        [SerializeField]
        private AnimationCurve cutResistanceByPressure =
            AnimationCurve.Linear(0f, 0.60f, 1f, 1.60f);

        [SerializeField]
        private AnimationCurve lifecycleByPressure =
            AnimationCurve.Linear(0f, 0.75f, 1f, 1.25f);

        [SerializeField]
        private AnimationCurve budgetByPressure =
            AnimationCurve.Linear(0f, 0.80f, 1f, 1.25f);

        public float DefaultDrawSpeed => defaultDrawSpeed;

        public float MaximumSegmentSampleDuration =>
            maximumSegmentSampleDuration;

        public OilStrokePressureProfile Evaluate(
            float averageDrawSpeed)
        {
            averageDrawSpeed =
                Mathf.Max(0f, averageDrawSpeed);

            float speedNormalized = Mathf.InverseLerp(
                slowDrawSpeed,
                fastDrawSpeed,
                averageDrawSpeed);

            // Yavaş çizgi yüksek basınçtır.
            float pressure =
                1f - speedNormalized;

            return new OilStrokePressureProfile(
                averageDrawSpeed,
                pressure,
                EvaluateCurve(widthByPressure, pressure, 1f),
                EvaluateCurve(heightByPressure, pressure, 1f),
                EvaluateCurve(pigmentByPressure, pressure, 1f),
                EvaluateCurve(
                    cutResistanceByPressure,
                    pressure,
                    1f),
                EvaluateCurve(
                    lifecycleByPressure,
                    pressure,
                    1f),
                EvaluateCurve(
                    budgetByPressure,
                    pressure,
                    1f));
        }

        private static float EvaluateCurve(
            AnimationCurve curve,
            float value,
            float fallback)
        {
            return curve != null && curve.length > 0
                ? Mathf.Max(0.1f, curve.Evaluate(value))
                : fallback;
        }

        private void OnValidate()
        {
            slowDrawSpeed =
                Mathf.Max(0.05f, slowDrawSpeed);

            fastDrawSpeed =
                Mathf.Max(
                    slowDrawSpeed + 0.05f,
                    fastDrawSpeed);

            defaultDrawSpeed = Mathf.Clamp(
                defaultDrawSpeed,
                slowDrawSpeed,
                fastDrawSpeed);

            maximumSegmentSampleDuration =
                Mathf.Max(
                    0.02f,
                    maximumSegmentSampleDuration);
        }
    }
}
