using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class MovingCanvasObstacle : MonoBehaviour, ILivingGalleryResettable
    {
        [SerializeField] private string bindingId;
        [SerializeField] private Transform visual;
        [SerializeField] private Transform pivot;
        [SerializeField, Min(0f)] private float maxSwingDegrees = 10f;
        [SerializeField, Min(1f)] private float swingSpeedDegreesPerSecond = 25f;
        [SerializeField, Range(-1f, 1f)] private float targetNormalized;
        [SerializeField] private PainterPartialVisibilitySurface partialVisibility;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float currentAngle;
        private bool initialized;
        private int painterCycleIndex;

        public float CurrentAngleDegrees => currentAngle;
        public float CurrentNormalized =>
            maxSwingDegrees <= 0.0001f
                ? 0f
                : Mathf.Clamp(currentAngle / maxSwingDegrees, -1f, 1f);
        public float SwingSpeedDegreesPerSecond => swingSpeedDegreesPerSecond;
        public string BindingId => bindingId;
        public bool IsConfigured => visual != null && pivot != null;
        public bool IsMoving =>
            Mathf.Abs(currentAngle - targetNormalized * maxSwingDegrees) > 0.05f;
        public string StateLabel
        {
            get
            {
                if (IsMoving)
                    return "SWINGING";
                if (targetNormalized > 0.25f)
                    return "RIGHT";
                if (targetNormalized < -0.25f)
                    return "LEFT";
                return "CENTER";
            }
        }

        public void Configure(
            string id,
            Transform visualTransform,
            Transform pivotTransform,
            float maxSwing,
            PainterPartialVisibilitySurface partial)
        {
            bindingId = id ?? string.Empty;
            visual = visualTransform;
            pivot = pivotTransform;
            maxSwingDegrees = Mathf.Max(0f, maxSwing);
            partialVisibility = partial;
            CaptureInitial();
        }

        public void ConfigureMotion(float degreesPerSecond)
        {
            swingSpeedDegreesPerSecond = Mathf.Max(1f, degreesPerSecond);
        }

        private void Awake() => CaptureInitial();

        private void CaptureInitial()
        {
            if (initialized || visual == null)
                return;
            initialPosition = visual.position;
            initialRotation = visual.rotation;
            initialized = true;
        }

        public void SetTargetNormalized(float normalized)
        {
            targetNormalized = Mathf.Clamp(normalized, -1f, 1f);
        }

        public bool CanPainterCycle(out string reason)
        {
            reason = string.Empty;

            if (!IsConfigured)
            {
                reason = "Moving Canvas binding incomplete";
                return false;
            }

            if (IsMoving)
            {
                reason = "Moving Canvas is already swinging";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Cycles CENTER -> RIGHT -> LEFT -> CENTER. The handoff authors a
        /// 10 degree limit but no command vocabulary; this conservative Unity
        /// policy exposes the authored swing without creating a hard collider.
        /// </summary>
        public bool TryPainterCycleAfterExternalTelegraph()
        {
            if (!CanPainterCycle(out _))
                return false;

            painterCycleIndex = (painterCycleIndex + 1) % 3;

            switch (painterCycleIndex)
            {
                case 1:
                    SetTargetNormalized(1f);
                    break;
                case 2:
                    SetTargetNormalized(-1f);
                    break;
                default:
                    SetTargetNormalized(0f);
                    break;
            }

            return true;
        }

        private void Update()
        {
            if (visual == null || pivot == null)
                return;
            float targetAngle = targetNormalized * maxSwingDegrees;
            currentAngle = Mathf.MoveTowards(
                currentAngle,
                targetAngle,
                swingSpeedDegreesPerSecond * Time.deltaTime);

            Quaternion delta = Quaternion.AngleAxis(currentAngle, pivot.right);
            Vector3 offset = initialPosition - pivot.position;
            visual.position = pivot.position + delta * offset;
            visual.rotation = delta * initialRotation;
        }

        public void ResetLivingGalleryState()
        {
            painterCycleIndex = 0;
            targetNormalized = 0f;
            currentAngle = 0f;
            if (visual != null)
                visual.SetPositionAndRotation(initialPosition, initialRotation);
        }
    }
}
