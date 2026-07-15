using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures
{
    [DisallowMultipleComponent]
    public sealed class FigureInputReader : MonoBehaviour
    {
        [Header("Figure Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference sprintAction;

        public Vector2 Move
        {
            get
            {
                if (moveAction == null || moveAction.action == null)
                {
                    return Vector2.zero;
                }

                return Vector2.ClampMagnitude(
                    moveAction.action.ReadValue<Vector2>(),
                    1f);
            }
        }

        public Vector2 Look
        {
            get
            {
                if (lookAction == null || lookAction.action == null)
                {
                    return Vector2.zero;
                }

                return lookAction.action.ReadValue<Vector2>();
            }
        }

        public bool JumpPressedThisFrame =>
            jumpAction != null &&
            jumpAction.action != null &&
            jumpAction.action.WasPressedThisFrame();

        public bool SprintHeld =>
            sprintAction != null &&
            sprintAction.action != null &&
            sprintAction.action.IsPressed();

        public bool LookUsesPointerDelta =>
            lookAction != null &&
            lookAction.action != null &&
            lookAction.action.activeControl?.device is Pointer;

        private void OnEnable()
        {
            SetActionEnabled(moveAction, true);
            SetActionEnabled(lookAction, true);
            SetActionEnabled(jumpAction, true);
            SetActionEnabled(sprintAction, true);
        }

        private void OnDisable()
        {
            SetActionEnabled(moveAction, false);
            SetActionEnabled(lookAction, false);
            SetActionEnabled(jumpAction, false);
            SetActionEnabled(sprintAction, false);
        }

        private static void SetActionEnabled(
            InputActionReference actionReference,
            bool enabled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            if (enabled)
            {
                actionReference.action.Enable();
            }
            else
            {
                actionReference.action.Disable();
            }
        }
    }
}
