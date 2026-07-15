using UnityEngine;

namespace PaintedAlive.Figures
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class FigureCameraController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private FigureInputReader inputReader;
        [SerializeField] private Transform cameraTarget;

        [Header("Sensitivity")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Min(0f)] private float gamepadSensitivity = 160f;

        [Header("Vertical Limits")]
        [SerializeField] private float minimumPitch = -35f;
        [SerializeField] private float maximumPitch = 70f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;

        private float yaw;
        private float pitch;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<FigureInputReader>();
            }

            if (cameraTarget == null)
            {
                Debug.LogError(
                    $"{nameof(FigureCameraController)} on {name} requires a camera target.",
                    this);

                enabled = false;
                return;
            }

            Vector3 initialAngles = cameraTarget.eulerAngles;
            yaw = initialAngles.y;
            pitch = NormalizeAngle(initialAngles.x);
        }

        private void OnEnable()
        {
            SetCursorState(lockCursor);
        }

        private void OnDisable()
        {
            SetCursorState(false);
        }

        private void Update()
        {
            Vector2 lookInput = inputReader.Look;

            float sensitivity = inputReader.LookUsesPointerDelta
                ? mouseSensitivity
                : gamepadSensitivity * Time.unscaledDeltaTime;

            yaw += lookInput.x * sensitivity;
            pitch -= lookInput.y * sensitivity;
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);

            cameraTarget.rotation = Quaternion.Euler(
                pitch,
                yaw,
                0f);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private static void SetCursorState(bool locked)
        {
            Cursor.lockState = locked
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !locked;
        }
    }
}
