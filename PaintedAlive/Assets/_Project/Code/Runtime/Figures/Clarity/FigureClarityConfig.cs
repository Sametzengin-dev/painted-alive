using UnityEngine;

namespace PaintedAlive.Figures
{
    [CreateAssetMenu(
        fileName = "FigureClarityConfig",
        menuName = "Painted Alive/Figure/Clarity Config")]
    public sealed class FigureClarityConfig : ScriptableObject
    {
        [Header("Clarity")]
        [SerializeField, Min(1f)] private float maximumClarity = 100f;

        [Header("Level Thresholds")]
        [SerializeField, Range(0f, 1f)] private float stainedThreshold = 0.80f;
        [SerializeField, Range(0f, 1f)] private float distortedThreshold = 0.55f;
        [SerializeField, Range(0f, 1f)] private float dissolvingThreshold = 0.30f;

        [Header("Paint Exposure Per Second")]
        [SerializeField, Min(0f)] private float wetPaintExposure = 12f;
        [SerializeField, Min(0f)] private float dryingPaintExposure = 4f;

        [Header("Movement Multipliers")]
        [SerializeField, Range(0f, 1f)] private float cleanMovement = 1f;
        [SerializeField, Range(0f, 1f)] private float stainedMovement = 0.92f;
        [SerializeField, Range(0f, 1f)] private float distortedMovement = 0.75f;
        [SerializeField, Range(0f, 1f)] private float dissolvingMovement = 0.45f;
        [SerializeField, Range(0f, 1f)] private float stainMovement = 0.35f;

        [Header("Regional Effects")]
        [SerializeField, Range(0f, 1f)] private float minimumLegMovement = 0.80f;
        [SerializeField, Range(0f, 1f)] private float minimumArmEfficiency = 0.70f;

        public float MaximumClarity => maximumClarity;

        public float StainedThreshold => stainedThreshold;
        public float DistortedThreshold => distortedThreshold;
        public float DissolvingThreshold => dissolvingThreshold;

        public float WetPaintExposure => wetPaintExposure;
        public float DryingPaintExposure => dryingPaintExposure;

        public float MinimumLegMovement => minimumLegMovement;
        public float MinimumArmEfficiency => minimumArmEfficiency;

        public float GetMovementMultiplier(FigureClarityLevel level)
        {
            return level switch
            {
                FigureClarityLevel.Clean => cleanMovement,
                FigureClarityLevel.Stained => stainedMovement,
                FigureClarityLevel.Distorted => distortedMovement,
                FigureClarityLevel.Dissolving => dissolvingMovement,
                FigureClarityLevel.Stain => stainMovement,
                _ => 1f
            };
        }
    }
}
