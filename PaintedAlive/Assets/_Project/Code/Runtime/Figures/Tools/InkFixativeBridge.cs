using PaintedAlive.Figures;
using PaintedAlive.Paint;
using PaintedAlive.Paint.Ink;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    [DefaultExecutionOrder(-39)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FixativeSprayController))]
    public sealed class InkFixativeBridge : MonoBehaviour
    {
        private const int MaximumHits = 32;

        private readonly RaycastHit[] hits = new RaycastHit[MaximumHits];

        [Header("Existing Fixative Dependencies")]
        [SerializeField]
        private FixativeSprayController fixativeController;

        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private Transform toolOrigin;

        [SerializeField]
        private InputActionReference useToolAction;

        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private FixativeSprayConfig fixativeConfig;

        [SerializeField]
        private FixativeSprayFeedback feedback;

        [Header("Ink Counterplay")]
        [SerializeField]
        private InkCounterplayConfig counterplayConfig;

        [SerializeField]
        private LayerMask inkMask = Physics.DefaultRaycastLayers;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private InkCreatureRuntime currentTarget;

        [SerializeField]
        private int fixedCreatureCount;

        [SerializeField]
        private float currentTargetDistance;

        private float nextApplicationTime;

        public InkCreatureRuntime CurrentTarget => currentTarget;
        public int FixedCreatureCount => fixedCreatureCount;
        public float CurrentTargetDistance => currentTargetDistance;

        private void Awake()
        {
            fixativeController ??=
                GetComponent<FixativeSprayController>();
            toolOrigin ??= transform;
            clarityState ??=
                GetComponentInParent<FigureClarityState>();
            feedback ??= GetComponent<FixativeSprayFeedback>();

            if (outputCamera == null)
            {
                outputCamera = Camera.main;
            }

            if (fixativeController == null ||
                outputCamera == null ||
                useToolAction == null ||
                fixativeConfig == null ||
                counterplayConfig == null)
            {
                Debug.LogError(
                    $"{nameof(InkFixativeBridge)} on {name} requires " +
                    "Fixative Controller, Camera, Input Action and both configs.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (fixativeController == null ||
                !fixativeController.isActiveAndEnabled ||
                useToolAction == null ||
                useToolAction.action == null ||
                !useToolAction.action.IsPressed() ||
                Time.time < nextApplicationTime)
            {
                currentTarget = null;
                currentTargetDistance = 0f;
                return;
            }

            if (clarityState != null &&
                !clarityState.CanUsePrimaryTool)
            {
                return;
            }

            nextApplicationTime =
                Time.time + fixativeConfig.ApplicationInterval;
            TryFixInkCreature();
        }

        private void TryFixInkCreature()
        {
            Ray aimRay = outputCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.SphereCastNonAlloc(
                aimRay,
                counterplayConfig.FixativeCastRadius,
                hits,
                counterplayConfig.FixativeMaximumAimDistance,
                inkMask,
                QueryTriggerInteraction.Collide);
            InkCreatureRuntime bestCreature = null;
            RaycastHit bestHit = default;
            float bestAimDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];

                if (hit.collider == null)
                {
                    continue;
                }

                InkCreatureRuntime creature =
                    hit.collider.GetComponentInParent<InkCreatureRuntime>();

                if (creature == null ||
                    !creature.IsInitialized ||
                    hit.distance >= bestAimDistance)
                {
                    continue;
                }

                bestCreature = creature;
                bestHit = hit;
                bestAimDistance = hit.distance;
            }

            currentTarget = bestCreature;

            if (bestCreature == null)
            {
                currentTargetDistance = 0f;
                return;
            }

            Vector3 rangeOrigin = toolOrigin != null
                ? toolOrigin.position
                : transform.position;
            currentTargetDistance =
                Vector3.Distance(rangeOrigin, bestHit.point);

            if (currentTargetDistance > counterplayConfig.FixativeReach)
            {
                return;
            }

            bool newlyFixed = bestCreature.ApplyFixative(
                counterplayConfig.FixativeDuration,
                counterplayConfig.FixedInkColor);

            if (!newlyFixed)
            {
                return;
            }

            fixedCreatureCount++;
            Vector3 direction = bestHit.point - rangeOrigin;
            feedback?.PlayApplied(
                rangeOrigin,
                direction.sqrMagnitude > 0.001f
                    ? direction.normalized
                    : aimRay.direction,
                bestHit.point,
                bestHit.normal.sqrMagnitude > 0.001f
                    ? bestHit.normal
                    : Vector3.up,
                1f,
                true);

            Debug.Log(
                "[M16 Ink Counterplay] Sabitleyici Lekebacak'ı " +
                $"{counterplayConfig.FixativeDuration:F1} saniye heykele çevirdi.",
                bestCreature);
        }
    }
}
