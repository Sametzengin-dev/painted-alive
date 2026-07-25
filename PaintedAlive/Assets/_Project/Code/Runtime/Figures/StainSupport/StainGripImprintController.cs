using PaintedAlive.Figures.StainMovement;
using PaintedAlive.Figures.StainTraversal;
using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.StainSupport
{
    [DefaultExecutionOrder(14800)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class StainGripImprintController :
        MonoBehaviour
    {
        private const int MaximumProbeHits = 24;

        private readonly RaycastHit[] probeHits =
            new RaycastHit[MaximumProbeHits];

        [Header("References")]
        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private CharacterController characterController;

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkStainCreatureHijackController hijackController;

        [SerializeField]
        private StainSurfaceCrawlController crawlController;

        [SerializeField]
        private StainSpongeCarryController carryController;

        [SerializeField]
        private StainCrackTraversalController crackController;

        [SerializeField]
        private StainGripImprintConfig config;

        [SerializeField]
        private Transform imprintVisual;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private StainCleanGripSurface nearbySurface;

        [SerializeField]
        private bool isImprinting;

        [SerializeField, Range(0f, 1f)]
        private float normalizedProgress;

        [SerializeField]
        private string lastResult = "Leke formu bekleniyor";

        private RaycastHit pendingSurfaceHit;
        private float imprintElapsed;
        private bool characterControllerWasEnabled;
        private Vector3 imprintVisualBaseScale = Vector3.one;

        public StainCleanGripSurface NearbySurface =>
            nearbySurface;
        public bool IsImprinting => isImprinting;
        public float NormalizedProgress => normalizedProgress;
        public string LastResult => lastResult;

        private bool IsStainRoleActive =>
            clarityState != null &&
            clarityState.CurrentLevel ==
                FigureClarityLevel.Stain &&
            roleAuthority != null &&
            !roleAuthority.IsInkPainter;

        private void Awake()
        {
            ResolveReferences();

            if (clarityState == null ||
                characterController == null ||
                roleAuthority == null ||
                hijackController == null ||
                crawlController == null ||
                carryController == null ||
                crackController == null ||
                config == null ||
                imprintVisual == null)
            {
                Debug.LogError(
                    "M32 Stain grip imprint references are " +
                    "incomplete. Run M32 Setup again.",
                    this);
                enabled = false;
                return;
            }

            imprintVisualBaseScale =
                imprintVisual.localScale;
            SetImprintVisualVisible(false);
        }

        private void OnDisable()
        {
            if (isImprinting)
            {
                FinishImprint(
                    false,
                    "M32 denetleyicisi kapandı");
            }

            SetImprintVisualVisible(false);
        }

        private void Update()
        {
            if (isImprinting)
            {
                UpdateImprint(Time.deltaTime);
                return;
            }

            nearbySurface = null;

            if (!CanBeginImprint() || IsEditingText())
            {
                lastResult = roleAuthority != null &&
                    roleAuthority.IsInkPainter
                        ? "Painter rolünde kapalı"
                        : "Leke formu ve serbest hareket bekleniyor";
                return;
            }

            if (!TryFindCleanSurface(
                    out StainCleanGripSurface surface,
                    out RaycastHit hit))
            {
                lastResult =
                    "İşaretlenmiş temiz yüzey aranıyor";
                return;
            }

            nearbySurface = surface;
            pendingSurfaceHit = hit;
            lastResult =
                $"E: {surface.SurfaceLabel} üzerine tutunma izi bırak";

            if (WasUsePressed())
            {
                BeginImprint(surface, hit);
            }
        }

        public void Configure(
            FigureClarityState targetClarity,
            CharacterController targetCharacterController,
            InkPainterRoleAuthority targetRoleAuthority,
            InkStainCreatureHijackController targetHijackController,
            StainSurfaceCrawlController targetCrawlController,
            StainSpongeCarryController targetCarryController,
            StainCrackTraversalController targetCrackController,
            StainGripImprintConfig targetConfig,
            Transform targetImprintVisual)
        {
            clarityState = targetClarity;
            characterController = targetCharacterController;
            roleAuthority = targetRoleAuthority;
            hijackController = targetHijackController;
            crawlController = targetCrawlController;
            carryController = targetCarryController;
            crackController = targetCrackController;
            config = targetConfig;
            imprintVisual = targetImprintVisual;

            if (imprintVisual != null)
            {
                imprintVisualBaseScale =
                    imprintVisual.localScale;
                SetImprintVisualVisible(false);
            }
        }

        private bool CanBeginImprint()
        {
            return IsStainRoleActive &&
                !hijackController.IsHijacking &&
                !carryController.IsCarried &&
                !crackController.IsTraversing &&
                crackController.NearbyPassage == null &&
                crawlController.IsCrawling &&
                crawlController.HasSurface;
        }

        private void BeginImprint(
            StainCleanGripSurface surface,
            RaycastHit hit)
        {
            if (surface == null ||
                !surface.AcceptsGripMarks ||
                !CanBeginImprint())
            {
                lastResult =
                    "Tutunma izi başlatılamadı";
                return;
            }

            nearbySurface = surface;
            pendingSurfaceHit = hit;
            imprintElapsed = 0f;
            normalizedProgress = 0f;
            characterControllerWasEnabled =
                characterController.enabled;

            crawlController.SetExternalSuspended(
                true,
                "M32 tutunma izi hazırlanıyor");

            if (characterController.enabled)
            {
                characterController.enabled = false;
            }

            PositionImprintVisual(
                pendingSurfaceHit.point,
                pendingSurfaceHit.normal);
            SetImprintVisualVisible(true);
            isImprinting = true;
            lastResult = "Temiz yüzeye yayılıyor";
        }

        private void UpdateImprint(float deltaTime)
        {
            if (!IsStainRoleActive ||
                hijackController.IsHijacking ||
                carryController.IsCarried ||
                crackController.IsTraversing ||
                nearbySurface == null ||
                !nearbySurface.AcceptsGripMarks)
            {
                FinishImprint(
                    false,
                    "Tutunma izi güvenli biçimde iptal edildi");
                return;
            }

            imprintElapsed += Mathf.Max(0f, deltaTime);
            normalizedProgress = Mathf.Clamp01(
                imprintElapsed /
                Mathf.Max(0.1f, config.ImprintDuration));
            float eased =
                normalizedProgress *
                normalizedProgress *
                (3f - 2f * normalizedProgress);

            if (imprintVisual != null)
            {
                imprintVisual.localScale =
                    imprintVisualBaseScale *
                    Mathf.Lerp(0.18f, 1f, eased);
            }

            if (normalizedProgress >= 1f)
            {
                CreateGripMark();
                FinishImprint(
                    true,
                    "Geçici tutunma izi oluşturuldu");
            }
        }

        private void CreateGripMark()
        {
            TrimOldestMarks();
            GameObject markObject =
                new GameObject("M32_StainGripMark");
            StainGripMark mark =
                markObject.AddComponent<StainGripMark>();
            mark.Initialize(
                pendingSurfaceHit.point,
                pendingSurfaceHit.normal,
                config);
        }

        private void TrimOldestMarks()
        {
            int allowedBeforeCreate =
                Mathf.Max(0, config.MaximumActiveMarks - 1);

            while (StainGripMark.ActiveMarks.Count >
                   allowedBeforeCreate)
            {
                StainGripMark oldest =
                    StainGripMark.ActiveMarks[0];

                if (oldest == null)
                {
                    return;
                }

                oldest.ExpireNow();
            }
        }

        private void FinishImprint(
            bool completed,
            string result)
        {
            isImprinting = false;
            normalizedProgress = completed ? 1f : 0f;
            imprintElapsed = 0f;
            nearbySurface = null;
            SetImprintVisualVisible(false);

            bool mayRestoreController =
                characterController != null &&
                characterControllerWasEnabled &&
                characterController.gameObject.activeInHierarchy &&
                carryController != null &&
                !carryController.IsCarried &&
                hijackController != null &&
                !hijackController.IsHijacking &&
                crackController != null &&
                !crackController.IsTraversing;

            if (mayRestoreController)
            {
                characterController.enabled = true;
            }

            if (crawlController != null)
            {
                crawlController.SetExternalSuspended(
                    false,
                    result);
            }

            lastResult = result;
        }

        private bool TryFindCleanSurface(
            out StainCleanGripSurface surface,
            out RaycastHit bestHit)
        {
            surface = null;
            bestHit = default;
            Vector3 normal = crawlController.SurfaceNormal;

            if (normal.sqrMagnitude < 0.001f)
            {
                return false;
            }

            normal.Normalize();
            Vector3 origin =
                transform.position + normal * 0.48f;
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                0.2f,
                -normal,
                probeHits,
                config.SurfaceProbeDistance,
                config.SurfaceMask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = probeHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearestDistance ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.GetComponentInParent<
                        StainGripMark>() != null)
                {
                    continue;
                }

                StainCleanGripSurface candidate =
                    hit.collider.GetComponentInParent<
                        StainCleanGripSurface>();

                if (candidate == null ||
                    !candidate.AcceptsGripMarks)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                surface = candidate;
                bestHit = hit;
            }

            return surface != null;
        }

        private void PositionImprintVisual(
            Vector3 point,
            Vector3 normal)
        {
            if (imprintVisual == null)
            {
                return;
            }

            Vector3 safeNormal =
                normal.sqrMagnitude > 0.001f
                    ? normal.normalized
                    : Vector3.up;
            imprintVisual.position =
                point + safeNormal * 0.025f;
            imprintVisual.rotation =
                Quaternion.FromToRotation(
                    Vector3.up,
                    safeNormal);
            imprintVisual.localScale =
                imprintVisualBaseScale * 0.18f;
        }

        private void SetImprintVisualVisible(bool visible)
        {
            if (imprintVisual != null &&
                imprintVisual.gameObject.activeSelf != visible)
            {
                imprintVisual.gameObject.SetActive(visible);
            }
        }

        private void ResolveReferences()
        {
            clarityState ??=
                GetComponent<FigureClarityState>();
            characterController ??=
                GetComponent<CharacterController>();
            roleAuthority ??=
                FindFirstObjectByType<
                    InkPainterRoleAuthority>();
            hijackController ??=
                GetComponent<
                    InkStainCreatureHijackController>();
            crawlController ??=
                GetComponent<
                    StainSurfaceCrawlController>();
            carryController ??=
                GetComponent<
                    StainSpongeCarryController>();
            crackController ??=
                GetComponent<
                    StainCrackTraversalController>();
        }

        private static bool WasUsePressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                keyboard.eKey.wasPressedThisFrame;
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

            Component[] components =
                selected.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component == null)
                {
                    continue;
                }

                string typeName =
                    component.GetType().Name;

                if (typeName == "InputField" ||
                    typeName == "TMP_InputField")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
