using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Button closeButton;

        [SerializeField] private Text advanceLabel;
        [SerializeField] private Text attackLabel;

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
            Button configuredCloseButton,
            Text configuredAdvanceLabel,
            Text configuredAttackLabel)
        {
            UnbindListeners();

            controller = configuredController;
            advanceButton = configuredAdvanceButton;
            undoButton = configuredUndoButton;
            clearButton = configuredClearButton;
            attackButton = configuredAttackButton;
            closeButton = configuredCloseButton;
            advanceLabel = configuredAdvanceLabel;
            attackLabel = configuredAttackLabel;

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

            closeButton.onClick.AddListener(
                HandleClose);

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
        }

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

        private void HandleClose()
        {
            controller.CloseFromToolbar();
        }
    }
}
