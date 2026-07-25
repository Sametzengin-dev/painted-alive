using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures.StainRestoration
{
    [DisallowMultipleComponent]
    public sealed class StainCleanPigmentSource :
        MonoBehaviour
    {
        private static readonly List<StainCleanPigmentSource>
            ActiveSourceList =
                new List<StainCleanPigmentSource>();

        [SerializeField]
        private bool infinitePrototypeSupply = true;

        [SerializeField, Min(0)]
        private int remainingCharges = 3;

        [SerializeField]
        private Transform interactionPoint;

        public static IReadOnlyList<StainCleanPigmentSource>
            ActiveSources => ActiveSourceList;
        public Vector3 InteractionPosition =>
            interactionPoint != null
                ? interactionPoint.position
                : transform.position;
        public bool HasPigment =>
            infinitePrototypeSupply || remainingCharges > 0;
        public int RemainingCharges => remainingCharges;

        private void OnEnable()
        {
            if (!ActiveSourceList.Contains(this))
            {
                ActiveSourceList.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveSourceList.Remove(this);
        }

        public void Configure(
            Transform point,
            bool infiniteSupply,
            int charges)
        {
            interactionPoint = point;
            infinitePrototypeSupply = infiniteSupply;
            remainingCharges = Mathf.Max(0, charges);
        }

        public bool TryTakeCharge()
        {
            if (!HasPigment)
            {
                return false;
            }

            if (!infinitePrototypeSupply)
            {
                remainingCharges =
                    Mathf.Max(0, remainingCharges - 1);
            }

            return true;
        }
    }
}
