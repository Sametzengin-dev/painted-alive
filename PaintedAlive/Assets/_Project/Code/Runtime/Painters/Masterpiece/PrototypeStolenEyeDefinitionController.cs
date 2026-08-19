using System;
using System.Collections.Generic;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Painters.SideCanvas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(-210)]
    [DisallowMultipleComponent]
    public sealed class PrototypeStolenEyeDefinitionController :
        MonoBehaviour
    {
        private static readonly PrototypeMasterpiecePartKind[]
            RevealedKinds =
            {
                PrototypeMasterpiecePartKind.Core,
                PrototypeMasterpiecePartKind.Head,
                PrototypeMasterpiecePartKind.LeftAttack,
                PrototypeMasterpiecePartKind.RightAttack,
                PrototypeMasterpiecePartKind.LeftContact,
                PrototypeMasterpiecePartKind.RightContact
            };

        [Header("Sources")]
        [SerializeField]
        private PrototypeMasterpieceWorldDeploymentController deployment;

        [SerializeField]
        private PrototypeMasterpieceDefinitionSeveringController severing;

        [SerializeField]
        private PrototypeLivingSideCanvasController sideCanvas;

        [SerializeField] private Transform figureRoot;
        [SerializeField] private Camera figureCamera;

        [Header("Pickup Contract")]
        [SerializeField, Min(0.1f)]
        private float pickupHoldSeconds = 0.45f;

        [SerializeField, Min(0.5f)]
        private float maximumPickupDistance = 1.80f;

        [SerializeField, Range(0.02f, 0.35f)]
        private float viewportAimTolerance = 0.16f;

        [SerializeField]
        private LayerMask lineOfSightMask = ~0;

        [SerializeField, Min(0f)]
        private float lineOfSightSkin = 0.06f;

        [Header("Carry")]
        [SerializeField]
        private Vector3 carryLocalPosition =
            new Vector3(
                0.34f,
                -0.24f,
                0.76f);

        [SerializeField]
        private Vector3 activeSightLocalPosition =
            new Vector3(
                0f,
                0.22f,
                0.88f);

        [SerializeField]
        private Vector3 dropVelocity =
            new Vector3(
                0f,
                0.65f,
                0.85f);

        [SerializeField]
        private Vector3 dropAngularVelocity =
            new Vector3(
                15f,
                110f,
                -35f);

        [Header("Eye Sight")]
        [SerializeField, Min(0.5f)]
        private float sightDurationSeconds = 6f;

        [SerializeField, Range(1, 24)]
        private int maximumDraftStrokeCount = 12;

        [SerializeField, Range(8, 128)]
        private int maximumDraftPointsPerStroke = 72;

        [SerializeField]
        private Vector2 draftWorldSize =
            new Vector2(
                1.85f,
                1.35f);

        [SerializeField, Min(1f)]
        private float draftHeight = 3.0f;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeSeveredDefinitionToken token;

        [SerializeField] private Transform carrySocket;

        [SerializeField]
        private PrototypeStolenEyeDefinitionState state =
            PrototypeStolenEyeDefinitionState.WaitingForSever;

        [SerializeField] private bool inputActionActive;
        [SerializeField] private bool figureRoleActive;
        [SerializeField] private bool tokenAimed;
        [SerializeField] private bool lineOfSightClear;
        [SerializeField] private bool carrying;
        [SerializeField] private bool sightActive;
        [SerializeField] private bool awaitingReleaseAfterPickup;
        [SerializeField] private bool positionExposed;
        [SerializeField] private bool draftSignalAvailable;
        [SerializeField] private int tokenResolveCount;
        [SerializeField] private int pickupStartCount;
        [SerializeField] private int pickupCancelCount;
        [SerializeField] private int pickupCompleteCount;
        [SerializeField] private int sightActivationCount;
        [SerializeField] private int sightConsumeCount;
        [SerializeField] private int manualDropCount;
        [SerializeField] private int roleAutoDropCount;
        [SerializeField] private int weakPointRevealCount;
        [SerializeField] private int draftStrokeRevealCount;
        [SerializeField] private int rejectedUseCount;
        [SerializeField] private float pickupProgress;
        [SerializeField] private float tokenDistance;
        [SerializeField] private float viewportAimOffset;
        [SerializeField] private float sightRemainingSeconds;
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private string lastAction =
            "Kesilmiş Head / Perception tanımı bekleniyor.";

        private readonly RaycastHit[] lineOfSightHits =
            new RaycastHit[32];

        private readonly List<LineRenderer> weakPointLines =
            new List<LineRenderer>();

        private readonly List<LineRenderer> draftLines =
            new List<LineRenderer>();

        private InputAction contextAction;
        private Material revealMaterial;
        private LineRenderer draftFrame;
        private LineRenderer exposureBeam;
        private LineRenderer exposureRingA;
        private LineRenderer exposureRingB;

        private float sightStartedAt;
        private int boundTokenInstanceId;
        private bool expended;

        public PrototypeStolenEyeDefinitionState State =>
            state;
        public bool InputActionActive =>
            inputActionActive;
        public bool FigureRoleActive =>
            figureRoleActive;
        public bool TokenAimed =>
            tokenAimed;
        public bool LineOfSightClear =>
            lineOfSightClear;
        public bool Carrying =>
            carrying;
        public bool SightActive =>
            sightActive;
        public bool PositionExposed =>
            positionExposed;
        public bool DraftSignalAvailable =>
            draftSignalAvailable;
        public int TokenResolveCount =>
            tokenResolveCount;
        public int PickupStartCount =>
            pickupStartCount;
        public int PickupCancelCount =>
            pickupCancelCount;
        public int PickupCompleteCount =>
            pickupCompleteCount;
        public int SightActivationCount =>
            sightActivationCount;
        public int SightConsumeCount =>
            sightConsumeCount;
        public int ManualDropCount =>
            manualDropCount;
        public int RoleAutoDropCount =>
            roleAutoDropCount;
        public int WeakPointRevealCount =>
            weakPointRevealCount;
        public int DraftStrokeRevealCount =>
            draftStrokeRevealCount;
        public int RejectedUseCount =>
            rejectedUseCount;
        public float PickupProgress =>
            pickupProgress;
        public float TokenDistance =>
            tokenDistance;
        public float ViewportAimOffset =>
            viewportAimOffset;
        public float SightRemainingSeconds =>
            sightRemainingSeconds;
        public string ResolvedRole =>
            resolvedRole;
        public string LastAction =>
            lastAction;
        public PrototypeSeveredDefinitionToken Token =>
            token;

        public bool StolenDefinitionSlotEnabled => true;
        public int MaximumCarrySlots => 1;
        public bool EyeSightEnabled => true;
        public bool PerceptionEchoEnabled => true;
        public bool HeadPerceptionTokenRequired => true;
        public bool LegacyEyeClassNamesRetainedForSerialization => true;
        public bool PainterDraftRevealEnabled => true;
        public bool WeakOrganRevealEnabled => true;
        public bool FigurePositionExposureEnabled => true;
        public bool RawDamageBonusEnabled => false;
        public bool InventoryListEnabled => false;
        public bool NewWeaponEnabled => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeMasterpieceWorldDeploymentController
                configuredDeployment,
            PrototypeMasterpieceDefinitionSeveringController
                configuredSevering,
            PrototypeLivingSideCanvasController
                configuredSideCanvas,
            Transform configuredFigureRoot,
            Camera configuredFigureCamera,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            deployment = configuredDeployment;
            severing = configuredSevering;
            sideCanvas = configuredSideCanvas;
            figureRoot = configuredFigureRoot;
            figureCamera = configuredFigureCamera;
            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            EnsureContextAction();
            EnsureCarrySocket();
            EnsureRevealResources();
            ResolveToken();
            RefreshHud();
        }

        private void Awake()
        {
            EnsureContextAction();
            EnsureCarrySocket();
            EnsureRevealResources();
            ResolveToken();
            RefreshHud();
        }

        private void OnEnable()
        {
            EnsureContextAction();
            EnsureCarrySocket();
            EnsureRevealResources();
            ResolveToken();
            RefreshHud();
        }

        private void Update()
        {
            RefreshRoleAuthority();
            ResolveToken();
            HandleRoleTransition();
            RefreshInputActionState();

            if (!figureRoleActive)
            {
                HideRevealVisuals();
                RefreshHud();
                return;
            }

            if (sightActive)
            {
                TickSight();
            }
            else if (carrying)
            {
                TickCarry();
            }
            else
            {
                TickPickup();
            }

            UpdatePositionExposure();
            UpdateRevealVisuals();
            RefreshHud();
        }

        private bool IsCompatibleHeadPerceptionToken(
            PrototypeSeveredDefinitionToken candidate)
        {
            return
                candidate != null &&
                !candidate.Consumed &&
                candidate.PartKind ==
                    PrototypeMasterpiecePartKind.Head &&
                candidate.Capability ==
                    PrototypeMasterpieceCapability.Perception;
        }

        public bool ForcePickupForPrototype()
        {
            RefreshRoleAuthority();
            ResolveToken();

            if (
                !figureRoleActive ||
                !IsCompatibleHeadPerceptionToken(
                    token)
            )
            {
                rejectedUseCount++;
                lastAction =
                    "Doğrudan pickup için Figure rolü ve geçerli Head / Perception tanımı gerekli.";

                return false;
            }

            return CompletePickup(
                "Editor force pickup");
        }

        public bool ActivateEyeSightForPrototype()
        {
            RefreshRoleAuthority();

            if (
                !figureRoleActive ||
                !carrying ||
                !IsCompatibleHeadPerceptionToken(
                    token)
            )
            {
                rejectedUseCount++;
                lastAction =
                    "Algı Yankısı için taşınan geçerli Head / Perception tanımı gerekli.";

                return false;
            }

            return BeginSight(
                "Editor force Eye Sight");
        }

        public bool DropCarriedDefinition()
        {
            if (
                token == null ||
                !carrying
            )
            {
                lastAction =
                    "Bırakılacak Çalınmış Tanım yok.";

                return false;
            }

            CancelSight(
                consume: false);

            bool dropped =
                token.Drop(
                    ResolveDropPosition(),
                    ResolveWorldDropVelocity(),
                    dropAngularVelocity);

            if (!dropped)
            {
                return false;
            }

            carrying = false;
            awaitingReleaseAfterPickup = false;
            pickupProgress = 0f;
            manualDropCount++;
            state =
                PrototypeStolenEyeDefinitionState.TokenFalling;

            lastAction =
                "Head / Perception tanımı yere bırakıldı.";

            return true;
        }

        private void EnsureContextAction()
        {
            if (contextAction != null)
            {
                return;
            }

            contextAction =
                new InputAction(
                    "M50.1 Stolen Head Perception Context",
                    InputActionType.Button,
                    "<Keyboard>/e");
        }

        private void EnsureCarrySocket()
        {
            if (carrySocket != null)
            {
                return;
            }

            Transform parent =
                figureCamera != null
                    ? figureCamera.transform
                    : figureRoot;

            if (parent == null)
            {
                return;
            }

            Transform existing =
                parent.Find(
                    "M50_1_StolenDefinitionCarrySocket");

            if (existing != null)
            {
                carrySocket = existing;
                return;
            }

            GameObject socketObject =
                new GameObject(
                    "M50_1_StolenDefinitionCarrySocket");

            socketObject.transform.SetParent(
                parent,
                false);

            socketObject.transform.localPosition =
                Vector3.zero;

            socketObject.transform.localRotation =
                Quaternion.identity;

            carrySocket =
                socketObject.transform;
        }

        private void EnsureRevealResources()
        {
            if (revealMaterial == null)
            {
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
                    revealMaterial =
                        new Material(
                            shader)
                        {
                            name =
                                "M50_1_StolenHeadPerceptionReveal_Runtime",
                            hideFlags =
                                HideFlags.DontSave
                        };
                }
            }

            while (
                weakPointLines.Count <
                RevealedKinds.Length
            )
            {
                weakPointLines.Add(
                    CreateLine(
                        $"M50_1_WeakPoint_{weakPointLines.Count:00}",
                        loop: true,
                        width: 0.045f));
            }

            int draftTarget =
                Mathf.Clamp(
                    maximumDraftStrokeCount,
                    1,
                    24);

            while (
                draftLines.Count <
                draftTarget
            )
            {
                draftLines.Add(
                    CreateLine(
                        $"M50_1_DraftStroke_{draftLines.Count:00}",
                        loop: false,
                        width: 0.026f));
            }

            if (draftFrame == null)
            {
                draftFrame =
                    CreateLine(
                        "M50_1_DraftFrame",
                        loop: true,
                        width: 0.032f);
            }

            if (exposureBeam == null)
            {
                exposureBeam =
                    CreateLine(
                        "M50_1_PositionExposureBeam",
                        loop: false,
                        width: 0.055f);
            }

            if (exposureRingA == null)
            {
                exposureRingA =
                    CreateLine(
                        "M50_1_PositionExposureRingA",
                        loop: true,
                        width: 0.048f);
            }

            if (exposureRingB == null)
            {
                exposureRingB =
                    CreateLine(
                        "M50_1_PositionExposureRingB",
                        loop: true,
                        width: 0.032f);
            }

            HideRevealVisuals();
        }

        private LineRenderer CreateLine(
            string objectName,
            bool loop,
            float width)
        {
            GameObject lineObject =
                new GameObject(
                    objectName);

            lineObject.transform.SetParent(
                transform,
                false);

            LineRenderer line =
                lineObject.AddComponent<
                    LineRenderer>();

            line.useWorldSpace = true;
            line.loop = loop;
            line.alignment =
                LineAlignment.View;

            line.textureMode =
                LineTextureMode.Stretch;

            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.shadowCastingMode =
                ShadowCastingMode.Off;

            line.receiveShadows = false;
            line.widthMultiplier = width;
            line.sharedMaterial =
                revealMaterial;

            line.enabled = false;
            return line;
        }

        private void RefreshRoleAuthority()
        {
            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedRole);
        }

        private void HandleRoleTransition()
        {
            if (
                figureRoleActive ||
                !carrying ||
                token == null
            )
            {
                return;
            }

            CancelSight(
                consume: false);

            bool dropped =
                token.Drop(
                    ResolveDropPosition(),
                    ResolveWorldDropVelocity(),
                    dropAngularVelocity);

            if (dropped)
            {
                roleAutoDropCount++;
            }

            carrying = false;
            awaitingReleaseAfterPickup = false;
            pickupProgress = 0f;
            state =
                PrototypeStolenEyeDefinitionState.PainterRole;

            lastAction =
                "Rol Painter oldu; Çalınmış Tanım otomatik bırakıldı.";
        }

        private void ResolveToken()
        {
            PrototypeSeveredDefinitionToken candidate =
                severing != null
                    ? severing.DetachedToken
                    : null;

            if (
                candidate != null &&
                !IsCompatibleHeadPerceptionToken(
                    candidate)
            )
            {
                candidate = null;
            }

            if (
                candidate == token &&
                (
                    token == null ||
                    token.GetInstanceID() ==
                        boundTokenInstanceId
                )
            )
            {
                return;
            }

            if (
                token != null &&
                carrying &&
                token != candidate
            )
            {
                carrying = false;
                sightActive = false;
            }

            token = candidate;
            boundTokenInstanceId =
                token != null
                    ? token.GetInstanceID()
                    : 0;

            pickupProgress = 0f;
            awaitingReleaseAfterPickup = false;

            if (token != null)
            {
                expended = false;
                carrying = token.IsCarried;
                tokenResolveCount++;

                state =
                    carrying
                        ? PrototypeStolenEyeDefinitionState.Carrying
                        : token.Settled
                            ? PrototypeStolenEyeDefinitionState.OutOfRange
                            : PrototypeStolenEyeDefinitionState.TokenFalling;

                lastAction =
                    carrying
                        ? "Head / Perception tanımı Figür tarafından taşınıyor."
                        : "Kesilmiş Head / Perception tanımı dünyada.";
            }
            else
            {
                carrying = false;
                sightActive = false;

                if (
                    expended &&
                    severing != null &&
                    severing.EyeSevered
                )
                {
                    state =
                        PrototypeStolenEyeDefinitionState.Expended;

                    lastAction =
                        "Algı Yankısı tamamlandı; tanım tükendi.";
                }
                else
                {
                    state =
                        PrototypeStolenEyeDefinitionState.WaitingForSever;

                    lastAction =
                        "Kesilmiş Head / Perception tanımı bekleniyor.";
                }
            }
        }

        private void RefreshInputActionState()
        {
            bool shouldEnable =
                figureRoleActive &&
                contextAction != null &&
                (
                    token != null ||
                    carrying
                ) &&
                !expended;

            if (shouldEnable)
            {
                if (!contextAction.enabled)
                {
                    contextAction.Enable();
                }
            }
            else if (
                contextAction != null &&
                contextAction.enabled
            )
            {
                contextAction.Disable();
            }

            inputActionActive =
                contextAction != null &&
                contextAction.enabled;
        }

        private void TickPickup()
        {
            tokenAimed = false;
            lineOfSightClear = false;
            tokenDistance = -1f;
            viewportAimOffset = 1f;

            if (
                !IsCompatibleHeadPerceptionToken(
                    token)
            )
            {
                state =
                    expended
                        ? PrototypeStolenEyeDefinitionState.Expended
                        : PrototypeStolenEyeDefinitionState.WaitingForSever;

                ResetPickupProgress(
                    "Geçerli token yok.");

                return;
            }

            if (!token.Settled)
            {
                state =
                    PrototypeStolenEyeDefinitionState.TokenFalling;

                ResetPickupProgress(
                    "Head / Perception tanımının yere oturması bekleniyor.");

                return;
            }

            if (
                figureRoot == null ||
                figureCamera == null
            )
            {
                state =
                    PrototypeStolenEyeDefinitionState.OutOfRange;

                ResetPickupProgress(
                    "Figür hedefleme kaynakları bulunamadı.");

                return;
            }

            tokenDistance =
                Vector3.Distance(
                    figureRoot.position,
                    token.transform.position);

            if (
                tokenDistance >
                maximumPickupDistance
            )
            {
                state =
                    PrototypeStolenEyeDefinitionState.OutOfRange;

                ResetPickupProgress(
                    "Head / Perception tanımına yaklaş.");

                return;
            }

            Vector3 viewport =
                figureCamera.WorldToViewportPoint(
                    token.transform.position);

            if (viewport.z <= 0f)
            {
                state =
                    PrototypeStolenEyeDefinitionState.AimRequired;

                ResetPickupProgress(
                    "Head / Perception tanımına bak.");

                return;
            }

            viewportAimOffset =
                Vector2.Distance(
                    new Vector2(
                        viewport.x,
                        viewport.y),
                    new Vector2(
                        0.5f,
                        0.5f));

            tokenAimed =
                viewportAimOffset <=
                viewportAimTolerance;

            if (!tokenAimed)
            {
                state =
                    PrototypeStolenEyeDefinitionState.AimRequired;

                ResetPickupProgress(
                    "Kamera merkezini Head / Perception tanımında tut.");

                return;
            }

            lineOfSightClear =
                HasClearLineOfSight(
                    token.transform.position);

            if (!lineOfSightClear)
            {
                state =
                    PrototypeStolenEyeDefinitionState.Occluded;

                ResetPickupProgress(
                    "Head / Perception tanımının görüş hattı engelli.");

                return;
            }

            bool pressed =
                inputActionActive &&
                contextAction.IsPressed();

            if (!pressed)
            {
                state =
                    PrototypeStolenEyeDefinitionState.ReadyToPickup;

                if (pickupProgress > 0f)
                {
                    pickupCancelCount++;
                    pickupProgress = 0f;
                    lastAction =
                        "E bırakıldı; alma ilerlemesi sıfırlandı.";
                }
                else
                {
                    lastAction =
                        "E basılı tut: Head / Perception tanımını al.";
                }

                return;
            }

            if (pickupProgress <= 0f)
            {
                pickupStartCount++;
                lastAction =
                    "Head / Perception tanımı tek Çalınmış Tanım slotuna alınıyor.";
            }

            pickupProgress +=
                Time.deltaTime /
                Mathf.Max(
                    0.05f,
                    pickupHoldSeconds);

            pickupProgress =
                Mathf.Clamp01(
                    pickupProgress);

            state =
                PrototypeStolenEyeDefinitionState.PickingUp;

            if (pickupProgress >= 1f)
            {
                CompletePickup(
                    "Context E hold completed");
            }
        }

        private bool CompletePickup(
            string source)
        {
            EnsureCarrySocket();

            if (
                !IsCompatibleHeadPerceptionToken(
                    token) ||
                carrySocket == null
            )
            {
                rejectedUseCount++;
                lastAction =
                    "Pickup tamamlanamadı; Head / Perception tanımı veya carry socket yok.";

                return false;
            }

            bool pickedUp =
                token.BeginCarry(
                    carrySocket,
                    carryLocalPosition,
                    Quaternion.identity);

            if (!pickedUp)
            {
                rejectedUseCount++;
                lastAction =
                    "Token carry yaşam döngüsü pickup'ı reddetti.";

                return false;
            }

            carrying = true;
            pickupProgress = 0f;
            awaitingReleaseAfterPickup = true;
            pickupCompleteCount++;
            state =
                PrototypeStolenEyeDefinitionState.Carrying;

            lastAction =
                "Head / Perception tanımı taşınıyor; E'yi bırak, yeniden basarak Algı Yankısı kullan.";

            Debug.Log(
                "[M50.1 Stolen Head / Perception Picked Up]\n" +
                $"Source={source}\n" +
                $"SnapshotHash={token.SnapshotHash}\n" +
                "Definition=Head/Perception\n" +
                "CarrySlot=1/1\n" +
                "FigurePositionExposed=True\n" +
                "RawDamageBonusEnabled=False\n" +
                "InventoryListEnabled=False\n" +
                "NetworkAuthorityEnabled=False",
                token);

            return true;
        }

        private void TickCarry()
        {
            if (
                token == null ||
                token.Consumed
            )
            {
                carrying = false;
                expended = true;
                state =
                    PrototypeStolenEyeDefinitionState.Expended;

                return;
            }

            float bob =
                Mathf.Sin(
                    Time.unscaledTime *
                    3.4f) *
                0.018f;

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    Time.unscaledTime *
                    42f,
                    Mathf.Sin(
                        Time.unscaledTime *
                        2.3f) *
                    6f);

            token.SetCarryPose(
                carryLocalPosition +
                Vector3.up *
                bob,
                rotation);

            state =
                PrototypeStolenEyeDefinitionState.Carrying;

            if (awaitingReleaseAfterPickup)
            {
                if (
                    !inputActionActive ||
                    !contextAction.IsPressed()
                )
                {
                    awaitingReleaseAfterPickup = false;
                    lastAction =
                        "Head / Perception tanımı hazır; E ile 6 saniyelik Algı Yankısı.";
                }

                return;
            }

            if (
                inputActionActive &&
                contextAction.WasPressedThisFrame()
            )
            {
                BeginSight(
                    "Context E press");
            }
        }

        private bool BeginSight(
            string source)
        {
            if (
                !IsCompatibleHeadPerceptionToken(
                    token) ||
                !carrying
            )
            {
                rejectedUseCount++;
                lastAction =
                    "Algı Yankısı için taşınan Head / Perception tanımı yok.";

                return false;
            }

            sightActive = true;
            sightStartedAt =
                Time.unscaledTime;

            sightRemainingSeconds =
                sightDurationSeconds;

            sightActivationCount++;
            state =
                PrototypeStolenEyeDefinitionState.SightActive;

            lastAction =
                "Algı Yankısı aktif: taslak ve capability düğümleri görünür.";

            Debug.Log(
                "[M50.1 Perception Echo Activated]\n" +
                $"Source={source}\n" +
                $"SnapshotHash={token.SnapshotHash}\n" +
                $"DurationSeconds={sightDurationSeconds:F2}\n" +
                "PainterDraftRevealEnabled=True\n" +
                "WeakOrganRevealEnabled=True\n" +
                "FigurePositionExposureEnabled=True\n" +
                "RawDamageBonusEnabled=False\n" +
                "NetworkAuthorityEnabled=False",
                token);

            return true;
        }

        private void TickSight()
        {
            if (
                token == null ||
                token.Consumed
            )
            {
                CancelSight(
                    consume: false);

                carrying = false;
                expended = true;
                state =
                    PrototypeStolenEyeDefinitionState.Expended;

                return;
            }

            float elapsed =
                Time.unscaledTime -
                sightStartedAt;

            sightRemainingSeconds =
                Mathf.Max(
                    0f,
                    sightDurationSeconds -
                    elapsed);

            float bob =
                Mathf.Sin(
                    Time.unscaledTime *
                    4.2f) *
                0.025f;

            token.SetCarryPose(
                activeSightLocalPosition +
                Vector3.up *
                bob,
                Quaternion.Euler(
                    0f,
                    Time.unscaledTime *
                    88f,
                    0f));

            state =
                PrototypeStolenEyeDefinitionState.SightActive;

            if (
                elapsed >=
                sightDurationSeconds
            )
            {
                CompleteSightUse();
            }
        }

        private void CompleteSightUse()
        {
            if (token != null)
            {
                token.MarkConsumed();

                GameObject tokenObject =
                    token.gameObject;

                token = null;
                boundTokenInstanceId = 0;

                if (Application.isPlaying)
                {
                    Destroy(
                        tokenObject);
                }
                else
                {
                    DestroyImmediate(
                        tokenObject);
                }
            }

            sightActive = false;
            carrying = false;
            sightRemainingSeconds = 0f;
            expended = true;
            sightConsumeCount++;
            state =
                PrototypeStolenEyeDefinitionState.Expended;

            lastAction =
                "Algı Yankısı tamamlandı; Çalınmış Tanım tükendi.";

            HideRevealVisuals();
        }

        private void CancelSight(
            bool consume)
        {
            if (!sightActive)
            {
                return;
            }

            sightActive = false;
            sightRemainingSeconds = 0f;

            if (consume)
            {
                CompleteSightUse();
            }

            HideRevealVisuals();
        }

        private void ResetPickupProgress(
            string reason)
        {
            if (pickupProgress <= 0f)
            {
                return;
            }

            pickupProgress = 0f;
            pickupCancelCount++;
            lastAction = reason;
        }

        private bool HasClearLineOfSight(
            Vector3 target)
        {
            if (figureCamera == null)
            {
                return false;
            }

            Vector3 origin =
                figureCamera.transform.position;

            Vector3 delta =
                target -
                origin;

            float distance =
                delta.magnitude;

            if (distance <= 0.001f)
            {
                return true;
            }

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    delta / distance,
                    lineOfSightHits,
                    distance,
                    lineOfSightMask,
                    QueryTriggerInteraction.Ignore);

            float nearestBlockingDistance =
                float.PositiveInfinity;

            Transform masterpieceRoot =
                deployment != null &&
                deployment.ActiveInstance != null
                    ? deployment.ActiveInstance.transform
                    : null;

            for (int index = 0;
                 index < hitCount;
                 index++)
            {
                Collider collider =
                    lineOfSightHits[index].collider;

                if (collider == null)
                {
                    continue;
                }

                Transform hitTransform =
                    collider.transform;

                if (
                    IsInside(
                        hitTransform,
                        figureRoot) ||
                    IsInside(
                        hitTransform,
                        masterpieceRoot)
                )
                {
                    continue;
                }

                nearestBlockingDistance =
                    Mathf.Min(
                        nearestBlockingDistance,
                        lineOfSightHits[index].distance);
            }

            return nearestBlockingDistance >=
                distance -
                Mathf.Max(
                    0f,
                    lineOfSightSkin);
        }

        private void UpdatePositionExposure()
        {
            positionExposed =
                figureRoleActive &&
                (
                    carrying ||
                    sightActive
                );

            if (
                !positionExposed ||
                figureRoot == null
            )
            {
                SetLineVisible(
                    exposureBeam,
                    false);

                SetLineVisible(
                    exposureRingA,
                    false);

                SetLineVisible(
                    exposureRingB,
                    false);

                return;
            }

            Vector3 basePosition =
                figureRoot.position +
                Vector3.up *
                0.045f;

            Color color =
                sightActive
                    ? new Color(
                        1f,
                        0.20f,
                        0.035f,
                        0.96f)
                    : new Color(
                        1f,
                        0.58f,
                        0.06f,
                        0.90f);

            SetAxis(
                exposureBeam,
                basePosition,
                basePosition +
                Vector3.up *
                2.45f,
                color);

            float pulse =
                1f +
                Mathf.Sin(
                    Time.unscaledTime *
                    5f) *
                0.08f;

            SetWorldCircle(
                exposureRingA,
                basePosition,
                Vector3.up,
                0.62f *
                pulse,
                color,
                30);

            SetWorldCircle(
                exposureRingB,
                basePosition +
                Vector3.up *
                1.72f,
                Vector3.up,
                0.34f *
                pulse,
                color,
                24);
        }

        private void UpdateRevealVisuals()
        {
            if (
                !figureRoleActive ||
                !sightActive
            )
            {
                HideWeakPoints();
                HideDraftEcho();
                return;
            }

            UpdateWeakPointReveal();
            UpdateDraftEcho();
        }

        private void UpdateWeakPointReveal()
        {
            PrototypeMasterpieceWorldInstance instance =
                deployment != null
                    ? deployment.ActiveInstance
                    : null;

            weakPointRevealCount = 0;

            if (instance == null)
            {
                HideWeakPoints();
                return;
            }

            for (int index = 0;
                 index < RevealedKinds.Length;
                 index++)
            {
                LineRenderer line =
                    weakPointLines[index];

                PrototypeMasterpiecePartKind kind =
                    RevealedKinds[index];

                if (
                    !instance.TryGetWorldPart(
                        kind,
                        out PrototypeMasterpieceWorldPart part) ||
                    part == null
                )
                {
                    SetLineVisible(
                        line,
                        false);

                    continue;
                }

                Vector3 center =
                    part.ProxyCollider != null
                        ? part.ProxyCollider.bounds.center
                        : part.transform.position;

                float radius =
                    ResolveWeakPointRadius(
                        part);

                int sides =
                    ResolveShapeSides(
                        kind);

                Color color =
                    ResolveWeakPointColor(
                        part);

                SetBillboardShape(
                    line,
                    center,
                    radius,
                    sides,
                    color);

                if (
                    part.CapabilityActive &&
                    kind !=
                        PrototypeMasterpiecePartKind.Core
                )
                {
                    weakPointRevealCount++;
                }
            }
        }

        private void UpdateDraftEcho()
        {
            PrototypeMasterpieceWorldInstance instance =
                deployment != null
                    ? deployment.ActiveInstance
                    : null;

            draftSignalAvailable =
                sideCanvas != null &&
                sideCanvas.StrokeCount > 0 &&
                sideCanvas.Strokes != null;

            draftStrokeRevealCount = 0;

            if (
                !draftSignalAvailable ||
                instance == null ||
                figureCamera == null
            )
            {
                HideDraftEcho();
                return;
            }

            Vector3 anchor =
                instance.transform.position +
                Vector3.up *
                draftHeight;

            Vector3 right =
                figureCamera.transform.right;

            Vector3 up =
                figureCamera.transform.up;

            int strokeCount =
                Mathf.Min(
                    sideCanvas.Strokes.Count,
                    draftLines.Count);

            for (int index = 0;
                 index < draftLines.Count;
                 index++)
            {
                LineRenderer line =
                    draftLines[index];

                if (index >= strokeCount)
                {
                    SetLineVisible(
                        line,
                        false);

                    continue;
                }

                PrototypeSideCanvasStroke stroke =
                    sideCanvas.Strokes[index];

                if (
                    stroke == null ||
                    stroke.Points == null ||
                    stroke.Points.Count < 2
                )
                {
                    SetLineVisible(
                        line,
                        false);

                    continue;
                }

                int pointCount =
                    Mathf.Min(
                        stroke.Points.Count,
                        maximumDraftPointsPerStroke);

                line.loop = false;
                line.positionCount =
                    pointCount;

                Color color =
                    new Color(
                        0.08f,
                        0.88f,
                        0.92f,
                        0.86f);

                line.startColor = color;
                line.endColor = color;
                line.widthMultiplier =
                    Mathf.Clamp(
                        stroke.Width *
                        1.6f,
                        0.018f,
                        0.055f);

                for (int pointIndex = 0;
                     pointIndex < pointCount;
                     pointIndex++)
                {
                    Vector2 point =
                        stroke.Points[
                            pointIndex];

                    Vector3 worldPoint =
                        anchor +
                        right *
                        (
                            point.x -
                            0.5f
                        ) *
                        draftWorldSize.x +
                        up *
                        (
                            point.y -
                            0.5f
                        ) *
                        draftWorldSize.y;

                    line.SetPosition(
                        pointIndex,
                        worldPoint);
                }

                line.enabled = true;
                draftStrokeRevealCount++;
            }

            Color frameColor =
                new Color(
                    0.08f,
                    0.76f,
                    0.82f,
                    0.78f);

            draftFrame.positionCount = 4;
            draftFrame.loop = true;
            draftFrame.startColor =
                frameColor;

            draftFrame.endColor =
                frameColor;

            draftFrame.SetPosition(
                0,
                anchor -
                right *
                draftWorldSize.x *
                0.5f -
                up *
                draftWorldSize.y *
                0.5f);

            draftFrame.SetPosition(
                1,
                anchor +
                right *
                draftWorldSize.x *
                0.5f -
                up *
                draftWorldSize.y *
                0.5f);

            draftFrame.SetPosition(
                2,
                anchor +
                right *
                draftWorldSize.x *
                0.5f +
                up *
                draftWorldSize.y *
                0.5f);

            draftFrame.SetPosition(
                3,
                anchor -
                right *
                draftWorldSize.x *
                0.5f +
                up *
                draftWorldSize.y *
                0.5f);

            draftFrame.enabled = true;
        }

        private float ResolveWeakPointRadius(
            PrototypeMasterpieceWorldPart part)
        {
            if (
                part != null &&
                part.ProxyCollider != null
            )
            {
                Bounds bounds =
                    part.ProxyCollider.bounds;

                float maximumExtent =
                    Mathf.Max(
                        bounds.extents.x,
                        Mathf.Max(
                            bounds.extents.y,
                            bounds.extents.z));

                return Mathf.Clamp(
                    maximumExtent *
                    1.35f,
                    0.20f,
                    0.52f);
            }

            return 0.30f;
        }

        private static int ResolveShapeSides(
            PrototypeMasterpiecePartKind kind)
        {
            switch (kind)
            {
                case PrototypeMasterpiecePartKind.Core:
                    return 4;

                case PrototypeMasterpiecePartKind.LeftAttack:
                case PrototypeMasterpiecePartKind.RightAttack:
                    return 3;

                case PrototypeMasterpiecePartKind.LeftContact:
                case PrototypeMasterpiecePartKind.RightContact:
                    return 4;

                default:
                    return 24;
            }
        }

        private static Color ResolveWeakPointColor(
            PrototypeMasterpieceWorldPart part)
        {
            if (part == null)
            {
                return Color.gray;
            }

            if (!part.CapabilityActive)
            {
                return new Color(
                    0.42f,
                    0.45f,
                    0.48f,
                    0.72f);
            }

            if (
                part.Kind ==
                PrototypeMasterpiecePartKind.Core
            )
            {
                return new Color(
                    0.82f,
                    0.32f,
                    0.94f,
                    0.90f);
            }

            return new Color(
                0.08f,
                0.90f,
                0.94f,
                0.94f);
        }

        private void SetBillboardShape(
            LineRenderer line,
            Vector3 center,
            float radius,
            int sides,
            Color color)
        {
            if (
                line == null ||
                figureCamera == null
            )
            {
                return;
            }

            int pointCount =
                Mathf.Max(
                    3,
                    sides);

            line.loop = true;
            line.positionCount =
                pointCount;

            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier =
                0.045f;

            Vector3 right =
                figureCamera.transform.right;

            Vector3 up =
                figureCamera.transform.up;

            float rotationOffset =
                pointCount == 4
                    ? Mathf.PI *
                      0.25f
                    : -Mathf.PI *
                      0.5f;

            for (int index = 0;
                 index < pointCount;
                 index++)
            {
                float angle =
                    index /
                    (float)pointCount *
                    Mathf.PI *
                    2f +
                    rotationOffset;

                line.SetPosition(
                    index,
                    center +
                    right *
                    (
                        Mathf.Cos(angle) *
                        radius
                    ) +
                    up *
                    (
                        Mathf.Sin(angle) *
                        radius
                    ));
            }

            line.enabled = true;
        }

        private static void SetAxis(
            LineRenderer line,
            Vector3 start,
            Vector3 end,
            Color color)
        {
            if (line == null)
            {
                return;
            }

            line.loop = false;
            line.positionCount = 2;
            line.startColor = color;
            line.endColor = color;
            line.SetPosition(
                0,
                start);

            line.SetPosition(
                1,
                end);

            line.enabled = true;
        }

        private static void SetWorldCircle(
            LineRenderer line,
            Vector3 center,
            Vector3 normal,
            float radius,
            Color color,
            int segments)
        {
            if (line == null)
            {
                return;
            }

            Vector3 normalized =
                normal.sqrMagnitude > 0.001f
                    ? normal.normalized
                    : Vector3.up;

            Vector3 tangent =
                Vector3.Cross(
                    normalized,
                    Mathf.Abs(
                        Vector3.Dot(
                            normalized,
                            Vector3.up)) >
                        0.92f
                        ? Vector3.right
                        : Vector3.up);

            if (tangent.sqrMagnitude <= 0.001f)
            {
                tangent = Vector3.right;
            }
            else
            {
                tangent.Normalize();
            }

            Vector3 bitangent =
                Vector3.Cross(
                    normalized,
                    tangent).normalized;

            int pointCount =
                Mathf.Max(
                    8,
                    segments);

            line.loop = true;
            line.positionCount =
                pointCount;

            line.startColor = color;
            line.endColor = color;

            for (int index = 0;
                 index < pointCount;
                 index++)
            {
                float angle =
                    index /
                    (float)pointCount *
                    Mathf.PI *
                    2f;

                line.SetPosition(
                    index,
                    center +
                    tangent *
                    (
                        Mathf.Cos(angle) *
                        radius
                    ) +
                    bitangent *
                    (
                        Mathf.Sin(angle) *
                        radius
                    ));
            }

            line.enabled = true;
        }

        private Vector3 ResolveDropPosition()
        {
            if (figureRoot == null)
            {
                return transform.position;
            }

            return
                figureRoot.position +
                Vector3.up *
                0.75f +
                figureRoot.forward *
                0.55f;
        }

        private Vector3 ResolveWorldDropVelocity()
        {
            if (figureRoot == null)
            {
                return dropVelocity;
            }

            return
                figureRoot.right *
                dropVelocity.x +
                Vector3.up *
                dropVelocity.y +
                figureRoot.forward *
                dropVelocity.z;
        }

        private void HideWeakPoints()
        {
            weakPointRevealCount = 0;

            for (int index = 0;
                 index < weakPointLines.Count;
                 index++)
            {
                SetLineVisible(
                    weakPointLines[index],
                    false);
            }
        }

        private void HideDraftEcho()
        {
            draftStrokeRevealCount = 0;
            draftSignalAvailable = false;

            for (int index = 0;
                 index < draftLines.Count;
                 index++)
            {
                SetLineVisible(
                    draftLines[index],
                    false);
            }

            SetLineVisible(
                draftFrame,
                false);
        }

        private void HideRevealVisuals()
        {
            HideWeakPoints();
            HideDraftEcho();

            SetLineVisible(
                exposureBeam,
                false);

            SetLineVisible(
                exposureRingA,
                false);

            SetLineVisible(
                exposureRingB,
                false);

            positionExposed = false;
        }

        private static void SetLineVisible(
            LineRenderer line,
            bool visible)
        {
            if (line != null)
            {
                line.enabled = visible;
            }
        }

        private void RefreshHud()
        {
            bool visible =
                figureRoleActive &&
                (
                    token != null ||
                    expended ||
                    (
                        severing != null &&
                        severing.EyeSevered
                    )
                );

            if (statusGroup != null)
            {
                statusGroup.alpha =
                    visible ? 1f : 0f;

                statusGroup.interactable = false;
                statusGroup.blocksRaycasts = false;
            }

            if (!visible)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    "ÇALINMIŞ TANIM • HEAD / PERCEPTION";
            }

            if (stateText != null)
            {
                stateText.text =
                    $"DURUM {TranslateState(state)}   " +
                    $"ALMA %{pickupProgress * 100f:F0}   " +
                    $"SÜRE {(sightActive ? sightRemainingSeconds.ToString("F1") : "-")} sn\n" +
                    $"SLOT {(carrying ? "1/1" : "0/1")}   " +
                    $"TASLAK {(draftSignalAvailable ? "VAR" : "YOK")}   " +
                    $"ZAYIF DÜĞÜM {weakPointRevealCount}   " +
                    $"KONUM {(positionExposed ? "AÇIK" : "GİZLİ")}\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    ResolveControlText();
            }
        }

        private string ResolveControlText()
        {
            if (state ==
                PrototypeStolenEyeDefinitionState.Expended)
            {
                return
                    "HEAD / PERCEPTION TANIMI TÜKENDİ • PERCEPTION HÂLÂ KAPALI\n" +
                    "M50.0 RESTORE İLE PROTOTİPİ SIFIRLA";
            }

            if (sightActive)
            {
                return
                    "ALGI YANKISI AKTİF • TASLAK + CAPABILITY DÜĞÜMLERİ\n" +
                    "BEDEL: FIGÜR KONUMU DÜNYADA AÇIK";
            }

            if (carrying)
            {
                return
                    "E • ALGI YANKISINÜ KULLAN (6 sn)\n" +
                    "TEK SLOT • HAM HASAR YOK • KONUM AÇIK";
            }

            return
                "E BASILI TUT • YERDEKİ HEAD / PERCEPTION TANIMINI AL\n" +
                "TEK ÇALINMIŞ TANIM SLOTU";
        }

        private static string TranslateState(
            PrototypeStolenEyeDefinitionState value)
        {
            switch (value)
            {
                case PrototypeStolenEyeDefinitionState.PainterRole:
                    return "PAINTER ROLÜ";

                case PrototypeStolenEyeDefinitionState.TokenFalling:
                    return "YERE DÜŞÜYOR";

                case PrototypeStolenEyeDefinitionState.OutOfRange:
                    return "MENZİL DIŞI";

                case PrototypeStolenEyeDefinitionState.AimRequired:
                    return "NİŞAN GEREKLİ";

                case PrototypeStolenEyeDefinitionState.Occluded:
                    return "ENGELLİ";

                case PrototypeStolenEyeDefinitionState.ReadyToPickup:
                    return "ALMAYA HAZIR";

                case PrototypeStolenEyeDefinitionState.PickingUp:
                    return "ALINIYOR";

                case PrototypeStolenEyeDefinitionState.Carrying:
                    return "TAŞINIYOR";

                case PrototypeStolenEyeDefinitionState.SightActive:
                    return "ALGI YANKISI";

                case PrototypeStolenEyeDefinitionState.Expended:
                    return "TÜKENDİ";

                default:
                    return "KESME BEKLENİYOR";
            }
        }

        private static bool IsInside(
            Transform candidate,
            Transform root)
        {
            return
                candidate != null &&
                root != null &&
                (
                    candidate == root ||
                    candidate.IsChildOf(root)
                );
        }

        private void OnDisable()
        {
            if (
                contextAction != null &&
                contextAction.enabled
            )
            {
                contextAction.Disable();
            }

            inputActionActive = false;
            HideRevealVisuals();
        }

        private void OnDestroy()
        {
            if (contextAction != null)
            {
                contextAction.Dispose();
                contextAction = null;
            }

            if (revealMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        revealMaterial);
                }
                else
                {
                    DestroyImmediate(
                        revealMaterial);
                }

                revealMaterial = null;
            }
        }
    }
}
