using PaintedAlive.Figures;
using PaintedAlive.Paint.Sponge;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Figures.Tools
{
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class SpongeController : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        private readonly RaycastHit[] interactionHits =
            new RaycastHit[64];

        private readonly Collider[] proximityColliders =
            new Collider[64];

        private readonly RaycastHit[] visibilityHits =
            new RaycastHit[64];

        [Header("Dependencies")]
        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private InputActionReference useToolAction;

        [SerializeField]
        private InputActionReference releasePaintAction;

        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private SpongeConfig config;

        [SerializeField]
        private SpongeReservoir reservoir;

        [SerializeField]
        private SpongeFeedback feedback;

        [SerializeField]
        private SpongeAbsorbablePaintSource dischargePuddlePrefab;

        [SerializeField]
        private Renderer spongeRenderer;

        [Header("Detection")]
        [SerializeField]
        private LayerMask absorptionMask =
            Physics.DefaultRaycastLayers;

        [SerializeField]
        private LayerMask dischargeSurfaceMask =
            Physics.DefaultRaycastLayers;

        [Header("Optional HUD")]
        [SerializeField]
        private GameObject hudRoot;

        [SerializeField]
        private Image capacityFillImage;

        [SerializeField]
        private Text capacityText;

        [SerializeField]
        private Text statusText;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool isAbsorbing;

        [SerializeField]
        private string currentTargetType = "Yok";

        [SerializeField]
        private float currentMovementMultiplier = 1f;

        private MaterialPropertyBlock spongePropertyBlock;
        private CharacterController figureController;
        private bool useWasHeld;

        public bool IsAbsorbing => isAbsorbing;
        public string CurrentTargetType => currentTargetType;
        public float CurrentMovementMultiplier =>
            currentMovementMultiplier;

        private void Awake()
        {
            if (clarityState == null)
            {
                clarityState =
                    GetComponentInParent<FigureClarityState>();
            }

            if (figureMotor == null)
            {
                figureMotor =
                    GetComponentInParent<FigureMotor>();
            }

            if (figureMotor != null)
            {
                figureController =
                    figureMotor.GetComponent<CharacterController>();
            }

            if (reservoir == null)
            {
                reservoir = GetComponent<SpongeReservoir>();
            }

            if (feedback == null)
            {
                feedback = GetComponent<SpongeFeedback>();
            }

            if (config == null ||
                reservoir == null ||
                figureMotor == null ||
                outputCamera == null)
            {
                Debug.LogError(
                    $"{nameof(SpongeController)} on {name} " +
                    "requires Camera, Config, Reservoir and " +
                    "FigureMotor references.",
                    this);
                enabled = false;
                return;
            }

            reservoir.Configure(config);
            reservoir.ReservoirChanged +=
                HandleReservoirChanged;
            spongePropertyBlock = new MaterialPropertyBlock();
            HandleReservoirChanged();
            SetHudVisible(false);
        }

        private void OnEnable()
        {
            EnableAction(useToolAction);
            EnableAction(releasePaintAction);
            SetHudVisible(true);
            RefreshHud();
        }

        private void OnDisable()
        {
            DisableAction(useToolAction);
            DisableAction(releasePaintAction);
            isAbsorbing = false;
            useWasHeld = false;
            currentTargetType = "Yok";
            SetHudVisible(false);
        }

        private void OnDestroy()
        {
            if (reservoir != null)
            {
                reservoir.ReservoirChanged -=
                    HandleReservoirChanged;
            }

            if (figureMotor != null)
            {
                figureMotor.SetEquipmentMovementMultiplier(1f);
            }
        }

        private void Update()
        {
            if (config == null || reservoir == null)
            {
                return;
            }

            if (WasDischargePressedThisFrame())
            {
                TryDischarge();
            }

            bool useHeld = IsUseHeld();
            bool usePressedThisFrame = useHeld && !useWasHeld;
            useWasHeld = useHeld;

            if (!useHeld)
            {
                isAbsorbing = false;
                currentTargetType = "Yok";
                RefreshHud();
                return;
            }

            if (clarityState != null &&
                !clarityState.CanUsePrimaryTool)
            {
                isAbsorbing = false;
                currentTargetType = "NETLİK YETERSİZ";

                if (usePressedThisFrame)
                {
                    feedback?.PlayRejected(transform.position);
                }

                RefreshHud();
                return;
            }

            if (reservoir.IsFull)
            {
                isAbsorbing = false;
                currentTargetType = "SÜNGER DOLU";

                if (usePressedThisFrame)
                {
                    feedback?.PlayRejected(transform.position);
                }

                RefreshHud();
                return;
            }

            bool interacted = TryAbsorbAimedTarget(Time.deltaTime);

            if (!interacted &&
                config.AllowSelfCleaningForPrototype)
            {
                interacted = TryCleanFigure(
                    clarityState,
                    Time.deltaTime,
                    true,
                    transform.position + Vector3.up * 1.2f,
                    Vector3.up);
            }

            isAbsorbing = interacted;

            if (!interacted)
            {
                currentTargetType = "HEDEF YOK";
            }

            RefreshHud();
        }

        private bool TryAbsorbAimedTarget(float deltaTime)
        {
            Ray aimRay =
                outputCamera.ViewportPointToRay(
                    new Vector3(0.5f, 0.5f, 0f));

            float aimSearchDistance =
                GetAimSearchDistance(aimRay.origin);

            int hitCount =
                Physics.SphereCastNonAlloc(
                    aimRay,
                    config.InteractionRadius,
                    interactionHits,
                    aimSearchDistance,
                    absorptionMask,
                    QueryTriggerInteraction.Collide);

            FigureClarityState bestFigure = null;
            ISpongeAbsorbableSource bestSource = null;
            RaycastHit bestFigureHit = default;
            RaycastHit bestSourceHit = default;
            float bestFigureDistance = float.PositiveInfinity;
            float bestSourceDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = interactionHits[i];

                if (hit.collider == null)
                {
                    continue;
                }

                FigureClarityState candidateFigure =
                    hit.collider.GetComponentInParent<
                        FigureClarityState>();

                if (candidateFigure != null &&
                    candidateFigure != clarityState &&
                    candidateFigure.CurrentLevel !=
                    FigureClarityLevel.Stain &&
                    candidateFigure.CurrentClarity <
                    candidateFigure.MaximumClarity &&
                    IsWithinFigureRange(GetHitPoint(hit)) &&
                    hit.distance < bestFigureDistance)
                {
                    bestFigure = candidateFigure;
                    bestFigureHit = hit;
                    bestFigureDistance = hit.distance;
                }

                ISpongeAbsorbableSource candidateSource =
                    FindAbsorbableSource(hit.collider);

                if (candidateSource != null &&
                    candidateSource.CanAbsorb &&
                    IsWithinFigureRange(
                        candidateSource,
                        GetHitPoint(hit)) &&
                    HasClearLineOfSight(
                        aimRay.origin,
                        candidateSource is
                            SpongeAbsorbablePaintSource paintSource
                                ? paintSource.InteractionPoint
                                : GetHitPoint(hit),
                        candidateSource) &&
                    hit.distance < bestSourceDistance)
                {
                    bestSource = candidateSource;
                    bestSourceHit = hit;
                    bestSourceDistance = hit.distance;
                }
            }

            if (bestFigure != null &&
                bestFigureDistance <= bestSourceDistance + 0.05f)
            {
                return TryCleanFigure(
                    bestFigure,
                    deltaTime,
                    false,
                    GetHitPoint(bestFigureHit),
                    GetHitNormal(bestFigureHit));
            }

            if (bestSource != null)
            {
                return TryAbsorbPaintSource(
                    bestSource,
                    deltaTime,
                    GetHitPoint(bestSourceHit),
                    GetHitNormal(bestSourceHit));
            }

            if (TryFindAbsorbableAlongAim(
                    aimRay,
                    aimSearchDistance,
                    out ISpongeAbsorbableSource nearbySource,
                    out Vector3 nearbyPoint,
                    out Vector3 nearbyNormal))
            {
                return TryAbsorbPaintSource(
                    nearbySource,
                    deltaTime,
                    nearbyPoint,
                    nearbyNormal);
            }

            Debug.DrawRay(
                aimRay.origin,
                aimRay.direction * config.InteractionRange,
                Color.yellow,
                0.05f);
            return false;
        }

        private bool TryFindAbsorbableAlongAim(
            Ray aimRay,
            float aimSearchDistance,
            out ISpongeAbsorbableSource bestSource,
            out Vector3 bestPoint,
            out Vector3 bestNormal)
        {
            bestSource = null;
            bestPoint = Vector3.zero;
            bestNormal = Vector3.up;

            Vector3 capsuleStart =
                aimRay.origin + aimRay.direction * 0.05f;
            Vector3 capsuleEnd =
                aimRay.origin +
                aimRay.direction * aimSearchDistance;
            float capsuleRadius =
                config.InteractionRadius * 1.35f;

            int overlapCount =
                Physics.OverlapCapsuleNonAlloc(
                    capsuleStart,
                    capsuleEnd,
                    capsuleRadius,
                    proximityColliders,
                    absorptionMask,
                    QueryTriggerInteraction.Collide);

            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < overlapCount; i++)
            {
                Collider candidateCollider =
                    proximityColliders[i];

                if (candidateCollider == null)
                {
                    continue;
                }

                ISpongeAbsorbableSource candidateSource =
                    FindAbsorbableSource(candidateCollider);

                if (candidateSource == null ||
                    !candidateSource.CanAbsorb)
                {
                    continue;
                }

                Vector3 boundsCenter =
                    candidateCollider.bounds.center;
                float projectedDistance =
                    Vector3.Dot(
                        boundsCenter - aimRay.origin,
                        aimRay.direction);

                if (projectedDistance < 0f ||
                    projectedDistance > aimSearchDistance)
                {
                    continue;
                }

                Vector3 pointOnAim =
                    aimRay.GetPoint(projectedDistance);
                Vector3 closestPoint =
                    candidateCollider.ClosestPoint(pointOnAim);
                float radialDistance =
                    Vector3.Distance(pointOnAim, closestPoint);

                if (radialDistance > capsuleRadius ||
                    !IsWithinFigureRange(
                        candidateSource,
                        closestPoint) ||
                    projectedDistance >= bestDistance ||
                    !HasClearLineOfSight(
                        aimRay.origin,
                        boundsCenter,
                        candidateSource))
                {
                    continue;
                }

                bestDistance = projectedDistance;
                bestSource = candidateSource;
                bestPoint = closestPoint;

                Vector3 surfaceDirection =
                    closestPoint - boundsCenter;
                bestNormal =
                    surfaceDirection.sqrMagnitude > 0.0001f
                        ? surfaceDirection.normalized
                        : Vector3.up;
            }

            // Thin puddles can be missed by a camera-origin physics cast.
            // They already maintain a zero-allocation runtime registry, so
            // evaluate that registry using camera aim but Figure-based range.
            foreach (SpongeAbsorbablePaintSource source in
                     SpongeAbsorbablePaintSource.ActiveSources)
            {
                if (source == null ||
                    !source.CanAbsorb ||
                    !IsWithinFigureRange(
                        source,
                        source.InteractionPoint))
                {
                    continue;
                }

                Vector3 sourcePoint = source.InteractionPoint;
                float projectedDistance = Vector3.Dot(
                    sourcePoint - aimRay.origin,
                    aimRay.direction);

                if (projectedDistance < 0f ||
                    projectedDistance > aimSearchDistance ||
                    projectedDistance >= bestDistance)
                {
                    continue;
                }

                Vector3 pointOnAim =
                    aimRay.GetPoint(projectedDistance);
                float allowedAimDistance =
                    capsuleRadius + source.InteractionRadius;

                if (Vector3.Distance(pointOnAim, sourcePoint) >
                        allowedAimDistance ||
                    !HasClearLineOfSight(
                        aimRay.origin,
                        sourcePoint,
                        source))
                {
                    continue;
                }

                bestDistance = projectedDistance;
                bestSource = source;
                bestPoint = sourcePoint;
                bestNormal = Vector3.up;
            }

            return bestSource != null;
        }

        private float GetAimSearchDistance(Vector3 aimOrigin)
        {
            return config.InteractionRange +
                   Vector3.Distance(
                       aimOrigin,
                       GetFigureInteractionOrigin());
        }

        private Vector3 GetFigureInteractionOrigin()
        {
            if (figureController != null)
            {
                return figureController.bounds.center;
            }

            return figureMotor != null
                ? figureMotor.transform.position + Vector3.up
                : transform.position + Vector3.up;
        }

        private bool IsWithinFigureRange(Vector3 targetPoint)
        {
            return Vector3.Distance(
                       GetFigureInteractionOrigin(),
                       targetPoint) <=
                   config.InteractionRange;
        }

        private bool IsWithinFigureRange(
            ISpongeAbsorbableSource source,
            Vector3 fallbackPoint)
        {
            if (source is SpongeAbsorbablePaintSource paintSource)
            {
                float distanceToSurface = Mathf.Max(
                    0f,
                    Vector3.Distance(
                        GetFigureInteractionOrigin(),
                        paintSource.InteractionPoint) -
                    paintSource.InteractionRadius);

                return distanceToSurface <=
                       config.InteractionRange;
            }

            return IsWithinFigureRange(fallbackPoint);
        }

        private bool HasClearLineOfSight(
            Vector3 origin,
            Vector3 target,
            ISpongeAbsorbableSource targetSource)
        {
            Vector3 offset = target - origin;
            float distance = offset.magnitude;

            if (distance <= 0.001f)
            {
                return true;
            }

            int hitCount =
                Physics.RaycastNonAlloc(
                    new Ray(origin, offset / distance),
                    visibilityHits,
                    distance + 0.05f,
                    absorptionMask,
                    QueryTriggerInteraction.Collide);

            Collider nearestCollider = null;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = visibilityHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                FigureMotor hitMotor =
                    hit.collider.GetComponentInParent<FigureMotor>();

                if (hitMotor == figureMotor)
                {
                    continue;
                }

                nearestCollider = hit.collider;
                nearestDistance = hit.distance;
            }

            if (nearestCollider == null)
            {
                return true;
            }

            return object.ReferenceEquals(
                FindAbsorbableSource(nearestCollider),
                targetSource);
        }

        private bool TryCleanFigure(
            FigureClarityState target,
            float deltaTime,
            bool selfCleaning,
            Vector3 feedbackPosition,
            Vector3 feedbackNormal)
        {
            if (target == null ||
                target.CurrentLevel == FigureClarityLevel.Stain ||
                target.CurrentClarity >= target.MaximumClarity)
            {
                return false;
            }

            float efficiency =
                selfCleaning
                    ? config.SelfCleaningEfficiency
                    : 1f;

            float requestedRestore =
                Mathf.Min(
                    config.ClarityRestoreRate *
                    efficiency * deltaTime,
                    reservoir.RemainingCapacity);

            float restored =
                target.RestoreAmount(requestedRestore);

            if (restored <= 0f)
            {
                return false;
            }

            reservoir.AddPaint(
                restored,
                config.ClarityPaintColor,
                config.StainedFigurePaintInstability);

            currentTargetType =
                selfCleaning
                    ? "KENDİNİ TEMİZLİYOR"
                    : "TAKIM ARKADAŞI";

            feedback?.PlayAbsorb(
                feedbackPosition,
                feedbackNormal,
                config.ClarityPaintColor,
                reservoir.NormalizedFill);
            return true;
        }

        private bool TryAbsorbPaintSource(
            ISpongeAbsorbableSource source,
            float deltaTime,
            Vector3 feedbackPosition,
            Vector3 feedbackNormal)
        {
            float requestedAmount =
                Mathf.Min(
                    config.PaintAbsorbRate * deltaTime,
                    reservoir.RemainingCapacity);

            Color sourceColor = source.PaintColor;
            float sourceInstability = source.Instability;
            float absorbed = source.Absorb(requestedAmount);

            if (absorbed <= 0f)
            {
                return false;
            }

            reservoir.AddPaint(
                absorbed,
                sourceColor,
                sourceInstability);
            currentTargetType = "BOYA EMİYOR";

            feedback?.PlayAbsorb(
                feedbackPosition,
                feedbackNormal,
                sourceColor,
                reservoir.NormalizedFill);
            return true;
        }

        private void TryDischarge()
        {
            if (reservoir == null ||
                reservoir.IsEmpty ||
                dischargePuddlePrefab == null ||
                outputCamera == null)
            {
                feedback?.PlayRejected(
                    outputCamera != null
                        ? outputCamera.transform.position
                        : transform.position);
                return;
            }

            Ray dischargeRay =
                outputCamera.ViewportPointToRay(
                    new Vector3(0.5f, 0.5f, 0f));

            int dischargeHitCount =
                Physics.RaycastNonAlloc(
                    dischargeRay,
                    interactionHits,
                    config.DischargeRange,
                    dischargeSurfaceMask,
                    QueryTriggerInteraction.Ignore);

            bool foundSurface = false;
            RaycastHit hit = default;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < dischargeHitCount; i++)
            {
                RaycastHit candidate = interactionHits[i];

                if (candidate.collider == null ||
                    candidate.distance >= nearestDistance)
                {
                    continue;
                }

                FigureMotor hitMotor =
                    candidate.collider.GetComponentInParent<
                        FigureMotor>();

                if (hitMotor == figureMotor)
                {
                    continue;
                }

                foundSurface = true;
                nearestDistance = candidate.distance;
                hit = candidate;
            }

            if (!foundSurface)
            {
                feedback?.PlayRejected(
                    outputCamera.transform.position);
                return;
            }

            float requestedAmount =
                Mathf.Min(
                    config.DischargeAmountPerUse,
                    reservoir.StoredAmount);

            Color paintColor = reservoir.StoredColor;
            float instability = reservoir.MixtureInstability;

            SpongeAbsorbablePaintSource created =
                Instantiate(
                    dischargePuddlePrefab,
                    hit.point +
                    hit.normal * config.DischargeSurfaceOffset,
                    Quaternion.FromToRotation(
                        Vector3.up,
                        hit.normal));

            if (created == null)
            {
                feedback?.PlayRejected(hit.point);
                return;
            }

            float removed =
                reservoir.RemovePaint(
                    requestedAmount,
                    out Color removedColor,
                    out float removedInstability);

            created.Initialize(
                removed,
                removedColor,
                removedInstability);

            currentTargetType = "BOYA BIRAKILDI";
            feedback?.PlayDischarge(
                hit.point,
                hit.normal,
                paintColor,
                removed / config.MaximumCapacity);
            RefreshHud();
        }

        private void HandleReservoirChanged()
        {
            if (reservoir == null || config == null)
            {
                return;
            }

            currentMovementMultiplier =
                Mathf.Lerp(
                    1f,
                    config.FullMovementMultiplier,
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        reservoir.NormalizedFill));

            figureMotor?.SetEquipmentMovementMultiplier(
                currentMovementMultiplier);
            RefreshSpongeVisual();
            RefreshHud();
        }

        private void RefreshSpongeVisual()
        {
            if (spongeRenderer == null ||
                reservoir == null ||
                config == null)
            {
                return;
            }

            if (spongePropertyBlock == null)
            {
                spongePropertyBlock = new MaterialPropertyBlock();
            }

            spongeRenderer.GetPropertyBlock(spongePropertyBlock);

            Color contentColor =
                reservoir.IsEmpty
                    ? config.DrySpongeColor
                    : reservoir.StoredColor;

            Color displayColor =
                Color.Lerp(
                    config.DrySpongeColor,
                    contentColor,
                    reservoir.NormalizedFill);

            displayColor =
                Color.Lerp(
                    displayColor,
                    new Color(0.92f, 0.08f, 0.62f, 1f),
                    reservoir.MixtureInstability * 0.55f);

            spongePropertyBlock.SetColor(
                BaseColorId,
                displayColor);
            spongePropertyBlock.SetColor(
                ColorId,
                displayColor);
            spongeRenderer.SetPropertyBlock(spongePropertyBlock);
        }

        private void RefreshHud()
        {
            if (reservoir == null || config == null)
            {
                return;
            }

            if (capacityFillImage != null)
            {
                capacityFillImage.fillAmount =
                    reservoir.NormalizedFill;

                capacityFillImage.color =
                    reservoir.IsEmpty
                        ? config.DrySpongeColor
                        : Color.Lerp(
                            reservoir.StoredColor,
                            new Color(0.95f, 0.08f, 0.62f, 1f),
                            reservoir.MixtureInstability * 0.55f);
            }

            if (capacityText != null)
            {
                capacityText.text =
                    $"{reservoir.StoredAmount:0} / " +
                    $"{config.MaximumCapacity:0}";
            }

            if (statusText != null)
            {
                statusText.text =
                    $"{currentTargetType}  •  " +
                    $"KARIŞIM %{reservoir.MixtureInstability * 100f:0}";
            }
        }

        private bool IsUseHeld()
        {
            if (useToolAction != null &&
                useToolAction.action != null)
            {
                return useToolAction.action.IsPressed();
            }

            bool keyboardHeld =
                Keyboard.current != null &&
                Keyboard.current.eKey.isPressed;
            bool gamepadHeld =
                Gamepad.current != null &&
                Gamepad.current.rightTrigger.isPressed;
            return keyboardHeld || gamepadHeld;
        }

        private bool WasDischargePressedThisFrame()
        {
            if (releasePaintAction != null &&
                releasePaintAction.action != null)
            {
                return releasePaintAction.action
                    .WasPressedThisFrame();
            }

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.rKey.wasPressedThisFrame;
            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.leftTrigger.wasPressedThisFrame;
            return keyboardPressed || gamepadPressed;
        }

        private static ISpongeAbsorbableSource
            FindAbsorbableSource(Collider collider)
        {
            MonoBehaviour[] behaviours =
                collider.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is ISpongeAbsorbableSource source)
                {
                    return source;
                }
            }

            return null;
        }

        private static Vector3 GetHitPoint(RaycastHit hit)
        {
            return hit.point != Vector3.zero
                ? hit.point
                : hit.collider.bounds.center;
        }

        private static Vector3 GetHitNormal(RaycastHit hit)
        {
            return hit.normal.sqrMagnitude > 0.0001f
                ? hit.normal
                : Vector3.up;
        }

        private void SetHudVisible(bool visible)
        {
            if (hudRoot != null)
            {
                hudRoot.SetActive(visible);
            }
        }

        private static void EnableAction(
            InputActionReference actionReference)
        {
            actionReference?.action?.Enable();
        }

        private static void DisableAction(
            InputActionReference actionReference)
        {
            actionReference?.action?.Disable();
        }
    }
}
