using System.Collections.Generic;
using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Painters
{
    [DisallowMultipleComponent]
    public sealed class PainterBrushController : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private enum BrushState
        {
            Idle,
            Previewing,
            Painting
        }

        [Header("Dependencies")]
        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private OilStrokeSystem strokeSystem;

        [SerializeField]
        private PainterPigmentReservoir pigmentReservoir;

        [SerializeField]
        private Transform brushVisual;

        [SerializeField]
        private Renderer brushRenderer;

        [SerializeField]
        private LineRenderer strokePreview;

        [SerializeField]
        private PainterStrokeModeSelector strokeModeSelector;

        [Header("Input")]
        [SerializeField]
        private InputActionReference pointerPositionAction;

        [SerializeField]
        private InputActionReference paintAction;

        [SerializeField]
        private InputActionReference clearAction;

        [Header("Surface Detection")]
        [SerializeField]
        private LayerMask paintSurfaceMask;

        [SerializeField, Min(1f)]
        private float maximumRayDistance = 100f;

        [Header("Protected Figure Zone")]
        [SerializeField]
        private LayerMask forbiddenZoneMask;

        [SerializeField, Min(0f)]
        private float forbiddenRadius = 0.9f;

        [Header("Telegraph")]
        [SerializeField, Min(0f)]
        private float telegraphDuration = 0.45f;

        [SerializeField, Min(0.02f)]
        private float previewPointSpacing = 0.22f;

        [Header("Brush Feedback")]
        [SerializeField]
        private Color validColor =
            new(0.75f, 0.12f, 0.16f, 1f);

        [SerializeField]
        private Color forbiddenColor =
            new(1f, 0.08f, 0.04f, 1f);

        private readonly List<Vector3> previewPoints =
            new();

        private MaterialPropertyBlock brushPropertyBlock;
        private BrushState state;
        private float telegraphElapsed;
        private Vector3 lastCommittedPoint;

        private OilStrokeShape activeShape =
            OilStrokeShape.Wall;

        private void Awake()
        {
            brushPropertyBlock =
                new MaterialPropertyBlock();

            ClearPreview();
        }

        private void OnEnable()
        {
            SetActionEnabled(
                pointerPositionAction,
                true);

            SetActionEnabled(
                paintAction,
                true);

            SetActionEnabled(
                clearAction,
                true);

            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;

            state = BrushState.Idle;
        }

        private void OnDisable()
        {
            CancelCurrentInteraction();

            SetActionEnabled(
                pointerPositionAction,
                false);

            SetActionEnabled(
                paintAction,
                false);

            SetActionEnabled(
                clearAction,
                false);

            SetBrushVisible(false);
        }

        private void Update()
        {
            if (outputCamera == null ||
                strokeSystem == null ||
                pigmentReservoir == null)
            {
                return;
            }

            bool hasPaintPoint =
                TryGetPaintPoint(
                    out Vector3 paintPoint,
                    out Vector3 surfaceNormal);

            bool forbidden =
                hasPaintPoint &&
                Physics.CheckSphere(
                    paintPoint,
                    forbiddenRadius,
                    forbiddenZoneMask,
                    QueryTriggerInteraction.Ignore);

            bool validPoint =
                hasPaintPoint &&
                !forbidden;

            UpdateBrushVisual(
                hasPaintPoint,
                validPoint,
                paintPoint,
                surfaceNormal);

            if (WasClearPressed())
            {
                CancelCurrentInteraction();
                strokeSystem.ClearAllStrokes();
                return;
            }

            if (paintAction == null ||
                paintAction.action == null)
            {
                return;
            }

            if (paintAction.action
                    .WasPressedThisFrame() &&
                validPoint)
            {
                StartPreview(paintPoint);
            }

            switch (state)
            {
                case BrushState.Previewing:
                    UpdatePreview(
                        validPoint,
                        paintPoint);
                    break;

                case BrushState.Painting:
                    UpdatePainting(
                        validPoint,
                        paintPoint);
                    break;
            }

            if (paintAction.action
                .WasReleasedThisFrame())
            {
                if (state ==
                    BrushState.Previewing)
                {
                    CancelPreview();
                }
                else if (state ==
                         BrushState.Painting)
                {
                    EndPainting();
                }
            }
        }

        private void StartPreview(
            Vector3 startPoint)
        {
            CancelCurrentInteraction();

            state =
                BrushState.Previewing;

            telegraphElapsed = 0f;

            previewPoints.Clear();
            previewPoints.Add(startPoint);

            pigmentReservoir
                .SetConsuming(true);

            OilStrokeShape previewShape =
                strokeModeSelector != null
                    ? strokeModeSelector
                        .CurrentShape
                    : OilStrokeShape.Wall;

            float previewWidth =
                strokeSystem.GetPreviewWidth(
                    previewShape);

            strokePreview.startWidth =
                Mathf.Clamp(
                    previewWidth * 0.15f,
                    0.08f,
                    0.25f);

            strokePreview.endWidth =
                strokePreview.startWidth;

            if (previewShape ==
                OilStrokeShape.Ramp)
            {
                strokePreview.startColor =
                    new Color(
                        0.7f,
                        0.1f,
                        0.12f,
                        0.2f);

                strokePreview.endColor =
                    new Color(
                        1f,
                        0.35f,
                        0.2f,
                        0.85f);
            }
            else
            {
                strokePreview.startColor =
                    new Color(
                        0.8f,
                        0.1f,
                        0.12f,
                        0.45f);

                strokePreview.endColor =
                    strokePreview.startColor;
            }

            strokePreview.enabled = true;
            strokePreview.positionCount = 1;

            strokePreview.SetPosition(
                0,
                startPoint);
        }

        private void UpdatePreview(
            bool validPoint,
            Vector3 paintPoint)
        {
            if (!paintAction.action
                .IsPressed())
            {
                return;
            }

            if (!validPoint)
            {
                CancelPreview();
                return;
            }

            AppendPreviewPoint(
                paintPoint);

            telegraphElapsed +=
                Time.deltaTime;

            bool telegraphComplete =
                telegraphElapsed >=
                telegraphDuration;

            bool hasDrawablePath =
                previewPoints.Count >= 2;

            if (telegraphComplete &&
                hasDrawablePath)
            {
                CommitPreview();
            }
        }

        private void AppendPreviewPoint(
            Vector3 point)
        {
            Vector3 previous =
                previewPoints[
                    previewPoints.Count - 1];

            float minimumDistanceSquared =
                previewPointSpacing *
                previewPointSpacing;

            if ((point - previous)
                .sqrMagnitude <
                minimumDistanceSquared)
            {
                return;
            }

            previewPoints.Add(point);

            strokePreview.positionCount =
                previewPoints.Count;

            strokePreview.SetPosition(
                previewPoints.Count - 1,
                point);
        }

        private void CommitPreview()
        {
            activeShape =
                strokeModeSelector != null
                    ? strokeModeSelector
                        .CurrentShape
                    : OilStrokeShape.Wall;

            float pigmentMultiplier =
                strokeSystem
                    .GetPigmentMultiplier(
                        activeShape);

            float requiredPigment =
                pigmentReservoir
                    .StrokeBeginCost *
                pigmentMultiplier;

            for (int i = 1;
                 i < previewPoints.Count;
                 i++)
            {
                float distance =
                    Vector3.Distance(
                        previewPoints[i - 1],
                        previewPoints[i]);

                requiredPigment +=
                    pigmentReservoir
                        .CalculateDistanceCost(
                            distance) *
                    pigmentMultiplier;
            }

            if (!pigmentReservoir
                .CanAfford(requiredPigment))
            {
                CancelPreview();
                return;
            }

            if (!strokeSystem.BeginStroke(
                    previewPoints[0],
                    activeShape))
            {
                CancelPreview();
                return;
            }

            Vector3 lastAcceptedPoint =
                previewPoints[0];

            for (int i = 1;
                 i < previewPoints.Count;
                 i++)
            {
                if (strokeSystem
                    .AppendStrokePoint(
                        previewPoints[i]))
                {
                    lastAcceptedPoint =
                        previewPoints[i];
                }
            }

            pigmentReservoir.TrySpend(
                requiredPigment);

            lastCommittedPoint =
                lastAcceptedPoint;

            state =
                BrushState.Painting;

            ClearPreview();
        }

        private void UpdatePainting(
            bool validPoint,
            Vector3 paintPoint)
        {
            if (!paintAction.action
                .IsPressed())
            {
                return;
            }

            if (!validPoint)
            {
                EndPainting();
                return;
            }

            float distance =
                Vector3.Distance(
                    lastCommittedPoint,
                    paintPoint);

            float pointCost =
                pigmentReservoir
                    .CalculateDistanceCost(
                        distance) *
                strokeSystem
                    .GetPigmentMultiplier(
                        activeShape);

            if (!pigmentReservoir
                .CanAfford(pointCost))
            {
                EndPainting();
                return;
            }

            bool accepted =
                strokeSystem
                    .AppendStrokePoint(
                        paintPoint);

            if (!accepted)
            {
                return;
            }

            pigmentReservoir.TrySpend(
                pointCost);

            lastCommittedPoint =
                paintPoint;
        }

        private void EndPainting()
        {
            strokeSystem.EndStroke();

            pigmentReservoir
                .SetConsuming(false);

            state = BrushState.Idle;

            ClearPreview();
        }

        private void CancelPreview()
        {
            pigmentReservoir
                .SetConsuming(false);

            state = BrushState.Idle;

            ClearPreview();
        }

        private void CancelCurrentInteraction()
        {
            if (state ==
                    BrushState.Painting &&
                strokeSystem != null)
            {
                strokeSystem.EndStroke();
            }

            if (pigmentReservoir != null)
            {
                pigmentReservoir
                    .SetConsuming(false);
            }

            state = BrushState.Idle;

            ClearPreview();
        }

        private void ClearPreview()
        {
            previewPoints.Clear();

            if (strokePreview != null)
            {
                strokePreview.positionCount =
                    0;

                strokePreview.enabled =
                    false;
            }
        }

        private bool WasClearPressed()
        {
            return clearAction != null &&
                   clearAction.action != null &&
                   clearAction.action
                       .WasPressedThisFrame();
        }

        private bool TryGetPaintPoint(
            out Vector3 paintPoint,
            out Vector3 surfaceNormal)
        {
            paintPoint = default;
            surfaceNormal = Vector3.up;

            if (pointerPositionAction == null ||
                pointerPositionAction.action ==
                null)
            {
                return false;
            }

            Vector2 pointerPosition =
                pointerPositionAction.action
                    .ReadValue<Vector2>();

            Ray ray =
                outputCamera.ScreenPointToRay(
                    pointerPosition);

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
                hit.normal *
                strokeSystem.SurfaceOffset;

            return true;
        }

        private void UpdateBrushVisual(
            bool visible,
            bool valid,
            Vector3 position,
            Vector3 normal)
        {
            if (brushVisual == null)
            {
                return;
            }

            brushVisual.gameObject
                .SetActive(visible);

            if (!visible)
            {
                return;
            }

            brushVisual.position =
                position;

            brushVisual.rotation =
                Quaternion.FromToRotation(
                    Vector3.up,
                    normal);

            if (brushRenderer == null)
            {
                return;
            }

            brushRenderer.GetPropertyBlock(
                brushPropertyBlock);

            brushPropertyBlock.SetColor(
                BaseColorId,
                valid
                    ? validColor
                    : forbiddenColor);

            brushRenderer.SetPropertyBlock(
                brushPropertyBlock);
        }

        private void SetBrushVisible(
            bool visible)
        {
            if (brushVisual != null)
            {
                brushVisual.gameObject
                    .SetActive(visible);
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