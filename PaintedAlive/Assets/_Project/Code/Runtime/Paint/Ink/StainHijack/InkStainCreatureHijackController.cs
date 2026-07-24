using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.Commands;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using PaintedAlive.Paint.Ink.Possession;
using PaintedAlive.Paint.Ink.StainSabotage;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Ink.StainHijack
{
    [DefaultExecutionOrder(14000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class InkStainCreatureHijackController : MonoBehaviour
    {
        private const int MaximumPhysicsHits = 24;

        private readonly RaycastHit[] targetHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] groundHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] obstacleHits =
            new RaycastHit[MaximumPhysicsHits];
        private readonly RaycastHit[] cameraHits =
            new RaycastHit[MaximumPhysicsHits];

        [Header("References")]
        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private Camera figureCamera;

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkStainSabotageController sabotageController;

        [SerializeField]
        private InkPossessionController painterPossession;

        [SerializeField]
        private InkStainHijackConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private InkCreatureRuntime currentTarget;

        [SerializeField]
        private InkCreatureRuntime hijackedCreature;

        [SerializeField]
        private bool isHijacking;

        [SerializeField]
        private bool entryArmed;

        [SerializeField, Range(0f, 1f)]
        private float entryProgress;

        [SerializeField]
        private float remainingSeconds;

        [SerializeField]
        private string lastResult =
            "Önce küçük bir Mürekkep yaratığını sabote et";

        private Transform cameraOriginalParent;
        private Vector3 cameraOriginalLocalPosition;
        private Quaternion cameraOriginalLocalRotation;
        private Behaviour cinemachineBrain;
        private bool cinemachineBrainWasEnabled;
        private bool figureMotorWasEnabled;
        private bool sabotageControllerWasEnabled;
        private bool painterPossessionWasEnabled;
        private float hijackEndsAt;
        private float earliestManualExitTime;
        private float yaw;
        private float pitch = 14f;
        private Vector3 cameraVelocity;
        private InkStainSabotageStatus activeSabotageStatus;
        private Renderer[] figureRenderers;
        private bool[] figureRendererStates;

        public InkCreatureRuntime CurrentTarget => currentTarget;
        public InkCreatureRuntime HijackedCreature => hijackedCreature;
        public bool IsHijacking => isHijacking;
        public bool EntryArmed => entryArmed;
        public float EntryProgress => entryProgress;
        public float RemainingSeconds => remainingSeconds;
        public float MaximumHijackDuration =>
            config != null ? config.MaximumHijackDuration : 1f;
        public string LastResult => lastResult;
        public bool IsStainRoleActive =>
            roleAuthority != null &&
            !roleAuthority.IsInkPainter &&
            clarityState != null &&
            clarityState.CurrentLevel == FigureClarityLevel.Stain;

        private void Awake()
        {
            ResolveReferences();

            if (clarityState == null ||
                figureMotor == null ||
                figureCamera == null ||
                roleAuthority == null ||
                sabotageController == null ||
                config == null)
            {
                Debug.LogError(
                    "M26 Stain hijack references are incomplete. " +
                    "Run M26 Setup again.",
                    this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (isHijacking)
            {
                ExitHijack("M26 controller disabled");
            }
        }

        private void OnDestroy()
        {
            if (isHijacking)
            {
                ExitHijack("M26 controller destroyed");
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (isHijacking)
            {
                UpdateActiveHijack(keyboard);
                return;
            }

            if (!IsStainRoleActive || IsEditingText())
            {
                ResetEntry(
                    "Ele geçirme yalnız Figürün tam Leke formunda çalışır");
                return;
            }

            InkCreatureRuntime candidate = FindBestHijackTarget();

            if (candidate != currentTarget)
            {
                currentTarget = candidate;
                entryProgress = 0f;
                entryArmed = false;
            }

            if (currentTarget == null)
            {
                entryProgress = 0f;
                entryArmed = false;
                lastResult =
                    "Önce küçük yaratığı E ile sabote et";
                return;
            }

            if (keyboard == null || !keyboard.eKey.isPressed)
            {
                entryProgress = 0f;
                entryArmed = true;
                lastResult =
                    $"E basılı tut: {GetTargetLabel(currentTarget)} içine gir";
                return;
            }

            if (!entryArmed)
            {
                entryProgress = 0f;
                lastResult =
                    "Sabotajdan sonra E tuşunu bir kez bırak";
                return;
            }

            entryProgress = Mathf.Clamp01(
                entryProgress +
                Time.unscaledDeltaTime /
                Mathf.Max(0.1f, config.EntryHoldDuration));
            lastResult =
                $"{GetTargetLabel(currentTarget)} ele geçiriliyor " +
                $"{entryProgress * 100f:0}%";

            if (entryProgress >= 1f)
            {
                TryEnterHijack(currentTarget);
            }
        }

        private void LateUpdate()
        {
            if (!isHijacking ||
                hijackedCreature == null ||
                figureCamera == null)
            {
                return;
            }

            UpdateHijackCamera(Time.unscaledDeltaTime);
        }

        public void Configure(
            FigureClarityState targetClarityState,
            FigureMotor targetFigure,
            Camera targetCamera,
            InkPainterRoleAuthority authority,
            InkStainSabotageController targetSabotageController,
            InkPossessionController targetPainterPossession,
            InkStainHijackConfig hijackConfig)
        {
            clarityState = targetClarityState;
            figureMotor = targetFigure;
            figureCamera = targetCamera;
            roleAuthority = authority;
            sabotageController = targetSabotageController;
            painterPossession = targetPainterPossession;
            config = hijackConfig;
        }

        public bool TryEnterHijack(InkCreatureRuntime target)
        {
            if (!Application.isPlaying ||
                isHijacking ||
                !IsStainRoleActive ||
                !IsValidHijackTarget(target))
            {
                lastResult = "Yaratık artık ele geçirilemez";
                return false;
            }

            InkStainSabotageStatus sabotage =
                target.GetComponent<InkStainSabotageStatus>();

            if (sabotage == null || !sabotage.IsSabotaged)
            {
                lastResult = "Yaratık önce sabote edilmelidir";
                return false;
            }

            ForcePainterPossessionExit(target);
            InkCreatureCommandAgent command =
                target.GetComponent<InkCreatureCommandAgent>();

            if (command != null && command.IsCommanded)
            {
                command.CancelCommand(
                    "Stain Figure hijacked creature");
            }

            activeSabotageStatus = sabotage;
            activeSabotageStatus.Apply(transform);
            hijackedCreature = target;
            currentTarget = null;
            isHijacking = true;
            entryArmed = false;
            entryProgress = 0f;
            remainingSeconds = config.MaximumHijackDuration;
            hijackEndsAt =
                Time.unscaledTime + config.MaximumHijackDuration;
            earliestManualExitTime = Time.unscaledTime + 0.35f;
            yaw = target.transform.eulerAngles.y;
            pitch = Mathf.Clamp(
                pitch,
                config.MinimumPitch,
                config.MaximumPitch);
            cameraVelocity = Vector3.zero;

            figureMotorWasEnabled =
                figureMotor != null && figureMotor.enabled;
            sabotageControllerWasEnabled =
                sabotageController != null &&
                sabotageController.enabled;
            painterPossessionWasEnabled =
                painterPossession != null &&
                painterPossession.enabled;

            if (figureMotor != null)
            {
                figureMotor.enabled = false;
            }

            if (sabotageController != null)
            {
                sabotageController.enabled = false;
            }

            if (painterPossession != null)
            {
                painterPossession.enabled = false;
            }

            CaptureAndHideFigureRenderers();
            CaptureCamera();
            lastResult =
                $"{GetTargetLabel(target)} kontrol altında";

            Debug.Log(
                "[M26] Leke yaratığın içine girdi. WASD: hareket, " +
                "fare: bakış, E: erken çıkış.",
                target);
            return true;
        }

        public void ExitHijack(string reason)
        {
            if (!isHijacking)
            {
                return;
            }

            InkCreatureRuntime released = hijackedCreature;
            isHijacking = false;
            hijackedCreature = null;
            remainingSeconds = 0f;
            entryProgress = 0f;
            entryArmed = false;

            RestoreCamera();
            RestoreFigureRenderers();

            if (figureMotor != null &&
                figureMotorWasEnabled &&
                roleAuthority != null &&
                !roleAuthority.IsInkPainter)
            {
                figureMotor.enabled = true;
            }

            if (sabotageController != null &&
                sabotageControllerWasEnabled)
            {
                sabotageController.enabled = true;
            }

            if (painterPossession != null)
            {
                painterPossession.enabled =
                    roleAuthority != null &&
                    roleAuthority.IsInkPainter
                        ? true
                        : painterPossessionWasEnabled;
            }

            if (activeSabotageStatus != null)
            {
                activeSabotageStatus.EndSabotage(
                    "Stain hijack ended");
            }

            activeSabotageStatus = null;
            lastResult = string.IsNullOrWhiteSpace(reason)
                ? "Ele geçirme sona erdi"
                : reason;

            Debug.Log(
                $"[M26] Leke ele geçirmesi bitti. " +
                $"Creature={(released != null ? released.name : "None")}, " +
                $"Reason={lastResult}",
                this);
        }

        private void UpdateActiveHijack(Keyboard keyboard)
        {
            remainingSeconds =
                Mathf.Max(0f, hijackEndsAt - Time.unscaledTime);

            if (!IsStainRoleActive)
            {
                ExitHijack("Figür rolü veya Leke formu değişti");
                return;
            }

            if (hijackedCreature == null)
            {
                ExitHijack("Yaratık yok edildi");
                return;
            }

            if (!hijackedCreature.IsInitialized ||
                !hijackedCreature.HasGlyph(InkGlyphType.Foot))
            {
                ExitHijack("Yaratığın hareket sembolü kayboldu");
                return;
            }

            if (hijackedCreature.IsFixed ||
                hijackedCreature.IsPinned)
            {
                ExitHijack("Yaratık sabitlendi");
                return;
            }

            if (Time.unscaledTime >= hijackEndsAt)
            {
                ExitHijack("Ele geçirme süresi doldu");
                return;
            }

            if (keyboard != null &&
                keyboard.eKey.wasPressedThisFrame &&
                Time.unscaledTime >= earliestManualExitTime)
            {
                ExitHijack("Oyuncu yaratığı bıraktı");
                return;
            }

            UpdateLookInput();
            SimulateMovement(keyboard, Time.deltaTime);
            lastResult =
                $"WASD hareket • E çıkış • {remainingSeconds:0.0}s";
        }

        private InkCreatureRuntime FindBestHijackTarget()
        {
            if (figureCamera == null || config == null)
            {
                return null;
            }

            return InkStainTargetingUtility.FindBestTarget(
                figureCamera,
                figureMotor != null
                    ? figureMotor.transform
                    : transform,
                config.AimAssistRadius,
                config.InteractionRange,
                config.TargetMask,
                targetHits,
                config.MaximumComplexity,
                InkStainTargetMode.SabotagedForHijack);
        }

        private bool IsValidHijackTarget(InkCreatureRuntime candidate)
        {
            return config != null &&
                InkStainTargetingUtility.IsValidTarget(
                    candidate,
                    config.MaximumComplexity,
                    InkStainTargetMode.SabotagedForHijack);
        }

        private void SimulateMovement(
            Keyboard keyboard,
            float deltaTime)
        {
            if (keyboard == null ||
                hijackedCreature == null ||
                deltaTime <= 0f)
            {
                return;
            }

            Vector2 input = Vector2.zero;
            input.y += keyboard.wKey.isPressed ? 1f : 0f;
            input.y -= keyboard.sKey.isPressed ? 1f : 0f;
            input.x += keyboard.dKey.isPressed ? 1f : 0f;
            input.x -= keyboard.aKey.isPressed ? 1f : 0f;
            input = Vector2.ClampMagnitude(input, 1f);

            if (input.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 direction =
                yawRotation *
                new Vector3(input.x, 0f, input.y);
            direction = AvoidObstacles(direction.normalized);
            float speed =
                GetFootMovementSpeed(hijackedCreature) *
                config.MovementSpeedMultiplier;
            Vector3 requested =
                hijackedCreature.transform.position +
                direction * speed * deltaTime;

            if (!TryFindGround(
                    requested,
                    out Vector3 groundedPosition,
                    out Vector3 groundNormal))
            {
                return;
            }

            Vector3 surfaceDirection =
                Vector3.ProjectOnPlane(
                    direction,
                    groundNormal).normalized;

            if (surfaceDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(
                surfaceDirection,
                groundNormal);
            hijackedCreature.transform.rotation =
                Quaternion.RotateTowards(
                    hijackedCreature.transform.rotation,
                    targetRotation,
                    config.TurnSpeedDegrees * deltaTime);
            hijackedCreature.transform.position =
                groundedPosition +
                groundNormal * config.SurfaceOffset;
        }

        private Vector3 AvoidObstacles(Vector3 direction)
        {
            Vector3 origin =
                hijackedCreature.WorldBounds.center;
            int count = Physics.SphereCastNonAlloc(
                origin,
                config.ObstacleProbeRadius,
                direction,
                obstacleHits,
                config.ObstacleProbeDistance,
                config.NavigationMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = obstacleHits[i].collider;

                if (hitCollider == null ||
                    IsHijackedCreatureCollider(hitCollider))
                {
                    continue;
                }

                Vector3 tangent = Vector3.Cross(
                    obstacleHits[i].normal,
                    Vector3.up).normalized;
                float side =
                    Vector3.Dot(tangent, direction) >= 0f
                        ? 1f
                        : -1f;
                return Vector3.Slerp(
                    direction,
                    tangent * side,
                    0.82f).normalized;
            }

            return direction;
        }

        private bool TryFindGround(
            Vector3 requested,
            out Vector3 position,
            out Vector3 normal)
        {
            Vector3 origin =
                requested +
                Vector3.up * config.GroundProbeHeight;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                config.GroundProbeHeight +
                config.GroundProbeDistance,
                config.NavigationMask,
                QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            RaycastHit best = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null ||
                    IsHijackedCreatureCollider(hit.collider) ||
                    hit.distance >= nearest ||
                    Vector3.Angle(hit.normal, Vector3.up) >
                    config.MaximumWalkableSlope)
                {
                    continue;
                }

                nearest = hit.distance;
                best = hit;
            }

            if (best.collider == null)
            {
                position = hijackedCreature.transform.position;
                normal = Vector3.up;
                return false;
            }

            position = best.point;
            normal = best.normal.normalized;
            return true;
        }

        private void UpdateLookInput()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * config.MouseSensitivity;
            pitch = Mathf.Clamp(
                pitch - delta.y * config.MouseSensitivity,
                config.MinimumPitch,
                config.MaximumPitch);
        }

        private void UpdateHijackCamera(float deltaTime)
        {
            Vector3 focus =
                hijackedCreature.WorldBounds.center +
                Vector3.up * config.CameraHeight;
            Quaternion rotation =
                Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired =
                focus -
                rotation * Vector3.forward *
                config.CameraDistance;
            Vector3 direction = desired - focus;
            float distance = direction.magnitude;

            if (distance > 0.01f)
            {
                int count = Physics.SphereCastNonAlloc(
                    focus,
                    config.CameraCollisionRadius,
                    direction / distance,
                    cameraHits,
                    distance,
                    config.NavigationMask,
                    QueryTriggerInteraction.Ignore);
                float closest = distance;

                for (int i = 0; i < count; i++)
                {
                    Collider hitCollider =
                        cameraHits[i].collider;

                    if (hitCollider == null ||
                        IsHijackedCreatureCollider(hitCollider))
                    {
                        continue;
                    }

                    closest = Mathf.Min(
                        closest,
                        Mathf.Max(
                            0.12f,
                            cameraHits[i].distance -
                            config.CameraCollisionRadius));
                }

                desired =
                    focus +
                    direction.normalized * closest;
            }

            float smoothTime =
                1f / Mathf.Max(
                    1f,
                    config.CameraFollowSharpness);
            figureCamera.transform.position =
                Vector3.SmoothDamp(
                    figureCamera.transform.position,
                    desired,
                    ref cameraVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    deltaTime);
            figureCamera.transform.rotation =
                Quaternion.LookRotation(
                    focus -
                    figureCamera.transform.position,
                    Vector3.up);
        }

        private void CaptureCamera()
        {
            cameraOriginalParent =
                figureCamera.transform.parent;
            cameraOriginalLocalPosition =
                figureCamera.transform.localPosition;
            cameraOriginalLocalRotation =
                figureCamera.transform.localRotation;
            figureCamera.transform.SetParent(null, true);
            cinemachineBrain =
                figureCamera.GetComponent("CinemachineBrain")
                as Behaviour;
            cinemachineBrainWasEnabled =
                cinemachineBrain != null &&
                cinemachineBrain.enabled;

            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = false;
            }

            UpdateHijackCamera(0.016f);
        }

        private void RestoreCamera()
        {
            if (figureCamera == null)
            {
                return;
            }

            figureCamera.transform.SetParent(
                cameraOriginalParent,
                false);
            figureCamera.transform.localPosition =
                cameraOriginalLocalPosition;
            figureCamera.transform.localRotation =
                cameraOriginalLocalRotation;
            cameraVelocity = Vector3.zero;

            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled =
                    cinemachineBrainWasEnabled;
            }

            cinemachineBrain = null;
        }

        private void CaptureAndHideFigureRenderers()
        {
            figureRenderers =
                figureMotor != null
                    ? figureMotor.GetComponentsInChildren<
                        Renderer>(true)
                    : System.Array.Empty<Renderer>();
            figureRendererStates =
                new bool[figureRenderers.Length];

            for (int i = 0; i < figureRenderers.Length; i++)
            {
                Renderer target = figureRenderers[i];

                if (target == null)
                {
                    continue;
                }

                figureRendererStates[i] = target.enabled;
                target.enabled = false;
            }
        }

        private void RestoreFigureRenderers()
        {
            if (figureRenderers == null ||
                figureRendererStates == null)
            {
                return;
            }

            for (int i = 0; i < figureRenderers.Length; i++)
            {
                Renderer target = figureRenderers[i];

                if (target != null &&
                    i < figureRendererStates.Length)
                {
                    target.enabled =
                        figureRendererStates[i];
                }
            }

            figureRenderers = null;
            figureRendererStates = null;
        }

        private void ForcePainterPossessionExit(
            InkCreatureRuntime target)
        {
            InkPossessionController[] controllers =
                Object.FindObjectsByType<InkPossessionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < controllers.Length; i++)
            {
                InkPossessionController controller =
                    controllers[i];

                if (controller != null &&
                    controller.IsPossessing &&
                    controller.PossessedCreature == target)
                {
                    controller.ExitPossession(
                        "Stain Figure hijacked creature");
                }
            }
        }

        private bool IsHijackedCreatureCollider(Collider target)
        {
            return target != null &&
                hijackedCreature != null &&
                target.transform.IsChildOf(
                    hijackedCreature.transform);
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
                    speed +=
                        module.Definition.MovementSpeed;
                }
            }

            return speed;
        }

        private void ResolveReferences()
        {
            if (roleAuthority == null)
            {
                roleAuthority =
                    InkPainterRoleAuthority.ActiveInstance;
            }

            if (clarityState == null)
            {
                clarityState =
                    GetComponent<FigureClarityState>();
            }

            if (figureMotor == null)
            {
                figureMotor = GetComponent<FigureMotor>();
            }

            if (figureCamera == null &&
                roleAuthority != null)
            {
                figureCamera =
                    roleAuthority.ActiveRoleCamera;
            }

            if (sabotageController == null)
            {
                sabotageController =
                    GetComponent<InkStainSabotageController>();
            }

            if (painterPossession == null)
            {
                painterPossession =
                    GetComponent<InkPossessionController>();
            }
        }

        private void ResetEntry(string reason)
        {
            currentTarget = null;
            entryArmed = false;
            entryProgress = 0f;
            lastResult = reason;
        }

        private static string GetTargetLabel(
            InkCreatureRuntime target)
        {
            if (target == null)
            {
                return "Mürekkep yaratığı";
            }

            if (target.HasGlyph(InkGlyphType.BrokenLine))
            {
                return "Kesik Avcı";
            }

            return target.Definition != null &&
                !string.IsNullOrWhiteSpace(
                    target.Definition.DisplayName)
                ? target.Definition.DisplayName
                : "Lekebacak";
        }

        private static bool IsEditingText()
        {
            GameObject selected =
                EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject
                    : null;
            return selected != null &&
                (selected.GetComponent("TMP_InputField") != null ||
                 selected.GetComponent("InputField") != null);
        }
    }
}
