using PaintedAlive.Figures.StainMovement;
using PaintedAlive.Figures.StainTraversal;
using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.StainSupport
{
    [DefaultExecutionOrder(14900)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class StainDirectionSignalController :
        MonoBehaviour
    {
        private const int MaximumRayHits = 32;

        private readonly RaycastHit[] rayHits =
            new RaycastHit[MaximumRayHits];

        [Header("References")]
        [SerializeField]
        private FigureClarityState clarityState;

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
        private StainGripImprintController imprintController;

        [SerializeField]
        private Camera figureCamera;

        [SerializeField]
        private StainDirectionSignalConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float nextPlacementTime;

        [SerializeField]
        private string lastResult = "Leke formu bekleniyor";

        public string LastResult => lastResult;
        public float CooldownRemaining =>
            Mathf.Max(0f, nextPlacementTime - Time.unscaledTime);

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
                roleAuthority == null ||
                hijackController == null ||
                crawlController == null ||
                carryController == null ||
                crackController == null ||
                imprintController == null ||
                figureCamera == null ||
                config == null)
            {
                Debug.LogError(
                    "M33 Stain direction signal references are " +
                    "incomplete. Run M33 Setup again.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!CanPlaceSignal())
            {
                lastResult = roleAuthority != null &&
                    roleAuthority.IsInkPainter
                        ? "Painter rolünde kapalı"
                        : "Leke formu ve serbest hareket bekleniyor";
                return;
            }

            if (Time.unscaledTime < nextPlacementTime)
            {
                lastResult =
                    $"Yön sinyali beklemede " +
                    $"({CooldownRemaining:F1}s)";
                return;
            }

            lastResult = "Q: baktığın yüzeye yön sinyali bırak";

            if (WasSignalPressed() && !IsEditingText())
            {
                TryPlaceSignal();
            }
        }

        public void Configure(
            FigureClarityState targetClarity,
            InkPainterRoleAuthority targetRoleAuthority,
            InkStainCreatureHijackController targetHijackController,
            StainSurfaceCrawlController targetCrawlController,
            StainSpongeCarryController targetCarryController,
            StainCrackTraversalController targetCrackController,
            StainGripImprintController targetImprintController,
            Camera targetCamera,
            StainDirectionSignalConfig targetConfig)
        {
            clarityState = targetClarity;
            roleAuthority = targetRoleAuthority;
            hijackController = targetHijackController;
            crawlController = targetCrawlController;
            carryController = targetCarryController;
            crackController = targetCrackController;
            imprintController = targetImprintController;
            figureCamera = targetCamera;
            config = targetConfig;
        }

        private bool CanPlaceSignal()
        {
            return IsStainRoleActive &&
                !hijackController.IsHijacking &&
                !carryController.IsCarried &&
                !crackController.IsTraversing &&
                !imprintController.IsImprinting &&
                crawlController.IsCrawling;
        }

        private void TryPlaceSignal()
        {
            if (!TryFindSurface(out RaycastHit hit))
            {
                lastResult =
                    "Kamera merkezinde işaretlenebilir yüzey yok";
                return;
            }

            Vector3 direction =
                Vector3.ProjectOnPlane(
                    figureCamera.transform.forward,
                    hit.normal).normalized;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction =
                    Vector3.ProjectOnPlane(
                        figureCamera.transform.up,
                        hit.normal).normalized;
            }

            if (direction.sqrMagnitude < 0.001f)
            {
                direction =
                    Vector3.Cross(
                        hit.normal,
                        figureCamera.transform.right).normalized;
            }

            TrimOldestSignals();
            GameObject signalObject =
                new GameObject("M33_StainDirectionSignal");
            StainDirectionSignal signal =
                signalObject.AddComponent<
                    StainDirectionSignal>();
            signal.Initialize(
                hit.point,
                hit.normal,
                direction,
                config);
            nextPlacementTime =
                Time.unscaledTime +
                config.PlacementCooldown;
            lastResult = "Geçici yön sinyali bırakıldı";

            Debug.Log(
                "[M33] Leke yön sinyali oluşturuldu. " +
                $"Point={hit.point}, Direction={direction}",
                signal);
        }

        private bool TryFindSurface(out RaycastHit bestHit)
        {
            bestHit = default;
            Vector3 origin =
                figureCamera.transform.position;
            Vector3 direction =
                figureCamera.transform.forward;
            int count = Physics.RaycastNonAlloc(
                origin,
                direction,
                rayHits,
                config.MaximumPlacementDistance,
                config.SurfaceMask,
                QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = rayHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearest ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.GetComponentInParent<
                        StainDirectionSignal>() != null)
                {
                    continue;
                }

                nearest = hit.distance;
                bestHit = hit;
            }

            return bestHit.collider != null;
        }

        private void TrimOldestSignals()
        {
            int allowedBeforeCreate =
                Mathf.Max(
                    0,
                    config.MaximumActiveSignals - 1);

            while (StainDirectionSignal.ActiveSignals.Count >
                   allowedBeforeCreate)
            {
                StainDirectionSignal oldest =
                    StainDirectionSignal.ActiveSignals[0];

                if (oldest == null)
                {
                    return;
                }

                oldest.ExpireNow();
            }
        }

        private void ResolveReferences()
        {
            clarityState ??=
                GetComponent<FigureClarityState>();
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
            imprintController ??=
                GetComponent<
                    StainGripImprintController>();
            figureCamera ??=
                GetComponentInChildren<Camera>(true);

            if (figureCamera == null &&
                roleAuthority != null)
            {
                figureCamera =
                    roleAuthority.ActiveRoleCamera;
            }
        }

        private static bool WasSignalPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                keyboard.qKey.wasPressedThisFrame;
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
