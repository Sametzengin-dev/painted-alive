using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Painters
{
    [DisallowMultipleComponent]
    public sealed class PainterPaintMoundController : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        [Header("Dependencies")]
        [SerializeField] private Camera outputCamera;
        [SerializeField] private PaintMoundSystem moundSystem;
        [SerializeField] private PainterPigmentReservoir pigmentReservoir;
        [SerializeField] private PainterBrushController brushController;

        [Header("Input")]
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private InputActionReference moundAction;
        [SerializeField] private InputActionReference clearAction;

        [Header("Surface Detection")]
        [SerializeField] private LayerMask paintSurfaceMask;

        [SerializeField, Min(1f)]
        private float maximumRayDistance = 100f;

        [Header("Protected Figure Zone")]
        [SerializeField] private LayerMask forbiddenZoneMask;

        [SerializeField, Min(0f)]
        private float forbiddenRadius = 1.1f;

        [Header("Preview")]
        [SerializeField] private Material previewMaterial;

        [SerializeField] private Color chargingColor =
            new(0.75f, 0.12f, 0.16f, 0.45f);

        [SerializeField] private Color readyColor =
            new(1f, 0.65f, 0.22f, 0.65f);

        [SerializeField] private Color blockedColor =
            new(1f, 0.08f, 0.04f, 0.55f);

        private GameObject previewObject;
        private Mesh previewMesh;
        private MeshRenderer previewRenderer;
        private MaterialPropertyBlock previewProperties;

        private bool isCharging;
        private bool hasValidTarget;
        private float chargeElapsed;
        private Vector3 targetPoint;
        private Vector3 targetNormal = Vector3.up;

        public bool IsCharging => isCharging;

        public float ChargeNormalized =>
            moundSystem != null && moundSystem.Config != null
                ? moundSystem.Config.GetChargeNormalized(chargeElapsed)
                : 0f;

        public float EstimatedPigmentCost =>
            moundSystem != null && moundSystem.Config != null
                ? moundSystem.Config.GetPigmentCost(ChargeNormalized)
                : 0f;

        public float PreviewRadius =>
            moundSystem != null && moundSystem.Config != null
                ? moundSystem.Config.GetRadius(ChargeNormalized)
                : 0f;

        public float PreviewHeight =>
            moundSystem != null && moundSystem.Config != null
                ? moundSystem.Config.GetHeight(ChargeNormalized)
                : 0f;

        public bool IsReady =>
            isCharging &&
            hasValidTarget &&
            moundSystem != null &&
            moundSystem.Config != null &&
            chargeElapsed >= moundSystem.Config.MinimumHoldDuration &&
            moundSystem.CanPlace(out _) &&
            pigmentReservoir != null &&
            pigmentReservoir.CanAfford(EstimatedPigmentCost);

        private void Awake()
        {
            CreatePreviewObject();
            CancelCharge();
        }

        private void OnEnable()
        {
            SetActionEnabled(pointerPositionAction, true);
            SetActionEnabled(moundAction, true);
        }

        private void OnDisable()
        {
            CancelCharge();
            SetActionEnabled(moundAction, false);
        }

        private void Update()
        {
            if (moundSystem == null ||
                moundSystem.Config == null ||
                pigmentReservoir == null)
            {
                return;
            }

            if (clearAction != null &&
                clearAction.action != null &&
                clearAction.action.WasPressedThisFrame())
            {
                CancelCharge();
                moundSystem.ClearAllMounds();
                return;
            }

            if (moundAction == null || moundAction.action == null)
                return;

            if (!isCharging &&
                moundAction.action.WasPressedThisFrame())
            {
                TryBeginCharge();
            }

            if (isCharging && moundAction.action.IsPressed())
            {
                UpdateCharge();
            }

            if (isCharging &&
                moundAction.action.WasReleasedThisFrame())
            {
                FinishCharge();
            }
        }

        private void TryBeginCharge()
        {
            if (brushController != null && brushController.IsPreviewing)
                return;

            if (!moundSystem.CanPlace(out _))
                return;

            if (!TryGetTarget(out targetPoint, out targetNormal))
                return;

            isCharging = true;
            chargeElapsed = 0f;
            hasValidTarget = IsTargetAllowed(targetPoint);
            pigmentReservoir.SetConsuming(true);

            if (previewObject != null)
                previewObject.SetActive(true);

            RefreshPreview();
        }

        private void UpdateCharge()
        {
            chargeElapsed = Mathf.Min(
                moundSystem.Config.MaximumChargeDuration,
                chargeElapsed + Time.deltaTime);

            if (TryGetTarget(out Vector3 point, out Vector3 normal))
            {
                targetPoint = point;
                targetNormal = normal;
                hasValidTarget = IsTargetAllowed(targetPoint);
            }
            else
            {
                hasValidTarget = false;
            }

            RefreshPreview();
        }

        private void FinishCharge()
        {
            if (!IsReady)
            {
                CancelCharge();
                return;
            }

            float cost = EstimatedPigmentCost;

            bool created = moundSystem.TryCreateMound(
                targetPoint,
                targetNormal,
                ChargeNormalized,
                out PaintMoundRuntime mound);

            if (!created)
            {
                CancelCharge();
                return;
            }

            if (!pigmentReservoir.TrySpend(cost))
            {
                moundSystem.RollbackLastPlacement(mound);
                CancelCharge();
                return;
            }

            CancelCharge();
        }

        private void RefreshPreview()
        {
            if (previewObject == null)
                return;

            Vector3 normal =
                targetNormal.sqrMagnitude > 0.001f
                    ? targetNormal.normalized
                    : Vector3.up;

            previewObject.transform.SetPositionAndRotation(
                targetPoint +
                normal * moundSystem.Config.SurfaceOffset,
                Quaternion.FromToRotation(Vector3.up, normal));

            previewObject.transform.localScale =
                new Vector3(
                    PreviewRadius,
                    PreviewHeight,
                    PreviewRadius);

            bool canAfford =
                pigmentReservoir.CanAfford(EstimatedPigmentCost);

            bool systemAllows = moundSystem.CanPlace(out _);

            Color color =
                !hasValidTarget || !canAfford || !systemAllows
                    ? blockedColor
                    : IsReady
                        ? readyColor
                        : chargingColor;

            if (previewRenderer != null)
            {
                previewRenderer.GetPropertyBlock(previewProperties);
                previewProperties.SetColor(BaseColorId, color);
                previewRenderer.SetPropertyBlock(previewProperties);
            }
        }

        private bool TryGetTarget(
            out Vector3 point,
            out Vector3 normal)
        {
            point = default;
            normal = Vector3.up;

            if (outputCamera == null ||
                pointerPositionAction == null ||
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

            point = hit.point;
            normal = hit.normal;
            return true;
        }

        private bool IsTargetAllowed(Vector3 point)
        {
            return !Physics.CheckSphere(
                point,
                forbiddenRadius,
                forbiddenZoneMask,
                QueryTriggerInteraction.Collide);
        }

        private void CancelCharge()
        {
            isCharging = false;
            hasValidTarget = false;
            chargeElapsed = 0f;

            if (pigmentReservoir != null)
                pigmentReservoir.SetConsuming(false);

            if (previewObject != null)
                previewObject.SetActive(false);
        }

        private void CreatePreviewObject()
        {
            if (previewObject != null)
                return;

            previewObject = new GameObject("PaintMoundPreview_Runtime");
            previewObject.transform.SetParent(transform, true);

            var filter = previewObject.AddComponent<MeshFilter>();
            previewRenderer = previewObject.AddComponent<MeshRenderer>();

            previewMesh = PaintMoundMeshUtility.CreateDomeMesh(
                1f,
                1f,
                16,
                6,
                "PaintMoundPreviewMesh");

            filter.sharedMesh = previewMesh;
            previewRenderer.sharedMaterial = previewMaterial;
            previewRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            previewRenderer.receiveShadows = false;

            previewProperties = new MaterialPropertyBlock();
            previewObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (previewMesh != null)
                Destroy(previewMesh);
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
