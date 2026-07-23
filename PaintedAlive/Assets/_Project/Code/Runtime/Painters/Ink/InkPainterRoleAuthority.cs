using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Paint.Ink.Possession;
using PaintedAlive.Paint.Watercolor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Painters.Ink
{
    public enum PaintedAliveLocalRole
    {
        Figure = 0,
        InkPainter = 1
    }

    [DefaultExecutionOrder(-20000)]
    [DisallowMultipleComponent]
    public sealed class InkPainterRoleAuthority : MonoBehaviour
    {
        private static InkPainterRoleAuthority activeInstance;

        [Header("Figure")]
        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private Camera figureCamera;

        [Header("Ink Painter")]
        [SerializeField]
        private Camera painterCamera;

        [SerializeField]
        private InkPainterIndependentCamera painterCameraController;

        [SerializeField]
        private InkPossessionController possessionController;

        [SerializeField]
        private InkPainterNestController nestController;

        [SerializeField]
        private GameObject painterHudRoot;

        [SerializeField]
        private GameObject painterCrosshairRoot;

        [Header("Figure-Only Prototype Inputs")]
        [SerializeField]
        private WatercolorFlowDebugSpawner[] watercolorSpawners;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private PaintedAliveLocalRole currentRole =
            PaintedAliveLocalRole.Figure;

        [SerializeField]
        private string lastRoleReason = "Not started";

        private AudioListener figureListener;
        private AudioListener painterListener;
        private bool applyingRole;

        public static InkPainterRoleAuthority ActiveInstance =>
            activeInstance;
        public PaintedAliveLocalRole CurrentRole => currentRole;
        public bool IsInkPainter =>
            currentRole == PaintedAliveLocalRole.InkPainter;
        public string LastRoleReason => lastRoleReason;
        public Camera ActiveRoleCamera => IsInkPainter
            ? painterCamera
            : figureCamera;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeInstance = null;
        }

        private void Awake()
        {
            if (activeInstance != null && activeInstance != this)
            {
                Debug.LogError(
                    "Duplicate InkPainterRoleAuthority disabled. Run M21 " +
                    "Diagnose and keep one local role authority.",
                    this);
                enabled = false;
                return;
            }

            activeInstance = this;
            figureListener = figureCamera != null
                ? figureCamera.GetComponent<AudioListener>()
                : null;
            painterListener = painterCamera != null
                ? painterCamera.GetComponent<AudioListener>()
                : null;
            CacheFigureOnlyInputs();

            if (figureMotor == null ||
                figureCamera == null ||
                painterCamera == null ||
                painterCameraController == null ||
                possessionController == null ||
                nestController == null)
            {
                Debug.LogError(
                    "InkPainterRoleAuthority references are incomplete. " +
                    "Run M21 Setup again.",
                    this);
                enabled = false;
                return;
            }

            ApplyRole(PaintedAliveLocalRole.Figure, "Play Mode started");
        }

        private void OnEnable()
        {
            if (activeInstance == null || activeInstance == this)
            {
                activeInstance = this;
            }
        }

        private void OnDisable()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || IsEditingText())
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                ApplyRole(
                    PaintedAliveLocalRole.Figure,
                    "F1 selected Figure");
            }
            else if (keyboard.f2Key.wasPressedThisFrame)
            {
                ApplyRole(
                    PaintedAliveLocalRole.InkPainter,
                    "F2 selected Ink Painter");
            }
        }

        private void LateUpdate()
        {
            if (applyingRole)
            {
                return;
            }

            bool painterActive = IsInkPainter;
            bool possessionActive = possessionController != null &&
                possessionController.IsPossessing;
            SetCameraState(
                figureCamera,
                figureListener,
                !painterActive);
            SetCameraState(
                painterCamera,
                painterListener,
                painterActive);

            if (painterCameraController != null &&
                painterCameraController.enabled !=
                (painterActive && !possessionActive))
            {
                painterCameraController.enabled =
                    painterActive && !possessionActive;
            }

            if (painterHudRoot != null &&
                painterHudRoot.activeSelf != painterActive)
            {
                painterHudRoot.SetActive(painterActive);
            }

            if (painterCrosshairRoot != null &&
                painterCrosshairRoot.activeSelf != painterActive)
            {
                painterCrosshairRoot.SetActive(painterActive);
            }

            SetFigureOnlyInputState(!painterActive);
        }

        public void Configure(
            FigureMotor targetFigure,
            Camera targetFigureCamera,
            Camera targetPainterCamera,
            InkPainterIndependentCamera targetCameraController,
            InkPossessionController targetPossession,
            InkPainterNestController targetNestController,
            GameObject targetPainterHud,
            GameObject targetCrosshair)
        {
            figureMotor = targetFigure;
            figureCamera = targetFigureCamera;
            painterCamera = targetPainterCamera;
            painterCameraController = targetCameraController;
            possessionController = targetPossession;
            nestController = targetNestController;
            painterHudRoot = targetPainterHud;
            painterCrosshairRoot = targetCrosshair;
            figureListener = figureCamera != null
                ? figureCamera.GetComponent<AudioListener>()
                : null;
            painterListener = painterCamera != null
                ? painterCamera.GetComponent<AudioListener>()
                : null;
        }

        public void SetInkPainterRole()
        {
            ApplyRole(
                PaintedAliveLocalRole.InkPainter,
                "Selected by public role command");
        }

        public void SetFigureRole()
        {
            ApplyRole(
                PaintedAliveLocalRole.Figure,
                "Selected by public role command");
        }

        public void ApplyRole(
            PaintedAliveLocalRole role,
            string reason)
        {
            if (applyingRole)
            {
                return;
            }

            applyingRole = true;

            if (role == PaintedAliveLocalRole.Figure &&
                possessionController != null &&
                possessionController.IsPossessing)
            {
                possessionController.ExitPossession(
                    "Role changed to Figure");
            }

            currentRole = role;
            lastRoleReason = string.IsNullOrWhiteSpace(reason)
                ? role.ToString()
                : reason;
            bool painterActive =
                role == PaintedAliveLocalRole.InkPainter;

            if (figureMotor != null)
            {
                figureMotor.enabled = !painterActive;
            }

            if (possessionController != null)
            {
                possessionController.enabled = painterActive;
            }

            if (nestController != null)
            {
                nestController.enabled = painterActive;
            }

            if (painterCameraController != null)
            {
                painterCameraController.enabled = painterActive;
            }

            SetCameraState(
                figureCamera,
                figureListener,
                !painterActive);
            SetCameraState(
                painterCamera,
                painterListener,
                painterActive);

            if (painterHudRoot != null)
            {
                painterHudRoot.SetActive(painterActive);
            }

            if (painterCrosshairRoot != null)
            {
                painterCrosshairRoot.SetActive(painterActive);
            }

            SetFigureOnlyInputState(!painterActive);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            applyingRole = false;

            Debug.Log(
                painterActive
                    ? "[M21.1] Ink Painter role active. F7: nest, F6: " +
                      "possess, WASD/QE: camera, R: reframe, F1: Figure."
                    : "[M21.1] Figure role active. Painter F6/F7 input " +
                      "is blocked, Figure F8 is enabled. Press F2 for " +
                      "Ink Painter.",
                this);
        }

        private void CacheFigureOnlyInputs()
        {
            if (watercolorSpawners != null &&
                watercolorSpawners.Length > 0)
            {
                return;
            }

            watercolorSpawners =
                Object.FindObjectsByType<WatercolorFlowDebugSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
        }

        private void SetFigureOnlyInputState(bool figureActive)
        {
            CacheFigureOnlyInputs();

            for (int i = 0; i < watercolorSpawners.Length; i++)
            {
                WatercolorFlowDebugSpawner spawner =
                    watercolorSpawners[i];

                if (spawner != null &&
                    spawner.enabled != figureActive)
                {
                    spawner.enabled = figureActive;
                }
            }
        }

        private static void SetCameraState(
            Camera targetCamera,
            AudioListener listener,
            bool active)
        {
            if (targetCamera != null &&
                targetCamera.enabled != active)
            {
                targetCamera.enabled = active;
            }

            if (listener != null &&
                listener.enabled != active)
            {
                listener.enabled = active;
            }
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
