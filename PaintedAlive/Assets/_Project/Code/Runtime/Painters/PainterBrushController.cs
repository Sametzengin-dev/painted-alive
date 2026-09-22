using System.Collections.Generic;
using PaintedAlive.Paint;
using PaintedAlive.Networking.M56;
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

        private const float PreviewNetworkInterval = 1f / 15f;
        private uint localPreviewId;
        private uint remotePreviewId;
        private int remotePreviewPointCount;
        private float nextPreviewNetworkAt;
        private int lastPreviewNetworkPointCount;

        public Camera OutputCamera => outputCamera;
        public LayerMask PaintSurfaceMask => paintSurfaceMask;

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

        public bool TryGetNetworkPresence(
            out Vector3 position,
            out Vector3 focusPoint,
            out Vector3 forward,
            out bool painting)
        {
            position = default;
            focusPoint = default;
            forward = Vector3.forward;
            painting = IsPreviewing;

            if (outputCamera == null)
                return false;

            Transform cameraTransform = outputCamera.transform;
            position = cameraTransform.position;
            forward = cameraTransform.forward;
            focusPoint = brushVisual != null && brushVisual.gameObject.activeInHierarchy
                ? brushVisual.position
                : position + forward * 8f;
            return true;
        }

        public void ApplyReplicatedPreview(
            uint previewId,
            bool visible,
            Vector3[] points,
            OilStrokeShape shape,
            float width)
        {
            if (previewId < remotePreviewId || strokePreview == null)
                return;

            if (previewId == remotePreviewId &&
                visible &&
                points != null &&
                points.Length < remotePreviewPointCount)
            {
                return;
            }

            if (previewId > remotePreviewId)
                remotePreviewPointCount = 0;

            remotePreviewId = previewId;

            if (!visible || points == null || points.Length == 0)
            {
                strokePreview.positionCount = 0;
                strokePreview.enabled = false;
                remotePreviewPointCount = 0;
                return;
            }

            strokePreview.useWorldSpace = true;
            strokePreview.startWidth = Mathf.Clamp(width, 0.08f, 1.5f);
            strokePreview.endWidth = strokePreview.startWidth;
            Color color = shape == OilStrokeShape.Ramp
                ? new Color(1f, 0.42f, 0.10f, 0.88f)
                : new Color(0.12f, 0.78f, 0.86f, 0.88f);
            strokePreview.startColor = color;
            strokePreview.endColor = color;
            strokePreview.positionCount = points.Length;
            strokePreview.SetPositions(points);
            strokePreview.enabled = true;
            remotePreviewPointCount = points.Length;
        }


        private void Awake()
        {
            paintSurfaceMask =
                PainterEnvironmentLayerUtility.ExpandSurfaceMask(
                    paintSurfaceMask);

            brushPropertyBlock =
                new MaterialPropertyBlock();

            ClearPreview();
        }

        public void IncludeEnvironmentSurfaceLayers()
        {
            paintSurfaceMask =
                PainterEnvironmentLayerUtility.ExpandSurfaceMask(
                    paintSurfaceMask);
        }

        public bool ApplyReplicatedStroke(
            int networkStrokeId,
            Vector3[] points,
            OilStrokeShape shape,
            OilStrokePressureProfile pressureProfile)
        {
            if (strokeSystem == null ||
                points == null ||
                points.Length < 2)
            {
                return false;
            }

            if (!strokeSystem.BeginReplicatedStroke(
                    networkStrokeId,
                    points[0],
                    shape,
                    pressureProfile))
            {
                return false;
            }

            int accepted = 1;
            for (int index = 1; index < points.Length; index++)
            {
                if (strokeSystem.AppendStrokePoint(points[index]))
                    accepted++;
            }

            strokeSystem.EndStroke();
            ApplyReplicatedPreview(
                remotePreviewId,
                false,
                System.Array.Empty<Vector3>(),
                shape,
                0.2f);
            return accepted >= 2;
        }

        public bool ApplyReplicatedCut(
            int networkStrokeId,
            Vector3 worldPoint,
            float gapWidth)
        {
            return strokeSystem != null &&
                   strokeSystem.TryApplyReplicatedCut(
                       networkStrokeId,
                       worldPoint,
                       gapWidth);
        }

        public void ApplyReplicatedClear()
        {
            if (strokeSystem != null)
                strokeSystem.ClearAllStrokes();

            if (strokeBudget != null)
                strokeBudget.ResetBudget();

            ApplyReplicatedPreview(
                remotePreviewId,
                false,
                System.Array.Empty<Vector3>(),
                OilStrokeShape.Wall,
                0.2f);
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
            if (PaintedAliveNetworkRoleBridge.GameplayInputSuppressed)
            {
                CancelCurrentInteraction();
                SetBrushVisible(false);
                return;
            }

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

                PaintedAliveNetworkGameplayBridge
                    .NotifyLocalOilStrokeClear();

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
            localPreviewId++;
            nextPreviewNetworkAt = 0f;
            lastPreviewNetworkPointCount = 0;

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
            PublishPreviewIfNeeded(force: true, visible: true);
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
            PublishPreviewIfNeeded(force: false, visible: true);
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

            int networkStrokeId =
                strokeSystem.LastFinalizedStroke != null
                    ? strokeSystem.LastFinalizedStroke.NetworkStrokeId
                    : 0;

            if (acceptedPointCount < 2)
            {
                CancelPreview();
                return;
            }

            pigmentReservoir.TrySpend(
                EstimatedPigmentCost);

            if (strokeBudget != null)
                strokeBudget.NotifyStrokeCommitted();

            Vector3[] committedPoints =
                previewPoints.ToArray();
            OilStrokePressureProfile committedProfile =
                CurrentPressureProfile;
            OilStrokeShape committedShape = activeShape;

            pigmentReservoir.SetConsuming(false);
            state = BrushState.Idle;

            PublishPreviewIfNeeded(force: true, visible: false);

            ClearPreview();

            PaintedAliveNetworkGameplayBridge.NotifyLocalOilStroke(
                networkStrokeId,
                committedPoints,
                committedShape,
                committedProfile);
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
            if (state == BrushState.Previewing)
            {
                PublishPreviewIfNeeded(force: true, visible: false);
            }

            pigmentReservoir.SetConsuming(false);
            state = BrushState.Idle;

            ClearPreview();
        }

        private void CancelCurrentInteraction()
        {
            if (state == BrushState.Previewing)
            {
                PublishPreviewIfNeeded(force: true, visible: false);
            }

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

        private void PublishPreviewIfNeeded(bool force, bool visible)
        {
            if (!PaintedAliveNetworkGameplayBridge.NetworkSessionActive)
                return;

            if (!force &&
                (Time.unscaledTime < nextPreviewNetworkAt ||
                 previewPoints.Count == lastPreviewNetworkPointCount))
            {
                return;
            }

            float width = strokeSystem != null
                ? strokeSystem.GetPreviewWidth(activeShape, CurrentPressureProfile) * 0.65f
                : 0.2f;

            PaintedAliveNetworkGameplayBridge.NotifyLocalOilPreview(
                localPreviewId,
                visible,
                visible ? previewPoints.ToArray() : System.Array.Empty<Vector3>(),
                activeShape,
                width);

            nextPreviewNetworkAt = Time.unscaledTime + PreviewNetworkInterval;
            lastPreviewNetworkPointCount = previewPoints.Count;
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
