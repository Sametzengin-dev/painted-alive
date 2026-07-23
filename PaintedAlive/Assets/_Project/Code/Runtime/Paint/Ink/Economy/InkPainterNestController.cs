using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Ink.Economy
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class InkPainterNestController : MonoBehaviour
    {
        private const int MaximumSurfaceHits = 24;
        private const int MaximumProtectedHits = 32;
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        private readonly RaycastHit[] surfaceHits =
            new RaycastHit[MaximumSurfaceHits];
        private readonly Collider[] protectedHits =
            new Collider[MaximumProtectedHits];

        [Header("References")]
        [SerializeField]
        private Camera painterCamera;

        [SerializeField]
        private InkSystemManager inkManager;

        [SerializeField]
        private InkPainterEconomy economy;

        [SerializeField]
        private InkPainterEconomyConfig config;

        [SerializeField]
        private InkGlyphLoadoutController loadoutController;

        [Header("Physics")]
        [SerializeField]
        private LayerMask surfaceMask = Physics.DefaultRaycastLayers;

        [Header("Preview")]
        [SerializeField]
        private GameObject previewRoot;

        [SerializeField]
        private Renderer previewRenderer;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool isCasting;

        [SerializeField]
        private bool targetValid;

        [SerializeField]
        private float castProgress;

        [SerializeField]
        private Vector3 targetPoint;

        [SerializeField]
        private Vector3 targetNormal = Vector3.up;

        [SerializeField]
        private string lastResult = "Press and hold F7";

        private MaterialPropertyBlock previewProperties;
        private float castStartedAt;
        private float nextPlacementTime;
        private bool inputWasHeld;

        public Camera PainterCamera => painterCamera;
        public InkSystemManager InkManager => inkManager;
        public InkPainterEconomy Economy => economy;
        public bool IsCasting => isCasting;
        public bool TargetValid => targetValid;
        public float CastProgress => castProgress;
        public Vector3 TargetPoint => targetPoint;
        public string LastResult => lastResult;
        public InkGlyphLoadoutController LoadoutController =>
            loadoutController;

        private void Awake()
        {
            inkManager ??= InkSystemManager.ActiveInstance;
            economy ??= InkPainterEconomy.ActiveInstance;
            SetPreviewActive(false);

            if (painterCamera == null ||
                inkManager == null ||
                economy == null ||
                config == null)
            {
                Debug.LogError(
                    "InkPainterNestController requires Camera, manager, " +
                    "economy and config.",
                    this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            CancelCast("Controller disabled");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || IsEditingText())
            {
                if (isCasting)
                {
                    CancelCast("Text input active");
                }

                return;
            }

            bool keyHeld = keyboard.f7Key.isPressed;
            bool keyPressed = keyboard.f7Key.wasPressedThisFrame;
            bool keyReleased = keyboard.f7Key.wasReleasedThisFrame ||
                (inputWasHeld && !keyHeld);
            inputWasHeld = keyHeld;

            if (keyPressed)
            {
                TryBeginCast();
            }

            if (isCasting && keyHeld)
            {
                UpdateCast();
            }

            if (isCasting && keyReleased)
            {
                FinishCast();
            }
        }

        public void Configure(
            Camera sourceCamera,
            InkSystemManager manager,
            InkPainterEconomy painterEconomy,
            InkPainterEconomyConfig economyConfig,
            GameObject targetPreview,
            Renderer targetPreviewRenderer)
        {
            painterCamera = sourceCamera;
            inkManager = manager;
            economy = painterEconomy;
            config = economyConfig;
            previewRoot = targetPreview;
            previewRenderer = targetPreviewRenderer;
            SetPreviewActive(false);
        }

        public void ConfigureLoadouts(
            InkGlyphLoadoutController controller)
        {
            loadoutController = controller;
        }

        [ContextMenu("Debug/Begin Ink Nest Cast")]
        public void BeginDebugCast()
        {
            if (Application.isPlaying)
            {
                TryBeginCast();
            }
        }

        private void TryBeginCast()
        {
            if (!Application.isPlaying || isCasting)
            {
                return;
            }

            if (Time.time < nextPlacementTime)
            {
                lastResult = "Placement cooling down";
                return;
            }

            if (economy.PossessionActive)
            {
                lastResult = "Cannot paint while possessing";
                return;
            }

            float selectedCost = ResolvePigmentCost();
            int selectedComplexity = ResolveComplexityCost();

            if (!economy.CanAfford(selectedCost))
            {
                lastResult = "Not enough pigment";
                return;
            }

            if (!economy.CanCreateNest(selectedComplexity))
            {
                lastResult = "Ink complexity budget full";
                return;
            }

            isCasting = true;
            targetValid = false;
            castProgress = 0f;
            castStartedAt = Time.time;
            lastResult = "Aiming";
            economy.SetCastInProgress(true);
            SetPreviewActive(true);
            UpdateCast();
        }

        private void UpdateCast()
        {
            castProgress = Mathf.Clamp01(
                (Time.time - castStartedAt) /
                Mathf.Max(0.01f, config.MinimumCastDuration));
            targetValid = TryResolveTarget(
                out targetPoint,
                out targetNormal,
                out string reason);
            lastResult = targetValid
                ? (castProgress >= 1f ? "Release to animate" : "Sketching")
                : reason;
            RefreshPreview();
        }

        private void FinishCast()
        {
            if (castProgress < 1f)
            {
                CancelCast("Released before sketch completed");
                return;
            }

            string invalidReason = string.IsNullOrEmpty(lastResult)
                ? "Invalid nest placement"
                : lastResult;

            if (!targetValid ||
                !TryResolveTarget(
                    out targetPoint,
                    out targetNormal,
                    out invalidReason))
            {
                CancelCast(invalidReason);
                return;
            }

            int selectedComplexity = ResolveComplexityCost();

            if (!economy.CanCreateNest(selectedComplexity))
            {
                CancelCast("Ink complexity budget full");
                return;
            }

            float cost = ResolvePigmentCost();

            if (!economy.TrySpend(cost, "Ink nest animated"))
            {
                CancelCast("Not enough pigment");
                return;
            }

            Vector3 facing = Vector3.ProjectOnPlane(
                painterCamera.transform.forward,
                targetNormal).normalized;

            InkCreatureDefinition selectedDefinition =
                loadoutController != null
                    ? loadoutController.ActiveCreatureDefinition
                    : null;

            if (!inkManager.TrySpawnCreature(
                    selectedDefinition,
                    targetPoint,
                    targetNormal,
                    facing,
                    out _,
                    out _))
            {
                economy.Refund(cost, "Nest spawn rejected; pigment refunded");
                CancelCast("Nest spawn rejected by Ink Manager");
                return;
            }

            nextPlacementTime = Time.time + config.PlacementCooldown;
            string displayName =
                loadoutController != null &&
                loadoutController.ActiveLoadout != null
                    ? loadoutController.ActiveLoadout.DisplayName
                    : "Lekebacak";
            EndCast(displayName + " nest created");
        }

        private float ResolvePigmentCost()
        {
            return loadoutController != null &&
                loadoutController.ActiveLoadout != null
                    ? loadoutController.ActivePigmentCost
                    : config != null
                        ? config.NestPlacementCost
                        : 35f;
        }

        private int ResolveComplexityCost()
        {
            return loadoutController != null &&
                loadoutController.ActiveLoadout != null
                    ? loadoutController.ActiveComplexityCost
                    : config != null
                        ? config.LekebacakComplexity
                        : 2;
        }

        private bool TryResolveTarget(
            out Vector3 point,
            out Vector3 normal,
            out string reason)
        {
            point = default;
            normal = Vector3.up;
            reason = "No valid surface";

            if (painterCamera == null)
            {
                reason = "Painter camera missing";
                return false;
            }

            Ray ray = painterCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                surfaceHits,
                config.MaximumPlacementDistance,
                surfaceMask,
                QueryTriggerInteraction.Ignore);
            RaycastHit bestHit = default;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = surfaceHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearestDistance ||
                    hit.collider.GetComponentInParent<FigureMotor>() != null ||
                    hit.collider.GetComponentInParent<InkCreatureRuntime>() !=
                    null)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            if (bestHit.collider == null)
            {
                return false;
            }

            point = bestHit.point;
            normal = bestHit.normal.sqrMagnitude > 0.001f
                ? bestHit.normal.normalized
                : Vector3.up;

            if (Vector3.Angle(normal, Vector3.up) >
                config.MaximumSurfaceAngle)
            {
                reason = "Surface is too steep";
                return false;
            }

            int protectedCount = Physics.OverlapSphereNonAlloc(
                point,
                config.ProtectedFigureRadius,
                protectedHits,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < protectedCount; i++)
            {
                Collider candidate = protectedHits[i];

                if (candidate != null &&
                    candidate.GetComponentInParent<FigureMotor>() != null)
                {
                    reason = "Figure protected zone";
                    return false;
                }
            }

            var surfaces = InkSurface.ActiveSurfaces;
            float minimumSpacingSquared =
                config.MinimumNestSpacing * config.MinimumNestSpacing;

            for (int i = 0; i < surfaces.Count; i++)
            {
                InkSurface surface = surfaces[i];

                if (surface != null &&
                    surface.IsInitialized &&
                    (surface.transform.position - point).sqrMagnitude <
                    minimumSpacingSquared)
                {
                    reason = "Too close to another Ink nest";
                    return false;
                }
            }

            reason = "Valid";
            return true;
        }

        private void RefreshPreview()
        {
            if (previewRoot == null)
            {
                return;
            }

            previewRoot.transform.SetPositionAndRotation(
                targetPoint + targetNormal * 0.025f,
                Quaternion.FromToRotation(Vector3.up, targetNormal));
            float radius = config.PreviewRadius *
                Mathf.Lerp(0.72f, 1f, castProgress);
            previewRoot.transform.localScale = new Vector3(
                radius * 2f,
                0.012f,
                radius * 2f);

            if (previewRenderer == null)
            {
                return;
            }

            Color color = targetValid
                ? Color.Lerp(
                    new Color(0.08f, 0.02f, 0.12f, 0.38f),
                    new Color(0.5f, 0.08f, 0.72f, 0.72f),
                    castProgress)
                : new Color(0.9f, 0.06f, 0.04f, 0.55f);
            previewProperties ??= new MaterialPropertyBlock();
            previewRenderer.GetPropertyBlock(previewProperties);
            previewProperties.SetColor(BaseColorId, color);
            previewProperties.SetColor(ColorId, color);
            previewRenderer.SetPropertyBlock(previewProperties);
        }

        private void CancelCast(string reason)
        {
            if (!isCasting)
            {
                SetPreviewActive(false);
                return;
            }

            EndCast(reason);
        }

        private void EndCast(string result)
        {
            isCasting = false;
            targetValid = false;
            castProgress = 0f;
            lastResult = string.IsNullOrWhiteSpace(result)
                ? "Cancelled"
                : result;
            economy?.SetCastInProgress(false);
            SetPreviewActive(false);
        }

        private void SetPreviewActive(bool active)
        {
            if (previewRoot != null && previewRoot.activeSelf != active)
            {
                previewRoot.SetActive(active);
            }
        }

        private static bool IsEditingText()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null
                ? eventSystem.currentSelectedGameObject
                : null;

            if (selected == null)
            {
                return false;
            }

            return selected.GetComponent("TMP_InputField") != null ||
                selected.GetComponent("InputField") != null;
        }
    }
}
