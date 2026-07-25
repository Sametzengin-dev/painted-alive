using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    [DisallowMultipleComponent]
    public sealed class StainSpongeCarrier : MonoBehaviour
    {
        private static readonly List<StainSpongeCarrier>
            ActiveCarrierList = new List<StainSpongeCarrier>();

        [Header("References")]
        [SerializeField]
        private FigureClarityState ownerClarity;

        [SerializeField]
        private Transform carrySocket;

        [SerializeField]
        private Renderer carriedStainRenderer;

        [Header("Carrier Type")]
        [SerializeField]
        private bool prototypeCarrier;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private StainSpongeCarryController passenger;

        public static IReadOnlyList<StainSpongeCarrier>
            ActiveCarriers => ActiveCarrierList;
        public FigureClarityState OwnerClarity => ownerClarity;
        public Transform CarrySocket => carrySocket;
        public bool IsPrototypeCarrier => prototypeCarrier;
        public bool HasPassenger => passenger != null;
        public StainSpongeCarryController Passenger => passenger;

        private void OnEnable()
        {
            if (!ActiveCarrierList.Contains(this))
            {
                ActiveCarrierList.Add(this);
            }

            SetPortableVisual(false);
        }

        private void OnDisable()
        {
            ActiveCarrierList.Remove(this);

            if (passenger != null)
            {
                passenger.HandleCarrierUnavailable(this);
            }

            passenger = null;
            SetPortableVisual(false);
        }

        public void Configure(
            FigureClarityState targetOwner,
            Transform targetSocket,
            Renderer targetPortableVisual,
            bool isPrototype)
        {
            ownerClarity = targetOwner;
            carrySocket = targetSocket;
            carriedStainRenderer = targetPortableVisual;
            prototypeCarrier = isPrototype;
            SetPortableVisual(passenger != null);
        }

        public bool CanBoard(
            StainSpongeCarryController candidate)
        {
            return candidate != null &&
                carrySocket != null &&
                passenger == null &&
                (ownerClarity == null ||
                 ownerClarity != candidate.ClarityState);
        }

        public bool TryBoard(
            StainSpongeCarryController candidate)
        {
            if (!CanBoard(candidate))
            {
                return false;
            }

            passenger = candidate;
            SetPortableVisual(true);
            return true;
        }

        public void Release(
            StainSpongeCarryController candidate)
        {
            if (candidate == null || passenger != candidate)
            {
                return;
            }

            passenger = null;
            SetPortableVisual(false);
        }

        private void SetPortableVisual(bool visible)
        {
            if (carriedStainRenderer != null)
            {
                carriedStainRenderer.enabled = visible;
            }
        }
    }
}
