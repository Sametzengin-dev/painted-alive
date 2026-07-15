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

        private void Start()
        {
            SetRole(initialRole);
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                SetRole(PrototypeRole.Figure);
            }

            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                SetRole(PrototypeRole.Painter);
            }
        }

        private void SetRole(PrototypeRole role)
        {
            currentRole = role;

            bool figureActive =
                currentRole == PrototypeRole.Figure;

            SetBehavioursEnabled(
                figureBehaviours,
                figureActive);

            SetBehavioursEnabled(
                painterBehaviours,
                !figureActive);

            if (figureCamera != null)
            {
                figureCamera.SetActive(figureActive);
            }

            if (painterCamera != null)
            {
                painterCamera.SetActive(!figureActive);
            }
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
