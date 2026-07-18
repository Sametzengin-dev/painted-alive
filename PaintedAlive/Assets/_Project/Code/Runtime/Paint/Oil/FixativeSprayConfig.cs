using UnityEngine;

namespace PaintedAlive.Paint
{
    [CreateAssetMenu(
        fileName = "FixativeSprayConfig",
        menuName = "Painted Alive/Figures/Tools/Fixative Spray Config")]
    public sealed class FixativeSprayConfig : ScriptableObject
    {
        [Header("Aim")]
        [SerializeField, Min(1f)]
        private float maximumAimDistance = 50f;

        [SerializeField, Min(0f)]
        private float castRadius = 0.18f;

        [SerializeField, Min(0.1f)]
        private float reach = 3f;

        [Header("Application")]
        [SerializeField, Min(0.02f)]
        private float applicationInterval = 0.1f;

        [SerializeField, Min(0.01f)]
        private float saturationPerSecond = 0.65f;

        [SerializeField, Min(0f)]
        private float lifecycleSecondsPerFullSaturation = 12f;

        [SerializeField, Min(1f)]
        private float maximumCutDamageMultiplier = 1.75f;

        public float MaximumAimDistance => maximumAimDistance;
        public float CastRadius => castRadius;
        public float Reach => reach;
        public float ApplicationInterval => applicationInterval;
        public float SaturationPerSecond => saturationPerSecond;
        public float LifecycleSecondsPerFullSaturation =>
            lifecycleSecondsPerFullSaturation;
        public float MaximumCutDamageMultiplier =>
            maximumCutDamageMultiplier;

        public float GetDosePerApplication(
            float toolEfficiency)
        {
            return saturationPerSecond *
                   applicationInterval *
                   Mathf.Max(0f, toolEfficiency);
        }

        public float GetLifecycleAdvance(float dose)
        {
            return Mathf.Max(0f, dose) *
                   lifecycleSecondsPerFullSaturation;
        }

        private void OnValidate()
        {
            maximumAimDistance =
                Mathf.Max(1f, maximumAimDistance);

            castRadius = Mathf.Max(0f, castRadius);
            reach = Mathf.Max(0.1f, reach);

            applicationInterval =
                Mathf.Max(0.02f, applicationInterval);

            saturationPerSecond =
                Mathf.Max(0.01f, saturationPerSecond);

            lifecycleSecondsPerFullSaturation =
                Mathf.Max(
                    0f,
                    lifecycleSecondsPerFullSaturation);

            maximumCutDamageMultiplier =
                Mathf.Max(
                    1f,
                    maximumCutDamageMultiplier);
        }
    }
}
