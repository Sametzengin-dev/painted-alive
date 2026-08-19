using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(24750)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceHitValidationController :
        MonoBehaviour
    {
        private const int DebugCircleSegments = 28;

        [Header("Sources")]
        [SerializeField]
        private PrototypeMasterpieceWorldDeploymentController deployment;

        [SerializeField]
        private PrototypeMasterpieceAutonomousEncounterController encounter;

        [SerializeField]
        private PrototypeMasterpiecePossessionController possession;

        [SerializeField]
        private PrototypeFigureHitQueryVolume figureVolume;

        [Header("Attack Query")]
        [SerializeField, Min(0.05f)]
        private float attackCapsuleRadius = 0.38f;

        [SerializeField, Min(0.2f)]
        private float attackCapsuleReach = 1.62f;

        [SerializeField, Min(0f)]
        private float attackBackOffset = 0.08f;

        [SerializeField, Range(0.05f, 0.95f)]
        private float strikeMomentNormalized = 0.46f;

        [SerializeField]
        private bool checkWorldOcclusion = true;

        [SerializeField]
        private LayerMask occlusionMask = ~0;

        [SerializeField, Min(0f)]
        private float occlusionSkin = 0.08f;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Debug")]
        [SerializeField] private bool debugVolumesVisible;

        [SerializeField] private Color attackVolumeColor =
            new Color(1f, 0.23f, 0.035f, 0.95f);

        [SerializeField] private Color targetVolumeColor =
            new Color(0.06f, 0.78f, 0.84f, 0.95f);

        [SerializeField] private Color hitResultColor =
            new Color(0.23f, 0.92f, 0.38f, 0.95f);

        [SerializeField] private Color missResultColor =
            new Color(1f, 0.70f, 0.08f, 0.95f);

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeMasterpieceHitValidationResult lastResult =
            PrototypeMasterpieceHitValidationResult.None;

        [SerializeField]
        private PrototypeMasterpieceAutonomyState previousEncounterState =
            PrototypeMasterpieceAutonomyState.WaitingForWorld;

        [SerializeField] private bool inputActionsActive;
        [SerializeField] private bool sequenceActive;
        [SerializeField] private bool strikePending;
        [SerializeField] private bool strikeValidated;
        [SerializeField] private bool telegraphPotentialOverlap;
        [SerializeField] private bool lastOverlap;
        [SerializeField] private bool lastOccluded;
        [SerializeField] private int sequenceId;
        [SerializeField] private int telegraphSnapshotCount;
        [SerializeField] private int validationCount;
        [SerializeField] private int hitCount;
        [SerializeField] private int missCount;
        [SerializeField] private int abortedCount;
        [SerializeField] private int duplicateValidationPreventedCount;
        [SerializeField] private int occlusionQueryCount;
        [SerializeField] private int volumeBuildFailureCount;
        [SerializeField] private int debugToggleCount;
        [SerializeField] private float lastCapsuleDistance;
        [SerializeField] private float lastCombinedRadius;
        [SerializeField] private float lastStrikeProgress;
        [SerializeField] private Vector3 telegraphSnapshotCenter;
        [SerializeField] private Vector3 lastAttackPointA;
        [SerializeField] private Vector3 lastAttackPointB;
        [SerializeField] private float lastAttackRadius;
        [SerializeField] private Vector3 lastTargetPointA;
        [SerializeField] private Vector3 lastTargetPointB;
        [SerializeField] private float lastTargetRadius;
        [SerializeField] private string lastReason =
            "M49.0 saldırı kararı bekleniyor.";

        private readonly RaycastHit[] occlusionHits =
            new RaycastHit[24];

        private InputAction toggleDebugAction;
        private Material debugMaterial;
        private LineRenderer attackAxis;
        private LineRenderer attackRingA;
        private LineRenderer attackRingB;
        private LineRenderer targetAxis;
        private LineRenderer targetRingA;
        private LineRenderer targetRingB;
        private LineRenderer resultRing;
        private float resultVisibleUntil;

        public PrototypeMasterpieceHitValidationResult LastResult =>
            lastResult;
        public bool InputActionsActive => inputActionsActive;
        public bool SequenceActive => sequenceActive;
        public bool StrikePending => strikePending;
        public bool StrikeValidated => strikeValidated;
        public bool DebugVolumesVisible => debugVolumesVisible;
        public int SequenceId => sequenceId;
        public int TelegraphSnapshotCount => telegraphSnapshotCount;
        public int ValidationCount => validationCount;
        public int HitCount => hitCount;
        public int MissCount => missCount;
        public int AbortedCount => abortedCount;
        public int DuplicateValidationPreventedCount =>
            duplicateValidationPreventedCount;
        public int OcclusionQueryCount => occlusionQueryCount;
        public int VolumeBuildFailureCount => volumeBuildFailureCount;
        public int DebugToggleCount => debugToggleCount;
        public float LastCapsuleDistance => lastCapsuleDistance;
        public float LastCombinedRadius => lastCombinedRadius;
        public float LastStrikeProgress => lastStrikeProgress;
        public string LastReason => lastReason;

        public bool HitValidationEnabled => true;
        public bool GameplayHitAuthorityEnabled => false;
        public bool ClarityWritesEnabled => false;
        public bool DamageAuthorityEnabled => false;
        public bool KnockbackEnabled => false;
        public bool ControlLockEnabled => false;
        public bool SingleResultPerSwing => true;
        public bool QueryOnlyVolumes => true;
        public bool NetworkIntegrationParked => true;

        public void Configure(
            PrototypeMasterpieceWorldDeploymentController
                configuredDeployment,
            PrototypeMasterpieceAutonomousEncounterController
                configuredEncounter,
            PrototypeMasterpiecePossessionController
                configuredPossession,
            PrototypeFigureHitQueryVolume
                configuredFigureVolume,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            deployment = configuredDeployment;
            encounter = configuredEncounter;
            possession = configuredPossession;
            figureVolume = configuredFigureVolume;
            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            previousEncounterState =
                encounter != null
                    ? encounter.State
                    : PrototypeMasterpieceAutonomyState.WaitingForWorld;

            EnsureDebugResources();
            RefreshHud();
        }

        private void Awake()
        {
            EnsureInputActions();
            EnsureDebugResources();

            previousEncounterState =
                encounter != null
                    ? encounter.State
                    : PrototypeMasterpieceAutonomyState.WaitingForWorld;

            RefreshHud();
        }

        private void OnEnable()
        {
            EnsureInputActions();
            EnableInputActions();
            EnsureDebugResources();
            RefreshHud();
        }

        private void Update()
        {
            if (encounter == null)
            {
                RefreshHud();
                return;
            }

            PrototypeMasterpieceAutonomyState currentState =
                encounter.State;

            if (currentState != previousEncounterState)
            {
                HandleStateTransition(
                    previousEncounterState,
                    currentState);

                previousEncounterState =
                    currentState;
            }

            if (
                currentState ==
                    PrototypeMasterpieceAutonomyState.Swing &&
                strikePending
            )
            {
                lastStrikeProgress =
                    encounter.StateProgress;

                if (
                    encounter.StateProgress >=
                    strikeMomentNormalized
                )
                {
                    ValidateStrike(
                        "Normalized strike window");
                }
            }

            if (
                Time.unscaledTime >
                resultVisibleUntil
            )
            {
                SetRendererVisible(
                    resultRing,
                    false);
            }

            UpdateDebugVolumes();
            RefreshHud();
        }

        public void ToggleDebugVolumes()
        {
            debugVolumesVisible =
                !debugVolumesVisible;

            debugToggleCount++;

            if (!debugVolumesVisible)
            {
                HideQueryDebug();
            }

            lastReason =
                debugVolumesVisible
                    ? "Query capsule debug görünümü açıldı."
                    : "Query capsule debug görünümü kapatıldı.";

            RefreshHud();
        }

        public void ForceValidateCurrentStrike()
        {
            if (
                encounter == null ||
                encounter.State !=
                    PrototypeMasterpieceAutonomyState.Swing
            )
            {
                lastReason =
                    "Doğrudan doğrulama için encounter Swing durumunda olmalı.";

                RefreshHud();
                return;
            }

            if (!strikePending)
            {
                BeginSequenceFromCurrentState();
                strikePending = true;
            }

            ValidateStrike(
                "Editor force validation");
        }

        private void EnsureInputActions()
        {
            if (toggleDebugAction != null)
            {
                return;
            }

            toggleDebugAction =
                new InputAction(
                    "M49.1 Toggle Hit Volumes",
                    InputActionType.Button,
                    "<Keyboard>/h");

            toggleDebugAction.performed +=
                HandleToggleDebug;
        }

        private void EnableInputActions()
        {
            toggleDebugAction?.Enable();

            inputActionsActive =
                toggleDebugAction != null &&
                toggleDebugAction.enabled;
        }

        private void DisableInputActions()
        {
            toggleDebugAction?.Disable();
            inputActionsActive = false;
        }

        private void HandleToggleDebug(
            InputAction.CallbackContext context)
        {
            ToggleDebugVolumes();
        }

        private void HandleStateTransition(
            PrototypeMasterpieceAutonomyState previous,
            PrototypeMasterpieceAutonomyState current)
        {
            if (
                current ==
                PrototypeMasterpieceAutonomyState.Telegraph
            )
            {
                BeginSequenceFromCurrentState();
                return;
            }

            if (
                current ==
                PrototypeMasterpieceAutonomyState.Swing
            )
            {
                if (!sequenceActive)
                {
                    BeginSequenceFromCurrentState();
                }

                strikePending = true;
                strikeValidated = false;
                lastResult =
                    PrototypeMasterpieceHitValidationResult.Pending;
                lastReason =
                    "Swing başladı; doğrulama penceresi bekleniyor.";

                CaptureTelegraphPotentialOverlap();
                return;
            }

            if (
                previous ==
                    PrototypeMasterpieceAutonomyState.Swing &&
                strikePending &&
                !strikeValidated
            )
            {
                ValidateStrike(
                    "Swing state exit fallback");

                return;
            }

            if (
                sequenceActive &&
                IsAbortState(current)
            )
            {
                PrototypeMasterpieceHitValidationResult abortResult =
                    ResolveAbortResult(
                        current);

                CompleteAborted(
                    abortResult,
                    $"Encounter state {current} nedeniyle saldırı iptal.");
                return;
            }

            if (
                sequenceActive &&
                previous ==
                    PrototypeMasterpieceAutonomyState.Telegraph &&
                current !=
                    PrototypeMasterpieceAutonomyState.Swing
            )
            {
                CompleteAborted(
                    PrototypeMasterpieceHitValidationResult.AbortedStateChange,
                    $"Telegraph {current} durumuna geçti.");
                return;
            }

            if (
                sequenceActive &&
                previous ==
                    PrototypeMasterpieceAutonomyState.Recovery &&
                current !=
                    PrototypeMasterpieceAutonomyState.Recovery
            )
            {
                sequenceActive = false;
                strikePending = false;
            }
        }

        private void BeginSequenceFromCurrentState()
        {
            sequenceId++;
            sequenceActive = true;
            strikePending = false;
            strikeValidated = false;
            telegraphPotentialOverlap = false;
            lastOverlap = false;
            lastOccluded = false;
            lastCapsuleDistance = -1f;
            lastCombinedRadius = 0f;
            lastStrikeProgress = 0f;
            lastResult =
                PrototypeMasterpieceHitValidationResult.Pending;

            if (
                figureVolume != null &&
                figureVolume.TryGetWorldCapsule(
                    out _,
                    out _,
                    out _,
                    out Vector3 targetCenter,
                    out _)
            )
            {
                telegraphSnapshotCenter =
                    targetCenter;

                telegraphSnapshotCount++;
            }
            else
            {
                telegraphSnapshotCenter =
                    encounter != null
                        ? encounter.LockedAttackTarget
                        : Vector3.zero;

                volumeBuildFailureCount++;
            }

            lastReason =
                $"Saldırı sequence {sequenceId} telegraph snapshot aldı.";
        }

        private void CaptureTelegraphPotentialOverlap()
        {
            if (!TryBuildVolumes(
                    out Vector3 attackA,
                    out Vector3 attackB,
                    out float attackRadius,
                    out Vector3 targetA,
                    out Vector3 targetB,
                    out float targetRadius,
                    out _))
            {
                telegraphPotentialOverlap = false;
                return;
            }

            float squaredDistance =
                SegmentSegmentSquaredDistance(
                    attackA,
                    attackB,
                    targetA,
                    targetB);

            float combined =
                attackRadius +
                targetRadius;

            telegraphPotentialOverlap =
                squaredDistance <=
                combined *
                combined;
        }

        private void ValidateStrike(
            string source)
        {
            if (strikeValidated)
            {
                duplicateValidationPreventedCount++;
                return;
            }

            strikeValidated = true;
            strikePending = false;
            validationCount++;

            if (
                deployment == null ||
                deployment.ActiveInstance == null ||
                encounter == null ||
                encounter.ActiveInstance == null
            )
            {
                CompleteAborted(
                    PrototypeMasterpieceHitValidationResult.AbortedRecall,
                    "Strike sırasında dünya instance yok.");

                return;
            }

            if (
                possession != null &&
                possession.Possessed
            )
            {
                CompleteAborted(
                    PrototypeMasterpieceHitValidationResult.AbortedPossession,
                    "Doğrudan sahiplenme strike otoritesini devraldı.");

                return;
            }

            if (!CurrentAttackCapabilityActive())
            {
                CompleteAborted(
                    PrototypeMasterpieceHitValidationResult.AbortedCapability,
                    "Aktif saldırı capability strike anında kapalı.");

                return;
            }

            if (!TryBuildVolumes(
                    out Vector3 attackA,
                    out Vector3 attackB,
                    out float attackRadius,
                    out Vector3 targetA,
                    out Vector3 targetB,
                    out float targetRadius,
                    out Vector3 targetCenter))
            {
                CompleteMiss(
                    PrototypeMasterpieceHitValidationResult.TargetUnavailable,
                    "Figür veya saldırı query hacmi üretilemedi.");

                return;
            }

            lastAttackPointA = attackA;
            lastAttackPointB = attackB;
            lastAttackRadius = attackRadius;
            lastTargetPointA = targetA;
            lastTargetPointB = targetB;
            lastTargetRadius = targetRadius;

            float squaredDistance =
                SegmentSegmentSquaredDistance(
                    attackA,
                    attackB,
                    targetA,
                    targetB);

            lastCapsuleDistance =
                Mathf.Sqrt(
                    Mathf.Max(
                        0f,
                        squaredDistance));

            lastCombinedRadius =
                attackRadius +
                targetRadius;

            lastOverlap =
                squaredDistance <=
                lastCombinedRadius *
                lastCombinedRadius;

            lastOccluded =
                lastOverlap &&
                checkWorldOcclusion &&
                IsOccluded(
                    attackA,
                    targetCenter);

            if (
                lastOverlap &&
                !lastOccluded
            )
            {
                lastResult =
                    PrototypeMasterpieceHitValidationResult.HitConfirmed;

                hitCount++;
                lastReason =
                    $"{source}: query capsule overlap doğrulandı.";

                ShowResultCue(
                    targetCenter,
                    hit: true);

                Debug.Log(
                    "[M49.1 Hit Confirmed]\n" +
                    $"SequenceId={sequenceId}\n" +
                    $"AttackPart={encounter.CurrentAttackPart}\n" +
                    $"CapsuleDistance={lastCapsuleDistance:F3}\n" +
                    $"CombinedRadius={lastCombinedRadius:F3}\n" +
                    $"Occluded={lastOccluded}\n" +
                    "GameplayHitAuthorityEnabled=False\n" +
                    "ClarityWritesEnabled=False\n" +
                    "DamageAuthorityEnabled=False",
                    encounter.ActiveInstance);

                return;
            }

            PrototypeMasterpieceHitValidationResult missResult;

            if (lastOccluded)
            {
                missResult =
                    PrototypeMasterpieceHitValidationResult.MissOccluded;
            }
            else if (telegraphPotentialOverlap)
            {
                missResult =
                    PrototypeMasterpieceHitValidationResult.MissDodged;
            }
            else
            {
                missResult =
                    PrototypeMasterpieceHitValidationResult.MissOutOfReach;
            }

            CompleteMiss(
                missResult,
                $"{source}: overlap={lastOverlap}, occluded={lastOccluded}.");

            ShowResultCue(
                targetCenter,
                hit: false);
        }

        private bool TryBuildVolumes(
            out Vector3 attackA,
            out Vector3 attackB,
            out float attackRadius,
            out Vector3 targetA,
            out Vector3 targetB,
            out float targetRadius,
            out Vector3 targetCenter)
        {
            attackA = default;
            attackB = default;
            attackRadius = attackCapsuleRadius;
            targetA = default;
            targetB = default;
            targetRadius = 0f;
            targetCenter = default;

            if (
                encounter == null ||
                encounter.ActiveInstance == null ||
                figureVolume == null
            )
            {
                volumeBuildFailureCount++;
                return false;
            }

            if (!TryResolveAttackOrigin(
                    out Vector3 attackOrigin))
            {
                volumeBuildFailureCount++;
                return false;
            }

            if (!figureVolume.TryGetWorldCapsule(
                    out targetA,
                    out targetB,
                    out targetRadius,
                    out targetCenter,
                    out _))
            {
                volumeBuildFailureCount++;
                return false;
            }

            Vector3 lockedTarget =
                encounter.LockedAttackTarget;

            Vector3 direction =
                lockedTarget -
                attackOrigin;

            if (direction.sqrMagnitude <=
                0.0001f)
            {
                direction =
                    targetCenter -
                    attackOrigin;
            }

            if (direction.sqrMagnitude <=
                0.0001f)
            {
                direction =
                    encounter.ActiveInstance.transform.forward;
            }

            direction.Normalize();

            float reach =
                Mathf.Max(
                    0.2f,
                    attackCapsuleReach);

            attackRadius =
                Mathf.Max(
                    0.05f,
                    attackCapsuleRadius);

            attackA =
                attackOrigin -
                direction *
                attackBackOffset;

            attackB =
                attackOrigin +
                direction *
                reach;

            return true;
        }

        private bool TryResolveAttackOrigin(
            out Vector3 origin)
        {
            origin = default;

            if (
                encounter == null ||
                encounter.ActiveInstance == null
            )
            {
                return false;
            }

            if (
                encounter.ActiveInstance.TryGetWorldPart(
                    encounter.CurrentAttackPart,
                    out PrototypeMasterpieceWorldPart part) &&
                part != null &&
                part.ProxyCollider != null
            )
            {
                origin =
                    part.ProxyCollider.bounds.center;

                return true;
            }

            origin =
                encounter.ActiveInstance.transform.position +
                Vector3.up *
                1.2f;

            return true;
        }

        private bool CurrentAttackCapabilityActive()
        {
            if (
                encounter == null ||
                encounter.ActiveInstance == null ||
                !encounter.ActiveInstance.TryGetWorldPart(
                    encounter.CurrentAttackPart,
                    out PrototypeMasterpieceWorldPart part) ||
                part == null
            )
            {
                return false;
            }

            return part.CapabilityActive;
        }

        private bool IsOccluded(
            Vector3 origin,
            Vector3 targetCenter)
        {
            Vector3 delta =
                targetCenter -
                origin;

            float distance =
                delta.magnitude;

            if (distance <= 0.001f)
            {
                return false;
            }

            occlusionQueryCount++;

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    delta / distance,
                    occlusionHits,
                    distance,
                    occlusionMask,
                    QueryTriggerInteraction.Ignore);

            float nearestBlockingDistance =
                float.PositiveInfinity;

            Transform masterpieceRoot =
                encounter != null &&
                encounter.ActiveInstance != null
                    ? encounter.ActiveInstance.transform
                    : null;

            Transform figureRoot =
                figureVolume != null
                    ? figureVolume.SourceRoot
                    : null;

            for (int index = 0;
                 index < hitCount;
                 index++)
            {
                RaycastHit hit =
                    occlusionHits[index];

                Collider collider =
                    hit.collider;

                if (collider == null)
                {
                    continue;
                }

                Transform hitTransform =
                    collider.transform;

                if (
                    masterpieceRoot != null &&
                    (
                        hitTransform ==
                            masterpieceRoot ||
                        hitTransform.IsChildOf(
                            masterpieceRoot)
                    )
                )
                {
                    continue;
                }

                if (
                    figureRoot != null &&
                    (
                        hitTransform ==
                            figureRoot ||
                        hitTransform.IsChildOf(
                            figureRoot)
                    )
                )
                {
                    continue;
                }

                nearestBlockingDistance =
                    Mathf.Min(
                        nearestBlockingDistance,
                        hit.distance);
            }

            return nearestBlockingDistance <
                distance -
                Mathf.Max(
                    0f,
                    occlusionSkin);
        }

        private void CompleteMiss(
            PrototypeMasterpieceHitValidationResult result,
            string reason)
        {
            lastResult = result;
            missCount++;
            lastReason = reason;
            sequenceActive = true;

            Debug.Log(
                "[M49.1 Hit Miss]\n" +
                $"SequenceId={sequenceId}\n" +
                $"Result={result}\n" +
                $"CapsuleDistance={lastCapsuleDistance:F3}\n" +
                $"CombinedRadius={lastCombinedRadius:F3}\n" +
                $"Occluded={lastOccluded}\n" +
                "GameplayHitAuthorityEnabled=False\n" +
                "ClarityWritesEnabled=False\n" +
                "DamageAuthorityEnabled=False",
                encounter != null
                    ? encounter.ActiveInstance
                    : this);
        }

        private void CompleteAborted(
            PrototypeMasterpieceHitValidationResult result,
            string reason)
        {
            if (
                strikeValidated &&
                lastResult !=
                    PrototypeMasterpieceHitValidationResult.Pending
            )
            {
                return;
            }

            strikeValidated = true;
            strikePending = false;
            sequenceActive = false;
            lastResult = result;
            abortedCount++;
            lastReason = reason;

            Debug.Log(
                "[M49.1 Hit Validation Aborted]\n" +
                $"SequenceId={sequenceId}\n" +
                $"Result={result}\n" +
                $"Reason={reason}\n" +
                "GameplayHitAuthorityEnabled=False",
                encounter != null
                    ? encounter
                    : this);
        }

        private static bool IsAbortState(
            PrototypeMasterpieceAutonomyState state)
        {
            return
                state ==
                    PrototypeMasterpieceAutonomyState.PossessedSuspended ||
                state ==
                    PrototypeMasterpieceAutonomyState.DisabledByUser ||
                state ==
                    PrototypeMasterpieceAutonomyState.CoreDisabled ||
                state ==
                    PrototypeMasterpieceAutonomyState.WaitingForWorld;
        }

        private static PrototypeMasterpieceHitValidationResult
            ResolveAbortResult(
                PrototypeMasterpieceAutonomyState state)
        {
            switch (state)
            {
                case PrototypeMasterpieceAutonomyState.PossessedSuspended:
                    return PrototypeMasterpieceHitValidationResult
                        .AbortedPossession;

                case PrototypeMasterpieceAutonomyState.WaitingForWorld:
                    return PrototypeMasterpieceHitValidationResult
                        .AbortedRecall;

                case PrototypeMasterpieceAutonomyState.CoreDisabled:
                    return PrototypeMasterpieceHitValidationResult
                        .AbortedCapability;

                default:
                    return PrototypeMasterpieceHitValidationResult
                        .AbortedStateChange;
            }
        }

        private static float SegmentSegmentSquaredDistance(
            Vector3 point1,
            Vector3 point2,
            Vector3 point3,
            Vector3 point4)
        {
            Vector3 direction1 =
                point2 -
                point1;

            Vector3 direction2 =
                point4 -
                point3;

            Vector3 relative =
                point1 -
                point3;

            float a =
                Vector3.Dot(
                    direction1,
                    direction1);

            float e =
                Vector3.Dot(
                    direction2,
                    direction2);

            float f =
                Vector3.Dot(
                    direction2,
                    relative);

            float s;
            float t;

            const float epsilon =
                0.000001f;

            if (
                a <= epsilon &&
                e <= epsilon
            )
            {
                return relative.sqrMagnitude;
            }

            if (a <= epsilon)
            {
                s = 0f;
                t =
                    Mathf.Clamp01(
                        f /
                        e);
            }
            else
            {
                float c =
                    Vector3.Dot(
                        direction1,
                        relative);

                if (e <= epsilon)
                {
                    t = 0f;
                    s =
                        Mathf.Clamp01(
                            -c /
                            a);
                }
                else
                {
                    float b =
                        Vector3.Dot(
                            direction1,
                            direction2);

                    float denominator =
                        a *
                        e -
                        b *
                        b;

                    if (
                        Mathf.Abs(
                            denominator) >
                        epsilon
                    )
                    {
                        s =
                            Mathf.Clamp01(
                                (
                                    b *
                                    f -
                                    c *
                                    e
                                ) /
                                denominator);
                    }
                    else
                    {
                        s = 0f;
                    }

                    float tNumerator =
                        b *
                        s +
                        f;

                    if (tNumerator < 0f)
                    {
                        t = 0f;
                        s =
                            Mathf.Clamp01(
                                -c /
                                a);
                    }
                    else if (tNumerator > e)
                    {
                        t = 1f;
                        s =
                            Mathf.Clamp01(
                                (
                                    b -
                                    c
                                ) /
                                a);
                    }
                    else
                    {
                        t =
                            tNumerator /
                            e;
                    }
                }
            }

            Vector3 closest1 =
                point1 +
                direction1 *
                s;

            Vector3 closest2 =
                point3 +
                direction2 *
                t;

            return (
                closest1 -
                closest2
            ).sqrMagnitude;
        }

        private void EnsureDebugResources()
        {
            if (debugMaterial == null)
            {
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
                    debugMaterial =
                        new Material(
                            shader)
                        {
                            name =
                                "M49_1_HitQueryDebug_Runtime",
                            hideFlags =
                                HideFlags.DontSave
                        };

                    if (debugMaterial.HasProperty(
                            "_BaseColor"))
                    {
                        debugMaterial.SetColor(
                            "_BaseColor",
                            Color.white);
                    }

                    if (debugMaterial.HasProperty(
                            "_Color"))
                    {
                        debugMaterial.SetColor(
                            "_Color",
                            Color.white);
                    }
                }
            }

            attackAxis =
                EnsureLine(
                    attackAxis,
                    "M49_1_AttackAxis",
                    loop: false,
                    0.035f);

            attackRingA =
                EnsureLine(
                    attackRingA,
                    "M49_1_AttackRingA",
                    loop: true,
                    0.032f);

            attackRingB =
                EnsureLine(
                    attackRingB,
                    "M49_1_AttackRingB",
                    loop: true,
                    0.032f);

            targetAxis =
                EnsureLine(
                    targetAxis,
                    "M49_1_TargetAxis",
                    loop: false,
                    0.035f);

            targetRingA =
                EnsureLine(
                    targetRingA,
                    "M49_1_TargetRingA",
                    loop: true,
                    0.032f);

            targetRingB =
                EnsureLine(
                    targetRingB,
                    "M49_1_TargetRingB",
                    loop: true,
                    0.032f);

            resultRing =
                EnsureLine(
                    resultRing,
                    "M49_1_ResultRing",
                    loop: true,
                    0.075f);

            HideQueryDebug();
            SetRendererVisible(
                resultRing,
                false);
        }

        private LineRenderer EnsureLine(
            LineRenderer existing,
            string objectName,
            bool loop,
            float width)
        {
            if (existing != null)
            {
                return existing;
            }

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
                debugMaterial;

            line.enabled = false;

            return line;
        }

        private void UpdateDebugVolumes()
        {
            if (
                !debugVolumesVisible ||
                encounter == null ||
                encounter.ActiveInstance == null ||
                figureVolume == null
            )
            {
                HideQueryDebug();
                return;
            }

            if (!TryBuildVolumes(
                    out Vector3 attackA,
                    out Vector3 attackB,
                    out float attackRadius,
                    out Vector3 targetA,
                    out Vector3 targetB,
                    out float targetRadius,
                    out _))
            {
                HideQueryDebug();
                return;
            }

            SetAxis(
                attackAxis,
                attackA,
                attackB,
                attackVolumeColor);

            SetCircle(
                attackRingA,
                attackA,
                attackB -
                    attackA,
                attackRadius,
                attackVolumeColor);

            SetCircle(
                attackRingB,
                attackB,
                attackB -
                    attackA,
                attackRadius,
                attackVolumeColor);

            SetAxis(
                targetAxis,
                targetA,
                targetB,
                targetVolumeColor);

            SetCircle(
                targetRingA,
                targetA,
                targetB -
                    targetA,
                targetRadius,
                targetVolumeColor);

            SetCircle(
                targetRingB,
                targetB,
                targetB -
                    targetA,
                targetRadius,
                targetVolumeColor);
        }

        private void ShowResultCue(
            Vector3 center,
            bool hit)
        {
            Color color =
                hit
                    ? hitResultColor
                    : missResultColor;

            float radius =
                Mathf.Max(
                    0.3f,
                    lastTargetRadius +
                    (
                        hit
                            ? 0.18f
                            : 0.34f
                    ));

            SetCircle(
                resultRing,
                center,
                Vector3.up,
                radius,
                color);

            resultVisibleUntil =
                Time.unscaledTime +
                0.55f;
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
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.enabled = true;
        }

        private static void SetCircle(
            LineRenderer line,
            Vector3 center,
            Vector3 axis,
            float radius,
            Color color)
        {
            if (line == null)
            {
                return;
            }

            Vector3 normal =
                axis.sqrMagnitude > 0.0001f
                    ? axis.normalized
                    : Vector3.up;

            Vector3 tangent =
                Vector3.Cross(
                    normal,
                    Mathf.Abs(
                        Vector3.Dot(
                            normal,
                            Vector3.up)) >
                        0.92f
                        ? Vector3.right
                        : Vector3.up);

            if (tangent.sqrMagnitude <=
                0.0001f)
            {
                tangent = Vector3.right;
            }
            else
            {
                tangent.Normalize();
            }

            Vector3 bitangent =
                Vector3.Cross(
                    normal,
                    tangent).normalized;

            line.loop = true;
            line.positionCount =
                DebugCircleSegments;

            line.startColor = color;
            line.endColor = color;

            for (int index = 0;
                 index < DebugCircleSegments;
                 index++)
            {
                float angle =
                    index /
                    (float)
                    DebugCircleSegments *
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

        private void HideQueryDebug()
        {
            SetRendererVisible(
                attackAxis,
                false);

            SetRendererVisible(
                attackRingA,
                false);

            SetRendererVisible(
                attackRingB,
                false);

            SetRendererVisible(
                targetAxis,
                false);

            SetRendererVisible(
                targetRingA,
                false);

            SetRendererVisible(
                targetRingB,
                false);
        }

        private static void SetRendererVisible(
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
            bool worldActive =
                deployment != null &&
                deployment.ActiveInstance != null;

            if (statusGroup != null)
            {
                statusGroup.alpha =
                    worldActive ? 1f : 0f;

                statusGroup.interactable = false;
                statusGroup.blocksRaycasts = false;
            }

            if (!worldActive)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    "BAŞ YAPIT • HIT DOĞRULAMA";
            }

            if (stateText != null)
            {
                stateText.text =
                    $"SONUÇ {TranslateResult(lastResult)}   " +
                    $"SEQ {sequenceId}   " +
                    $"DOĞRULAMA {validationCount}\n" +
                    $"HIT {hitCount}   MISS {missCount}   " +
                    $"ABORT {abortedCount}   " +
                    $"TEKRAR ENGEL {duplicateValidationPreventedCount}\n" +
                    $"MESAFE {FormatDistance(lastCapsuleDistance)}   " +
                    $"TOPLAM R {lastCombinedRadius:F2}   " +
                    $"OCC {(lastOccluded ? "EVET" : "HAYIR")}\n" +
                    lastReason;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    "H • QUERY HACİMLERİNİ GÖSTER/GİZLE\n" +
                    "M49.1: HIT/MISS VAR • NETLİK/HASAR/KNOCKBACK YOK";
            }
        }

        private static string TranslateResult(
            PrototypeMasterpieceHitValidationResult result)
        {
            switch (result)
            {
                case PrototypeMasterpieceHitValidationResult.Pending:
                    return "BEKLİYOR";

                case PrototypeMasterpieceHitValidationResult.HitConfirmed:
                    return "HIT";

                case PrototypeMasterpieceHitValidationResult.MissDodged:
                    return "MISS • KAÇTI";

                case PrototypeMasterpieceHitValidationResult.MissOutOfReach:
                    return "MISS • MENZİL";

                case PrototypeMasterpieceHitValidationResult.MissOccluded:
                    return "MISS • ENGEL";

                case PrototypeMasterpieceHitValidationResult.TargetUnavailable:
                    return "HEDEF YOK";

                case PrototypeMasterpieceHitValidationResult.AbortedPossession:
                    return "ABORT • SAHİPLENME";

                case PrototypeMasterpieceHitValidationResult.AbortedCapability:
                    return "ABORT • ORGAN";

                case PrototypeMasterpieceHitValidationResult.AbortedRecall:
                    return "ABORT • RECALL";

                case PrototypeMasterpieceHitValidationResult.AbortedStateChange:
                    return "ABORT • STATE";

                default:
                    return "YOK";
            }
        }

        private static string FormatDistance(
            float value)
        {
            return value < 0f
                ? "-"
                : value.ToString("F2");
        }

        private void OnDisable()
        {
            DisableInputActions();
            HideQueryDebug();
            SetRendererVisible(
                resultRing,
                false);
        }

        private void OnDestroy()
        {
            if (toggleDebugAction != null)
            {
                toggleDebugAction.performed -=
                    HandleToggleDebug;

                toggleDebugAction.Dispose();
                toggleDebugAction = null;
            }

            if (debugMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        debugMaterial);
                }
                else
                {
                    DestroyImmediate(
                        debugMaterial);
                }

                debugMaterial = null;
            }
        }
    }
}
