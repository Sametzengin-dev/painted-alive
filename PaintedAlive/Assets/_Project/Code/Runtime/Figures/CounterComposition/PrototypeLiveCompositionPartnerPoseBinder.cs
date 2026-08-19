using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(310)]
    [DisallowMultipleComponent]
    public sealed class PrototypeLiveCompositionPartnerPoseBinder :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeLiveCompositionController liveComposition;

        [SerializeField]
        private PrototypeLiveCompositionConsentHarness consentHarness;

        [SerializeField]
        private Transform partnerProxyRoot;

        [Header("Pose")]
        [SerializeField, Range(0.05f, 0.45f)]
        private float partnerAlongSpanOffsetNormalized = 0.18f;

        [SerializeField, Min(0.05f)]
        private float partnerLateralOffset = 0.58f;

        [SerializeField]
        private float partnerVerticalOffset = 0.42f;

        [SerializeField, Min(1f)]
        private float followSharpness = 18f;

        [Header("Runtime Read Only")]
        [SerializeField]
        private bool partnerPoseBound;

        [SerializeField]
        private bool compositionObserved;

        [SerializeField]
        private bool partnerConsentSessionObserved;

        [SerializeField]
        private int bindCount;

        [SerializeField]
        private int releaseCount;

        [SerializeField]
        private int watercolorFollowFrameCount;

        [SerializeField]
        private Vector3 boundTargetPosition;

        [SerializeField]
        private Vector3 homePosition;

        [SerializeField]
        private Quaternion homeRotation = Quaternion.identity;

        [SerializeField]
        private Vector3 homeScale = Vector3.one;

        [SerializeField]
        private string lastState = "Waiting";

        private bool homeCaptured;
        private LineRenderer participantLink;
        private Material runtimeMaterial;
        private Vector3 previousBridgeBodyPoint;
        private bool previousBodyPointValid;

        public bool PartnerPoseBound => partnerPoseBound;
        public bool CompositionObserved => compositionObserved;
        public bool PartnerConsentSessionObserved =>
            partnerConsentSessionObserved;
        public int BindCount => bindCount;
        public int ReleaseCount => releaseCount;
        public int WatercolorFollowFrameCount =>
            watercolorFollowFrameCount;
        public Vector3 BoundTargetPosition =>
            boundTargetPosition;
        public string LastState => lastState;

        public bool PartnerProxyPhysicalContributionEnabled => true;
        public bool PartnerProxyFollowsWatercolorDeformation => true;
        public bool IndependentWithdrawalRestoresPartnerPose => true;
        public bool FigureReleaseRestoresPartnerPose => true;
        public bool PartnerProxyHasGameplayCollider => false;
        public bool PartnerProxyAffectsBridgeCollision => false;
        public bool RealSecondFigureMotorBound => false;
        public bool AutomaticNearbyPlayerBindingEnabled => false;
        public bool IKEnabled => false;
        public bool RagdollEnabled => false;
        public bool AddsNewGameplayInputAction => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeLiveCompositionController configuredLiveComposition,
            PrototypeLiveCompositionConsentHarness configuredConsentHarness,
            Transform configuredPartnerProxyRoot)
        {
            liveComposition =
                configuredLiveComposition;

            consentHarness =
                configuredConsentHarness;

            partnerProxyRoot =
                configuredPartnerProxyRoot;

            CaptureHomePose();
            EnsureLinkVisual();
            RefreshLinkVisual();
        }

        private void Awake()
        {
            CaptureHomePose();
            EnsureLinkVisual();
            RefreshLinkVisual();
        }

        private void OnEnable()
        {
            CaptureHomePose();
        }

        private void Update()
        {
            bool compositionActive =
                liveComposition != null &&
                liveComposition.Composing &&
                liveComposition.ActiveBridge != null &&
                liveComposition.ActiveBridge.ActiveBridge;

            bool consentActive =
                consentHarness != null &&
                consentHarness.State ==
                    PrototypeLiveCompositionConsentState.Composing;

            compositionObserved =
                compositionActive;

            partnerConsentSessionObserved =
                consentActive;

            if (
                compositionActive &&
                consentActive
            )
            {
                if (!partnerPoseBound)
                {
                    BindPartnerPose();
                }

                FollowCompositionPose();
            }
            else if (partnerPoseBound)
            {
                ReleasePartnerPose(
                    compositionActive
                        ? "Partner consent session ended"
                        : "Composition ended");
            }

            RefreshLinkVisual();
        }

        private void CaptureHomePose()
        {
            if (
                homeCaptured ||
                partnerProxyRoot == null
            )
            {
                return;
            }

            homePosition =
                partnerProxyRoot.position;

            homeRotation =
                partnerProxyRoot.rotation;

            homeScale =
                partnerProxyRoot.localScale;

            homeCaptured = true;
        }

        private void BindPartnerPose()
        {
            if (
                partnerProxyRoot == null ||
                liveComposition == null ||
                liveComposition.ActiveBridge == null
            )
            {
                lastState =
                    "Bind failed: source missing";

                return;
            }

            CaptureHomePose();

            partnerPoseBound = true;
            bindCount++;

            previousBodyPointValid = false;

            lastState =
                "Partner proxy physically bound";
        }

        private void FollowCompositionPose()
        {
            if (
                !partnerPoseBound ||
                partnerProxyRoot == null ||
                liveComposition == null ||
                liveComposition.ActiveBridge == null
            )
            {
                return;
            }

            PrototypeLiveCompositionBridge bridge =
                liveComposition.ActiveBridge;

            Vector3 anchorA =
                bridge.AnchorPointA;

            Vector3 anchorB =
                bridge.AnchorPointB;

            Vector3 body =
                bridge.BodyPoint;

            Vector3 span =
                anchorB -
                anchorA;

            Vector3 planarSpan =
                Vector3.ProjectOnPlane(
                    span,
                    Vector3.up);

            Vector3 spanDirection =
                planarSpan.sqrMagnitude >
                0.001f
                    ? planarSpan.normalized
                    : Vector3.forward;

            Vector3 lateral =
                Vector3.Cross(
                    Vector3.up,
                    spanDirection);

            if (
                lateral.sqrMagnitude <
                0.001f
            )
            {
                lateral =
                    Vector3.right;
            }
            else
            {
                lateral.Normalize();
            }

            float halfSpan =
                span.magnitude *
                0.5f;

            float alongOffset =
                Mathf.Min(
                    halfSpan *
                    0.65f,
                    span.magnitude *
                    partnerAlongSpanOffsetNormalized);

            boundTargetPosition =
                body +
                spanDirection *
                alongOffset +
                lateral *
                partnerLateralOffset +
                Vector3.up *
                partnerVerticalOffset;

            float blend =
                1f -
                Mathf.Exp(
                    -Mathf.Max(
                        1f,
                        followSharpness) *
                    Mathf.Max(
                        0f,
                        Time.deltaTime));

            partnerProxyRoot.position =
                Vector3.Lerp(
                    partnerProxyRoot.position,
                    boundTargetPosition,
                    blend);

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    spanDirection,
                    Vector3.up);

            partnerProxyRoot.rotation =
                Quaternion.Slerp(
                    partnerProxyRoot.rotation,
                    targetRotation,
                    blend);

            if (previousBodyPointValid)
            {
                if (
                    Vector3.Distance(
                        previousBridgeBodyPoint,
                        body) >
                    0.001f
                )
                {
                    watercolorFollowFrameCount++;
                }
            }

            previousBridgeBodyPoint =
                body;

            previousBodyPointValid = true;

            lastState =
                "Bound / following composition body";
        }

        public void ForceReleaseForPrototype(
            string source)
        {
            if (!partnerPoseBound)
            {
                return;
            }

            ReleasePartnerPose(
                source);
        }

        private void ReleasePartnerPose(
            string reason)
        {
            if (!partnerPoseBound)
            {
                return;
            }

            partnerPoseBound = false;
            previousBodyPointValid = false;

            if (
                partnerProxyRoot != null &&
                homeCaptured
            )
            {
                partnerProxyRoot.position =
                    homePosition;

                partnerProxyRoot.rotation =
                    homeRotation;

                partnerProxyRoot.localScale =
                    homeScale;
            }

            releaseCount++;

            lastState =
                string.IsNullOrWhiteSpace(
                    reason)
                    ? "Released / home restored"
                    : $"Released / home restored: {reason}";
        }

        private void EnsureLinkVisual()
        {
            if (
                participantLink != null ||
                partnerProxyRoot == null
            )
            {
                return;
            }

            Shader shader =
                Shader.Find(
                    "Sprites/Default");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                runtimeMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M53_3_PartnerPoseLink_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject linkObject =
                new GameObject(
                    "PartnerPoseLink");

            linkObject.transform.SetParent(
                transform,
                false);

            participantLink =
                linkObject.AddComponent<
                    LineRenderer>();

            participantLink.useWorldSpace = true;
            participantLink.loop = false;
            participantLink.positionCount = 2;
            participantLink.widthMultiplier = 0.055f;
            participantLink.numCapVertices = 3;
            participantLink.numCornerVertices = 3;
            participantLink.alignment =
                LineAlignment.View;
            participantLink.shadowCastingMode =
                ShadowCastingMode.Off;
            participantLink.receiveShadows = false;
            participantLink.sharedMaterial =
                runtimeMaterial;

            Color color =
                new Color(
                    0.82f,
                    0.94f,
                    0.54f,
                    0.94f);

            participantLink.startColor =
                color;
            participantLink.endColor =
                color;
            participantLink.enabled = false;
        }

        private void RefreshLinkVisual()
        {
            if (participantLink == null)
            {
                return;
            }

            bool visible =
                partnerPoseBound &&
                partnerProxyRoot != null &&
                liveComposition != null &&
                liveComposition.ActiveBridge != null &&
                liveComposition.ActiveBridge.ActiveBridge;

            participantLink.enabled =
                visible;

            if (!visible)
            {
                return;
            }

            Vector3 bodyPoint =
                liveComposition
                    .ActiveBridge
                    .BodyPoint;

            participantLink.SetPosition(
                0,
                bodyPoint +
                Vector3.up *
                0.42f);

            participantLink.SetPosition(
                1,
                partnerProxyRoot.position +
                Vector3.up *
                0.65f);
        }

        private void OnDisable()
        {
            if (partnerPoseBound)
            {
                ReleasePartnerPose(
                    "Binder disabled");
            }
        }

        private void OnDestroy()
        {
            if (partnerPoseBound)
            {
                ReleasePartnerPose(
                    "Binder destroyed");
            }

            if (runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(
                    runtimeMaterial);
            }
            else
            {
                DestroyImmediate(
                    runtimeMaterial);
            }

            runtimeMaterial = null;
        }
    }
}
