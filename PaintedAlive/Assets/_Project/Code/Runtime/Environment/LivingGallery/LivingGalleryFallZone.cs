using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LivingGalleryFallZone : MonoBehaviour
    {
        [SerializeField] private string bindingId = "FALL_RESPAWN_UNITY";
        [SerializeField] private LivingGalleryRespawnCoordinator respawnCoordinator;
        [SerializeField] private BoxCollider triggerVolume;

        private readonly HashSet<FigureMotor> processing =
            new HashSet<FigureMotor>();

        public string BindingId => bindingId;
        public BoxCollider TriggerVolume => triggerVolume;

        public void Configure(
            string id,
            LivingGalleryRespawnCoordinator coordinator,
            BoxCollider volume)
        {
            bindingId = id ?? string.Empty;
            respawnCoordinator = coordinator;
            triggerVolume = volume;
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

            respawnCoordinator?.TryRespawn(figure);
            processing.Remove(figure);
        }
    }
}
