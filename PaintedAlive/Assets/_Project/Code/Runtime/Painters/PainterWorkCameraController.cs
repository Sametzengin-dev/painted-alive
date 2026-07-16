using UnityEngine;

namespace PaintedAlive.Painters
{
    [DefaultExecutionOrder(50)]
    public sealed class PainterWorkCameraController : MonoBehaviour
    {
        [SerializeField] private Transform figureTarget;

        [Header("Working Area")]
        [SerializeField] private Vector3 cameraOffset =
            new(0f, 14f, -10f);

        [SerializeField] private Vector3 routeDirection =
            Vector3.forward;

        [SerializeField, Min(0f)] private float lookAhead = 9f;
        [SerializeField, Min(0f)] private float targetHeight = 0.5f;

        [Header("Smoothing")]
        [SerializeField, Min(0.01f)]
        private float positionSmoothTime = 0.15f;

        [SerializeField, Min(0f)]
        private float rotationSharpness = 15f;

        private Vector3 positionVelocity;

        private void OnEnable()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (figureTarget == null)
            {
                return;
            }

            Vector3 direction =
                routeDirection.sqrMagnitude > 0.001f
                    ? routeDirection.normalized
                    : Vector3.forward;

            Vector3 focusPoint =
                figureTarget.position +
                direction * lookAhead +
                Vector3.up * targetHeight;

            Vector3 desiredPosition =
                figureTarget.position +
                cameraOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);

            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    focusPoint - transform.position,
                    Vector3.up);

            float rotationT =
                1f - Mathf.Exp(
                    -rotationSharpness *
                    Time.deltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationT);
        }

        private void SnapToTarget()
        {
            if (figureTarget == null)
            {
                return;
            }

            positionVelocity = Vector3.zero;
            transform.position =
                figureTarget.position + cameraOffset;

            Vector3 direction =
                routeDirection.sqrMagnitude > 0.001f
                    ? routeDirection.normalized
                    : Vector3.forward;

            Vector3 focusPoint =
                figureTarget.position +
                direction * lookAhead +
                Vector3.up * targetHeight;

            transform.rotation = Quaternion.LookRotation(
                focusPoint - transform.position,
                Vector3.up);
        }
    }
}
