using System;
using System.Collections;
using System.Collections.Generic;
using PaintedAlive.Core.RoleAuthority;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(24500)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceAutonomousEncounterController :
        MonoBehaviour
    {
        private sealed class AttackPose
        {
            public Transform Transform;
            public Quaternion LocalRotation;
        }

        [Header("Sources")]
        [SerializeField]
        private PrototypeMasterpieceWorldDeploymentController deployment;

        [SerializeField]
        private PrototypeMasterpiecePossessionController possession;

        [SerializeField] private Transform figureTarget;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Autonomy")]
        [SerializeField] private bool autonomyEnabled = true;

        [SerializeField, Min(0.1f)]
        private float deploymentArmingSeconds = 1.2f;

        [SerializeField, Min(0.1f)]
        private float possessionReleaseRearmSeconds = 0.45f;

        [SerializeField, Min(0.5f)]
        private float patrolRadius = 2.5f;

        [SerializeField, Min(0.1f)]
        private float patrolArrivalDistance = 0.35f;

        [SerializeField, Min(0.5f)]
        private float leashRadius = 8.5f;

        [Header("Detection")]
        [SerializeField, Min(0.5f)]
        private float perceptionDetectionRadius = 8f;

        [SerializeField, Min(0.5f)]
        private float blindDetectionRadius = 2.4f;

        [SerializeField, Min(0.5f)]
        private float pursuitForgetRadius = 10f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)]
        private float twoContactMoveSpeed = 2.15f;

        [SerializeField, Min(0.1f)]
        private float oneContactMoveSpeed = 0.92f;

        [SerializeField, Min(1f)]
        private float turnSpeedDegrees = 360f;

        [SerializeField, Min(0.05f)]
        private float movementProbeRadius = 0.42f;

        [SerializeField, Min(0.1f)]
        private float movementProbeHeight = 0.78f;

        [SerializeField, Min(0.5f)]
        private float groundProbeHeight = 3f;

        [SerializeField, Min(0.5f)]
        private float groundProbeDistance = 8f;

        [SerializeField]
        private LayerMask movementCollisionMask = ~0;

        [Header("Attack Preview")]
        [SerializeField, Min(0.5f)]
        private float attackRange = 2.15f;

        [SerializeField, Min(0.1f)]
        private float attackCooldownSeconds = 2.6f;

        [SerializeField, Min(0.1f)]
        private float telegraphSeconds = 0.92f;

        [SerializeField, Min(0.05f)]
        private float swingSeconds = 0.20f;

        [SerializeField, Min(0.05f)]
        private float recoverySeconds = 0.48f;

        [SerializeField, Min(0.2f)]
        private float telegraphRadius = 1.15f;

        [SerializeField, Min(8)]
        private int telegraphSegments = 32;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeMasterpieceWorldInstance activeInstance;

        [SerializeField]
        private PrototypeMasterpieceAutonomyState state =
            PrototypeMasterpieceAutonomyState.WaitingForWorld;

        [SerializeField] private bool inputActionsActive;
        [SerializeField] private bool painterControlRoleActive;
        [SerializeField] private int roleRejectedActionCount;
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private bool targetAvailable;
        [SerializeField] private bool targetDetected;
        [SerializeField] private bool leftMovementActive;
        [SerializeField] private bool rightMovementActive;
        [SerializeField] private bool leftAttackActive;
        [SerializeField] private bool rightAttackActive;
        [SerializeField] private bool perceptionActive;
        [SerializeField] private bool coreIntegrityActive;
        [SerializeField] private int activeMovementCapabilityCount;
        [SerializeField] private int activeAttackCapabilityCount;
        [SerializeField] private int instanceBindCount;
        [SerializeField] private int patrolTargetCount;
        [SerializeField] private int pursuitEnterCount;
        [SerializeField] private int attackDecisionCount;
        [SerializeField] private int attackTelegraphCount;
        [SerializeField] private int attackSwingCount;
        [SerializeField] private int attackRecoveryCount;
        [SerializeField] private int attackAbortCount;
        [SerializeField] private int possessionSuspendCount;
        [SerializeField] private int possessionResumeCount;
        [SerializeField] private int blockedMovementCount;
        [SerializeField] private int groundRejectCount;
        [SerializeField] private int leashClampCount;
        [SerializeField] private int capabilityTestChangeCount;
        [SerializeField] private int autonomyToggleCount;
        [SerializeField] private float travelledDistance;
        [SerializeField] private float targetDistance;
        [SerializeField] private float currentLeashDistance;
        [SerializeField] private string lastAction =
            "Dünya Baş Yapıtı bekleniyor.";

        private readonly Dictionary<
            PrototypeMasterpiecePartKind,
            AttackPose> attackPoses =
            new Dictionary<
                PrototypeMasterpiecePartKind,
                AttackPose>();

        private InputAction toggleAutonomyAction;
        private Rigidbody body;
        private Vector3 autonomyOrigin;
        private Vector3 patrolTarget;
        private int patrolIndex;
        private float stateEnteredAt;
        private float nextAttackAllowedAt;
        private bool nextAttackUsesLeft = true;
        private PrototypeMasterpiecePartKind currentAttackPart =
            PrototypeMasterpiecePartKind.LeftAttack;
        private LineRenderer telegraphRing;
        private LineRenderer telegraphGuide;
        private Material telegraphMaterial;
        private Vector3 lockedAttackTarget;
        private float stateProgress;

        public bool AutonomyEnabled => autonomyEnabled;
        public PrototypeMasterpieceAutonomyState State => state;
        public bool InputActionsActive => inputActionsActive;
        public bool PainterControlRoleActive => painterControlRoleActive;
        public int RoleRejectedActionCount => roleRejectedActionCount;
        public string ResolvedRole => resolvedRole;
        public bool TargetAvailable => targetAvailable;
        public bool TargetDetected => targetDetected;
        public int ActiveMovementCapabilityCount =>
            activeMovementCapabilityCount;
        public int ActiveAttackCapabilityCount =>
            activeAttackCapabilityCount;
        public bool PerceptionActive => perceptionActive;
        public bool CoreIntegrityActive => coreIntegrityActive;
        public int InstanceBindCount => instanceBindCount;
        public int PatrolTargetCount => patrolTargetCount;
        public int PursuitEnterCount => pursuitEnterCount;
        public int AttackDecisionCount => attackDecisionCount;
        public int AttackTelegraphCount => attackTelegraphCount;
        public int AttackSwingCount => attackSwingCount;
        public int AttackRecoveryCount => attackRecoveryCount;
        public int AttackAbortCount => attackAbortCount;
        public int PossessionSuspendCount => possessionSuspendCount;
        public int PossessionResumeCount => possessionResumeCount;
        public int BlockedMovementCount => blockedMovementCount;
        public int GroundRejectCount => groundRejectCount;
        public int LeashClampCount => leashClampCount;
        public int CapabilityTestChangeCount =>
            capabilityTestChangeCount;
        public int AutonomyToggleCount => autonomyToggleCount;
        public float TravelledDistance => travelledDistance;
        public float TargetDistance => targetDistance;
        public float CurrentLeashDistance => currentLeashDistance;
        public float StateProgress => stateProgress;
        public string LastAction => lastAction;
        public PrototypeMasterpieceWorldInstance ActiveInstance =>
            activeInstance;
        public Transform FigureTarget => figureTarget;
        public PrototypeMasterpiecePartKind CurrentAttackPart =>
            currentAttackPart;
        public Vector3 LockedAttackTarget =>
            lockedAttackTarget;
        public float AttackRange => attackRange;
        public float TelegraphSeconds => telegraphSeconds;
        public float SwingSeconds => swingSeconds;
        public float RecoverySeconds => recoverySeconds;

        public bool HitAuthorityEnabled => false;
        public bool DamageAuthorityEnabled => false;
        public bool ClarityWritesEnabled => false;
        public bool BossAIPrototypeEnabled => true;
        public bool NetworkIntegrationParked => true;
        public bool DirectControlTakesPriority => true;
        public bool TriggerCollidersPreserved => true;

        public void Configure(
            PrototypeMasterpieceWorldDeploymentController
                configuredDeployment,
            PrototypeMasterpiecePossessionController
                configuredPossession,
            Transform configuredFigureTarget,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            deployment = configuredDeployment;
            possession = configuredPossession;
            figureTarget = configuredFigureTarget;
            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolveInstance();
            RefreshHud();
        }

        private void Awake()
        {
            EnsureInputActions();
            EnsureTelegraph();
            ResolveInstance();
            RefreshHud();
        }

        private void OnEnable()
        {
            EnsureInputActions();
            RefreshRoleAuthority(forceInputRefresh: true);
            EnsureTelegraph();
            ResolveInstance();
            RefreshHud();
        }

        private void Update()
        {
            RefreshRoleAuthority(forceInputRefresh: false);
            ResolveInstance();
            RefreshCapabilities();
            RefreshTargetState();
            TickStateMachine();
            UpdateTelegraph();
            RefreshHud();
        }

        private void FixedUpdate()
        {
            if (
                activeInstance == null ||
                body == null ||
                possession != null &&
                possession.Possessed ||
                !autonomyEnabled
            )
            {
                return;
            }

            if (
                state ==
                    PrototypeMasterpieceAutonomyState.Patrol ||
                state ==
                    PrototypeMasterpieceAutonomyState.Pursuit
            )
            {
                SimulateAutonomousMovement(
                    Time.fixedDeltaTime);
            }
        }

        public void ToggleAutonomy()
        {
            if (!RequirePainterControl("Otonomi aç/kapat"))
            {
                return;
            }

            autonomyEnabled =
                !autonomyEnabled;

            autonomyToggleCount++;

            if (!autonomyEnabled)
            {
                AbortAttack(
                    "Otonomi kullanıcı tarafından kapatıldı.");

                EnterState(
                    PrototypeMasterpieceAutonomyState.DisabledByUser,
                    "I ile otonomi kapatıldı.");
            }
            else if (activeInstance != null)
            {
                EnterState(
                    PrototypeMasterpieceAutonomyState.Arming,
                    "I ile otonomi açıldı; kısa güvenlik arması.");
            }

            RefreshHud();
        }

        public void ForceAttackDecision()
        {
            if (
                activeInstance == null ||
                !autonomyEnabled ||
                possession != null &&
                possession.Possessed
            )
            {
                lastAction =
                    "Zorunlu saldırı için aktif ve serbest Baş Yapıt gerekli.";

                return;
            }

            RefreshCapabilities();
            RefreshTargetState();

            if (!targetAvailable)
            {
                lastAction =
                    "Zorunlu saldırı için aktif Figür hedefi yok.";

                return;
            }

            if (!TrySelectAttackPart(
                    out PrototypeMasterpiecePartKind selected))
            {
                lastAction =
                    "Aktif saldırı capability'si yok.";

                return;
            }

            BeginAttack(
                selected,
                figureTarget.position,
                forced: true);
        }

        public bool SetCapabilityForTest(
            PrototypeMasterpiecePartKind kind,
            bool active)
        {
            if (
                activeInstance == null ||
                !activeInstance.TryGetWorldPart(
                    kind,
                    out PrototypeMasterpieceWorldPart part) ||
                part == null
            )
            {
                lastAction =
                    $"{kind} world part bulunamadı.";

                return false;
            }

            bool changed =
                part.SetCapabilityActive(
                    active,
                    "M49.0 capability test");

            if (changed)
            {
                capabilityTestChangeCount++;

                lastAction =
                    $"{kind} capability {(active ? "açıldı" : "kapatıldı")}.";

                RefreshCapabilities();

                if (
                    IsAttackState(state) &&
                    kind ==
                        currentAttackPart &&
                    !part.CapabilityActive
                )
                {
                    AbortAttack(
                        "Aktif saldırı organı capability testiyle kapatıldı.");
                }
            }

            return changed;
        }

        public int RestoreAllCapabilitiesForTest()
        {
            if (activeInstance == null)
            {
                return 0;
            }

            PrototypeMasterpieceWorldPart[] parts =
                activeInstance.GetComponentsInChildren<
                    PrototypeMasterpieceWorldPart>(
                        true);

            int changed = 0;

            for (int index = 0;
                 index < parts.Length;
                 index++)
            {
                PrototypeMasterpieceWorldPart part =
                    parts[index];

                if (
                    part != null &&
                    part.SetCapabilityActive(
                        true,
                        "M49.0 restore all capabilities")
                )
                {
                    changed++;
                }
            }

            capabilityTestChangeCount +=
                changed;

            lastAction =
                $"{changed} capability yeniden açıldı.";

            RefreshCapabilities();

            if (
                autonomyEnabled &&
                activeInstance != null &&
                state !=
                    PrototypeMasterpieceAutonomyState.PossessedSuspended
            )
            {
                EnterState(
                    PrototypeMasterpieceAutonomyState.Arming,
                    "Capability restore sonrası AI yeniden arming.");
            }

            return changed;
        }

        private void RefreshRoleAuthority(bool forceInputRefresh)
        {
            bool previous = painterControlRoleActive;
            painterControlRoleActive =
                PrototypeRoleAuthorityResolver.IsPainter(
                    out resolvedRole);

            if (!forceInputRefresh && previous == painterControlRoleActive)
            {
                return;
            }

            if (painterControlRoleActive)
            {
                EnableInputActions();
            }
            else
            {
                DisableInputActions();
            }
        }

        private bool RequirePainterControl(string action)
        {
            RefreshRoleAuthority(forceInputRefresh: false);

            if (painterControlRoleActive)
            {
                return true;
            }

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
            return false;
        }

        private void EnsureInputActions()
        {
            if (toggleAutonomyAction != null)
            {
                return;
            }

            toggleAutonomyAction =
                new InputAction(
                    "M49 Toggle Autonomy",
                    InputActionType.Button,
                    "<Keyboard>/i");

            toggleAutonomyAction.performed +=
                HandleToggleAutonomy;
        }

        private void EnableInputActions()
        {
            toggleAutonomyAction?.Enable();

            inputActionsActive =
                toggleAutonomyAction != null &&
                toggleAutonomyAction.enabled;
        }

        private void DisableInputActions()
        {
            toggleAutonomyAction?.Disable();
            inputActionsActive = false;
        }

        private void HandleToggleAutonomy(
            InputAction.CallbackContext context)
        {
            ToggleAutonomy();
        }

        private void ResolveInstance()
        {
            PrototypeMasterpieceWorldInstance candidate =
                deployment != null
                    ? deployment.ActiveInstance
                    : null;

            if (candidate == activeInstance)
            {
                return;
            }

            ReleaseInstance();
            activeInstance = candidate;

            if (activeInstance == null)
            {
                EnterState(
                    PrototypeMasterpieceAutonomyState.WaitingForWorld,
                    "Dünya Baş Yapıtı bekleniyor.");

                return;
            }

            body =
                activeInstance.KinematicRigidbody;

            autonomyOrigin =
                activeInstance.transform.position;

            travelledDistance = 0f;
            currentLeashDistance = 0f;
            targetDistance = -1f;
            nextAttackAllowedAt =
                Time.unscaledTime +
                deploymentArmingSeconds;

            CaptureAttackPoses();
            SelectNextPatrolTarget();

            instanceBindCount++;

            EnterState(
                autonomyEnabled
                    ? PrototypeMasterpieceAutonomyState.Arming
                    : PrototypeMasterpieceAutonomyState.DisabledByUser,
                autonomyEnabled
                    ? "Dünya Baş Yapıtı AI arming süresinde."
                    : "Otonomi kapalı.");
        }

        private void ReleaseInstance()
        {
            AbortAttack(
                "Dünya instance değişti veya recall edildi.");

            RestoreAttackPoses();

            activeInstance = null;
            body = null;
            targetAvailable = false;
            targetDetected = false;
            targetDistance = -1f;
            attackPoses.Clear();
        }

        private void RefreshCapabilities()
        {
            if (activeInstance == null)
            {
                leftMovementActive = false;
                rightMovementActive = false;
                leftAttackActive = false;
                rightAttackActive = false;
                perceptionActive = false;
                coreIntegrityActive = false;
                activeMovementCapabilityCount = 0;
                activeAttackCapabilityCount = 0;
                return;
            }

            leftMovementActive =
                activeInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.LeftMovement);

            rightMovementActive =
                activeInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.RightMovement);

            leftAttackActive =
                activeInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.LeftAttack);

            rightAttackActive =
                activeInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.RightAttack);

            perceptionActive =
                activeInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.Perception);

            coreIntegrityActive =
                activeInstance.HasActiveCapability(
                    PrototypeMasterpieceCapability.CoreIntegrity);

            activeMovementCapabilityCount =
                (leftMovementActive ? 1 : 0) +
                (rightMovementActive ? 1 : 0);

            activeAttackCapabilityCount =
                (leftAttackActive ? 1 : 0) +
                (rightAttackActive ? 1 : 0);
        }

        private void RefreshTargetState()
        {
            targetAvailable =
                figureTarget != null &&
                figureTarget.gameObject.activeInHierarchy;

            if (
                !targetAvailable ||
                activeInstance == null
            )
            {
                targetDetected = false;
                targetDistance = -1f;
                return;
            }

            Vector3 delta =
                Vector3.ProjectOnPlane(
                    figureTarget.position -
                    activeInstance.transform.position,
                    Vector3.up);

            targetDistance =
                delta.magnitude;

            float detectionRadius =
                perceptionActive
                    ? perceptionDetectionRadius
                    : blindDetectionRadius;

            targetDetected =
                targetDistance <=
                detectionRadius;
        }

        private void TickStateMachine()
        {
            if (activeInstance == null)
            {
                return;
            }

            if (!coreIntegrityActive)
            {
                if (
                    state !=
                    PrototypeMasterpieceAutonomyState.CoreDisabled
                )
                {
                    AbortAttack(
                        "Core integrity capability kapandı.");

                    EnterState(
                        PrototypeMasterpieceAutonomyState.CoreDisabled,
                        "Çekirdek capability kapalı; AI tamamen durdu.");
                }

                return;
            }

            bool possessedNow =
                possession != null &&
                possession.Possessed;

            if (possessedNow)
            {
                if (
                    state !=
                    PrototypeMasterpieceAutonomyState.PossessedSuspended
                )
                {
                    AbortAttack(
                        "Doğrudan sahiplenme AI otoritesini devraldı.");

                    possessionSuspendCount++;

                    EnterState(
                        PrototypeMasterpieceAutonomyState.PossessedSuspended,
                        "P sahiplenmesi aktif; AI askıda.");
                }

                return;
            }

            if (
                state ==
                PrototypeMasterpieceAutonomyState.PossessedSuspended
            )
            {
                possessionResumeCount++;

                EnterState(
                    PrototypeMasterpieceAutonomyState.Arming,
                    "Sahiplenme bırakıldı; AI kısa rearm süresinde.");

                stateEnteredAt =
                    Time.unscaledTime -
                    Mathf.Max(
                        0f,
                        deploymentArmingSeconds -
                        possessionReleaseRearmSeconds);

                return;
            }

            if (!autonomyEnabled)
            {
                if (
                    state !=
                    PrototypeMasterpieceAutonomyState.DisabledByUser
                )
                {
                    AbortAttack(
                        "Otonomi kapalı.");

                    EnterState(
                        PrototypeMasterpieceAutonomyState.DisabledByUser,
                        "I ile otonomi kapalı.");
                }

                return;
            }

            if (
                activeMovementCapabilityCount <= 0 &&
                !IsAttackState(state)
            )
            {
                if (
                    state !=
                    PrototypeMasterpieceAutonomyState.MovementDisabled
                )
                {
                    EnterState(
                        PrototypeMasterpieceAutonomyState.MovementDisabled,
                        "İki hareket capability'si de kapalı.");
                }

                if (
                    targetAvailable &&
                    targetDistance <=
                        attackRange &&
                    activeAttackCapabilityCount > 0 &&
                    Time.unscaledTime >=
                        nextAttackAllowedAt &&
                    TrySelectAttackPart(
                        out PrototypeMasterpiecePartKind stationaryAttack)
                )
                {
                    BeginAttack(
                        stationaryAttack,
                        figureTarget.position,
                        forced: false);
                }

                return;
            }

            switch (state)
            {
                case PrototypeMasterpieceAutonomyState.Arming:
                    TickArming();
                    break;

                case PrototypeMasterpieceAutonomyState.Patrol:
                    TickPatrol();
                    break;

                case PrototypeMasterpieceAutonomyState.Pursuit:
                    TickPursuit();
                    break;

                case PrototypeMasterpieceAutonomyState.Telegraph:
                    TickTelegraph();
                    break;

                case PrototypeMasterpieceAutonomyState.Swing:
                    TickSwing();
                    break;

                case PrototypeMasterpieceAutonomyState.Recovery:
                    TickRecovery();
                    break;

                case PrototypeMasterpieceAutonomyState.MovementDisabled:
                    if (activeMovementCapabilityCount > 0)
                    {
                        EnterState(
                            PrototypeMasterpieceAutonomyState.Arming,
                            "Hareket capability geri geldi.");
                    }
                    break;

                case PrototypeMasterpieceAutonomyState.CoreDisabled:
                    if (coreIntegrityActive)
                    {
                        EnterState(
                            PrototypeMasterpieceAutonomyState.Arming,
                            "Çekirdek capability geri geldi.");
                    }
                    break;

                default:
                    EnterState(
                        PrototypeMasterpieceAutonomyState.Arming,
                        "AI state yeniden arming'e alındı.");
                    break;
            }
        }

        private void TickArming()
        {
            float duration =
                deploymentArmingSeconds;

            stateProgress =
                Mathf.Clamp01(
                    (
                        Time.unscaledTime -
                        stateEnteredAt
                    ) /
                    Mathf.Max(
                        0.01f,
                        duration));

            if (stateProgress < 1f)
            {
                return;
            }

            if (
                targetDetected &&
                targetAvailable
            )
            {
                pursuitEnterCount++;

                EnterState(
                    PrototypeMasterpieceAutonomyState.Pursuit,
                    "Figür hedefi algılandı.");
            }
            else
            {
                EnterState(
                    PrototypeMasterpieceAutonomyState.Patrol,
                    "Aktif Figür hedefi yok; route anchor devriyesi.");
            }
        }

        private void TickPatrol()
        {
            stateProgress = 0f;

            if (
                targetDetected &&
                targetAvailable
            )
            {
                pursuitEnterCount++;

                EnterState(
                    PrototypeMasterpieceAutonomyState.Pursuit,
                    "Figür algılandı; pursuit başladı.");

                return;
            }

            Vector3 flatDelta =
                Vector3.ProjectOnPlane(
                    patrolTarget -
                    activeInstance.transform.position,
                    Vector3.up);

            if (
                flatDelta.magnitude <=
                patrolArrivalDistance
            )
            {
                SelectNextPatrolTarget();
            }
        }

        private void TickPursuit()
        {
            stateProgress = 0f;

            if (
                !targetAvailable ||
                targetDistance >
                    pursuitForgetRadius
            )
            {
                SelectNextPatrolTarget();

                EnterState(
                    PrototypeMasterpieceAutonomyState.Patrol,
                    "Figür hedefi kaybedildi; devriyeye dönüldü.");

                return;
            }

            FacePoint(
                figureTarget.position,
                Time.deltaTime);

            if (
                targetDistance <=
                    attackRange &&
                activeAttackCapabilityCount > 0 &&
                Time.unscaledTime >=
                    nextAttackAllowedAt &&
                TrySelectAttackPart(
                    out PrototypeMasterpiecePartKind selected)
            )
            {
                BeginAttack(
                    selected,
                    figureTarget.position,
                    forced: false);
            }
        }

        private void TickTelegraph()
        {
            stateProgress =
                Mathf.Clamp01(
                    (
                        Time.unscaledTime -
                        stateEnteredAt
                    ) /
                    Mathf.Max(
                        0.01f,
                        telegraphSeconds));

            if (!CurrentAttackCapabilityAvailable())
            {
                AbortAttack(
                    "Telegraph sırasında saldırı capability kapandı.");

                return;
            }

            if (targetAvailable)
            {
                lockedAttackTarget =
                    figureTarget.position;
            }

            FacePoint(
                lockedAttackTarget,
                Time.deltaTime);

            ApplyAttackRotation(
                currentAttackPart,
                Mathf.Lerp(
                    0f,
                    ResolveAttackSideSign(
                        currentAttackPart) *
                    -20f,
                    SmoothStep01(
                        stateProgress)));

            if (stateProgress >= 1f)
            {
                attackSwingCount++;

                EnterState(
                    PrototypeMasterpieceAutonomyState.Swing,
                    "Telegraph tamamlandı; swing ön izlemesi.");
            }
        }

        private void TickSwing()
        {
            stateProgress =
                Mathf.Clamp01(
                    (
                        Time.unscaledTime -
                        stateEnteredAt
                    ) /
                    Mathf.Max(
                        0.01f,
                        swingSeconds));

            if (!CurrentAttackCapabilityAvailable())
            {
                AbortAttack(
                    "Swing sırasında saldırı capability kapandı.");

                return;
            }

            float eased =
                1f -
                Mathf.Pow(
                    1f -
                    stateProgress,
                    3f);

            ApplyAttackRotation(
                currentAttackPart,
                ResolveAttackSideSign(
                    currentAttackPart) *
                Mathf.Lerp(
                    -20f,
                    78f,
                    eased));

            if (stateProgress >= 1f)
            {
                attackRecoveryCount++;

                EnterState(
                    PrototypeMasterpieceAutonomyState.Recovery,
                    "Swing tamamlandı; recovery.");
            }
        }

        private void TickRecovery()
        {
            stateProgress =
                Mathf.Clamp01(
                    (
                        Time.unscaledTime -
                        stateEnteredAt
                    ) /
                    Mathf.Max(
                        0.01f,
                        recoverySeconds));

            ApplyAttackRotation(
                currentAttackPart,
                ResolveAttackSideSign(
                    currentAttackPart) *
                Mathf.Lerp(
                    78f,
                    0f,
                    SmoothStep01(
                        stateProgress)));

            if (stateProgress < 1f)
            {
                return;
            }

            RestoreAttackPose(
                currentAttackPart);

            nextAttackAllowedAt =
                Time.unscaledTime +
                attackCooldownSeconds;

            if (
                activeMovementCapabilityCount <= 0
            )
            {
                EnterState(
                    PrototypeMasterpieceAutonomyState.MovementDisabled,
                    "Recovery sonrası hareket capability yok.");
            }
            else if (
                targetAvailable &&
                targetDistance <=
                    pursuitForgetRadius
            )
            {
                EnterState(
                    PrototypeMasterpieceAutonomyState.Pursuit,
                    "Recovery sonrası pursuit.");
            }
            else
            {
                SelectNextPatrolTarget();

                EnterState(
                    PrototypeMasterpieceAutonomyState.Patrol,
                    "Recovery sonrası devriye.");
            }
        }

        private void BeginAttack(
            PrototypeMasterpiecePartKind selected,
            Vector3 target,
            bool forced)
        {
            if (IsAttackState(state))
            {
                return;
            }

            currentAttackPart = selected;
            lockedAttackTarget = target;
            attackDecisionCount++;
            attackTelegraphCount++;

            EnterState(
                PrototypeMasterpieceAutonomyState.Telegraph,
                forced
                    ? "Editor doğrudan saldırı kararı."
                    : "AI saldırı kararı; telegraph başladı.");
        }

        private void AbortAttack(
            string reason)
        {
            if (!IsAttackState(state))
            {
                HideTelegraph();
                return;
            }

            attackAbortCount++;
            RestoreAttackPose(
                currentAttackPart);

            HideTelegraph();
            nextAttackAllowedAt =
                Time.unscaledTime +
                attackCooldownSeconds;

            if (
                possession != null &&
                possession.Possessed
            )
            {
                state =
                    PrototypeMasterpieceAutonomyState.PossessedSuspended;
            }
            else if (!autonomyEnabled)
            {
                state =
                    PrototypeMasterpieceAutonomyState.DisabledByUser;
            }
            else if (!coreIntegrityActive)
            {
                state =
                    PrototypeMasterpieceAutonomyState.CoreDisabled;
            }
            else if (
                activeMovementCapabilityCount <= 0
            )
            {
                state =
                    PrototypeMasterpieceAutonomyState.MovementDisabled;
            }
            else
            {
                state =
                    PrototypeMasterpieceAutonomyState.Arming;

                stateEnteredAt =
                    Time.unscaledTime;
            }

            lastAction =
                $"Saldırı iptal edildi: {reason}";
        }

        private void SimulateAutonomousMovement(
            float deltaTime)
        {
            if (
                activeInstance == null ||
                body == null ||
                activeMovementCapabilityCount <= 0
            )
            {
                return;
            }

            Vector3 destination =
                state ==
                    PrototypeMasterpieceAutonomyState.Pursuit &&
                targetAvailable
                    ? figureTarget.position
                    : patrolTarget;

            Vector3 flatDirection =
                Vector3.ProjectOnPlane(
                    destination -
                    body.position,
                    Vector3.up);

            float remainingDistance =
                flatDirection.magnitude;

            if (remainingDistance <= 0.001f)
            {
                return;
            }

            flatDirection.Normalize();

            float speed =
                activeMovementCapabilityCount >= 2
                    ? twoContactMoveSpeed
                    : oneContactMoveSpeed;

            float stopDistance =
                state ==
                    PrototypeMasterpieceAutonomyState.Pursuit
                    ? Mathf.Max(
                        0.2f,
                        attackRange *
                        0.76f)
                    : patrolArrivalDistance;

            if (remainingDistance <= stopDistance)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    flatDirection,
                    Vector3.up);

            body.MoveRotation(
                Quaternion.RotateTowards(
                    body.rotation,
                    targetRotation,
                    turnSpeedDegrees *
                    deltaTime));

            float stepDistance =
                Mathf.Min(
                    speed *
                    deltaTime,
                    Mathf.Max(
                        0f,
                        remainingDistance -
                        stopDistance));

            if (stepDistance <= 0.0001f)
            {
                return;
            }

            Vector3 probeOrigin =
                body.position +
                Vector3.up *
                movementProbeHeight;

            if (Physics.SphereCast(
                    probeOrigin,
                    movementProbeRadius,
                    flatDirection,
                    out RaycastHit obstruction,
                    stepDistance + 0.08f,
                    movementCollisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                if (
                    figureTarget == null ||
                    !obstruction.transform.IsChildOf(
                        figureTarget)
                )
                {
                    blockedMovementCount++;

                    if (
                        state ==
                        PrototypeMasterpieceAutonomyState.Patrol
                    )
                    {
                        SelectNextPatrolTarget();
                    }

                    lastAction =
                        $"AI hareketi engellendi: {obstruction.collider.name}";

                    return;
                }
            }

            Vector3 proposed =
                body.position +
                flatDirection *
                stepDistance;

            Vector3 horizontalOffset =
                Vector3.ProjectOnPlane(
                    proposed -
                    autonomyOrigin,
                    Vector3.up);

            if (
                horizontalOffset.magnitude >
                leashRadius
            )
            {
                horizontalOffset =
                    horizontalOffset.normalized *
                    leashRadius;

                proposed =
                    new Vector3(
                        autonomyOrigin.x +
                            horizontalOffset.x,
                        proposed.y,
                        autonomyOrigin.z +
                            horizontalOffset.z);

                leashClampCount++;
            }

            Vector3 groundOrigin =
                proposed +
                Vector3.up *
                groundProbeHeight;

            if (!Physics.Raycast(
                    groundOrigin,
                    Vector3.down,
                    out RaycastHit ground,
                    groundProbeDistance,
                    movementCollisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                groundRejectCount++;

                if (
                    state ==
                    PrototypeMasterpieceAutonomyState.Patrol
                )
                {
                    SelectNextPatrolTarget();
                }

                lastAction =
                    "AI hedef adımının altında güvenli zemin yok.";

                return;
            }

            proposed.y =
                ground.point.y +
                0.02f;

            Vector3 before =
                body.position;

            body.MovePosition(
                proposed);

            travelledDistance +=
                Vector3.Distance(
                    before,
                    proposed);

            currentLeashDistance =
                Vector3.ProjectOnPlane(
                    proposed -
                    autonomyOrigin,
                    Vector3.up)
                .magnitude;
        }

        private void FacePoint(
            Vector3 worldPoint,
            float deltaTime)
        {
            if (
                body == null ||
                activeInstance == null
            )
            {
                return;
            }

            Vector3 direction =
                Vector3.ProjectOnPlane(
                    worldPoint -
                    body.position,
                    Vector3.up);

            if (direction.sqrMagnitude <=
                0.0001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);

            body.MoveRotation(
                Quaternion.RotateTowards(
                    body.rotation,
                    targetRotation,
                    turnSpeedDegrees *
                    Mathf.Max(
                        0.001f,
                        deltaTime)));
        }

        private void SelectNextPatrolTarget()
        {
            if (activeInstance == null)
            {
                return;
            }

            const int pointCount = 6;

            for (int attempt = 0;
                 attempt < pointCount;
                 attempt++)
            {
                int index =
                    (
                        patrolIndex +
                        attempt
                    ) %
                    pointCount;

                float angle =
                    (
                        index /
                        (float)pointCount
                    ) *
                    Mathf.PI *
                    2f +
                    (
                        activeInstance.SnapshotHash %
                        360
                    ) *
                    Mathf.Deg2Rad;

                float radius =
                    patrolRadius *
                    (
                        index % 2 == 0
                            ? 1f
                            : 0.72f
                    );

                Vector3 candidate =
                    autonomyOrigin +
                    new Vector3(
                        Mathf.Cos(angle) *
                            radius,
                        0f,
                        Mathf.Sin(angle) *
                            radius);

                Vector3 groundOrigin =
                    candidate +
                    Vector3.up *
                    groundProbeHeight;

                if (Physics.Raycast(
                        groundOrigin,
                        Vector3.down,
                        out RaycastHit ground,
                        groundProbeDistance,
                        movementCollisionMask,
                        QueryTriggerInteraction.Ignore))
                {
                    patrolTarget =
                        ground.point +
                        Vector3.up *
                        0.02f;

                    patrolIndex =
                        (
                            index +
                            1
                        ) %
                        pointCount;

                    patrolTargetCount++;
                    return;
                }
            }

            patrolTarget =
                autonomyOrigin;

            patrolTargetCount++;
            lastAction =
                "Geçerli devriye zemini bulunamadı; origin hedeflendi.";
        }

        private bool TrySelectAttackPart(
            out PrototypeMasterpiecePartKind selected)
        {
            if (
                leftAttackActive &&
                rightAttackActive
            )
            {
                selected =
                    nextAttackUsesLeft
                        ? PrototypeMasterpiecePartKind.LeftAttack
                        : PrototypeMasterpiecePartKind.RightAttack;

                nextAttackUsesLeft =
                    !nextAttackUsesLeft;

                return true;
            }

            if (leftAttackActive)
            {
                selected =
                    PrototypeMasterpiecePartKind.LeftAttack;

                return true;
            }

            if (rightAttackActive)
            {
                selected =
                    PrototypeMasterpiecePartKind.RightAttack;

                return true;
            }

            selected =
                PrototypeMasterpiecePartKind.LeftAttack;

            return false;
        }

        private bool CurrentAttackCapabilityAvailable()
        {
            return currentAttackPart ==
                    PrototypeMasterpiecePartKind.LeftAttack
                ? leftAttackActive
                : rightAttackActive;
        }

        private void CaptureAttackPoses()
        {
            attackPoses.Clear();

            CaptureAttackPose(
                PrototypeMasterpiecePartKind.LeftAttack);

            CaptureAttackPose(
                PrototypeMasterpiecePartKind.RightAttack);
        }

        private void CaptureAttackPose(
            PrototypeMasterpiecePartKind kind)
        {
            if (
                activeInstance == null ||
                !activeInstance.TryGetPartRoot(
                    kind,
                    out Transform partRoot) ||
                partRoot == null
            )
            {
                return;
            }

            attackPoses[kind] =
                new AttackPose
                {
                    Transform = partRoot,
                    LocalRotation =
                        partRoot.localRotation
                };
        }

        private void ApplyAttackRotation(
            PrototypeMasterpiecePartKind kind,
            float zDegrees)
        {
            if (
                !attackPoses.TryGetValue(
                    kind,
                    out AttackPose pose) ||
                pose?.Transform == null
            )
            {
                return;
            }

            pose.Transform.localRotation =
                pose.LocalRotation *
                Quaternion.AngleAxis(
                    zDegrees,
                    Vector3.forward);
        }

        private void RestoreAttackPose(
            PrototypeMasterpiecePartKind kind)
        {
            if (
                attackPoses.TryGetValue(
                    kind,
                    out AttackPose pose) &&
                pose?.Transform != null
            )
            {
                pose.Transform.localRotation =
                    pose.LocalRotation;
            }
        }

        private void RestoreAttackPoses()
        {
            foreach (KeyValuePair<
                         PrototypeMasterpiecePartKind,
                         AttackPose> pair in attackPoses)
            {
                AttackPose pose =
                    pair.Value;

                if (pose?.Transform != null)
                {
                    pose.Transform.localRotation =
                        pose.LocalRotation;
                }
            }
        }

        private void EnsureTelegraph()
        {
            if (
                telegraphRing != null &&
                telegraphGuide != null
            )
            {
                return;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default");
            }

            if (
                shader != null &&
                telegraphMaterial == null
            )
            {
                telegraphMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M49_0_AutonomousTelegraph_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };

                Color color =
                    new Color(
                        1f,
                        0.20f,
                        0.025f,
                        0.94f);

                if (telegraphMaterial.HasProperty(
                        "_BaseColor"))
                {
                    telegraphMaterial.SetColor(
                        "_BaseColor",
                        color);
                }

                if (telegraphMaterial.HasProperty(
                        "_Color"))
                {
                    telegraphMaterial.SetColor(
                        "_Color",
                        color);
                }
            }

            telegraphRing =
                CreateTelegraphLine(
                    "M49_AttackTargetRing",
                    loop: true,
                    width: 0.085f);

            telegraphGuide =
                CreateTelegraphLine(
                    "M49_AttackGuide",
                    loop: false,
                    width: 0.055f);

            HideTelegraph();
        }

        private LineRenderer CreateTelegraphLine(
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

            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.shadowCastingMode =
                ShadowCastingMode.Off;

            line.receiveShadows = false;
            line.widthMultiplier = width;
            line.sharedMaterial =
                telegraphMaterial;

            Color color =
                new Color(
                    1f,
                    0.20f,
                    0.025f,
                    0.94f);

            line.startColor = color;
            line.endColor = color;
            line.enabled = false;

            return line;
        }

        private void UpdateTelegraph()
        {
            bool visible =
                state ==
                    PrototypeMasterpieceAutonomyState.Telegraph ||
                state ==
                    PrototypeMasterpieceAutonomyState.Swing;

            if (
                !visible ||
                activeInstance == null
            )
            {
                HideTelegraph();
                return;
            }

            if (telegraphRing != null)
            {
                int segments =
                    Mathf.Max(
                        12,
                        telegraphSegments);

                telegraphRing.positionCount =
                    segments;

                float pulse =
                    state ==
                        PrototypeMasterpieceAutonomyState.Telegraph
                        ? Mathf.Lerp(
                            0.78f,
                            1f,
                            Mathf.PingPong(
                                stateProgress *
                                2f,
                                1f))
                        : 1f;

                float radius =
                    telegraphRadius *
                    pulse;

                Vector3 center =
                    lockedAttackTarget +
                    Vector3.up *
                    0.035f;

                for (int index = 0;
                     index < segments;
                     index++)
                {
                    float angle =
                        index /
                        (float)segments *
                        Mathf.PI *
                        2f;

                    telegraphRing.SetPosition(
                        index,
                        center +
                        new Vector3(
                            Mathf.Cos(angle) *
                                radius,
                            0f,
                            Mathf.Sin(angle) *
                                radius));
                }

                telegraphRing.enabled = true;
            }

            if (telegraphGuide != null)
            {
                telegraphGuide.positionCount = 2;

                Vector3 start =
                    ResolveAttackPoint(
                        currentAttackPart);

                telegraphGuide.SetPosition(
                    0,
                    start);

                telegraphGuide.SetPosition(
                    1,
                    lockedAttackTarget +
                    Vector3.up *
                    0.8f);

                telegraphGuide.enabled = true;
            }
        }

        private Vector3 ResolveAttackPoint(
            PrototypeMasterpiecePartKind kind)
        {
            if (
                activeInstance != null &&
                activeInstance.TryGetWorldPart(
                    kind,
                    out PrototypeMasterpieceWorldPart part) &&
                part != null &&
                part.ProxyCollider != null
            )
            {
                return part.ProxyCollider.bounds.center;
            }

            return activeInstance != null
                ? activeInstance.transform.position +
                    Vector3.up *
                    1.2f
                : transform.position;
        }

        private void HideTelegraph()
        {
            if (telegraphRing != null)
            {
                telegraphRing.enabled = false;
            }

            if (telegraphGuide != null)
            {
                telegraphGuide.enabled = false;
            }
        }

        private void EnterState(
            PrototypeMasterpieceAutonomyState next,
            string reason)
        {
            state = next;
            stateEnteredAt =
                Time.unscaledTime;

            stateProgress = 0f;
            lastAction = reason;

            if (!IsAttackState(next))
            {
                HideTelegraph();
            }
        }

        private static bool IsAttackState(
            PrototypeMasterpieceAutonomyState candidate)
        {
            return
                candidate ==
                    PrototypeMasterpieceAutonomyState.Telegraph ||
                candidate ==
                    PrototypeMasterpieceAutonomyState.Swing ||
                candidate ==
                    PrototypeMasterpieceAutonomyState.Recovery;
        }

        private static float ResolveAttackSideSign(
            PrototypeMasterpiecePartKind kind)
        {
            return kind ==
                    PrototypeMasterpiecePartKind.LeftAttack
                ? -1f
                : 1f;
        }

        private static float SmoothStep01(
            float value)
        {
            value =
                Mathf.Clamp01(
                    value);

            return value *
                value *
                (
                    3f -
                    2f *
                    value
                );
        }

        private void RefreshHud()
        {
            bool visible =
                activeInstance != null;

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
                    "BAŞ YAPIT • OTONOM ENCOUNTER";
            }

            if (stateText != null)
            {
                stateText.text =
                    $"DURUM {TranslateState(state)}   " +
                    $"HEDEF {(targetDetected ? "ALGILANDI" : targetAvailable ? "UZAK" : "YOK")}   " +
                    $"MESAFE {(targetDistance >= 0f ? targetDistance.ToString("F1") : "-")} m\n" +
                    $"HAREKET {activeMovementCapabilityCount}/2   " +
                    $"SALDIRI {activeAttackCapabilityCount}/2   " +
                    $"ALGI {(perceptionActive ? "AÇIK" : "KAPALI")}   " +
                    $"CORE {(coreIntegrityActive ? "AÇIK" : "KAPALI")}\n" +
                    $"KARAR {attackDecisionCount}   " +
                    $"SWING {attackSwingCount}   " +
                    $"İPTAL {attackAbortCount}   " +
                    $"YOL {travelledDistance:F1} m\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    painterControlRoleActive
                        ? "I • OTONOMİ AÇ/KAPAT   " +
                          "P • SAHİPLENME AI'DAN ÖNCELİKLİ\n" +
                          "PAINTER OTORİTESİ • TELEGRAPH/HIT BRIDGE"
                        : "FIGURE • KAÇIN / KARŞI OYUN YAP\n" +
                          "DEPLOY • SAHİPLENME • OTONOMİ KONTROLÜ PAINTER'A AİT";
            }
        }

        private static string TranslateState(
            PrototypeMasterpieceAutonomyState value)
        {
            switch (value)
            {
                case PrototypeMasterpieceAutonomyState.DisabledByUser:
                    return "KAPALI";

                case PrototypeMasterpieceAutonomyState.Arming:
                    return "ARMING";

                case PrototypeMasterpieceAutonomyState.Patrol:
                    return "DEVRİYE";

                case PrototypeMasterpieceAutonomyState.Pursuit:
                    return "TAKİP";

                case PrototypeMasterpieceAutonomyState.Telegraph:
                    return "TELEGRAPH";

                case PrototypeMasterpieceAutonomyState.Swing:
                    return "SWING";

                case PrototypeMasterpieceAutonomyState.Recovery:
                    return "RECOVERY";

                case PrototypeMasterpieceAutonomyState.PossessedSuspended:
                    return "SAHİPLENİLDİ";

                case PrototypeMasterpieceAutonomyState.MovementDisabled:
                    return "HAREKETSİZ";

                case PrototypeMasterpieceAutonomyState.CoreDisabled:
                    return "CORE KAPALI";

                default:
                    return "BEKLİYOR";
            }
        }

        private void OnDisable()
        {
            DisableInputActions();
            AbortAttack(
                "M49 controller devre dışı.");
            RestoreAttackPoses();
            HideTelegraph();
        }

        private void OnDestroy()
        {
            if (toggleAutonomyAction != null)
            {
                toggleAutonomyAction.performed -=
                    HandleToggleAutonomy;

                toggleAutonomyAction.Dispose();
                toggleAutonomyAction = null;
            }

            if (telegraphMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        telegraphMaterial);
                }
                else
                {
                    DestroyImmediate(
                        telegraphMaterial);
                }

                telegraphMaterial = null;
            }
        }
    }
}
