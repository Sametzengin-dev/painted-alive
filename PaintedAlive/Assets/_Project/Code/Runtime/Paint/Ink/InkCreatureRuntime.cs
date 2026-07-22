using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    public enum InkCreatureState
    {
        Dormant,
        Patrol,
        Pursuit,
        ContactRecovery,
        SurfaceLost,
        Blinded,
        Crippled,
        Fixed,
        Pinned
    }

    [DisallowMultipleComponent]
    public sealed class InkCreatureRuntime : MonoBehaviour
    {
        private const int MaximumPhysicsHits = 16;
        private const float AnchorScanInterval = 0.1f;
        private const string FrameAnchorMarkerName =
            "FrameGunAnchor_Runtime";

        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        private readonly List<InkGlyphModule> modules = new();
        private readonly RaycastHit[] lineOfSightHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] obstacleHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] groundHits =
            new RaycastHit[MaximumPhysicsHits];

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        private Renderer bodyRenderer;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private InkCreatureDefinition definition;

        [SerializeField]
        private FigureMotor currentTarget;

        [SerializeField]
        private InkCreatureState currentState = InkCreatureState.Dormant;

        [SerializeField]
        private float currentSpeed;

        [SerializeField]
        private float currentDurability;

        [SerializeField, Range(0f, 1f)]
        private float waterExposure;

        [SerializeField]
        private bool surfaceValid;

        [SerializeField]
        private bool initialized;

        [SerializeField]
        private float fixedUntil;

        [SerializeField]
        private bool pinnedByFrameGun;

        [SerializeField]
        private int activeGlyphCount;

        private InkSystemManager owner;
        private InkSystemConfig config;
        private Vector3 homePosition;
        private Vector3 baseVisualScale = Vector3.one;
        private Vector3 watercolorDirection;
        private float movementSpeed;
        private float turnSpeedDegrees;
        private float detectionRange;
        private float targetRefreshInterval = 0.25f;
        private bool requiresLineOfSight;
        private bool hasEye;
        private bool hasFoot;
        private float nextTargetRefreshTime;
        private float nextContactTime;
        private float nextAnchorScanTime;
        private float patrolPhase;
        private Color fixedInkColor =
            new Color(0.36f, 0.46f, 0.54f, 1f);
        private InkGlyphHitZone[] glyphHitZones;
        private Renderer[] allRenderers;
        private MaterialPropertyBlock counterplayPropertyBlock;
        private int lastVisualMode = -1;

        public InkCreatureDefinition Definition => definition;
        public FigureMotor CurrentTarget => currentTarget;
        public InkCreatureState CurrentState => currentState;
        public float CurrentSpeed => currentSpeed;
        public float CurrentDurability => currentDurability;
        public float WaterExposure => waterExposure;
        public bool SurfaceValid => surfaceValid;
        public bool IsInitialized => initialized;
        public bool IsFixed => initialized && Time.time < fixedUntil;
        public bool IsPinned => pinnedByFrameGun;
        public int ActiveGlyphCount => activeGlyphCount;
        public IReadOnlyList<InkGlyphModule> Modules => modules;
        public Bounds WorldBounds => bodyRenderer != null
            ? bodyRenderer.bounds
            : new Bounds(transform.position, Vector3.one);

        private void Awake()
        {
            visualRoot ??= transform;
            bodyRenderer ??= GetComponentInChildren<Renderer>();
            CacheVisualComponents();
        }

        private void OnDestroy()
        {
            if (owner != null)
            {
                owner.UnregisterCreature(this);
            }
        }

        public bool Initialize(
            InkSystemManager systemOwner,
            InkSystemConfig systemConfig,
            InkCreatureDefinition creatureDefinition,
            Vector3 home)
        {
            owner = systemOwner;
            config = systemConfig;
            definition = creatureDefinition;
            homePosition = home;
            patrolPhase = Mathf.Abs(GetInstanceID() * 0.173f) %
                (Mathf.PI * 2f);
            visualRoot ??= transform;
            bodyRenderer ??= GetComponentInChildren<Renderer>();
            baseVisualScale = visualRoot.localScale;
            modules.Clear();

            if (config == null || definition == null)
            {
                initialized = false;
                return false;
            }

            movementSpeed = 0f;
            turnSpeedDegrees = 0f;
            detectionRange = 0f;
            targetRefreshInterval = 0.25f;
            requiresLineOfSight = false;
            currentDurability = definition.BaseDurability;
            currentTarget = null;
            waterExposure = 0f;
            fixedUntil = 0f;
            pinnedByFrameGun = false;

            IReadOnlyList<InkGlyphDefinition> glyphs = definition.Glyphs;

            if (glyphs != null)
            {
                for (int i = 0; i < glyphs.Count; i++)
                {
                    InkGlyphDefinition glyph = glyphs[i];

                    if (glyph == null)
                    {
                        continue;
                    }

                    modules.Add(new InkGlyphModule(glyph));
                    currentDurability += glyph.DurabilityModifier;

                    switch (glyph.GlyphType)
                    {
                        case InkGlyphType.Eye:
                            detectionRange = Mathf.Max(
                                detectionRange,
                                glyph.DetectionRange);
                            targetRefreshInterval = Mathf.Min(
                                targetRefreshInterval,
                                glyph.TargetRefreshInterval);
                            requiresLineOfSight |=
                                glyph.RequiresLineOfSight;
                            break;

                        case InkGlyphType.Foot:
                            movementSpeed += glyph.MovementSpeed;
                            turnSpeedDegrees = Mathf.Max(
                                turnSpeedDegrees,
                                glyph.TurnSpeedDegrees);
                            break;
                    }
                }
            }

            currentDurability = Mathf.Max(1f, currentDurability);
            initialized = true;
            surfaceValid = true;
            CacheVisualComponents();
            RefreshCapabilitiesFromModules();
            currentState = hasFoot
                ? InkCreatureState.Patrol
                : InkCreatureState.Dormant;
            RefreshGlyphHitZones();
            ApplyVisualState();
            return true;
        }

        public void Simulate(
            float deltaTime,
            float now,
            IReadOnlyList<FigureMotor> figures,
            LayerMask navigationMask,
            LayerMask visibilityMask)
        {
            if (!initialized || config == null || deltaTime <= 0f)
            {
                return;
            }

            waterExposure = Mathf.MoveTowards(
                waterExposure,
                0f,
                config.CreatureWaterDecayPerSecond * deltaTime);
            RefreshCapabilitiesFromModules();
            RefreshFrameAnchorState(now);

            if (IsFixed)
            {
                currentSpeed = 0f;
                currentState = InkCreatureState.Fixed;
                ApplyVisualState();
                return;
            }

            if (pinnedByFrameGun)
            {
                currentSpeed = 0f;
                currentState = InkCreatureState.Pinned;
                ApplyVisualState();
                return;
            }

            if (hasEye && now >= nextTargetRefreshTime)
            {
                RefreshTarget(figures, visibilityMask);
                nextTargetRefreshTime =
                    now + Mathf.Max(0.05f, targetRefreshInterval);
            }

            if (!hasFoot || movementSpeed <= 0f)
            {
                currentTarget = null;
                currentSpeed = 0f;
                currentState = InkCreatureState.Crippled;
                ApplyVisualState();
                return;
            }

            Vector3 desiredDirection = GetDesiredDirection(now);
            desiredDirection = ApplyWatercolorInstability(
                desiredDirection,
                now);
            desiredDirection = AvoidObstacles(
                desiredDirection,
                navigationMask);

            float control = Mathf.Lerp(
                1f,
                config.MinimumWaterControl,
                waterExposure);
            float speedMultiplier = Mathf.Lerp(
                1f,
                config.MaximumWaterSpeedMultiplier,
                waterExposure);
            float stepDistance =
                movementSpeed * speedMultiplier * deltaTime;
            Vector3 predictedPosition =
                transform.position + desiredDirection * stepDistance;

            if (!TryPlaceOnGround(
                    predictedPosition,
                    navigationMask,
                    out Vector3 groundedPosition,
                    out Vector3 groundNormal))
            {
                surfaceValid = false;
                currentSpeed = 0f;
                currentState = InkCreatureState.SurfaceLost;
                ApplyVisualState();
                return;
            }

            surfaceValid = true;
            Vector3 surfaceDirection = Vector3.ProjectOnPlane(
                desiredDirection,
                groundNormal).normalized;

            if (surfaceDirection.sqrMagnitude < 0.001f)
            {
                currentSpeed = 0f;
                ApplyVisualState();
                return;
            }

            float turnAmount = turnSpeedDegrees * control * deltaTime;
            Quaternion desiredRotation = Quaternion.LookRotation(
                surfaceDirection,
                groundNormal);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                turnAmount);
            transform.position = groundedPosition;
            currentSpeed = movementSpeed * speedMultiplier;

            TryApplyContact(now);
            ApplyVisualState();
        }

        public void ApplyWatercolorExposure(
            float amount,
            Vector3 flowDirection)
        {
            if (!initialized || amount <= 0f)
            {
                return;
            }

            waterExposure = Mathf.Clamp01(waterExposure + amount);

            if (flowDirection.sqrMagnitude > 0.001f)
            {
                watercolorDirection = flowDirection.normalized;
            }

            ApplyVisualState();
        }

        public bool ApplyFixative(float duration, Color statueColor)
        {
            if (!initialized || duration <= 0f)
            {
                return false;
            }

            bool wasFixed = IsFixed;
            fixedUntil = Mathf.Max(fixedUntil, Time.time + duration);
            fixedInkColor = statueColor;
            currentTarget = null;
            currentSpeed = 0f;
            currentState = InkCreatureState.Fixed;
            ApplyVisualState();
            return !wasFixed;
        }

        public bool TryDamageGlyph(
            InkGlyphType type,
            float damage,
            out bool glyphDisabled,
            out float remainingDurability)
        {
            glyphDisabled = false;
            remainingDurability = 0f;

            if (!initialized || damage <= 0f)
            {
                return false;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                InkGlyphModule module = modules[i];

                if (module == null ||
                    !module.IsEnabled ||
                    module.Type != type)
                {
                    continue;
                }

                float appliedDamage = module.ApplyDamage(damage);

                if (appliedDamage <= 0f)
                {
                    return false;
                }

                currentDurability = Mathf.Max(
                    0f,
                    currentDurability - appliedDamage);
                remainingDurability = module.CurrentDurability;
                glyphDisabled = !module.IsEnabled;
                RefreshCapabilitiesFromModules();
                RefreshGlyphHitZones();
                ApplyVisualState();
                return true;
            }

            return false;
        }

        public bool HasGlyph(InkGlyphType type)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                InkGlyphModule module = modules[i];

                if (module.IsEnabled && module.Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        public float GetGlyphDurability(InkGlyphType type)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                InkGlyphModule module = modules[i];

                if (module.Type == type)
                {
                    return module.CurrentDurability;
                }
            }

            return 0f;
        }

        public void RefreshGlyphHitZones()
        {
            if (glyphHitZones == null || glyphHitZones.Length == 0)
            {
                glyphHitZones =
                    GetComponentsInChildren<InkGlyphHitZone>(true);
            }

            for (int i = 0; i < glyphHitZones.Length; i++)
            {
                glyphHitZones[i]?.RefreshFromCreature();
            }
        }

        private void RefreshCapabilitiesFromModules()
        {
            hasEye = HasGlyph(InkGlyphType.Eye);
            hasFoot = HasGlyph(InkGlyphType.Foot);
            activeGlyphCount = 0;

            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].IsEnabled)
                {
                    activeGlyphCount++;
                }
            }

            if (!hasEye)
            {
                currentTarget = null;
            }
        }

        private void RefreshTarget(
            IReadOnlyList<FigureMotor> figures,
            LayerMask visibilityMask)
        {
            FigureMotor bestTarget = null;
            float bestDistanceSquared = detectionRange * detectionRange;

            if (figures != null)
            {
                for (int i = 0; i < figures.Count; i++)
                {
                    FigureMotor figure = figures[i];

                    if (figure == null || !figure.isActiveAndEnabled)
                    {
                        continue;
                    }

                    float distanceSquared =
                        (figure.transform.position - transform.position)
                        .sqrMagnitude;

                    if (distanceSquared >= bestDistanceSquared ||
                        (requiresLineOfSight &&
                         !HasLineOfSight(figure, visibilityMask)))
                    {
                        continue;
                    }

                    bestDistanceSquared = distanceSquared;
                    bestTarget = figure;
                }
            }

            currentTarget = bestTarget;
        }

        private bool HasLineOfSight(FigureMotor figure, LayerMask mask)
        {
            Vector3 origin = transform.position + Vector3.up * 0.45f;
            Vector3 target =
                figure.transform.position + Vector3.up * 0.65f;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;

            if (distance <= 0.01f)
            {
                return true;
            }

            int count = Physics.RaycastNonAlloc(
                origin,
                direction / distance,
                lineOfSightHits,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);
            float nearestBlocker = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = lineOfSightHits[i].collider;

                if (hitCollider == null ||
                    IsOwnCollider(hitCollider))
                {
                    continue;
                }

                if (hitCollider.GetComponentInParent<FigureMotor>() == figure)
                {
                    continue;
                }

                nearestBlocker = Mathf.Min(
                    nearestBlocker,
                    lineOfSightHits[i].distance);
            }

            return nearestBlocker >= distance - 0.08f;
        }

        private Vector3 GetDesiredDirection(float now)
        {
            if (currentTarget != null)
            {
                currentState = now < nextContactTime
                    ? InkCreatureState.ContactRecovery
                    : InkCreatureState.Pursuit;
                return Vector3.ProjectOnPlane(
                    currentTarget.transform.position - transform.position,
                    Vector3.up).normalized;
            }

            currentState = hasEye
                ? InkCreatureState.Patrol
                : InkCreatureState.Blinded;
            float angle = patrolPhase + now * 0.7f;
            Vector3 patrolPoint = homePosition + new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)) * config.PatrolRadius;
            Vector3 direction = Vector3.ProjectOnPlane(
                patrolPoint - transform.position,
                Vector3.up);

            if (direction.sqrMagnitude < 0.04f)
            {
                return transform.forward;
            }

            return direction.normalized;
        }

        private Vector3 ApplyWatercolorInstability(
            Vector3 direction,
            float now)
        {
            if (direction.sqrMagnitude < 0.001f ||
                waterExposure <= 0.001f)
            {
                return direction;
            }

            float phase = now * 4.3f + GetInstanceID() * 0.031f;
            float wobble = Mathf.Sin(phase) *
                config.MaximumWaterWobbleDegrees *
                waterExposure;
            Vector3 unstable =
                Quaternion.AngleAxis(wobble, Vector3.up) * direction;

            if (watercolorDirection.sqrMagnitude > 0.001f)
            {
                unstable = Vector3.Slerp(
                    unstable,
                    Vector3.ProjectOnPlane(
                        watercolorDirection,
                        Vector3.up).normalized,
                    waterExposure * 0.22f *
                    (0.5f + 0.5f * Mathf.Sin(phase * 0.61f)));
            }

            return unstable.normalized;
        }

        private Vector3 AvoidObstacles(Vector3 direction, LayerMask mask)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return transform.forward;
            }

            Vector3 origin = transform.position + Vector3.up * 0.35f;
            int count = Physics.SphereCastNonAlloc(
                origin,
                config.ObstacleProbeRadius,
                direction,
                obstacleHits,
                config.ObstacleProbeDistance,
                mask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = obstacleHits[i].collider;

                if (hitCollider == null ||
                    IsOwnCollider(hitCollider) ||
                    hitCollider.GetComponentInParent<FigureMotor>() ==
                    currentTarget)
                {
                    continue;
                }

                Vector3 tangent = Vector3.Cross(
                    obstacleHits[i].normal,
                    Vector3.up).normalized;

                if ((GetInstanceID() & 1) == 0)
                {
                    tangent = -tangent;
                }

                return Vector3.Slerp(
                    direction,
                    tangent,
                    0.82f).normalized;
            }

            return direction;
        }

        private bool TryPlaceOnGround(
            Vector3 predictedPosition,
            LayerMask mask,
            out Vector3 groundedPosition,
            out Vector3 normal)
        {
            Vector3 origin =
                predictedPosition + Vector3.up * config.GroundProbeHeight;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                config.GroundProbeHeight + config.GroundProbeDistance,
                mask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            RaycastHit bestHit = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null ||
                    IsOwnCollider(hit.collider) ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                float slope = Vector3.Angle(hit.normal, Vector3.up);

                if (slope > config.MaximumWalkableSlope)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            if (bestHit.collider == null)
            {
                groundedPosition = transform.position;
                normal = Vector3.up;
                return false;
            }

            normal = bestHit.normal.normalized;
            groundedPosition =
                bestHit.point + normal * config.SurfaceOffset;
            return true;
        }

        private void TryApplyContact(float now)
        {
            if (currentTarget == null ||
                !hasEye ||
                now < nextContactTime)
            {
                return;
            }

            Vector3 delta =
                currentTarget.transform.position - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude >
                config.ContactDistance * config.ContactDistance)
            {
                return;
            }

            FigureClarityState clarity =
                currentTarget.GetComponent<FigureClarityState>();

            if (clarity != null)
            {
                clarity.ApplyPaintExposure(
                    config.ClarityExposurePerContact,
                    FigurePaintRegion.Legs);
            }

            nextContactTime = now + config.ContactCooldown;
            currentState = InkCreatureState.ContactRecovery;
        }

        private bool IsOwnCollider(Collider targetCollider)
        {
            return targetCollider != null &&
                   (targetCollider.transform == transform ||
                    targetCollider.transform.IsChildOf(transform));
        }

        private void RefreshFrameAnchorState(float now)
        {
            if (now < nextAnchorScanTime)
            {
                return;
            }

            nextAnchorScanTime = now + AnchorScanInterval;
            pinnedByFrameGun =
                ContainsFrameAnchorMarker(transform);
        }

        private static bool ContainsFrameAnchorMarker(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name.StartsWith(
                        FrameAnchorMarkerName,
                        System.StringComparison.Ordinal) ||
                    ContainsFrameAnchorMarker(child))
                {
                    return true;
                }
            }

            return false;
        }

        private void CacheVisualComponents()
        {
            glyphHitZones =
                GetComponentsInChildren<InkGlyphHitZone>(true);
            allRenderers = GetComponentsInChildren<Renderer>(true);
            counterplayPropertyBlock ??= new MaterialPropertyBlock();
        }

        private void ApplyVisualState()
        {
            if (visualRoot == null || definition == null || config == null)
            {
                return;
            }

            float scale = definition.BaseScale * Mathf.Lerp(
                1f,
                config.MaximumWaterScale,
                waterExposure);
            float squash = 1f - waterExposure * 0.18f;
            visualRoot.localScale = Vector3.Scale(
                baseVisualScale,
                new Vector3(scale, scale * squash, scale));

            int visualMode = IsFixed ? 2 : pinnedByFrameGun ? 1 : 0;

            if (visualMode == lastVisualMode)
            {
                return;
            }

            lastVisualMode = visualMode;

            if (allRenderers == null || allRenderers.Length == 0)
            {
                allRenderers = GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer targetRenderer = allRenderers[i];

                if (targetRenderer == null)
                {
                    continue;
                }

                if (visualMode == 0)
                {
                    targetRenderer.SetPropertyBlock(null);
                    continue;
                }

                counterplayPropertyBlock.Clear();
                Color stateColor = visualMode == 2
                    ? fixedInkColor
                    : new Color(0.12f, 0.055f, 0.18f, 1f);
                counterplayPropertyBlock.SetColor(BaseColorId, stateColor);
                counterplayPropertyBlock.SetColor(ColorId, stateColor);
                targetRenderer.SetPropertyBlock(counterplayPropertyBlock);
            }
        }
    }
}
