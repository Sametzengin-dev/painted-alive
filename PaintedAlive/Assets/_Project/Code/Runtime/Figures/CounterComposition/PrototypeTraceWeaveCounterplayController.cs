using System;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(190)]
    [DisallowMultipleComponent]
    public sealed class PrototypeTraceWeaveCounterplayController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeTraceWeaveController traceWeave;

        [SerializeField]
        private PrototypeTraceWeaveTeamRouteController teamRoute;

        [SerializeField] private Transform figureRoot;
        [SerializeField] private MonoBehaviour paletteKnifeController;

        [Header("Painter Targeting")]
        [SerializeField, Min(0.02f)]
        private float painterAimRadius = 0.18f;

        [SerializeField, Min(1f)]
        private float painterMaximumDistance = 120f;

        [SerializeField]
        private LayerMask painterAimMask = ~0;

        [Header("Watercolor")]
        [SerializeField, Min(0.1f)]
        private float watercolorBendRadius = 1.45f;

        [SerializeField, Min(0f)]
        private float watercolorBendStrength = 0.68f;

        [Header("Oil Paint")]
        [SerializeField, Min(1f)]
        private float oilAddedLoadKg = 100f;

        [SerializeField, Min(0.1f)]
        private float oilLoadSeconds = 2f;

        [Header("Palette Knife")]
        [SerializeField, Min(0.2f)]
        private float paletteKnifeRouteReach = 2.25f;

        [SerializeField, Range(0.08f, 0.35f)]
        private float safeEndpointFraction = 0.22f;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Runtime Read Only")]
        [SerializeField] private bool painterRoleActive;
        [SerializeField] private bool figureRoleActive;
        [SerializeField] private bool routeAvailable;
        [SerializeField] private bool painterAimValid;
        [SerializeField] private bool painterCameraResolved;
        [SerializeField] private bool paletteKnifeActive;
        [SerializeField] private bool paletteKnifeInputResolved;
        [SerializeField] private int routeBindCount;
        [SerializeField] private int watercolorAttemptCount;
        [SerializeField] private int watercolorSuccessCount;
        [SerializeField] private int oilAttemptCount;
        [SerializeField] private int oilSuccessCount;
        [SerializeField] private int eraserAttemptCount;
        [SerializeField] private int eraserSuccessCount;
        [SerializeField] private int paletteKnifeAttemptCount;
        [SerializeField] private int paletteKnifeConnectionCount;
        [SerializeField] private int paletteKnifeBreakCount;
        [SerializeField] private int rejectedCounterplayCount;
        [SerializeField] private Vector3 painterAimPoint;
        [SerializeField] private float painterAimRouteDistance = -1f;
        [SerializeField] private string resolvedPainterRole = "Unknown";
        [SerializeField] private string resolvedFigureRole = "Unknown";
        [SerializeField] private string paletteKnifeActivityContract =
            "Unresolved";
        [SerializeField] private string lastAction =
            "Aktif İz Dokuma bekleniyor.";

        private readonly RaycastHit[] aimHits =
            new RaycastHit[32];

        private InputAction watercolorAction;
        private InputAction oilAction;
        private InputAction eraserAction;

        private PrototypeTraceWeaveRoute boundRoute;
        private Camera activeCamera;

        private InputActionReference paletteUseToolAction;
        private Transform paletteToolOrigin;

        public bool PainterRoleActive => painterRoleActive;
        public bool FigureRoleActive => figureRoleActive;
        public bool RouteAvailable => routeAvailable;
        public bool PainterAimValid => painterAimValid;
        public bool PainterCameraResolved => painterCameraResolved;
        public bool PaletteKnifeActive => paletteKnifeActive;
        public bool PaletteKnifeInputResolved => paletteKnifeInputResolved;
        public int RouteBindCount => routeBindCount;
        public int WatercolorAttemptCount => watercolorAttemptCount;
        public int WatercolorSuccessCount => watercolorSuccessCount;
        public int OilAttemptCount => oilAttemptCount;
        public int OilSuccessCount => oilSuccessCount;
        public int EraserAttemptCount => eraserAttemptCount;
        public int EraserSuccessCount => eraserSuccessCount;
        public int PaletteKnifeAttemptCount => paletteKnifeAttemptCount;
        public int PaletteKnifeConnectionCount => paletteKnifeConnectionCount;
        public int PaletteKnifeBreakCount => paletteKnifeBreakCount;
        public int RejectedCounterplayCount => rejectedCounterplayCount;
        public Vector3 PainterAimPoint => painterAimPoint;
        public float PainterAimRouteDistance => painterAimRouteDistance;
        public string PaletteKnifeActivityContract =>
            paletteKnifeActivityContract;
        public string LastAction => lastAction;

        public bool WatercolorCounterplayEnabled => true;
        public bool OilPaintCounterplayEnabled => true;
        public bool EraserCounterplayEnabled => true;
        public bool PaletteKnifeRouteEditingEnabled => true;
        public bool PaletteKnifeUsesExistingUseToolActionReadOnly => true;
        public bool AddsSecondPaletteKnifeInputOwner => false;
        public bool PainterPrototypeBindingsTemporary => true;
        public string WatercolorPrototypeBinding => "F4";
        public string OilPaintPrototypeBinding => "F10";
        public string EraserPrototypeBinding => "F12";
        public bool FinalPainterMaterialToolbarIntegrated => false;
        public bool SharedPainterMaterialActionsStayEnabledWithoutTraceRoute => true;
        public bool AllowsM52SharedMaterialInputReadOnly => true;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeTraceWeaveController configuredTraceWeave,
            PrototypeTraceWeaveTeamRouteController configuredTeamRoute,
            Transform configuredFigureRoot,
            MonoBehaviour configuredPaletteKnifeController,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            traceWeave = configuredTraceWeave;
            teamRoute = configuredTeamRoute;
            figureRoot = configuredFigureRoot;
            paletteKnifeController =
                configuredPaletteKnifeController;

            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolvePaletteKnifeContracts();
            EnsurePainterActions();
            ResolveRoute();
            RefreshHud();
        }

        private void Awake()
        {
            ResolvePaletteKnifeContracts();
            EnsurePainterActions();
        }

        private void OnEnable()
        {
            ResolvePaletteKnifeContracts();
            EnsurePainterActions();
        }

        private void Update()
        {
            RefreshRoles();
            ResolveRoute();
            RefreshPainterActionState();
            RefreshPaletteKnifeState();

            if (
                painterRoleActive &&
                routeAvailable
            )
            {
                ResolveActiveCamera();
                RefreshPainterAim();
                ProcessPainterInputs();
            }
            else
            {
                painterAimValid = false;
                painterAimRouteDistance = -1f;
            }

            if (
                figureRoleActive &&
                routeAvailable
            )
            {
                ProcessPaletteKnifeInput();
            }

            RefreshHud();
        }

        public bool ForceWatercolorBendForPrototype()
        {
            if (
                !TryResolveForcePoint(
                    out Vector3 point)
            )
            {
                return Reject(
                    "Force Suluboya için aktif rota yok.");
            }

            Vector3 direction =
                activeCamera != null
                    ? activeCamera.transform.right
                    : Vector3.right;

            return ApplyWatercolor(
                point,
                direction,
                "Editor force");
        }

        public bool ForceOilLoadForPrototype()
        {
            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute ||
                teamRoute == null
            )
            {
                return Reject(
                    "Force Yağlı Boya için aktif rota / TeamRoute yok.");
            }

            return ApplyOilLoad(
                "Editor force");
        }

        public bool ForceEraserForPrototype()
        {
            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute
            )
            {
                return Reject(
                    "Force Silgi için aktif rota yok.");
            }

            return ApplyEraser(
                "Editor force");
        }

        public bool ForcePaletteKnifeConnectionForPrototype()
        {
            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute ||
                boundRoute.RoutePointCount < 3
            )
            {
                return Reject(
                    "Force Palet Bıçağı için aktif rota yok.");
            }

            int targetIndex =
                Mathf.Clamp(
                    1,
                    0,
                    boundRoute.RoutePointCount - 1);

            if (
                !boundRoute.TryGetRoutePoint(
                    targetIndex,
                    out Vector3 point)
            )
            {
                return Reject(
                    "Force Palet Bıçağı hedef noktası çözülemedi.");
            }

            return ApplyPaletteKnifeCut(
                point,
                maximumDistance:
                    0.35f,
                source:
                    "Editor force safe endpoint");
        }

        private void EnsurePainterActions()
        {
            if (watercolorAction == null)
            {
                watercolorAction =
                    new InputAction(
                        "M51.2 Watercolor Bend",
                        InputActionType.Button,
                        "<Keyboard>/f4");
            }

            if (oilAction == null)
            {
                oilAction =
                    new InputAction(
                        "M51.2 Oil Load",
                        InputActionType.Button,
                        "<Keyboard>/f10");
            }

            if (eraserAction == null)
            {
                eraserAction =
                    new InputAction(
                        "M51.2 Eraser Remove",
                        InputActionType.Button,
                        "<Keyboard>/f12");
            }
        }

        private void RefreshRoles()
        {
            painterRoleActive =
                PrototypeRoleAuthorityResolver.IsPainter(
                    out resolvedPainterRole);

            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedFigureRole);
        }

        private void ResolveRoute()
        {
            PrototypeTraceWeaveRoute candidate =
                traceWeave != null
                    ? traceWeave.ActiveRoute
                    : null;

            if (
                candidate != null &&
                candidate.Expired
            )
            {
                candidate = null;
            }

            if (candidate == boundRoute)
            {
                routeAvailable =
                    boundRoute != null &&
                    boundRoute.ActiveRoute;

                return;
            }

            boundRoute = candidate;

            routeAvailable =
                boundRoute != null &&
                boundRoute.ActiveRoute;

            painterAimValid = false;
            painterAimRouteDistance = -1f;

            if (boundRoute != null)
            {
                routeBindCount++;
                lastAction =
                    "İz Dokuma karşı-oyun hedefi bağlandı.";
            }
            else
            {
                lastAction =
                    "Aktif İz Dokuma bekleniyor.";
            }
        }

        private void RefreshPainterActionState()
        {
            // M51.2 owns the temporary Painter material actions.
            // Keep them enabled for the whole Painter role so later
            // context systems (M52+) can read the same actions without
            // creating a second keyboard owner.
            bool shouldEnable =
                painterRoleActive;

            SetActionState(
                watercolorAction,
                shouldEnable);

            SetActionState(
                oilAction,
                shouldEnable);

            SetActionState(
                eraserAction,
                shouldEnable);
        }

        private static void SetActionState(
            InputAction action,
            bool enabled)
        {
            if (action == null)
            {
                return;
            }

            if (
                enabled &&
                !action.enabled
            )
            {
                action.Enable();
            }
            else if (
                !enabled &&
                action.enabled
            )
            {
                action.Disable();
            }
        }

        private void ResolveActiveCamera()
        {
            if (
                activeCamera != null &&
                activeCamera.isActiveAndEnabled &&
                activeCamera.gameObject.activeInHierarchy
            )
            {
                painterCameraResolved = true;
                return;
            }

            Camera main =
                Camera.main;

            if (
                main != null &&
                main.isActiveAndEnabled &&
                main.gameObject.activeInHierarchy
            )
            {
                activeCamera = main;
                painterCameraResolved = true;
                return;
            }

            Camera[] cameras =
                UnityEngine.Object.FindObjectsByType<
                    Camera>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            Camera best = null;
            int bestScore = int.MinValue;

            for (int index = 0;
                 index < cameras.Length;
                 index++)
            {
                Camera candidate =
                    cameras[index];

                if (
                    candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.isActiveAndEnabled
                )
                {
                    continue;
                }

                int score = 0;

                if (
                    candidate.name.IndexOf(
                        "Painter",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 300;
                }

                if (candidate.CompareTag("MainCamera"))
                {
                    score += 100;
                }

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            activeCamera = best;
            painterCameraResolved =
                activeCamera != null;
        }

        private void RefreshPainterAim()
        {
            painterAimValid = false;
            painterAimRouteDistance = -1f;

            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute ||
                activeCamera == null ||
                Mouse.current == null
            )
            {
                return;
            }

            Vector2 pointer =
                Mouse.current.position.ReadValue();

            Ray ray =
                activeCamera.ScreenPointToRay(
                    pointer);

            int hitCount =
                Physics.SphereCastNonAlloc(
                    ray,
                    painterAimRadius,
                    aimHits,
                    painterMaximumDistance,
                    painterAimMask,
                    QueryTriggerInteraction.Ignore);

            float nearest =
                float.PositiveInfinity;

            for (int index = 0;
                 index < hitCount;
                 index++)
            {
                RaycastHit hit =
                    aimHits[index];

                if (
                    hit.collider == null ||
                    !IsInside(
                        hit.collider.transform,
                        boundRoute.transform)
                )
                {
                    continue;
                }

                if (
                    hit.distance <
                    nearest
                )
                {
                    nearest =
                        hit.distance;

                    painterAimPoint =
                        hit.point;

                    painterAimValid = true;
                }
            }

            if (
                !painterAimValid
            )
            {
                return;
            }

            if (
                boundRoute.TryGetNearestRouteSample(
                    painterAimPoint,
                    out _,
                    out _,
                    out float distance,
                    out _)
            )
            {
                painterAimRouteDistance =
                    distance;
            }
        }

        private void ProcessPainterInputs()
        {
            if (
                watercolorAction != null &&
                watercolorAction.WasPressedThisFrame()
            )
            {
                watercolorAttemptCount++;

                if (!painterAimValid)
                {
                    Reject(
                        "Suluboya: imleç İz Dokuma üzerinde değil.");

                    return;
                }

                Vector2 pointer =
                    Mouse.current != null
                        ? Mouse.current.position.ReadValue()
                        : Vector2.zero;

                float centerX =
                    activeCamera != null
                        ? activeCamera.pixelRect.center.x
                        : Screen.width *
                          0.5f;

                float sign =
                    pointer.x <
                    centerX
                        ? -1f
                        : 1f;

                Vector3 direction =
                    activeCamera != null
                        ? activeCamera.transform.right *
                          sign
                        : Vector3.right *
                          sign;

                ApplyWatercolor(
                    painterAimPoint,
                    direction,
                    "Painter F4");

                return;
            }

            if (
                oilAction != null &&
                oilAction.WasPressedThisFrame()
            )
            {
                oilAttemptCount++;

                if (!painterAimValid)
                {
                    Reject(
                        "Yağlı Boya: imleç İz Dokuma üzerinde değil.");

                    return;
                }

                ApplyOilLoad(
                    "Painter F10");

                return;
            }

            if (
                eraserAction != null &&
                eraserAction.WasPressedThisFrame()
            )
            {
                eraserAttemptCount++;

                if (!painterAimValid)
                {
                    Reject(
                        "Silgi: imleç İz Dokuma üzerinde değil.");

                    return;
                }

                ApplyEraser(
                    "Painter F12");
            }
        }

        private bool ApplyWatercolor(
            Vector3 point,
            Vector3 direction,
            string source)
        {
            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute
            )
            {
                return Reject(
                    "Suluboya: aktif rota yok.");
            }

            if (
                !boundRoute.TryApplyWatercolorBend(
                    point,
                    direction,
                    watercolorBendRadius,
                    watercolorBendStrength,
                    out string reason)
            )
            {
                return Reject(
                    "Suluboya reddedildi: " +
                    reason);
            }

            watercolorSuccessCount++;

            lastAction =
                "Suluboya İz Dokumayı yana büktü; A/B ankrajları korundu.";

            Debug.Log(
                "[M51.2 Watercolor Counterplay]\n" +
                $"Source={source}\n" +
                $"BendRadius={watercolorBendRadius:F2}\n" +
                $"BendStrength={watercolorBendStrength:F2}\n" +
                "EndpointsPreserved=True\n" +
                "FreeAirExtensionCreated=False",
                this);

            return true;
        }

        private bool ApplyOilLoad(
            string source)
        {
            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute ||
                teamRoute == null
            )
            {
                return Reject(
                    "Yağlı Boya: aktif rota / yük sistemi yok.");
            }

            teamRoute.ApplySimulatedHeavyLoad(
                oilAddedLoadKg,
                oilLoadSeconds);

            oilSuccessCount++;

            lastAction =
                $"+{oilAddedLoadKg:F0} kg Yağlı Boya yükü M51.1 stres hattına aktarıldı.";

            Debug.Log(
                "[M51.2 Oil Paint Counterplay]\n" +
                $"Source={source}\n" +
                $"AddedLoadKg={oilAddedLoadKg:F1}\n" +
                $"DurationSeconds={oilLoadSeconds:F2}\n" +
                "UsesM51_1HeavyLoadPipeline=True\n" +
                "InstantRouteDelete=False",
                this);

            return true;
        }

        private bool ApplyEraser(
            string source)
        {
            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute
            )
            {
                return Reject(
                    "Silgi: aktif rota yok.");
            }

            if (
                !boundRoute.TryErase(
                    source,
                    out string reason)
            )
            {
                return Reject(
                    "Silgi reddedildi: " +
                    reason);
            }

            eraserSuccessCount++;

            lastAction =
                "Silgi hedeflenen İz Dokumayı kaldırdı.";

            Debug.Log(
                "[M51.2 Eraser Counterplay]\n" +
                $"Source={source}\n" +
                "TargetedRouteExpired=True",
                this);

            return true;
        }

        private void ResolvePaletteKnifeContracts()
        {
            paletteUseToolAction = null;
            paletteToolOrigin = null;
            paletteKnifeInputResolved = false;

            if (paletteKnifeController == null)
            {
                paletteKnifeActivityContract =
                    "Controller missing";

                return;
            }

            Type runtimeType =
                paletteKnifeController.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            FieldInfo inputField =
                runtimeType.GetField(
                    "useToolAction",
                    flags);

            if (
                inputField != null &&
                typeof(InputActionReference)
                    .IsAssignableFrom(
                        inputField.FieldType)
            )
            {
                try
                {
                    paletteUseToolAction =
                        inputField.GetValue(
                            paletteKnifeController)
                        as InputActionReference;
                }
                catch (Exception)
                {
                    paletteUseToolAction = null;
                }
            }

            FieldInfo originField =
                runtimeType.GetField(
                    "toolOrigin",
                    flags);

            if (
                originField != null &&
                typeof(Transform)
                    .IsAssignableFrom(
                        originField.FieldType)
            )
            {
                try
                {
                    paletteToolOrigin =
                        originField.GetValue(
                            paletteKnifeController)
                        as Transform;
                }
                catch (Exception)
                {
                    paletteToolOrigin = null;
                }
            }

            paletteKnifeInputResolved =
                paletteUseToolAction != null &&
                paletteUseToolAction.action != null;

            paletteKnifeActivityContract =
                paletteKnifeInputResolved
                    ? "Existing useToolAction read-only"
                    : "InputActionReference unresolved";
        }

        private void RefreshPaletteKnifeState()
        {
            if (paletteKnifeController == null)
            {
                paletteKnifeActive = false;
                return;
            }

            paletteKnifeActive =
                paletteKnifeController.enabled &&
                paletteKnifeController.gameObject.activeInHierarchy;

            if (paletteToolOrigin == null)
            {
                ResolvePaletteKnifeContracts();
            }
        }

        private void ProcessPaletteKnifeInput()
        {
            if (
                !paletteKnifeActive ||
                !paletteKnifeInputResolved ||
                paletteUseToolAction == null ||
                paletteUseToolAction.action == null ||
                !paletteUseToolAction.action.enabled
            )
            {
                return;
            }

            if (
                !paletteUseToolAction.action
                    .WasPressedThisFrame()
            )
            {
                return;
            }

            paletteKnifeAttemptCount++;

            Vector3 origin =
                paletteToolOrigin != null
                    ? paletteToolOrigin.position
                    : figureRoot != null
                        ? figureRoot.position
                        : transform.position;

            ApplyPaletteKnifeCut(
                origin,
                paletteKnifeRouteReach,
                "Existing PaletteKnife UseTool");
        }

        private bool ApplyPaletteKnifeCut(
            Vector3 point,
            float maximumDistance,
            string source)
        {
            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute
            )
            {
                return Reject(
                    "Palet Bıçağı: aktif İz Dokuma yok.");
            }

            if (
                !boundRoute.TryOpenPaletteKnifeConnection(
                    point,
                    maximumDistance,
                    safeEndpointFraction,
                    out bool routeBroken,
                    out string reason)
            )
            {
                return Reject(
                    "Palet Bıçağı reddedildi: " +
                    reason);
            }

            if (routeBroken)
            {
                paletteKnifeBreakCount++;

                lastAction =
                    "Yanlış orta kesim: İz Dokuma tamamen koptu.";
            }
            else
            {
                paletteKnifeConnectionCount++;

                lastAction =
                    "Palet Bıçağı yeni rota bağlantı noktası açtı.";
            }

            Debug.Log(
                "[M51.2 Palette Knife Route Edit]\n" +
                $"Source={source}\n" +
                $"RouteBroken={routeBroken}\n" +
                $"SafeEndpointFraction={safeEndpointFraction:F2}\n" +
                "UsesExistingUseToolActionReadOnly=True\n" +
                "AddsSecondPaletteKnifeInputOwner=False\n" +
                "CreatesFreeAirGeometry=False",
                this);

            return true;
        }

        private bool TryResolveForcePoint(
            out Vector3 point)
        {
            point = Vector3.zero;

            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute ||
                boundRoute.RoutePointCount < 2
            )
            {
                return false;
            }

            int index =
                Mathf.Clamp(
                    boundRoute.RoutePointCount /
                    2,
                    0,
                    boundRoute.RoutePointCount -
                    1);

            return boundRoute.TryGetRoutePoint(
                index,
                out point);
        }

        private bool Reject(
            string reason)
        {
            rejectedCounterplayCount++;
            lastAction = reason;
            return false;
        }

        private void RefreshHud()
        {
            bool visible =
                routeAvailable &&
                (
                    painterRoleActive ||
                    figureRoleActive
                );

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
                    painterRoleActive
                        ? "İZ DOKUMA • RESSAM KARŞI-OYUNU"
                        : "İZ DOKUMA • PALET BIÇAĞI BAĞLANTISI";
            }

            if (stateText != null)
            {
                if (painterRoleActive)
                {
                    stateText.text =
                        $"HEDEF {(painterAimValid ? "ROTA" : "YOK")}   " +
                        $"KAMERA {(painterCameraResolved ? "OK" : "YOK")}\n" +
                        $"SU {watercolorSuccessCount}   " +
                        $"YAĞ {oilSuccessCount}   " +
                        $"SİLGİ {eraserSuccessCount}\n" +
                        lastAction;
                }
                else
                {
                    stateText.text =
                        $"PALET BIÇAĞI {(paletteKnifeActive ? "AKTİF" : "KAPALI")}   " +
                        $"INPUT {(paletteKnifeInputResolved ? "OK" : "YOK")}\n" +
                        $"BAĞLANTI {paletteKnifeConnectionCount}   " +
                        $"KOPMA {paletteKnifeBreakCount}\n" +
                        lastAction;
                }
            }

            if (controlsText != null)
            {
                controlsText.text =
                    painterRoleActive
                        ? "F4 • SULUBOYA EĞ   F10 • YAĞLI BOYA YÜK   F12 • SİLGİ\n" +
                          "GEÇİCİ RİSK-SPIKE TUŞLARI • FİNAL TOOLBAR DEĞİL"
                        : "MEVCUT PALET BIÇAĞI + E • UÇTA YENİ BAĞLANTI\n" +
                          "ORTA KESİM ROTAYI TAMAMEN KOPARIR";
            }
        }

        private static bool IsInside(
            Transform candidate,
            Transform root)
        {
            return
                candidate != null &&
                root != null &&
                (
                    candidate == root ||
                    candidate.IsChildOf(root)
                );
        }

        private void OnDisable()
        {
            SetActionState(
                watercolorAction,
                false);

            SetActionState(
                oilAction,
                false);

            SetActionState(
                eraserAction,
                false);
        }

        private void OnDestroy()
        {
            DisposeAction(
                ref watercolorAction);

            DisposeAction(
                ref oilAction);

            DisposeAction(
                ref eraserAction);
        }

        private static void DisposeAction(
            ref InputAction action)
        {
            if (action == null)
            {
                return;
            }

            if (action.enabled)
            {
                action.Disable();
            }

            action.Dispose();
            action = null;
        }
    }
}
