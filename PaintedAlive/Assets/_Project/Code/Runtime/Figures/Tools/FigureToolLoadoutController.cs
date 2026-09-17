using System;
using PaintedAlive.Networking.M56;
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

        [Header("Runtime Theft State - Read Only")]
        [SerializeField]
        private bool toolStolen;

        [SerializeField]
        private FigureToolId stolenTool;

        [SerializeField]
        private UnityEngine.Object theftOwner;

        [SerializeField]
        private string lastTheftReason = "None";

        private FigureToolId activeTool;
        private bool initialized;

        public FigureToolId ActiveTool => activeTool;
        public bool IsToolStolen => toolStolen;
        public FigureToolId StolenTool => stolenTool;
        public UnityEngine.Object TheftOwner => theftOwner;
        public string LastTheftReason => lastTheftReason;

        public event Action<FigureToolId> ActiveToolChanged;
        public event Action<FigureToolId, bool> ToolTheftChanged;

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
            if (PaintedAliveNetworkRoleBridge.GameplayInputSuppressed)
                return;

            if (toolStolen)
            {
                return;
            }

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
            if (toolStolen && theftOwner == null)
            {
                ForceReturnStolenTool(
                    "Theft owner was destroyed; safety return");
            }

            if (!SelectionStateMatches())
            {
                ApplySelection(false);
            }
        }

        public void SelectTool(FigureToolId tool)
        {
            if (toolStolen)
            {
                return;
            }

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
            return !toolStolen && activeTool == tool;
        }

        public bool TryStealActiveTool(
            UnityEngine.Object ownerToken,
            out FigureToolId stolen)
        {
            stolen = activeTool;

            if (!Application.isPlaying ||
                ownerToken == null ||
                toolStolen ||
                !isActiveAndEnabled)
            {
                return false;
            }

            toolStolen = true;
            stolenTool = activeTool;
            theftOwner = ownerToken;
            lastTheftReason =
                $"Stolen by {ownerToken.name}";

            ApplySelection(false);
            ToolTheftChanged?.Invoke(stolenTool, true);
            return true;
        }

        public bool TryTransferStolenTool(
            UnityEngine.Object currentOwner,
            UnityEngine.Object newOwner)
        {
            if (!toolStolen ||
                currentOwner == null ||
                newOwner == null ||
                theftOwner != currentOwner)
            {
                return false;
            }

            theftOwner = newOwner;
            lastTheftReason =
                $"Transferred to {newOwner.name}";
            return true;
        }

        public bool TryReturnStolenTool(
            UnityEngine.Object ownerToken,
            string reason)
        {
            if (!toolStolen ||
                ownerToken == null ||
                theftOwner != ownerToken)
            {
                return false;
            }

            ReturnStolenToolInternal(reason);
            return true;
        }

        public void ForceReturnStolenTool(string reason)
        {
            if (!toolStolen)
            {
                return;
            }

            ReturnStolenToolInternal(reason);
        }

        private void ReturnStolenToolInternal(string reason)
        {
            FigureToolId returnedTool = stolenTool;
            toolStolen = false;
            theftOwner = null;
            lastTheftReason = string.IsNullOrWhiteSpace(reason)
                ? "Tool returned"
                : reason;

            if (isActiveAndEnabled)
            {
                ApplySelection(false);
            }

            ToolTheftChanged?.Invoke(returnedTool, false);
        }

        private void ApplySelection(bool force)
        {
            bool toolAvailable = !toolStolen;
            bool usePaletteKnife =
                toolAvailable &&
                activeTool == FigureToolId.PaletteKnife;
            bool useFixativeSpray =
                toolAvailable &&
                activeTool == FigureToolId.FixativeSpray;
            bool useFrameGun =
                toolAvailable &&
                activeTool == FigureToolId.FrameGun;
            bool useSponge =
                toolAvailable &&
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
            bool toolAvailable = !toolStolen;
            bool usePaletteKnife =
                toolAvailable &&
                activeTool == FigureToolId.PaletteKnife;
            bool useFixativeSpray =
                toolAvailable &&
                activeTool == FigureToolId.FixativeSpray;
            bool useFrameGun =
                toolAvailable &&
                activeTool == FigureToolId.FrameGun;
            bool useSponge =
                toolAvailable &&
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

        public static string GetDisplayName(FigureToolId tool)
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
