using System.Collections;
using UnityEngine;

namespace PaintedAlive.Paint
{
    /// <summary>
    /// Cosmetic observer for completed wall strokes. It follows the real
    /// stroke path after finalization and never creates paint, colliders,
    /// authority, budget cost, or network state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OilStrokeCreationFeedback : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private OilStrokeSystem strokeSystem;

        [SerializeField]
        private OilPaintFeedbackService feedbackService;

        [Header("M58.0E.1 Trace")]
        [SerializeField, Range(2, 16)]
        private int maximumTraceEmissions = 10;

        [SerializeField, Min(0.05f)]
        private float traceDuration = 0.3f;

        public OilStrokeSystem StrokeSystem => strokeSystem;
        public OilPaintFeedbackService FeedbackService => feedbackService;

        public bool IsConfigured =>
            strokeSystem != null &&
            feedbackService != null;

        public void Configure(
            OilStrokeSystem targetStrokeSystem,
            OilPaintFeedbackService targetFeedbackService)
        {
            bool wasActive = isActiveAndEnabled;

            if (wasActive && strokeSystem != null)
            {
                strokeSystem.StrokeFinalized -= HandleStrokeFinalized;
            }

            strokeSystem = targetStrokeSystem;
            feedbackService = targetFeedbackService;

            if (wasActive && strokeSystem != null)
            {
                strokeSystem.StrokeFinalized += HandleStrokeFinalized;
            }
        }

        private void OnEnable()
        {
            if (strokeSystem != null)
            {
                strokeSystem.StrokeFinalized += HandleStrokeFinalized;
            }
        }

        private void OnDisable()
        {
            if (strokeSystem != null)
            {
                strokeSystem.StrokeFinalized -= HandleStrokeFinalized;
            }

            StopAllCoroutines();
        }

        private void HandleStrokeFinalized(OilStrokeRuntime stroke)
        {
            if (stroke == null ||
                stroke.Shape != OilStrokeShape.Wall ||
                feedbackService == null ||
                !feedbackService.HasStrokeCreationParticle)
            {
                return;
            }

            StartCoroutine(PlayTrace(stroke));
        }

        private IEnumerator PlayTrace(OilStrokeRuntime stroke)
        {
            int availablePoints = stroke.ControlPointCount;
            int emissionCount = Mathf.Clamp(
                availablePoints,
                2,
                maximumTraceEmissions);

            float interval = emissionCount > 1
                ? traceDuration / (emissionCount - 1)
                : 0f;

            for (int emission = 0;
                 emission < emissionCount;
                 emission++)
            {
                if (stroke == null)
                {
                    yield break;
                }

                float normalized = emissionCount > 1
                    ? emission / (float)(emissionCount - 1)
                    : 0f;

                int pointIndex = Mathf.RoundToInt(
                    normalized * (availablePoints - 1));

                int neighbourIndex = Mathf.Min(
                    pointIndex + 1,
                    availablePoints - 1);

                if (neighbourIndex == pointIndex)
                {
                    neighbourIndex = Mathf.Max(0, pointIndex - 1);
                }

                if (stroke.TryGetWorldControlPoint(
                        pointIndex,
                        out Vector3 position) &&
                    stroke.TryGetWorldControlPoint(
                        neighbourIndex,
                        out Vector3 neighbour))
                {
                    Vector3 direction = neighbour - position;

                    if (direction.sqrMagnitude < 0.0001f)
                    {
                        direction = Vector3.forward;
                    }

                    feedbackService.PlayStrokeCreation(
                        position,
                        direction.normalized,
                        stroke.VisualWidth);
                }

                if (emission < emissionCount - 1 && interval > 0f)
                {
                    float elapsed = 0f;

                    while (elapsed < interval)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                }
            }
        }

        private void OnValidate()
        {
            maximumTraceEmissions = Mathf.Clamp(
                maximumTraceEmissions,
                2,
                16);

            traceDuration = Mathf.Max(0.05f, traceDuration);
        }
    }
}
