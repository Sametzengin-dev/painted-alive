using System;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(290)]
    [DisallowMultipleComponent]
    public sealed class PrototypeLiveCompositionController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private Transform figureRoot;

        [SerializeField]
        private MonoBehaviour figureMotor;

        [SerializeField]
        private PrototypeUnderlayerFrameBraceController frameGunSource;

        [SerializeField]
        private MonoBehaviour clarityState;

        [SerializeField]
        private PrototypeLiveCompositionConsentHarness consentHarness;

        [SerializeField]
        private PrototypeLiveCompositionPainterCounterplayController painterCounterplayHarness;

        [Header("Composition")]
        [SerializeField]
        private PrototypeLiveCompositionAnchor anchorA;

        [SerializeField]
        private PrototypeLiveCompositionAnchor anchorB;

        [SerializeField]
        private Transform posePoint;

        [SerializeField, Min(0.5f)]
        private float anchorUseRadius = 2.25f;

        [SerializeField, Min(0.5f)]
        private float poseCommitRadius = 1.25f;

        [SerializeField, Min(1f)]
        private float maximumAnchorSpan = 7.0f;

        [SerializeField, Min(0.5f)]
        private float compositionDurationSeconds = 7.0f;

        [SerializeField, Min(0.1f)]
        private float rearmDelaySeconds = 0.35f;

        [Header("HUD")]
        [SerializeField]
        private CanvasGroup statusGroup;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Text controlsText;

        [Header("Focus")]
        [SerializeField, Min(0f)]
        private float contextualHudHoldSeconds = 3f;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeLiveCompositionState state =
            PrototypeLiveCompositionState.WaitingForFigure;

        [SerializeField]
        private bool figureRoleActive;

        [SerializeField]
        private bool frameGunSelected;

        [SerializeField]
        private bool useToolInputResolved;

        [SerializeField]
        private bool primaryToolAllowed = true;

        [SerializeField]
        private bool fullStain;

        [SerializeField]
        private bool composing;

        [SerializeField]
        private bool motorSuppressed;

        [SerializeField]
        private bool explicitConsentReceived;

        [SerializeField]
        private bool releaseInputAvailable;

        [SerializeField]
        private int anchorLatchCount;

        [SerializeField]
        private int compositionStartCount;

        [SerializeField]
        private int compositionReleaseCount;

        [SerializeField]
        private int safetyReleaseCount;

        [SerializeField]
        private int rejectedActionCount;

        [SerializeField]
        private int partnerWithdrawalReleaseCount;

        [SerializeField]
        private int partnerGateRejectCount;

        [SerializeField]
        private float nearestAnchorDistance = -1f;

        [SerializeField]
        private float poseDistance = -1f;

        [SerializeField]
        private float remainingCompositionSeconds;

        [SerializeField]
        private string nearestAnchor = "None";

        [SerializeField]
        private string clarityLevel = "Unknown";

        [SerializeField]
        private string resolvedRole = "Unknown";

        [SerializeField]
        private string inputContract = "Unresolved";

        [SerializeField]
        private string lastAction =
            "Canlı Kompozisyon ankrajları bekleniyor.";

        private PrototypeLiveCompositionAnchor nearestAnchorReference;
        private PrototypeLiveCompositionBridge activeBridge;

        private InputActionReference sharedUseToolAction;

        private PropertyInfo clarityLevelProperty;

        private bool savedMotorEnabled;
        private bool motorStateCaptured;
        private float compositionEndsAt;
        private float nextActionAllowedAt;
        private float hudVisibleUntilUnscaled;

        public PrototypeLiveCompositionState State => state;
        public bool FigureRoleActive => figureRoleActive;
        public bool FrameGunSelected => frameGunSelected;
        public bool UseToolInputResolved => useToolInputResolved;
        public bool PrimaryToolAllowed => primaryToolAllowed;
        public bool FullStain => fullStain;
        public bool Composing => composing;
        public bool MotorSuppressed => motorSuppressed;
        public bool ExplicitConsentReceived =>
            explicitConsentReceived;
        public bool ReleaseInputAvailable =>
            releaseInputAvailable;
        public int AnchorLatchCount => anchorLatchCount;
        public int CompositionStartCount =>
            compositionStartCount;
        public int CompositionReleaseCount =>
            compositionReleaseCount;
        public int SafetyReleaseCount =>
            safetyReleaseCount;
        public int RejectedActionCount =>
            rejectedActionCount;
        public int PartnerWithdrawalReleaseCount =>
            partnerWithdrawalReleaseCount;
        public int PartnerGateRejectCount =>
            partnerGateRejectCount;
        public float NearestAnchorDistance =>
            nearestAnchorDistance;
        public float PoseDistance => poseDistance;
        public float RemainingCompositionSeconds =>
            remainingCompositionSeconds;
        public string NearestAnchor => nearestAnchor;
        public string ClarityLevel => clarityLevel;
        public string ResolvedRole => resolvedRole;
        public string InputContract => inputContract;
        public string LastAction => lastAction;
        public PrototypeLiveCompositionBridge ActiveBridge =>
            activeBridge;
        public PrototypeLiveCompositionConsentHarness ConsentHarness =>
            consentHarness;
        public PrototypeLiveCompositionPainterCounterplayController PainterCounterplayHarness =>
            painterCounterplayHarness;
        public Transform FigureRoot => figureRoot;
        public Transform PosePoint => posePoint;

        public bool SingleFigureTemplateEnabled => true;
        public bool BridgeTemplateEnabled => true;
        public bool TwoEnvironmentalAnchorsRequired => true;
        public bool ExplicitParticipationConsentRequired => true;
        public bool SingleInputReleaseEnabled => true;
        public bool AutomaticNearbyPlayerBindingEnabled => false;
        public bool RagdollCompositionEnabled => false;
        public bool FreeformPhysicsCompositionEnabled => false;
        public bool UsesExistingFrameGunUseToolReadOnly => true;
        public bool AddsNewGameplayInputAction => false;
        public bool MultiplayerCompositionEnabled => false;
        public bool LocalTwoParticipantConsentHarnessSupported => true;
        public bool PartnerConsentIsPerSession => true;
        public bool StalePartnerConsentReusable => false;
        public bool ParticipantCanWithdrawIndependently => true;
        public bool PainterConnectionCounterplaySupported => true;
        public bool LocalPainterRoleSwitchBypassOnlyForCounterplayTest => true;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            Transform configuredFigureRoot,
            MonoBehaviour configuredFigureMotor,
            PrototypeUnderlayerFrameBraceController configuredFrameGunSource,
            MonoBehaviour configuredClarityState,
            PrototypeLiveCompositionAnchor configuredAnchorA,
            PrototypeLiveCompositionAnchor configuredAnchorB,
            Transform configuredPosePoint,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            figureRoot = configuredFigureRoot;
            figureMotor = configuredFigureMotor;
            frameGunSource = configuredFrameGunSource;
            clarityState = configuredClarityState;
            anchorA = configuredAnchorA;
            anchorB = configuredAnchorB;
            posePoint = configuredPosePoint;

            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolveSharedInput();
            ResolveClarityContract();
            RefreshHud();
        }

        public void ConfigureConsentHarness(
            PrototypeLiveCompositionConsentHarness configuredConsentHarness)
        {
            consentHarness =
                configuredConsentHarness;

            if (consentHarness != null)
            {
                consentHarness.BindController(
                    this);
            }

            RefreshHud();
        }

        public void ConfigurePainterCounterplayHarness(
            PrototypeLiveCompositionPainterCounterplayController configuredHarness)
        {
            painterCounterplayHarness =
                configuredHarness;

            RefreshHud();
        }

        private void Awake()
        {
            ResolveSharedInput();
            ResolveClarityContract();
        }

        private void OnEnable()
        {
            ResolveSharedInput();
            ResolveClarityContract();
        }

        private void Update()
        {
            RefreshAuthority();
            RefreshClarity();
            RefreshFrameGunContract();
            RefreshNearestAnchor();
            RefreshPoseDistance();

            if (composing)
            {
                TickComposition();
            }
            else
            {
                ProcessSetupInput();
            }

            RefreshState();
            RefreshHud();
        }

        public bool ForceLatchA()
        {
            return LatchAnchor(
                anchorA,
                "Editor force A");
        }

        public bool ForceLatchB()
        {
            return LatchAnchor(
                anchorB,
                "Editor force B");
        }

        public bool ForceStartComposition()
        {
            explicitConsentReceived = true;
            return StartComposition(
                "Editor force");
        }

        public void ForceReleaseComposition()
        {
            if (!composing)
            {
                return;
            }

            ReleaseComposition(
                "Editor force release",
                safety: false);
        }

        private void RefreshAuthority()
        {
            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedRole);
        }

        private void ResolveSharedInput()
        {
            sharedUseToolAction = null;
            useToolInputResolved = false;

            if (frameGunSource == null)
            {
                inputContract =
                    "M52.2 frame source missing";

                return;
            }

            Type type =
                frameGunSource.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            FieldInfo field =
                type.GetField(
                    "useToolAction",
                    flags);

            if (
                field == null ||
                !typeof(InputActionReference)
                    .IsAssignableFrom(
                        field.FieldType)
            )
            {
                inputContract =
                    "M52.2 shared useToolAction unresolved";

                return;
            }

            try
            {
                sharedUseToolAction =
                    field.GetValue(
                        frameGunSource)
                    as InputActionReference;
            }
            catch (Exception)
            {
                sharedUseToolAction = null;
            }

            useToolInputResolved =
                sharedUseToolAction != null &&
                sharedUseToolAction.action != null;

            inputContract =
                useToolInputResolved
                    ? "M52.2 existing Figure UseTool read-only"
                    : "M52.2 UseTool action unavailable";
        }

        private void RefreshFrameGunContract()
        {
            if (frameGunSource == null)
            {
                frameGunSelected = false;
                primaryToolAllowed = true;
                return;
            }

            frameGunSelected =
                frameGunSource.FrameGunSelected;

            primaryToolAllowed =
                frameGunSource.PrimaryToolAllowed;

            if (
                sharedUseToolAction == null ||
                sharedUseToolAction.action == null
            )
            {
                ResolveSharedInput();
            }

            useToolInputResolved =
                sharedUseToolAction != null &&
                sharedUseToolAction.action != null;
        }

        private void ResolveClarityContract()
        {
            clarityLevelProperty = null;

            if (clarityState == null)
            {
                return;
            }

            clarityLevelProperty =
                clarityState
                    .GetType()
                    .GetProperty(
                        "CurrentLevel",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);
        }

        private void RefreshClarity()
        {
            clarityLevel = "Unknown";
            fullStain = false;

            if (
                clarityState == null ||
                clarityLevelProperty == null
            )
            {
                ResolveClarityContract();
                return;
            }

            try
            {
                object value =
                    clarityLevelProperty.GetValue(
                        clarityState);

                clarityLevel =
                    value != null
                        ? value.ToString()
                        : "Unknown";

                fullStain =
                    string.Equals(
                        clarityLevel,
                        "Stain",
                        StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                clarityLevel = "Unknown";
                fullStain = false;
            }
        }

        private void RefreshNearestAnchor()
        {
            nearestAnchorReference = null;
            nearestAnchorDistance =
                float.PositiveInfinity;
            nearestAnchor = "None";

            if (figureRoot == null)
            {
                nearestAnchorDistance = -1f;
                return;
            }

            EvaluateAnchor(
                anchorA);

            EvaluateAnchor(
                anchorB);

            if (
                float.IsPositiveInfinity(
                    nearestAnchorDistance)
            )
            {
                nearestAnchorDistance = -1f;
            }
        }

        private void EvaluateAnchor(
            PrototypeLiveCompositionAnchor anchor)
        {
            if (
                anchor == null ||
                !anchor.IsConfigured ||
                anchor.AnchorPoint == null
            )
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    figureRoot.position,
                    anchor.AnchorPoint.position);

            if (
                distance <
                nearestAnchorDistance
            )
            {
                nearestAnchorReference = anchor;
                nearestAnchorDistance = distance;
                nearestAnchor =
                    anchor.AnchorId.ToString();
            }
        }

        private void RefreshPoseDistance()
        {
            if (
                figureRoot == null ||
                posePoint == null
            )
            {
                poseDistance = -1f;
                return;
            }

            poseDistance =
                Vector3.Distance(
                    figureRoot.position,
                    posePoint.position);
        }

        private void ProcessSetupInput()
        {
            releaseInputAvailable = false;

            if (
                !figureRoleActive ||
                fullStain ||
                !frameGunSelected ||
                !primaryToolAllowed ||
                !useToolInputResolved ||
                sharedUseToolAction == null ||
                sharedUseToolAction.action == null ||
                !sharedUseToolAction.action.enabled ||
                Time.unscaledTime <
                    nextActionAllowedAt
            )
            {
                return;
            }

            if (
                !sharedUseToolAction.action
                    .WasPressedThisFrame()
            )
            {
                return;
            }

            nextActionAllowedAt =
                Time.unscaledTime +
                rearmDelaySeconds;

            if (
                nearestAnchorReference != null &&
                nearestAnchorDistance >= 0f &&
                nearestAnchorDistance <=
                    anchorUseRadius &&
                !nearestAnchorReference.Latched
            )
            {
                LatchAnchor(
                    nearestAnchorReference,
                    "Existing Frame Gun UseTool");

                return;
            }

            if (
                BothAnchorsLatched() &&
                poseDistance >= 0f &&
                poseDistance <=
                    poseCommitRadius
            )
            {
                if (
                    consentHarness != null &&
                    consentHarness.RequiresPartnerConsent
                )
                {
                    if (
                        !consentHarness.PartnerConsentGranted
                    )
                    {
                        if (
                            !consentHarness.InvitationPending
                        )
                        {
                            if (
                                consentHarness.RequestInvitation(
                                    "Figure pose UseTool")
                            )
                            {
                                lastAction =
                                    "Partner daveti gönderildi; bağımsız onay bekleniyor.";
                            }
                            else
                            {
                                partnerGateRejectCount++;

                                Reject(
                                    "Partner daveti oluşturulamadı.");
                            }
                        }
                        else
                        {
                            partnerGateRejectCount++;

                            Reject(
                                "Partner onayı henüz gelmedi.");
                        }

                        return;
                    }
                }

                explicitConsentReceived = true;

                StartComposition(
                    consentHarness != null &&
                    consentHarness.RequiresPartnerConsent
                        ? "Mutual consent commit"
                        : "Explicit UseTool consent");

                return;
            }

            Reject(
                BothAnchorsLatched()
                    ? "Kompozisyon için orta poz noktasına yaklaş."
                    : "Önce iki çevresel ankrajı Çerçeve Tabancasıyla sabitle.");
        }

        private bool LatchAnchor(
            PrototypeLiveCompositionAnchor anchor,
            string source)
        {
            if (
                anchor == null ||
                !anchor.IsConfigured
            )
            {
                Reject(
                    "Geçerli kompozisyon ankrajı yok.");

                return false;
            }

            if (anchor.Latched)
            {
                Reject(
                    $"Ankraj {anchor.AnchorId} zaten sabit.");

                return false;
            }

            if (
                !anchor.Latch(
                    source)
            )
            {
                Reject(
                    $"Ankraj {anchor.AnchorId} sabitlenemedi.");

                return false;
            }

            anchorLatchCount++;

            lastAction =
                $"Çerçeve Ankrajı {anchor.AnchorId} sabitlendi.";

            return true;
        }

        private bool StartComposition(
            string source)
        {
            if (
                composing ||
                figureRoot == null ||
                figureMotor == null ||
                anchorA == null ||
                anchorB == null ||
                posePoint == null
            )
            {
                Reject(
                    "Kompozisyon başlangıç sözleşmesi eksik.");

                return false;
            }

            if (!BothAnchorsLatched())
            {
                Reject(
                    "İki çevresel ankraj da gerekli.");

                return false;
            }

            if (!explicitConsentReceived)
            {
                Reject(
                    "Kompozisyona katılım açık onay gerektirir.");

                return false;
            }

            if (
                consentHarness != null &&
                consentHarness.RequiresPartnerConsent &&
                !consentHarness.CanCommitComposition
            )
            {
                partnerGateRejectCount++;

                Reject(
                    "Partner onayı geçerli değil; yeni davet/onay gerekli.");

                return false;
            }

            if (
                fullStain ||
                !figureRoleActive
            )
            {
                Reject(
                    "Bu Köprü şablonu normal Figure formu gerektirir.");

                return false;
            }

            float span =
                Vector3.Distance(
                    anchorA.AnchorPoint.position,
                    anchorB.AnchorPoint.position);

            if (
                span >
                maximumAnchorSpan
            )
            {
                Reject(
                    $"Ankraj açıklığı çok uzun: {span:F1} m.");

                return false;
            }

            GameObject bridgeObject =
                new GameObject(
                    $"M53_0_LiveBridge_{compositionStartCount + 1:00}");

            activeBridge =
                bridgeObject.AddComponent<
                    PrototypeLiveCompositionBridge>();

            if (
                !activeBridge.Build(
                    anchorA.AnchorPoint.position,
                    anchorB.AnchorPoint.position,
                    posePoint.position)
            )
            {
                Destroy(
                    bridgeObject);

                activeBridge = null;

                Reject(
                    "Köprü şablonu collision üretemedi.");

                return false;
            }

            CaptureAndSuppressMotor();
            MoveFigureToPose();

            composing = true;
            releaseInputAvailable = true;

            compositionEndsAt =
                Time.unscaledTime +
                compositionDurationSeconds;

            remainingCompositionSeconds =
                compositionDurationSeconds;

            compositionStartCount++;

            if (consentHarness != null)
            {
                consentHarness.NotifyCompositionStarted();
            }

            lastAction =
                consentHarness != null &&
                consentHarness.RequiresPartnerConsent
                    ? $"Ortak Köprü Kompozisyonu aktif • {compositionDurationSeconds:F1} sn."
                    : $"Köprü Kompozisyonu aktif • {compositionDurationSeconds:F1} sn.";

            Debug.Log(
                "[M53.0 Live Composition Started]\n" +
                $"Source={source}\n" +
                $"AnchorSpan={span:F2}\n" +
                $"BridgeColliderSegments={activeBridge.ColliderSegmentCount}\n" +
                "SingleFigureTemplateEnabled=True\n" +
                "BridgeTemplateEnabled=True\n" +
                "TwoEnvironmentalAnchorsRequired=True\n" +
                "ExplicitParticipationConsentRequired=True\n" +
                "SingleInputReleaseEnabled=True\n" +
                "AutomaticNearbyPlayerBindingEnabled=False\n" +
                "RagdollCompositionEnabled=False\n" +
                "FreeformPhysicsCompositionEnabled=False\n" +
                "AddsNewGameplayInputAction=False\n" +
                "MultiplayerCompositionEnabled=False\n" +
                $"LocalConsentHarnessActive={consentHarness != null}\n" +
                $"PartnerConsentValid={(consentHarness != null && consentHarness.PartnerConsentGranted)}\n" +
                "StalePartnerConsentReusable=False\n" +
                "ParticipantCanWithdrawIndependently=True\n" +
                "NetworkAuthorityEnabled=False",
                this);

            return true;
        }

        private void TickComposition()
        {
            remainingCompositionSeconds =
                Mathf.Max(
                    0f,
                    compositionEndsAt -
                    Time.unscaledTime);

            releaseInputAvailable =
                sharedUseToolAction != null &&
                sharedUseToolAction.action != null &&
                sharedUseToolAction.action.enabled;

            if (
                consentHarness != null &&
                consentHarness.WithdrawalRequested
            )
            {
                partnerWithdrawalReleaseCount++;

                ReleaseComposition(
                    "Partner withdrew consent",
                    safety: false);

                return;
            }

            bool localPainterCounterplayBypass =
                IsLocalPainterCounterplayBypassActive();

            if (
                (
                    !figureRoleActive &&
                    !localPainterCounterplayBypass
                ) ||
                fullStain
            )
            {
                safetyReleaseCount++;

                ReleaseComposition(
                    !figureRoleActive
                        ? "Role authority changed"
                        : "Full Stain transition",
                    safety: true);

                return;
            }

            if (
                remainingCompositionSeconds <= 0f
            )
            {
                ReleaseComposition(
                    "Composition duration complete",
                    safety: false);

                return;
            }

            if (
                releaseInputAvailable &&
                Time.unscaledTime >=
                    nextActionAllowedAt &&
                sharedUseToolAction.action
                    .WasPressedThisFrame()
            )
            {
                nextActionAllowedAt =
                    Time.unscaledTime +
                    rearmDelaySeconds;

                ReleaseComposition(
                    "Explicit one-input release",
                    safety: false);
            }
        }

        public bool TryApplyPainterWatercolorBend(
            Vector3 worldDelta,
            float maximumBodyDisplacement,
            string source,
            out string reason)
        {
            reason = "Unknown";

            if (
                !composing ||
                activeBridge == null ||
                !activeBridge.ActiveBridge
            )
            {
                reason =
                    "No active composition bridge";

                return false;
            }

            if (
                !activeBridge.TryApplyWatercolorBend(
                    worldDelta,
                    maximumBodyDisplacement,
                    source,
                    out reason)
            )
            {
                return false;
            }

            MoveFigureToWorldPose(
                activeBridge.BodyPoint);

            lastAction =
                $"Ressam Suluboya ile Köprü Kompozisyonunu büktü.";

            return true;
        }

        public bool ForceExternalCompositionRelease(
            string source)
        {
            if (!composing)
            {
                return false;
            }

            ReleaseComposition(
                string.IsNullOrWhiteSpace(
                    source)
                    ? "External counterplay release"
                    : source,
                safety: false);

            return true;
        }

        private bool IsLocalPainterCounterplayBypassActive()
        {
            if (
                painterCounterplayHarness == null ||
                !painterCounterplayHarness.isActiveAndEnabled ||
                !painterCounterplayHarness.AllowLocalPainterRoleSwitchBypass
            )
            {
                return false;
            }

            return
                PrototypeRoleAuthorityResolver.IsPainter(
                    out _);
        }

        private void CaptureAndSuppressMotor()
        {
            if (
                figureMotor == null ||
                motorStateCaptured
            )
            {
                return;
            }

            savedMotorEnabled =
                figureMotor.enabled;

            motorStateCaptured = true;

            if (figureMotor.enabled)
            {
                figureMotor.enabled = false;
                motorSuppressed = true;
            }
        }

        private void RestoreMotor()
        {
            if (
                figureMotor == null ||
                !motorStateCaptured
            )
            {
                motorSuppressed = false;
                motorStateCaptured = false;
                return;
            }

            figureMotor.enabled =
                savedMotorEnabled;

            motorSuppressed = false;
            motorStateCaptured = false;
        }

        private void MoveFigureToPose()
        {
            if (posePoint == null)
            {
                return;
            }

            MoveFigureToWorldPose(
                posePoint.position);
        }

        private void MoveFigureToWorldPose(
            Vector3 worldPosition)
        {
            if (figureRoot == null)
            {
                return;
            }

            CharacterController characterController =
                figureRoot.GetComponent<
                    CharacterController>();

            bool controllerWasEnabled =
                characterController != null &&
                characterController.enabled;

            if (controllerWasEnabled)
            {
                characterController.enabled = false;
            }

            figureRoot.position =
                worldPosition;

            if (
                anchorA != null &&
                anchorB != null &&
                anchorA.AnchorPoint != null &&
                anchorB.AnchorPoint != null
            )
            {
                Vector3 across =
                    anchorB.AnchorPoint.position -
                    anchorA.AnchorPoint.position;

                across =
                    Vector3.ProjectOnPlane(
                        across,
                        Vector3.up);

                if (
                    across.sqrMagnitude >
                    0.001f
                )
                {
                    figureRoot.rotation =
                        Quaternion.LookRotation(
                            across.normalized,
                            Vector3.up);
                }
            }

            if (controllerWasEnabled)
            {
                characterController.enabled = true;
            }

            Physics.SyncTransforms();
        }

        private void ReleaseComposition(
            string reason,
            bool safety)
        {
            if (!composing)
            {
                return;
            }

            composing = false;
            remainingCompositionSeconds = 0f;
            releaseInputAvailable = false;

            if (activeBridge != null)
            {
                activeBridge.Deactivate(
                    reason);

                Destroy(
                    activeBridge.gameObject);

                activeBridge = null;
            }

            RestoreMotor();

            if (anchorA != null)
            {
                anchorA.Release(
                    reason);
            }

            if (anchorB != null)
            {
                anchorB.Release(
                    reason);
            }

            explicitConsentReceived = false;

            compositionReleaseCount++;

            if (consentHarness != null)
            {
                consentHarness.NotifyCompositionReleased(
                    reason);
            }

            if (safety)
            {
                state =
                    PrototypeLiveCompositionState.SafetyRelease;
            }

            lastAction =
                safety
                    ? $"Güvenli ayrılma: {reason}."
                    : $"Kompozisyondan ayrıldı: {reason}.";

            Debug.Log(
                "[M53.0 Live Composition Released]\n" +
                $"Reason={reason}\n" +
                $"Safety={safety}\n" +
                $"ReleaseCount={compositionReleaseCount}\n" +
                "FigureMotorRestored=True\n" +
                "AnchorsReleased=True",
                this);
        }

        private bool BothAnchorsLatched()
        {
            return
                anchorA != null &&
                anchorB != null &&
                anchorA.Latched &&
                anchorB.Latched;
        }

        private void Reject(
            string reason)
        {
            rejectedActionCount++;
            lastAction = reason;
        }

        private void RefreshState()
        {
            if (figureRoot == null)
            {
                state =
                    PrototypeLiveCompositionState.WaitingForFigure;

                return;
            }

            if (!figureRoleActive)
            {
                state =
                    PrototypeLiveCompositionState.PainterRole;

                return;
            }

            if (composing)
            {
                state =
                    PrototypeLiveCompositionState.Composing;

                return;
            }

            if (
                fullStain ||
                !frameGunSelected
            )
            {
                state =
                    PrototypeLiveCompositionState.FrameGunRequired;

                return;
            }

            if (
                anchorA == null ||
                !anchorA.Latched
            )
            {
                state =
                    PrototypeLiveCompositionState.SeekingFirstAnchor;

                return;
            }

            if (
                anchorB == null ||
                !anchorB.Latched
            )
            {
                state =
                    PrototypeLiveCompositionState.SeekingSecondAnchor;

                return;
            }

            state =
                PrototypeLiveCompositionState.ReadyForCommit;
        }

        private void RefreshHud()
        {
            bool anchorLatched =
                anchorA != null && anchorA.Latched ||
                anchorB != null && anchorB.Latched;

            bool relevantNow =
                figureRoleActive &&
                (composing ||
                 anchorLatched ||
                 state == PrototypeLiveCompositionState.SeekingSecondAnchor ||
                 state == PrototypeLiveCompositionState.ReadyForCommit ||
                 state == PrototypeLiveCompositionState.SafetyRelease ||
                 (poseDistance >= 0f && poseDistance <= poseCommitRadius * 1.35f));

            bool visible =
                PrototypeRuntimeHudVisibilityUtility.Hold(
                    relevantNow,
                    ref hudVisibleUntilUnscaled,
                    contextualHudHoldSeconds);

            PrototypeRuntimeHudVisibilityUtility.ApplyCanvasGroup(
                statusGroup,
                visible);

            if (!visible)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    composing
                        ? "CANLI KOMPOZİSYON • KÖPRÜ AKTİF"
                        : "CANLI KOMPOZİSYON • TEK FİGÜR KÖPRÜ";
            }

            if (stateText != null)
            {
                string a =
                    anchorA != null &&
                    anchorA.Latched
                        ? "A ✓"
                        : "A ○";

                string b =
                    anchorB != null &&
                    anchorB.Latched
                        ? "B ✓"
                        : "B ○";

                string partner =
                    consentHarness == null
                        ? "PARTNER N/A"
                        : $"PARTNER {consentHarness.StateLabel}";

                stateText.text =
                    $"DURUM {TranslateState(state)}   {a}   {b}\n" +
                    $"FRAME {(frameGunSelected ? "AKTİF" : "KAPALI")}   " +
                    $"INPUT {(useToolInputResolved ? "OK" : "YOK")}   " +
                    $"{partner}\n" +
                    (
                        composing
                            ? $"SÜRE {remainingCompositionSeconds:F1}s   AYRIL E"
                            : $"POZ {(poseDistance >= 0f ? poseDistance.ToString("F2") : "-")} m   {lastAction}"
                    );
            }

            if (controlsText != null)
            {
                controlsText.text =
                    composing
                        ? "FIGURE E • ANINDA AYRIL   PARTNER WITHDRAW • ANINDA AYRIL\n" +
                          "İKİ TARAF DA BAĞIMSIZ ÇIKIŞ HAKKINA SAHİP"
                        : consentHarness != null
                            ? "3 ÇERÇEVE TABANCASI • A E • B E • ORTA E = DAVET\n" +
                              "PARTNER ONAYINDAN SONRA ORTA E = FIGURE ONAYI"
                            : "3 ÇERÇEVE TABANCASI • A E • B E • ORTA POZ E\n" +
                              "AÇIK ONAY ZORUNLU • YENİ TUŞ YOK";
            }
        }

        private static string TranslateState(
            PrototypeLiveCompositionState value)
        {
            switch (value)
            {
                case PrototypeLiveCompositionState.PainterRole:
                    return "PAINTER ROLÜ";

                case PrototypeLiveCompositionState.FrameGunRequired:
                    return "ÇERÇEVE TABANCASI GEREKLİ";

                case PrototypeLiveCompositionState.SeekingFirstAnchor:
                    return "ANKRAJ A";

                case PrototypeLiveCompositionState.SeekingSecondAnchor:
                    return "ANKRAJ B";

                case PrototypeLiveCompositionState.ReadyForCommit:
                    return "POZ ONAYI";

                case PrototypeLiveCompositionState.Composing:
                    return "KÖPRÜ";

                case PrototypeLiveCompositionState.SafetyRelease:
                    return "GÜVENLİ AYRILMA";

                default:
                    return "BEKLİYOR";
            }
        }

        private void OnDisable()
        {
            if (composing)
            {
                safetyReleaseCount++;

                ReleaseComposition(
                    "Controller disabled",
                    safety: true);
            }
            else
            {
                RestoreMotor();
            }
        }

        private void OnDestroy()
        {
            if (composing)
            {
                ReleaseComposition(
                    "Controller destroyed",
                    safety: true);
            }
            else
            {
                RestoreMotor();
            }
        }
    }
}
