using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    public sealed class OilStrokeFractureFeedback : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private OilStrokeStructuralIntegritySystem integritySystem;

        [SerializeField]
        private OilPaintFeedbackService feedbackService;

        private void Awake()
        {
            if (integritySystem == null)
            {
                integritySystem =
                    GetComponent<
                        OilStrokeStructuralIntegritySystem>();
            }

            if (feedbackService == null)
            {
                feedbackService =
                    GetComponent<OilPaintFeedbackService>();
            }
        }

        private void OnEnable()
        {
            if (integritySystem != null)
            {
                integritySystem.StrokeFractured +=
                    HandleStrokeFractured;
            }
        }

        private void OnDisable()
        {
            if (integritySystem != null)
            {
                integritySystem.StrokeFractured -=
                    HandleStrokeFractured;
            }
        }

        private void HandleStrokeFractured(
            OilStrokeRuntime stroke,
            int fragmentCount)
        {
            if (stroke == null || feedbackService == null)
            {
                return;
            }

            Renderer sourceRenderer =
                stroke.GetComponent<Renderer>();

            Vector3 position = stroke.transform.position;
            float effectRadius = 1f;

            if (sourceRenderer != null)
            {
                Bounds bounds = sourceRenderer.bounds;
                position = bounds.center;
                effectRadius =
                    Mathf.Max(0.25f, bounds.extents.magnitude);
            }

            feedbackService.PlayFracture(
                position,
                effectRadius,
                fragmentCount);
        }
    }
}
