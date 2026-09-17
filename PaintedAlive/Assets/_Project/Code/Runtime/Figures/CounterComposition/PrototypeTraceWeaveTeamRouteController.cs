using System;
using System.Collections.Generic;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(180)]
    [DisallowMultipleComponent]
    public sealed class PrototypeTraceWeaveTeamRouteController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeTraceWeaveController traceWeave;

        [SerializeField] private MonoBehaviour figureMotor;
        [SerializeField] private Transform figureRoot;
        [SerializeField] private MonoBehaviour clarityState;

        [Header("Team Route Assist")]
        [SerializeField, Min(0.1f)]
        private float routeUseRadius = 0.62f;

        [SerializeField, Min(0f)]
        private float minimumMovementSpeed = 0.35f;

        [SerializeField, Range(0f, 1f)]
        private float minimumDirectionAlignment = 0.42f;

        [SerializeField, Min(0f)]
        private float followAssistSpeed = 0.85f;

        [Header("Heavy Load")]
        [SerializeField, Min(1f)]
        private float figureEquivalentLoadKg = 70f;

        [SerializeField, Min(1f)]
        private float heavyLoadThresholdKg = 150f;

        [SerializeField, Min(0.1f)]
        private float overloadHoldSeconds = 1.15f;

        [SerializeField, Min(0.1f)]
        private float overloadRecoverySeconds = 0.75f;

        [SerializeField, Min(0.1f)]
        private float loadProbeRadius = 0.58f;

        [SerializeField, Min(0.05f)]
        private float loadProbeInterval = 0.25f;

        [SerializeField]
        private LayerMask loadProbeMask = ~0;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Focus")]
        [SerializeField, Min(0f)] private float contextualHudHoldSeconds = 2.5f;

        [Header("Runtime Read Only")]
        [SerializeField] private bool figureRoleActive;
        [SerializeField] private bool fullStainBlocked;
        [SerializeField] private bool routeAvailable;
        [SerializeField] private bool routeVisualResolved;
        [SerializeField] private bool velocityContractResolved;
        [SerializeField] private bool velocityFallbackActive;
        [SerializeField] private bool onRoute;
        [SerializeField] private bool directionAligned;
        [SerializeField] private bool assistActive;
        [SerializeField] private bool overloaded;
        [SerializeField] private int routeBindCount;
        [SerializeField] private int routeEnterCount;
        [SerializeField] private int routeExitCount;
        [SerializeField] private int assistFrameCount;
        [SerializeField] private int overloadCollapseCount;
        [SerializeField] private int loadProbeCount;
        [SerializeField] private int simulatedLoadUseCount;
        [SerializeField] private float routeDistance = -1f;
        [SerializeField] private float routeProgress;
        [SerializeField] private float movementSpeed;
        [SerializeField] private float directionAlignment;
        [SerializeField] private float currentLoadKg;
        [SerializeField] private float overloadProgress;
        [SerializeField] private float assistedDistance;
        [SerializeField] private float simulatedLoadKg;
        [SerializeField] private float simulatedLoadUntil;
        [SerializeField] private string clarityLevel = "Unknown";
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private string lastAction =
            "Aktif İz Dokuma bekleniyor.";

        private readonly Collider[] loadHits =
            new Collider[64];

        private readonly HashSet<int> rigidbodyIds =
            new HashSet<int>();

        private readonly HashSet<int> characterIds =
            new HashSet<int>();

        private CharacterController characterController;
        private PropertyInfo velocityProperty;
        private PropertyInfo clarityLevelProperty;

        private PrototypeTraceWeaveRoute boundRoute;
        private LineRenderer boundRouteLine;

        private Vector3 nearestRoutePoint;
        private Vector3 routeTangent = Vector3.forward;
        private Vector3 movementVelocity;
        private Vector3 lastFigurePosition;
        private bool hasLastFigurePosition;
        private bool wasOnRoute;
        private float nextLoadProbeAt;
        private float hudVisibleUntilUnscaled;

        private LineRenderer assistArrow;
        private Material assistMaterial;

        public bool FigureRoleActive => figureRoleActive;
        public bool FullStainBlocked => fullStainBlocked;
        public bool RouteAvailable => routeAvailable;
        public bool RouteVisualResolved => routeVisualResolved;
        public bool VelocityContractResolved => velocityContractResolved;
        public bool VelocityFallbackActive => velocityFallbackActive;
        public bool OnRoute => onRoute;
        public bool DirectionAligned => directionAligned;
        public bool AssistActive => assistActive;
        public bool Overloaded => overloaded;
        public int RouteBindCount => routeBindCount;
        public int RouteEnterCount => routeEnterCount;
        public int RouteExitCount => routeExitCount;
        public int AssistFrameCount => assistFrameCount;
        public int OverloadCollapseCount => overloadCollapseCount;
        public int LoadProbeCount => loadProbeCount;
        public int SimulatedLoadUseCount => simulatedLoadUseCount;
        public float RouteDistance => routeDistance;
        public float RouteProgress => routeProgress;
        public float MovementSpeed => movementSpeed;
        public float DirectionAlignment => directionAlignment;
        public float CurrentLoadKg => currentLoadKg;
        public float OverloadProgress => overloadProgress;
        public float AssistedDistance => assistedDistance;
        public string ClarityLevel => clarityLevel;
        public string ResolvedRole => resolvedRole;
        public string LastAction => lastAction;

        public bool TeamRouteUseEnabled => true;
        public bool DirectionalFollowAssistEnabled => true;
        public bool HeavyLoadCollapseEnabled => true;
        public bool FullStainReceivesAssist => false;
        public bool UsesFigureMotorVelocityReadOnly => true;
        public bool MutatesFigureMovementConfig => false;
        public bool AppliesRawTransformTeleport => false;
        public bool PainterCounterplayIntegrated => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeTraceWeaveController configuredTraceWeave,
            MonoBehaviour configuredFigureMotor,
            Transform configuredFigureRoot,
            MonoBehaviour configuredClarityState,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            traceWeave = configuredTraceWeave;
            figureMotor = configuredFigureMotor;
            figureRoot = configuredFigureRoot;
            clarityState = configuredClarityState;
            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolveContracts();
            EnsureAssistArrow();
            RefreshHud();
        }

        private void Awake()
        {
            ResolveContracts();
            EnsureAssistArrow();
        }

        private void OnEnable()
        {
            ResolveContracts();
            EnsureAssistArrow();
        }

        private void Update()
        {
            RefreshAuthority();
            ResolveRoute();

            if (
                !figureRoleActive ||
                fullStainBlocked ||
                boundRoute == null ||
                !boundRoute.ActiveRoute ||
                !routeVisualResolved ||
                figureRoot == null
            )
            {
                ResetUsageState();
                UpdateLoadStress(0f);
                HideAssistArrow();
                RefreshHud();
                CaptureFallbackPosition();
                return;
            }

            UpdateRouteUsage();
            UpdateHeavyLoad();
            UpdateAssistArrow();
            RefreshHud();
            CaptureFallbackPosition();
        }

        private void LateUpdate()
        {
            ApplyDirectionalAssist();
        }

        public void ApplySimulatedHeavyLoad(
            float addedKilograms,
            float seconds)
        {
            simulatedLoadKg =
                Mathf.Max(0f, addedKilograms);

            simulatedLoadUntil =
                Time.unscaledTime +
                Mathf.Max(0.1f, seconds);

            simulatedLoadUseCount++;
            lastAction =
                $"Simüle ağır yük: +{simulatedLoadKg:F0} kg.";
        }

        public void ClearSimulatedLoad()
        {
            simulatedLoadKg = 0f;
            simulatedLoadUntil = 0f;
            lastAction = "Simüle ağır yük temizlendi.";
        }

        private void ResolveContracts()
        {
            characterController = null;
            velocityProperty = null;
            clarityLevelProperty = null;
            velocityContractResolved = false;

            if (figureRoot != null)
            {
                characterController =
                    figureRoot.GetComponent<CharacterController>();

                if (characterController == null)
                {
                    characterController =
                        figureRoot.GetComponentInChildren<
                            CharacterController>(true);
                }
            }

            if (figureMotor != null)
            {
                velocityProperty =
                    figureMotor.GetType().GetProperty(
                        "Velocity",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                velocityContractResolved =
                    velocityProperty != null &&
                    velocityProperty.PropertyType ==
                        typeof(Vector3);
            }

            if (clarityState != null)
            {
                clarityLevelProperty =
                    clarityState.GetType().GetProperty(
                        "CurrentLevel",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);
            }

            if (figureRoot != null)
            {
                lastFigurePosition = figureRoot.position;
                hasLastFigurePosition = true;
            }
        }

        private void RefreshAuthority()
        {
            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedRole);

            clarityLevel = ReadClarityLevel();

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
                clarityLevelProperty == null
            )
            {
                return "Unknown";
            }

            try
            {
                object value =
                    clarityLevelProperty.GetValue(
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
            boundRouteLine = null;
            routeVisualResolved = false;
            routeAvailable =
                boundRoute != null &&
                boundRoute.ActiveRoute;

            ResetUsageState();
            overloadProgress = 0f;
            currentLoadKg = 0f;

            if (boundRoute == null)
            {
                lastAction =
                    "Aktif İz Dokuma bekleniyor.";
                return;
            }

            LineRenderer[] lines =
                boundRoute.GetComponentsInChildren<
                    LineRenderer>(true);

            for (int index = 0;
                 index < lines.Length;
                 index++)
            {
                LineRenderer candidateLine = lines[index];

                if (candidateLine == null)
                {
                    continue;
                }

                if (
                    candidateLine.gameObject.name ==
                    "TraceWeaveVisual"
                )
                {
                    boundRouteLine = candidateLine;
                    break;
                }

                if (boundRouteLine == null)
                {
                    boundRouteLine = candidateLine;
                }
            }

            routeVisualResolved =
                boundRouteLine != null &&
                boundRouteLine.positionCount >= 2;

            routeBindCount++;

            lastAction = routeVisualResolved
                ? "Yeni İz Dokuma takım rotası bağlandı."
                : "İz Dokuma görsel rotası çözülemedi.";
        }

        private void UpdateRouteUsage()
        {
            onRoute = false;
            directionAligned = false;
            assistActive = false;
            routeDistance = -1f;
            routeProgress = 0f;
            movementSpeed = 0f;
            directionAlignment = 0f;

            if (
                !TryGetNearestRouteSample(
                    figureRoot.position,
                    out nearestRoutePoint,
                    out routeTangent,
                    out routeDistance,
                    out routeProgress)
            )
            {
                TrackRouteTransition();
                return;
            }

            onRoute =
                routeDistance <= routeUseRadius;

            movementVelocity =
                ReadFigureVelocity();

            movementSpeed =
                movementVelocity.magnitude;

            if (movementSpeed > 0.001f)
            {
                Vector3 movementDirection =
                    movementVelocity /
                    movementSpeed;

                float forwardAlignment =
                    Vector3.Dot(
                        movementDirection,
                        routeTangent);

                float backwardAlignment =
                    Vector3.Dot(
                        movementDirection,
                        -routeTangent);

                if (backwardAlignment > forwardAlignment)
                {
                    routeTangent = -routeTangent;
                    directionAlignment = backwardAlignment;
                }
                else
                {
                    directionAlignment = forwardAlignment;
                }
            }

            directionAligned =
                onRoute &&
                movementSpeed >= minimumMovementSpeed &&
                directionAlignment >= minimumDirectionAlignment;

            TrackRouteTransition();
        }

        private Vector3 ReadFigureVelocity()
        {
            velocityFallbackActive = false;

            if (
                figureMotor != null &&
                velocityContractResolved
            )
            {
                try
                {
                    object value =
                        velocityProperty.GetValue(
                            figureMotor);

                    if (value is Vector3 velocity)
                    {
                        return velocity;
                    }
                }
                catch (Exception)
                {
                    // Fall through to position-delta fallback.
                }
            }

            velocityFallbackActive = true;

            if (
                !hasLastFigurePosition ||
                figureRoot == null ||
                Time.deltaTime <= 0.0001f
            )
            {
                return Vector3.zero;
            }

            return
                (figureRoot.position - lastFigurePosition) /
                Time.deltaTime;
        }

        private void CaptureFallbackPosition()
        {
            if (figureRoot == null)
            {
                hasLastFigurePosition = false;
                return;
            }

            lastFigurePosition = figureRoot.position;
            hasLastFigurePosition = true;
        }

        private bool TryGetNearestRouteSample(
            Vector3 worldPosition,
            out Vector3 nearestPoint,
            out Vector3 tangent,
            out float distance,
            out float normalizedDistance)
        {
            nearestPoint = Vector3.zero;
            tangent = Vector3.forward;
            distance = float.PositiveInfinity;
            normalizedDistance = 0f;

            if (
                boundRouteLine == null ||
                boundRouteLine.positionCount < 2
            )
            {
                return false;
            }

            float totalLength = 0f;
            float bestSquaredDistance =
                float.PositiveInfinity;
            float bestAccumulatedLength = 0f;
            float accumulatedLength = 0f;

            Vector3 previous =
                GetRoutePointWorld(0);

            for (int index = 1;
                 index < boundRouteLine.positionCount;
                 index++)
            {
                Vector3 current =
                    GetRoutePointWorld(index);

                Vector3 segment =
                    current - previous;

                float segmentLength =
                    segment.magnitude;

                if (segmentLength <= 0.0001f)
                {
                    previous = current;
                    continue;
                }

                Vector3 direction =
                    segment / segmentLength;

                float projected =
                    Vector3.Dot(
                        worldPosition - previous,
                        direction);

                float clamped =
                    Mathf.Clamp(
                        projected,
                        0f,
                        segmentLength);

                Vector3 candidate =
                    previous +
                    direction * clamped;

                float squaredDistance =
                    (worldPosition - candidate)
                    .sqrMagnitude;

                if (squaredDistance < bestSquaredDistance)
                {
                    bestSquaredDistance = squaredDistance;
                    nearestPoint = candidate;
                    tangent = direction;
                    bestAccumulatedLength =
                        accumulatedLength + clamped;
                }

                accumulatedLength += segmentLength;
                totalLength += segmentLength;
                previous = current;
            }

            if (
                float.IsPositiveInfinity(
                    bestSquaredDistance)
            )
            {
                return false;
            }

            distance =
                Mathf.Sqrt(bestSquaredDistance);

            normalizedDistance =
                totalLength > 0.0001f
                    ? Mathf.Clamp01(
                        bestAccumulatedLength /
                        totalLength)
                    : 0f;

            return true;
        }

        private Vector3 GetRoutePointWorld(
            int index)
        {
            Vector3 point =
                boundRouteLine.GetPosition(index);

            return boundRouteLine.useWorldSpace
                ? point
                : boundRouteLine.transform.TransformPoint(
                    point);
        }

        private void TrackRouteTransition()
        {
            if (onRoute && !wasOnRoute)
            {
                routeEnterCount++;
                lastAction =
                    "Figür takım rotasına girdi.";
            }
            else if (!onRoute && wasOnRoute)
            {
                routeExitCount++;
                lastAction =
                    "Figür takım rotasından çıktı.";
            }

            wasOnRoute = onRoute;
        }

        private void ApplyDirectionalAssist()
        {
            assistActive = false;

            if (
                !figureRoleActive ||
                fullStainBlocked ||
                boundRoute == null ||
                !boundRoute.ActiveRoute ||
                !directionAligned ||
                characterController == null ||
                !characterController.enabled
            )
            {
                return;
            }

            float deltaTime =
                Mathf.Max(0f, Time.deltaTime);

            Vector3 assistDelta =
                routeTangent *
                followAssistSpeed *
                deltaTime;

            CollisionFlags flags =
                characterController.Move(
                    assistDelta);

            float actualDistance =
                assistDelta.magnitude;

            if (
                (flags & CollisionFlags.Sides) != 0
            )
            {
                actualDistance *= 0.35f;
            }

            assistedDistance += actualDistance;
            assistFrameCount++;
            assistActive = true;
            lastAction =
                "Takım rotası hareket yönünü destekliyor.";
        }

        private void UpdateHeavyLoad()
        {
            if (
                boundRoute == null ||
                !boundRoute.ActiveRoute ||
                boundRouteLine == null
            )
            {
                currentLoadKg = 0f;
                UpdateLoadStress(0f);
                return;
            }

            if (Time.unscaledTime >= nextLoadProbeAt)
            {
                nextLoadProbeAt =
                    Time.unscaledTime +
                    loadProbeInterval;

                currentLoadKg =
                    EstimateRouteLoad();

                if (Time.unscaledTime < simulatedLoadUntil)
                {
                    currentLoadKg += simulatedLoadKg;
                }
                else
                {
                    simulatedLoadKg = 0f;
                    simulatedLoadUntil = 0f;
                }

                loadProbeCount++;
            }

            float normalizedStress =
                heavyLoadThresholdKg > 0f
                    ? currentLoadKg /
                      heavyLoadThresholdKg
                    : 0f;

            UpdateLoadStress(
                normalizedStress);
        }

        private float EstimateRouteLoad()
        {
            rigidbodyIds.Clear();
            characterIds.Clear();

            float totalLoad = 0f;
            int pointCount =
                boundRouteLine.positionCount;

            int stride =
                Mathf.Max(
                    1,
                    pointCount / 8);

            for (int pointIndex = 0;
                 pointIndex < pointCount;
                 pointIndex += stride)
            {
                Vector3 point =
                    GetRoutePointWorld(pointIndex);

                int hitCount =
                    Physics.OverlapSphereNonAlloc(
                        point,
                        loadProbeRadius,
                        loadHits,
                        loadProbeMask,
                        QueryTriggerInteraction.Ignore);

                for (int hitIndex = 0;
                     hitIndex < hitCount;
                     hitIndex++)
                {
                    Collider collider =
                        loadHits[hitIndex];

                    if (
                        collider == null ||
                        collider.transform.IsChildOf(
                            boundRoute.transform)
                    )
                    {
                        continue;
                    }

                    Rigidbody body =
                        collider.attachedRigidbody;

                    if (body != null)
                    {
                        int bodyId =
                            body.GetInstanceID();

                        if (rigidbodyIds.Add(bodyId))
                        {
                            totalLoad +=
                                Mathf.Max(0f, body.mass);
                        }

                        continue;
                    }

                    CharacterController character =
                        collider.GetComponentInParent<
                            CharacterController>();

                    if (character == null)
                    {
                        continue;
                    }

                    if (
                        characterController != null &&
                        character == characterController
                    )
                    {
                        continue;
                    }

                    int characterId =
                        character.GetInstanceID();

                    if (characterIds.Add(characterId))
                    {
                        totalLoad +=
                            figureEquivalentLoadKg;
                    }
                }
            }

            if (onRoute)
            {
                totalLoad +=
                    figureEquivalentLoadKg;
            }

            return totalLoad;
        }

        private void UpdateLoadStress(
            float normalizedStress)
        {
            overloaded =
                normalizedStress > 1f;

            float deltaTime =
                Mathf.Max(0f, Time.deltaTime);

            if (overloaded)
            {
                overloadProgress +=
                    deltaTime /
                    Mathf.Max(
                        0.1f,
                        overloadHoldSeconds);
            }
            else
            {
                overloadProgress -=
                    deltaTime /
                    Mathf.Max(
                        0.1f,
                        overloadRecoverySeconds);
            }

            overloadProgress =
                Mathf.Clamp01(overloadProgress);

            if (
                overloadProgress < 1f ||
                boundRoute == null ||
                !boundRoute.ActiveRoute
            )
            {
                return;
            }

            boundRoute.Expire();
            overloadCollapseCount++;
            assistActive = false;
            onRoute = false;
            lastAction =
                $"Ağır yük dokumayı çözdü: {currentLoadKg:F0} kg.";

            Debug.Log(
                "[M51.1 Trace Weave Collapsed Under Load]\n" +
                $"MeasuredLoadKg={currentLoadKg:F1}\n" +
                $"ThresholdKg={heavyLoadThresholdKg:F1}\n" +
                $"OverloadHoldSeconds={overloadHoldSeconds:F2}\n" +
                "HeavyLoadCollapseEnabled=True\n" +
                "PainterCounterplayIntegrated=False\n" +
                "NetworkAuthorityEnabled=False",
                this);
        }

        private void EnsureAssistArrow()
        {
            if (assistArrow != null)
            {
                return;
            }

            Shader shader =
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader = Shader.Find(
                    "Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                assistMaterial =
                    new Material(shader)
                    {
                        name =
                            "M51_1_TeamRouteAssist_Runtime",
                        hideFlags = HideFlags.DontSave
                    };
            }

            GameObject arrowObject =
                new GameObject(
                    "M51_1_TeamRouteDirectionGuide");

            arrowObject.transform.SetParent(
                transform,
                false);

            assistArrow =
                arrowObject.AddComponent<LineRenderer>();

            assistArrow.useWorldSpace = true;
            assistArrow.loop = false;
            assistArrow.alignment = LineAlignment.View;
            assistArrow.textureMode = LineTextureMode.Stretch;
            assistArrow.numCapVertices = 3;
            assistArrow.numCornerVertices = 3;
            assistArrow.shadowCastingMode =
                ShadowCastingMode.Off;
            assistArrow.receiveShadows = false;
            assistArrow.widthMultiplier = 0.045f;
            assistArrow.sharedMaterial = assistMaterial;

            Color color =
                new Color(
                    0.92f,
                    0.94f,
                    0.80f,
                    0.88f);

            assistArrow.startColor = color;
            assistArrow.endColor = color;
            assistArrow.enabled = false;
        }

        private void UpdateAssistArrow()
        {
            if (
                assistArrow == null ||
                !onRoute ||
                !directionAligned ||
                boundRoute == null
            )
            {
                HideAssistArrow();
                return;
            }

            Vector3 start =
                nearestRoutePoint +
                Vector3.up * 0.14f;

            Vector3 end =
                start +
                routeTangent * 0.82f;

            Vector3 side =
                Vector3.Cross(
                    routeTangent,
                    Vector3.up);

            if (side.sqrMagnitude < 0.001f)
            {
                side = Vector3.right;
            }
            else
            {
                side.Normalize();
            }

            assistArrow.positionCount = 4;
            assistArrow.SetPosition(0, start);
            assistArrow.SetPosition(1, end);
            assistArrow.SetPosition(
                2,
                end - routeTangent * 0.20f +
                side * 0.12f);
            assistArrow.SetPosition(3, end);
            assistArrow.enabled = true;
        }

        private void HideAssistArrow()
        {
            if (assistArrow != null)
            {
                assistArrow.enabled = false;
            }
        }

        private void ResetUsageState()
        {
            if (wasOnRoute)
            {
                routeExitCount++;
            }

            wasOnRoute = false;
            onRoute = false;
            directionAligned = false;
            assistActive = false;
            routeDistance = -1f;
            routeProgress = 0f;
            movementSpeed = 0f;
            directionAlignment = 0f;
        }

        private void RefreshHud()
        {
            bool relevantNow =
                figureRoleActive &&
                routeAvailable &&
                (assistActive ||
                 onRoute ||
                 overloaded ||
                 currentLoadKg > 0.01f ||
                 (routeDistance >= 0f && routeDistance <= routeUseRadius * 1.75f));

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
                    "İZ DOKUMA • TAKIM ROTASI / YÜK";
            }

            if (stateText != null)
            {
                string routeState = routeAvailable
                    ? "AKTİF"
                    : "YOK";

                string useState = assistActive
                    ? "DESTEK"
                    : onRoute
                        ? "ÜZERİNDE"
                        : "DIŞINDA";

                stateText.text =
                    $"ROTA {routeState}   KULLANIM {useState}   " +
                    $"HIZ {movementSpeed:F1} m/s\n" +
                    $"YÜK {currentLoadKg:F0}/{heavyLoadThresholdKg:F0} kg   " +
                    $"STRES %{overloadProgress * 100f:F0}   " +
                    $"MESAFE {(routeDistance >= 0f ? routeDistance.ToString("F2") : "-")} m\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text = fullStainBlocked
                    ? "FULL STAIN • TAKIM ROTASI İVMESİ YOK\n" +
                      "MEVCUT STAIN YETENEKLERİ DEĞİŞMEDİ"
                    : "ROTA YÖNÜNDE HAREKET ET → KÜÇÜK TAKİP İVMESİ\n" +
                      "AĞIR YÜK EŞİĞİ AŞILIRSA DOKUMA ERKEN ÇÖZÜLÜR";
            }
        }

        private void OnDisable()
        {
            HideAssistArrow();
            ResetUsageState();
        }

        private void OnDestroy()
        {
            if (assistMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(assistMaterial);
            }
            else
            {
                DestroyImmediate(assistMaterial);
            }

            assistMaterial = null;
        }
    }
}
