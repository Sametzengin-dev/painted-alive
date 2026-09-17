using PaintedAlive.Figures;
using PaintedAlive.Networking.M56;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Painters.Ink
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class InkPainterIndependentCamera : MonoBehaviour
    {
        [SerializeField]
        private Camera controlledCamera;

        [SerializeField]
        private FigureMotor trackedFigure;

        [SerializeField]
        private InkPainterRoleCameraConfig config;

        [Header("Runtime View Tuning")]
        [SerializeField, Min(0.1f)]
        private float preciseHeightStep = 1f;

        [SerializeField, Min(0.5f)]
        private float fieldOfViewStep = 2f;

        [SerializeField, Range(25f, 120f)]
        private float minimumRuntimeFieldOfView = 35f;

        [SerializeField, Range(25f, 120f)]
        private float maximumRuntimeFieldOfView = 100f;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float yaw;

        [SerializeField]
        private float pitch;

        [SerializeField]
        private bool planningStance;

        [SerializeField]
        private bool boundaryLimited;

        [SerializeField]
        private float runtimeReframeHeightBias;

        [SerializeField]
        private float runtimeFieldOfViewOffset;

        private bool initialized;

        public Camera ControlledCamera => controlledCamera;
        public FigureMotor TrackedFigure => trackedFigure;
        public InkPainterRoleCameraConfig Config => config;
        public bool PlanningStance => planningStance;
        public bool BoundaryLimited => boundaryLimited;
        public float RuntimeReframeHeightBias => runtimeReframeHeightBias;
        public float RuntimeFieldOfViewOffset => runtimeFieldOfViewOffset;
        public float CurrentHeightFromFigure =>
            trackedFigure != null
                ? transform.position.y - trackedFigure.transform.position.y
                : 0f;

        private void Awake()
        {
            controlledCamera ??= GetComponent<Camera>();

            if (controlledCamera == null ||
                trackedFigure == null ||
                config == null)
            {
                Debug.LogError(
                    "InkPainterIndependentCamera requires Camera, " +
                    "FigureMotor and config.",
                    this);
                enabled = false;
                return;
            }

            CaptureAngles();
        }

        private void OnEnable()
        {
            if (controlledCamera == null ||
                trackedFigure == null ||
                config == null)
            {
                return;
            }

            if (!initialized)
            {
                ReframeOnFigure();
                initialized = true;
            }
            else
            {
                CaptureAngles();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (PaintedAliveNetworkRoleBridge.GameplayInputSuppressed)
                return;

            if (trackedFigure == null || config == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard == null || IsEditingText())
            {
                return;
            }

            if (keyboard.homeKey.wasPressedThisFrame)
            {
                ResetRuntimeViewTuning();
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                ReframeOnFigure();
            }

            if (keyboard.pageUpKey.wasPressedThisFrame)
            {
                NudgePreferredHeight(preciseHeightStep);
            }
            else if (keyboard.pageDownKey.wasPressedThisFrame)
            {
                NudgePreferredHeight(-preciseHeightStep);
            }

            if (keyboard.leftBracketKey.wasPressedThisFrame)
            {
                runtimeFieldOfViewOffset -= fieldOfViewStep;
                ClampRuntimeFieldOfViewOffset();
            }
            else if (keyboard.rightBracketKey.wasPressedThisFrame)
            {
                runtimeFieldOfViewOffset += fieldOfViewStep;
                ClampRuntimeFieldOfViewOffset();
            }

            planningStance = keyboard.leftAltKey.isPressed ||
                keyboard.rightAltKey.isPressed;
            UpdateLook(mouse);
            UpdateMovement(keyboard, Time.unscaledDeltaTime);
            UpdateFieldOfView(Time.unscaledDeltaTime);
        }

        public void Configure(
            Camera targetCamera,
            FigureMotor figure,
            InkPainterRoleCameraConfig cameraConfig)
        {
            controlledCamera = targetCamera;
            trackedFigure = figure;
            config = cameraConfig;
            CaptureAngles();
        }

        public void PrepareForPainterRoleActivation()
        {
            if (trackedFigure == null || config == null)
                return;

            ReframeOnFigure();
            initialized = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        [ContextMenu("Debug/Reframe Painter Camera")]
        public void ReframeOnFigure()
        {
            if (trackedFigure == null || config == null)
            {
                return;
            }

            Vector3 figureForward = Vector3.ProjectOnPlane(
                trackedFigure.transform.forward,
                Vector3.up).normalized;

            if (figureForward.sqrMagnitude < 0.001f)
            {
                figureForward = Vector3.forward;
            }

            Vector3 focus = trackedFigure.transform.position +
                Vector3.up * 1.4f;

            float preferredHeight = Mathf.Clamp(
                config.ReframeHeight + runtimeReframeHeightBias,
                config.MinimumHeightFromFigure,
                config.MaximumHeightFromFigure);

            Vector3 candidate = focus -
                figureForward * config.ReframeDistance +
                Vector3.up * preferredHeight;

            transform.position = ConstrainToWorkVolume(
                candidate,
                out boundaryLimited);
            transform.rotation = Quaternion.LookRotation(
                focus - transform.position,
                Vector3.up);
            CaptureAngles();
            boundaryLimited = false;
        }

        public void ResetRuntimeViewTuning()
        {
            runtimeReframeHeightBias = 0f;
            runtimeFieldOfViewOffset = 0f;
            ReframeOnFigure();
        }

        private void NudgePreferredHeight(float delta)
        {
            if (trackedFigure == null || config == null)
                return;

            float currentHeight = CurrentHeightFromFigure;
            float desiredHeight = Mathf.Clamp(
                currentHeight + delta,
                config.MinimumHeightFromFigure,
                config.MaximumHeightFromFigure);

            runtimeReframeHeightBias =
                desiredHeight - config.ReframeHeight;

            Vector3 candidate = transform.position;
            candidate.y =
                trackedFigure.transform.position.y + desiredHeight;

            transform.position = ConstrainToWorkVolume(
                candidate,
                out boundaryLimited);
        }

        private void ClampRuntimeFieldOfViewOffset()
        {
            if (config == null)
                return;

            float baseFov = planningStance
                ? config.PlanningFieldOfView
                : config.NormalFieldOfView;

            float target = Mathf.Clamp(
                baseFov + runtimeFieldOfViewOffset,
                minimumRuntimeFieldOfView,
                maximumRuntimeFieldOfView);

            runtimeFieldOfViewOffset = target - baseFov;
        }

        private void UpdateLook(Mouse mouse)
        {
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
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateMovement(
            Keyboard keyboard,
            float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 input = Vector3.zero;
            input.z += keyboard.wKey.isPressed ? 1f : 0f;
            input.z -= keyboard.sKey.isPressed ? 1f : 0f;
            input.x += keyboard.dKey.isPressed ? 1f : 0f;
            input.x -= keyboard.aKey.isPressed ? 1f : 0f;
            input.y += keyboard.eKey.isPressed ? 1f : 0f;
            input.y -= keyboard.qKey.isPressed ? 1f : 0f;
            input = Vector3.ClampMagnitude(input, 1f);

            if (input.sqrMagnitude < 0.001f)
            {
                boundaryLimited = false;
                return;
            }

            Vector3 planarForward = Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up).normalized;

            if (planarForward.sqrMagnitude < 0.001f)
            {
                planarForward = Vector3.forward;
            }

            Vector3 planarRight = Vector3.Cross(
                Vector3.up,
                planarForward).normalized;
            Vector3 direction =
                planarRight * input.x +
                planarForward * input.z +
                Vector3.up * input.y;
            float boost = keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed
                    ? config.BoostMultiplier
                    : 1f;
            Vector3 candidate = transform.position +
                direction.normalized *
                config.MovementSpeed *
                boost *
                deltaTime;
            transform.position = ConstrainToWorkVolume(
                candidate,
                out boundaryLimited);
        }

        private Vector3 ConstrainToWorkVolume(
            Vector3 candidate,
            out bool wasLimited)
        {
            Vector3 center = trackedFigure.transform.position;
            Vector3 offset = candidate - center;
            Vector2 horizontal = new Vector2(offset.x, offset.z);
            wasLimited = false;

            if (horizontal.magnitude > config.MaximumWorkRadius)
            {
                horizontal = horizontal.normalized *
                    config.MaximumWorkRadius;
                candidate.x = center.x + horizontal.x;
                candidate.z = center.z + horizontal.y;
                wasLimited = true;
            }

            float minimumY = center.y + config.MinimumHeightFromFigure;
            float maximumY = center.y + config.MaximumHeightFromFigure;
            float clampedY = Mathf.Clamp(candidate.y, minimumY, maximumY);

            if (!Mathf.Approximately(candidate.y, clampedY))
            {
                candidate.y = clampedY;
                wasLimited = true;
            }

            return candidate;
        }

        private void UpdateFieldOfView(float deltaTime)
        {
            if (controlledCamera == null)
            {
                return;
            }

            float baseTarget = planningStance
                ? config.PlanningFieldOfView
                : config.NormalFieldOfView;

            float target = Mathf.Clamp(
                baseTarget + runtimeFieldOfViewOffset,
                minimumRuntimeFieldOfView,
                maximumRuntimeFieldOfView);
            float interpolation = 1f - Mathf.Exp(
                -config.FieldOfViewSharpness * deltaTime);
            controlledCamera.fieldOfView = Mathf.Lerp(
                controlledCamera.fieldOfView,
                target,
                interpolation);
        }

        private void CaptureAngles()
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x > 180f
                ? angles.x - 360f
                : angles.x;
            pitch = config != null
                ? Mathf.Clamp(
                    pitch,
                    config.MinimumPitch,
                    config.MaximumPitch)
                : pitch;
        }

        private static bool IsEditingText()
        {
            GameObject selected =
                EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject
                    : null;
            return selected != null &&
                selected.GetComponent<UnityEngine.UI.InputField>() != null;
        }
    }
}
