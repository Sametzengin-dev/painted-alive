using PaintedAlive.Figures;
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

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float yaw;

        [SerializeField]
        private float pitch;

        [SerializeField]
        private bool planningStance;

        [SerializeField]
        private bool boundaryLimited;

        private bool initialized;

        public Camera ControlledCamera => controlledCamera;
        public FigureMotor TrackedFigure => trackedFigure;
        public bool PlanningStance => planningStance;
        public bool BoundaryLimited => boundaryLimited;

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

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ReframeOnFigure();
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
            transform.position = focus -
                figureForward * config.ReframeDistance +
                Vector3.up * config.ReframeHeight;
            transform.rotation = Quaternion.LookRotation(
                focus - transform.position,
                Vector3.up);
            CaptureAngles();
            boundaryLimited = false;
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

            float target = planningStance
                ? config.PlanningFieldOfView
                : config.NormalFieldOfView;
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
