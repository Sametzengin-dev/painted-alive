using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PalimpsestRegionVolume : MonoBehaviour
    {
        [SerializeField] private int regionIndex;
        [SerializeField] private BoxCollider triggerVolume;
        [SerializeField] private PalimpsestRespawnCoordinator respawnCoordinator;
        [SerializeField] private PalimpsestRespawnPoint regionalRespawn;

        public int RegionIndex => regionIndex;

        public void Configure(
            int index,
            BoxCollider volume,
            PalimpsestRespawnCoordinator coordinator,
            PalimpsestRespawnPoint point)
        {
            regionIndex = index;
            triggerVolume = volume;
            respawnCoordinator = coordinator;
            regionalRespawn = point;
            if (triggerVolume != null)
                triggerVolume.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            FigureMotor figure = other != null
                ? other.GetComponentInParent<FigureMotor>()
                : null;
            if (figure != null)
                respawnCoordinator?.NotifyRegionEntered(figure, regionalRespawn);
        }
    }
}
