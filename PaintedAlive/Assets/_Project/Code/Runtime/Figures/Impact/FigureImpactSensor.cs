using System;
using UnityEngine;

namespace PaintedAlive.Figures.Impact
{
    public readonly struct FigureImpactData
    {
        public FigureImpactData(
            float speed,
            Vector3 point,
            Vector3 normal,
            UnityEngine.Object source)
        {
            Speed = speed;
            Point = point;
            Normal = normal;
            Source = source;
        }

        public float Speed { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public UnityEngine.Object Source { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FigureMotor))]
    public sealed class FigureImpactSensor : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float minimumReportedSpeed = 2.5f;

        [SerializeField, Min(0f)]
        private float repeatedImpactCooldown = 0.16f;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float lastImpactSpeed;

        [SerializeField]
        private Vector3 lastImpactPoint;

        private FigureMotor figureMotor;
        private float nextImpactTime;
        private int lastSourceInstanceId;

        public float LastImpactSpeed => lastImpactSpeed;
        public Vector3 LastImpactPoint => lastImpactPoint;

        public event Action<FigureImpactData> ImpactDetected;

        private void Awake()
        {
            figureMotor = GetComponent<FigureMotor>();
        }

        private void OnControllerColliderHit(
            ControllerColliderHit hit)
        {
            if (hit == null || hit.collider == null)
            {
                return;
            }

            Vector3 safeNormal =
                hit.normal.sqrMagnitude > 0.0001f
                    ? hit.normal.normalized
                    : Vector3.up;

            Vector3 figureVelocity =
                figureMotor != null
                    ? figureMotor.Velocity
                    : Vector3.zero;

            float figureIntoSurfaceSpeed =
                Mathf.Max(
                    0f,
                    -Vector3.Dot(
                        figureVelocity,
                        safeNormal));

            float incomingBodySpeed = 0f;

            if (hit.rigidbody != null)
            {
                Vector3 relativeVelocity =
                    hit.rigidbody.linearVelocity -
                    figureVelocity;

                incomingBodySpeed =
                    Mathf.Max(
                        0f,
                        Vector3.Dot(
                            relativeVelocity,
                            safeNormal));
            }

            float impactSpeed =
                Mathf.Max(
                    figureIntoSurfaceSpeed,
                    incomingBodySpeed);

            ReportImpact(
                impactSpeed,
                hit.point,
                safeNormal,
                hit.collider);
        }

        public void ReportImpact(
            float speed,
            Vector3 point,
            Vector3 normal,
            UnityEngine.Object source = null)
        {
            float safeSpeed = Mathf.Max(0f, speed);

            if (safeSpeed < minimumReportedSpeed)
            {
                return;
            }

            int sourceInstanceId =
                source != null
                    ? source.GetInstanceID()
                    : 0;

            if (Time.time < nextImpactTime &&
                sourceInstanceId == lastSourceInstanceId)
            {
                return;
            }

            Vector3 safeNormal =
                normal.sqrMagnitude > 0.0001f
                    ? normal.normalized
                    : Vector3.up;

            lastImpactSpeed = safeSpeed;
            lastImpactPoint = point;
            lastSourceInstanceId = sourceInstanceId;
            nextImpactTime =
                Time.time + repeatedImpactCooldown;

            ImpactDetected?.Invoke(
                new FigureImpactData(
                    safeSpeed,
                    point,
                    safeNormal,
                    source));
        }

        [ContextMenu("Debug/Report Hard Impact")]
        private void DebugReportHardImpact()
        {
            ReportImpact(
                12f,
                transform.position + Vector3.up,
                Vector3.up,
                this);
        }

        private void OnValidate()
        {
            minimumReportedSpeed =
                Mathf.Max(0f, minimumReportedSpeed);
            repeatedImpactCooldown =
                Mathf.Max(0f, repeatedImpactCooldown);
        }
    }
}
