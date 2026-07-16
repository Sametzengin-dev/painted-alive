using System;
using UnityEngine;

namespace PaintedAlive.Painters
{
    [DisallowMultipleComponent]
    public sealed class PainterPigmentReservoir : MonoBehaviour
    {
        [SerializeField] private PainterPigmentConfig config;

        [Header("Runtime Debug")]
        [SerializeField] private float currentPigment;
        [SerializeField] private bool isConsuming;

        public event Action<float, float> PigmentChanged;

        public float Current => currentPigment;

        public float Capacity =>
            config != null ? config.Capacity : 0f;

        public float Normalized =>
            Capacity > 0f ? currentPigment / Capacity : 0f;

        public float StrokeBeginCost =>
            config != null ? config.StrokeBeginCost : 0f;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(PainterPigmentReservoir)} requires a config.",
                    this);

                enabled = false;
                return;
            }

            currentPigment = config.Capacity;
            NotifyChanged();
        }

        private void Update()
        {
            if (isConsuming ||
                currentPigment >= config.Capacity)
            {
                return;
            }

            float previousValue = currentPigment;

            currentPigment = Mathf.Min(
                config.Capacity,
                currentPigment +
                config.RegenerationPerSecond * Time.deltaTime);

            if (!Mathf.Approximately(previousValue, currentPigment))
            {
                NotifyChanged();
            }
        }

        public float CalculateDistanceCost(float distance)
        {
            if (config == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, distance) *
                   config.CostPerMeter;
        }

        public bool CanAfford(float amount)
        {
            return amount <= 0f ||
                   currentPigment + 0.0001f >= amount;
        }

        public bool TrySpend(float amount)
        {
            amount = Mathf.Max(0f, amount);

            if (!CanAfford(amount))
            {
                return false;
            }

            currentPigment = Mathf.Max(
                0f,
                currentPigment - amount);

            NotifyChanged();
            return true;
        }

        public void SetConsuming(bool consuming)
        {
            isConsuming = consuming;
        }

        public void Refill()
        {
            if (config == null)
            {
                return;
            }

            currentPigment = config.Capacity;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            PigmentChanged?.Invoke(
                currentPigment,
                Capacity);
        }
    }
}
