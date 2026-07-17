using System;
using UnityEngine;

namespace PaintedAlive.Figures
{
    public sealed class FigureCleanPigmentInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumPigment = 3;
        [SerializeField, Min(0)] private int startingPigment = 2;

        [Header("Runtime - Read Only")]
        [SerializeField] private int currentPigment;

        public event Action<int, int> PigmentChanged;

        public int CurrentPigment => currentPigment;
        public int MaximumPigment => maximumPigment;
        public bool HasPigment => currentPigment > 0;

        private void Awake()
        {
            ResetInventory();
        }

        public bool TryConsume(int amount)
        {
            if (amount <= 0 || currentPigment < amount)
                return false;

            int previous = currentPigment;
            currentPigment -= amount;

            PigmentChanged?.Invoke(previous, currentPigment);
            return true;
        }

        public int AddPigment(int amount)
        {
            if (amount <= 0)
                return 0;

            int previous = currentPigment;

            currentPigment = Mathf.Min(
                maximumPigment,
                currentPigment + amount);

            int addedAmount = currentPigment - previous;

            if (addedAmount > 0)
                PigmentChanged?.Invoke(previous, currentPigment);

            return addedAmount;
        }

        public void ResetInventory()
        {
            int previous = currentPigment;

            currentPigment = Mathf.Clamp(
                startingPigment,
                0,
                maximumPigment);

            PigmentChanged?.Invoke(previous, currentPigment);
        }

        [ContextMenu("Debug/Add Clean Pigment")]
        private void DebugAddPigment()
        {
            AddPigment(1);
        }
    }
}