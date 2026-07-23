using UnityEngine;

namespace PaintedAlive.Paint.Ink.Economy
{
    [CreateAssetMenu(
        fileName = "InkPainterEconomyConfig",
        menuName = "Painted Alive/Paint/Ink/Painter Economy Config")]
    public sealed class InkPainterEconomyConfig : ScriptableObject
    {
        [Header("Pigment")]
        [SerializeField, Min(1f)]
        private float pigmentCapacity = 100f;

        [SerializeField, Min(0f)]
        private float startingPigment = 100f;

        [SerializeField, Min(0f)]
        private float regenerationPerSecond = 5f;

        [SerializeField, Min(0f)]
        private float nestPlacementCost = 35f;

        [SerializeField, Min(0f)]
        private float possessionDrainPerSecond = 3f;

        [Header("Complexity")]
        [SerializeField, Range(1, 64)]
        private int maximumComplexity = 16;

        [SerializeField, Range(1, 16)]
        private int nestComplexity = 2;

        [SerializeField, Range(1, 16)]
        private int lekebacakComplexity = 2;

        [SerializeField, Range(0.05f, 1f)]
        private float complexityRefreshInterval = 0.2f;

        [Header("Nest Cast")]
        [SerializeField, Range(0.1f, 2f)]
        private float minimumCastDuration = 0.65f;

        [SerializeField, Range(0.1f, 4f)]
        private float placementCooldown = 1f;

        [SerializeField, Min(2f)]
        private float maximumPlacementDistance = 35f;

        [SerializeField, Range(0f, 65f)]
        private float maximumSurfaceAngle = 52f;

        [SerializeField, Range(0.25f, 3f)]
        private float protectedFigureRadius = 1.15f;

        [SerializeField, Range(0.5f, 8f)]
        private float minimumNestSpacing = 2.4f;

        [SerializeField, Range(0.2f, 2f)]
        private float previewRadius = 0.9f;

        public float PigmentCapacity => pigmentCapacity;
        public float StartingPigment => startingPigment;
        public float RegenerationPerSecond => regenerationPerSecond;
        public float NestPlacementCost => nestPlacementCost;
        public float PossessionDrainPerSecond => possessionDrainPerSecond;
        public int MaximumComplexity => maximumComplexity;
        public int NestComplexity => nestComplexity;
        public int LekebacakComplexity => lekebacakComplexity;
        public float ComplexityRefreshInterval => complexityRefreshInterval;
        public float MinimumCastDuration => minimumCastDuration;
        public float PlacementCooldown => placementCooldown;
        public float MaximumPlacementDistance => maximumPlacementDistance;
        public float MaximumSurfaceAngle => maximumSurfaceAngle;
        public float ProtectedFigureRadius => protectedFigureRadius;
        public float MinimumNestSpacing => minimumNestSpacing;
        public float PreviewRadius => previewRadius;

        private void OnValidate()
        {
            pigmentCapacity = Mathf.Max(1f, pigmentCapacity);
            startingPigment = Mathf.Clamp(
                startingPigment,
                0f,
                pigmentCapacity);
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            nestPlacementCost = Mathf.Clamp(
                nestPlacementCost,
                0f,
                pigmentCapacity);
            possessionDrainPerSecond = Mathf.Max(
                0f,
                possessionDrainPerSecond);
            maximumComplexity = Mathf.Clamp(maximumComplexity, 1, 64);
            nestComplexity = Mathf.Clamp(
                nestComplexity,
                1,
                maximumComplexity);
            lekebacakComplexity = Mathf.Clamp(
                lekebacakComplexity,
                1,
                maximumComplexity);
            complexityRefreshInterval = Mathf.Clamp(
                complexityRefreshInterval,
                0.05f,
                1f);
            minimumCastDuration = Mathf.Clamp(
                minimumCastDuration,
                0.1f,
                2f);
            placementCooldown = Mathf.Clamp(placementCooldown, 0.1f, 4f);
            maximumPlacementDistance = Mathf.Max(
                2f,
                maximumPlacementDistance);
            maximumSurfaceAngle = Mathf.Clamp(
                maximumSurfaceAngle,
                0f,
                65f);
            protectedFigureRadius = Mathf.Clamp(
                protectedFigureRadius,
                0.25f,
                3f);
            minimumNestSpacing = Mathf.Clamp(
                minimumNestSpacing,
                0.5f,
                8f);
            previewRadius = Mathf.Clamp(previewRadius, 0.2f, 2f);
        }
    }
}
