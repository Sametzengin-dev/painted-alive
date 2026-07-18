using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    public enum FigureToolId
    {
        PaletteKnife,
        FixativeSpray
    }

    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class FigureToolLoadoutController : MonoBehaviour
    {
        [Header("Tool Controllers")]
        [SerializeField]
        private MonoBehaviour paletteKnifeController;

        [SerializeField]
        private MonoBehaviour fixativeSprayController;

        [Header("Optional Tool Visuals")]
        [SerializeField]
        private GameObject paletteKnifeVisual;

        [SerializeField]
        private GameObject fixativeSprayVisual;

        [Header("Optional Rebindable Actions")]
        [SerializeField]
        private InputActionReference selectPaletteKnifeAction;

        [SerializeField]
        private InputActionReference selectFixativeSprayAction;

        [Header("Initial State")]
        [SerializeField]
        private FigureToolId initialTool =
            FigureToolId.PaletteKnife;

        [Header("Debug")]
        [SerializeField]
        private bool logToolChanges = true;

        private FigureToolId activeTool;
        private bool initialized;

        public FigureToolId ActiveTool => activeTool;

        public event Action<FigureToolId> ActiveToolChanged;

        private void Awake()
        {
            activeTool = initialTool;
            ApplySelection(true);
            initialized = true;
        }

        private void OnEnable()
        {
            EnableAction(selectPaletteKnifeAction);
            EnableAction(selectFixativeSprayAction);

            if (initialized)
            {
                ApplySelection(true);
            }
        }

        private void OnDisable()
        {
            DisableAction(selectPaletteKnifeAction);
            DisableAction(selectFixativeSprayAction);
        }

        private void Update()
        {
            if (WasPaletteKnifeSelected())
            {
                SelectTool(FigureToolId.PaletteKnife);
            }
            else if (WasFixativeSpraySelected())
            {
                SelectTool(FigureToolId.FixativeSpray);
            }
        }

        public void SelectTool(FigureToolId tool)
        {
            if (initialized && activeTool == tool)
            {
                return;
            }

            activeTool = tool;
            ApplySelection(false);
            initialized = true;
        }

        public bool IsToolActive(FigureToolId tool)
        {
            return activeTool == tool;
        }

        private void ApplySelection(bool force)
        {
            bool usePaletteKnife =
                activeTool == FigureToolId.PaletteKnife;

            if (paletteKnifeController != null)
            {
                paletteKnifeController.enabled = false;
            }

            if (fixativeSprayController != null)
            {
                fixativeSprayController.enabled = false;
            }

            if (usePaletteKnife &&
                paletteKnifeController != null)
            {
                paletteKnifeController.enabled = true;
            }
            else if (!usePaletteKnife &&
                     fixativeSprayController != null)
            {
                fixativeSprayController.enabled = true;
            }

            if (paletteKnifeVisual != null)
            {
                paletteKnifeVisual.SetActive(usePaletteKnife);
            }

            if (fixativeSprayVisual != null)
            {
                fixativeSprayVisual.SetActive(!usePaletteKnife);
            }

            if (logToolChanges &&
                (force || Application.isPlaying))
            {
                Debug.Log(
                    $"[{nameof(FigureToolLoadoutController)}] " +
                    $"Aktif alet: {GetDisplayName(activeTool)}",
                    this);
            }

            ActiveToolChanged?.Invoke(activeTool);
        }

        private bool WasPaletteKnifeSelected()
        {
            if (selectPaletteKnifeAction != null &&
                selectPaletteKnifeAction.action != null)
            {
                return selectPaletteKnifeAction.action
                    .WasPressedThisFrame();
            }

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.digit1Key
                    .wasPressedThisFrame;

            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.dpad.left
                    .wasPressedThisFrame;

            return keyboardPressed || gamepadPressed;
        }

        private bool WasFixativeSpraySelected()
        {
            if (selectFixativeSprayAction != null &&
                selectFixativeSprayAction.action != null)
            {
                return selectFixativeSprayAction.action
                    .WasPressedThisFrame();
            }

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.digit2Key
                    .wasPressedThisFrame;

            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.dpad.right
                    .wasPressedThisFrame;

            return keyboardPressed || gamepadPressed;
        }

        private static void EnableAction(
            InputActionReference actionReference)
        {
            actionReference?.action?.Enable();
        }

        private static void DisableAction(
            InputActionReference actionReference)
        {
            actionReference?.action?.Disable();
        }

        private static string GetDisplayName(FigureToolId tool)
        {
            return tool switch
            {
                FigureToolId.PaletteKnife =>
                    "Palet Bıçağı",

                FigureToolId.FixativeSpray =>
                    "Sabitleyici Sprey",

                _ => tool.ToString()
            };
        }
    }
}
