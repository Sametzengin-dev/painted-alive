using System;
using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class SpongeReservoir : MonoBehaviour
    {
        [SerializeField]
        private SpongeConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField, Min(0f)]
        private float storedAmount;

        [SerializeField]
        private Color storedColor = Color.clear;

        [SerializeField, Range(0f, 1f)]
        private float mixtureInstability;

        public float StoredAmount => storedAmount;
        public Color StoredColor => storedColor;
        public float MixtureInstability => mixtureInstability;

        public float MaximumCapacity =>
            config != null ? config.MaximumCapacity : 100f;

        public float RemainingCapacity =>
            Mathf.Max(0f, MaximumCapacity - storedAmount);

        public float NormalizedFill =>
            MaximumCapacity > 0f
                ? Mathf.Clamp01(storedAmount / MaximumCapacity)
                : 0f;

        public bool IsEmpty => storedAmount <= 0.001f;
        public bool IsFull => RemainingCapacity <= 0.001f;

        public event Action ReservoirChanged;

        public void Configure(SpongeConfig spongeConfig)
        {
            config = spongeConfig;
            ClampRuntimeState();
            ReservoirChanged?.Invoke();
        }

        public float AddPaint(
            float requestedAmount,
            Color incomingColor,
            float additionalInstability = 0f)
        {
            float accepted =
                Mathf.Min(
                    Mathf.Max(0f, requestedAmount),
                    RemainingCapacity);

            if (accepted <= 0f)
            {
                return 0f;
            }

            Color safeIncomingColor = incomingColor;
            safeIncomingColor.a = 1f;

            float scaledAdditionalInstability =
                Mathf.Max(0f, additionalInstability) *
                (accepted / MaximumCapacity);

            if (storedAmount <= 0.001f)
            {
                storedColor = safeIncomingColor;
            }
            else
            {
                float previousAmount = storedAmount;
                float mixT =
                    accepted / (previousAmount + accepted);

                float colorDifference =
                    CalculateColorDifference(
                        storedColor,
                        safeIncomingColor);

                float mixInstability =
                    colorDifference *
                    (accepted / MaximumCapacity) *
                    (config != null
                        ? config.ColorMixInstabilityScale
                        : 0.65f);

                mixtureInstability =
                    Mathf.Clamp01(
                        mixtureInstability +
                        mixInstability +
                        scaledAdditionalInstability);

                storedColor =
                    Color.Lerp(
                        storedColor,
                        safeIncomingColor,
                        mixT);
            }

            if (storedAmount <= 0.001f)
            {
                mixtureInstability =
                    Mathf.Clamp01(
                        mixtureInstability +
                        scaledAdditionalInstability);
            }

            storedAmount += accepted;
            ClampRuntimeState();
            ReservoirChanged?.Invoke();
            return accepted;
        }

        public float RemovePaint(
            float requestedAmount,
            out Color removedColor,
            out float removedInstability)
        {
            removedColor = storedColor;
            removedInstability = mixtureInstability;

            float removed =
                Mathf.Min(
                    Mathf.Max(0f, requestedAmount),
                    storedAmount);

            if (removed <= 0f)
            {
                return 0f;
            }

            float previousAmount = storedAmount;
            storedAmount -= removed;

            float retainedFraction =
                previousAmount > 0f
                    ? storedAmount / previousAmount
                    : 0f;

            mixtureInstability *= retainedFraction;

            if (storedAmount <= 0.001f)
            {
                storedAmount = 0f;
                storedColor = Color.clear;
                mixtureInstability = 0f;
            }

            ReservoirChanged?.Invoke();
            return removed;
        }

        [ContextMenu("Debug/Empty Sponge")]
        public void EmptyImmediately()
        {
            storedAmount = 0f;
            storedColor = Color.clear;
            mixtureInstability = 0f;
            ReservoirChanged?.Invoke();
        }

        private void OnValidate()
        {
            ClampRuntimeState();
        }

        private void ClampRuntimeState()
        {
            storedAmount =
                Mathf.Clamp(storedAmount, 0f, MaximumCapacity);
            mixtureInstability =
                Mathf.Clamp01(mixtureInstability);
        }

        private static float CalculateColorDifference(
            Color a,
            Color b)
        {
            Vector3 difference =
                new Vector3(a.r - b.r, a.g - b.g, a.b - b.b);

            return Mathf.Clamp01(
                difference.magnitude / Mathf.Sqrt(3f));
        }
    }
}
