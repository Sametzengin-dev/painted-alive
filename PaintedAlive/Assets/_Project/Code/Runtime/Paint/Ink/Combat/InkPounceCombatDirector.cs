using System.Collections.Generic;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.Possession;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Combat
{
    public enum InkPounceState
    {
        Ready,
        Windup,
        Pouncing,
        HitRecovery,
        MissVulnerable
    }

    [DefaultExecutionOrder(15000)]
    [DisallowMultipleComponent]
    public sealed class InkPounceCombatDirector : MonoBehaviour
    {
        private const int MaximumPhysicsHits = 24;
        private const string ActiveLockMarkerName =
            "FrameGunAnchor_Runtime_M18AttackLock";
        private const string InactiveLockMarkerName =
            "M18AttackLock_Inactive";

        private sealed class AgentRecord
        {
            public InkCreatureRuntime Creature;
            public InkPounceState State;
            public FigureMotor Target;
            public FigureMotor ExcludedFigure;
            public InkPossessionController PossessionController;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public Vector3 BaseScale;
            public float PhaseStartedAt;
            public float PhaseEndsAt;
            public float NextAttackTime;
            public bool PlayerDriven;
            public bool RestoreEnabled;
            public GameObject LockMarker;
        }

        private static InkPounceCombatDirector activeInstance;

        private readonly List<AgentRecord> agents = new();
        private readonly RaycastHit[] movementHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] groundHits =
            new RaycastHit[MaximumPhysicsHits];

        [SerializeField]
        private InkPounceAttackConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int trackedCreatures;

        [SerializeField]
        private int activeAttacks;

        [SerializeField]
        private int totalAttacks;

        [SerializeField]
        private int totalHits;

        [SerializeField]
        private int totalMisses;

        [SerializeField]
        private string lastAttackResult = "None";

        private float nextCreatureSyncTime;

        public static InkPounceCombatDirector ActiveInstance =>
            activeInstance;
        public InkPounceAttackConfig Config => config;
        public int TrackedCreatures => trackedCreatures;
        public int ActiveAttacks => activeAttacks;
        public int TotalAttacks => totalAttacks;
        public int TotalHits => totalHits;
        public int TotalMisses => totalMisses;
        public string LastAttackResult => lastAttackResult;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeInstance = null;
        }

        private void Awake()
        {
            if (activeInstance != null && activeInstance != this)
            {
                Debug.LogError(
                    "Duplicate InkPounceCombatDirector disabled. Run M18 " +
                    "Diagnose and keep only one director.",
                    this);
                enabled = false;
                return;
            }

            activeInstance = this;

            if (config == null)
            {
                Debug.LogError(
                    "InkPounceCombatDirector requires an " +
                    "InkPounceAttackConfig.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (activeInstance == null || activeInstance == this)
            {
                activeInstance = this;
            }
        }

        private void OnDisable()
        {
            ReleaseAllAgents();

            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        private void Update()
        {
            if (config == null)
            {
                return;
            }

            float now = Time.time;

            if (now >= nextCreatureSyncTime)
            {
                SyncCreatures();
                nextCreatureSyncTime = now + 0.25f;
            }

            activeAttacks = 0;

            for (int i = agents.Count - 1; i >= 0; i--)
            {
                AgentRecord agent = agents[i];

                if (agent.Creature == null)
                {
                    agents.RemoveAt(i);
                    continue;
                }

                if (agent.State == InkPounceState.Ready)
                {
                    TryStartAiAttack(agent, now);
                    continue;
                }

                activeAttacks++;
                KeepCreatureMotionLocked(agent);
                UpdateAttack(agent, now);
            }

            trackedCreatures = agents.Count;
        }

        public bool TryBeginPlayerPounce(
            InkCreatureRuntime creature,
            Vector3 direction,
            FigureMotor excludedFigure,
            InkPossessionController possessionController)
        {
            if (!isActiveAndEnabled ||
                creature == null ||
                direction.sqrMagnitude < 0.001f)
            {
                return false;
            }

            AgentRecord agent = GetOrCreateAgent(creature);

            if (!CanAttack(agent, Time.time))
            {
                return false;
            }

            direction = Vector3.ProjectOnPlane(
                direction,
                Vector3.up).normalized;

            if (direction.sqrMagnitude < 0.001f)
            {
                return false;
            }

            BeginAttack(
                agent,
                direction,
                null,
                excludedFigure,
                possessionController,
                true,
                Time.time);
            return true;
        }

        public bool IsCreatureBusy(InkCreatureRuntime creature)
        {
            AgentRecord agent = FindAgent(creature);
            return agent != null && agent.State != InkPounceState.Ready;
        }

        public InkPounceState GetCreatureState(
            InkCreatureRuntime creature)
        {
            AgentRecord agent = FindAgent(creature);
            return agent != null
                ? agent.State
                : InkPounceState.Ready;
        }

        public string BuildRuntimeSummary()
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder(256);

            for (int i = 0; i < agents.Count; i++)
            {
                AgentRecord agent = agents[i];

                if (agent.Creature == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(agent.Creature.name)
                    .Append(':')
                    .Append(agent.State)
                    .Append(agent.PlayerDriven ? "(Player)" : "(AI)");
            }

            return builder.Length > 0 ? builder.ToString() : "No creatures";
        }

        private void SyncCreatures()
        {
            InkSystemManager manager = InkSystemManager.ActiveInstance;

            if (manager == null || manager.ActiveCreatures == null)
            {
                return;
            }

            for (int i = 0; i < manager.ActiveCreatures.Count; i++)
            {
                InkCreatureRuntime creature = manager.ActiveCreatures[i];

                if (creature != null && FindAgent(creature) == null)
                {
                    agents.Add(new AgentRecord
                    {
                        Creature = creature,
                        State = InkPounceState.Ready,
                        BaseScale = creature.transform.localScale
                    });
                }
            }
        }

        private AgentRecord GetOrCreateAgent(InkCreatureRuntime creature)
        {
            AgentRecord agent = FindAgent(creature);

            if (agent != null)
            {
                return agent;
            }

            agent = new AgentRecord
            {
                Creature = creature,
                State = InkPounceState.Ready,
                BaseScale = creature.transform.localScale
            };
            agents.Add(agent);
            trackedCreatures = agents.Count;
            return agent;
        }

        private AgentRecord FindAgent(InkCreatureRuntime creature)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].Creature == creature)
                {
                    return agents[i];
                }
            }

            return null;
        }

        private void TryStartAiAttack(AgentRecord agent, float now)
        {
            InkCreatureRuntime creature = agent.Creature;

            if (!CanAttack(agent, now) ||
                !creature.isActiveAndEnabled ||
                creature.CurrentTarget == null)
            {
                return;
            }

            Vector3 offset =
                creature.CurrentTarget.transform.position -
                creature.transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude >
                config.AiAttackRange * config.AiAttackRange ||
                offset.sqrMagnitude < 0.01f)
            {
                return;
            }

            BeginAttack(
                agent,
                offset.normalized,
                creature.CurrentTarget,
                null,
                null,
                false,
                now);
        }

        private bool CanAttack(AgentRecord agent, float now)
        {
            InkCreatureRuntime creature = agent != null
                ? agent.Creature
                : null;

            return creature != null &&
                   agent.State == InkPounceState.Ready &&
                   now >= agent.NextAttackTime &&
                   creature.IsInitialized &&
                   creature.HasGlyph(InkGlyphType.Eye) &&
                   creature.HasGlyph(InkGlyphType.Foot) &&
                   !creature.IsFixed &&
                   !creature.IsPinned &&
                   !ContainsRealFrameAnchor(creature.transform);
        }

        private void BeginAttack(
            AgentRecord agent,
            Vector3 direction,
            FigureMotor target,
            FigureMotor excludedFigure,
            InkPossessionController possessionController,
            bool playerDriven,
            float now)
        {
            InkCreatureRuntime creature = agent.Creature;
            agent.State = InkPounceState.Windup;
            agent.Target = target;
            agent.ExcludedFigure = excludedFigure;
            agent.PossessionController = possessionController;
            agent.PlayerDriven = playerDriven;
            agent.RestoreEnabled = creature.enabled;
            agent.StartPosition = creature.transform.position;
            agent.BaseScale = creature.transform.localScale;
            agent.PhaseStartedAt = now;
            agent.PhaseEndsAt = now + config.WindupDuration;

            direction = ApplyWaterAimError(creature, direction);
            float distance = config.PounceDistance;

            if (!playerDriven && target != null)
            {
                float targetDistance = Vector3.Distance(
                    creature.transform.position,
                    target.transform.position);
                distance = Mathf.Min(
                    config.PounceDistance,
                    targetDistance + config.HitRadius * 0.35f);
            }

            agent.EndPosition = agent.StartPosition + direction * distance;
            EnsureAttackLock(agent);
            creature.enabled = false;
            totalAttacks++;
            lastAttackResult = playerDriven
                ? "Player windup"
                : "AI windup";
        }

        private void UpdateAttack(AgentRecord agent, float now)
        {
            InkCreatureRuntime creature = agent.Creature;

            bool canBeCounteredNow =
                agent.State == InkPounceState.Windup ||
                agent.State == InkPounceState.Pouncing;

            if (canBeCounteredNow &&
                (!creature.HasGlyph(InkGlyphType.Foot) ||
                 creature.IsFixed ||
                 ContainsRealFrameAnchor(creature.transform)))
            {
                CompleteMiss(agent, now, "Countered during attack");
                return;
            }

            switch (agent.State)
            {
                case InkPounceState.Windup:
                    UpdateWindup(agent, now);
                    break;

                case InkPounceState.Pouncing:
                    UpdatePounce(agent, now);
                    break;

                case InkPounceState.HitRecovery:
                case InkPounceState.MissVulnerable:
                    UpdateRecovery(agent, now);
                    break;
            }
        }

        private void UpdateWindup(AgentRecord agent, float now)
        {
            float progress = Mathf.InverseLerp(
                agent.PhaseStartedAt,
                agent.PhaseEndsAt,
                now);
            float pulse = Mathf.SmoothStep(0f, 1f, progress);
            agent.Creature.transform.localScale = new Vector3(
                agent.BaseScale.x * Mathf.Lerp(
                    1f,
                    config.WindupScaleXZ,
                    pulse),
                agent.BaseScale.y * Mathf.Lerp(
                    1f,
                    config.WindupScaleY,
                    pulse),
                agent.BaseScale.z * Mathf.Lerp(
                    1f,
                    config.WindupScaleXZ,
                    pulse));

            Vector3 direction = agent.EndPosition - agent.StartPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                agent.Creature.transform.rotation =
                    Quaternion.RotateTowards(
                        agent.Creature.transform.rotation,
                        Quaternion.LookRotation(direction.normalized),
                        720f * Time.deltaTime);
            }

            if (now < agent.PhaseEndsAt)
            {
                return;
            }

            agent.State = InkPounceState.Pouncing;
            agent.PhaseStartedAt = now;
            agent.PhaseEndsAt = now + config.PounceDuration;
            agent.StartPosition = agent.Creature.transform.position;
            agent.Creature.transform.localScale = agent.BaseScale;
        }

        private void UpdatePounce(AgentRecord agent, float now)
        {
            float progress = Mathf.Clamp01(Mathf.InverseLerp(
                agent.PhaseStartedAt,
                agent.PhaseEndsAt,
                now));
            Vector3 previous = agent.Creature.transform.position;
            Vector3 next = Vector3.Lerp(
                agent.StartPosition,
                agent.EndPosition,
                progress);
            next.y += Mathf.Sin(progress * Mathf.PI) *
                config.PounceArcHeight;

            if (TryResolveTravel(
                    agent,
                    previous,
                    next,
                    out FigureMotor hitFigure,
                    out bool blocked))
            {
                agent.Creature.transform.position = next;
                ApplyHit(agent, hitFigure, now);
                return;
            }

            if (blocked)
            {
                CompleteMiss(agent, now, "Pounce blocked");
                return;
            }

            agent.Creature.transform.position = next;

            if (progress < 1f)
            {
                return;
            }

            SnapToGround(agent.Creature);
            CompleteMiss(agent, now, "Pounce missed");
        }

        private bool TryResolveTravel(
            AgentRecord agent,
            Vector3 previous,
            Vector3 next,
            out FigureMotor hitFigure,
            out bool blocked)
        {
            hitFigure = null;
            blocked = false;
            Vector3 delta = next - previous;
            float distance = delta.magnitude;

            if (distance <= 0.0001f)
            {
                return false;
            }

            int count = Physics.SphereCastNonAlloc(
                previous + Vector3.up * 0.18f,
                config.HitRadius,
                delta / distance,
                movementHits,
                distance + 0.04f,
                config.CollisionMask,
                QueryTriggerInteraction.Collide);
            float nearestFigureDistance = float.PositiveInfinity;
            float nearestObstacleDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = movementHits[i];
                Collider collider = hit.collider;

                if (collider == null ||
                    IsOwnCollider(agent.Creature, collider))
                {
                    continue;
                }

                FigureMotor figure =
                    collider.GetComponentInParent<FigureMotor>();

                if (figure != null)
                {
                    if (figure == agent.ExcludedFigure ||
                        !figure.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (hit.distance < nearestFigureDistance)
                    {
                        nearestFigureDistance = hit.distance;
                        hitFigure = figure;
                    }

                    continue;
                }

                if (Vector3.Dot(hit.normal, Vector3.up) > 0.55f)
                {
                    continue;
                }

                nearestObstacleDistance = Mathf.Min(
                    nearestObstacleDistance,
                    hit.distance);
            }

            if (hitFigure != null &&
                nearestFigureDistance <= nearestObstacleDistance)
            {
                return true;
            }

            hitFigure = null;
            blocked = nearestObstacleDistance < float.PositiveInfinity;
            return false;
        }

        private void ApplyHit(
            AgentRecord agent,
            FigureMotor figure,
            float now)
        {
            if (figure == null)
            {
                CompleteMiss(agent, now, "Invalid figure hit");
                return;
            }

            FigureInkFootStainStatus stain =
                figure.GetComponent<FigureInkFootStainStatus>();

            if (stain == null)
            {
                stain = figure.gameObject.AddComponent<
                    FigureInkFootStainStatus>();
            }

            stain.ApplyStain(config);
            totalHits++;
            lastAttackResult =
                $"Hit {figure.name}; stacks={stain.StackCount}";
            BeginRecovery(
                agent,
                InkPounceState.HitRecovery,
                config.HitRecoveryDuration,
                now);
        }

        private void CompleteMiss(
            AgentRecord agent,
            float now,
            string reason)
        {
            if (agent.State == InkPounceState.MissVulnerable)
            {
                return;
            }

            SnapToGround(agent.Creature);
            totalMisses++;
            lastAttackResult = reason;
            BeginRecovery(
                agent,
                InkPounceState.MissVulnerable,
                config.MissVulnerabilityDuration,
                now);
        }

        private void BeginRecovery(
            AgentRecord agent,
            InkPounceState state,
            float duration,
            float now)
        {
            agent.State = state;
            agent.PhaseStartedAt = now;
            agent.PhaseEndsAt = now + duration;
        }

        private void UpdateRecovery(AgentRecord agent, float now)
        {
            if (agent.State == InkPounceState.MissVulnerable)
            {
                float progress = Mathf.InverseLerp(
                    agent.PhaseStartedAt,
                    agent.PhaseEndsAt,
                    now);
                float flatten = Mathf.Sin(progress * Mathf.PI);
                agent.Creature.transform.localScale = new Vector3(
                    agent.BaseScale.x * Mathf.Lerp(1f, 1.25f, flatten),
                    agent.BaseScale.y * Mathf.Lerp(1f, 0.55f, flatten),
                    agent.BaseScale.z * Mathf.Lerp(1f, 1.25f, flatten));
            }

            if (now < agent.PhaseEndsAt)
            {
                return;
            }

            FinishRecovery(agent, now);
        }

        private void FinishRecovery(AgentRecord agent, float now)
        {
            InkCreatureRuntime creature = agent.Creature;
            creature.transform.localScale = agent.BaseScale;
            ReleaseAttackLock(agent);
            agent.State = InkPounceState.Ready;
            agent.Target = null;
            agent.ExcludedFigure = null;
            agent.NextAttackTime = now + config.AttackCooldown;

            bool stillPossessed =
                agent.PossessionController != null &&
                agent.PossessionController.IsPossessing &&
                agent.PossessionController.PossessedCreature == creature;
            creature.enabled = stillPossessed
                ? false
                : agent.RestoreEnabled || agent.PlayerDriven;
            agent.PossessionController = null;
            agent.PlayerDriven = false;
        }

        private void KeepCreatureMotionLocked(AgentRecord agent)
        {
            if (agent.Creature != null && agent.Creature.enabled)
            {
                agent.Creature.enabled = false;
            }

            if (agent.LockMarker != null)
            {
                agent.LockMarker.name = ActiveLockMarkerName;
            }
        }

        private void EnsureAttackLock(AgentRecord agent)
        {
            if (agent.LockMarker == null)
            {
                agent.LockMarker = new GameObject(ActiveLockMarkerName);
                agent.LockMarker.transform.SetParent(
                    agent.Creature.transform,
                    false);
            }

            agent.LockMarker.name = ActiveLockMarkerName;
        }

        private static void ReleaseAttackLock(AgentRecord agent)
        {
            if (agent.LockMarker != null)
            {
                agent.LockMarker.name = InactiveLockMarkerName;
            }
        }

        private void SnapToGround(InkCreatureRuntime creature)
        {
            Vector3 origin = creature.transform.position +
                Vector3.up * config.GroundProbeHeight;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                config.GroundProbeHeight + config.GroundProbeDistance,
                config.GroundMask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            RaycastHit bestHit = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null ||
                    IsOwnCollider(creature, hit.collider) ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            if (bestHit.collider != null)
            {
                creature.transform.position = bestHit.point +
                    bestHit.normal * config.SurfaceOffset;
            }
        }

        private Vector3 ApplyWaterAimError(
            InkCreatureRuntime creature,
            Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up)
                .normalized;
            float exposure = creature != null
                ? creature.WaterExposure
                : 0f;

            if (exposure <= 0.001f)
            {
                return direction;
            }

            float sign = Mathf.Sin(
                Time.time * 3.17f + creature.GetInstanceID() * 0.071f);
            float angle = sign * config.MaximumWaterAimError * exposure;
            return Quaternion.AngleAxis(angle, Vector3.up) * direction;
        }

        private static bool IsOwnCollider(
            InkCreatureRuntime creature,
            Collider collider)
        {
            return creature != null &&
                   collider != null &&
                   (collider.transform == creature.transform ||
                    collider.transform.IsChildOf(creature.transform));
        }

        private static bool ContainsRealFrameAnchor(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name.StartsWith(
                        "FrameGunAnchor_Runtime",
                        System.StringComparison.Ordinal) &&
                    child.name != ActiveLockMarkerName)
                {
                    return true;
                }

                if (ContainsRealFrameAnchor(child))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReleaseAllAgents()
        {
            for (int i = 0; i < agents.Count; i++)
            {
                AgentRecord agent = agents[i];

                if (agent.Creature != null &&
                    agent.State != InkPounceState.Ready)
                {
                    agent.Creature.transform.localScale = agent.BaseScale;
                    agent.Creature.enabled = agent.RestoreEnabled;
                }

                ReleaseAttackLock(agent);
            }

            activeAttacks = 0;
        }
    }
}
