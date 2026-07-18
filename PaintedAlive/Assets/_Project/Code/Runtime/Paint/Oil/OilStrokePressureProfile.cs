using System;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [Serializable]
    public struct OilStrokePressureProfile
    {
        [SerializeField] private float averageDrawSpeed;
        [SerializeField] private float pressureNormalized;
        [SerializeField] private float widthMultiplier;
        [SerializeField] private float heightMultiplier;
        [SerializeField] private float pigmentMultiplier;
        [SerializeField] private float cutResistanceMultiplier;
        [SerializeField] private float lifecycleDurationMultiplier;
        [SerializeField] private float budgetMultiplier;

        public float AverageDrawSpeed => averageDrawSpeed;
        public float PressureNormalized => pressureNormalized;
        public float WidthMultiplier => widthMultiplier;
        public float HeightMultiplier => heightMultiplier;
        public float PigmentMultiplier => pigmentMultiplier;

        public float CutResistanceMultiplier =>
            cutResistanceMultiplier;

        public float LifecycleDurationMultiplier =>
            lifecycleDurationMultiplier;

        public float BudgetMultiplier => budgetMultiplier;

        public bool IsValid =>
            widthMultiplier > 0f &&
            heightMultiplier > 0f &&
            pigmentMultiplier > 0f &&
            cutResistanceMultiplier > 0f &&
            lifecycleDurationMultiplier > 0f &&
            budgetMultiplier > 0f;

        public OilStrokePressureProfile(
            float drawSpeed,
            float pressure,
            float width,
            float height,
            float pigment,
            float cutResistance,
            float lifecycleDuration,
            float budget)
        {
            averageDrawSpeed = Mathf.Max(0f, drawSpeed);
            pressureNormalized = Mathf.Clamp01(pressure);
            widthMultiplier = Mathf.Max(0.1f, width);
            heightMultiplier = Mathf.Max(0.1f, height);
            pigmentMultiplier = Mathf.Max(0.1f, pigment);
            cutResistanceMultiplier = Mathf.Max(0.1f, cutResistance);
            lifecycleDurationMultiplier = Mathf.Max(0.1f, lifecycleDuration);
            budgetMultiplier = Mathf.Max(0.1f, budget);
        }

        public static OilStrokePressureProfile Balanced =>
            new(
                2.5f,
                0.5f,
                1f,
                1f,
                1f,
                1f,
                1f,
                1f);
    }
}
