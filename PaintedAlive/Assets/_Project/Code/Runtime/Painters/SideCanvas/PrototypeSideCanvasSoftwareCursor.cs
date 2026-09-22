using PaintedAlive.Networking.M56;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Painters.SideCanvas
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSideCanvasSoftwareCursor :
        MonoBehaviour
    {
        [Header("Core")]
        [SerializeField]
        private PrototypeLivingSideCanvasController controller;

        [SerializeField] private RectTransform cursorRect;
        [SerializeField] private CanvasGroup cursorGroup;
        [SerializeField]
        private PrototypeSideCanvasPointerInput drawingInput;

        [Header("Manual Toolbar Targets")]
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button deployButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button[] colorButtons = new Button[0];

        [Header("Virtual Pointer")]
        [SerializeField, Range(0.25f, 3f)]
        private float mouseSensitivity = 1f;

        [SerializeField, Range(0f, 64f)]
        private float screenPadding = 8f;

        [SerializeField, Range(20f, 500f)]
        private float maximumDeltaPerFrame = 180f;

        [Header("Runtime Read Only")]
        [SerializeField] private Vector2 virtualScreenPosition;
        [SerializeField] private bool initializedForOpen;
        [SerializeField] private bool drawingGestureActive;
        [SerializeField] private int visibleFrameCount;
        [SerializeField] private int deltaFrameCount;
        [SerializeField] private int drawingGestureCount;
        [SerializeField] private int toolbarClickCount;
        [SerializeField] private string hoveredTarget = "None";

        public bool IsConfigured =>
            controller != null &&
            cursorRect != null &&
            cursorGroup != null &&
            drawingInput != null;

        public int VisibleFrameCount =>
            visibleFrameCount;

        public int DeltaFrameCount =>
            deltaFrameCount;

        public int DrawingGestureCount =>
            drawingGestureCount;

        public int ToolbarClickCount =>
            toolbarClickCount;

        public Vector2 VirtualScreenPosition =>
            virtualScreenPosition;

        public string HoveredTarget =>
            hoveredTarget;

        public void Configure(
            PrototypeLivingSideCanvasController configuredController,
            RectTransform configuredCursorRect,
            CanvasGroup configuredCursorGroup,
            PrototypeSideCanvasPointerInput configuredDrawingInput,
            Button configuredAdvanceButton,
            Button configuredUndoButton,
            Button configuredClearButton,
            Button configuredAttackButton,
            Button configuredDeployButton,
            Button configuredCloseButton,
            Button[] configuredColorButtons)
        {
            controller = configuredController;
            cursorRect = configuredCursorRect;
            cursorGroup = configuredCursorGroup;
            drawingInput = configuredDrawingInput;
            advanceButton = configuredAdvanceButton;
            undoButton = configuredUndoButton;
            clearButton = configuredClearButton;
            attackButton = configuredAttackButton;
            deployButton = configuredDeployButton;
            closeButton = configuredCloseButton;
            colorButtons = configuredColorButtons ?? new Button[0];

            cursorGroup.interactable = false;
            cursorGroup.blocksRaycasts = false;
            cursorGroup.alpha = 0f;

            initializedForOpen = false;
            drawingGestureActive = false;
        }

        private void LateUpdate()
        {
            bool visible =
                !PaintedAliveNetworkRoleBridge.GameplayInputSuppressed &&
                IsConfigured &&
                controller.IsOpen &&
                Mouse.current != null;

            cursorGroup.alpha =
                visible ? 1f : 0f;

            if (!visible)
            {
                initializedForOpen = false;
                drawingGestureActive = false;
                    hoveredTarget = "None";
                return;
            }

            if (!initializedForOpen)
            {
                InitializeVirtualPosition();
            }

            UpdateVirtualPosition();
            UpdateHover();
            ProcessButtons();

            cursorRect.position =
                virtualScreenPosition;

            cursorRect.SetAsLastSibling();
            visibleFrameCount++;
        }

        private void InitializeVirtualPosition()
        {
            Vector2 start =
                new Vector2(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f);

            RectTransform drawingRect =
                drawingInput.InputRect;

            if (drawingRect != null)
            {
                Vector3 worldCenter =
                    drawingRect.TransformPoint(
                        drawingRect.rect.center);

                start =
                    RectTransformUtility
                        .WorldToScreenPoint(
                            null,
                            worldCenter);
            }

            virtualScreenPosition =
                ClampToScreen(start);

            initializedForOpen = true;
            drawingGestureActive = false;
        }

        private void UpdateVirtualPosition()
        {
            Vector2 delta =
                Mouse.current.delta.ReadValue();

            float maximum =
                Mathf.Max(
                    20f,
                    maximumDeltaPerFrame);

            if (delta.magnitude > maximum)
            {
                delta =
                    delta.normalized *
                    maximum;
            }

            if (delta.sqrMagnitude > 0.0001f)
            {
                deltaFrameCount++;
            }

            virtualScreenPosition =
                ClampToScreen(
                    virtualScreenPosition +
                    delta *
                    mouseSensitivity);
        }

        private void ProcessButtons()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (TryInvokeToolbarAtPosition())
                {
                    drawingGestureActive = false;
                }
                else if (drawingInput.TryNormalizeScreenPoint(
                             virtualScreenPosition,
                             clampOutside: false,
                             out Vector2 normalized))
                {
                    drawingGestureActive = true;
                    drawingGestureCount++;
                    drawingInput.RecordVirtualEvent();

                    controller.HandleCanvasPointerDown(
                        normalized,
                        PointerEventData.InputButton.Left);
                }
            }

            if (mouse.leftButton.isPressed &&
                drawingGestureActive &&
                drawingInput.TryNormalizeScreenPoint(
                    virtualScreenPosition,
                    clampOutside: true,
                    out Vector2 dragNormalized))
            {
                drawingInput.RecordVirtualEvent();

                controller.HandleCanvasPointerDrag(
                    dragNormalized,
                    PointerEventData.InputButton.Left);
            }

            if (mouse.leftButton.wasReleasedThisFrame &&
                drawingGestureActive)
            {
                drawingInput.TryNormalizeScreenPoint(
                    virtualScreenPosition,
                    clampOutside: true,
                    out Vector2 upNormalized);

                drawingInput.RecordVirtualEvent();

                controller.HandleCanvasPointerUp(
                    upNormalized,
                    PointerEventData.InputButton.Left);

                drawingGestureActive = false;
            }

            if (mouse.rightButton.wasPressedThisFrame &&
                drawingInput.TryNormalizeScreenPoint(
                    virtualScreenPosition,
                    clampOutside: false,
                    out Vector2 rightNormalized))
            {
                drawingInput.RecordVirtualEvent();

                controller.HandleCanvasPointerDown(
                    rightNormalized,
                    PointerEventData.InputButton.Right);
            }

            if (mouse.rightButton.wasReleasedThisFrame)
            {
                }
        }

        private bool TryInvokeToolbarAtPosition()
        {
            Button target =
                ResolveToolbarButton(
                    virtualScreenPosition,
                    out string targetName);

            if (target == null ||
                !target.interactable ||
                !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            hoveredTarget = targetName;
            toolbarClickCount++;
            target.onClick.Invoke();
            return true;
        }

        private void UpdateHover()
        {
            if (drawingInput.ContainsScreenPoint(
                    virtualScreenPosition))
            {
                hoveredTarget = "DrawingCanvas";
                return;
            }

            ResolveToolbarButton(
                virtualScreenPosition,
                out string targetName);

            hoveredTarget =
                string.IsNullOrWhiteSpace(targetName)
                    ? "None"
                    : targetName;
        }

        private Button ResolveToolbarButton(
            Vector2 screenPoint,
            out string targetName)
        {
            Button[] buttons =
            {
                advanceButton,
                undoButton,
                clearButton,
                attackButton,
                deployButton,
                closeButton
            };

            string[] names =
            {
                "Advance",
                "Undo",
                "Clear",
                "Attack",
                "Deploy",
                "Close"
            };

            for (int index = 0;
                 index < buttons.Length;
                 index++)
            {
                Button button =
                    buttons[index];

                if (button == null)
                {
                    continue;
                }

                RectTransform rect =
                    button.transform as RectTransform;

                if (rect != null &&
                    RectTransformUtility
                        .RectangleContainsScreenPoint(
                            rect,
                            screenPoint,
                            null))
                {
                    targetName = names[index];
                    return button;
                }
            }

            for (int index = 0; index < colorButtons.Length; index++)
            {
                Button button = colorButtons[index];
                if (button == null)
                {
                    continue;
                }

                RectTransform rect = button.transform as RectTransform;
                if (rect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        rect,
                        screenPoint,
                        null))
                {
                    targetName = $"Color{index + 1}";
                    return button;
                }
            }

            targetName = string.Empty;
            return null;
        }

        private Vector2 ClampToScreen(
            Vector2 position)
        {
            float padding =
                Mathf.Max(
                    0f,
                    screenPadding);

            return new Vector2(
                Mathf.Clamp(
                    position.x,
                    padding,
                    Mathf.Max(
                        padding,
                        Screen.width - padding)),
                Mathf.Clamp(
                    position.y,
                    padding,
                    Mathf.Max(
                        padding,
                        Screen.height - padding)));
        }
    }
}
