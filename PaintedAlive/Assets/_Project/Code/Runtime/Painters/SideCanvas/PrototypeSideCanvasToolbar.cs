using UnityEngine;
using UnityEngine.UI;
using PaintedAlive.Painters.Masterpiece;

namespace PaintedAlive.Painters.SideCanvas
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSideCanvasToolbar :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeLivingSideCanvasController controller;

        [SerializeField] private Button advanceButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button deployButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Text advanceLabel;
        [SerializeField] private Text attackLabel;
        [SerializeField] private Text deployLabel;
        [SerializeField] private Button[] colorButtons = new Button[0];
        [SerializeField] private Text[] colorLabels = new Text[0];
        [SerializeField]
        private PrototypeMasterpieceWorldDeploymentController deploymentController;

        private bool listenersBound;

        public bool IsConfigured =>
            controller != null &&
            advanceButton != null &&
            undoButton != null &&
            clearButton != null &&
            attackButton != null &&
            closeButton != null;

        public void Configure(
            PrototypeLivingSideCanvasController configuredController,
            Button configuredAdvanceButton,
            Button configuredUndoButton,
            Button configuredClearButton,
            Button configuredAttackButton,
            Button configuredDeployButton,
            Button configuredCloseButton,
            Text configuredAdvanceLabel,
            Text configuredAttackLabel,
            Text configuredDeployLabel,
            Button[] configuredColorButtons,
            Text[] configuredColorLabels,
            PrototypeMasterpieceWorldDeploymentController configuredDeploymentController)
        {
            UnbindListeners();

            controller = configuredController;
            advanceButton = configuredAdvanceButton;
            undoButton = configuredUndoButton;
            clearButton = configuredClearButton;
            attackButton = configuredAttackButton;
            deployButton = configuredDeployButton;
            closeButton = configuredCloseButton;
            advanceLabel = configuredAdvanceLabel;
            attackLabel = configuredAttackLabel;
            deployLabel = configuredDeployLabel;
            colorButtons = configuredColorButtons ?? new Button[0];
            colorLabels = configuredColorLabels ?? new Text[0];
            deploymentController = configuredDeploymentController;

            BindListeners();
            RefreshState();
        }

        private void OnEnable()
        {
            BindListeners();
            RefreshState();
        }

        private void OnDisable()
        {
            UnbindListeners();
        }

        private void Update()
        {
            RefreshState();
        }

        private void BindListeners()
        {
            if (listenersBound ||
                !IsConfigured)
            {
                return;
            }

            advanceButton.onClick.AddListener(
                HandleAdvance);

            undoButton.onClick.AddListener(
                HandleUndo);

            clearButton.onClick.AddListener(
                HandleClear);

            attackButton.onClick.AddListener(
                HandleAttack);

            if (deployButton != null)
            {
                deployButton.onClick.AddListener(
                    HandleDeploy);
            }

            closeButton.onClick.AddListener(
                HandleClose);

            BindColorListeners(true);

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveListener(
                    HandleAdvance);
            }

            if (undoButton != null)
            {
                undoButton.onClick.RemoveListener(
                    HandleUndo);
            }

            if (clearButton != null)
            {
                clearButton.onClick.RemoveListener(
                    HandleClear);
            }

            if (attackButton != null)
            {
                attackButton.onClick.RemoveListener(
                    HandleAttack);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(
                    HandleClose);
            }

            if (deployButton != null)
            {
                deployButton.onClick.RemoveListener(
                    HandleDeploy);
            }

            BindColorListeners(false);

            listenersBound = false;
        }

        private void RefreshState()
        {
            if (controller == null)
            {
                return;
            }

            bool open =
                controller.IsOpen;

            if (advanceButton != null)
            {
                advanceButton.interactable =
                    open;
            }

            if (undoButton != null)
            {
                undoButton.interactable =
                    open &&
                    (controller.StrokeCount > 0 ||
                     controller.MarkerCount > 0);
            }

            if (clearButton != null)
            {
                clearButton.interactable =
                    open &&
                    (controller.StrokeCount > 0 ||
                     controller.MarkerCount > 0);
            }

            if (attackButton != null)
            {
                attackButton.interactable =
                    open &&
                    controller.CurrentMode ==
                        PrototypeSideCanvasMode.Preview;
            }

            if (closeButton != null)
            {
                closeButton.interactable =
                    open;
            }

            if (deployButton != null)
            {
                deployButton.interactable =
                    open &&
                    controller.DeployEnabled &&
                    deploymentController != null;
            }

            if (advanceLabel != null)
            {
                switch (controller.CurrentMode)
                {
                    case PrototypeSideCanvasMode.Rig:
                        advanceLabel.text =
                            "KUKLA TESTİ";

                        break;

                    case PrototypeSideCanvasMode.Preview:
                        advanceLabel.text =
                            "ÇİZİME DÖN";

                        break;

                    default:
                        advanceLabel.text =
                            "RİGLE";

                        break;
                }
            }

            if (attackLabel != null)
            {
                attackLabel.text =
                    controller.CurrentMode ==
                        PrototypeSideCanvasMode.Preview
                        ? "SALDIRI TESTİ"
                        : "TEST KİLİTLİ";
            }

            if (deployLabel != null)
            {
                deployLabel.text = controller.DeployEnabled
                    ? "DÜNYAYA AKTAR"
                    : "AKTARIM KİLİTLİ";
            }

            for (int index = 0; index < colorButtons.Length; index++)
            {
                Button colorButton = colorButtons[index];
                if (colorButton != null)
                {
                    colorButton.interactable =
                        open &&
                        controller.CurrentMode == PrototypeSideCanvasMode.Draw;

                    RectTransform rect = colorButton.transform as RectTransform;
                    if (rect != null)
                    {
                        rect.localScale = index == controller.SelectedDrawingColorIndex
                            ? Vector3.one * 1.12f
                            : Vector3.one;
                    }
                }

                if (index < colorLabels.Length && colorLabels[index] != null)
                {
                    colorLabels[index].text =
                        index == controller.SelectedDrawingColorIndex
                            ? $"◆ {index + 1}"
                            : (index + 1).ToString();
                }
            }
        }

        private void BindColorListeners(bool bind)
        {
            if (colorButtons == null)
            {
                return;
            }

            for (int index = 0; index < colorButtons.Length; index++)
            {
                Button button = colorButtons[index];
                if (button == null)
                {
                    continue;
                }

                switch (index)
                {
                    case 0: SetColorListener(button, HandleColor0, bind); break;
                    case 1: SetColorListener(button, HandleColor1, bind); break;
                    case 2: SetColorListener(button, HandleColor2, bind); break;
                    case 3: SetColorListener(button, HandleColor3, bind); break;
                    case 4: SetColorListener(button, HandleColor4, bind); break;
                    case 5: SetColorListener(button, HandleColor5, bind); break;
                }
            }
        }

        private static void SetColorListener(
            Button button,
            UnityEngine.Events.UnityAction action,
            bool bind)
        {
            if (bind) button.onClick.AddListener(action);
            else button.onClick.RemoveListener(action);
        }

        private void HandleColor0() => controller.SelectDrawingColor(0);
        private void HandleColor1() => controller.SelectDrawingColor(1);
        private void HandleColor2() => controller.SelectDrawingColor(2);
        private void HandleColor3() => controller.SelectDrawingColor(3);
        private void HandleColor4() => controller.SelectDrawingColor(4);
        private void HandleColor5() => controller.SelectDrawingColor(5);

        private void HandleAdvance()
        {
            controller.AdvanceMode();
        }

        private void HandleUndo()
        {
            controller.Undo();
        }

        private void HandleClear()
        {
            controller.Clear();
        }

        private void HandleAttack()
        {
            controller.TriggerPreviewAttack();
        }

        private void HandleDeploy()
        {
            if (controller == null ||
                !controller.DeployEnabled ||
                deploymentController == null)
            {
                return;
            }

            deploymentController.TryDeploy();
        }

        private void HandleClose()
        {
            controller.CloseFromToolbar();
        }
    }
}
