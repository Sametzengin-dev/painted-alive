using PaintedAlive.Figures;
using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class FixativeSprayController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private Transform toolOrigin;

        [SerializeField]
        private InputActionReference useToolAction;

        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private FixativeSprayConfig config;

        [SerializeField]
        private FixativeSprayFeedback feedback;

        [Header("Aim Detection")]
        [SerializeField]
        private LayerMask oilPaintMask;

        [Header("Debug")]
        [SerializeField]
        private bool logStateChanges = true;

        private float nextApplicationTime;

        private void Awake()
        {
            if (toolOrigin == null)
            {
                toolOrigin = transform;
            }

            if (clarityState == null)
            {
                clarityState =
                    GetComponentInParent<
                        FigureClarityState>();
            }

            if (feedback == null)
            {
                feedback =
                    GetComponent<FixativeSprayFeedback>();
            }

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(FixativeSprayController)} on {name} " +
                    "requires a config.",
                    this);

                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (useToolAction != null &&
                useToolAction.action != null)
            {
                useToolAction.action.Enable();
            }

            nextApplicationTime = 0f;
        }

        private void OnDisable()
        {
            if (useToolAction != null &&
                useToolAction.action != null)
            {
                useToolAction.action.Disable();
            }
        }

        private void Update()
        {
            if (config == null ||
                useToolAction == null ||
                useToolAction.action == null)
            {
                return;
            }

            if (!useToolAction.action.IsPressed())
            {
                return;
            }

            if (Time.time < nextApplicationTime)
            {
                return;
            }

            nextApplicationTime =
                Time.time + config.ApplicationInterval;

            TryApplyFixative();
        }

        private void TryApplyFixative()
        {
            if (clarityState != null &&
                !clarityState.CanUsePrimaryTool)
            {
                feedback?.PlayRejected(
                    toolOrigin.position,
                    toolOrigin.forward);

                return;
            }

            if (outputCamera == null)
            {
                return;
            }

            Ray aimRay =
                outputCamera.ViewportPointToRay(
                    new Vector3(0.5f, 0.5f, 0f));

            bool foundPaint =
                Physics.SphereCast(
                    aimRay,
                    config.CastRadius,
                    out RaycastHit hit,
                    config.MaximumAimDistance,
                    oilPaintMask,
                    QueryTriggerInteraction.Ignore);

            Debug.DrawRay(
                aimRay.origin,
                aimRay.direction *
                config.MaximumAimDistance,
                foundPaint ? Color.cyan : Color.gray,
                config.ApplicationInterval * 1.5f);

            if (!foundPaint)
            {
                feedback?.PlayMiss(
                    toolOrigin.position,
                    aimRay.direction);

                return;
            }

            float distanceFromTool =
                Vector3.Distance(
                    toolOrigin.position,
                    hit.point);

            if (distanceFromTool > config.Reach)
            {
                feedback?.PlayRejected(
                    toolOrigin.position,
                    aimRay.direction);

                return;
            }

            OilStrokeRuntime stroke =
                hit.collider
                    .GetComponentInParent<
                        OilStrokeRuntime>();

            if (stroke == null ||
                !stroke.IsFinalized ||
                stroke.State == OilStrokeState.Dry)
            {
                feedback?.PlayRejected(
                    hit.point,
                    hit.normal);

                return;
            }

            float toolEfficiency =
                clarityState != null
                    ? clarityState.ToolEfficiency
                    : 1f;

            float requestedDose =
                config.GetDosePerApplication(
                    toolEfficiency);

            float lifecycleAdvance =
                config.GetLifecycleAdvance(
                    requestedDose);

            bool advanced =
                stroke.TryAdvanceLifecycle(
                    lifecycleAdvance,
                    out OilStrokeState previousState,
                    out OilStrokeState currentState);

            if (!advanced)
            {
                feedback?.PlayRejected(
                    hit.point,
                    hit.normal);

                return;
            }

            OilStrokeFixativeStatus status =
                stroke.GetComponent<
                    OilStrokeFixativeStatus>();

            if (status == null)
            {
                status =
                    stroke.gameObject.AddComponent<
                        OilStrokeFixativeStatus>();
            }

            status.ApplyDose(
                requestedDose,
                config.MaximumCutDamageMultiplier);

            feedback?.PlayApplied(
                toolOrigin.position,
                (hit.point - toolOrigin.position)
                    .normalized,
                hit.point,
                hit.normal,
                status.Saturation,
                currentState == OilStrokeState.Dry);

            if (logStateChanges &&
                previousState != currentState)
            {
                Debug.Log(
                    $"[{nameof(FixativeSprayController)}] " +
                    $"Stroke durumu: {previousState} -> " +
                    $"{currentState}. Sabitleyici: " +
                    $"%{status.Saturation * 100f:F0}",
                    stroke);
            }
        }
    }
}
