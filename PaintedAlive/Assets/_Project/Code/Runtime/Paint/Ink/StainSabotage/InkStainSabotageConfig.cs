using UnityEngine;

namespace PaintedAlive.Paint.Ink.StainSabotage
{
    [CreateAssetMenu(
        fileName = "InkStainSabotageConfig",
        menuName = "Painted Alive/Paint/Ink/Stain Sabotage Config")]
    public sealed class InkStainSabotageConfig : ScriptableObject
    {
        [Header("Targeting")]
        [SerializeField, Min(0.5f)]
        private float interactionRange = 3.6f;

        [SerializeField, Min(0.02f)]
        private float aimAssistRadius = 0.24f;

        [SerializeField]
        private LayerMask targetMask = Physics.DefaultRaycastLayers;

        [SerializeField, Range(1, 16)]
        private int maximumComplexity = 3;

        [Header("Sabotage")]
        [SerializeField, Min(0.1f)]
        private float holdDuration = 1.15f;

        [SerializeField, Min(0.1f)]
        private float sabotageDuration = 4.25f;

        [SerializeField, Min(0f)]
        private float reuseCooldown = 1.25f;

        [Header("Feedback")]
        [SerializeField]
        private Color lowPulseColor =
            new Color(0.08f, 0.9f, 0.72f, 1f);

        [SerializeField]
        private Color highPulseColor =
            new Color(0.95f, 0.12f, 0.82f, 1f);

        [SerializeField, Min(0.1f)]
        private float pulseSpeed = 9f;

        [SerializeField, Range(0f, 0.2f)]
        private float scalePulse = 0.055f;

        public float InteractionRange => interactionRange;
        public float AimAssistRadius => aimAssistRadius;
        public LayerMask TargetMask => targetMask;
        public int MaximumComplexity => maximumComplexity;
        public float HoldDuration => holdDuration;
        public float SabotageDuration => sabotageDuration;
        public float ReuseCooldown => reuseCooldown;
        public Color LowPulseColor => lowPulseColor;
        public Color HighPulseColor => highPulseColor;
        public float PulseSpeed => pulseSpeed;
        public float ScalePulse => scalePulse;

        private void OnValidate()
        {
            interactionRange = Mathf.Max(0.5f, interactionRange);
            aimAssistRadius = Mathf.Max(0.02f, aimAssistRadius);
            maximumComplexity = Mathf.Clamp(maximumComplexity, 1, 16);
            holdDuration = Mathf.Max(0.1f, holdDuration);
            sabotageDuration = Mathf.Max(0.1f, sabotageDuration);
            reuseCooldown = Mathf.Max(0f, reuseCooldown);
            pulseSpeed = Mathf.Max(0.1f, pulseSpeed);
            scalePulse = Mathf.Clamp(scalePulse, 0f, 0.2f);
        }
    }
}
