using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Figures;
using UnityEngine;
using UnityEngine.Events;

namespace PaintedAlive.Environment.LivingGallery
{
    /// <summary>
    /// Local-prototype respawn adapter that deliberately reuses FigureMotor's
    /// existing Teleport/ResetMotion authority instead of adding a second
    /// movement or Rigidbody path. This is the one Living Gallery respawn entry
    /// point and can later be replaced by the production network character
    /// authority without changing FallZone geometry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LivingGalleryRespawnCoordinator : MonoBehaviour,
        ILivingGalleryResettable
    {
        [SerializeField] private FigureMotor configuredFigure;
        [SerializeField] private Transform respawnPoint;
        [SerializeField, Min(0f)] private float verticalOffset = 0.08f;
        [SerializeField, Min(0f)] private float sameFigureCooldown = 0.35f;
        [SerializeField] private int respawnRevision;
        [SerializeField] private UnityEvent onFigureRespawned = new UnityEvent();

        private float nextAllowedRespawnTime;

        public Transform RespawnPoint => respawnPoint;
        public int RespawnRevision => respawnRevision;
        public bool IsConfigured => respawnPoint != null;

        public void Configure(
            FigureMotor figure,
            Transform point,
            float offset = 0.08f)
        {
            configuredFigure = figure;
            respawnPoint = point;
            verticalOffset = Mathf.Max(0f, offset);
        }

        public bool TryRespawn(FigureMotor figure)
        {
            if (figure == null || respawnPoint == null)
                return false;

            if (configuredFigure != null && figure != configuredFigure)
                return false;

            if (Time.time < nextAllowedRespawnTime)
                return false;

            // Do not move the dormant Figure body while the local player is
            // actively in the Painter role. If role resolution is unavailable,
            // FigureMotor ownership remains the fallback authority.
            if (PrototypeRoleAuthorityResolver.IsPainter(out _))
                return false;

            nextAllowedRespawnTime = Time.time + sameFigureCooldown;
            figure.Teleport(
                respawnPoint.position + Vector3.up * verticalOffset,
                respawnPoint.rotation);

            respawnRevision++;
            onFigureRespawned?.Invoke();
            return true;
        }

        public void ResetLivingGalleryState()
        {
            nextAllowedRespawnTime = 0f;
        }
    }
}
