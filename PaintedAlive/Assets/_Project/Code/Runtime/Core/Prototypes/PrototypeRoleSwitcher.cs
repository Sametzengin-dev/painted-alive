using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Core.Prototypes
{
    public sealed class PrototypeRoleSwitcher : MonoBehaviour
    {
        private enum PrototypeRole
        {
            Figure,
            Painter
        }

        [Header("Initial State")]
        [SerializeField]
        private PrototypeRole initialRole = PrototypeRole.Figure;

        [Header("Figure")]
        [SerializeField] private GameObject figureCamera;
        [SerializeField] private Behaviour[] figureBehaviours;

        [Header("Painter")]
        [SerializeField] private GameObject painterCamera;
        [SerializeField] private Behaviour[] painterBehaviours;

        private PrototypeRole currentRole;
        private bool interactionsLocked;

        private void Start()
        {
            currentRole = initialRole;
            ApplyState();
        }

        private void Update()
        {
            if (interactionsLocked ||
                Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                SelectFigure();
            }

            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                SelectPainter();
            }
        }

        public void SelectFigure()
        {
            currentRole = PrototypeRole.Figure;
            ApplyState();
        }

        public void SelectPainter()
        {
            currentRole = PrototypeRole.Painter;
            ApplyState();
        }

        public void SetInteractionsLocked(bool locked)
        {
            interactionsLocked = locked;
            ApplyState();
        }

        private void ApplyState()
        {
            bool figureSelected =
                currentRole == PrototypeRole.Figure;

            if (figureCamera != null)
            {
                figureCamera.SetActive(figureSelected);
            }

            if (painterCamera != null)
            {
                painterCamera.SetActive(!figureSelected);
            }

            SetBehavioursEnabled(
                figureBehaviours,
                figureSelected && !interactionsLocked);

            SetBehavioursEnabled(
                painterBehaviours,
                !figureSelected && !interactionsLocked);
        }

        private static void SetBehavioursEnabled(
            Behaviour[] behaviours,
            bool enabled)
        {
            if (behaviours == null)
            {
                return;
            }

            foreach (Behaviour behaviour in behaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = enabled;
                }
            }
        }
    }
}
