using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Ink.Possession
{
    [DefaultExecutionOrder(20000)]
    [DisallowMultipleComponent]
    public sealed class InkPossessionController : MonoBehaviour
    {
        private const int MaximumPhysicsHits = 24;
        private const string FrameAnchorMarkerName =
            "FrameGunAnchor_Runtime";

        private static InkPossessionController activeController;

        private readonly RaycastHit[] selectionHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] obstacleHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] groundHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] cameraHits =
            new RaycastHit[MaximumPhysicsHits];
        private FigureMotor[] contactFigures =
            System.Array.Empty<FigureMotor>();

        [Header("References")]
        [SerializeField]
        private FigureMotor targetFigure;

        [SerializeField]
        private Camera sourceCamera;

        [SerializeField]
        private InkPossessionConfig config;

        [Header("Physics")]
        [SerializeField]
        private LayerMask navigationMask = Physics.DefaultRaycastLayers;

        [SerializeField]
        private LayerMask selectionMask = Physics.DefaultRaycastLayers;

        [SerializeField]
        private LayerMask cameraCollisionMask = Physics.DefaultRaycastLayers;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private InkCreatureRuntime possessedCreature;

        [SerializeField]
        private bool isPossessing;

        [SerializeField]
        private string lastExitReason = "Not started";

        [SerializeField]
        private float currentPossessionDuration;

        [SerializeField]
        private bool movementBlocked;

        private Transform cameraOriginalParent;
        private Vector3 cameraOriginalLocalPosition;
        private Quaternion cameraOriginalLocalRotation;
        private bool figureWasEnabled;
        private bool creatureWasEnabled;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;
        private Behaviour cachedCinemachineBrain;
        private bool cinemachineBrainWasEnabled;
        private float yaw;
        private float pitch = 14f;
        private float nextToggleTime;
        private float nextContactTime;
        private Vector3 cameraVelocity;

        public FigureMotor TargetFigure => targetFigure;
        public Camera SourceCamera => sourceCamera;
        public InkCreatureRuntime PossessedCreature => possessedCreature;
        public bool IsPossessing => isPossessing;
        public string LastExitReason => lastExitReason;
        public float CurrentPossessionDuration => currentPossessionDuration;
        public bool MovementBlocked => movementBlocked;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeController = null;
        }

        private void Awake()
        {
            targetFigure ??= GetComponentInParent<FigureMotor>();
            sourceCamera ??= targetFigure != null
                ? targetFigure.GetComponentInChildren<Camera>(true)
                : null;

            if (activeController != null && activeController != this)
            {
                Debug.LogError(
                    "Duplicate InkPossessionController disabled. Run M17 " +
                    "Diagnose and keep only one local controller.",
                    this);
                enabled = false;
                return;
            }

            activeController = this;

            if (targetFigure == null || sourceCamera == null || config == null)
            {
                Debug.LogError(
                    "InkPossessionController requires a FigureMotor, Camera " +
                    "and InkPossessionConfig.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (activeController == null || activeController == this)
            {
                activeController = this;
            }
        }

        private void OnDisable()
        {
            if (isPossessing)
            {
                ExitPossession("Controller disabled");
            }

            if (activeController == this)
            {
                activeController = null;
            }
        }

        private void OnDestroy()
        {
            if (isPossessing)
            {
                ExitPossession("Controller destroyed");
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null &&
                keyboard.f6Key.wasPressedThisFrame &&
                Time.unscaledTime >= nextToggleTime &&
                !IsEditingText())
            {
                nextToggleTime =
                    Time.unscaledTime + config.ToggleCooldown;

                if (isPossessing)
                {
                    ExitPossession("Released by player");
                }
                else
                {
                    TryEnterPossession();
                }
            }

            if (!isPossessing)
            {
                return;
            }

            currentPossessionDuration += Time.unscaledDeltaTime;

            if (possessedCreature == null)
            {
                ExitPossession("Creature was destroyed");
                return;
            }

            if (config.ForceExitWhenEyeIsLost &&
                !possessedCreature.HasGlyph(InkGlyphType.Eye))
            {
                ExitPossession("Eye glyph was disabled");
                return;
            }

            UpdateLookInput();
            SimulateDirectMovement(keyboard, Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (!isPossessing ||
                possessedCreature == null ||
                sourceCamera == null)
            {
                return;
            }

            UpdatePossessionCamera(Time.unscaledDeltaTime);
        }

        [ContextMenu("Debug/Toggle Ink Possession")]
        public void TogglePossession()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (isPossessing)
            {
                ExitPossession("Released by context command");
            }
            else
            {
                TryEnterPossession();
            }
        }

        public bool TryEnterPossession()
        {
            if (!Application.isPlaying ||
                isPossessing ||
                sourceCamera == null ||
                targetFigure == null)
            {
                return false;
            }

            InkCreatureRuntime target = FindBestCreature();

            if (target == null)
            {
                Debug.LogWarning(
                    "M17 possession found no valid Lekebacak. Spawn one with " +
                    "F9, aim at it and press F6.",
                    this);
                return false;
            }

            possessedCreature = target;
            isPossessing = true;
            currentPossessionDuration = 0f;
            movementBlocked = false;
            lastExitReason = "Active";
            nextContactTime = 0f;
            cameraVelocity = Vector3.zero;
            contactFigures =
                Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            figureWasEnabled = targetFigure.enabled;
            creatureWasEnabled = possessedCreature.enabled;
            targetFigure.enabled = false;
            possessedCreature.enabled = false;

            cameraOriginalParent = sourceCamera.transform.parent;
            cameraOriginalLocalPosition =
                sourceCamera.transform.localPosition;
            cameraOriginalLocalRotation =
                sourceCamera.transform.localRotation;
            sourceCamera.transform.SetParent(null, true);

            yaw = possessedCreature.transform.eulerAngles.y;
            pitch = Mathf.Clamp(
                pitch,
                config.MinimumPitch,
                config.MaximumPitch);

            CacheAndPauseCinemachineBrain();
            StoreAndApplyCursorState();
            SnapCameraToCreature();

            Debug.Log(
                "[M17] Lekebacak possessed. WASD: move, Mouse: look, " +
                "F6: release.",
                possessedCreature);
            return true;
        }

        public void ExitPossession(string reason)
        {
            if (!isPossessing)
            {
                return;
            }

            InkCreatureRuntime releasedCreature = possessedCreature;
            isPossessing = false;
            possessedCreature = null;
            movementBlocked = false;
            lastExitReason = string.IsNullOrWhiteSpace(reason)
                ? "Released"
                : reason;

            if (releasedCreature != null)
            {
                releasedCreature.enabled = creatureWasEnabled;
            }

            if (targetFigure != null)
            {
                targetFigure.enabled = figureWasEnabled;
            }

            RestoreCamera();
            RestoreCinemachineBrain();
            RestoreCursorState();

            Debug.Log(
                $"[M17] Ink possession ended. Reason={lastExitReason}",
                this);
        }

        private InkCreatureRuntime FindBestCreature()
        {
            InkSystemManager manager = InkSystemManager.ActiveInstance;

            if (manager == null || manager.ActiveCreatures == null)
            {
                return null;
            }

            Transform cameraTransform = sourceCamera.transform;
            Vector3 origin = cameraTransform.position;
            Vector3 forward = cameraTransform.forward;
            float range = config.SelectionRange;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                0.22f,
                forward,
                selectionHits,
                range,
                selectionMask,
                QueryTriggerInteraction.Collide);
            InkCreatureRuntime rayTarget = null;
            float rayTargetDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = selectionHits[i].collider;
                InkCreatureRuntime candidate = hitCollider != null
                    ? hitCollider.GetComponentInParent<InkCreatureRuntime>()
                    : null;

                if (!IsValidPossessionTarget(candidate) ||
                    selectionHits[i].distance >= rayTargetDistance)
                {
                    continue;
                }

                rayTarget = candidate;
                rayTargetDistance = selectionHits[i].distance;
            }

            if (rayTarget != null)
            {
                return rayTarget;
            }

            InkCreatureRuntime best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < manager.ActiveCreatures.Count; i++)
            {
                InkCreatureRuntime candidate = manager.ActiveCreatures[i];

                if (!IsValidPossessionTarget(candidate))
                {
                    continue;
                }

                Vector3 offset =
                    candidate.WorldBounds.center - origin;
                float distance = offset.magnitude;

                if (distance <= 0.01f || distance > range)
                {
                    continue;
                }

                float aimDot = Vector3.Dot(
                    forward,
                    offset / distance);

                if (aimDot < config.MinimumAimDot)
                {
                    continue;
                }

                float score = aimDot * 3f - distance / range;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static bool IsValidPossessionTarget(
            InkCreatureRuntime candidate)
        {
            return candidate != null &&
                   candidate.isActiveAndEnabled &&
                   candidate.IsInitialized &&
                   candidate.HasGlyph(InkGlyphType.Eye);
        }

        private void UpdateLookInput()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                return;
            }

            Vector2 lookDelta = mouse.delta.ReadValue();
            yaw += lookDelta.x * config.MouseSensitivity;
            pitch = Mathf.Clamp(
                pitch - lookDelta.y * config.MouseSensitivity,
                config.MinimumPitch,
                config.MaximumPitch);
        }

        private void SimulateDirectMovement(
            Keyboard keyboard,
            float deltaTime)
        {
            if (possessedCreature == null || deltaTime <= 0f)
            {
                return;
            }

            bool pinned = ContainsFrameAnchorMarker(
                possessedCreature.transform);
            bool hasFoot = possessedCreature.HasGlyph(InkGlyphType.Foot);
            movementBlocked = possessedCreature.IsFixed || pinned || !hasFoot;

            if (movementBlocked || keyboard == null)
            {
                return;
            }

            Vector2 moveInput = Vector2.zero;
            moveInput.y += keyboard.wKey.isPressed ? 1f : 0f;
            moveInput.y -= keyboard.sKey.isPressed ? 1f : 0f;
            moveInput.x += keyboard.dKey.isPressed ? 1f : 0f;
            moveInput.x -= keyboard.aKey.isPressed ? 1f : 0f;
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            if (moveInput.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 desiredDirection =
                yawRotation * new Vector3(moveInput.x, 0f, moveInput.y);
            desiredDirection = ApplyWatercolorInstability(
                desiredDirection.normalized);
            desiredDirection = AvoidObstacles(desiredDirection);

            InkSystemConfig inkConfig =
                InkSystemManager.ActiveInstance != null
                    ? InkSystemManager.ActiveInstance.Config
                    : null;

            if (inkConfig == null)
            {
                return;
            }

            float footSpeed = GetFootMovementSpeed(possessedCreature);
            float waterSpeed = Mathf.Lerp(
                1f,
                inkConfig.MaximumWaterSpeedMultiplier,
                possessedCreature.WaterExposure);
            float speed = footSpeed *
                config.MovementSpeedMultiplier *
                waterSpeed;
            Vector3 predictedPosition =
                possessedCreature.transform.position +
                desiredDirection * speed * deltaTime;

            if (!TryPlaceOnGround(
                    predictedPosition,
                    inkConfig,
                    out Vector3 groundedPosition,
                    out Vector3 groundNormal))
            {
                movementBlocked = true;
                return;
            }

            Vector3 surfaceDirection = Vector3.ProjectOnPlane(
                desiredDirection,
                groundNormal).normalized;

            if (surfaceDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(
                surfaceDirection,
                groundNormal);
            possessedCreature.transform.rotation =
                Quaternion.RotateTowards(
                    possessedCreature.transform.rotation,
                    desiredRotation,
                    config.TurnSpeedDegrees * deltaTime);
            possessedCreature.transform.position = groundedPosition;
            TryApplyFigureContact(inkConfig);
        }

        private Vector3 ApplyWatercolorInstability(Vector3 direction)
        {
            float exposure = possessedCreature != null
                ? possessedCreature.WaterExposure
                : 0f;

            if (exposure <= 0.001f)
            {
                return direction;
            }

            InkSystemConfig inkConfig =
                InkSystemManager.ActiveInstance != null
                    ? InkSystemManager.ActiveInstance.Config
                    : null;

            if (inkConfig == null)
            {
                return direction;
            }

            float phase = Time.time * 4.3f +
                possessedCreature.GetInstanceID() * 0.031f;
            float wobble = Mathf.Sin(phase) *
                inkConfig.MaximumWaterWobbleDegrees *
                exposure;
            return (Quaternion.AngleAxis(wobble, Vector3.up) * direction)
                .normalized;
        }

        private Vector3 AvoidObstacles(Vector3 direction)
        {
            InkSystemConfig inkConfig =
                InkSystemManager.ActiveInstance != null
                    ? InkSystemManager.ActiveInstance.Config
                    : null;

            if (inkConfig == null)
            {
                return direction;
            }

            Vector3 origin =
                possessedCreature.transform.position + Vector3.up * 0.35f;
            int count = Physics.SphereCastNonAlloc(
                origin,
                inkConfig.ObstacleProbeRadius,
                direction,
                obstacleHits,
                inkConfig.ObstacleProbeDistance,
                navigationMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = obstacleHits[i].collider;

                if (hitCollider == null ||
                    IsPossessedCreatureCollider(hitCollider) ||
                    hitCollider.GetComponentInParent<FigureMotor>() != null)
                {
                    continue;
                }

                Vector3 tangent = Vector3.Cross(
                    obstacleHits[i].normal,
                    Vector3.up).normalized;
                float side = Vector3.Dot(tangent, direction) >= 0f
                    ? 1f
                    : -1f;
                return Vector3.Slerp(
                    direction,
                    tangent * side,
                    0.82f).normalized;
            }

            return direction;
        }

        private bool TryPlaceOnGround(
            Vector3 predictedPosition,
            InkSystemConfig inkConfig,
            out Vector3 groundedPosition,
            out Vector3 groundNormal)
        {
            Vector3 origin = predictedPosition +
                Vector3.up * inkConfig.GroundProbeHeight;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                inkConfig.GroundProbeHeight +
                inkConfig.GroundProbeDistance,
                navigationMask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            RaycastHit bestHit = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null ||
                    IsPossessedCreatureCollider(hit.collider) ||
                    hit.distance >= nearestDistance ||
                    Vector3.Angle(hit.normal, Vector3.up) >
                    inkConfig.MaximumWalkableSlope)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            if (bestHit.collider == null)
            {
                groundedPosition = possessedCreature.transform.position;
                groundNormal = Vector3.up;
                return false;
            }

            groundNormal = bestHit.normal.normalized;
            groundedPosition = bestHit.point +
                groundNormal * inkConfig.SurfaceOffset;
            return true;
        }

        private void TryApplyFigureContact(InkSystemConfig inkConfig)
        {
            if (Time.time < nextContactTime)
            {
                return;
            }

            if (contactFigures == null)
            {
                return;
            }

            float contactDistanceSquared =
                inkConfig.ContactDistance * inkConfig.ContactDistance;

            for (int i = 0; i < contactFigures.Length; i++)
            {
                FigureMotor figure = contactFigures[i];

                if (figure == null ||
                    figure == targetFigure ||
                    !figure.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 delta = figure.transform.position -
                    possessedCreature.transform.position;
                delta.y = 0f;

                if (delta.sqrMagnitude > contactDistanceSquared)
                {
                    continue;
                }

                FigureClarityState clarity =
                    figure.GetComponent<FigureClarityState>();
                clarity?.ApplyPaintExposure(
                    inkConfig.ClarityExposurePerContact,
                    FigurePaintRegion.Legs);
                nextContactTime = Time.time + inkConfig.ContactCooldown;
                break;
            }
        }

        private static float GetFootMovementSpeed(
            InkCreatureRuntime creature)
        {
            if (creature == null || creature.Modules == null)
            {
                return 0f;
            }

            float speed = 0f;

            for (int i = 0; i < creature.Modules.Count; i++)
            {
                InkGlyphModule module = creature.Modules[i];

                if (module != null &&
                    module.IsEnabled &&
                    module.Type == InkGlyphType.Foot &&
                    module.Definition != null)
                {
                    speed += module.Definition.MovementSpeed;
                }
            }

            return speed;
        }

        private void UpdatePossessionCamera(float deltaTime)
        {
            Vector3 focus = possessedCreature.WorldBounds.center +
                Vector3.up * config.CameraHeight;
            Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = focus -
                viewRotation * Vector3.forward * config.CameraDistance;
            Vector3 cameraDirection = desiredPosition - focus;
            float cameraDistance = cameraDirection.magnitude;

            if (cameraDistance > 0.01f)
            {
                int count = Physics.SphereCastNonAlloc(
                    focus,
                    config.CameraCollisionRadius,
                    cameraDirection / cameraDistance,
                    cameraHits,
                    cameraDistance,
                    cameraCollisionMask,
                    QueryTriggerInteraction.Ignore);
                float closestDistance = cameraDistance;

                for (int i = 0; i < count; i++)
                {
                    Collider hitCollider = cameraHits[i].collider;

                    if (hitCollider == null ||
                        IsPossessedCreatureCollider(hitCollider))
                    {
                        continue;
                    }

                    closestDistance = Mathf.Min(
                        closestDistance,
                        Mathf.Max(
                            0.1f,
                            cameraHits[i].distance -
                            config.CameraCollisionRadius));
                }

                desiredPosition = focus +
                    cameraDirection.normalized * closestDistance;
            }

            float smoothTime = 1f / config.CameraFollowSharpness;
            sourceCamera.transform.position = Vector3.SmoothDamp(
                sourceCamera.transform.position,
                desiredPosition,
                ref cameraVelocity,
                smoothTime,
                float.PositiveInfinity,
                Mathf.Max(0.0001f, deltaTime));
            sourceCamera.transform.rotation = Quaternion.LookRotation(
                focus - sourceCamera.transform.position,
                Vector3.up);
        }

        private void SnapCameraToCreature()
        {
            if (possessedCreature == null || sourceCamera == null)
            {
                return;
            }

            Vector3 focus = possessedCreature.WorldBounds.center +
                Vector3.up * config.CameraHeight;
            Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);
            sourceCamera.transform.position = focus -
                viewRotation * Vector3.forward * config.CameraDistance;
            sourceCamera.transform.rotation = Quaternion.LookRotation(
                focus - sourceCamera.transform.position,
                Vector3.up);
        }

        private bool IsPossessedCreatureCollider(Collider targetCollider)
        {
            return targetCollider != null &&
                   possessedCreature != null &&
                   (targetCollider.transform ==
                        possessedCreature.transform ||
                    targetCollider.transform.IsChildOf(
                        possessedCreature.transform));
        }

        private static bool ContainsFrameAnchorMarker(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name.StartsWith(
                        FrameAnchorMarkerName,
                        System.StringComparison.Ordinal) ||
                    ContainsFrameAnchorMarker(child))
                {
                    return true;
                }
            }

            return false;
        }

        private void CacheAndPauseCinemachineBrain()
        {
            cachedCinemachineBrain = null;

            if (sourceCamera == null)
            {
                return;
            }

            MonoBehaviour[] behaviours =
                sourceCamera.GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];

                if (behaviour == null ||
                    !string.Equals(
                        behaviour.GetType().Name,
                        "CinemachineBrain",
                        System.StringComparison.Ordinal))
                {
                    continue;
                }

                cachedCinemachineBrain = behaviour;
                cinemachineBrainWasEnabled = behaviour.enabled;
                behaviour.enabled = false;
                break;
            }
        }

        private void RestoreCinemachineBrain()
        {
            if (cachedCinemachineBrain != null)
            {
                cachedCinemachineBrain.enabled =
                    cinemachineBrainWasEnabled;
            }

            cachedCinemachineBrain = null;
        }

        private void StoreAndApplyCursorState()
        {
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            if (config.LockCursorWhilePossessing)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void RestoreCursorState()
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }

        private void RestoreCamera()
        {
            if (sourceCamera == null)
            {
                return;
            }

            sourceCamera.transform.SetParent(cameraOriginalParent, false);
            sourceCamera.transform.localPosition =
                cameraOriginalLocalPosition;
            sourceCamera.transform.localRotation =
                cameraOriginalLocalRotation;
            cameraVelocity = Vector3.zero;
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
