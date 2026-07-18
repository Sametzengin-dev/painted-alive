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
            Previewing
        }

        [Header("Dependencies")]
        [SerializeField] private Camera outputCamera;
        [SerializeField] private OilStrokeSystem strokeSystem;
        [SerializeField] private PainterPigmentReservoir pigmentReservoir;
        [SerializeField] private PainterStrokeBudget strokeBudget;

        [SerializeField]
        private PainterStrokePressureTracker pressureTracker;
        [SerializeField] private PainterStrokeModeSelector strokeModeSelector;

        [Header("Visuals")]
        [SerializeField] private Transform brushVisual;
        [SerializeField] private Renderer brushRenderer;
        [SerializeField] private LineRenderer strokePreview;

        [Header("Input")]
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private InputActionReference paintAction;
        [SerializeField] private InputActionReference clearAction;

        [Header("Surface Detection")]
        [SerializeField] private LayerMask paintSurfaceMask;

        [SerializeField, Min(1f)]
        private float maximumRayDistance = 100f;

        [Header("Protected Figure Zone")]
        [SerializeField] private LayerMask forbiddenZoneMask;

        [SerializeField, Min(0f)]
        private float forbiddenRadius = 0.9f;

        [Header("Telegraph Fallback")]
        [SerializeField, Min(0f)]
        private float fallbackTelegraphDuration = 1f;

        [SerializeField, Min(0.02f)]
        private float previewPointSpacing = 0.22f;

        [Header("Brush Feedback")]
        [SerializeField] private Color validColor =
            new(0.75f, 0.12f, 0.16f, 1f);

        [SerializeField] private Color forbiddenColor =
            new(1f, 0.08f, 0.04f, 1f);

        [SerializeField] private Color budgetBlockedColor =
            new(1f, 0.45f, 0.05f, 1f);

        private readonly List<Vector3> previewPoints = new();

        private MaterialPropertyBlock brushPropertyBlock;
        private BrushState state;

        private float telegraphElapsed;
        private float currentTelegraphDuration;

        private OilStrokeShape activeShape =
            OilStrokeShape.Wall;

        public bool IsPreviewing =>
            state == BrushState.Previewing;

        public OilStrokeShape ActivePreviewShape =>
            activeShape;

        public float EstimatedPigmentCost { get; private set; }

        public bool PreviewCanAfford =>
            pigmentReservoir != null &&
            pigmentReservoir.CanAfford(
                EstimatedPigmentCost);

        public float TelegraphNormalized =>
            currentTelegraphDuration > 0f
                ? Mathf.Clamp01(
                    telegraphElapsed /
                    currentTelegraphDuration)
                : 1f;

        public bool IsTelegraphComplete =>
            IsPreviewing &&
            previewPoints.Count >= 2 &&
            TelegraphNormalized >= 1f;

        public OilStrokePressureProfile CurrentPressureProfile =>

            pressureTracker != null

                ? pressureTracker.CurrentProfile

                : OilStrokePressureProfile.Balanced;


        private void Awake()
        {
            brushPropertyBlock =
                new MaterialPropertyBlock();

            ClearPreview();
        }

        private void OnEnable()
        {
            SetActionEnabled(pointerPositionAction, true);
            SetActionEnabled(paintAction, true);
            SetActionEnabled(clearAction, true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            state = BrushState.Idle;
        }

        private void OnDisable()
        {
            CancelCurrentInteraction();

            SetActionEnabled(pointerPositionAction, false);
            SetActionEnabled(paintAction, false);
            SetActionEnabled(clearAction, false);

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

            bool hasPaintPoint = TryGetPaintPoint(
                out Vector3 paintPoint,
                out Vector3 surfaceNormal);

            bool forbidden =
                hasPaintPoint &&
                Physics.CheckSphere(
                    paintPoint,
                    forbiddenRadius,
                    forbiddenZoneMask,
                    QueryTriggerInteraction.Ignore);

            bool spatiallyValid =
                hasPaintPoint && !forbidden;

            OilStrokeShape selectedShape =
                strokeModeSelector != null
                    ? strokeModeSelector.CurrentShape
                    : OilStrokeShape.Wall;

            bool budgetAvailable =
                strokeBudget == null ||
                strokeBudget.CanBeginStroke(
                    selectedShape,
                    out _);

            bool brushValid =
                spatiallyValid && budgetAvailable;

            UpdateBrushVisual(
                hasPaintPoint,
                brushValid,
                budgetAvailable,
                paintPoint,
                surfaceNormal);

            if (WasClearPressed())
            {
                CancelCurrentInteraction();
                strokeSystem.ClearAllStrokes();

                if (strokeBudget != null)
                    strokeBudget.ResetBudget();

                return;
            }

            if (paintAction == null ||
                paintAction.action == null)
            {
                return;
            }

            if (state == BrushState.Idle &&
                paintAction.action.WasPressedThisFrame() &&
                spatiallyValid &&
                budgetAvailable)
            {
                StartPreview(
                    paintPoint,
                    selectedShape);
            }

            if (state == BrushState.Previewing)
            {
                UpdatePreview(
                    spatiallyValid,
                    paintPoint);
            }

            if (paintAction.action.WasReleasedThisFrame() &&
                state == BrushState.Previewing)
            {
                FinishPreview();
            }
        }

        private void StartPreview(
            Vector3 startPoint,
            OilStrokeShape shape)
        {
            CancelCurrentInteraction();

            state = BrushState.Previewing;
            activeShape = shape;
            telegraphElapsed = 0f;

            currentTelegraphDuration =
                strokeBudget != null
                    ? strokeBudget.GetTelegraphDuration(shape)
                    : fallbackTelegraphDuration;

            previewPoints.Clear();
            previewPoints.Add(startPoint);

            if (pressureTracker != null)
            {
                pressureTracker.BeginTracking(startPoint);
            }

            pigmentReservoir.SetConsuming(true);

            EstimatedPigmentCost =
                CalculatePreviewCost();

            if (strokePreview == null)
                return;

            strokePreview.useWorldSpace = true;

            RefreshPreviewWidth();

            strokePreview.enabled = true;
            strokePreview.positionCount = 1;
            strokePreview.SetPosition(0, startPoint);

            UpdatePreviewAppearance();
        }

        private void UpdatePreview(
            bool spatiallyValid,
            Vector3 paintPoint)
        {
            if (paintAction.action == null ||
                !paintAction.action.IsPressed())
            {
                return;
            }

            if (!spatiallyValid)
            {
                CancelPreview();
                return;
            }

            AppendPreviewPoint(paintPoint);

            telegraphElapsed += Time.deltaTime;

            EstimatedPigmentCost =
                CalculatePreviewCost();

            UpdatePreviewAppearance();
        }

        private void AppendPreviewPoint(Vector3 point)
        {
            if (previewPoints.Count == 0)
                return;

            Vector3 previous =
                previewPoints[previewPoints.Count - 1];

            float minimumDistanceSquared =
                previewPointSpacing *
                previewPointSpacing;

            if ((point - previous).sqrMagnitude <
                minimumDistanceSquared)
            {
                return;
            }

            previewPoints.Add(point);

            if (pressureTracker != null)
            {
                pressureTracker.RecordPoint(point);
            }

            RefreshPreviewWidth();

            if (strokePreview == null)
                return;

            strokePreview.positionCount =
                previewPoints.Count;

            strokePreview.SetPosition(
                previewPoints.Count - 1,
                point);
        }

        private void RefreshPreviewWidth()
        {
            if (strokePreview == null ||
                strokeSystem == null)
            {
                return;
            }

            float previewWidth =
                strokeSystem.GetPreviewWidth(
                    activeShape,
                    CurrentPressureProfile);

            strokePreview.startWidth =
                Mathf.Clamp(
                    previewWidth * 0.65f,
                    0.12f,
                    1.5f);

            strokePreview.endWidth =
                strokePreview.startWidth;
        }

        private void FinishPreview()
        {
            if (!IsTelegraphComplete ||
                !PreviewCanAfford)
            {
                CancelPreview();
                return;
            }

            if (strokeBudget != null &&
                !strokeBudget.CanBeginStroke(
                    activeShape,
                    CurrentPressureProfile,
                    out _))
            {
                CancelPreview();
                return;
            }

            CommitPreview();
        }

        private void CommitPreview()
        {
            EstimatedPigmentCost =
                CalculatePreviewCost();

            if (!pigmentReservoir.CanAfford(
                    EstimatedPigmentCost))
            {
                CancelPreview();
                return;
            }

            if (!strokeSystem.BeginStroke(
                    previewPoints[0],
                    activeShape,
                    CurrentPressureProfile))
            {
                CancelPreview();
                return;
            }

            int acceptedPointCount = 1;

            for (int i = 1;
                 i < previewPoints.Count;
                 i++)
            {
                if (strokeSystem.AppendStrokePoint(
                        previewPoints[i]))
                {
                    acceptedPointCount++;
                }
            }

            strokeSystem.EndStroke();

            if (acceptedPointCount < 2)
            {
                CancelPreview();
                return;
            }

            pigmentReservoir.TrySpend(
                EstimatedPigmentCost);

            if (strokeBudget != null)
                strokeBudget.NotifyStrokeCommitted();

            pigmentReservoir.SetConsuming(false);
            state = BrushState.Idle;

            ClearPreview();
        }

        private float CalculatePreviewCost()
        {
            float cost =
                pigmentReservoir != null
                    ? pigmentReservoir.StrokeBeginCost
                    : 0f;

            if (strokeBudget != null)
            {
                cost += strokeBudget.GetPigmentSurcharge(
                    activeShape);
            }

            float shapeMultiplier =
                strokeSystem != null
                    ? strokeSystem.GetPigmentMultiplier(
                        activeShape)
                    : 1f;

            for (int i = 1;
                 i < previewPoints.Count;
                 i++)
            {
                float distance = Vector3.Distance(
                    previewPoints[i - 1],
                    previewPoints[i]);

                cost += pigmentReservoir
                    .CalculateDistanceCost(distance) *
                    shapeMultiplier;
            }

            float pressurePigmentMultiplier =
                CurrentPressureProfile.IsValid
                    ? CurrentPressureProfile.PigmentMultiplier
                    : 1f;

            return Mathf.Max(
                0f,
                cost * pressurePigmentMultiplier);
        }

        private void UpdatePreviewAppearance()
        {
            if (strokePreview == null)
                return;

            Color color;

            if (!PreviewCanAfford)
            {
                color = new Color(
                    1f,
                    0.28f,
                    0.04f,
                    0.9f);
            }
            else if (IsTelegraphComplete)
            {
                color = new Color(
                    1f,
                    0.72f,
                    0.35f,
                    0.95f);
            }
            else
            {
                float pulse =
                    0.45f +
                    Mathf.Sin(Time.time * 9f) *
                    0.15f;

                color = activeShape ==
                        OilStrokeShape.Ramp
                    ? new Color(
                        0.95f,
                        0.25f,
                        0.12f,
                        pulse)
                    : new Color(
                        0.78f,
                        0.08f,
                        0.12f,
                        pulse);
            }

            strokePreview.startColor = color;
            strokePreview.endColor = color;
        }

        private void CancelPreview()
        {
            pigmentReservoir.SetConsuming(false);
            state = BrushState.Idle;

            ClearPreview();
        }

        private void CancelCurrentInteraction()
        {
            if (strokeSystem != null &&
                strokeSystem.IsDrawing)
            {
                strokeSystem.EndStroke();
            }

            if (pigmentReservoir != null)
                pigmentReservoir.SetConsuming(false);

            state = BrushState.Idle;

            ClearPreview();
        }

        private void ClearPreview()
        {
            previewPoints.Clear();

            if (pressureTracker != null)
            {
                pressureTracker.ResetTracking();
            }
            telegraphElapsed = 0f;
            EstimatedPigmentCost = 0f;

            if (strokePreview != null)
            {
                strokePreview.positionCount = 0;
                strokePreview.enabled = false;
            }
        }

        private bool WasClearPressed()
        {
            return clearAction != null &&
                   clearAction.action != null &&
                   clearAction.action.WasPressedThisFrame();
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
            bool spatiallyAndBudgetValid,
            bool budgetAvailable,
            Vector3 position,
            Vector3 normal)
        {
            if (brushVisual == null)
                return;

            brushVisual.gameObject.SetActive(visible);

            if (!visible)
                return;

            brushVisual.position = position;

            brushVisual.rotation =
                Quaternion.FromToRotation(
                    Vector3.up,
                    normal);

            if (brushRenderer == null)
                return;

            Color color;

            if (!budgetAvailable)
                color = budgetBlockedColor;
            else if (spatiallyAndBudgetValid)
                color = validColor;
            else
                color = forbiddenColor;

            brushRenderer.GetPropertyBlock(
                brushPropertyBlock);

            brushPropertyBlock.SetColor(
                BaseColorId,
                color);

            brushRenderer.SetPropertyBlock(
                brushPropertyBlock);
        }

        private void SetBrushVisible(bool visible)
        {
            if (brushVisual != null)
                brushVisual.gameObject.SetActive(visible);
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
                actionReference.action.Enable();
            else
                actionReference.action.Disable();
        }
    }
}