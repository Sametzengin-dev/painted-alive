using UnityEngine;

namespace PaintedAlive.Paint.Ink.Thief
{
    [CreateAssetMenu(
        fileName = "InkToolThiefConfig",
        menuName = "Painted Alive/Paint/Ink/Tool Thief Config")]
    public sealed class InkToolThiefConfig : ScriptableObject
    {
        [Header("Targeting")]
        [SerializeField, Min(0.5f)]
        private float detectionRange = 8.5f;

        [SerializeField, Range(0.05f, 1f)]
        private float targetRefreshInterval = 0.2f;

        [SerializeField]
        private bool requiresLineOfSight = true;

        [Header("Steal Telegraph")]
        [SerializeField, Min(0.25f)]
        private float stealDistance = 1.15f;

        [SerializeField, Min(0.1f)]
        private float stealWindup = 0.85f;

        [SerializeField, Min(0f)]
        private float stealRetryCooldown = 1.0f;

        [Header("Escape / Drop")]
        [SerializeField, Min(0.5f)]
        private float desiredEscapeDistance = 6f;

        [SerializeField, Min(0.5f)]
        private float dropDistanceFromVictim = 5f;

        [SerializeField, Min(0f)]
        private float minimumCarryDuration = 2f;

        [SerializeField, Min(0.5f)]
        private float maximumCarryDuration = 7f;

        [SerializeField, Min(2f)]
        private float pickupAutoReturnTime = 12f;

        [Header("Sponge Counterplay")]
        [SerializeField, Min(0.05f)]
        private float mouthInkAmount = 0.65f;

        public float DetectionRange => detectionRange;
        public float TargetRefreshInterval => targetRefreshInterval;
        public bool RequiresLineOfSight => requiresLineOfSight;
        public float StealDistance => stealDistance;
        public float StealWindup => stealWindup;
        public float StealRetryCooldown => stealRetryCooldown;
        public float DesiredEscapeDistance => desiredEscapeDistance;
        public float DropDistanceFromVictim => dropDistanceFromVictim;
        public float MinimumCarryDuration => minimumCarryDuration;
        public float MaximumCarryDuration => maximumCarryDuration;
        public float PickupAutoReturnTime => pickupAutoReturnTime;
        public float MouthInkAmount => mouthInkAmount;

        private void OnValidate()
        {
            detectionRange = Mathf.Max(0.5f, detectionRange);
            targetRefreshInterval = Mathf.Clamp(
                targetRefreshInterval,
                0.05f,
                1f);
            stealDistance = Mathf.Max(0.25f, stealDistance);
            stealWindup = Mathf.Max(0.1f, stealWindup);
            stealRetryCooldown = Mathf.Max(0f, stealRetryCooldown);
            desiredEscapeDistance = Mathf.Max(0.5f, desiredEscapeDistance);
            dropDistanceFromVictim = Mathf.Max(0.5f, dropDistanceFromVictim);
            minimumCarryDuration = Mathf.Max(0f, minimumCarryDuration);
            maximumCarryDuration = Mathf.Max(
                minimumCarryDuration + 0.5f,
                maximumCarryDuration);
            pickupAutoReturnTime = Mathf.Max(2f, pickupAutoReturnTime);
            mouthInkAmount = Mathf.Max(0.05f, mouthInkAmount);
        }
    }
}
