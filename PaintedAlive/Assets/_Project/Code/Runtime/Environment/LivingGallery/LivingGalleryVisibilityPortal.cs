using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    /// <summary>
    /// A non-blocking authoring/runtime portal that forces the linked occupancy
    /// volume to recompute membership at a real opening. The actual HIDDEN state
    /// still comes from PainterVisibilityVolume, so overlapping regions remain
    /// resolved by the existing registry instead of by portal enter/exit order.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LivingGalleryVisibilityPortal : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private PainterVisibilityVolume targetVolume;
        [SerializeField] private BoxCollider portalTrigger;

        public string BindingId => bindingId;
        public PainterVisibilityVolume TargetVolume => targetVolume;

        public void Configure(
            string id,
            PainterVisibilityVolume volume,
            BoxCollider trigger)
        {
            bindingId = id ?? string.Empty;
            targetVolume = volume;
            portalTrigger = trigger;
            if (portalTrigger != null)
                portalTrigger.isTrigger = true;
        }

        private void Awake()
        {
            if (portalTrigger == null)
                portalTrigger = GetComponent<BoxCollider>();
            if (portalTrigger != null)
                portalTrigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null ||
                other.GetComponentInParent<FigureMotor>() == null)
            {
                return;
            }

            targetVolume?.RefreshOccupancy();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null ||
                other.GetComponentInParent<FigureMotor>() == null)
            {
                return;
            }

            targetVolume?.RefreshOccupancy();
        }
    }
}
