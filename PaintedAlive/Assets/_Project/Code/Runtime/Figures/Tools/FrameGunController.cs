using System;
using PaintedAlive.Figures;
using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    public enum FrameGunAnchorSurfaceType
    {
        Standard,
        WetOil,
        DryingOil,
        DryOil,
        FixedOil,
        OilFragment
    }

    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class FrameGunController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private Transform muzzle;

        [SerializeField]
        private Transform ropeSocket;

        [SerializeField]
        private InputActionReference useToolAction;

        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private FrameGunConfig config;

        [SerializeField]
        private FrameGunRopeVisual ropeVisual;

        [SerializeField]
        private FrameGunFeedback feedback;

        [SerializeField]
        private GameObject anchorMarkerPrefab;

        [Header("Anchor Detection")]
        [SerializeField]
        private LayerMask anchorSurfaceMask =
            Physics.DefaultRaycastLayers;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool isAnchored;

        [SerializeField]
        private float ropeLength;

        [SerializeField]
        private float currentDistance;

        [SerializeField, Range(0f, 1f)]
        private float normalizedTension;

        [SerializeField]
        private FrameGunAnchorSurfaceType anchorSurfaceType;

        [SerializeField, Range(0f, 1f)]
        private float surfaceGrip = 1f;

        [SerializeField]
        private bool isAnchorSliding;

        [SerializeField, Min(0f)]
        private float anchorSlipSpeed;

        [SerializeField, Min(0f)]
        private float accumulatedSlipDistance;

        private Transform anchorParent;
        private Collider anchorCollider;
        private Rigidbody anchorBody;
        private Vector3 localAnchorPosition;
        private Vector3 localAnchorNormal = Vector3.forward;
        private Vector3 dynamicAnchorReactionForce;
        private GameObject anchorMarkerInstance;

        private OilStrokeRuntime anchorStroke;
        private OilStrokeFixativeStatus anchorFixativeStatus;
        private OilStrokeStructuralIntegrity anchorIntegrity;

        public bool IsAnchored => isAnchored;
        public float RopeLength => ropeLength;
        public float CurrentDistance => currentDistance;
        public float NormalizedTension => normalizedTension;
        public FrameGunAnchorSurfaceType AnchorSurfaceType =>
            anchorSurfaceType;
        public float SurfaceGrip => surfaceGrip;
        public bool IsAnchorSliding => isAnchorSliding;
        public float AnchorSlipSpeed => anchorSlipSpeed;
        public float AccumulatedSlipDistance =>
            accumulatedSlipDistance;

        public event Action<bool> AnchorStateChanged;

        private void Awake()
        {
            if (muzzle == null)
            {
                muzzle = transform;
            }

            if (ropeSocket == null)
            {
                ropeSocket = muzzle;
            }

            if (clarityState == null)
            {
                clarityState =
                    GetComponentInParent<FigureClarityState>();
            }

            if (figureMotor == null)
            {
                figureMotor =
                    GetComponentInParent<FigureMotor>();
            }

            if (feedback == null)
            {
                feedback = GetComponent<FrameGunFeedback>();
            }

            if (config == null || figureMotor == null)
            {
                Debug.LogError(
                    $"{nameof(FrameGunController)} on {name} " +
                    "requires Config and FigureMotor references.",
                    this);

                enabled = false;
                return;
            }

            ropeVisual?.Configure(config);
            ropeVisual?.SetVisible(false);
        }

        private void OnEnable()
        {
            if (useToolAction != null &&
                useToolAction.action != null)
            {
                useToolAction.action.Enable();
            }

            ropeVisual?.Configure(config);
        }

        private void OnDisable()
        {
            if (useToolAction != null &&
                useToolAction.action != null)
            {
                useToolAction.action.Disable();
            }

            ReleaseAnchor(false, false);
        }

        private void Update()
        {
            if (config == null || figureMotor == null)
            {
                return;
            }

            if (isAnchored &&
                (anchorParent == null || anchorCollider == null))
            {
                ReleaseAnchor(true, true);
                return;
            }

            if (useToolAction != null &&
                useToolAction.action != null &&
                useToolAction.action.WasPressedThisFrame())
            {
                if (isAnchored)
                {
                    ReleaseAnchor(false, true);
                }
                else
                {
                    TryFireAnchor();
                }
            }

            if (!isAnchored)
            {
                return;
            }

            ApplyRopeConstraint(Time.deltaTime);

            if (isAnchored)
            {
                UpdateAnchorSurface(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (!isAnchored ||
                anchorParent == null ||
                ropeSocket == null)
            {
                ropeVisual?.SetVisible(false);
                return;
            }

            ropeVisual?.SetVisible(true);
            ropeVisual?.RenderRope(
                ropeSocket.position,
                GetAnchorPosition(),
                ropeLength,
                normalizedTension,
                isAnchorSliding,
                surfaceGrip);
        }

        private void FixedUpdate()
        {
            if (!isAnchored ||
                anchorParent == null ||
                anchorBody == null ||
                anchorBody.isKinematic ||
                dynamicAnchorReactionForce.sqrMagnitude <=
                0.000001f)
            {
                return;
            }

            anchorBody.AddForceAtPosition(
                dynamicAnchorReactionForce,
                GetAnchorPosition(),
                ForceMode.Force);
        }

        public void ReleaseAnchor()
        {
            ReleaseAnchor(false, true);
        }

        private void TryFireAnchor()
        {
            if (clarityState != null &&
                !clarityState.CanUsePrimaryTool)
            {
                feedback?.PlayRejected(muzzle.position);
                return;
            }

            if (outputCamera == null || muzzle == null)
            {
                return;
            }

            Ray aimRay =
                outputCamera.ViewportPointToRay(
                    new Vector3(0.5f, 0.5f, 0f));

            feedback?.PlayFire(muzzle.position);

            bool foundSurface =
                Physics.SphereCast(
                    aimRay,
                    config.CastRadius,
                    out RaycastHit hit,
                    config.MaximumAimDistance,
                    anchorSurfaceMask,
                    QueryTriggerInteraction.Ignore);

            Debug.DrawRay(
                aimRay.origin,
                aimRay.direction * config.MaximumAimDistance,
                foundSurface ? Color.magenta : Color.red,
                1f);

            if (!foundSurface || hit.collider == null)
            {
                feedback?.PlayRejected(muzzle.position);
                return;
            }

            FigureMotor hitFigure =
                hit.collider.GetComponentInParent<FigureMotor>();

            if (hitFigure != null)
            {
                feedback?.PlayRejected(hit.point);
                return;
            }

            float distance =
                Vector3.Distance(
                    ropeSocket.position,
                    hit.point);

            if (distance < config.MinimumAnchorDistance ||
                distance > config.MaximumRopeLength)
            {
                feedback?.PlayRejected(hit.point);
                return;
            }

            AttachAnchor(hit, distance);
        }

        private void AttachAnchor(
            RaycastHit hit,
            float distance)
        {
            anchorCollider = hit.collider;
            anchorParent = hit.collider.transform;
            anchorBody = hit.rigidbody;
            localAnchorPosition =
                anchorParent.InverseTransformPoint(hit.point);
            localAnchorNormal =
                anchorParent
                    .InverseTransformDirection(hit.normal)
                    .normalized;

            ropeLength =
                Mathf.Clamp(
                    distance * config.InitialLengthMultiplier,
                    config.MinimumRopeLength,
                    config.MaximumRopeLength);

            currentDistance = distance;
            normalizedTension = 0f;
            accumulatedSlipDistance = 0f;
            anchorSlipSpeed = 0f;
            isAnchorSliding = false;
            isAnchored = true;

            ResolveAnchorSurface();

            if (anchorMarkerPrefab != null)
            {
                anchorMarkerInstance =
                    Instantiate(
                        anchorMarkerPrefab,
                        hit.point,
                        Quaternion.FromToRotation(
                            Vector3.forward,
                            hit.normal),
                        anchorParent);

                anchorMarkerInstance.name =
                    "FrameGunAnchor_Runtime";
            }

            ropeVisual?.SetVisible(true);
            feedback?.PlayAttached(hit.point, hit.normal);
            AnchorStateChanged?.Invoke(true);
        }

        private void ResolveAnchorSurface()
        {
            anchorStroke =
                anchorCollider != null
                    ? anchorCollider
                        .GetComponentInParent<OilStrokeRuntime>()
                    : null;

            anchorFixativeStatus =
                anchorStroke != null
                    ? anchorStroke.GetComponent<
                        OilStrokeFixativeStatus>()
                    : null;

            anchorIntegrity =
                anchorStroke != null
                    ? anchorStroke.GetComponent<
                        OilStrokeStructuralIntegrity>()
                    : null;

            if (anchorStroke != null)
            {
                RefreshOilSurfaceState();
                return;
            }

            OilStrokeFragmentRuntime fragment =
                anchorCollider != null
                    ? anchorCollider.GetComponentInParent<
                        OilStrokeFragmentRuntime>()
                    : null;

            anchorSurfaceType =
                fragment != null
                    ? FrameGunAnchorSurfaceType.OilFragment
                    : FrameGunAnchorSurfaceType.Standard;

            surfaceGrip = 1f;
        }

        private void RefreshOilSurfaceState()
        {
            if (anchorStroke == null)
            {
                return;
            }

            float baseGrip;

            switch (anchorStroke.State)
            {
                case OilStrokeState.Wet:
                    baseGrip = config.WetOilGrip;
                    anchorSurfaceType =
                        FrameGunAnchorSurfaceType.WetOil;
                    break;

                case OilStrokeState.Drying:
                    baseGrip = config.DryingOilGrip;
                    anchorSurfaceType =
                        FrameGunAnchorSurfaceType.DryingOil;
                    break;

                default:
                    baseGrip = config.DryOilGrip;
                    anchorSurfaceType =
                        FrameGunAnchorSurfaceType.DryOil;
                    break;
            }

            if (anchorFixativeStatus == null)
            {
                anchorFixativeStatus =
                    anchorStroke.GetComponent<
                        OilStrokeFixativeStatus>();
            }

            float fixativeSaturation =
                anchorFixativeStatus != null
                    ? anchorFixativeStatus.Saturation
                    : 0f;

            if (fixativeSaturation > 0.01f)
            {
                anchorSurfaceType =
                    FrameGunAnchorSurfaceType.FixedOil;
            }

            surfaceGrip =
                Mathf.Lerp(
                    baseGrip,
                    config.FixedOilGrip,
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        fixativeSaturation));
        }

        private void ApplyRopeConstraint(float deltaTime)
        {
            Vector3 anchorPosition = GetAnchorPosition();
            Vector3 socketPosition = ropeSocket.position;
            Vector3 toAnchor = anchorPosition - socketPosition;
            currentDistance = toAnchor.magnitude;

            if (currentDistance <= 0.0001f)
            {
                normalizedTension = 0f;
                dynamicAnchorReactionForce = Vector3.zero;
                return;
            }

            float stretch = currentDistance - ropeLength;

            if (stretch > config.MaximumStretchBeforeBreak)
            {
                ReleaseAnchor(true, true);
                return;
            }

            float effectiveStretch =
                stretch - config.SlackTolerance;

            if (effectiveStretch <= 0f)
            {
                normalizedTension = 0f;
                dynamicAnchorReactionForce = Vector3.zero;
                return;
            }

            Vector3 towardAnchor = toAnchor / currentDistance;
            float radialVelocityTowardAnchor =
                Vector3.Dot(
                    figureMotor.Velocity,
                    towardAnchor);
            float outwardSpeed =
                Mathf.Max(0f, -radialVelocityTowardAnchor);

            float pullAcceleration =
                effectiveStretch * config.SpringAcceleration +
                outwardSpeed * config.RadialDamping;

            pullAcceleration =
                Mathf.Clamp(
                    pullAcceleration,
                    0f,
                    config.MaximumPullAcceleration);

            normalizedTension =
                config.MaximumPullAcceleration > 0f
                    ? pullAcceleration /
                      config.MaximumPullAcceleration
                    : 0f;

            figureMotor.AddExternalImpulse(
                towardAnchor * pullAcceleration * deltaTime);

            if (anchorBody != null &&
                !anchorBody.isKinematic &&
                config.DynamicBodyReactionForceScale > 0f)
            {
                dynamicAnchorReactionForce =
                    -towardAnchor *
                    pullAcceleration *
                    config.DynamicBodyReactionForceScale;
            }
            else
            {
                dynamicAnchorReactionForce = Vector3.zero;
            }
        }

        private void UpdateAnchorSurface(float deltaTime)
        {
            isAnchorSliding = false;
            anchorSlipSpeed = 0f;

            if (anchorStroke == null)
            {
                return;
            }

            if (!TryRefreshOilAnchorContact())
            {
                ReleaseAnchor(true, true);
                return;
            }

            RefreshOilSurfaceState();

            if (ApplyStructuralRopeLoad(deltaTime))
            {
                ReleaseAnchor(true, true);
                return;
            }

            float slipThreshold =
                Mathf.Clamp01(
                    surfaceGrip +
                    config.SlipStartTensionBias);

            if (normalizedTension <= slipThreshold ||
                surfaceGrip >= 0.999f)
            {
                return;
            }

            float slipFactor =
                Mathf.InverseLerp(
                    slipThreshold,
                    1f,
                    normalizedTension);

            anchorSlipSpeed =
                Mathf.Lerp(
                    config.MinimumSlipSpeed,
                    config.MaximumSlipSpeed,
                    slipFactor) *
                Mathf.Lerp(1f, 0.35f, surfaceGrip);

            if (anchorSlipSpeed <= 0f)
            {
                return;
            }

            isAnchorSliding = true;

            if (!TrySlideAnchor(anchorSlipSpeed * deltaTime))
            {
                ReleaseAnchor(true, true);
            }
        }

        private bool TryRefreshOilAnchorContact()
        {
            if (anchorCollider == null ||
                anchorParent == null)
            {
                return false;
            }

            Vector3 anchorPosition = GetAnchorPosition();
            Vector3 surfaceNormal = GetAnchorNormal();
            float probeDistance = config.SurfaceProbeDistance;

            var contactRay =
                new Ray(
                    anchorPosition +
                    surfaceNormal * (probeDistance * 0.5f),
                    -surfaceNormal);

            if (!anchorCollider.Raycast(
                    contactRay,
                    out RaycastHit hit,
                    probeDistance))
            {
                return false;
            }

            SetAnchorContact(hit.point, hit.normal);
            return true;
        }

        private bool TrySlideAnchor(float distance)
        {
            Vector3 anchorPosition = GetAnchorPosition();
            Vector3 surfaceNormal = GetAnchorNormal();
            Vector3 towardSocket =
                ropeSocket.position - anchorPosition;

            Vector3 surfaceDirection =
                Vector3.ProjectOnPlane(
                    towardSocket,
                    surfaceNormal);

            if (surfaceDirection.sqrMagnitude < 0.0001f)
            {
                surfaceDirection =
                    Vector3.ProjectOnPlane(
                        Vector3.down,
                        surfaceNormal);
            }

            if (surfaceDirection.sqrMagnitude < 0.0001f)
            {
                isAnchorSliding = false;
                anchorSlipSpeed = 0f;
                return true;
            }

            surfaceDirection.Normalize();

            Vector3 candidate =
                anchorPosition + surfaceDirection * distance;
            float probeDistance = config.SurfaceProbeDistance;

            var slideRay =
                new Ray(
                    candidate +
                    surfaceNormal * (probeDistance * 0.5f),
                    -surfaceNormal);

            if (!anchorCollider.Raycast(
                    slideRay,
                    out RaycastHit hit,
                    probeDistance))
            {
                return false;
            }

            float travelled =
                Vector3.Distance(anchorPosition, hit.point);

            accumulatedSlipDistance += travelled;

            if (accumulatedSlipDistance >
                config.MaximumContinuousSlipDistance)
            {
                return false;
            }

            SetAnchorContact(hit.point, hit.normal);
            feedback?.PlaySliding(
                hit.point,
                hit.normal,
                normalizedTension);
            return true;
        }

        private bool ApplyStructuralRopeLoad(float deltaTime)
        {
            if (anchorIntegrity == null &&
                anchorStroke != null)
            {
                anchorIntegrity =
                    anchorStroke.GetComponent<
                        OilStrokeStructuralIntegrity>();
            }

            if (anchorIntegrity == null ||
                !anchorIntegrity.IsInitialized ||
                normalizedTension <=
                config.MinimumDamageTension)
            {
                return false;
            }

            float stateBrittleness;

            switch (anchorStroke.State)
            {
                case OilStrokeState.Wet:
                    stateBrittleness = config.WetBrittleness;
                    break;

                case OilStrokeState.Drying:
                    stateBrittleness =
                        config.DryingBrittleness;
                    break;

                default:
                    stateBrittleness = 1f;
                    break;
            }

            float fixativeSaturation =
                anchorFixativeStatus != null
                    ? anchorFixativeStatus.Saturation
                    : 0f;

            float fixativeMultiplier =
                Mathf.Lerp(
                    1f,
                    config.MaximumFixativeBrittlenessMultiplier,
                    fixativeSaturation);

            float tensionFactor =
                Mathf.InverseLerp(
                    config.MinimumDamageTension,
                    1f,
                    normalizedTension);

            float damage =
                config.TensionDamagePerSecond *
                tensionFactor *
                stateBrittleness *
                fixativeMultiplier *
                deltaTime;

            return anchorIntegrity.ApplyExternalDamage(
                damage,
                true);
        }

        private void SetAnchorContact(
            Vector3 worldPosition,
            Vector3 worldNormal)
        {
            if (anchorParent == null)
            {
                return;
            }

            Vector3 safeNormal =
                worldNormal.sqrMagnitude > 0.0001f
                    ? worldNormal.normalized
                    : GetAnchorNormal();

            localAnchorPosition =
                anchorParent.InverseTransformPoint(worldPosition);
            localAnchorNormal =
                anchorParent
                    .InverseTransformDirection(safeNormal)
                    .normalized;

            if (anchorMarkerInstance != null)
            {
                anchorMarkerInstance.transform.SetPositionAndRotation(
                    worldPosition,
                    Quaternion.FromToRotation(
                        Vector3.forward,
                        safeNormal));
            }
        }

        private Vector3 GetAnchorPosition()
        {
            return anchorParent != null
                ? anchorParent.TransformPoint(localAnchorPosition)
                : Vector3.zero;
        }

        private Vector3 GetAnchorNormal()
        {
            if (anchorParent == null)
            {
                return Vector3.forward;
            }

            Vector3 worldNormal =
                anchorParent.TransformDirection(localAnchorNormal);

            return worldNormal.sqrMagnitude > 0.0001f
                ? worldNormal.normalized
                : Vector3.forward;
        }

        private void ReleaseAnchor(
            bool broken,
            bool playFeedback)
        {
            if (!isAnchored)
            {
                ropeVisual?.SetVisible(false);
                return;
            }

            Vector3 releasePosition =
                anchorParent != null
                    ? GetAnchorPosition()
                    : ropeSocket != null
                        ? ropeSocket.position
                        : transform.position;

            if (anchorMarkerInstance != null)
            {
                Destroy(anchorMarkerInstance);
            }

            anchorMarkerInstance = null;
            anchorParent = null;
            anchorCollider = null;
            anchorBody = null;
            localAnchorPosition = Vector3.zero;
            localAnchorNormal = Vector3.forward;
            dynamicAnchorReactionForce = Vector3.zero;
            anchorStroke = null;
            anchorFixativeStatus = null;
            anchorIntegrity = null;
            ropeLength = 0f;
            currentDistance = 0f;
            normalizedTension = 0f;
            anchorSurfaceType =
                FrameGunAnchorSurfaceType.Standard;
            surfaceGrip = 1f;
            isAnchorSliding = false;
            anchorSlipSpeed = 0f;
            accumulatedSlipDistance = 0f;
            isAnchored = false;

            ropeVisual?.SetVisible(false);

            if (playFeedback)
            {
                if (broken)
                {
                    feedback?.PlayBroken(releasePosition);
                }
                else
                {
                    feedback?.PlayReleased(releasePosition);
                }
            }

            AnchorStateChanged?.Invoke(false);
        }
    }
}
