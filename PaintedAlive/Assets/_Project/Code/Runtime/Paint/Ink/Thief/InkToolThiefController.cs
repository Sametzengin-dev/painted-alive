using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint.Ink.Lifecycle;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Thief
{
    [DefaultExecutionOrder(5)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkCreatureRuntime))]
    public sealed class InkToolThiefController : MonoBehaviour
    {
        private const int MaximumLineOfSightHits = 24;

        private readonly RaycastHit[] lineOfSightHits =
            new RaycastHit[MaximumLineOfSightHits];

        [SerializeField]
        private InkCreatureRuntime creature;

        [SerializeField]
        private InkToolThiefConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool thiefDefinitionActive;

        [SerializeField]
        private FigureMotor targetFigure;

        [SerializeField]
        private FigureToolLoadoutController targetLoadout;

        [SerializeField]
        private bool stealWindupActive;

        [SerializeField, Range(0f, 1f)]
        private float stealWindupProgress;

        [SerializeField]
        private bool carryingTool;

        [SerializeField]
        private FigureToolId carriedTool;

        [SerializeField]
        private Vector3 escapePoint;

        [SerializeField]
        private string lastResult = "Inactive";

        private FigureMotor victimFigure;
        private FigureToolLoadoutController victimLoadout;
        private float nextTargetRefreshTime;
        private float stealWindupStartedAt;
        private float nextStealAttemptTime;
        private float toolStolenAt;
        private GameObject carriedToolVisual;
        private GameObject telegraphVisual;
        private Transform telegraphTransform;
        private Vector3 telegraphBaseScale;

        public bool IsThiefDefinitionActive => thiefDefinitionActive;
        public bool IsCarryingTool => carryingTool;
        public FigureToolId CarriedTool => carriedTool;
        public float StealWindupProgress => stealWindupProgress;
        public string LastResult => lastResult;

        private void Awake()
        {
            creature ??= GetComponent<InkCreatureRuntime>();
        }

        private void Start()
        {
            RefreshDefinitionState();
        }

        private void Update()
        {
            RefreshDefinitionState();

            if (!thiefDefinitionActive || creature == null)
            {
                HideTelegraph();
                return;
            }

            if (!creature.enabled)
            {
                if (carryingTool)
                {
                    DropOrSafetyReturn(
                        "Autonomy interrupted");
                }

                ClearTarget();
                HideTelegraph();
                lastResult = "Autonomy paused";
                return;
            }

            if (ShouldDropForCounter(out string counterReason))
            {
                if (carryingTool)
                {
                    DropOrSafetyReturn(counterReason);
                }

                CancelWindup();
                ClearTarget();
                lastResult = counterReason;
                return;
            }

            if (carryingTool)
            {
                UpdateCarry();
                return;
            }

            if (Time.time >= nextTargetRefreshTime)
            {
                RefreshTarget();
                nextTargetRefreshTime = Time.time +
                    Mathf.Max(0.05f, TargetRefreshInterval);
            }

            UpdateStealAttempt();
        }

        private void OnDisable()
        {
            if (carryingTool &&
                victimLoadout != null &&
                victimLoadout.TheftOwner == this)
            {
                victimLoadout.TryReturnStolenTool(
                    this,
                    "Boya Hırsızı disabled; safety return");
            }

            ClearCarryState();
            CancelWindup();
            ClearTarget();
        }

        private void OnDestroy()
        {
            if (victimLoadout != null &&
                victimLoadout.IsToolStolen &&
                victimLoadout.TheftOwner == this)
            {
                victimLoadout.TryReturnStolenTool(
                    this,
                    "Boya Hırsızı destroyed; safety return");
            }
        }

        public void Configure(InkToolThiefConfig targetConfig)
        {
            creature ??= GetComponent<InkCreatureRuntime>();
            config = targetConfig;
            RefreshDefinitionState();

            InkToolThiefMouthSpongeSource mouth =
                GetComponentInChildren<
                    InkToolThiefMouthSpongeSource>(true);
            mouth?.Configure(creature, config);
        }

        /// <summary>
        /// Called by InkCreatureRuntime. Returns true only while the thief
        /// owns a special seek/windup/escape movement decision. With no
        /// target, the existing blinded patrol remains in charge.
        /// </summary>
        public bool TryGetMovementOverride(
            float now,
            out Vector3 desiredDirection,
            out InkCreatureState state)
        {
            desiredDirection = Vector3.zero;
            state = InkCreatureState.Blinded;

            if (!thiefDefinitionActive ||
                creature == null ||
                !creature.IsInitialized ||
                creature.IsFixed ||
                creature.IsPinned)
            {
                return false;
            }

            if (carryingTool)
            {
                state = InkCreatureState.ToolEscape;
                Vector3 escapeDirection =
                    escapePoint - transform.position;
                escapeDirection = Vector3.ProjectOnPlane(
                    escapeDirection,
                    Vector3.up);

                if (escapeDirection.sqrMagnitude < 0.04f &&
                    victimFigure != null)
                {
                    escapeDirection = Vector3.ProjectOnPlane(
                        transform.position -
                        victimFigure.transform.position,
                        Vector3.up);
                }

                desiredDirection = escapeDirection.sqrMagnitude > 0.001f
                    ? escapeDirection.normalized
                    : transform.forward;
                return true;
            }

            if (stealWindupActive)
            {
                state = InkCreatureState.ToolStealWindup;
                desiredDirection = Vector3.zero;
                return true;
            }

            if (targetFigure != null && targetLoadout != null)
            {
                state = InkCreatureState.ToolSeeking;
                Vector3 toTarget = Vector3.ProjectOnPlane(
                    targetFigure.transform.position -
                    transform.position,
                    Vector3.up);
                desiredDirection = toTarget.sqrMagnitude > 0.001f
                    ? toTarget.normalized
                    : transform.forward;
                return true;
            }

            return false;
        }

        private void RefreshDefinitionState()
        {
            bool active = creature != null &&
                creature.IsInitialized &&
                creature.Definition != null &&
                creature.Definition.ContainsGlyph(InkGlyphType.Hand) &&
                creature.Definition.ContainsGlyph(InkGlyphType.Mouth) &&
                creature.Definition.ContainsGlyph(InkGlyphType.Foot) &&
                !creature.Definition.ContainsGlyph(InkGlyphType.Eye);

            if (active == thiefDefinitionActive)
            {
                return;
            }

            thiefDefinitionActive = active;
            lastResult = active
                ? "Boya Hırsızı active"
                : "Inactive for this creature definition";

            if (active)
            {
                EnsureTelegraphVisual();

                InkToolThiefMouthSpongeSource mouth =
                    GetComponentInChildren<
                        InkToolThiefMouthSpongeSource>(true);
                mouth?.Configure(creature, config);
            }
            else
            {
                HideTelegraph();
            }
        }

        private void RefreshTarget()
        {
            if (stealWindupActive || carryingTool)
            {
                return;
            }

            FigureToolLoadoutController[] loadouts =
                Object.FindObjectsByType<FigureToolLoadoutController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            FigureToolLoadoutController bestLoadout = null;
            FigureMotor bestFigure = null;
            float bestDistanceSquared = DetectionRange * DetectionRange;

            for (int i = 0; i < loadouts.Length; i++)
            {
                FigureToolLoadoutController candidate = loadouts[i];

                if (candidate == null ||
                    !candidate.isActiveAndEnabled ||
                    candidate.IsToolStolen)
                {
                    continue;
                }

                FigureMotor figure =
                    candidate.GetComponentInParent<FigureMotor>();

                if (figure == null || !figure.isActiveAndEnabled)
                {
                    continue;
                }

                float distanceSquared =
                    (figure.transform.position - transform.position)
                    .sqrMagnitude;

                if (distanceSquared >= bestDistanceSquared ||
                    (RequiresLineOfSight &&
                     !HasLineOfSight(figure)))
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;
                bestLoadout = candidate;
                bestFigure = figure;
            }

            targetLoadout = bestLoadout;
            targetFigure = bestFigure;
            lastResult = targetFigure != null
                ? $"Seeking {targetFigure.name} tool"
                : "No stealable Figure tool in range";
        }

        private void UpdateStealAttempt()
        {
            if (targetFigure == null || targetLoadout == null)
            {
                CancelWindup();
                return;
            }

            if (!targetLoadout.isActiveAndEnabled ||
                targetLoadout.IsToolStolen ||
                !targetFigure.isActiveAndEnabled)
            {
                CancelWindup();
                ClearTarget();
                return;
            }

            float distance = Vector3.Distance(
                transform.position,
                targetFigure.transform.position);

            if (distance > StealDistance ||
                (RequiresLineOfSight &&
                 !HasLineOfSight(targetFigure)))
            {
                CancelWindup();
                return;
            }

            if (Time.time < nextStealAttemptTime)
            {
                return;
            }

            if (!stealWindupActive)
            {
                stealWindupActive = true;
                stealWindupStartedAt = Time.time;
                stealWindupProgress = 0f;
                lastResult = "Steal telegraph";
                ShowTelegraph();
                return;
            }

            stealWindupProgress = Mathf.Clamp01(
                (Time.time - stealWindupStartedAt) /
                Mathf.Max(0.1f, StealWindup));
            UpdateTelegraphPulse();

            if (stealWindupProgress < 1f)
            {
                return;
            }

            TryCompleteSteal();
        }

        private void TryCompleteSteal()
        {
            FigureToolLoadoutController loadout = targetLoadout;
            FigureMotor figure = targetFigure;
            CancelWindup();

            if (loadout == null || figure == null ||
                !loadout.TryStealActiveTool(
                    this,
                    out FigureToolId stolenTool))
            {
                nextStealAttemptTime =
                    Time.time + StealRetryCooldown;
                lastResult = "Steal rejected";
                return;
            }

            carryingTool = true;
            carriedTool = stolenTool;
            victimLoadout = loadout;
            victimFigure = figure;
            toolStolenAt = Time.time;
            escapePoint = BuildEscapePoint(figure);
            CreateCarriedToolVisual(stolenTool);
            ClearTarget();
            lastResult =
                $"Carrying {FigureToolLoadoutController.GetDisplayName(stolenTool)}";

            Debug.Log(
                "[M55.3 Boya Hırsızı] " +
                $"{FigureToolLoadoutController.GetDisplayName(stolenTool)} " +
                $"{figure.name} üzerinden çalındı.",
                this);
        }

        private void UpdateCarry()
        {
            if (victimLoadout == null ||
                !victimLoadout.IsToolStolen ||
                victimLoadout.TheftOwner != this)
            {
                ClearCarryState();
                lastResult = "Tool ownership ended externally";
                return;
            }

            float carryAge = Time.time - toolStolenAt;
            float victimDistance = victimFigure != null
                ? Vector3.Distance(
                    transform.position,
                    victimFigure.transform.position)
                : float.PositiveInfinity;

            if (carryAge >= MaximumCarryDuration ||
                (carryAge >= MinimumCarryDuration &&
                 victimDistance >= DropDistanceFromVictim))
            {
                DropOrSafetyReturn(
                    carryAge >= MaximumCarryDuration
                        ? "Maximum carry duration reached"
                        : "Escape distance reached");
            }
        }

        private bool ShouldDropForCounter(out string reason)
        {
            reason = null;

            if (creature == null)
            {
                reason = "Creature unavailable";
                return true;
            }

            InkCreatureDeathSequence death =
                GetComponent<InkCreatureDeathSequence>();

            if (death != null && death.IsDying)
            {
                reason = "Creature dying";
                return true;
            }

            if (creature.IsFixed)
            {
                reason = "Fixative counter";
                return true;
            }

            if (creature.IsPinned)
            {
                reason = "Frame Gun pin counter";
                return true;
            }

            if (!creature.HasGlyph(InkGlyphType.Hand))
            {
                reason = "Hand glyph severed";
                return true;
            }

            if (!creature.HasGlyph(InkGlyphType.Mouth))
            {
                reason = "Mouth glyph disabled";
                return true;
            }

            if (!creature.HasGlyph(InkGlyphType.Foot))
            {
                reason = "Foot glyph lost";
                return true;
            }

            return false;
        }

        private void DropOrSafetyReturn(string reason)
        {
            FigureToolLoadoutController loadout = victimLoadout;
            FigureToolId tool = carriedTool;

            if (loadout == null ||
                !loadout.IsToolStolen ||
                loadout.TheftOwner != this)
            {
                ClearCarryState();
                return;
            }

            Vector3 pickupPosition =
                transform.position + Vector3.up * 0.3f;
            bool spawned = InkStolenToolPickup.TrySpawn(
                loadout,
                tool,
                this,
                pickupPosition,
                PickupAutoReturnTime,
                out _);

            if (!spawned)
            {
                loadout.TryReturnStolenTool(
                    this,
                    $"{reason}; pickup creation failed");
            }

            Debug.Log(
                "[M55.3 Boya Hırsızı] Çalınan " +
                $"{FigureToolLoadoutController.GetDisplayName(tool)} " +
                $"düşürüldü. Neden={reason}",
                this);

            ClearCarryState();
            nextStealAttemptTime =
                Time.time + StealRetryCooldown;
            lastResult = $"Dropped tool: {reason}";
        }

        private Vector3 BuildEscapePoint(FigureMotor figure)
        {
            Vector3 away = figure != null
                ? Vector3.ProjectOnPlane(
                    transform.position - figure.transform.position,
                    Vector3.up)
                : -transform.forward;

            if (away.sqrMagnitude < 0.001f)
            {
                away = -transform.forward;
            }

            away.Normalize();
            float side = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            Vector3 lateral = Vector3.Cross(Vector3.up, away) *
                (0.28f * side);
            Vector3 escapeDirection =
                (away + lateral).normalized;
            return transform.position +
                escapeDirection * DesiredEscapeDistance;
        }

        private bool HasLineOfSight(FigureMotor figure)
        {
            if (figure == null)
            {
                return false;
            }

            Vector3 origin =
                transform.position + transform.up * 0.42f;
            Vector3 target =
                figure.transform.position + Vector3.up * 0.7f;
            Vector3 delta = target - origin;
            float distance = delta.magnitude;

            if (distance <= 0.05f)
            {
                return true;
            }

            int count = Physics.RaycastNonAlloc(
                origin,
                delta / distance,
                lineOfSightHits,
                distance + 0.08f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            float nearestBlocker = float.PositiveInfinity;
            float nearestFigureHit = distance;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = lineOfSightHits[i];

                if (hit.collider == null || IsOwnCollider(hit.collider))
                {
                    continue;
                }

                FigureMotor hitFigure =
                    hit.collider.GetComponentInParent<FigureMotor>();

                if (hitFigure == figure)
                {
                    nearestFigureHit = Mathf.Min(
                        nearestFigureHit,
                        hit.distance);
                    continue;
                }

                nearestBlocker = Mathf.Min(
                    nearestBlocker,
                    hit.distance);
            }

            return nearestBlocker >= nearestFigureHit - 0.05f;
        }

        private bool IsOwnCollider(Collider targetCollider)
        {
            return targetCollider != null &&
                (targetCollider.transform == transform ||
                 targetCollider.transform.IsChildOf(transform));
        }

        private void CreateCarriedToolVisual(FigureToolId tool)
        {
            DestroyCarriedToolVisual();
            carriedToolVisual = InkToolProxyVisualUtility.CreateProxy(
                transform,
                tool,
                "CarriedTool_M55_3",
                new Vector3(0f, 0.43f, 0.57f),
                0.85f);
        }

        private void DestroyCarriedToolVisual()
        {
            if (carriedToolVisual != null)
            {
                Destroy(carriedToolVisual);
                carriedToolVisual = null;
            }
        }

        private void EnsureTelegraphVisual()
        {
            if (telegraphVisual != null)
            {
                return;
            }

            telegraphVisual = GameObject.CreatePrimitive(
                PrimitiveType.Sphere);
            telegraphVisual.name = "StealTelegraph_M55_3";
            telegraphTransform = telegraphVisual.transform;
            telegraphTransform.SetParent(transform, false);
            telegraphTransform.localPosition =
                new Vector3(0f, 1.0f, 0f);
            telegraphTransform.localScale =
                Vector3.one * 0.18f;
            telegraphBaseScale = telegraphTransform.localScale;

            Collider collider = telegraphVisual.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer =
                telegraphVisual.GetComponent<Renderer>();
            InkToolProxyVisualUtility.ApplyColor(
                renderer,
                new Color(1f, 0.42f, 0.08f, 1f));
            telegraphVisual.SetActive(false);
        }

        private void ShowTelegraph()
        {
            EnsureTelegraphVisual();

            if (telegraphVisual != null)
            {
                telegraphVisual.SetActive(true);
            }
        }

        private void HideTelegraph()
        {
            if (telegraphVisual != null)
            {
                telegraphVisual.SetActive(false);
            }
        }

        private void UpdateTelegraphPulse()
        {
            if (telegraphTransform == null)
            {
                return;
            }

            float pulse = 1f +
                Mathf.Sin(Time.time * 18f) * 0.28f;
            telegraphTransform.localScale =
                telegraphBaseScale * pulse;
        }

        private void CancelWindup()
        {
            stealWindupActive = false;
            stealWindupProgress = 0f;
            HideTelegraph();
        }

        private void ClearTarget()
        {
            targetFigure = null;
            targetLoadout = null;
        }

        private void ClearCarryState()
        {
            carryingTool = false;
            victimFigure = null;
            victimLoadout = null;
            DestroyCarriedToolVisual();
        }

        private float DetectionRange =>
            config != null ? config.DetectionRange : 8.5f;
        private float TargetRefreshInterval =>
            config != null ? config.TargetRefreshInterval : 0.2f;
        private bool RequiresLineOfSight =>
            config == null || config.RequiresLineOfSight;
        private float StealDistance =>
            config != null ? config.StealDistance : 1.15f;
        private float StealWindup =>
            config != null ? config.StealWindup : 0.85f;
        private float StealRetryCooldown =>
            config != null ? config.StealRetryCooldown : 1f;
        private float DesiredEscapeDistance =>
            config != null ? config.DesiredEscapeDistance : 6f;
        private float DropDistanceFromVictim =>
            config != null ? config.DropDistanceFromVictim : 5f;
        private float MinimumCarryDuration =>
            config != null ? config.MinimumCarryDuration : 2f;
        private float MaximumCarryDuration =>
            config != null ? config.MaximumCarryDuration : 7f;
        private float PickupAutoReturnTime =>
            config != null ? config.PickupAutoReturnTime : 12f;
    }
}
