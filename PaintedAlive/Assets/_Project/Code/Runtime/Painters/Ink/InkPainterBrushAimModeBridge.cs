using System;
using System.Reflection;
using PaintedAlive.Paint.Ink.Economy;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PaintedAlive.Painters.Ink
{
    [DefaultExecutionOrder(30000)]
    [DisallowMultipleComponent]
    public sealed class InkPainterBrushAimModeBridge : MonoBehaviour
    {
        private static readonly string[] CameraFieldNames =
        {
            "outputCamera",
            "sourceCamera",
            "painterCamera",
            "targetCamera",
            "controlledCamera"
        };

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private Camera painterCamera;

        [SerializeField]
        private InkPainterNestController nestController;

        [SerializeField]
        private MonoBehaviour[] painterAimControllers =
            Array.Empty<MonoBehaviour>();

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int reboundControllerCount;

        [SerializeField]
        private string currentAimMode = "Figure";

        private bool wasPainter;

        public int ReboundControllerCount => reboundControllerCount;
        public string CurrentAimMode => currentAimMode;

        private void Awake()
        {
            ApplyPainterCameraBindings();
        }

        private void OnEnable()
        {
            ApplyPainterCameraBindings();
        }

        private void LateUpdate()
        {
            if (roleAuthority == null)
            {
                currentAimMode = "Authority missing";
                return;
            }

            bool painterActive = roleAuthority.IsInkPainter;

            if (painterActive && !wasPainter)
            {
                ApplyPainterCameraBindings();
            }

            wasPainter = painterActive;

            if (!painterActive)
            {
                currentAimMode = "Figure";
                return;
            }

            if (!IsEditingText())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            currentAimMode =
                nestController != null && nestController.IsCasting
                    ? "Nest"
                    : "Brush";
        }

        public void Configure(
            InkPainterRoleAuthority authority,
            Camera targetPainterCamera,
            InkPainterNestController targetNestController,
            MonoBehaviour[] targetPainterAimControllers)
        {
            roleAuthority = authority;
            painterCamera = targetPainterCamera;
            nestController = targetNestController;
            painterAimControllers =
                targetPainterAimControllers ?? Array.Empty<MonoBehaviour>();
            ApplyPainterCameraBindings();
        }

        [ContextMenu("Debug/Rebind Painter Aim Camera")]
        public void ApplyPainterCameraBindings()
        {
            reboundControllerCount = 0;

            if (painterCamera == null || painterAimControllers == null)
            {
                return;
            }

            for (int i = 0; i < painterAimControllers.Length; i++)
            {
                MonoBehaviour controller = painterAimControllers[i];

                if (controller != null &&
                    TryAssignCamera(controller, painterCamera))
                {
                    reboundControllerCount++;
                }
            }
        }

        private static bool TryAssignCamera(
            MonoBehaviour controller,
            Camera targetCamera)
        {
            Type type = controller.GetType();
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            for (int i = 0; i < CameraFieldNames.Length; i++)
            {
                FieldInfo field = type.GetField(
                    CameraFieldNames[i],
                    flags);

                if (field == null ||
                    !typeof(Camera).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                Camera currentCamera = field.GetValue(controller) as Camera;

if (currentCamera != targetCamera)
{
    field.SetValue(controller, targetCamera);
}

                return true;
            }

            return false;
        }

        private static bool IsEditingText()
        {
            GameObject selected =
                EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject
                    : null;

            return selected != null &&
                selected.GetComponent<UnityEngine.UI.InputField>() != null;
        }
    }
}
