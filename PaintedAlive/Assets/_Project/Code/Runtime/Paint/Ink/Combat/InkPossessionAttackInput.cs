using PaintedAlive.Paint.Ink.Possession;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Ink.Combat
{
    [DefaultExecutionOrder(19900)]
    [DisallowMultipleComponent]
    public sealed class InkPossessionAttackInput : MonoBehaviour
    {
        [SerializeField]
        private InkPossessionController possessionController;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private string lastInputResult = "Not used";

        public InkPossessionController PossessionController =>
            possessionController;
        public string LastInputResult => lastInputResult;

        private void Awake()
        {
            possessionController ??=
                GetComponent<InkPossessionController>();

            if (possessionController == null)
            {
                Debug.LogError(
                    "InkPossessionAttackInput requires an " +
                    "InkPossessionController.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (possessionController == null ||
                !possessionController.IsPossessing ||
                possessionController.PossessedCreature == null ||
                IsEditingText())
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            bool attackPressed =
                (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                (keyboard != null && keyboard.spaceKey.wasPressedThisFrame);

            if (!attackPressed)
            {
                return;
            }

            InkPounceCombatDirector director =
                InkPounceCombatDirector.ActiveInstance;

            if (director == null)
            {
                lastInputResult = "No M18 combat director";
                return;
            }

            Camera sourceCamera = possessionController.SourceCamera;
            Vector3 direction = sourceCamera != null
                ? sourceCamera.transform.forward
                : possessionController.PossessedCreature.transform.forward;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);

            bool started = director.TryBeginPlayerPounce(
                possessionController.PossessedCreature,
                direction,
                possessionController.TargetFigure,
                possessionController);
            lastInputResult = started
                ? "Pounce started"
                : "Pounce unavailable or cooling down";
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
