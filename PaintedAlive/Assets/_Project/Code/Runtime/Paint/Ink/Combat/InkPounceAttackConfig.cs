using UnityEngine;

namespace PaintedAlive.Paint.Ink.Combat
{
    [CreateAssetMenu(
        fileName = "InkPounceAttackConfig",
        menuName = "Painted Alive/Paint/Ink/Pounce Attack Config")]
    public sealed class InkPounceAttackConfig : ScriptableObject
    {
        [Header("Attack Rhythm")]
        [SerializeField, Min(0.5f)]
        private float aiAttackRange = 2.15f;

        [SerializeField, Min(0.1f)]
        private float windupDuration = 0.6f;

        [SerializeField, Min(0.1f)]
        private float pounceDuration = 0.28f;

        [SerializeField, Min(0.5f)]
        private float pounceDistance = 2.35f;

        [SerializeField, Min(0.1f)]
        private float hitRecoveryDuration = 0.72f;

        [SerializeField, Min(0.1f)]
        private float missVulnerabilityDuration = 1.2f;

        [SerializeField, Min(0.1f)]
        private float attackCooldown = 2.5f;

        [Header("Hit")]
        [SerializeField, Min(0.1f)]
        private float hitRadius = 0.48f;

        [SerializeField, Min(0f)]
        private float clarityDamage = 7f;

        [SerializeField, Range(1, 5)]
        private int maximumStainStacks = 3;

        [SerializeField, Min(0.5f)]
        private float stainDuration = 5f;

        [Header("Readability")]
        [SerializeField, Range(0f, 0.6f)]
        private float pounceArcHeight = 0.22f;

        [SerializeField, Range(0f, 50f)]
        private float maximumWaterAimError = 28f;

        [SerializeField, Range(0.5f, 1f)]
        private float windupScaleY = 0.7f;

        [SerializeField, Range(1f, 1.5f)]
        private float windupScaleXZ = 1.15f;

        [Header("Footprints")]
        [SerializeField]
        private Material footprintMaterial;

        [SerializeField, Min(0.15f)]
        private float footprintStepDistance = 0.38f;

        [SerializeField, Min(0.25f)]
        private float footprintLifetime = 4.5f;

        [SerializeField, Range(4, 32)]
        private int footprintPoolSize = 16;

        [Header("Physics")]
        [SerializeField]
        private LayerMask collisionMask = Physics.DefaultRaycastLayers;

        [SerializeField]
        private LayerMask groundMask = Physics.DefaultRaycastLayers;

        [SerializeField, Min(0.1f)]
        private float groundProbeHeight = 0.9f;

        [SerializeField, Min(0.2f)]
        private float groundProbeDistance = 1.8f;

        [SerializeField, Min(0f)]
        private float surfaceOffset = 0.08f;

        public float AiAttackRange => aiAttackRange;
        public float WindupDuration => windupDuration;
        public float PounceDuration => pounceDuration;
        public float PounceDistance => pounceDistance;
        public float HitRecoveryDuration => hitRecoveryDuration;
        public float MissVulnerabilityDuration => missVulnerabilityDuration;
        public float AttackCooldown => attackCooldown;
        public float HitRadius => hitRadius;
        public float ClarityDamage => clarityDamage;
        public int MaximumStainStacks => maximumStainStacks;
        public float StainDuration => stainDuration;
        public float PounceArcHeight => pounceArcHeight;
        public float MaximumWaterAimError => maximumWaterAimError;
        public float WindupScaleY => windupScaleY;
        public float WindupScaleXZ => windupScaleXZ;
        public Material FootprintMaterial => footprintMaterial;
        public float FootprintStepDistance => footprintStepDistance;
        public float FootprintLifetime => footprintLifetime;
        public int FootprintPoolSize => footprintPoolSize;
        public LayerMask CollisionMask => collisionMask;
        public LayerMask GroundMask => groundMask;
        public float GroundProbeHeight => groundProbeHeight;
        public float GroundProbeDistance => groundProbeDistance;
        public float SurfaceOffset => surfaceOffset;

        private void OnValidate()
        {
            aiAttackRange = Mathf.Max(0.5f, aiAttackRange);
            windupDuration = Mathf.Max(0.1f, windupDuration);
            pounceDuration = Mathf.Max(0.1f, pounceDuration);
            pounceDistance = Mathf.Max(0.5f, pounceDistance);
            hitRecoveryDuration = Mathf.Max(0.1f, hitRecoveryDuration);
            missVulnerabilityDuration = Mathf.Max(
                0.1f,
                missVulnerabilityDuration);
            attackCooldown = Mathf.Max(0.1f, attackCooldown);
            hitRadius = Mathf.Max(0.1f, hitRadius);
            clarityDamage = Mathf.Max(0f, clarityDamage);
            maximumStainStacks = Mathf.Clamp(maximumStainStacks, 1, 5);
            stainDuration = Mathf.Max(0.5f, stainDuration);
            pounceArcHeight = Mathf.Clamp(pounceArcHeight, 0f, 0.6f);
            maximumWaterAimError = Mathf.Clamp(
                maximumWaterAimError,
                0f,
                50f);
            windupScaleY = Mathf.Clamp(windupScaleY, 0.5f, 1f);
            windupScaleXZ = Mathf.Clamp(windupScaleXZ, 1f, 1.5f);
            footprintStepDistance = Mathf.Max(0.15f, footprintStepDistance);
            footprintLifetime = Mathf.Max(0.25f, footprintLifetime);
            footprintPoolSize = Mathf.Clamp(footprintPoolSize, 4, 32);
            groundProbeHeight = Mathf.Max(0.1f, groundProbeHeight);
            groundProbeDistance = Mathf.Max(0.2f, groundProbeDistance);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
        }
    }
}
