using UnityEngine;

namespace PaintedAlive.Paint
{
    [CreateAssetMenu(
        fileName = "OilStrokeFragmentInteractionConfig",
        menuName = "Painted Alive/Paint/Oil Stroke Fragment Interaction Config")]
    public sealed class OilStrokeFragmentInteractionConfig : ScriptableObject
    {
        [Header("Impact Qualification")]
        [SerializeField, Min(0f)]
        private float minimumRelativeSpeed = 1.4f;

        [SerializeField, Min(0f)]
        private float maximumMassInfluence = 2f;

        [Header("Figure Push")]
        [SerializeField, Min(0f)]
        private float pushVelocityPerSpeed = 0.38f;

        [SerializeField, Min(0f)]
        private float maximumPushVelocity = 3.2f;

        [SerializeField, Range(0f, 1f)]
        private float upwardVelocityFraction = 0.15f;

        [SerializeField, Min(0f)]
        private float maximumUpwardVelocity = 1.2f;

        [SerializeField, Min(0f)]
        private float contactCooldown = 0.35f;

        [Header("Fragment Reaction")]
        [SerializeField, Range(0f, 1f)]
        private float counterImpulseScale = 0.3f;

        public float MinimumRelativeSpeed => minimumRelativeSpeed;
        public float MaximumMassInfluence => maximumMassInfluence;
        public float PushVelocityPerSpeed => pushVelocityPerSpeed;
        public float MaximumPushVelocity => maximumPushVelocity;
        public float UpwardVelocityFraction => upwardVelocityFraction;
        public float MaximumUpwardVelocity => maximumUpwardVelocity;
        public float ContactCooldown => contactCooldown;
        public float CounterImpulseScale => counterImpulseScale;

        private void OnValidate()
        {
            minimumRelativeSpeed =
                Mathf.Max(0f, minimumRelativeSpeed);

            maximumMassInfluence =
                Mathf.Max(0f, maximumMassInfluence);

            pushVelocityPerSpeed =
                Mathf.Max(0f, pushVelocityPerSpeed);

            maximumPushVelocity =
                Mathf.Max(0f, maximumPushVelocity);

            upwardVelocityFraction =
                Mathf.Clamp01(upwardVelocityFraction);

            maximumUpwardVelocity =
                Mathf.Max(0f, maximumUpwardVelocity);

            contactCooldown =
                Mathf.Max(0f, contactCooldown);

            counterImpulseScale =
                Mathf.Clamp01(counterImpulseScale);
        }
    }
}
