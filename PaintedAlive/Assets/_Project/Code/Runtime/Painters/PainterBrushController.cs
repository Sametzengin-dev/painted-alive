using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Painters
{
    [DisallowMultipleComponent]
    public sealed class PainterBrushController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Camera outputCamera;
        [SerializeField] private OilStrokeSystem strokeSystem;
        [SerializeField] private Transform brushVisual;

        [Header("Input")]
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private InputActionReference paintAction;
        [SerializeField] private InputActionReference clearAction;

        [Header("Surface Detection")]
        [SerializeField] private LayerMask paintSurfaceMask;
        [SerializeField, Min(1f)] private float maximumRayDistance = 100f;

        private void OnEnable()
        {
            SetActionEnabled(pointerPositionAction, true);
            SetActionEnabled(paintAction, true);
            SetActionEnabled(clearAction, true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDisable()
        {
            if (strokeSystem != null)
            {
                strokeSystem.EndStroke();
            }

            SetActionEnabled(pointerPositionAction, false);
            SetActionEnabled(paintAction, false);
            SetActionEnabled(clearAction, false);

            SetBrushVisible(false);
        }

        private void Update()
        {
            if (outputCamera == null || strokeSystem == null)
            {
                return;
            }

            bool hasPaintPoint = TryGetPaintPoint(
                out Vector3 paintPoint,
                out Vector3 surfaceNormal);

            UpdateBrushVisual(
                hasPaintPoint,
                paintPoint,
                surfaceNormal);

            if (clearAction != null &&
                clearAction.action != null &&
                clearAction.action.WasPressedThisFrame())
            {
                strokeSystem.ClearAllStrokes();
            }

            if (paintAction == null || paintAction.action == null)
            {
                return;
            }

            if (paintAction.action.IsPressed() && hasPaintPoint)
            {
                if (!strokeSystem.IsDrawing)
                {
                    strokeSystem.BeginStroke(paintPoint);
                }
                else
                {
                    strokeSystem.AppendStrokePoint(paintPoint);
                }
            }

            if (paintAction.action.WasReleasedThisFrame())
            {
                strokeSystem.EndStroke();
            }
        }

        private bool TryGetPaintPoint(
            out Vector3 paintPoint,
            out Vector3 surfaceNormal)
        {
            paintPoint = default;
            surfaceNormal = Vector3.up;

            if (pointerPositionAction == null ||
                pointerPositionAction.action == null)
            {
                return false;
            }

            Vector2 pointerPosition =
                pointerPositionAction.action.ReadValue<Vector2>();

            Ray ray = outputCamera.ScreenPointToRay(pointerPosition);

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    maximumRayDistance,
                    paintSurfaceMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            surfaceNormal = hit.normal;

            paintPoint =
                hit.point +
                hit.normal * strokeSystem.SurfaceOffset;

            return true;
        }

        private void UpdateBrushVisual(
            bool visible,
            Vector3 position,
            Vector3 normal)
        {
            if (brushVisual == null)
            {
                return;
            }

            brushVisual.gameObject.SetActive(visible);

            if (!visible)
            {
                return;
            }

            brushVisual.position = position;

            brushVisual.rotation = Quaternion.FromToRotation(
                Vector3.up,
                normal);
        }

        private void SetBrushVisible(bool visible)
        {
            if (brushVisual != null)
            {
                brushVisual.gameObject.SetActive(visible);
            }
        }

        private static void SetActionEnabled(
            InputActionReference actionReference,
            bool enabled)
        {
            if (actionReference == null ||
                actionReference.action == null)
            {
                return;
            }

            if (enabled)
            {
                actionReference.action.Enable();
            }
            else
            {
                actionReference.action.Disable();
            }
        }
    }
}
