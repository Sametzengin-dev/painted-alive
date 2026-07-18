using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Painters
{
    [DisallowMultipleComponent]
    public sealed class PainterStrokePressureTracker : MonoBehaviour
    {
        [SerializeField]
        private PainterStrokePressureConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField] private float accumulatedDistance;
        [SerializeField] private float accumulatedMotionTime;
        [SerializeField] private float averageDrawSpeed;
        [SerializeField] private OilStrokePressureProfile currentProfile;

        private Vector3 previousPoint;
        private float previousPointTime;
        private bool isTracking;

        public bool IsTracking => isTracking;
        public float AverageDrawSpeed => averageDrawSpeed;

        public OilStrokePressureProfile CurrentProfile =>
            currentProfile.IsValid
                ? currentProfile
                : OilStrokePressureProfile.Balanced;

        public float PressureNormalized =>
            CurrentProfile.PressureNormalized;

        private void Awake()
        {
            ResetTracking();
        }

        public void BeginTracking(Vector3 startPoint)
        {
            isTracking = true;

            accumulatedDistance = 0f;
            accumulatedMotionTime = 0f;

            previousPoint = startPoint;
            previousPointTime = Time.unscaledTime;

            averageDrawSpeed =
                config != null
                    ? config.DefaultDrawSpeed
                    : OilStrokePressureProfile
                        .Balanced
                        .AverageDrawSpeed;

            currentProfile =
                config != null
                    ? config.Evaluate(averageDrawSpeed)
                    : OilStrokePressureProfile.Balanced;
        }

        public void RecordPoint(Vector3 point)
        {
            if (!isTracking)
            {
                BeginTracking(point);
                return;
            }

            float distance =
                Vector3.Distance(
                    previousPoint,
                    point);

            float currentTime =
                Time.unscaledTime;

            float elapsed =
                currentTime - previousPointTime;

            if (distance > 0.0001f)
            {
                float maximumDuration =
                    config != null
                        ? config.MaximumSegmentSampleDuration
                        : 0.75f;

                elapsed = Mathf.Clamp(
                    elapsed,
                    0.01f,
                    maximumDuration);

                accumulatedDistance += distance;
                accumulatedMotionTime += elapsed;

                averageDrawSpeed =
                    accumulatedMotionTime > 0f
                        ? accumulatedDistance /
                          accumulatedMotionTime
                        : 0f;

                currentProfile =
                    config != null
                        ? config.Evaluate(
                            averageDrawSpeed)
                        : OilStrokePressureProfile.Balanced;
            }

            previousPoint = point;
            previousPointTime = currentTime;
        }

        public void ResetTracking()
        {
            isTracking = false;

            accumulatedDistance = 0f;
            accumulatedMotionTime = 0f;
            averageDrawSpeed = 0f;

            currentProfile =
                OilStrokePressureProfile.Balanced;
        }
    }
}
