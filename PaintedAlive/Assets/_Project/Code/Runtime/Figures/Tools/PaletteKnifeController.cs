using PaintedAlive.Figures;
using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class PaletteKnifeController : MonoBehaviour
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

        [Header("Aim Detection")]
        [SerializeField]
        private LayerMask oilPaintMask;

        [SerializeField, Min(1f)]
        private float maximumAimDistance = 50f;

        [SerializeField, Min(0f)]
        private float castRadius = 0.14f;

        [Header("Tool Properties")]
        [SerializeField, Min(0.1f)]
        private float reach = 2.5f;

        [SerializeField, Min(0.1f)]
        private float gapWidth = 1.25f;

        [Header("Debug")]
        [SerializeField]
        private bool logDebugMessages = true;

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
        }

        private void OnEnable()
        {
            if (useToolAction != null &&
                useToolAction.action != null)
            {
                useToolAction.action.Enable();
            }
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
            if (useToolAction == null ||
                useToolAction.action == null)
            {
                return;
            }

            if (useToolAction.action
                .WasPressedThisFrame())
            {
                TryCut();
            }
        }

        private void TryCut()
        {
            if (clarityState != null &&
                !clarityState.CanUsePrimaryTool)
            {
                Log(
                    "Clarity seviyesi ana alet " +
                    "kullanımına izin vermiyor.");

                return;
            }

            if (outputCamera == null)
            {
                Log("Output Camera atanmamış.");
                return;
            }

            Ray aimRay =
                outputCamera.ViewportPointToRay(
                    new Vector3(
                        0.5f,
                        0.5f,
                        0f));

            bool foundPaint =
                Physics.SphereCast(
                    aimRay,
                    castRadius,
                    out RaycastHit hit,
                    maximumAimDistance,
                    oilPaintMask,
                    QueryTriggerInteraction.Ignore);

            Debug.DrawRay(
                aimRay.origin,
                aimRay.direction *
                maximumAimDistance,
                foundPaint
                    ? Color.green
                    : Color.red,
                1f);

            if (!foundPaint)
            {
                Log(
                    "Kamera merkezinde OilPaint " +
                    "katmanında boya bulunamadı.");

                return;
            }

            float distanceFromTool =
                Vector3.Distance(
                    toolOrigin.position,
                    hit.point);

            Debug.DrawLine(
                toolOrigin.position,
                hit.point,
                distanceFromTool <= reach
                    ? Color.green
                    : Color.yellow,
                1f);

            if (distanceFromTool > reach)
            {
                Log(
                    $"Boya menzil dışında. " +
                    $"Mesafe: {distanceFromTool:F2} m / " +
                    $"Menzil: {reach:F2} m");

                return;
            }

            OilStrokeRuntime stroke =
                hit.collider
                    .GetComponentInParent<
                        OilStrokeRuntime>();

            if (stroke == null)
            {
                Log(
                    "Collider üzerinde " +
                    "OilStrokeRuntime bulunamadı.");

                return;
            }

            float effectiveGapWidth = gapWidth;

            if (clarityState != null)
            {
                effectiveGapWidth *=
                    clarityState.ToolEfficiency;
            }

            bool cutSucceeded =
                stroke.TryCutWorldPoint(
                    hit.point,
                    effectiveGapWidth);

            Log(
                cutSucceeded
                    ? $"Boya kesildi. " +
                      $"Durum: {stroke.State}, " +
                      $"Kesim genişliği: " +
                      $"{effectiveGapWidth:F2}"
                    : "Kesme işlemi " +
                      "OilStrokeRuntime tarafından " +
                      "reddedildi.");
        }

        private void Log(string message)
        {
            if (!logDebugMessages)
            {
                return;
            }

            Debug.Log(
                $"[{nameof(PaletteKnifeController)}] " +
                message,
                this);
        }
    }
}