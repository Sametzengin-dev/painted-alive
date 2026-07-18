using System;
using PaintedAlive.Figures;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
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

        private Transform anchorParent;
        private Rigidbody anchorBody;
        private Vector3 localAnchorPosition;
        private Vector3 dynamicAnchorReactionForce;
        private GameObject anchorMarkerInstance;

        public bool IsAnchored => isAnchored;
        public float RopeLength => ropeLength;
        public float CurrentDistance => currentDistance;
        public float NormalizedTension => normalizedTension;

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
                    GetComponentInParent<
                        FigureClarityState>();
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

            if (isAnchored && anchorParent == null)
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

            if (isAnchored)
            {
                ApplyRopeConstraint(Time.deltaTime);
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
                normalizedTension);
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
                aimRay.direction *
                config.MaximumAimDistance,
                foundSurface ? Color.magenta : Color.red,
                1f);

            if (!foundSurface || hit.collider == null)
            {
                feedback?.PlayRejected(muzzle.position);
                return;
            }

            FigureMotor hitFigure =
                hit.collider
                    .GetComponentInParent<FigureMotor>();

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
            anchorParent = hit.collider.transform;
            anchorBody = hit.rigidbody;

            localAnchorPosition =
                anchorParent.InverseTransformPoint(hit.point);

            ropeLength =
                Mathf.Clamp(
                    distance * config.InitialLengthMultiplier,
                    config.MinimumRopeLength,
                    config.MaximumRopeLength);

            currentDistance = distance;
            normalizedTension = 0f;
            isAnchored = true;

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

            Vector3 towardAnchor =
                toAnchor / currentDistance;

            float radialVelocityTowardAnchor =
                Vector3.Dot(
                    figureMotor.Velocity,
                    towardAnchor);

            float outwardSpeed =
                Mathf.Max(
                    0f,
                    -radialVelocityTowardAnchor);

            float pullAcceleration =
                effectiveStretch *
                config.SpringAcceleration +
                outwardSpeed *
                config.RadialDamping;

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
                towardAnchor *
                pullAcceleration *
                deltaTime);

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

        private Vector3 GetAnchorPosition()
        {
            return anchorParent != null
                ? anchorParent.TransformPoint(
                    localAnchorPosition)
                : Vector3.zero;
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
            anchorBody = null;
            localAnchorPosition = Vector3.zero;
            dynamicAnchorReactionForce = Vector3.zero;
            ropeLength = 0f;
            currentDistance = 0f;
            normalizedTension = 0f;
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
