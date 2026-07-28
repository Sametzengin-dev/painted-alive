using System;
using System.Collections;
using System.Reflection;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FigureClarityState))]
    [RequireComponent(typeof(FigureInputReader))]
    public sealed class StainWatercolorFlowController : MonoBehaviour
    {
        private const BindingFlags ReflectionFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private readonly Collider[] flowHits = new Collider[24];
        private readonly Collider[] overlapHits = new Collider[32];

        [Header("Configuration")]
        [SerializeField] private StainWatercolorFlowConfig config;

        [Header("Figure Dependencies")]
        [SerializeField] private FigureClarityState clarityState;
        [SerializeField] private FigureInputReader inputReader;
        [SerializeField] private FigureMotor figureMotor;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Behaviour surfaceCrawlController;
        [SerializeField] private Camera figureCamera;

        [Header("Existing Exclusive Stain Abilities")]
        [SerializeField] private Behaviour[] exclusiveStateBehaviours = Array.Empty<Behaviour>();

        [Header("Runtime - Read Only")]
        [SerializeField] private bool isRidingFlow;
        [SerializeField] private bool isTransitioningFlow;
        [SerializeField] private WatercolorFlowSourceAdapter currentSource;
        [SerializeField] private WatercolorFlowSourceAdapter entryCandidateSource;
        [SerializeField] private Vector3 currentVelocity;
        [SerializeField] private Vector3 observedRootVelocity;
        [SerializeField] private Vector3 smoothedFlowDirection;
        [SerializeField] private Vector3 lastSurfaceNormal = Vector3.up;
        [SerializeField] private float entryCandidateTime;
        [SerializeField] private float rideElapsedTime;
        [SerializeField] private float missingFlowTime;
        [SerializeField] private float reentryBlockedUntil;

        private bool crawlWasEnabled;
        private bool motorWasEnabled;
        private bool controllerWasEnabled;
        private bool hasObservedRootPosition;
        private Vector3 previousObservedRootPosition;
        private Vector3 velocitySmoothReference;
        private Coroutine exitRoutine;
        private float nextAdapterRefreshTime;

        public bool IsRidingFlow => isRidingFlow;
        public WatercolorFlowSourceAdapter CurrentSource => currentSource;
        public Vector3 CurrentVelocity => currentVelocity;

        public void Configure(
            StainWatercolorFlowConfig flowConfig,
            FigureClarityState figureClarity,
            FigureInputReader figureInput,
            FigureMotor motor,
            CharacterController controller,
            Behaviour crawlController,
            Camera roleCamera,
            Behaviour[] exclusiveBehaviours)
        {
            config = flowConfig;
            clarityState = figureClarity;
            inputReader = figureInput;
            figureMotor = motor;
            characterController = controller;
            surfaceCrawlController = crawlController;
            figureCamera = roleCamera;
            exclusiveStateBehaviours = exclusiveBehaviours ?? Array.Empty<Behaviour>();
        }

        private void Awake()
        {
            ResolveMissingDependencies();
            previousObservedRootPosition = transform.position;
            hasObservedRootPosition = true;

            if (config == null ||
                clarityState == null ||
                inputReader == null ||
                characterController == null ||
                surfaceCrawlController == null)
            {
                Debug.LogError(
                    "StainWatercolorFlowController requires M25 clarity, M28 surface crawl, " +
                    "FigureInputReader, CharacterController and an M34 config.",
                    this);

                enabled = false;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            UpdateObservedRootVelocity(deltaTime);

            if (isTransitioningFlow)
            {
                return;
            }

            if (isRidingFlow)
            {
                UpdateFlowRide(deltaTime);
                return;
            }

            UpdateEntrySearch(deltaTime);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ForceRestoreMovementAuthority();
        }

        private void ResolveMissingDependencies()
        {
            clarityState ??= GetComponent<FigureClarityState>();
            inputReader ??= GetComponent<FigureInputReader>();
            figureMotor ??= GetComponent<FigureMotor>();
            characterController ??= GetComponent<CharacterController>();

            if (surfaceCrawlController == null)
            {
                surfaceCrawlController = FindSurfaceCrawlController();
            }

            if (figureCamera == null)
            {
                figureCamera = Camera.main;
            }
        }

        private bool CanEnterFlow()
        {
            return config != null &&
                   !isRidingFlow &&
                   !isTransitioningFlow &&
                   exitRoutine == null &&
                   Time.time >= reentryBlockedUntil &&
                   clarityState != null &&
                   clarityState.CurrentLevel == FigureClarityLevel.Stain &&
                   surfaceCrawlController != null &&
                   surfaceCrawlController.enabled &&
                   !HasExclusiveState();
        }

        private void UpdateEntrySearch(float deltaTime)
        {
            if (!CanEnterFlow())
            {
                ResetEntryCandidate();
                return;
            }

            if (!TryFindBestFlow(
                    out WatercolorFlowSourceAdapter source,
                    out StainWatercolorFlowSample sample))
            {
                ResetEntryCandidate();
                return;
            }

            if (source != entryCandidateSource)
            {
                entryCandidateSource = source;
                entryCandidateTime = 0f;
                return;
            }

            entryCandidateTime += deltaTime;
            if (entryCandidateTime < config.EntryConfirmationDuration)
            {
                return;
            }

            ResetEntryCandidate();
            EnterFlow(source, sample);
        }

        private void ResetEntryCandidate()
        {
            entryCandidateSource = null;
            entryCandidateTime = 0f;
        }

        private void UpdateObservedRootVelocity(float deltaTime)
        {
            Vector3 currentPosition = transform.position;
            if (!hasObservedRootPosition || deltaTime <= 0.00001f)
            {
                previousObservedRootPosition = currentPosition;
                hasObservedRootPosition = true;
                return;
            }

            Vector3 measuredVelocity =
                (currentPosition - previousObservedRootPosition) / deltaTime;

            float maximumObservedSpeed = config != null
                ? Mathf.Max(10f, config.MaximumFlowSpeed * 2f)
                : 15f;

            measuredVelocity = Vector3.ClampMagnitude(
                measuredVelocity,
                maximumObservedSpeed);

            float smoothing = 1f - Mathf.Exp(-12f * deltaTime);
            observedRootVelocity = Vector3.Lerp(
                observedRootVelocity,
                measuredVelocity,
                smoothing);

            previousObservedRootPosition = currentPosition;
        }

        private void EnterFlow(
            WatercolorFlowSourceAdapter source,
            StainWatercolorFlowSample sample)
        {
            if (source == null || isRidingFlow || isTransitioningFlow)
            {
                return;
            }

            isTransitioningFlow = true;

            crawlWasEnabled = surfaceCrawlController != null && surfaceCrawlController.enabled;
            motorWasEnabled = figureMotor != null && figureMotor.enabled;
            controllerWasEnabled = characterController != null && characterController.enabled;

            Vector3 resolvedNormal = sample.SurfaceNormal.sqrMagnitude > 0.0001f
                ? sample.SurfaceNormal.normalized
                : Vector3.up;

            Vector3 resolvedDirection = Vector3.ProjectOnPlane(
                sample.Direction,
                resolvedNormal);

            if (resolvedDirection.sqrMagnitude < 0.0001f)
            {
                resolvedDirection = Vector3.ProjectOnPlane(
                    transform.forward,
                    resolvedNormal);
            }

            if (resolvedDirection.sqrMagnitude < 0.0001f)
            {
                resolvedDirection = Vector3.Cross(resolvedNormal, Vector3.right);
            }

            resolvedDirection.Normalize();

            Vector3 inheritedVelocity = Vector3.ProjectOnPlane(
                observedRootVelocity,
                resolvedNormal);

            inheritedVelocity = Vector3.ClampMagnitude(
                inheritedVelocity,
                config.MaximumFlowSpeed + config.SteeringSpeed);

            Vector3 targetVelocity =
                resolvedDirection * ClampFlowSpeed(sample.Speed);

            currentVelocity = Vector3.Lerp(
                inheritedVelocity,
                targetVelocity,
                config.EntryTargetVelocityBlend);

            velocitySmoothReference = Vector3.zero;
            smoothedFlowDirection = resolvedDirection;
            lastSurfaceNormal = resolvedNormal;
            rideElapsedTime = 0f;
            missingFlowTime = 0f;
            currentSource = source;

            // Movement authority is transferred once. M28 and FigureMotor stay
            // suspended until the exit coroutine returns authority once.
            if (surfaceCrawlController != null)
            {
                surfaceCrawlController.enabled = false;
            }

            if (figureMotor != null)
            {
                figureMotor.enabled = false;
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            isRidingFlow = true;
            isTransitioningFlow = false;
        }

        private void UpdateFlowRide(float deltaTime)
        {
            if (clarityState == null ||
                clarityState.CurrentLevel != FigureClarityLevel.Stain ||
                HasExclusiveState())
            {
                BeginExitFlow(false);
                return;
            }

            rideElapsedTime += deltaTime;

            if (currentSource == null ||
                !currentSource.TrySample(
                    transform.position,
                    out StainWatercolorFlowSample sample))
            {
                CoastDuringMissingSample(deltaTime);
                return;
            }

            float maximumAttachmentDistance = config.DetectionRadius * 1.65f;
            if ((sample.ClosestPoint - transform.position).sqrMagnitude >
                maximumAttachmentDistance * maximumAttachmentDistance)
            {
                CoastDuringMissingSample(deltaTime);
                return;
            }

            missingFlowTime = 0f;

            Vector3 sampledNormal = sample.SurfaceNormal.sqrMagnitude > 0.0001f
                ? sample.SurfaceNormal.normalized
                : lastSurfaceNormal.sqrMagnitude > 0.0001f
                    ? lastSurfaceNormal.normalized
                    : Vector3.up;

            float normalBlend = 1f - Mathf.Exp(
                -config.SurfaceNormalResponsiveness * deltaTime);

            lastSurfaceNormal = Vector3.Slerp(
                lastSurfaceNormal,
                sampledNormal,
                normalBlend).normalized;

            Vector3 sampledDirection = Vector3.ProjectOnPlane(
                sample.Direction,
                lastSurfaceNormal);

            if (sampledDirection.sqrMagnitude < 0.0001f)
            {
                sampledDirection = smoothedFlowDirection;
            }

            if (sampledDirection.sqrMagnitude < 0.0001f)
            {
                sampledDirection = Vector3.ProjectOnPlane(
                    transform.forward,
                    lastSurfaceNormal);
            }

            sampledDirection.Normalize();

            float directionBlend = 1f - Mathf.Exp(
                -config.DirectionResponsiveness * deltaTime);

            smoothedFlowDirection = Vector3.Slerp(
                smoothedFlowDirection,
                sampledDirection,
                directionBlend).normalized;

            Vector3 steering = ResolveSteering(lastSurfaceNormal);
            Vector3 targetVelocity =
                smoothedFlowDirection * ClampFlowSpeed(sample.Speed) +
                steering * config.SteeringSpeed;

            currentVelocity = Vector3.SmoothDamp(
                currentVelocity,
                targetVelocity,
                ref velocitySmoothReference,
                config.VelocitySmoothTime,
                Mathf.Max(config.VelocityAcceleration, config.MaximumFlowSpeed),
                deltaTime);

            Vector3 predictedPosition =
                transform.position + currentVelocity * deltaTime;

            if (currentSource.TrySample(
                    predictedPosition,
                    out StainWatercolorFlowSample predictedSample))
            {
                float predictedAttachmentDistance = Vector3.Distance(
                    predictedSample.ClosestPoint,
                    predictedPosition);

                if (predictedAttachmentDistance <= maximumAttachmentDistance)
                {
                    predictedPosition = ApplySurfaceNormalAdhesion(
                        predictedPosition,
                        predictedSample,
                        deltaTime);
                }
            }

            transform.position = predictedPosition;
        }

        private Vector3 ApplySurfaceNormalAdhesion(
            Vector3 predictedPosition,
            StainWatercolorFlowSample sample,
            float deltaTime)
        {
            Vector3 surfaceNormal = sample.SurfaceNormal.sqrMagnitude > 0.0001f
                ? sample.SurfaceNormal.normalized
                : lastSurfaceNormal.sqrMagnitude > 0.0001f
                    ? lastSurfaceNormal.normalized
                    : Vector3.up;

            Vector3 desiredSurfacePosition =
                sample.ClosestPoint +
                surfaceNormal * config.SurfaceOffset;

            // Only correct separation along the surface normal. Correcting the
            // full vector would also pull the Stain backwards along the flow
            // tangent at a mesh edge. That caused an end-of-flow position loop
            // and visible camera shaking on M13's finite non-convex mesh.
            float signedNormalDistance = Vector3.Dot(
                desiredSurfacePosition - predictedPosition,
                surfaceNormal);

            float maximumCorrection =
                config.SurfaceAdhesionSpeed * deltaTime;

            float normalCorrection = Mathf.Clamp(
                signedNormalDistance,
                -maximumCorrection,
                maximumCorrection);

            return predictedPosition + surfaceNormal * normalCorrection;
        }


        private void CoastDuringMissingSample(float deltaTime)
        {
            missingFlowTime += deltaTime;

            float drag = Mathf.Exp(-config.MissingSampleDrag * deltaTime);
            currentVelocity *= drag;
            velocitySmoothReference *= drag;

            transform.position += currentVelocity * deltaTime;

            if (rideElapsedTime >= config.MinimumRideDuration &&
                missingFlowTime >= config.ExitGraceDuration)
            {
                BeginExitFlow(true);
            }
        }

        private Vector3 ResolveSteering(Vector3 surfaceNormal)
        {
            Vector2 moveInput = inputReader != null ? inputReader.Move : Vector2.zero;
            if (moveInput.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            Camera cameraToUse = ResolveActiveCamera();
            Vector3 forward = cameraToUse != null ? cameraToUse.transform.forward : transform.forward;
            Vector3 right = cameraToUse != null ? cameraToUse.transform.right : transform.right;

            forward = Vector3.ProjectOnPlane(forward, surfaceNormal);
            right = Vector3.ProjectOnPlane(right, surfaceNormal);

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.Cross(surfaceNormal, right);
            }

            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.Cross(forward, surfaceNormal);
            }

            forward.Normalize();
            right.Normalize();

            Vector3 steering = forward * moveInput.y + right * moveInput.x;
            return Vector3.ClampMagnitude(steering, 1f);
        }

        private Camera ResolveActiveCamera()
        {
            if (figureCamera != null && figureCamera.isActiveAndEnabled)
            {
                return figureCamera;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.isActiveAndEnabled)
            {
                figureCamera = mainCamera;
                return mainCamera;
            }

            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].isActiveAndEnabled)
                {
                    figureCamera = cameras[i];
                    return figureCamera;
                }
            }

            return figureCamera;
        }

        private bool TryFindBestFlow(
            out WatercolorFlowSourceAdapter bestSource,
            out StainWatercolorFlowSample bestSample)
        {
            bestSource = null;
            bestSample = default;
            float bestDistanceSquared = float.PositiveInfinity;

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                config.DetectionRadius,
                flowHits,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = flowHits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                WatercolorFlowSourceAdapter adapter = ResolveAdapter(hit);
                if (adapter == null ||
                    !adapter.TrySample(transform.position, out StainWatercolorFlowSample sample))
                {
                    continue;
                }

                float distanceSquared =
                    (sample.ClosestPoint - transform.position).sqrMagnitude;

                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestSource = adapter;
                    bestSample = sample;
                }
            }

            return bestSource != null;
        }

        private WatercolorFlowSourceAdapter ResolveAdapter(Collider hit)
        {
            WatercolorFlowSourceAdapter adapter =
                hit.GetComponentInParent<WatercolorFlowSourceAdapter>();

            if (adapter != null)
            {
                return adapter;
            }

            if (Time.unscaledTime < nextAdapterRefreshTime)
            {
                return null;
            }

            MonoBehaviour source = FindWatercolorSource(hit);
            Transform adapterHost = source != null ? source.transform : FindNamedFlowRoot(hit.transform);

            if (adapterHost == null)
            {
                return null;
            }

            adapter = adapterHost.GetComponent<WatercolorFlowSourceAdapter>();
            if (adapter == null)
            {
                adapter = adapterHost.gameObject.AddComponent<WatercolorFlowSourceAdapter>();
            }

            adapter.Configure(source, hit, config.FallbackFlowSpeed);
            nextAdapterRefreshTime = Time.unscaledTime + config.AdapterRefreshInterval;
            return adapter;
        }

        private static MonoBehaviour FindWatercolorSource(Collider hit)
        {
            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>(true);
            MonoBehaviour best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour is WatercolorFlowSourceAdapter)
                {
                    continue;
                }

                string name = behaviour.GetType().Name;
                int score = ScoreWatercolorSource(name);
                if (score > bestScore)
                {
                    best = behaviour;
                    bestScore = score;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private static int ScoreWatercolorSource(string typeName)
        {
            if (typeName.IndexOf("Watercolor", StringComparison.OrdinalIgnoreCase) < 0 ||
                typeName.IndexOf("Flow", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return int.MinValue;
            }

            if (ContainsAny(
                    typeName,
                    "Interactor",
                    "Body",
                    "Debug",
                    "Spawner",
                    "Fixative",
                    "Reaction",
                    "Adapter"))
            {
                return -100;
            }

            int score = 10;
            if (typeName.Equals("WatercolorFlowSurface", StringComparison.OrdinalIgnoreCase))
            {
                score += 100;
            }

            if (typeName.IndexOf("Surface", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 50;
            }

            if (typeName.IndexOf("Runtime", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;
            }

            return score;
        }

        private static Transform FindNamedFlowRoot(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                if (current.name.IndexOf("WatercolorFlow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (current.name.IndexOf("Watercolor", StringComparison.OrdinalIgnoreCase) >= 0 &&
                     current.name.IndexOf("Flow", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private void BeginExitFlow(bool preserveMomentum)
        {
            if (!isRidingFlow || isTransitioningFlow || exitRoutine != null)
            {
                return;
            }

            isTransitioningFlow = true;
            exitRoutine = StartCoroutine(ExitFlowRoutine(preserveMomentum));
        }

        private IEnumerator ExitFlowRoutine(bool preserveMomentum)
        {
            WatercolorFlowSourceAdapter exitingSource = currentSource;
            Vector3 exitVelocity = currentVelocity;

            isRidingFlow = false;
            currentSource = null;
            missingFlowTime = 0f;
            rideElapsedTime = 0f;
            ResetEntryCandidate();

            if (preserveMomentum &&
                config.ExitGlideDuration > 0.0001f &&
                exitVelocity.sqrMagnitude > 0.0001f)
            {
                float elapsed = 0f;
                while (elapsed < config.ExitGlideDuration)
                {
                    float deltaTime = Time.deltaTime;
                    if (deltaTime <= 0f)
                    {
                        yield return null;
                        continue;
                    }

                    elapsed += deltaTime;
                    float normalizedTime = Mathf.Clamp01(
                        elapsed / config.ExitGlideDuration);

                    float easedTime = normalizedTime * normalizedTime *
                        (3f - 2f * normalizedTime);

                    float retention = Mathf.Lerp(
                        1f,
                        config.ExitVelocityRetention,
                        easedTime);

                    currentVelocity = exitVelocity * retention;
                    transform.position += currentVelocity * deltaTime;
                    yield return null;
                }
            }

            currentVelocity = Vector3.zero;
            velocitySmoothReference = Vector3.zero;

            Vector3 exitNormal = lastSurfaceNormal.sqrMagnitude > 0.0001f
                ? lastSurfaceNormal.normalized
                : Vector3.up;

            Vector3 preferredPosition =
                transform.position + exitNormal * config.ExitNudge;

            transform.position = ResolveSafeExitPosition(
                preferredPosition,
                exitingSource != null ? exitingSource.SurfaceCollider : null);

            bool stillStain =
                clarityState != null &&
                clarityState.CurrentLevel == FigureClarityLevel.Stain;

            // Restore the CharacterController only once, after the glide has
            // moved the Stain beyond the flow edge. This prevents the previous
            // enter/exit authority ping-pong visible in the Inspector.
            if (characterController != null &&
                (controllerWasEnabled || stillStain))
            {
                characterController.enabled = true;
            }

            if (figureMotor != null)
            {
                figureMotor.enabled = false;
            }

            yield return null;

            stillStain =
                clarityState != null &&
                clarityState.CurrentLevel == FigureClarityLevel.Stain;

            if (stillStain &&
                crawlWasEnabled &&
                surfaceCrawlController != null &&
                !HasExclusiveState())
            {
                surfaceCrawlController.enabled = true;
            }
            else if (!stillStain &&
                     figureMotor != null &&
                     motorWasEnabled &&
                     characterController != null &&
                     characterController.enabled)
            {
                figureMotor.enabled = true;
            }

            reentryBlockedUntil = Time.time + config.ReentryCooldown;
            previousObservedRootPosition = transform.position;
            observedRootVelocity = Vector3.zero;
            hasObservedRootPosition = true;
            exitRoutine = null;
            isTransitioningFlow = false;
        }

        private Vector3 ResolveSafeExitPosition(
            Vector3 preferredPosition,
            Collider ignoredFlowCollider)
        {
            Vector3 normal = lastSurfaceNormal.sqrMagnitude > 0.0001f
                ? lastSurfaceNormal.normalized
                : Vector3.up;

            float radius = characterController != null
                ? Mathf.Max(0.05f, characterController.radius)
                : 0.35f;

            Vector3[] candidates =
            {
                preferredPosition,
                preferredPosition + normal * radius,
                preferredPosition + Vector3.up * radius,
                transform.position,
                transform.position + normal * radius,
                transform.position + Vector3.up * radius
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (IsCharacterCapsuleFree(candidates[i], ignoredFlowCollider))
                {
                    return candidates[i];
                }
            }

            return preferredPosition;
        }

        private bool IsCharacterCapsuleFree(
            Vector3 rootPosition,
            Collider ignoredFlowCollider)
        {
            if (characterController == null)
            {
                return true;
            }

            float radius = Mathf.Max(0.02f, characterController.radius - 0.01f);
            float height = Mathf.Max(characterController.height, radius * 2f);
            Vector3 worldCenter =
                rootPosition + transform.rotation * characterController.center;

            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 axis = transform.up * halfSegment;

            int count = Physics.OverlapCapsuleNonAlloc(
                worldCenter + axis,
                worldCenter - axis,
                radius,
                overlapHits,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider overlap = overlapHits[i];
                if (overlap == null ||
                    overlap == ignoredFlowCollider ||
                    overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private bool HasExclusiveState()
        {
            for (int i = 0; i < exclusiveStateBehaviours.Length; i++)
            {
                Behaviour behaviour = exclusiveStateBehaviours[i];
                if (behaviour == null || !behaviour.isActiveAndEnabled)
                {
                    continue;
                }

                if (TryReadActiveState(behaviour, out bool active) && active)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadActiveState(
            Behaviour behaviour,
            out bool active)
        {
            active = false;
            string[] names =
            {
                "IsHijacking",
                "IsHijacked",
                "IsPossessing",
                "IsCarried",
                "IsInsideSponge",
                "IsTraversing",
                "IsInTraversal",
                "IsPreparing",
                "IsPreparingImprint",
                "IsPlacingImprint",
                "IsInsideCarrier",
                "IsInsideSponge"
            };

            Type type = behaviour.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                PropertyInfo property = type.GetProperty(names[i], ReflectionFlags);
                if (property != null &&
                    property.PropertyType == typeof(bool) &&
                    property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        active = (bool)property.GetValue(behaviour);
                        return true;
                    }
                    catch (Exception)
                    {
                        // Prototype compatibility path; ignore inaccessible diagnostics.
                    }
                }

                FieldInfo field = type.GetField(names[i], ReflectionFlags);
                if (field != null && field.FieldType == typeof(bool))
                {
                    try
                    {
                        active = (bool)field.GetValue(behaviour);
                        return true;
                    }
                    catch (Exception)
                    {
                        // Prototype compatibility path; ignore inaccessible diagnostics.
                    }
                }
            }

            return false;
        }

        private Behaviour FindSurfaceCrawlController()
        {
            Behaviour[] behaviours = GetComponents<Behaviour>();
            Behaviour best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this)
                {
                    continue;
                }

                string name = behaviour.GetType().Name;
                int score = 0;

                if (name.Equals("StainSurfaceCrawlController", StringComparison.OrdinalIgnoreCase))
                {
                    score = 100;
                }
                else if (name.IndexOf("Stain", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         name.IndexOf("Surface", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         name.IndexOf("Crawl", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score = 80;
                }
                else if (name.IndexOf("Stain", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         name.IndexOf("Crawl", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score = 40;
                }

                if (score > bestScore)
                {
                    best = behaviour;
                    bestScore = score;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private float ClampFlowSpeed(float speed)
        {
            float resolved = speed > 0.001f ? speed : config.FallbackFlowSpeed;
            return Mathf.Clamp(
                resolved,
                config.MinimumFlowSpeed,
                config.MaximumFlowSpeed);
        }

        private void ForceRestoreMovementAuthority()
        {
            if (exitRoutine != null)
            {
                StopCoroutine(exitRoutine);
                exitRoutine = null;
            }

            bool hadFlowAuthority = isRidingFlow || isTransitioningFlow;

            isRidingFlow = false;
            isTransitioningFlow = false;
            currentSource = null;
            currentVelocity = Vector3.zero;
            velocitySmoothReference = Vector3.zero;
            missingFlowTime = 0f;
            rideElapsedTime = 0f;
            ResetEntryCandidate();

            if (!hadFlowAuthority)
            {
                return;
            }

            bool stillStain =
                clarityState != null &&
                clarityState.CurrentLevel == FigureClarityLevel.Stain;

            if (characterController != null &&
                (controllerWasEnabled || stillStain))
            {
                characterController.enabled = true;
            }

            if (stillStain &&
                surfaceCrawlController != null &&
                crawlWasEnabled)
            {
                surfaceCrawlController.enabled = true;
            }
            else if (!stillStain &&
                     figureMotor != null &&
                     motorWasEnabled)
            {
                figureMotor.enabled = true;
            }
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (value.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
