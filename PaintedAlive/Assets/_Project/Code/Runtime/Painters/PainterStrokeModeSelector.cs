using System;
using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Painters
{
    [DisallowMultipleComponent]
    public sealed class PainterStrokeModeSelector : MonoBehaviour
    {
        [SerializeField]
        private OilStrokeShape initialShape = OilStrokeShape.Wall;

        [Header("Input")]
        [SerializeField] private InputActionReference selectWallAction;
        [SerializeField] private InputActionReference selectRampAction;

        [Header("HUD")]
        [SerializeField] private Text modeText;

        public event Action<OilStrokeShape> ShapeChanged;

        public OilStrokeShape CurrentShape { get; private set; }

        private void Awake()
        {
            CurrentShape = initialShape;
            UpdateHud();
        }

        private void OnEnable()
        {
            SetActionEnabled(selectWallAction, true);
            SetActionEnabled(selectRampAction, true);

            UpdateHud();
        }

        private void OnDisable()
        {
            SetActionEnabled(selectWallAction, false);
            SetActionEnabled(selectRampAction, false);
        }

        private void Update()
        {
            if (selectWallAction != null &&
                selectWallAction.action != null &&
                selectWallAction.action.WasPressedThisFrame())
            {
                SelectShape(OilStrokeShape.Wall);
            }

            if (selectRampAction != null &&
                selectRampAction.action != null &&
                selectRampAction.action.WasPressedThisFrame())
            {
                SelectShape(OilStrokeShape.Ramp);
            }
        }

        private void SelectShape(OilStrokeShape shape)
        {
            if (CurrentShape == shape)
            {
                return;
            }

            CurrentShape = shape;
            UpdateHud();
            ShapeChanged?.Invoke(CurrentShape);
        }

        private void UpdateHud()
        {
            if (modeText == null)
            {
                return;
            }

            modeText.text = CurrentShape switch
            {
                OilStrokeShape.Wall =>
                    "1  IMPASTO DUVARI\n2  Kabartma Rampası",

                OilStrokeShape.Ramp =>
                    "1  Impasto Duvarı\n2  KABARTMA RAMPASI",

                _ => CurrentShape.ToString()
            };
        }

        private static void SetActionEnabled(
            InputActionReference actionReference,
            bool enabled)
        {
            if (actionReference == null ||
                actionReference.action == null)
            {
                return;
            }

            if (enabled)
            {
                actionReference.action.Enable();
            }
            else
            {
                actionReference.action.Disable();
            }
        }
    }
}
