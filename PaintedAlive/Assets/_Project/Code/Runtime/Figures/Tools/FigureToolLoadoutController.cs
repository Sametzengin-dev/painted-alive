using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    public enum FigureToolId
    {
        PaletteKnife,
        FixativeSpray,
        FrameGun,
        Sponge
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

        [SerializeField]
        private MonoBehaviour spongeController;

        [Header("Optional Tool Visuals")]
        [SerializeField]
        private GameObject paletteKnifeVisual;

        [SerializeField]
        private GameObject fixativeSprayVisual;

        [SerializeField]
        private GameObject frameGunVisual;

        [SerializeField]
        private GameObject spongeVisual;

        [Header("Optional Rebindable Actions")]
        [SerializeField]
        private InputActionReference selectPaletteKnifeAction;

        [SerializeField]
        private InputActionReference selectFixativeSprayAction;

        [SerializeField]
        private InputActionReference selectFrameGunAction;

        [SerializeField]
        private InputActionReference selectSpongeAction;

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
            EnableAction(selectSpongeAction);

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
            DisableAction(selectSpongeAction);

            SetControllerEnabled(paletteKnifeController, false);
            SetControllerEnabled(fixativeSprayController, false);
            SetControllerEnabled(frameGunController, false);
            SetControllerEnabled(spongeController, false);

            SetVisualActive(paletteKnifeVisual, false);
            SetVisualActive(fixativeSprayVisual, false);
            SetVisualActive(frameGunVisual, false);
            SetVisualActive(spongeVisual, false);
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
            else if (WasSpongeSelected())
            {
                SelectTool(FigureToolId.Sponge);
            }
        }

        private void LateUpdate()
        {
            if (!SelectionStateMatches())
            {
                ApplySelection(false);
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
            bool useSponge =
                activeTool == FigureToolId.Sponge;

            SetControllerEnabled(paletteKnifeController, false);
            SetControllerEnabled(fixativeSprayController, false);
            SetControllerEnabled(frameGunController, false);
            SetControllerEnabled(spongeController, false);

            if (usePaletteKnife)
            {
                SetControllerEnabled(paletteKnifeController, true);
            }
            else if (useFixativeSpray)
            {
                SetControllerEnabled(fixativeSprayController, true);
            }
            else if (useFrameGun)
            {
                SetControllerEnabled(frameGunController, true);
            }
            else if (useSponge)
            {
                SetControllerEnabled(spongeController, true);
            }

            SetVisualActive(paletteKnifeVisual, usePaletteKnife);
            SetVisualActive(fixativeSprayVisual, useFixativeSpray);
            SetVisualActive(frameGunVisual, useFrameGun);
            SetVisualActive(spongeVisual, useSponge);

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

        private bool SelectionStateMatches()
        {
            bool usePaletteKnife =
                activeTool == FigureToolId.PaletteKnife;
            bool useFixativeSpray =
                activeTool == FigureToolId.FixativeSpray;
            bool useFrameGun =
                activeTool == FigureToolId.FrameGun;
            bool useSponge =
                activeTool == FigureToolId.Sponge;

            return ControllerMatches(
                       paletteKnifeController,
                       usePaletteKnife) &&
                   ControllerMatches(
                       fixativeSprayController,
                       useFixativeSpray) &&
                   ControllerMatches(
                       frameGunController,
                       useFrameGun) &&
                   ControllerMatches(
                       spongeController,
                       useSponge) &&
                   VisualMatches(
                       paletteKnifeVisual,
                       usePaletteKnife) &&
                   VisualMatches(
                       fixativeSprayVisual,
                       useFixativeSpray) &&
                   VisualMatches(
                       frameGunVisual,
                       useFrameGun) &&
                   VisualMatches(
                       spongeVisual,
                       useSponge);
        }

        private bool WasPaletteKnifeSelected()
        {
            if (WasActionPressed(selectPaletteKnifeAction))
            {
                return true;
            }

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.digit1Key.wasPressedThisFrame;
            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.dpad.left.wasPressedThisFrame;
            return keyboardPressed || gamepadPressed;
        }

        private bool WasFixativeSpraySelected()
        {
            if (WasActionPressed(selectFixativeSprayAction))
            {
                return true;
            }

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.digit2Key.wasPressedThisFrame;
            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.dpad.right.wasPressedThisFrame;
            return keyboardPressed || gamepadPressed;
        }

        private bool WasFrameGunSelected()
        {
            if (WasActionPressed(selectFrameGunAction))
            {
                return true;
            }

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.digit3Key.wasPressedThisFrame;
            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.dpad.up.wasPressedThisFrame;
            return keyboardPressed || gamepadPressed;
        }

        private bool WasSpongeSelected()
        {
            if (WasActionPressed(selectSpongeAction))
            {
                return true;
            }

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.digit4Key.wasPressedThisFrame;
            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.dpad.down.wasPressedThisFrame;
            return keyboardPressed || gamepadPressed;
        }

        private static bool WasActionPressed(
            InputActionReference actionReference)
        {
            return actionReference != null &&
                   actionReference.action != null &&
                   actionReference.action.WasPressedThisFrame();
        }

        private static void SetControllerEnabled(
            MonoBehaviour controller,
            bool value)
        {
            if (controller != null)
            {
                controller.enabled = value;
            }
        }

        private static bool ControllerMatches(
            MonoBehaviour controller,
            bool expected)
        {
            return controller == null ||
                   controller.enabled == expected;
        }

        private static void SetVisualActive(
            GameObject visual,
            bool value)
        {
            if (visual != null)
            {
                visual.SetActive(value);
            }
        }

        private static bool VisualMatches(
            GameObject visual,
            bool expected)
        {
            return visual == null ||
                   visual.activeSelf == expected;
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
                FigureToolId.PaletteKnife => "Palet Bıçağı",
                FigureToolId.FixativeSpray => "Sabitleyici Sprey",
                FigureToolId.FrameGun => "Çerçeve Tabancası",
                FigureToolId.Sponge => "Sünger",
                _ => tool.ToString()
            };
        }
    }
}
