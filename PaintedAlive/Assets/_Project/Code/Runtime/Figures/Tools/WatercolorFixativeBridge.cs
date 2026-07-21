using PaintedAlive.Paint;
using PaintedAlive.Paint.Watercolor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FixativeSprayController))]
    public sealed class WatercolorFixativeBridge : MonoBehaviour
    {
        private const int MaximumHits = 32;
        private const float IdleTargetRefreshInterval = 0.08f;
        private const float SprayingTargetRefreshInterval = 0.05f;

        private readonly RaycastHit[] hits =
            new RaycastHit[MaximumHits];

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
        private FixativeSprayConfig config;

        [SerializeField]
        private FixativeSprayFeedback feedback;

        [Header("Watercolor Detection")]
        [SerializeField]
        private LayerMask watercolorMask =
            Physics.DefaultRaycastLayers;

        [SerializeField]
        private Color fixedWatercolorColor =
            new Color(0.72f, 0.93f, 1f, 0.82f);

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int frozenSurfaceCount;

        [SerializeField]
        private WatercolorFixedState lastFrozenSurface;

        [SerializeField]
        private WatercolorFlowSurface currentTarget;

        [SerializeField]
        private string detectionStatus = "AKIŞ YOK";

        [SerializeField]
        private bool fixativeIsActive;

        [SerializeField]
        private bool inputIsHeld;

        [SerializeField]
        private int activeSurfaceCount;

        [SerializeField]
        private float currentTargetDistance;

        private float nextApplicationTime;
        private float nextTargetRefreshTime;
        private Vector3 currentTargetPoint;
        private Vector3 currentTargetNormal = Vector3.up;

        public int FrozenSurfaceCount => frozenSurfaceCount;
        public WatercolorFixedState LastFrozenSurface =>
            lastFrozenSurface;
        public WatercolorFlowSurface CurrentTarget => currentTarget;
        public string DetectionStatus => detectionStatus;
        public bool FixativeIsActive => fixativeIsActive;
        public bool InputIsHeld => inputIsHeld;
        public int ActiveSurfaceCount => activeSurfaceCount;
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
                config == null)
            {
                Debug.LogError(
                    $"{nameof(WatercolorFixativeBridge)} on {name} " +
                    "requires Fixative Controller, Camera, Input " +
                    "Action and Config references.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            fixativeIsActive =
                fixativeController != null &&
                fixativeController.isActiveAndEnabled;
            inputIsHeld =
                useToolAction != null &&
                useToolAction.action != null &&
                useToolAction.action.IsPressed();
            activeSurfaceCount =
                WatercolorFlowSurface.ActiveSurfaces.Count;

            if (!fixativeIsActive)
            {
                ClearCurrentTarget();
                detectionStatus = "SABİTLEYİCİ AKTİF DEĞİL";
                return;
            }

            if (activeSurfaceCount <= 0)
            {
                ClearCurrentTarget();
                detectionStatus = "AKTİF AKIŞ YOK";
                return;
            }

            if (Time.unscaledTime >= nextTargetRefreshTime ||
                (currentTarget != null &&
                 !currentTarget.isActiveAndEnabled))
            {
                RefreshCurrentTarget();
                nextTargetRefreshTime =
                    Time.unscaledTime +
                    (inputIsHeld
                        ? SprayingTargetRefreshInterval
                        : IdleTargetRefreshInterval);
            }

            if (currentTarget == null)
            {
                return;
            }

            if (!inputIsHeld)
            {
                detectionStatus = "HEDEF HAZIR — E BASILI TUT";
                return;
            }

            if (Time.time < nextApplicationTime)
            {
                return;
            }

            if (clarityState != null &&
                !clarityState.CanUsePrimaryTool)
            {
                detectionStatus = "NETLİK YETERSİZ";
                return;
            }

            nextApplicationTime =
                Time.time + config.ApplicationInterval;
            TryFreezeCurrentTarget();
        }

        private void RefreshCurrentTarget()
        {
            Ray aimRay = outputCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.SphereCastNonAlloc(
                aimRay,
                config.CastRadius,
                hits,
                config.MaximumAimDistance,
                watercolorMask,
                QueryTriggerInteraction.Ignore);

            currentTarget = null;
            currentTargetDistance = 0f;
            currentTargetPoint = Vector3.zero;
            currentTargetNormal = Vector3.up;
            float nearestAimDistance = float.PositiveInfinity;
            bool foundInRange = false;
            bool foundOutOfRange = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];

                if (hit.collider == null)
                {
                    continue;
                }

                WatercolorFlowSurface surface =
                    hit.collider.GetComponentInParent<
                        WatercolorFlowSurface>();

                if (surface == null ||
                    !surface.isActiveAndEnabled)
                {
                    continue;
                }

                float toolDistance = Vector3.Distance(
                    GetRangeOrigin(),
                    hit.point);

                if (toolDistance > GetEffectiveReach())
                {
                    foundOutOfRange = true;
                    continue;
                }

                foundInRange = true;

                if (hit.distance >= nearestAimDistance)
                {
                    continue;
                }

                currentTarget = surface;
                currentTargetPoint = hit.point;
                currentTargetNormal =
                    hit.normal.sqrMagnitude > 0.0001f
                        ? hit.normal
                        : Vector3.up;
                currentTargetDistance = toolDistance;
                nearestAimDistance = hit.distance;
            }

            var activeSurfaces =
                WatercolorFlowSurface.ActiveSurfaces;

            for (int i = activeSurfaces.Count - 1; i >= 0; i--)
            {
                WatercolorFlowSurface surface = activeSurfaces[i];

                if (surface == null ||
                    !surface.isActiveAndEnabled)
                {
                    continue;
                }

                Collider surfaceCollider =
                    surface.GetComponentInChildren<Collider>();

                if (surfaceCollider == null ||
                    !surfaceCollider.enabled)
                {
                    continue;
                }

                Bounds bounds = surfaceCollider.bounds;
                Vector3 rangeOrigin = GetRangeOrigin();
                Vector3 boundsRangePoint =
                    bounds.ClosestPoint(rangeOrigin);
                float boundsDistance = Vector3.Distance(
                    rangeOrigin,
                    boundsRangePoint);

                if (boundsDistance > GetEffectiveReach())
                {
                    foundOutOfRange = true;
                    continue;
                }

                foundInRange = true;

                if (surfaceCollider.Raycast(
                        aimRay,
                        out RaycastHit directHit,
                        config.MaximumAimDistance))
                {
                    float directDistance = Vector3.Distance(
                        rangeOrigin,
                        directHit.point);

                    if (directDistance <= GetEffectiveReach() &&
                        directHit.distance < nearestAimDistance)
                    {
                        currentTarget = surface;
                        currentTargetPoint = directHit.point;
                        currentTargetNormal =
                            directHit.normal.sqrMagnitude > 0.0001f
                                ? directHit.normal
                                : Vector3.up;
                        currentTargetDistance = directDistance;
                        nearestAimDistance = directHit.distance;
                    }
                }

                if (TrySampleSurfaceAlongAim(
                        surface,
                        bounds,
                        aimRay,
                        out Vector3 sampledPoint,
                        out Vector3 sampledNormal,
                        out float sampledAimDistance))
                {
                    float sampledRangeDistance = Vector3.Distance(
                        rangeOrigin,
                        sampledPoint);

                    if (sampledRangeDistance <= GetEffectiveReach() &&
                        sampledAimDistance < nearestAimDistance)
                    {
                        currentTarget = surface;
                        currentTargetPoint = sampledPoint;
                        currentTargetNormal = sampledNormal;
                        currentTargetDistance = sampledRangeDistance;
                        nearestAimDistance = sampledAimDistance;
                    }
                }

            }

            if (currentTarget != null)
            {
                detectionStatus = "AKIŞ HEDEFTE";
            }
            else if (activeSurfaceCount <= 0)
            {
                detectionStatus = "AKTİF AKIŞ YOK";
            }
            else if (foundOutOfRange && !foundInRange)
            {
                detectionStatus = "MENZİL DIŞI";
            }
            else
            {
                detectionStatus = "HEDEFİ ORTALA";
            }
        }

        private void ClearCurrentTarget()
        {
            currentTarget = null;
            currentTargetDistance = 0f;
            currentTargetPoint = Vector3.zero;
            currentTargetNormal = Vector3.up;
        }

        private void TryFreezeCurrentTarget()
        {
            WatercolorFlowSurface target = currentTarget;

            if (target == null || !target.isActiveAndEnabled)
            {
                return;
            }

            WatercolorFixedState fixedState =
                target.GetComponent<WatercolorFixedState>();

            if (fixedState == null)
            {
                fixedState = target.gameObject.AddComponent<
                    WatercolorFixedState>();
            }

            if (!fixedState.Freeze(fixedWatercolorColor))
            {
                return;
            }

            frozenSurfaceCount++;
            lastFrozenSurface = fixedState;
            detectionStatus = "AKIŞ SABİTLENDİ";
            feedback?.PlayApplied(
                toolOrigin.position,
                (currentTargetPoint - toolOrigin.position).normalized,
                currentTargetPoint,
                currentTargetNormal,
                1f,
                true);

            Debug.Log(
                "[Watercolor] Sabitleyici akışı dondurdu. " +
                "Yüzey artık yayılmaz, kaydırmaz ve emilemez.",
                target);
        }

        private float GetEffectiveReach()
        {
            return config != null
                ? config.Reach + 1.25f
                : 4.25f;
        }

        private Vector3 GetRangeOrigin()
        {
            if (clarityState != null)
            {
                return clarityState.transform.position +
                       Vector3.up * 0.75f;
            }

            if (fixativeController != null)
            {
                return fixativeController.transform.position +
                       Vector3.up * 0.75f;
            }

            return toolOrigin != null
                ? toolOrigin.position
                : transform.position;
        }

        private bool TrySampleSurfaceAlongAim(
            WatercolorFlowSurface surface,
            Bounds bounds,
            Ray aimRay,
            out Vector3 sampledPoint,
            out Vector3 sampledNormal,
            out float sampledAimDistance)
        {
            sampledPoint = Vector3.zero;
            sampledNormal = Vector3.up;
            sampledAimDistance = float.PositiveInfinity;

            float projectedCenter = Vector3.Dot(
                bounds.center - aimRay.origin,
                aimRay.direction);
            float searchRadius =
                Mathf.Min(6f, bounds.extents.magnitude + 0.75f);
            float startDistance = Mathf.Max(
                0.05f,
                projectedCenter - searchRadius);
            float endDistance = Mathf.Min(
                config.MaximumAimDistance,
                projectedCenter + searchRadius);

            if (endDistance <= startDistance)
            {
                return false;
            }

            // This is a geometric fallback for very thin runtime meshes.
            // It is intentionally sparse because RefreshCurrentTarget is a
            // targeting aid, not a per-frame collision simulation.
            const int sampleCount = 7;
            float bestInfluence = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float normalizedIndex =
                    i / (sampleCount - 1f);
                float distance = Mathf.Lerp(
                    startDistance,
                    endDistance,
                    normalizedIndex);
                Vector3 pointOnAim = aimRay.GetPoint(distance);

                if (!surface.TrySampleFlow(
                        pointOnAim,
                        out _,
                        out float influence,
                        out Vector3 nearestPoint) ||
                    influence <= bestInfluence)
                {
                    continue;
                }

                bestInfluence = influence;
                sampledPoint = nearestPoint;
                sampledNormal = Vector3.up;
                sampledAimDistance = distance;
            }

            return bestInfluence > 0.001f;
        }
    }
}
