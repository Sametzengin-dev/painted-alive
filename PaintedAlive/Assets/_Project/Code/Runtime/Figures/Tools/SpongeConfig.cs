using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [CreateAssetMenu(
        fileName = "SpongeConfig",
        menuName = "Painted Alive/Figures/Tools/Sponge Config")]
    public sealed class SpongeConfig : ScriptableObject
    {
        [Header("Reservoir")]
        [SerializeField, Min(1f)]
        private float maximumCapacity = 100f;

        [SerializeField, Range(0.1f, 1f)]
        private float fullMovementMultiplier = 0.72f;

        [SerializeField, Range(0f, 2f)]
        private float colorMixInstabilityScale = 0.65f;

        [Header("Absorption")]
        [SerializeField, Min(0.5f)]
        private float interactionRange = 4.5f;

        [SerializeField, Min(0.01f)]
        private float interactionRadius = 0.32f;

        [SerializeField, Min(0f)]
        private float paintAbsorbRate = 28f;

        [SerializeField, Min(0f)]
        private float clarityRestoreRate = 20f;

        [SerializeField, Range(0f, 1f)]
        private float stainedFigurePaintInstability = 0.15f;

        [Header("Prototype Self Test")]
        [SerializeField]
        private bool allowSelfCleaningForPrototype = true;

        [SerializeField, Range(0.1f, 1f)]
        private float selfCleaningEfficiency = 0.65f;

        [Header("Discharge")]
        [SerializeField, Min(1f)]
        private float dischargeAmountPerUse = 35f;

        [SerializeField, Min(0.5f)]
        private float dischargeRange = 6f;

        [SerializeField, Min(0f)]
        private float dischargeSurfaceOffset = 0.035f;

        [Header("Visual")]
        [SerializeField]
        private Color drySpongeColor =
            new Color(0.95f, 0.72f, 0.14f, 1f);

        [SerializeField]
        private Color clarityPaintColor =
            new Color(0.22f, 0.58f, 0.95f, 1f);

        public float MaximumCapacity => maximumCapacity;
        public float FullMovementMultiplier =>
            fullMovementMultiplier;
        public float ColorMixInstabilityScale =>
            colorMixInstabilityScale;
        public float InteractionRange => interactionRange;
        public float InteractionRadius => interactionRadius;
        public float PaintAbsorbRate => paintAbsorbRate;
        public float ClarityRestoreRate => clarityRestoreRate;
        public float StainedFigurePaintInstability =>
            stainedFigurePaintInstability;
        public bool AllowSelfCleaningForPrototype =>
            allowSelfCleaningForPrototype;
        public float SelfCleaningEfficiency =>
            selfCleaningEfficiency;
        public float DischargeAmountPerUse =>
            dischargeAmountPerUse;
        public float DischargeRange => dischargeRange;
        public float DischargeSurfaceOffset =>
            dischargeSurfaceOffset;
        public Color DrySpongeColor => drySpongeColor;
        public Color ClarityPaintColor => clarityPaintColor;

        private void OnValidate()
        {
            maximumCapacity = Mathf.Max(1f, maximumCapacity);
            fullMovementMultiplier =
                Mathf.Clamp(fullMovementMultiplier, 0.1f, 1f);
            colorMixInstabilityScale =
                Mathf.Clamp(colorMixInstabilityScale, 0f, 2f);
            interactionRange = Mathf.Max(0.5f, interactionRange);
            interactionRadius = Mathf.Max(0.01f, interactionRadius);
            paintAbsorbRate = Mathf.Max(0f, paintAbsorbRate);
            clarityRestoreRate =
                Mathf.Max(0f, clarityRestoreRate);
            stainedFigurePaintInstability =
                Mathf.Clamp01(stainedFigurePaintInstability);
            selfCleaningEfficiency =
                Mathf.Clamp(selfCleaningEfficiency, 0.1f, 1f);
            dischargeAmountPerUse =
                Mathf.Max(1f, dischargeAmountPerUse);
            dischargeRange = Mathf.Max(0.5f, dischargeRange);
            dischargeSurfaceOffset =
                Mathf.Max(0f, dischargeSurfaceOffset);
        }
    }
}
