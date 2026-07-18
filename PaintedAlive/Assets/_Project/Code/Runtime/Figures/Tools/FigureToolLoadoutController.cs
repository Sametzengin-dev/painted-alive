using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    public enum FigureToolId
    {
        PaletteKnife,
        FixativeSpray,
        FrameGun
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

        [SerializeField]
        private MonoBehaviour frameGunController;

        [Header("Optional Tool Visuals")]
        [SerializeField]
        private GameObject paletteKnifeVisual;

        [SerializeField]
        private GameObject fixativeSprayVisual;

        [SerializeField]
        private GameObject frameGunVisual;

        [Header("Optional Rebindable Actions")]
        [SerializeField]
        private InputActionReference selectPaletteKnifeAction;

        [SerializeField]
        private InputActionReference selectFixativeSprayAction;

        [SerializeField]
        private InputActionReference selectFrameGunAction;

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
            EnableAction(selectFrameGunAction);

            if (initialized)
            {
                ApplySelection(true);
            }
        }

        private void OnDisable()
        {
            DisableAction(selectPaletteKnifeAction);
            DisableAction(selectFixativeSprayAction);
            DisableAction(selectFrameGunAction);
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
            else if (WasFrameGunSelected())
            {
                SelectTool(FigureToolId.FrameGun);
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

            bool useFixativeSpray =
                activeTool == FigureToolId.FixativeSpray;

            bool useFrameGun =
                activeTool == FigureToolId.FrameGun;

            if (paletteKnifeController != null)
            {
                paletteKnifeController.enabled = false;
            }

            if (fixativeSprayController != null)
            {
                fixativeSprayController.enabled = false;
            }

            if (frameGunController != null)
            {
                frameGunController.enabled = false;
            }

            if (usePaletteKnife &&
                paletteKnifeController != null)
            {
                paletteKnifeController.enabled = true;
            }
            else if (useFixativeSpray &&
                     fixativeSprayController != null)
            {
                fixativeSprayController.enabled = true;
            }
            else if (useFrameGun &&
                     frameGunController != null)
            {
                frameGunController.enabled = true;
            }

            if (paletteKnifeVisual != null)
            {
                paletteKnifeVisual.SetActive(usePaletteKnife);
            }

            if (fixativeSprayVisual != null)
            {
                fixativeSprayVisual.SetActive(useFixativeSpray);
            }

            if (frameGunVisual != null)
            {
                frameGunVisual.SetActive(useFrameGun);
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

        private bool WasFrameGunSelected()
        {
            if (selectFrameGunAction != null &&
                selectFrameGunAction.action != null)
            {
                return selectFrameGunAction.action
                    .WasPressedThisFrame();
            }

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.digit3Key
                    .wasPressedThisFrame;

            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.dpad.up
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

                FigureToolId.FrameGun =>
                    "Çerçeve Tabancası",

                _ => tool.ToString()
            };
        }
    }
}
