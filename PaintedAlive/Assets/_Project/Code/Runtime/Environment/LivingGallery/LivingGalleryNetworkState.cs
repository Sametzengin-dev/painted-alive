using PaintedAlive.MatchFlow;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    public interface ILivingGalleryResettable
    {
        void ResetLivingGalleryState();
    }

    [DisallowMultipleComponent]
    public sealed class LivingGalleryNetworkState : MonoBehaviour
    {
        [SerializeField] private PrototypeCoreMatchController matchController;
        [SerializeField] private int lastObservedResetCount = -1;
        [SerializeField] private int localWorldRevision;

        public int LocalWorldRevision => localWorldRevision;

        private void Start()
        {
            ResolveMatchController();
            if (matchController != null)
                lastObservedResetCount = matchController.ResetCount;
        }

        private void Update()
        {
            ResolveMatchController();
            if (matchController == null)
                return;
            if (lastObservedResetCount < 0)
            {
                lastObservedResetCount = matchController.ResetCount;
                return;
            }
            if (matchController.ResetCount == lastObservedResetCount)
                return;

            lastObservedResetCount = matchController.ResetCount;
            ResetBoundState();
        }

        private void ResolveMatchController()
        {
            if (matchController != null)
                return;
            PrototypeCoreMatchController[] controllers =
                Object.FindObjectsByType<PrototypeCoreMatchController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (controllers != null && controllers.Length > 0)
                matchController = controllers[0];
        }

        public void ResetBoundState()
        {
            localWorldRevision++;
            PainterVisibilityRegistry.ClearAll();

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is ILivingGalleryResettable resettable)
                    resettable.ResetLivingGalleryState();
            }

            PainterVisibilityVolume[] volumes = GetComponentsInChildren<PainterVisibilityVolume>(true);
            foreach (PainterVisibilityVolume volume in volumes)
            {
                if (volume != null && volume.isActiveAndEnabled)
                    volume.RefreshOccupancy();
            }
        }
    }
}
