using System;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(320)]
    [DisallowMultipleComponent]
    public sealed class PrototypeLiveCompositionPainterCounterplayController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeLiveCompositionController liveComposition;

        [SerializeField]
        private MonoBehaviour sharedPainterMaterialInputSource;

        [Header("Aim")]
        [SerializeField, Min(0.05f)]
        private float physicsAimRadius = 0.22f;

        [SerializeField, Min(8f)]
        private float screenAimPixels = 42f;

        [SerializeField, Min(5f)]
        private float maximumAimDistance = 120f;

        [Header("Watercolor")]
        [SerializeField, Min(0.1f)]
        private float watercolorBendStep = 0.62f;

        [SerializeField, Min(0.2f)]
        private float maximumWatercolorDisplacement = 1.20f;

        [Header("Oil")]
        [SerializeField, Min(1f)]
        private float oilLoadKilograms = 100f;

        [SerializeField, Min(0.25f)]
        private float oilLoadSeconds = 2.0f;

        [SerializeField, Min(0.25f)]
        private float oilCollapseHoldSeconds = 1.15f;

        [SerializeField, Min(0f)]
        private float oilStressRecoveryPerSecond = 0.85f;

        [Header("HUD")]
        [SerializeField]
        private CanvasGroup statusGroup;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Text controlsText;

        [Header("Runtime Read Only")]
        [SerializeField]
        private bool painterRoleActive;

        [SerializeField]
        private bool sharedMaterialInputResolved;

        [SerializeField]
        private bool painterCameraResolved;

        [SerializeField]
        private bool bridgeTargetValid;

        [SerializeField]
        private bool oilLoadActive;

        [SerializeField]
        private int watercolorAttemptCount;

        [SerializeField]
        private int watercolorSuccessCount;

        [SerializeField]
        private int oilAttemptCount;

        [SerializeField]
        private int oilSuccessCount;

        [SerializeField]
        private int oilCollapseCount;

        [SerializeField]
        private int eraserAttemptCount;

        [SerializeField]
        private int eraserSuccessCount;

        [SerializeField]
        private int rejectedCounterplayCount;

        [SerializeField]
        private float oilStressSeconds;

        [SerializeField]
        private float oilStressNormalized;

        [SerializeField]
        private float oilLoadRemainingSeconds;

        [SerializeField]
        private Vector3 aimedWorldPoint;

        [SerializeField]
        private string resolvedPainterRole = "Unknown";

        [SerializeField]
        private string sharedInputContract = "Unresolved";

        [SerializeField]
        private string lastAction =
            "Canlı Kompozisyon bağlantısı bekleniyor.";

        private readonly RaycastHit[] hitBuffer =
            new RaycastHit[24];

        private InputAction watercolorAction;
        private InputAction oilAction;
        private InputAction eraserAction;

        private Camera painterCamera;
        private float oilLoadUntil;

        public bool PainterRoleActive => painterRoleActive;
        public bool SharedMaterialInputResolved =>
            sharedMaterialInputResolved;
        public bool PainterCameraResolved =>
            painterCameraResolved;
        public bool BridgeTargetValid => bridgeTargetValid;
        public bool OilLoadActive => oilLoadActive;
        public int WatercolorAttemptCount =>
            watercolorAttemptCount;
        public int WatercolorSuccessCount =>
            watercolorSuccessCount;
        public int OilAttemptCount => oilAttemptCount;
        public int OilSuccessCount => oilSuccessCount;
        public int OilCollapseCount => oilCollapseCount;
        public int EraserAttemptCount => eraserAttemptCount;
        public int EraserSuccessCount => eraserSuccessCount;
        public int RejectedCounterplayCount =>
            rejectedCounterplayCount;
        public float OilStressSeconds => oilStressSeconds;
        public float OilStressNormalized => oilStressNormalized;
        public float OilLoadRemainingSeconds =>
            oilLoadRemainingSeconds;
        public Vector3 AimedWorldPoint => aimedWorldPoint;
        public string SharedInputContract =>
            sharedInputContract;
        public string LastAction => lastAction;

        public bool UsesExistingM51PainterMaterialActionsReadOnly => true;
        public bool AddsNewPainterGameplayInputAction => false;
        public bool WatercolorBendEnabled => true;
        public bool OilDelayedCollapseEnabled => true;
        public bool EraserSafeSeverEnabled => true;
        public bool WatercolorRawDamageEnabled => false;
        public bool OilRawDamageEnabled => false;
        public bool EraserRawDamageEnabled => false;
        public bool AllowLocalPainterRoleSwitchBypass => true;
        public bool LocalRoleSwitchBypassIsPrototypeOnly => true;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeLiveCompositionController configuredLiveComposition,
            MonoBehaviour configuredSharedPainterMaterialInputSource,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            liveComposition =
                configuredLiveComposition;

            sharedPainterMaterialInputSource =
                configuredSharedPainterMaterialInputSource;

            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolveSharedMaterialActions();
            RefreshHud();
        }

        private void Awake()
        {
            ResolveSharedMaterialActions();
        }

        private void OnEnable()
        {
            ResolveSharedMaterialActions();
        }

        private void Update()
        {
            painterRoleActive =
                PrototypeRoleAuthorityResolver.IsPainter(
                    out resolvedPainterRole);

            RefreshSharedMaterialActions();

            if (painterRoleActive)
            {
                ResolvePainterCamera();
                RefreshBridgeAim();
                ProcessPainterInputs();
            }
            else
            {
                bridgeTargetValid = false;
                aimedWorldPoint = Vector3.zero;
            }

            TickOilStress();
            RefreshHud();
        }

        public bool ForceWatercolorBendForPrototype()
        {
            if (
                liveComposition == null ||
                !liveComposition.Composing
            )
            {
                return Reject(
                    "Force Watercolor: aktif kompozisyon yok.");
            }

            Vector3 direction =
                painterCamera != null
                    ? Vector3.ProjectOnPlane(
                        painterCamera.transform.right,
                        Vector3.up)
                    : Vector3.right;

            if (
                direction.sqrMagnitude <
                0.001f
            )
            {
                direction = Vector3.right;
            }

            direction.Normalize();

            return TryWatercolorBend(
                direction *
                watercolorBendStep,
                "Editor force");
        }

        public bool ForceOilLoadForPrototype()
        {
            if (
                liveComposition == null ||
                !liveComposition.Composing
            )
            {
                return Reject(
                    "Force Oil: aktif kompozisyon yok.");
            }

            StartOilLoad(
                "Editor force");

            return true;
        }

        public bool ForceEraserSeverForPrototype()
        {
            if (
                liveComposition == null ||
                !liveComposition.Composing
            )
            {
                return Reject(
                    "Force Eraser: aktif kompozisyon yok.");
            }

            return TryEraserSever(
                "Editor force");
        }

        private void ResolveSharedMaterialActions()
        {
            watercolorAction = null;
            oilAction = null;
            eraserAction = null;
            sharedMaterialInputResolved = false;

            if (
                sharedPainterMaterialInputSource == null
            )
            {
                sharedInputContract =
                    "M51.2 material source missing";

                return;
            }

            Type runtimeType =
                sharedPainterMaterialInputSource.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            watercolorAction =
                ReadActionField(
                    runtimeType,
                    "watercolorAction",
                    flags);

            oilAction =
                ReadActionField(
                    runtimeType,
                    "oilAction",
                    flags);

            eraserAction =
                ReadActionField(
                    runtimeType,
                    "eraserAction",
                    flags);

            sharedMaterialInputResolved =
                watercolorAction != null &&
                oilAction != null &&
                eraserAction != null;

            sharedInputContract =
                sharedMaterialInputResolved
                    ? "M51.2 F4/F10/F12 actions read-only"
                    : "M51.2 shared material actions unresolved";
        }

        private InputAction ReadActionField(
            Type runtimeType,
            string fieldName,
            BindingFlags flags)
        {
            FieldInfo field =
                runtimeType.GetField(
                    fieldName,
                    flags);

            if (
                field == null ||
                !typeof(InputAction)
                    .IsAssignableFrom(
                        field.FieldType)
            )
            {
                return null;
            }

            try
            {
                return
                    field.GetValue(
                        sharedPainterMaterialInputSource)
                    as InputAction;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void RefreshSharedMaterialActions()
        {
            if (
                watercolorAction == null ||
                oilAction == null ||
                eraserAction == null
            )
            {
                ResolveSharedMaterialActions();
            }

            sharedMaterialInputResolved =
                watercolorAction != null &&
                oilAction != null &&
                eraserAction != null;
        }

        private void ResolvePainterCamera()
        {
            painterCamera = null;
            painterCameraResolved = false;

            Camera[] cameras =
                UnityEngine.Object.FindObjectsByType<
                    Camera>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);

            int bestScore =
                int.MinValue;

            for (int index = 0;
                 index < cameras.Length;
                 index++)
            {
                Camera candidate =
                    cameras[index];

                if (
                    candidate == null ||
                    !candidate.isActiveAndEnabled ||
                    !candidate.gameObject.activeInHierarchy
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

                if (
                    candidate.CompareTag(
                        "MainCamera")
                )
                {
                    score += 100;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    painterCamera = candidate;
                }
            }

            if (
                painterCamera == null &&
                Camera.main != null &&
                Camera.main.isActiveAndEnabled
            )
            {
                painterCamera =
                    Camera.main;
            }

            painterCameraResolved =
                painterCamera != null;
        }

        private void RefreshBridgeAim()
        {
            bridgeTargetValid = false;
            aimedWorldPoint = Vector3.zero;

            if (
                liveComposition == null ||
                !liveComposition.Composing ||
                liveComposition.ActiveBridge == null ||
                !liveComposition.ActiveBridge.ActiveBridge ||
                painterCamera == null ||
                Mouse.current == null
            )
            {
                return;
            }

            PrototypeLiveCompositionBridge bridge =
                liveComposition.ActiveBridge;

            Vector2 pointer =
                Mouse.current.position.ReadValue();

            Ray ray =
                painterCamera.ScreenPointToRay(
                    pointer);

            int hitCount =
                Physics.SphereCastNonAlloc(
                    ray,
                    physicsAimRadius,
                    hitBuffer,
                    maximumAimDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore);

            float bestDistance =
                float.PositiveInfinity;

            for (int index = 0;
                 index < hitCount;
                 index++)
            {
                RaycastHit hit =
                    hitBuffer[index];

                if (
                    hit.collider == null ||
                    !bridge.OwnsCollider(
                        hit.collider)
                )
                {
                    continue;
                }

                if (
                    hit.distance <
                    bestDistance
                )
                {
                    bestDistance =
                        hit.distance;

                    aimedWorldPoint =
                        hit.point;

                    bridgeTargetValid = true;
                }
            }

            if (bridgeTargetValid)
            {
                return;
            }

            TryScreenSpaceBridgeAim(
                bridge,
                pointer);
        }

        private void TryScreenSpaceBridgeAim(
            PrototypeLiveCompositionBridge bridge,
            Vector2 pointer)
        {
            Vector3 screenA =
                painterCamera.WorldToScreenPoint(
                    bridge.AnchorPointA);

            Vector3 screenBody =
                painterCamera.WorldToScreenPoint(
                    bridge.BodyPoint);

            Vector3 screenB =
                painterCamera.WorldToScreenPoint(
                    bridge.AnchorPointB);

            if (
                screenA.z <= 0f &&
                screenBody.z <= 0f &&
                screenB.z <= 0f
            )
            {
                return;
            }

            Vector2 a =
                new Vector2(
                    screenA.x,
                    screenA.y);

            Vector2 body =
                new Vector2(
                    screenBody.x,
                    screenBody.y);

            Vector2 b =
                new Vector2(
                    screenB.x,
                    screenB.y);

            float leftT;
            float rightT;

            float leftDistance =
                DistancePointToSegment(
                    pointer,
                    a,
                    body,
                    out leftT);

            float rightDistance =
                DistancePointToSegment(
                    pointer,
                    body,
                    b,
                    out rightT);

            float best =
                Mathf.Min(
                    leftDistance,
                    rightDistance);

            if (
                best >
                screenAimPixels
            )
            {
                return;
            }

            if (
                leftDistance <=
                rightDistance
            )
            {
                aimedWorldPoint =
                    Vector3.Lerp(
                        bridge.AnchorPointA,
                        bridge.BodyPoint,
                        leftT);
            }
            else
            {
                aimedWorldPoint =
                    Vector3.Lerp(
                        bridge.BodyPoint,
                        bridge.AnchorPointB,
                        rightT);
            }

            bridgeTargetValid = true;
        }

        private static float DistancePointToSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end,
            out float t)
        {
            Vector2 segment =
                end -
                start;

            float lengthSquared =
                segment.sqrMagnitude;

            if (
                lengthSquared <=
                0.0001f
            )
            {
                t = 0f;

                return
                    Vector2.Distance(
                        point,
                        start);
            }

            t =
                Mathf.Clamp01(
                    Vector2.Dot(
                        point -
                        start,
                        segment) /
                    lengthSquared);

            Vector2 nearest =
                start +
                segment *
                t;

            return
                Vector2.Distance(
                    point,
                    nearest);
        }

        private void ProcessPainterInputs()
        {
            if (
                !sharedMaterialInputResolved ||
                !bridgeTargetValid ||
                liveComposition == null ||
                !liveComposition.Composing
            )
            {
                return;
            }

            if (
                watercolorAction != null &&
                watercolorAction.enabled &&
                watercolorAction.WasPressedThisFrame()
            )
            {
                watercolorAttemptCount++;

                float side =
                    Mouse.current != null &&
                    Mouse.current.position.ReadValue().x >=
                    Screen.width *
                    0.5f
                        ? 1f
                        : -1f;

                Vector3 direction =
                    Vector3.ProjectOnPlane(
                        painterCamera.transform.right,
                        Vector3.up);

                if (
                    direction.sqrMagnitude <
                    0.001f
                )
                {
                    direction =
                        Vector3.right;
                }

                direction.Normalize();

                TryWatercolorBend(
                    direction *
                    side *
                    watercolorBendStep,
                    "Shared M51.2 F4");

                return;
            }

            if (
                oilAction != null &&
                oilAction.enabled &&
                oilAction.WasPressedThisFrame()
            )
            {
                oilAttemptCount++;

                StartOilLoad(
                    "Shared M51.2 F10");

                return;
            }

            if (
                eraserAction != null &&
                eraserAction.enabled &&
                eraserAction.WasPressedThisFrame()
            )
            {
                eraserAttemptCount++;

                TryEraserSever(
                    "Shared M51.2 F12");
            }
        }

        private bool TryWatercolorBend(
            Vector3 delta,
            string source)
        {
            string reason =
                "Live composition missing";

            if (
                liveComposition == null ||
                !liveComposition.TryApplyPainterWatercolorBend(
                    delta,
                    maximumWatercolorDisplacement,
                    source,
                    out reason)
            )
            {
                return Reject(
                    $"Suluboya reddedildi: {reason}");
            }

            watercolorSuccessCount++;

            lastAction =
                "Suluboya Köprü gövdesini ve Figure pozunu yana büktü.";

            Debug.Log(
                "[M53.2 Watercolor Composition Bend]\n" +
                $"Source={source}\n" +
                $"BendStep={watercolorBendStep:F2}\n" +
                $"MaxDisplacement={maximumWatercolorDisplacement:F2}\n" +
                $"BridgeBendCount={liveComposition.ActiveBridge.WatercolorBendCount}\n" +
                "FigurePoseMovedWithBridge=True\n" +
                "CollisionRebuiltWithBodyPoint=True\n" +
                "WatercolorRawDamageEnabled=False",
                this);

            return true;
        }

        private void StartOilLoad(
            string source)
        {
            if (
                liveComposition == null ||
                !liveComposition.Composing
            )
            {
                Reject(
                    "Yağlı Boya: aktif kompozisyon yok.");

                return;
            }

            oilLoadActive = true;

            oilLoadUntil =
                Time.unscaledTime +
                oilLoadSeconds;

            oilLoadRemainingSeconds =
                oilLoadSeconds;

            oilSuccessCount++;

            lastAction =
                $"Yağlı Boya {oilLoadKilograms:F0} kg eşdeğer yük bindirdi.";

            Debug.Log(
                "[M53.2 Oil Composition Load]\n" +
                $"Source={source}\n" +
                $"LoadKg={oilLoadKilograms:F1}\n" +
                $"LoadSeconds={oilLoadSeconds:F2}\n" +
                $"CollapseHoldSeconds={oilCollapseHoldSeconds:F2}\n" +
                "DelayedCollapse=True\n" +
                "OilRawDamageEnabled=False",
                this);
        }

        private void TickOilStress()
        {
            bool compositionActive =
                liveComposition != null &&
                liveComposition.Composing;

            if (!compositionActive)
            {
                oilLoadActive = false;
                oilLoadUntil = 0f;
                oilLoadRemainingSeconds = 0f;
                oilStressSeconds = 0f;
                oilStressNormalized = 0f;

                return;
            }

            float now =
                Time.unscaledTime;

            oilLoadActive =
                oilLoadUntil > 0f &&
                now <
                oilLoadUntil;

            oilLoadRemainingSeconds =
                oilLoadActive
                    ? Mathf.Max(
                        0f,
                        oilLoadUntil -
                        now)
                    : 0f;

            if (oilLoadActive)
            {
                oilStressSeconds +=
                    Mathf.Max(
                        0f,
                        Time.deltaTime);
            }
            else
            {
                oilStressSeconds =
                    Mathf.Max(
                        0f,
                        oilStressSeconds -
                        oilStressRecoveryPerSecond *
                        Mathf.Max(
                            0f,
                            Time.deltaTime));
            }

            oilStressNormalized =
                Mathf.Clamp01(
                    oilStressSeconds /
                    Mathf.Max(
                        0.1f,
                        oilCollapseHoldSeconds));

            if (
                oilStressSeconds <
                oilCollapseHoldSeconds
            )
            {
                return;
            }

            if (
                liveComposition.ForceExternalCompositionRelease(
                    "Painter Oil overload collapsed composition")
            )
            {
                oilCollapseCount++;

                lastAction =
                    "Yağlı Boya yükü Köprü Kompozisyonunu güvenli çözdü.";
            }

            oilLoadActive = false;
            oilLoadUntil = 0f;
            oilLoadRemainingSeconds = 0f;
            oilStressSeconds = 0f;
            oilStressNormalized = 0f;
        }

        private bool TryEraserSever(
            string source)
        {
            if (
                liveComposition == null ||
                !liveComposition.Composing
            )
            {
                return Reject(
                    "Silgi: aktif kompozisyon yok.");
            }

            if (
                !liveComposition.ForceExternalCompositionRelease(
                    "Painter Eraser severed composition connection")
            )
            {
                return Reject(
                    "Silgi güvenli çözülmeyi başlatamadı.");
            }

            eraserSuccessCount++;

            lastAction =
                "Silgi bağlantıyı kesti; kompozisyon güvenli çözüldü.";

            Debug.Log(
                "[M53.2 Eraser Composition Sever]\n" +
                $"Source={source}\n" +
                "ImmediateSafeRelease=True\n" +
                "FigureMotorRestoredByLiveComposition=True\n" +
                "ConsentSessionResetByLiveComposition=True\n" +
                "EraserRawDamageEnabled=False",
                this);

            return true;
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
                painterRoleActive &&
                liveComposition != null &&
                liveComposition.Composing;

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
                    "CANLI KOMPOZİSYON • RESSAM KARŞI-OYUNU";
            }

            if (stateText != null)
            {
                string target =
                    bridgeTargetValid
                        ? "KÖPRÜ"
                        : "YOK";

                stateText.text =
                    $"HEDEF {target}   " +
                    $"KAMERA {(painterCameraResolved ? "OK" : "YOK")}   " +
                    $"INPUT {(sharedMaterialInputResolved ? "ORTAK" : "YOK")}\n" +
                    $"SULUBOYA {watercolorSuccessCount}   " +
                    $"YAĞ {oilSuccessCount}   " +
                    $"SİLGİ {eraserSuccessCount}\n" +
                    $"YAĞ STRES %{oilStressNormalized * 100f:F0}   " +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    "KÖPRÜYE İMLEÇ • F4 SULUBOYA EĞ • F10 YAĞ YÜK • F12 SİLGİ KES\n" +
                    "M51.2 İLE AYNI MALZEME INPUT SAHİBİ • YENİ TUŞ YOK";
            }
        }
    }
}
