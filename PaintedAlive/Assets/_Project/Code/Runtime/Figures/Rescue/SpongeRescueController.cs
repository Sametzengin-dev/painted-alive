using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures
{
    [RequireComponent(typeof(FigureCleanPigmentInventory))]
    public sealed class SpongeRescueController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        private InputActionProperty useSpongeAction;

        [Header("Detection")]
        [SerializeField]
        private Transform rescueOrigin;

        [SerializeField, Min(0.5f)]
        private float rescueRange = 2.25f;

        [SerializeField]
        private LayerMask figureLayerMask;

        [Header("Rescue")]
        [SerializeField, Min(0.1f)]
        private float rescueDuration = 2f;

        [SerializeField, Min(1f)]
        private float restoredClarity = 40f;

        [SerializeField, Min(1)]
        private int pigmentCost = 1;

        private readonly Collider[] overlapResults =
            new Collider[12];

        private FigureCleanPigmentInventory inventory;
        private FigureClarityState ownClarity;

        private float rescueProgress;
        private bool waitingForRelease;
        private bool enabledInputHere;

        public FigureClarityState CurrentTarget
        {
            get;
            private set;
        }

        public float NormalizedProgress =>
            rescueDuration > 0f
                ? Mathf.Clamp01(
                    rescueProgress /
                    rescueDuration)
                : 0f;

        public bool CanUseSponge =>
            ownClarity == null ||
            ownClarity.CanUsePrimaryTool;

        private void Awake()
        {
            inventory =
                GetComponent<FigureCleanPigmentInventory>();

            ownClarity =
                GetComponent<FigureClarityState>();

            if (rescueOrigin == null)
            {
                rescueOrigin = transform;
            }
        }

        private void OnEnable()
        {
            InputAction action =
                useSpongeAction.action;

            if (action != null &&
                !action.enabled)
            {
                action.Enable();
                enabledInputHere = true;
            }
        }

        private void OnDisable()
        {
            if (enabledInputHere &&
                useSpongeAction.action != null)
            {
                useSpongeAction.action.Disable();
            }

            enabledInputHere = false;
            CurrentTarget = null;
            rescueProgress = 0f;
            waitingForRelease = false;
        }

        private void Update()
        {
            if (!CanUseSponge)
            {
                CurrentTarget = null;
                rescueProgress = 0f;
                waitingForRelease = false;
                return;
            }

            UpdateTarget();

            InputAction action =
                useSpongeAction.action;

            bool isHeld =
                action != null &&
                action.IsPressed();

            if (!isHeld)
            {
                rescueProgress = 0f;
                waitingForRelease = false;
                return;
            }

            if (waitingForRelease)
            {
                return;
            }

            if (CurrentTarget == null ||
                !inventory.HasPigment)
            {
                rescueProgress = 0f;
                return;
            }

            rescueProgress += Time.deltaTime;

            if (rescueProgress < rescueDuration)
            {
                return;
            }

            CompleteRescue();
        }

        private void UpdateTarget()
        {
            FigureClarityState previousTarget =
                CurrentTarget;

            CurrentTarget =
                FindNearestTarget();

            if (previousTarget != CurrentTarget)
            {
                rescueProgress = 0f;
            }
        }

        private FigureClarityState FindNearestTarget()
        {
            Vector3 origin =
                rescueOrigin.position;

            int hitCount =
                Physics.OverlapSphereNonAlloc(
                    origin,
                    rescueRange,
                    overlapResults,
                    figureLayerMask,
                    QueryTriggerInteraction.Collide);

            FigureClarityState nearestTarget = null;

            float nearestDistanceSquared =
                float.MaxValue;

            for (int i = 0;
                 i < hitCount;
                 i++)
            {
                Collider hit =
                    overlapResults[i];

                if (hit == null)
                {
                    continue;
                }

                FigureClarityState candidate =
                    hit.GetComponentInParent<
                        FigureClarityState>();

                if (candidate == null ||
                    candidate == ownClarity)
                {
                    continue;
                }

                if (candidate.CurrentClarity >=
                    candidate.MaximumClarity - 0.01f)
                {
                    continue;
                }

                float distanceSquared =
                    (
                        candidate.transform.position -
                        origin
                    ).sqrMagnitude;

                if (distanceSquared >=
                    nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared =
                    distanceSquared;

                nearestTarget =
                    candidate;
            }

            return nearestTarget;
        }

        private void CompleteRescue()
        {
            FigureClarityState target =
                CurrentTarget;

            rescueProgress = 0f;
            waitingForRelease = true;

            if (target == null)
            {
                return;
            }

            if (!inventory.TryConsume(pigmentCost))
            {
                return;
            }

            bool restored =
                target.RestoreClarity(
                    restoredClarity);

            if (!restored)
            {
                inventory.AddPigment(
                    pigmentCost);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin =
                rescueOrigin != null
                    ? rescueOrigin
                    : transform;

            Gizmos.color =
                new Color(
                    0.3f,
                    0.9f,
                    0.75f,
                    0.5f);

            Gizmos.DrawWireSphere(
                origin.position,
                rescueRange);
        }
    }
}