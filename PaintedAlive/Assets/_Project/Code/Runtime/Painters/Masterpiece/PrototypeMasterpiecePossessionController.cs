using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Painters.SideCanvas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(25000)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpiecePossessionController :
        MonoBehaviour
    {
        private sealed class BehaviourState
        {
            public Behaviour Behaviour;
            public bool Enabled;
        }

        private sealed class PartPose
        {
            public Transform Transform;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        private sealed class TransformState
        {
            public Transform Transform;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        [Header("Sources")]
        [SerializeField]
        private PrototypeMasterpieceWorldDeploymentController deployment;

        [SerializeField] private Camera mainCamera;

        [SerializeField]
        private MonoBehaviour painterBrushController;

        [Header("Movement")]
        [SerializeField, Min(0.1f)]
        private float moveSpeed = 2.8f;

        [SerializeField, Min(0.1f)]
        private float sprintSpeed = 4.4f;

        [SerializeField, Min(1f)]
        private float turnSpeedDegrees = 520f;

        [SerializeField, Min(0.5f)]
        private float leashRadius = 9f;

        [SerializeField, Min(0.05f)]
        private float movementProbeRadius = 0.42f;

        [SerializeField, Min(0.5f)]
        private float groundProbeHeight = 3f;

        [SerializeField, Min(0.5f)]
        private float groundProbeDistance = 8f;

        [SerializeField]
        private LayerMask collisionMask = ~0;

        [Header("Camera")]
        [SerializeField, Min(1f)]
        private float cameraDistance = 4.6f;

        [SerializeField, Min(0.5f)]
        private float cameraTargetHeight = 1.45f;

        [SerializeField, Range(5f, 70f)]
        private float minimumPitch = 12f;

        [SerializeField, Range(10f, 80f)]
        private float maximumPitch = 48f;

        [SerializeField, Min(0.001f)]
        private float mouseSensitivity = 0.075f;

        [SerializeField, Min(0.01f)]
        private float cameraCollisionRadius = 0.18f;

        [SerializeField, Range(25f, 90f)]
        private float possessionFieldOfView = 56f;

        [Header("Attack Preview")]
        [SerializeField, Min(0.05f)]
        private float attackTelegraphSeconds = 0.18f;

        [SerializeField, Min(0.05f)]
        private float attackSwingSeconds = 0.16f;

        [SerializeField, Min(0.05f)]
        private float attackRecoverySeconds = 0.22f;

        [SerializeField, Min(0.05f)]
        private float attackCooldownSeconds = 0.72f;

        [SerializeField, Min(0.2f)]
        private float attackTelegraphRadius = 1.65f;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeMasterpieceWorldInstance possessedInstance;

        [SerializeField] private bool possessed;
        [SerializeField] private bool inputActionsActive;
        [SerializeField] private bool painterRoleActive;
        [SerializeField] private int roleRejectedActionCount;
        [SerializeField] private int roleForcedReleaseCount;
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private bool cameraOverrideActive;
        [SerializeField] private bool inputAuthorityActive;
        [SerializeField] private bool attackPreviewActive;
        [SerializeField] private int possessionCount;
        [SerializeField] private int releaseCount;
        [SerializeField] private int rejectedPossessionCount;
        [SerializeField] private int movementStepCount;
        [SerializeField] private int blockedMovementCount;
        [SerializeField] private int attackPreviewCount;
        [SerializeField] private int blockedAttackCount;
        [SerializeField] private int inputConflictCount;
        [SerializeField] private int suppressedBehaviourCount;
        [SerializeField] private int m45KnownBlockerCount;
        [SerializeField] private int painterRoleListBlockerCount;
        [SerializeField] private int cameraRigBehaviourCount;
        [SerializeField] private int frozenAuthorityTransformCount;
        [SerializeField] private int cameraLateWriteCount;
        [SerializeField] private int cameraAuthorityConflictCount;
        [SerializeField] private bool cameraReparented;
        [SerializeField] private float travelledDistance;
        [SerializeField] private float currentLeashDistance;
        [SerializeField] private float averageFrameMilliseconds;
        [SerializeField] private float maximumFrameMilliseconds;
        [SerializeField] private string lastAction =
            "Deploy edilmiş Baş Yapıt bekleniyor.";

        private readonly List<BehaviourState> suppressedBehaviours =
            new List<BehaviourState>();

        private readonly List<BehaviourState> cameraBrainStates =
            new List<BehaviourState>();

        private readonly List<TransformState> frozenAuthorityTransforms =
            new List<TransformState>();

        private readonly Dictionary<
            PrototypeMasterpiecePartKind,
            PartPose> partPoses =
            new Dictionary<
                PrototypeMasterpiecePartKind,
                PartPose>();

        private InputAction togglePossessionAction;
        private InputAction releasePossessionAction;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction attackAction;

        private Vector3 possessionOrigin;
        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;
        private float savedCameraFieldOfView;
        private Transform savedCameraParent;
        private int savedCameraSiblingIndex;
        private Vector3 savedCameraLocalPosition;
        private Quaternion savedCameraLocalRotation;
        private Vector3 savedCameraLocalScale;
        private CursorLockMode savedCursorLockMode;
        private bool savedCursorVisible;
        private bool savedProxyDebugVisible;
        private float cameraYaw;
        private float cameraPitch = 24f;
        private float locomotionPhase;
        private float lastAttackAt = -100f;
        private bool nextAttackUsesLeft = true;
        private Coroutine attackRoutine;
        private LineRenderer attackTelegraph;
        private Material attackTelegraphMaterial;

        public bool Possessed => possessed;
        public bool InputActionsActive => inputActionsActive;
        public bool PainterRoleActive => painterRoleActive;
        public int RoleRejectedActionCount => roleRejectedActionCount;
        public int RoleForcedReleaseCount => roleForcedReleaseCount;
        public string ResolvedRole => resolvedRole;
        public bool CameraOverrideActive => cameraOverrideActive;
        public bool InputAuthorityActive => inputAuthorityActive;
        public bool AttackPreviewActive => attackPreviewActive;
        public int PossessionCount => possessionCount;
        public int ReleaseCount => releaseCount;
        public int RejectedPossessionCount => rejectedPossessionCount;
        public int MovementStepCount => movementStepCount;
        public int BlockedMovementCount => blockedMovementCount;
        public int AttackPreviewCount => attackPreviewCount;
        public int BlockedAttackCount => blockedAttackCount;
        public int InputConflictCount => inputConflictCount;
        public int SuppressedBehaviourCount =>
            suppressedBehaviourCount;
        public int M45KnownBlockerCount =>
            m45KnownBlockerCount;
        public int PainterRoleListBlockerCount =>
            painterRoleListBlockerCount;
        public int CameraRigBehaviourCount =>
            cameraRigBehaviourCount;
        public int FrozenAuthorityTransformCount =>
            frozenAuthorityTransformCount;
        public int CameraLateWriteCount =>
            cameraLateWriteCount;
        public int CameraAuthorityConflictCount =>
            cameraAuthorityConflictCount;
        public bool CameraReparented =>
            cameraReparented;
        public float TravelledDistance => travelledDistance;
        public float CurrentLeashDistance => currentLeashDistance;
        public float AverageFrameMilliseconds =>
            averageFrameMilliseconds;
        public float MaximumFrameMilliseconds =>
            maximumFrameMilliseconds;
        public string LastAction => lastAction;

        public bool BossAIEnabled => false;
        public bool DamageAuthorityEnabled => false;
        public bool HitAuthorityEnabled => false;
        public bool NetworkIntegrationParked => true;
        public bool ModerationLayerEnabled => false;
        public bool SafeSilhouetteFallbackEnabled => false;

        public void Configure(
            PrototypeMasterpieceWorldDeploymentController
                configuredDeployment,
            Camera configuredMainCamera,
            MonoBehaviour configuredPainterBrushController,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            deployment = configuredDeployment;
            mainCamera = configuredMainCamera;
            painterBrushController =
                configuredPainterBrushController;

            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            RefreshHud();
        }

        private void Awake()
        {
            EnsureInputActions();
            EnsureAttackTelegraph();
            RefreshRoleAuthority(forceInputRefresh: true);
            RefreshHud();
        }

        private void OnEnable()
        {
            EnsureInputActions();
            RefreshRoleAuthority(forceInputRefresh: true);
            RefreshHud();
        }

        private void OnDisable()
        {
            if (possessed)
            {
                EndPossessionInternal(
                    countRelease: false,
                    "M48 controller devre dışı kaldı.");
            }

            DisableInputActions();
        }

        private void Update()
        {
            RefreshRoleAuthority(forceInputRefresh: false);

            if (!painterRoleActive && possessed)
            {
                roleForcedReleaseCount++;
                EndPossessionInternal(
                    countRelease: true,
                    "Rol Figure oldu; sahiplenme otomatik bırakıldı.");
            }

            if (possessed)
            {
                if (possessedInstance == null ||
                    deployment == null ||
                    deployment.ActiveInstance !=
                        possessedInstance)
                {
                    EndPossessionInternal(
                        countRelease: true,
                        "Dünya Baş Yapıtı geri çağrıldı.");
                }
                else
                {
                    UpdatePerformanceTelemetry();
                    EnforceInputAuthority();
                    UpdatePartAnimation(
                        Time.unscaledDeltaTime);
                }
            }

            RefreshHud();
        }

        private void LateUpdate()
        {
            if (!possessed ||
                possessedInstance == null)
            {
                return;
            }

            EnforceInputAuthority();
            EnforceCameraRigAuthority();
            EnforceFrozenAuthorityTransforms();
            UpdatePossessionCamera();
            cameraLateWriteCount++;
        }

        private void FixedUpdate()
        {
            if (!possessed ||
                possessedInstance == null)
            {
                return;
            }

            SimulateMovement(
                Time.fixedDeltaTime);
        }

        public void TogglePossession()
        {
            RefreshRoleAuthority(forceInputRefresh: false);

            if (!painterRoleActive)
            {
                if (possessed)
                {
                    roleForcedReleaseCount++;
                    EndPossessionInternal(
                        countRelease: true,
                        "Rol Figure oldu; sahiplenme otomatik bırakıldı.");
                }
                else
                {
                    RejectRoleAction("Sahiplenme");
                }

                return;
            }

            if (possessed)
            {
                EndPossessionInternal(
                    countRelease: true,
                    "Sahiplenme Ressam tarafından bırakıldı.");

                return;
            }

            TryBeginPossession();
        }

        public bool TryBeginPossession()
        {
            RefreshRoleAuthority(forceInputRefresh: false);

            if (!painterRoleActive)
            {
                RejectRoleAction("Sahiplenme");
                return false;
            }

            if (deployment == null)
            {
                RejectPossession(
                    "M47 deployment controller bağlı değil.");
                return false;
            }

            PrototypeMasterpieceWorldInstance candidate =
                deployment.ActiveInstance;

            if (candidate == null)
            {
                RejectPossession(
                    "Önce V ile Baş Yapıtı dünyaya deploy et.");
                return false;
            }

            if (!candidate.BuildSucceeded ||
                candidate.KinematicRigidbody == null ||
                !candidate.KinematicRigidbody.isKinematic)
            {
                RejectPossession(
                    "Dünya instance kinematic kontrol için hazır değil.");
                return false;
            }

            if (mainCamera == null)
            {
                mainCamera =
                    Camera.main;

                if (mainCamera == null)
                {
                    RejectPossession(
                        "Main Camera bulunamadı.");
                    return false;
                }
            }

            possessedInstance = candidate;
            possessionOrigin =
                possessedInstance.transform.position;

            cameraYaw =
                possessedInstance.transform.eulerAngles.y;

            cameraPitch =
                Mathf.Clamp(
                    cameraPitch,
                    minimumPitch,
                    maximumPitch);

            savedProxyDebugVisible =
                possessedInstance.ProxyDebugVisible;

            possessedInstance.SetProxyDebugVisible(
                false);

            CapturePartPoses();
            CaptureCameraAuthority();
            CaptureInputAuthority();
            CaptureCursorAuthority();

            possessed = true;
            possessionCount++;
            locomotionPhase = 0f;
            travelledDistance = 0f;
            currentLeashDistance = 0f;
            averageFrameMilliseconds = 0f;
            maximumFrameMilliseconds = 0f;

            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;

            lastAction =
                "Baş Yapıt doğrudan kontrol ediliyor.";

            Debug.Log(
                "[M48 Possession Begin]\n" +
                $"Instance={possessedInstance.name}\n" +
                $"SnapshotHash={possessedInstance.SnapshotHash}\n" +
                $"PartCount={possessedInstance.PartCount}\n" +
                $"ColliderCount={possessedInstance.ColliderCount}\n" +
                $"CameraOverride={cameraOverrideActive}\n" +
                $"CameraReparented={cameraReparented}\n" +
                $"InputAuthority={inputAuthorityActive}\n" +
                $"SuppressedBehaviours={suppressedBehaviourCount}\n" +
                $"M45KnownBlockers={m45KnownBlockerCount}\n" +
                $"PainterRoleListBlockers={painterRoleListBlockerCount}\n" +
                $"CameraRigBehaviours={cameraRigBehaviourCount}\n" +
                $"FrozenAuthorityTransforms={frozenAuthorityTransformCount}",
                possessedInstance);

            return true;
        }

        public void EndPossession()
        {
            if (!possessed)
            {
                return;
            }

            EndPossessionInternal(
                countRelease: true,
                "Sahiplenme bırakıldı.");
        }

        public void TriggerAttackPreview()
        {
            RefreshRoleAuthority(forceInputRefresh: false);

            if (!painterRoleActive)
            {
                RejectRoleAction("Doğrudan kontrol saldırısı");
                return;
            }

            if (!possessed ||
                possessedInstance == null)
            {
                blockedAttackCount++;
                lastAction =
                    "Saldırı ön izlemesi için önce P ile sahiplen.";
                return;
            }

            if (attackPreviewActive ||
                Time.unscaledTime -
                    lastAttackAt <
                    attackCooldownSeconds)
            {
                blockedAttackCount++;
                lastAction =
                    "Saldırı ön izlemesi cooldown içinde.";
                return;
            }

            PrototypeMasterpiecePartKind selectedKind;

            if (!TrySelectAttackPart(
                    out selectedKind))
            {
                blockedAttackCount++;
                lastAction =
                    "Aktif saldırı capability'si bulunamadı.";
                return;
            }

            if (attackRoutine != null)
            {
                StopCoroutine(
                    attackRoutine);
            }

            attackRoutine =
                StartCoroutine(
                    AttackPreviewRoutine(
                        selectedKind));
        }

        private void RefreshRoleAuthority(bool forceInputRefresh)
        {
            bool previous = painterRoleActive;
            painterRoleActive =
                PrototypeRoleAuthorityResolver.IsPainter(
                    out resolvedRole);

            if (!forceInputRefresh && previous == painterRoleActive)
            {
                return;
            }

            if (painterRoleActive)
            {
                EnableInputActions();
            }
            else
            {
                DisableInputActions();
            }
        }

        private void RejectRoleAction(string action)
        {
            roleRejectedActionCount++;
            lastAction =
                $"{action} yalnız Painter rolünde kullanılabilir.";

            Debug.Log(
                "[Role Authority Block]\n" +
                $"Action={action}\n" +
                $"ResolvedRole={resolvedRole}\n" +
                "RequiredRole=Painter\n" +
                "Severity=Info",
                this);

            RefreshHud();
        }

        private void EnsureInputActions()
        {
            if (togglePossessionAction == null)
            {
                togglePossessionAction =
                    new InputAction(
                        "M48 Toggle Possession",
                        InputActionType.Button,
                        "<Keyboard>/p");

                togglePossessionAction.performed +=
                    HandleTogglePossession;
            }

            if (releasePossessionAction == null)
            {
                releasePossessionAction =
                    new InputAction(
                        "M48 Release Possession",
                        InputActionType.Button,
                        "<Keyboard>/escape");

                releasePossessionAction.performed +=
                    HandleReleasePossession;
            }

            if (moveAction == null)
            {
                moveAction =
                    new InputAction(
                        "M48 Move",
                        InputActionType.Value);

                moveAction
                    .AddCompositeBinding(
                        "2DVector")
                    .With(
                        "Up",
                        "<Keyboard>/w")
                    .With(
                        "Down",
                        "<Keyboard>/s")
                    .With(
                        "Left",
                        "<Keyboard>/a")
                    .With(
                        "Right",
                        "<Keyboard>/d");
            }

            if (lookAction == null)
            {
                lookAction =
                    new InputAction(
                        "M48 Look",
                        InputActionType.PassThrough,
                        "<Mouse>/delta");
            }

            if (sprintAction == null)
            {
                sprintAction =
                    new InputAction(
                        "M48 Sprint",
                        InputActionType.Button,
                        "<Keyboard>/leftShift");
            }

            if (attackAction == null)
            {
                attackAction =
                    new InputAction(
                        "M48 Attack Preview",
                        InputActionType.Button,
                        "<Mouse>/leftButton");

                attackAction.performed +=
                    HandleAttackPreview;
            }
        }

        private void EnableInputActions()
        {
            togglePossessionAction?.Enable();
            releasePossessionAction?.Enable();
            moveAction?.Enable();
            lookAction?.Enable();
            sprintAction?.Enable();
            attackAction?.Enable();

            inputActionsActive =
                togglePossessionAction != null &&
                togglePossessionAction.enabled &&
                releasePossessionAction != null &&
                releasePossessionAction.enabled &&
                moveAction != null &&
                moveAction.enabled &&
                lookAction != null &&
                lookAction.enabled &&
                sprintAction != null &&
                sprintAction.enabled &&
                attackAction != null &&
                attackAction.enabled;
        }

        private void DisableInputActions()
        {
            togglePossessionAction?.Disable();
            releasePossessionAction?.Disable();
            moveAction?.Disable();
            lookAction?.Disable();
            sprintAction?.Disable();
            attackAction?.Disable();
            inputActionsActive = false;
        }

        private void HandleTogglePossession(
            InputAction.CallbackContext context)
        {
            TogglePossession();
        }

        private void HandleReleasePossession(
            InputAction.CallbackContext context)
        {
            if (possessed)
            {
                EndPossessionInternal(
                    countRelease: true,
                    "Escape ile sahiplenme bırakıldı.");
            }
        }

        private void HandleAttackPreview(
            InputAction.CallbackContext context)
        {
            if (possessed)
            {
                TriggerAttackPreview();
            }
        }

        private void SimulateMovement(
            float deltaTime)
        {
            Rigidbody body =
                possessedInstance.KinematicRigidbody;

            if (body == null)
            {
                return;
            }

            Vector2 input =
                moveAction != null
                    ? Vector2.ClampMagnitude(
                        moveAction.ReadValue<Vector2>(),
                        1f)
                    : Vector2.zero;

            Quaternion yawRotation =
                Quaternion.Euler(
                    0f,
                    cameraYaw,
                    0f);

            Vector3 desiredDirection =
                yawRotation *
                new Vector3(
                    input.x,
                    0f,
                    input.y);

            float inputMagnitude =
                Mathf.Clamp01(
                    desiredDirection.magnitude);

            if (desiredDirection.sqrMagnitude >
                0.0001f)
            {
                desiredDirection.Normalize();
            }

            bool leftMovement =
                possessedInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.LeftMovement);

            bool rightMovement =
                possessedInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.RightMovement);

            int movementCapabilities =
                (leftMovement ? 1 : 0) +
                (rightMovement ? 1 : 0);

            float capabilityMultiplier =
                movementCapabilities >= 2
                    ? 1f
                    : movementCapabilities == 1
                        ? 0.55f
                        : 0f;

            float selectedSpeed =
                sprintAction != null &&
                sprintAction.IsPressed()
                    ? sprintSpeed
                    : moveSpeed;

            selectedSpeed *=
                capabilityMultiplier;

            Quaternion targetRotation =
                Quaternion.Euler(
                    0f,
                    cameraYaw,
                    0f);

            body.MoveRotation(
                Quaternion.RotateTowards(
                    body.rotation,
                    targetRotation,
                    turnSpeedDegrees *
                    deltaTime));

            if (inputMagnitude <= 0.001f ||
                selectedSpeed <= 0f)
            {
                return;
            }

            Vector3 current =
                body.position;

            Vector3 delta =
                desiredDirection *
                selectedSpeed *
                inputMagnitude *
                deltaTime;

            float distance =
                delta.magnitude;

            if (distance <= 0.0001f)
            {
                return;
            }

            Vector3 obstructionProbeOrigin =
                current +
                Vector3.up *
                0.78f;

            if (Physics.SphereCast(
                    obstructionProbeOrigin,
                    movementProbeRadius,
                    desiredDirection,
                    out _,
                    distance + 0.05f,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                blockedMovementCount++;
                lastAction =
                    "Hareket çevre proxy sorgusu tarafından engellendi.";
                return;
            }

            Vector3 target =
                current +
                delta;

            Vector3 horizontalOffset =
                Vector3.ProjectOnPlane(
                    target -
                    possessionOrigin,
                    Vector3.up);

            if (horizontalOffset.magnitude >
                leashRadius)
            {
                horizontalOffset =
                    horizontalOffset.normalized *
                    leashRadius;

                target =
                    new Vector3(
                        possessionOrigin.x +
                            horizontalOffset.x,
                        target.y,
                        possessionOrigin.z +
                            horizontalOffset.z);

                blockedMovementCount++;
                lastAction =
                    "M48 test leash sınırına ulaşıldı.";
            }

            Vector3 groundOrigin =
                target +
                Vector3.up *
                groundProbeHeight;

            if (!Physics.Raycast(
                    groundOrigin,
                    Vector3.down,
                    out RaycastHit groundHit,
                    groundProbeDistance,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                blockedMovementCount++;
                lastAction =
                    "Altında güvenli zemin bulunmadığı için hareket durduruldu.";
                return;
            }

            target.y =
                groundHit.point.y +
                0.02f;

            body.MovePosition(
                target);

            travelledDistance +=
                Vector3.Distance(
                    current,
                    target);

            currentLeashDistance =
                Vector3.ProjectOnPlane(
                    target -
                    possessionOrigin,
                    Vector3.up)
                .magnitude;

            movementStepCount++;
        }

        private void UpdatePossessionCamera()
        {
            if (mainCamera == null ||
                possessedInstance == null)
            {
                return;
            }

            Vector2 look =
                lookAction != null
                    ? lookAction.ReadValue<Vector2>()
                    : Vector2.zero;

            cameraYaw +=
                look.x *
                mouseSensitivity;

            cameraPitch -=
                look.y *
                mouseSensitivity;

            cameraPitch =
                Mathf.Clamp(
                    cameraPitch,
                    minimumPitch,
                    maximumPitch);

            Vector3 target =
                possessedInstance.transform.position +
                Vector3.up *
                cameraTargetHeight;

            Quaternion orbit =
                Quaternion.Euler(
                    cameraPitch,
                    cameraYaw,
                    0f);

            Vector3 desiredPosition =
                target +
                orbit *
                (Vector3.back *
                 cameraDistance);

            Vector3 cameraDirection =
                desiredPosition -
                target;

            float desiredDistance =
                cameraDirection.magnitude;

            if (desiredDistance > 0.001f &&
                Physics.SphereCast(
                    target,
                    cameraCollisionRadius,
                    cameraDirection.normalized,
                    out RaycastHit cameraHit,
                    desiredDistance,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                desiredPosition =
                    cameraHit.point +
                    cameraHit.normal *
                    cameraCollisionRadius;
            }

            mainCamera.transform.position =
                desiredPosition;

            mainCamera.transform.rotation =
                Quaternion.LookRotation(
                    target -
                    desiredPosition,
                    Vector3.up);

            mainCamera.fieldOfView =
                possessionFieldOfView;
        }

        private void CaptureCameraAuthority()
        {
            cameraBrainStates.Clear();

            savedCameraPosition =
                mainCamera.transform.position;

            savedCameraRotation =
                mainCamera.transform.rotation;

            savedCameraFieldOfView =
                mainCamera.fieldOfView;

            savedCameraParent =
                mainCamera.transform.parent;

            savedCameraSiblingIndex =
                mainCamera.transform.GetSiblingIndex();

            savedCameraLocalPosition =
                mainCamera.transform.localPosition;

            savedCameraLocalRotation =
                mainCamera.transform.localRotation;

            savedCameraLocalScale =
                mainCamera.transform.localScale;

            CaptureCameraRigBehaviours();

            mainCamera.transform.SetParent(
                transform,
                true);

            cameraReparented =
                mainCamera.transform.parent ==
                    transform;

            cameraOverrideActive = true;
        }

        private void CaptureCameraRigBehaviours()
        {
            var unique =
                new HashSet<int>();

            Transform cursor =
                mainCamera != null
                    ? mainCamera.transform
                    : null;

            int depth = 0;

            while (cursor != null &&
                   depth < 6)
            {
                MonoBehaviour[] behaviours =
                    cursor.GetComponents<
                        MonoBehaviour>();

                for (int index = 0;
                     index < behaviours.Length;
                     index++)
                {
                    MonoBehaviour candidate =
                        behaviours[index];

                    if (candidate == null ||
                        candidate == this ||
                        candidate == deployment)
                    {
                        continue;
                    }

                    string typeName =
                        candidate.GetType().Name;

                    string typeNamespace =
                        candidate.GetType().Namespace ??
                        string.Empty;

                    if (!typeNamespace.StartsWith(
                            "Cinemachine",
                            StringComparison.Ordinal) &&
                        !typeNamespace.StartsWith(
                            "Unity.Cinemachine",
                            StringComparison.Ordinal) &&
                        !ContainsAny(
                            typeName,
                            "Camera",
                            "Look",
                            "Orbit",
                            "FreeLook",
                            "Pan",
                            "Zoom",
                            "Input",
                            "Controller",
                            "Movement",
                            "Locomotion"))
                    {
                        continue;
                    }

                    int id =
                        candidate.GetInstanceID();

                    if (!unique.Add(id))
                    {
                        continue;
                    }

                    cameraBrainStates.Add(
                        new BehaviourState
                        {
                            Behaviour = candidate,
                            Enabled = candidate.enabled
                        });

                    CaptureFrozenTransform(
                        candidate.transform);

                    if (candidate.enabled)
                    {
                        candidate.enabled = false;
                        inputConflictCount++;
                    }
                }

                cursor = cursor.parent;
                depth++;
            }

            cameraRigBehaviourCount =
                cameraBrainStates.Count;
        }

        private void EnforceCameraRigAuthority()
        {
            for (int index = 0;
                 index < cameraBrainStates.Count;
                 index++)
            {
                BehaviourState state =
                    cameraBrainStates[index];

                if (state?.Behaviour != null &&
                    state.Behaviour.enabled)
                {
                    state.Behaviour.enabled = false;
                    inputConflictCount++;
                    cameraAuthorityConflictCount++;
                }
            }

            if (mainCamera != null &&
                mainCamera.transform.parent !=
                    transform)
            {
                mainCamera.transform.SetParent(
                    transform,
                    true);

                cameraReparented = true;
                cameraAuthorityConflictCount++;
            }

            cameraOverrideActive =
                mainCamera != null &&
                mainCamera.transform.parent ==
                    transform;
        }

        private void RestoreCameraAuthority()
        {
            if (mainCamera != null)
            {
                mainCamera.transform.SetParent(
                    savedCameraParent,
                    false);

                if (savedCameraParent != null)
                {
                    mainCamera.transform.SetSiblingIndex(
                        Mathf.Clamp(
                            savedCameraSiblingIndex,
                            0,
                            Mathf.Max(
                                0,
                                savedCameraParent.childCount - 1)));
                }

                mainCamera.transform.localPosition =
                    savedCameraLocalPosition;

                mainCamera.transform.localRotation =
                    savedCameraLocalRotation;

                mainCamera.transform.localScale =
                    savedCameraLocalScale;

                mainCamera.fieldOfView =
                    savedCameraFieldOfView;
            }

            RestoreFrozenAuthorityTransforms();

            for (int index = 0;
                 index < cameraBrainStates.Count;
                 index++)
            {
                BehaviourState state =
                    cameraBrainStates[index];

                if (state?.Behaviour != null)
                {
                    state.Behaviour.enabled =
                        state.Enabled;
                }
            }

            cameraBrainStates.Clear();
            cameraRigBehaviourCount = 0;
            cameraReparented = false;
            cameraOverrideActive = false;
        }

        private void CaptureInputAuthority()
        {
            suppressedBehaviours.Clear();
            frozenAuthorityTransforms.Clear();

            m45KnownBlockerCount = 0;
            painterRoleListBlockerCount = 0;

            var unique =
                new HashSet<int>();

            AddSuppressedBehaviour(
                painterBrushController,
                unique);

            CaptureM45KnownInputBlockers(
                unique);

            CapturePainterRoleBehaviourLists(
                unique);

            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    behaviours[index];

                if (!LooksLikePainterInputBlocker(
                        candidate))
                {
                    continue;
                }

                AddSuppressedBehaviour(
                    candidate,
                    unique);
            }

            for (int index = 0;
                 index < suppressedBehaviours.Count;
                 index++)
            {
                BehaviourState state =
                    suppressedBehaviours[index];

                if (state?.Behaviour == null)
                {
                    continue;
                }

                if (LooksLikeMotionOrCameraAuthority(
                        state.Behaviour))
                {
                    CaptureFrozenTransform(
                        state.Behaviour.transform);
                }

                if (state.Behaviour.enabled)
                {
                    state.Behaviour.enabled = false;
                    inputConflictCount++;
                }
            }

            suppressedBehaviourCount =
                suppressedBehaviours.Count;

            frozenAuthorityTransformCount =
                frozenAuthorityTransforms.Count;

            inputAuthorityActive = true;
        }

        private void CaptureM45KnownInputBlockers(
            HashSet<int> unique)
        {
            PrototypeLivingSideCanvasController[] canvases =
                UnityEngine.Object.FindObjectsByType<
                    PrototypeLivingSideCanvasController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public;

            for (int canvasIndex = 0;
                 canvasIndex < canvases.Length;
                 canvasIndex++)
            {
                PrototypeLivingSideCanvasController canvas =
                    canvases[canvasIndex];

                if (canvas == null ||
                    !canvas.gameObject.scene.IsValid())
                {
                    continue;
                }

                Type canvasType =
                    canvas.GetType();

                FieldInfo blockersField =
                    canvasType.GetField(
                        "worldInputBlockers",
                        flags);

                if (blockersField?.GetValue(canvas)
                    is MonoBehaviour[] blockers)
                {
                    for (int blockerIndex = 0;
                         blockerIndex < blockers.Length;
                         blockerIndex++)
                    {
                        MonoBehaviour blocker =
                            blockers[blockerIndex];

                        int previousCount =
                            suppressedBehaviours.Count;

                        AddSuppressedBehaviour(
                            blocker,
                            unique);

                        if (suppressedBehaviours.Count >
                            previousCount)
                        {
                            m45KnownBlockerCount++;
                        }
                    }
                }

                FieldInfo brushField =
                    canvasType.GetField(
                        "painterBrushController",
                        flags);

                if (brushField?.GetValue(canvas)
                    is MonoBehaviour knownBrush)
                {
                    int previousCount =
                        suppressedBehaviours.Count;

                    AddSuppressedBehaviour(
                        knownBrush,
                        unique);

                    if (suppressedBehaviours.Count >
                        previousCount)
                    {
                        m45KnownBlockerCount++;
                    }
                }
            }
        }

        private void CapturePainterRoleBehaviourLists(
            HashSet<int> unique)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public;

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour owner =
                    behaviours[index];

                if (owner == null ||
                    !owner.gameObject.scene.IsValid())
                {
                    continue;
                }

                string ownerTypeName =
                    owner.GetType().Name;

                if (!ContainsAny(
                        ownerTypeName,
                        "RoleSwitcher",
                        "RoleAuthority",
                        "RoleController"))
                {
                    continue;
                }

                FieldInfo[] fields =
                    owner.GetType().GetFields(
                        flags);

                for (int fieldIndex = 0;
                     fieldIndex < fields.Length;
                     fieldIndex++)
                {
                    FieldInfo field =
                        fields[fieldIndex];

                    if (field == null ||
                        field.Name.IndexOf(
                            "painter",
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    object value;

                    try
                    {
                        value =
                            field.GetValue(owner);
                    }
                    catch
                    {
                        continue;
                    }

                    if (value == null ||
                        value is string ||
                        value is not IEnumerable enumerable)
                    {
                        continue;
                    }

                    foreach (object item in enumerable)
                    {
                        if (item is not Behaviour behaviour)
                        {
                            continue;
                        }

                        int previousCount =
                            suppressedBehaviours.Count;

                        AddSuppressedBehaviour(
                            behaviour,
                            unique);

                        if (suppressedBehaviours.Count >
                            previousCount)
                        {
                            painterRoleListBlockerCount++;
                        }
                    }
                }
            }
        }

        private void AddSuppressedBehaviour(
            Behaviour candidate,
            HashSet<int> unique)
        {
            if (candidate == null ||
                candidate == this ||
                unique == null)
            {
                return;
            }

            int instanceId =
                candidate.GetInstanceID();

            if (!unique.Add(
                    instanceId))
            {
                return;
            }

            suppressedBehaviours.Add(
                new BehaviourState
                {
                    Behaviour = candidate,
                    Enabled = candidate.enabled
                });
        }

        private bool LooksLikePainterInputBlocker(
            MonoBehaviour candidate)
        {
            if (candidate == null ||
                candidate == this ||
                candidate == deployment ||
                !candidate.gameObject.scene.IsValid())
            {
                return false;
            }

            string candidateNamespace =
                candidate.GetType().Namespace ??
                string.Empty;

            if (candidateNamespace.StartsWith(
                    "PaintedAlive.Painters.Masterpiece",
                    StringComparison.Ordinal) ||
                candidateNamespace.StartsWith(
                    "PaintedAlive.UI",
                    StringComparison.Ordinal))
            {
                return false;
            }

            string typeName =
                candidate.GetType().Name;

            string objectName =
                candidate.gameObject.name;

            string hierarchyPath =
                BuildHierarchyPath(
                    candidate.transform);

            bool painterHierarchy =
                ContainsAny(
                    typeName,
                    "Painter",
                    "InkPainter") ||
                ContainsAny(
                    objectName,
                    "Painter",
                    "InkPainter",
                    "M21") ||
                ContainsAny(
                    hierarchyPath,
                    "Painter",
                    "InkPainter",
                    "M21");

            bool inputOrMotion =
                ContainsAny(
                    typeName,
                    "Brush",
                    "Camera",
                    "Look",
                    "Input",
                    "Movement",
                    "Locomotion",
                    "Traversal",
                    "Rail",
                    "Orbit",
                    "FreeLook",
                    "Fly",
                    "Pan",
                    "Zoom",
                    "Controller") ||
                ContainsAny(
                    objectName,
                    "Brush",
                    "Camera",
                    "Look",
                    "Input",
                    "Movement",
                    "Locomotion",
                    "Traversal",
                    "Rail",
                    "Orbit",
                    "FreeLook",
                    "Fly",
                    "Pan",
                    "Zoom");

            bool cameraHierarchy =
                mainCamera != null &&
                (
                    candidate.transform ==
                        mainCamera.transform ||
                    mainCamera.transform.IsChildOf(
                        candidate.transform) ||
                    candidate.transform.IsChildOf(
                        mainCamera.transform)
                );

            return candidate ==
                    painterBrushController ||
                (
                    painterHierarchy &&
                    inputOrMotion
                ) ||
                (
                    cameraHierarchy &&
                    inputOrMotion
                );
        }

        private bool LooksLikeMotionOrCameraAuthority(
            Behaviour candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            string typeName =
                candidate.GetType().Name;

            string objectName =
                candidate.gameObject.name;

            return ContainsAny(
                    typeName,
                    "Camera",
                    "Look",
                    "Input",
                    "Movement",
                    "Locomotion",
                    "Traversal",
                    "Rail",
                    "Orbit",
                    "FreeLook",
                    "Fly",
                    "Pan",
                    "Zoom",
                    "Controller") ||
                ContainsAny(
                    objectName,
                    "Camera",
                    "Movement",
                    "Locomotion",
                    "Rail",
                    "Orbit",
                    "FreeLook");
        }

        private void CaptureFrozenTransform(
            Transform target)
        {
            if (target == null ||
                target == mainCamera.transform ||
                target == transform)
            {
                return;
            }

            for (int index = 0;
                 index < frozenAuthorityTransforms.Count;
                 index++)
            {
                TransformState existing =
                    frozenAuthorityTransforms[index];

                if (existing?.Transform ==
                    target)
                {
                    return;
                }
            }

            frozenAuthorityTransforms.Add(
                new TransformState
                {
                    Transform = target,
                    Position = target.position,
                    Rotation = target.rotation
                });
        }

        private void EnforceFrozenAuthorityTransforms()
        {
            for (int index = 0;
                 index < frozenAuthorityTransforms.Count;
                 index++)
            {
                TransformState state =
                    frozenAuthorityTransforms[index];

                if (state?.Transform == null)
                {
                    continue;
                }

                if (
                    Vector3.SqrMagnitude(
                        state.Transform.position -
                        state.Position) >
                    0.000001f ||
                    Quaternion.Angle(
                        state.Transform.rotation,
                        state.Rotation) >
                    0.001f
                )
                {
                    state.Transform.SetPositionAndRotation(
                        state.Position,
                        state.Rotation);

                    inputConflictCount++;
                    cameraAuthorityConflictCount++;
                }
            }
        }

        private void RestoreFrozenAuthorityTransforms()
        {
            for (int index = 0;
                 index < frozenAuthorityTransforms.Count;
                 index++)
            {
                TransformState state =
                    frozenAuthorityTransforms[index];

                if (state?.Transform != null)
                {
                    state.Transform.SetPositionAndRotation(
                        state.Position,
                        state.Rotation);
                }
            }

            frozenAuthorityTransforms.Clear();
            frozenAuthorityTransformCount = 0;
        }

        private static string BuildHierarchyPath(
            Transform target)
        {
            if (target == null)
            {
                return "NULL";
            }

            string path =
                target.name;

            while (target.parent != null)
            {
                target =
                    target.parent;

                path =
                    target.name +
                    "/" +
                    path;
            }

            return path;
        }

        private void EnforceInputAuthority()
        {
            for (int index = 0;
                 index < suppressedBehaviours.Count;
                 index++)
            {
                BehaviourState state =
                    suppressedBehaviours[index];

                if (state?.Behaviour != null &&
                    state.Behaviour.enabled)
                {
                    state.Behaviour.enabled = false;
                    inputConflictCount++;
                }
            }

            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;

            suppressedBehaviourCount =
                suppressedBehaviours.Count;

            inputAuthorityActive = true;
        }

        private void RestoreInputAuthority()
        {
            for (int index = 0;
                 index < suppressedBehaviours.Count;
                 index++)
            {
                BehaviourState state =
                    suppressedBehaviours[index];

                if (state?.Behaviour != null)
                {
                    state.Behaviour.enabled =
                        state.Enabled;
                }
            }

            suppressedBehaviours.Clear();
            suppressedBehaviourCount = 0;
            m45KnownBlockerCount = 0;
            painterRoleListBlockerCount = 0;
            inputAuthorityActive = false;
        }

        private void CaptureCursorAuthority()
        {
            savedCursorLockMode =
                Cursor.lockState;

            savedCursorVisible =
                Cursor.visible;
        }

        private void RestoreCursorAuthority()
        {
            Cursor.lockState =
                savedCursorLockMode;

            Cursor.visible =
                savedCursorVisible;
        }

        private void CapturePartPoses()
        {
            partPoses.Clear();

            Array values =
                Enum.GetValues(
                    typeof(
                        PrototypeMasterpiecePartKind));

            foreach (object value in values)
            {
                var kind =
                    (PrototypeMasterpiecePartKind)
                    value;

                if (!possessedInstance.TryGetPartRoot(
                        kind,
                        out Transform partRoot))
                {
                    continue;
                }

                partPoses[kind] =
                    new PartPose
                    {
                        Transform = partRoot,
                        LocalPosition =
                            partRoot.localPosition,
                        LocalRotation =
                            partRoot.localRotation
                    };
            }
        }

        private void RestorePartPoses()
        {
            foreach (KeyValuePair<
                         PrototypeMasterpiecePartKind,
                         PartPose> pair in partPoses)
            {
                PartPose pose =
                    pair.Value;

                if (pose?.Transform == null)
                {
                    continue;
                }

                pose.Transform.localPosition =
                    pose.LocalPosition;

                pose.Transform.localRotation =
                    pose.LocalRotation;
            }

            partPoses.Clear();
        }

        private void UpdatePartAnimation(
            float deltaTime)
        {
            if (possessedInstance == null)
            {
                return;
            }

            Vector2 moveInput =
                moveAction != null
                    ? Vector2.ClampMagnitude(
                        moveAction.ReadValue<Vector2>(),
                        1f)
                    : Vector2.zero;

            float movementAmount =
                Mathf.Clamp01(
                    moveInput.magnitude);

            locomotionPhase +=
                deltaTime *
                Mathf.Lerp(
                    2.2f,
                    8.5f,
                    movementAmount);

            float swing =
                Mathf.Sin(
                    locomotionPhase) *
                18f *
                movementAmount;

            ApplyPartRotation(
                PrototypeMasterpiecePartKind.LeftContact,
                swing);

            ApplyPartRotation(
                PrototypeMasterpiecePartKind.RightContact,
                -swing);

            float bob =
                Mathf.Abs(
                    Mathf.Sin(
                        locomotionPhase)) *
                0.045f *
                movementAmount;

            ApplyPartPositionOffset(
                PrototypeMasterpiecePartKind.Core,
                Vector3.up *
                bob);

            ApplyPartPositionOffset(
                PrototypeMasterpiecePartKind.Head,
                Vector3.up *
                bob *
                1.25f);

            if (!attackPreviewActive)
            {
                ApplyPartRotation(
                    PrototypeMasterpiecePartKind.LeftAttack,
                    0f);

                ApplyPartRotation(
                    PrototypeMasterpiecePartKind.RightAttack,
                    0f);
            }
        }

        private void ApplyPartRotation(
            PrototypeMasterpiecePartKind kind,
            float zDegrees)
        {
            if (!partPoses.TryGetValue(
                    kind,
                    out PartPose pose) ||
                pose?.Transform == null)
            {
                return;
            }

            pose.Transform.localRotation =
                pose.LocalRotation *
                Quaternion.AngleAxis(
                    zDegrees,
                    Vector3.forward);
        }

        private void ApplyPartPositionOffset(
            PrototypeMasterpiecePartKind kind,
            Vector3 offset)
        {
            if (!partPoses.TryGetValue(
                    kind,
                    out PartPose pose) ||
                pose?.Transform == null)
            {
                return;
            }

            pose.Transform.localPosition =
                pose.LocalPosition +
                offset;
        }

        private bool TrySelectAttackPart(
            out PrototypeMasterpiecePartKind selectedKind)
        {
            bool leftActive =
                possessedInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.LeftAttack);

            bool rightActive =
                possessedInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.RightAttack);

            if (!leftActive &&
                !rightActive)
            {
                selectedKind =
                    PrototypeMasterpiecePartKind.LeftAttack;

                return false;
            }

            if (leftActive &&
                rightActive)
            {
                selectedKind =
                    nextAttackUsesLeft
                        ? PrototypeMasterpiecePartKind.LeftAttack
                        : PrototypeMasterpiecePartKind.RightAttack;

                nextAttackUsesLeft =
                    !nextAttackUsesLeft;

                return true;
            }

            selectedKind =
                leftActive
                    ? PrototypeMasterpiecePartKind.LeftAttack
                    : PrototypeMasterpiecePartKind.RightAttack;

            return true;
        }

        private IEnumerator AttackPreviewRoutine(
            PrototypeMasterpiecePartKind partKind)
        {
            if (!partPoses.TryGetValue(
                    partKind,
                    out PartPose pose) ||
                pose?.Transform == null)
            {
                blockedAttackCount++;
                attackRoutine = null;
                yield break;
            }

            attackPreviewActive = true;
            attackPreviewCount++;
            lastAttackAt = Time.unscaledTime;

            float sideSign =
                partKind ==
                    PrototypeMasterpiecePartKind.LeftAttack
                    ? -1f
                    : 1f;

            lastAction =
                $"{partKind} saldırı telegraph ön izlemesi.";

            SetAttackTelegraphVisible(
                true);

            float timer = 0f;

            while (timer <
                   attackTelegraphSeconds)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        timer /
                        attackTelegraphSeconds);

                UpdateAttackTelegraph(
                    sideSign,
                    progress);

                pose.Transform.localRotation =
                    pose.LocalRotation *
                    Quaternion.AngleAxis(
                        sideSign *
                        Mathf.Lerp(
                            0f,
                            -18f,
                            progress),
                        Vector3.forward);

                yield return null;
            }

            SetAttackTelegraphVisible(
                false);

            timer = 0f;

            while (timer <
                   attackSwingSeconds)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        timer /
                        attackSwingSeconds);

                float eased =
                    1f -
                    Mathf.Pow(
                        1f -
                        progress,
                        3f);

                pose.Transform.localRotation =
                    pose.LocalRotation *
                    Quaternion.AngleAxis(
                        sideSign *
                        Mathf.Lerp(
                            -18f,
                            78f,
                            eased),
                        Vector3.forward);

                yield return null;
            }

            timer = 0f;

            while (timer <
                   attackRecoverySeconds)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        timer /
                        attackRecoverySeconds);

                pose.Transform.localRotation =
                    pose.LocalRotation *
                    Quaternion.AngleAxis(
                        sideSign *
                        Mathf.Lerp(
                            78f,
                            0f,
                            progress),
                        Vector3.forward);

                yield return null;
            }

            pose.Transform.localRotation =
                pose.LocalRotation;

            attackPreviewActive = false;
            attackRoutine = null;

            Debug.Log(
                "[M48 Attack Preview]\n" +
                $"Part={partKind}\n" +
                "HitAuthority=False\n" +
                "DamageAuthority=False\n" +
                $"AttackPreviewCount={attackPreviewCount}",
                possessedInstance);
        }

        private void EnsureAttackTelegraph()
        {
            if (attackTelegraph != null)
            {
                return;
            }

            GameObject telegraphObject =
                new GameObject(
                    "M48_AttackTelegraph");

            telegraphObject.transform.SetParent(
                transform,
                false);

            attackTelegraph =
                telegraphObject.AddComponent<
                    LineRenderer>();

            attackTelegraph.useWorldSpace = true;
            attackTelegraph.loop = false;
            attackTelegraph.positionCount = 22;
            attackTelegraph.startWidth = 0.085f;
            attackTelegraph.endWidth = 0.025f;
            attackTelegraph.numCapVertices = 3;
            attackTelegraph.numCornerVertices = 3;
            attackTelegraph.shadowCastingMode =
                UnityEngine.Rendering
                    .ShadowCastingMode.Off;

            attackTelegraph.receiveShadows = false;
            attackTelegraph.enabled = false;

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default");
            }

            if (shader != null)
            {
                attackTelegraphMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M48_AttackTelegraph_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };

                Color orange =
                    new Color(
                        1f,
                        0.28f,
                        0.04f,
                        0.92f);

                if (attackTelegraphMaterial.HasProperty(
                        "_BaseColor"))
                {
                    attackTelegraphMaterial.SetColor(
                        "_BaseColor",
                        orange);
                }

                if (attackTelegraphMaterial.HasProperty(
                        "_Color"))
                {
                    attackTelegraphMaterial.SetColor(
                        "_Color",
                        orange);
                }

                attackTelegraph.sharedMaterial =
                    attackTelegraphMaterial;

                attackTelegraph.startColor =
                    orange;

                attackTelegraph.endColor =
                    new Color(
                        1f,
                        0.72f,
                        0.12f,
                        0.25f);
            }
        }

        private void UpdateAttackTelegraph(
            float sideSign,
            float progress)
        {
            if (attackTelegraph == null ||
                possessedInstance == null)
            {
                return;
            }

            Vector3 center =
                possessedInstance.transform.position +
                Vector3.up *
                1.08f +
                possessedInstance.transform.forward *
                0.42f;

            float startAngle =
                sideSign < 0f
                    ? -108f
                    : 108f;

            float endAngle =
                sideSign < 0f
                    ? 18f
                    : -18f;

            int pointCount =
                attackTelegraph.positionCount;

            for (int index = 0;
                 index < pointCount;
                 index++)
            {
                float t =
                    pointCount <= 1
                        ? 0f
                        : index /
                          (float)
                          (pointCount - 1);

                float angle =
                    Mathf.Lerp(
                        startAngle,
                        endAngle,
                        t);

                Quaternion rotation =
                    Quaternion.AngleAxis(
                        angle,
                        Vector3.up);

                Vector3 direction =
                    rotation *
                    possessedInstance
                        .transform.forward;

                float radius =
                    attackTelegraphRadius *
                    Mathf.Lerp(
                        0.82f,
                        1f,
                        progress);

                attackTelegraph.SetPosition(
                    index,
                    center +
                    direction *
                    radius);
            }
        }

        private void SetAttackTelegraphVisible(
            bool visible)
        {
            if (attackTelegraph != null)
            {
                attackTelegraph.enabled =
                    visible;
            }
        }

        private void UpdatePerformanceTelemetry()
        {
            float frameMilliseconds =
                Time.unscaledDeltaTime *
                1000f;

            if (averageFrameMilliseconds <=
                0f)
            {
                averageFrameMilliseconds =
                    frameMilliseconds;
            }
            else
            {
                averageFrameMilliseconds =
                    Mathf.Lerp(
                        averageFrameMilliseconds,
                        frameMilliseconds,
                        0.08f);
            }

            maximumFrameMilliseconds =
                Mathf.Max(
                    maximumFrameMilliseconds,
                    frameMilliseconds);
        }

        private void EndPossessionInternal(
            bool countRelease,
            string reason)
        {
            if (!possessed &&
                possessedInstance == null)
            {
                return;
            }

            if (attackRoutine != null)
            {
                StopCoroutine(
                    attackRoutine);

                attackRoutine = null;
            }

            attackPreviewActive = false;
            SetAttackTelegraphVisible(
                false);

            RestorePartPoses();
            RestoreInputAuthority();
            RestoreCameraAuthority();
            RestoreCursorAuthority();

            if (possessedInstance != null)
            {
                possessedInstance.SetProxyDebugVisible(
                    savedProxyDebugVisible);
            }

            PrototypeMasterpieceWorldInstance released =
                possessedInstance;

            possessed = false;
            possessedInstance = null;
            currentLeashDistance = 0f;

            if (countRelease)
            {
                releaseCount++;
            }

            lastAction =
                reason;

            Debug.Log(
                "[M48 Possession End]\n" +
                $"Reason={reason}\n" +
                $"TravelledDistance={travelledDistance:F2}\n" +
                $"AttackPreviewCount={attackPreviewCount}\n" +
                $"AverageFrameMs={averageFrameMilliseconds:F2}\n" +
                $"MaximumFrameMs={maximumFrameMilliseconds:F2}",
                released != null
                    ? released
                    : this);
        }

        private void RejectPossession(
            string reason)
        {
            rejectedPossessionCount++;
            lastAction =
                $"Sahiplenme reddedildi: {reason}";

            Debug.LogWarning(
                "[M48 Possession Rejected]\n" +
                $"Reason={reason}\n" +
                $"RejectedCount={rejectedPossessionCount}",
                this);
        }

        private void RefreshHud()
        {
            bool worldInstanceAvailable =
                deployment != null &&
                deployment.ActiveInstance != null;

            if (statusGroup != null)
            {
                statusGroup.alpha =
                    painterRoleActive &&
                    (
                        worldInstanceAvailable ||
                        possessed
                    )
                        ? 1f
                        : 0f;

                statusGroup.interactable = false;
                statusGroup.blocksRaycasts = false;
            }

            if (!painterRoleActive)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    possessed
                        ? "BAŞ YAPIT • SAHİPLENİLDİ"
                        : "BAŞ YAPIT • DOĞRUDAN KONTROL";

                titleText.color =
                    possessed
                        ? new Color(
                            1f,
                            0.54f,
                            0.06f,
                            1f)
                        : new Color(
                            0.08f,
                            0.70f,
                            0.75f,
                            1f);
            }

            if (stateText != null)
            {
                if (possessed)
                {
                    stateText.text =
                        $"MESAFE {currentLeashDistance:F1}/{leashRadius:F0} m   " +
                        $"TOPLAM {travelledDistance:F1} m   " +
                        $"SALDIRI {attackPreviewCount}\n" +
                        $"FRAME ORT {averageFrameMilliseconds:F1} ms   " +
                        $"MAKS {maximumFrameMilliseconds:F1} ms   " +
                        $"ENGEL {blockedMovementCount}\n" +
                        $"KAMERA {(cameraOverrideActive ? "M48" : "PAINTER")}   " +
                        $"BLOCKER {suppressedBehaviourCount}   " +
                        $"LATE {cameraLateWriteCount}\n" +
                        lastAction;
                }
                else if (worldInstanceAvailable)
                {
                    stateText.text =
                        "P ile Baş Yapıt kamerasına geç.\n" +
                        "AI, hit ve hasar otoritesi bu spike'ta kapalı.\n" +
                        lastAction;
                }
                else
                {
                    stateText.text =
                        "Önce M47 ile Baş Yapıtı dünyaya deploy et.";
                }
            }

            if (controlsText != null)
            {
                controlsText.text =
                    possessed
                        ? "WASD • HAREKET   SHIFT • KOŞ   " +
                          "MOUSE • KAMERA   SOL TIK • SALDIRI ÖN İZLEME   " +
                          "P / ESC • BIRAK"
                        : "P • SAHİPLEN   " +
                          "M47: V DEPLOY • X GERİ ÇAĞIR • O PROXY";
            }
        }

        private static bool ContainsAny(
            string value,
            params string[] fragments)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }

            for (int index = 0;
                 index < fragments.Length;
                 index++)
            {
                if (value.Contains(
                        fragments[index],
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            if (togglePossessionAction != null)
            {
                togglePossessionAction.performed -=
                    HandleTogglePossession;

                togglePossessionAction.Dispose();
                togglePossessionAction = null;
            }

            if (releasePossessionAction != null)
            {
                releasePossessionAction.performed -=
                    HandleReleasePossession;

                releasePossessionAction.Dispose();
                releasePossessionAction = null;
            }

            moveAction?.Dispose();
            moveAction = null;

            lookAction?.Dispose();
            lookAction = null;

            sprintAction?.Dispose();
            sprintAction = null;

            if (attackAction != null)
            {
                attackAction.performed -=
                    HandleAttackPreview;

                attackAction.Dispose();
                attackAction = null;
            }

            if (attackTelegraphMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        attackTelegraphMaterial);
                }
                else
                {
                    DestroyImmediate(
                        attackTelegraphMaterial);
                }
            }
        }
    }
}
