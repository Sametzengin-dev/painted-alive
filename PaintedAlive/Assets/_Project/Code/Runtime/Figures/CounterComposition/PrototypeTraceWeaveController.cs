using System;
using System.Collections.Generic;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(-180)]
    [DisallowMultipleComponent]
    public sealed class PrototypeTraceWeaveController :
        MonoBehaviour
    {
        private struct TraceSample
        {
            public float Time;
            public Vector3 Position;

            public TraceSample(
                float time,
                Vector3 position)
            {
                Time = time;
                Position = position;
            }
        }

        [Header("Figure Sources")]
        [SerializeField] private Transform figureRoot;
        [SerializeField] private MonoBehaviour clarityState;

        [Header("Trace Capture")]
        [SerializeField, Min(1f)]
        private float historySeconds = 4f;

        [SerializeField, Min(0.02f)]
        private float minimumSampleSpacing = 0.16f;

        [SerializeField, Range(8, 96)]
        private int maximumStoredSamples = 48;

        [SerializeField, Range(4, 32)]
        private int maximumWeavePoints = 18;

        [Header("Anchor Validation")]
        [SerializeField, Min(0.1f)]
        private float anchorRayStartHeight = 0.65f;

        [SerializeField, Min(0.2f)]
        private float anchorRayDistance = 1.80f;

        [SerializeField, Min(0f)]
        private float anchorSurfaceLift = 0.10f;

        [SerializeField, Range(0f, 75f)]
        private float maximumAnchorSlope = 58f;

        [SerializeField]
        private LayerMask anchorMask = ~0;

        [Header("Route Contract")]
        [SerializeField, Min(0.5f)]
        private float weaveLifetimeSeconds = 8f;

        [SerializeField, Min(0.05f)]
        private float routeRadius = 0.12f;

        [SerializeField, Min(0.5f)]
        private float minimumRouteLength = 1.10f;

        [SerializeField, Min(1f)]
        private float maximumRouteLength = 10f;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Focus")]
        [SerializeField, Min(0f)]
        private float contextualHudHoldSeconds = 2.5f;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeTraceWeaveState state =
            PrototypeTraceWeaveState.WaitingForFigure;

        [SerializeField] private bool figureRoleActive;
        [SerializeField] private bool fullStainBlocked;
        [SerializeField] private bool inputActionActive;
        [SerializeField] private bool startAnchorValid;
        [SerializeField] private bool endAnchorValid;
        [SerializeField] private int storedSampleCount;
        [SerializeField] private int weaveAttemptCount;
        [SerializeField] private int weaveSuccessCount;
        [SerializeField] private int weaveRejectCount;
        [SerializeField] private int replacedWeaveCount;
        [SerializeField] private int startAnchorQueryCount;
        [SerializeField] private int endAnchorQueryCount;
        [SerializeField] private int lastBuiltPointCount;
        [SerializeField] private float recordedHistorySeconds;
        [SerializeField] private float lastRouteLength;
        [SerializeField] private Vector3 lastStartAnchor;
        [SerializeField] private Vector3 lastEndAnchor;
        [SerializeField] private string clarityLevel = "Unknown";
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private string lastAction =
            "Figür hareket izi bekleniyor.";

        private readonly List<TraceSample> samples =
            new List<TraceSample>();

        private readonly List<Vector3> buildPoints =
            new List<Vector3>();

        private readonly RaycastHit[] anchorHits =
            new RaycastHit[24];

        private InputAction weaveAction;
        private LineRenderer previewLine;
        private Material previewMaterial;
        private PropertyInfo currentLevelProperty;

        private PrototypeTraceWeaveRoute activeRoute;
        private Vector3 lastSamplePosition;
        private bool hasLastSamplePosition;
        private float hudVisibleUntilUnscaled;

        public PrototypeTraceWeaveState State => state;
        public bool FigureRoleActive => figureRoleActive;
        public bool FullStainBlocked => fullStainBlocked;
        public bool InputActionActive => inputActionActive;
        public bool StartAnchorValid => startAnchorValid;
        public bool EndAnchorValid => endAnchorValid;
        public int StoredSampleCount => storedSampleCount;
        public int WeaveAttemptCount => weaveAttemptCount;
        public int WeaveSuccessCount => weaveSuccessCount;
        public int WeaveRejectCount => weaveRejectCount;
        public int ReplacedWeaveCount => replacedWeaveCount;
        public int LastBuiltPointCount => lastBuiltPointCount;
        public float RecordedHistorySeconds => recordedHistorySeconds;
        public float LastRouteLength => lastRouteLength;
        public string ClarityLevel => clarityLevel;
        public string ResolvedRole => resolvedRole;
        public string LastAction => lastAction;
        public PrototypeTraceWeaveRoute ActiveRoute => activeRoute;

        public bool TraceWeaveEnabled => true;
        public float RecordedWindowSeconds => historySeconds;
        public bool RequiresValidatedStartAnchor => true;
        public bool RequiresValidatedEndAnchor => true;
        public bool FreeAirGeometryEnabled => false;
        public int MaximumActiveWeaves => 1;
        public bool UsesCapsuleSegmentProxies => true;
        public bool UsesMeshCollider => false;
        public bool FullStainCanWeave => false;
        public bool PrototypeKeyboardBinding => true;
        public string PrototypeBinding => "T";
        public bool ContourThreadInventoryIntegrated => false;
        public bool PainterCounterplayIntegrated => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            Transform configuredFigureRoot,
            MonoBehaviour configuredClarityState,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            figureRoot = configuredFigureRoot;
            clarityState = configuredClarityState;
            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolveClarityContract();
            EnsureInputAction();
            EnsurePreview();
            RefreshHud();
        }

        private void Awake()
        {
            ResolveClarityContract();
            EnsureInputAction();
            EnsurePreview();
        }

        private void OnEnable()
        {
            ResolveClarityContract();
            EnsureInputAction();
            EnsurePreview();
        }

        private void Update()
        {
            RefreshAuthority();
            RefreshInputActionState();

            if (
                figureRoleActive &&
                !fullStainBlocked &&
                figureRoot != null
            )
            {
                RecordMovementTrace();
            }
            else
            {
                HidePreview();

                if (
                    fullStainBlocked
                )
                {
                    state =
                        PrototypeTraceWeaveState.StainFormBlocked;
                }
                else if (!figureRoleActive)
                {
                    state =
                        PrototypeTraceWeaveState.PainterRole;
                }
            }

            if (
                activeRoute != null &&
                activeRoute.Expired
            )
            {
                activeRoute = null;
            }

            RefreshPreview();
            RefreshHud();
        }

        public bool TryWeaveNow()
        {
            RefreshAuthority();

            if (
                !figureRoleActive ||
                fullStainBlocked
            )
            {
                weaveAttemptCount++;
                weaveRejectCount++;

                lastAction =
                    fullStainBlocked
                        ? "Full Stain formu Kontur İpliği kullanamaz."
                        : "İz Dokuma yalnız Figure rolünde çalışır.";

                return false;
            }

            return TryCreateWeave(
                "Direct / Editor");
        }

        private void ResolveClarityContract()
        {
            currentLevelProperty = null;

            if (clarityState == null)
            {
                return;
            }

            currentLevelProperty =
                clarityState
                    .GetType()
                    .GetProperty(
                        "CurrentLevel",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);
        }

        private void EnsureInputAction()
        {
            if (weaveAction != null)
            {
                return;
            }

            weaveAction =
                new InputAction(
                    "M51.0 Freeze Trace Weave",
                    InputActionType.Button,
                    "<Keyboard>/t");

            weaveAction.performed +=
                HandleWeavePerformed;
        }

        private void RefreshInputActionState()
        {
            bool shouldEnable =
                figureRoleActive &&
                !fullStainBlocked &&
                figureRoot != null;

            if (shouldEnable)
            {
                if (!weaveAction.enabled)
                {
                    weaveAction.Enable();
                }
            }
            else if (weaveAction.enabled)
            {
                weaveAction.Disable();
            }

            inputActionActive =
                weaveAction.enabled;
        }

        private void HandleWeavePerformed(
            InputAction.CallbackContext context)
        {
            TryCreateWeave(
                "Keyboard T");
        }

        private void RefreshAuthority()
        {
            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedRole);

            clarityLevel =
                ReadClarityLevel();

            fullStainBlocked =
                string.Equals(
                    clarityLevel,
                    "Stain",
                    StringComparison.OrdinalIgnoreCase);
        }

        private string ReadClarityLevel()
        {
            if (
                clarityState == null ||
                currentLevelProperty == null
            )
            {
                return "Unknown";
            }

            try
            {
                object value =
                    currentLevelProperty.GetValue(
                        clarityState);

                return value != null
                    ? value.ToString()
                    : "Unknown";
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }

        private void RecordMovementTrace()
        {
            float now =
                Time.unscaledTime;

            Vector3 position =
                figureRoot.position;

            bool shouldSample =
                !hasLastSamplePosition ||
                Vector3.Distance(
                    lastSamplePosition,
                    position) >=
                    minimumSampleSpacing;

            if (shouldSample)
            {
                samples.Add(
                    new TraceSample(
                        now,
                        position));

                lastSamplePosition =
                    position;

                hasLastSamplePosition = true;
            }

            float cutoff =
                now -
                historySeconds;

            while (
                samples.Count > 0 &&
                (
                    samples[0].Time <
                        cutoff ||
                    samples.Count >
                        maximumStoredSamples
                )
            )
            {
                samples.RemoveAt(0);
            }

            storedSampleCount =
                samples.Count;

            recordedHistorySeconds =
                samples.Count >= 2
                    ? samples[
                        samples.Count - 1
                    ].Time -
                    samples[0].Time
                    : 0f;

            if (
                activeRoute != null &&
                activeRoute.ActiveRoute
            )
            {
                state =
                    PrototypeTraceWeaveState.ActiveWeave;
            }
            else if (samples.Count >= 4)
            {
                state =
                    PrototypeTraceWeaveState.Ready;
            }
            else
            {
                state =
                    PrototypeTraceWeaveState.Recording;
            }
        }

        private bool TryCreateWeave(
            string source)
        {
            weaveAttemptCount++;

            if (
                samples.Count < 4
            )
            {
                Reject(
                    PrototypeTraceWeaveState.InsufficientTrace,
                    "Son hareket izi yeterince uzun değil.");

                return false;
            }

            BuildSimplifiedPath();

            if (buildPoints.Count < 2)
            {
                Reject(
                    PrototypeTraceWeaveState.InsufficientTrace,
                    "Normalize edilmiş rota üretilemedi.");

                return false;
            }

            if (
                !TryResolveAnchor(
                    buildPoints[0],
                    isStart: true,
                    out Vector3 startAnchor)
            )
            {
                Reject(
                    PrototypeTraceWeaveState.StartAnchorInvalid,
                    "Başlangıç ankrajı doğrulanamadı.");

                return false;
            }

            if (
                !TryResolveAnchor(
                    buildPoints[
                        buildPoints.Count - 1],
                    isStart: false,
                    out Vector3 endAnchor)
            )
            {
                Reject(
                    PrototypeTraceWeaveState.EndAnchorInvalid,
                    "Bitiş ankrajı doğrulanamadı.");

                return false;
            }

            buildPoints[0] =
                startAnchor +
                Vector3.up *
                anchorSurfaceLift;

            buildPoints[
                buildPoints.Count - 1] =
                    endAnchor +
                    Vector3.up *
                    anchorSurfaceLift;

            float routeLength =
                CalculateLength(
                    buildPoints);

            if (
                routeLength <
                minimumRouteLength
            )
            {
                Reject(
                    PrototypeTraceWeaveState.InsufficientTrace,
                    $"Rota çok kısa: {routeLength:F2} m.");

                return false;
            }

            if (
                routeLength >
                maximumRouteLength
            )
            {
                Reject(
                    PrototypeTraceWeaveState.RouteTooLong,
                    $"Rota çok uzun: {routeLength:F2} m.");

                return false;
            }

            if (
                activeRoute != null
            )
            {
                activeRoute.Expire();

                if (
                    activeRoute.gameObject != null
                )
                {
                    Destroy(
                        activeRoute.gameObject);
                }

                activeRoute = null;
                replacedWeaveCount++;
            }

            GameObject routeObject =
                new GameObject(
                    $"M51_TraceWeave_{weaveSuccessCount + 1:00}");

            activeRoute =
                routeObject.AddComponent<
                    PrototypeTraceWeaveRoute>();

            activeRoute.Build(
                buildPoints,
                weaveLifetimeSeconds,
                routeRadius);

            if (!activeRoute.ActiveRoute)
            {
                Destroy(
                    routeObject);

                activeRoute = null;

                Reject(
                    PrototypeTraceWeaveState.InsufficientTrace,
                    "Collider segmentli rota üretilemedi.");

                return false;
            }

            lastStartAnchor =
                startAnchor;

            lastEndAnchor =
                endAnchor;

            lastRouteLength =
                routeLength;

            lastBuiltPointCount =
                buildPoints.Count;

            weaveSuccessCount++;

            state =
                PrototypeTraceWeaveState.ActiveWeave;

            lastAction =
                $"İz sabitlendi: {routeLength:F1} m, " +
                $"{activeRoute.ColliderSegmentCount} capsule segment.";

            Debug.Log(
                "[M51.0 Trace Weave Created]\n" +
                $"Source={source}\n" +
                $"HistorySeconds={recordedHistorySeconds:F2}\n" +
                $"SourceSamples={storedSampleCount}\n" +
                $"BuiltPoints={lastBuiltPointCount}\n" +
                $"RouteLength={routeLength:F2}\n" +
                $"ColliderSegments={activeRoute.ColliderSegmentCount}\n" +
                $"LifetimeSeconds={weaveLifetimeSeconds:F2}\n" +
                "StartAnchorValidated=True\n" +
                "EndAnchorValidated=True\n" +
                "FreeAirGeometryEnabled=False\n" +
                "MaximumActiveWeaves=1\n" +
                "UsesCapsuleSegmentProxies=True\n" +
                "UsesMeshCollider=False\n" +
                "FullStainCanWeave=False\n" +
                "ContourThreadInventoryIntegrated=False\n" +
                "PainterCounterplayIntegrated=False\n" +
                "NetworkAuthorityEnabled=False",
                this);

            return true;
        }

        private void BuildSimplifiedPath()
        {
            buildPoints.Clear();

            int sourceCount =
                samples.Count;

            int targetCount =
                Mathf.Clamp(
                    maximumWeavePoints,
                    4,
                    32);

            if (
                sourceCount <=
                targetCount
            )
            {
                for (int index = 0;
                     index < sourceCount;
                     index++)
                {
                    buildPoints.Add(
                        samples[index].Position);
                }

                return;
            }

            float lastIndex =
                sourceCount -
                1;

            for (int targetIndex = 0;
                 targetIndex < targetCount;
                 targetIndex++)
            {
                float normalized =
                    targetCount <= 1
                        ? 0f
                        : targetIndex /
                          (float)(
                              targetCount - 1);

                float sourceIndex =
                    normalized *
                    lastIndex;

                int lower =
                    Mathf.FloorToInt(
                        sourceIndex);

                int upper =
                    Mathf.Min(
                        sourceCount - 1,
                        lower + 1);

                float blend =
                    sourceIndex -
                    lower;

                Vector3 point =
                    Vector3.Lerp(
                        samples[lower].Position,
                        samples[upper].Position,
                        blend);

                buildPoints.Add(
                    point);
            }
        }

        private bool TryResolveAnchor(
            Vector3 routePoint,
            bool isStart,
            out Vector3 anchor)
        {
            anchor = Vector3.zero;

            if (isStart)
            {
                startAnchorQueryCount++;
                startAnchorValid = false;
            }
            else
            {
                endAnchorQueryCount++;
                endAnchorValid = false;
            }

            Vector3 origin =
                routePoint +
                Vector3.up *
                anchorRayStartHeight;

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    Vector3.down,
                    anchorHits,
                    anchorRayDistance,
                    anchorMask,
                    QueryTriggerInteraction.Ignore);

            bool found = false;
            float nearestDistance =
                float.PositiveInfinity;

            RaycastHit best = default;

            for (int index = 0;
                 index < hitCount;
                 index++)
            {
                RaycastHit hit =
                    anchorHits[index];

                if (
                    hit.collider == null ||
                    (
                        figureRoot != null &&
                        (
                            hit.collider.transform ==
                                figureRoot ||
                            hit.collider.transform.IsChildOf(
                                figureRoot)
                        )
                    )
                )
                {
                    continue;
                }

                float slope =
                    Vector3.Angle(
                        hit.normal,
                        Vector3.up);

                if (
                    slope >
                    maximumAnchorSlope
                )
                {
                    continue;
                }

                if (
                    hit.distance <
                    nearestDistance
                )
                {
                    nearestDistance =
                        hit.distance;

                    best = hit;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            anchor =
                best.point;

            if (isStart)
            {
                startAnchorValid = true;
            }
            else
            {
                endAnchorValid = true;
            }

            return true;
        }

        private void Reject(
            PrototypeTraceWeaveState rejectState,
            string reason)
        {
            weaveRejectCount++;
            state = rejectState;
            lastAction = reason;
        }

        private static float CalculateLength(
            IReadOnlyList<Vector3> points)
        {
            float length = 0f;

            if (points == null)
            {
                return length;
            }

            for (int index = 1;
                 index < points.Count;
                 index++)
            {
                length +=
                    Vector3.Distance(
                        points[index - 1],
                        points[index]);
            }

            return length;
        }

        private void EnsurePreview()
        {
            if (previewLine != null)
            {
                return;
            }

            Shader shader =
                Shader.Find(
                    "Sprites/Default");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                previewMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M51_0_TracePreview_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject previewObject =
                new GameObject(
                    "M51_0_RecentTracePreview");

            previewObject.transform.SetParent(
                transform,
                false);

            previewLine =
                previewObject.AddComponent<
                    LineRenderer>();

            previewLine.useWorldSpace = true;
            previewLine.loop = false;
            previewLine.alignment =
                LineAlignment.View;

            previewLine.textureMode =
                LineTextureMode.Stretch;

            previewLine.numCapVertices = 3;
            previewLine.numCornerVertices = 3;
            previewLine.shadowCastingMode =
                ShadowCastingMode.Off;

            previewLine.receiveShadows = false;
            previewLine.widthMultiplier =
                0.035f;

            previewLine.sharedMaterial =
                previewMaterial;

            Color color =
                new Color(
                    0.82f,
                    0.82f,
                    0.70f,
                    0.48f);

            previewLine.startColor = color;
            previewLine.endColor = color;
            previewLine.enabled = false;
        }

        private void RefreshPreview()
        {
            if (
                previewLine == null ||
                !figureRoleActive ||
                fullStainBlocked ||
                samples.Count < 2
            )
            {
                HidePreview();
                return;
            }

            previewLine.positionCount =
                samples.Count;

            for (int index = 0;
                 index < samples.Count;
                 index++)
            {
                previewLine.SetPosition(
                    index,
                    samples[index].Position +
                    Vector3.up *
                    0.055f);
            }

            previewLine.enabled = true;
        }

        private void HidePreview()
        {
            if (previewLine != null)
            {
                previewLine.enabled = false;
            }
        }

        private void RefreshHud()
        {
            bool relevantNow =
                figureRoleActive &&
                (activeRoute != null && activeRoute.ActiveRoute ||
                 startAnchorValid ||
                 endAnchorValid ||
                 state == PrototypeTraceWeaveState.Ready ||
                 state == PrototypeTraceWeaveState.InsufficientTrace ||
                 state == PrototypeTraceWeaveState.StartAnchorInvalid ||
                 state == PrototypeTraceWeaveState.EndAnchorInvalid ||
                 state == PrototypeTraceWeaveState.RouteTooLong);

            bool visible =
                PrototypeRuntimeHudVisibilityUtility.Hold(
                    relevantNow,
                    ref hudVisibleUntilUnscaled,
                    contextualHudHoldSeconds);

            PrototypeRuntimeHudVisibilityUtility.ApplyCanvasGroup(
                statusGroup,
                visible);

            if (!visible)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    "İZ DOKUMA • KONTUR İPLİĞİ PROTOTİPİ";
            }

            if (stateText != null)
            {
                string active =
                    activeRoute != null &&
                    activeRoute.ActiveRoute
                        ? $"{activeRoute.RemainingSeconds:F1} sn"
                        : "YOK";

                stateText.text =
                    $"DURUM {TranslateState(state)}   " +
                    $"İZ {recordedHistorySeconds:F1}/{historySeconds:F0} sn   " +
                    $"ÖRNEK {storedSampleCount}\n" +
                    $"ANKRAJ {(startAnchorValid ? "A" : "-")}/" +
                    $"{(endAnchorValid ? "B" : "-")}   " +
                    $"AKTİF ROTA {active}   " +
                    $"LEKE {(fullStainBlocked ? "BLOKLU" : clarityLevel)}\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    fullStainBlocked
                        ? "FULL STAIN • KONTUR İPLİĞİ KULLANILAMAZ\n" +
                          "STAIN DESTEK YETENEKLERİ DEĞİŞMEDİ"
                        : "T • SON 4 sn İZİ SABİTLE   " +
                          "1 AKTİF DOKUMA   8 sn\n" +
                          "BAŞ/BİTİŞ ANKRAJI ZORUNLU • SERBEST HAVA ÇİZİMİ YOK";
            }
        }

        private static string TranslateState(
            PrototypeTraceWeaveState value)
        {
            switch (value)
            {
                case PrototypeTraceWeaveState.PainterRole:
                    return "PAINTER ROLÜ";

                case PrototypeTraceWeaveState.StainFormBlocked:
                    return "FULL STAIN";

                case PrototypeTraceWeaveState.Recording:
                    return "KAYDEDİLİYOR";

                case PrototypeTraceWeaveState.InsufficientTrace:
                    return "İZ YETERSİZ";

                case PrototypeTraceWeaveState.StartAnchorInvalid:
                    return "A ANKRAJI YOK";

                case PrototypeTraceWeaveState.EndAnchorInvalid:
                    return "B ANKRAJI YOK";

                case PrototypeTraceWeaveState.RouteTooLong:
                    return "ROTA ÇOK UZUN";

                case PrototypeTraceWeaveState.Ready:
                    return "HAZIR";

                case PrototypeTraceWeaveState.ActiveWeave:
                    return "DOKUMA AKTİF";

                default:
                    return "BEKLİYOR";
            }
        }

        private void OnDisable()
        {
            if (
                weaveAction != null &&
                weaveAction.enabled
            )
            {
                weaveAction.Disable();
            }

            inputActionActive = false;
            HidePreview();
        }

        private void OnDestroy()
        {
            if (weaveAction != null)
            {
                weaveAction.performed -=
                    HandleWeavePerformed;

                weaveAction.Dispose();
                weaveAction = null;
            }

            if (previewMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        previewMaterial);
                }
                else
                {
                    DestroyImmediate(
                        previewMaterial);
                }

                previewMaterial = null;
            }
        }
    }
}
