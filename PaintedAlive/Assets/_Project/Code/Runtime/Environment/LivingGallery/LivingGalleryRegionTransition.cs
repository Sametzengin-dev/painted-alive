using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    public enum LivingGalleryRegionTransitionMode
    {
        IncomingReference = 0,
        CurrentJourneyTerminal = 1,
        FutureRegion = 2
    }

    /// <summary>
    /// Explicit Unity-side interpretation of the authored ENTRY/EXIT helpers.
    /// It does not load scenes by itself. Existing match/session flow remains the
    /// authority; the current Living Gallery exit is deliberately the end of the
    /// presently implemented journey until a real next-region ID is authored.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LivingGalleryRegionTransition : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private LivingGalleryRegionTransitionMode mode;
        [SerializeField] private string incomingRegionId;
        [SerializeField] private string destinationRegionId;
        [SerializeField] private bool existingJourneyExitOwnsCompletion;

        public string BindingId => bindingId;
        public LivingGalleryRegionTransitionMode Mode => mode;
        public string IncomingRegionId => incomingRegionId;
        public string DestinationRegionId => destinationRegionId;
        public bool IsResolved =>
            mode == LivingGalleryRegionTransitionMode.IncomingReference
                ? !string.IsNullOrEmpty(incomingRegionId)
                : mode == LivingGalleryRegionTransitionMode.CurrentJourneyTerminal
                    ? existingJourneyExitOwnsCompletion
                    : !string.IsNullOrEmpty(destinationRegionId);

        public void ConfigureIncoming(string id, string incomingRegion)
        {
            bindingId = id ?? string.Empty;
            mode = LivingGalleryRegionTransitionMode.IncomingReference;
            incomingRegionId = incomingRegion ?? string.Empty;
            destinationRegionId = string.Empty;
            existingJourneyExitOwnsCompletion = false;
        }

        public void ConfigureCurrentJourneyTerminal(string id)
        {
            bindingId = id ?? string.Empty;
            mode = LivingGalleryRegionTransitionMode.CurrentJourneyTerminal;
            incomingRegionId = string.Empty;
            destinationRegionId = string.Empty;
            existingJourneyExitOwnsCompletion = true;
        }

        public void ConfigureFutureRegion(string id, string destination)
        {
            bindingId = id ?? string.Empty;
            mode = LivingGalleryRegionTransitionMode.FutureRegion;
            incomingRegionId = string.Empty;
            destinationRegionId = destination ?? string.Empty;
            existingJourneyExitOwnsCompletion = false;
        }
    }
}
