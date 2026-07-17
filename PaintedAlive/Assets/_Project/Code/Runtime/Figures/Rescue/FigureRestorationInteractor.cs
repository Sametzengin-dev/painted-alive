using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures
{
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class FigureRestorationInteractor : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        private InputActionProperty restorationAction;

        [Header("Detection")]
        [SerializeField] private Transform detectionOrigin;
        [SerializeField, Min(0.5f)] private float detectionRange = 2.5f;
        [SerializeField] private LayerMask restorationPointMask;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)]
        private float interactionDuration = 2.5f;

        private readonly Collider[] overlapResults = new Collider[8];

        private FigureClarityState clarityState;
        private float interactionProgress;
        private bool waitingForRelease;
        private bool enabledInputHere;

        public RestorationPoint CurrentPoint { get; private set; }

        public float NormalizedProgress =>
            interactionDuration > 0f
                ? Mathf.Clamp01(
                    interactionProgress / interactionDuration)
                : 0f;

        public bool CanUseCurrentPoint =>
            CurrentPoint != null &&
            CurrentPoint.CanRestore(clarityState);

        public bool CurrentPointAlreadyUsed =>
            CurrentPoint != null &&
            CurrentPoint.HasBeenUsedBy(clarityState);

        public bool RestorationNotNeeded =>
            CurrentPoint != null &&
            !CurrentPointAlreadyUsed &&
            !CurrentPoint.NeedsRestoration(clarityState);

        private void Awake()
        {
            clarityState = GetComponent<FigureClarityState>();

            if (detectionOrigin == null)
                detectionOrigin = transform;
        }

        private void OnEnable()
        {
            InputAction action = restorationAction.action;

            if (action != null && !action.enabled)
            {
                action.Enable();
                enabledInputHere = true;
            }
        }

        private void OnDisable()
        {
            if (enabledInputHere &&
                restorationAction.action != null)
            {
                restorationAction.action.Disable();
            }

            enabledInputHere = false;
            CurrentPoint = null;
            interactionProgress = 0f;
            waitingForRelease = false;
        }

        private void Update()
        {
            UpdateCurrentPoint();

            InputAction action = restorationAction.action;
            bool isHeld = action != null && action.IsPressed();

            if (!isHeld)
            {
                interactionProgress = 0f;
                waitingForRelease = false;
                return;
            }

            if (waitingForRelease)
                return;

            if (!CanUseCurrentPoint)
            {
                interactionProgress = 0f;
                return;
            }

            interactionProgress += Time.deltaTime;

            if (interactionProgress < interactionDuration)
                return;

            CompleteRestoration();
        }

        private void UpdateCurrentPoint()
        {
            RestorationPoint previousPoint = CurrentPoint;
            CurrentPoint = FindNearestPoint();

            if (previousPoint != CurrentPoint)
                interactionProgress = 0f;
        }

        private RestorationPoint FindNearestPoint()
        {
            Vector3 origin = detectionOrigin.position;

            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                detectionRange,
                overlapResults,
                restorationPointMask,
                QueryTriggerInteraction.Collide);

            RestorationPoint nearestPoint = null;
            float nearestDistanceSquared = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = overlapResults[i];

                if (hit == null)
                    continue;

                RestorationPoint point =
                    hit.GetComponentInParent<RestorationPoint>();

                if (point == null)
                    continue;

                Vector3 pointPosition =
                    point.InteractionPosition.position;

                float distanceSquared =
                    (pointPosition - origin).sqrMagnitude;

                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearestDistanceSquared = distanceSquared;
                nearestPoint = point;
            }

            return nearestPoint;
        }

        private void CompleteRestoration()
        {
            RestorationPoint point = CurrentPoint;

            interactionProgress = 0f;
            waitingForRelease = true;

            if (point == null)
                return;

            point.TryRestore(clarityState);
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin =
                detectionOrigin != null
                    ? detectionOrigin
                    : transform;

            Gizmos.color = new Color(
                0.95f,
                0.82f,
                0.35f,
                0.6f);

            Gizmos.DrawWireSphere(
                origin.position,
                detectionRange);
        }
    }
}
