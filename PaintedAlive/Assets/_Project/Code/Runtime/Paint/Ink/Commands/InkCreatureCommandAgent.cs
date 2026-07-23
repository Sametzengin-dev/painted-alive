using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Commands
{
    public enum InkCreatureCommandRole
    {
        Runner,
        Bulwark,
        Ambusher
    }

    public enum InkCreatureCommandState
    {
        Autonomous,
        Moving,
        Holding,
        Countered
    }

    [DefaultExecutionOrder(11000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkCreatureRuntime))]
    public sealed class InkCreatureCommandAgent : MonoBehaviour
    {
        private const int MaximumGroundHits = 12;
        private const int MaximumObstacleHits = 8;

        private readonly RaycastHit[] groundHits =
            new RaycastHit[MaximumGroundHits];
        private readonly RaycastHit[] obstacleHits =
            new RaycastHit[MaximumObstacleHits];

        [SerializeField]
        private InkCreatureRuntime creature;

        [Header("Movement")]
        [SerializeField, Min(0.5f)]
        private float runnerSpeed = 3.25f;

        [SerializeField, Min(0.5f)]
        private float bulwarkSpeed = 2.15f;

        [SerializeField, Min(0.5f)]
        private float ambusherSpeed = 3.55f;

        [SerializeField, Min(30f)]
        private float turnSpeed = 420f;

        [SerializeField, Min(0.05f)]
        private float arrivalRadius = 0.55f;

        [SerializeField, Min(0.5f)]
        private float groundProbeHeight = 1.8f;

        [SerializeField, Min(0.1f)]
        private float groundProbeDistance = 3.5f;

        [SerializeField, Min(0.05f)]
        private float surfaceOffset = 0.08f;

        [SerializeField, Min(0.05f)]
        private float obstacleProbeRadius = 0.22f;

        [SerializeField, Min(0.1f)]
        private float obstacleProbeDistance = 0.75f;

        [SerializeField]
        private LayerMask navigationMask = Physics.DefaultRaycastLayers;

        [Header("Role Behaviour")]
        [SerializeField, Min(1f)]
        private float bulwarkTriggerRadius = 6.5f;

        [SerializeField, Min(1f)]
        private float ambusherTriggerRadius = 5.2f;

        [SerializeField, Min(2f)]
        private float maximumHoldDuration = 18f;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private InkCreatureCommandRole commandRole;

        [SerializeField]
        private InkCreatureCommandState commandState =
            InkCreatureCommandState.Autonomous;

        [SerializeField]
        private Vector3 commandPoint;

        [SerializeField]
        private Vector3 commandNormal = Vector3.up;

        [SerializeField]
        private int commandSequence;

        [SerializeField]
        private string lastResult = "Autonomous";

        private InkCreatureCommandDirector director;
        private bool commandActive;
        private bool creatureWasEnabled;
        private bool hasCapturedRuntimeState;
        private float holdStartedAt;

        public bool IsCommanded => commandActive;
        public InkCreatureCommandRole CommandRole => commandRole;
        public InkCreatureCommandState CommandState => commandState;
        public Vector3 CommandPoint => commandPoint;
        public int CommandSequence => commandSequence;
        public string LastResult => lastResult;

        private void Awake()
        {
            if (creature == null)
            {
                creature = GetComponent<InkCreatureRuntime>();
            }
        }

        private void OnDisable()
        {
            RestoreCreatureRuntime();
        }

        private void OnDestroy()
        {
            RestoreCreatureRuntime();
        }

        private void Update()
        {
            if (!commandActive || creature == null)
            {
                return;
            }

            if (!creature.IsInitialized ||
                !creature.HasGlyph(InkGlyphType.Eye) ||
                !creature.HasGlyph(InkGlyphType.Foot))
            {
                CancelCommand("Critical glyph lost");
                return;
            }

            if (creature.IsFixed || creature.IsPinned)
            {
                commandState = InkCreatureCommandState.Countered;
                lastResult = creature.IsFixed
                    ? "Held by fixative"
                    : "Held by frame anchor";
                SetCreatureSimulation(true);
                return;
            }

            if (commandState == InkCreatureCommandState.Countered)
            {
                commandState = InkCreatureCommandState.Moving;
            }

            SetCreatureSimulation(false);

            if (commandState == InkCreatureCommandState.Moving)
            {
                MoveTowardsCommand(Time.deltaTime);
                return;
            }

            if (commandState == InkCreatureCommandState.Holding)
            {
                UpdateHold();
            }
        }

        public bool AssignCommand(
            InkCreatureCommandDirector source,
            Vector3 point,
            Vector3 normal,
            int sequence)
        {
            if (creature == null)
            {
                creature = GetComponent<InkCreatureRuntime>();
            }

            if (creature == null ||
                !creature.IsInitialized ||
                !creature.HasGlyph(InkGlyphType.Eye) ||
                !creature.HasGlyph(InkGlyphType.Foot))
            {
                return false;
            }

            if (!commandActive)
            {
                creatureWasEnabled = creature.enabled;
                hasCapturedRuntimeState = true;
            }

            director = source;
            commandPoint = point;
            commandNormal = normal.sqrMagnitude > 0.001f
                ? normal.normalized
                : Vector3.up;
            commandSequence = sequence;
            commandRole = ResolveRole();
            commandState = creature.IsFixed || creature.IsPinned
                ? InkCreatureCommandState.Countered
                : InkCreatureCommandState.Moving;
            commandActive = true;
            holdStartedAt = 0f;
            lastResult = commandRole + " moving";

            if (commandState == InkCreatureCommandState.Moving)
            {
                SetCreatureSimulation(false);
            }

            return true;
        }

        public void CancelCommand(string reason)
        {
            if (!commandActive)
            {
                return;
            }

            commandActive = false;
            commandState = InkCreatureCommandState.Autonomous;
            lastResult = string.IsNullOrWhiteSpace(reason)
                ? "Autonomous"
                : reason;
            RestoreCreatureRuntime();
            director = null;
        }

        private InkCreatureCommandRole ResolveRole()
        {
            if (creature != null &&
                creature.HasGlyph(InkGlyphType.Shell))
            {
                return InkCreatureCommandRole.Bulwark;
            }

            if (creature != null &&
                creature.HasGlyph(InkGlyphType.BrokenLine))
            {
                return InkCreatureCommandRole.Ambusher;
            }

            return InkCreatureCommandRole.Runner;
        }

        private void MoveTowardsCommand(float deltaTime)
        {
            Vector3 toTarget = Vector3.ProjectOnPlane(
                commandPoint - transform.position,
                commandNormal);
            float distance = toTarget.magnitude;

            if (distance <= arrivalRadius)
            {
                ArriveAtCommand();
                return;
            }

            Vector3 direction = distance > 0.001f
                ? toTarget / distance
                : transform.forward;
            direction = AvoidObstacles(direction);
            float step = ResolveSpeed() * Mathf.Max(0f, deltaTime);
            Vector3 requested =
                transform.position + direction * Mathf.Min(step, distance);

            if (!TryFindGround(
                    requested,
                    out Vector3 groundedPoint,
                    out Vector3 groundedNormal))
            {
                lastResult = "No walkable command route";
                return;
            }

            Vector3 surfaceDirection = Vector3.ProjectOnPlane(
                direction,
                groundedNormal).normalized;

            if (surfaceDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(
                    surfaceDirection,
                    groundedNormal);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * deltaTime);
            }

            transform.position =
                groundedPoint + groundedNormal * surfaceOffset;
            lastResult = commandRole + " moving";
        }

        private void ArriveAtCommand()
        {
            if (commandRole == InkCreatureCommandRole.Runner)
            {
                CancelCommand("Runner reached order");
                return;
            }

            commandState = InkCreatureCommandState.Holding;
            holdStartedAt = Time.time;
            lastResult = commandRole == InkCreatureCommandRole.Bulwark
                ? "Bulwark guarding"
                : "Ambusher waiting";
        }

        private void UpdateHold()
        {
            float triggerRadius =
                commandRole == InkCreatureCommandRole.Bulwark
                    ? bulwarkTriggerRadius
                    : ambusherTriggerRadius;
            FigureMotor threat = director != null
                ? director.FindNearestFigure(
                    transform.position,
                    triggerRadius)
                : null;

            if (threat != null)
            {
                CancelCommand(
                    commandRole == InkCreatureCommandRole.Bulwark
                        ? "Bulwark intercepted Figure"
                        : "Ambusher sprung");
                return;
            }

            if (Time.time - holdStartedAt >= maximumHoldDuration)
            {
                CancelCommand("Hold expired");
            }
        }

        private float ResolveSpeed()
        {
            switch (commandRole)
            {
                case InkCreatureCommandRole.Bulwark:
                    return bulwarkSpeed;

                case InkCreatureCommandRole.Ambusher:
                    return ambusherSpeed;

                default:
                    return runnerSpeed;
            }
        }

        private Vector3 AvoidObstacles(Vector3 direction)
        {
            Vector3 origin = transform.position + Vector3.up * 0.35f;
            int count = Physics.SphereCastNonAlloc(
                origin,
                obstacleProbeRadius,
                direction,
                obstacleHits,
                obstacleProbeDistance,
                navigationMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = obstacleHits[i];
                Collider hitCollider = hit.collider;

                if (hitCollider == null ||
                    hitCollider.transform.IsChildOf(transform) ||
                    hitCollider.GetComponentInParent<FigureMotor>() != null)
                {
                    continue;
                }

                Vector3 tangent = Vector3.Cross(
                    hit.normal,
                    Vector3.up).normalized;

                if ((GetInstanceID() & 1) == 0)
                {
                    tangent = -tangent;
                }

                return Vector3.Slerp(
                    direction,
                    tangent,
                    0.78f).normalized;
            }

            return direction;
        }

        private bool TryFindGround(
            Vector3 requested,
            out Vector3 point,
            out Vector3 normal)
        {
            Vector3 origin = requested + Vector3.up * groundProbeHeight;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                groundProbeHeight + groundProbeDistance,
                navigationMask,
                QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            RaycastHit best = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.distance >= nearest ||
                    Vector3.Angle(hit.normal, Vector3.up) > 62f)
                {
                    continue;
                }

                nearest = hit.distance;
                best = hit;
            }

            if (best.collider == null)
            {
                point = transform.position;
                normal = Vector3.up;
                return false;
            }

            point = best.point;
            normal = best.normal.normalized;
            return true;
        }

        private void SetCreatureSimulation(bool active)
        {
            if (creature != null && creature.enabled != active)
            {
                creature.enabled = active;
            }
        }

        private void RestoreCreatureRuntime()
        {
            if (hasCapturedRuntimeState &&
                creature != null &&
                creature.enabled != creatureWasEnabled)
            {
                creature.enabled = creatureWasEnabled;
            }

            hasCapturedRuntimeState = false;
        }
    }
}
