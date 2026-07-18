using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OilStrokeFragmentFigurePush : MonoBehaviour
    {
        private readonly Dictionary<int, float> nextAllowedImpactTimes =
            new();

        private OilStrokeFragmentInteractionConfig config;
        private Rigidbody body;

        public bool IsInitialized =>
            config != null && body != null;

        public void Initialize(
            OilStrokeFragmentInteractionConfig interactionConfig,
            Rigidbody fragmentBody)
        {
            config = interactionConfig;
            body = fragmentBody != null
                ? fragmentBody
                : GetComponent<Rigidbody>();

            if (config == null || body == null)
            {
                Debug.LogError(
                    $"{nameof(OilStrokeFragmentFigurePush)} on {name} " +
                    "requires a config and Rigidbody.",
                    this);

                enabled = false;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsInitialized || body.isKinematic)
            {
                return;
            }

            FigureMotor motor =
                collision.collider
                    .GetComponentInParent<FigureMotor>();

            if (motor == null)
            {
                return;
            }

            int figureId = motor.GetInstanceID();

            if (nextAllowedImpactTimes.TryGetValue(
                    figureId,
                    out float nextAllowedTime) &&
                Time.time < nextAllowedTime)
            {
                return;
            }

            float relativeSpeed =
                collision.relativeVelocity.magnitude;

            if (relativeSpeed < config.MinimumRelativeSpeed)
            {
                return;
            }

            Vector3 awayFromFragment =
                motor.transform.position -
                body.worldCenterOfMass;

            Vector3 planarDirection =
                Vector3.ProjectOnPlane(
                    awayFromFragment,
                    Vector3.up);

            if (planarDirection.sqrMagnitude < 0.0001f &&
                collision.contactCount > 0)
            {
                planarDirection =
                    Vector3.ProjectOnPlane(
                        -collision.GetContact(0).normal,
                        Vector3.up);
            }

            if (planarDirection.sqrMagnitude < 0.0001f)
            {
                planarDirection =
                    Vector3.ProjectOnPlane(
                        -body.linearVelocity,
                        Vector3.up);
            }

            if (planarDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            planarDirection.Normalize();

            float speedAboveThreshold =
                relativeSpeed -
                config.MinimumRelativeSpeed;

            float massInfluence =
                Mathf.Sqrt(
                    Mathf.Clamp(
                        body.mass,
                        0.1f,
                        Mathf.Max(
                            0.1f,
                            config.MaximumMassInfluence)));

            float planarPushVelocity =
                Mathf.Clamp(
                    speedAboveThreshold *
                    config.PushVelocityPerSpeed *
                    massInfluence,
                    0f,
                    config.MaximumPushVelocity);

            if (planarPushVelocity <= 0f)
            {
                return;
            }

            float upwardVelocity =
                Mathf.Min(
                    config.MaximumUpwardVelocity,
                    planarPushVelocity *
                    config.UpwardVelocityFraction);

            Vector3 velocityChange =
                planarDirection * planarPushVelocity +
                Vector3.up * upwardVelocity;

            motor.AddExternalImpulse(velocityChange);

            body.AddForce(
                -planarDirection *
                planarPushVelocity *
                config.CounterImpulseScale,
                ForceMode.Impulse);

            nextAllowedImpactTimes[figureId] =
                Time.time + config.ContactCooldown;
        }
    }
}
