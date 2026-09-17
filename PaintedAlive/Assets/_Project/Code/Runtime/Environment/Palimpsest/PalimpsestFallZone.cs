using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PalimpsestFallZone : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private BoxCollider triggerVolume;
        [SerializeField] private PalimpsestRespawnCoordinator respawnCoordinator;
        [SerializeField] private PalimpsestRespawnPoint preferredRespawn;

        private readonly HashSet<FigureMotor> processing =
            new HashSet<FigureMotor>();

        public string BindingId => bindingId;
        public PalimpsestRespawnPoint PreferredRespawn => preferredRespawn;

        public void Configure(
            string id,
            BoxCollider volume,
            PalimpsestRespawnCoordinator coordinator,
            PalimpsestRespawnPoint preferred)
        {
            bindingId = id ?? string.Empty;
            triggerVolume = volume;
            respawnCoordinator = coordinator;
            preferredRespawn = preferred;
            if (triggerVolume != null)
                triggerVolume.isTrigger = true;
        }

        private void Awake()
        {
            if (triggerVolume == null)
                triggerVolume = GetComponent<BoxCollider>();
            if (triggerVolume != null)
                triggerVolume.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            FigureMotor figure = other != null
                ? other.GetComponentInParent<FigureMotor>()
                : null;
            if (figure == null || !processing.Add(figure))
                return;

            respawnCoordinator?.TryRespawn(figure, preferredRespawn);
            processing.Remove(figure);
        }
    }
}
