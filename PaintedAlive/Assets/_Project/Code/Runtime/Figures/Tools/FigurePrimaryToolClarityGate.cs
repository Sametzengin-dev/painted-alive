using PaintedAlive.Painters.Ink;
using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    /// <summary>
    /// Makes the Figure primary-tool loadout mutually exclusive with the
    /// full Stain support form and with the local Ink Painter role.
    /// M25 Stain sabotage reads E independently, so disabling the loadout
    /// leaves E exclusively available to the sabotage controller.
    /// </summary>
    [DefaultExecutionOrder(-19000)]
    [DisallowMultipleComponent]
    public sealed class FigurePrimaryToolClarityGate : MonoBehaviour
    {
        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private FigureToolLoadoutController toolLoadout;

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool loadoutSuppressedByGate;

        [SerializeField]
        private string lastState = "Not evaluated";

        public bool LoadoutSuppressed => loadoutSuppressedByGate;
        public string LastState => lastState;

        private void Awake()
        {
            ResolveReferences();

            if (clarityState == null ||
                toolLoadout == null ||
                roleAuthority == null)
            {
                Debug.LogError(
                    $"{nameof(FigurePrimaryToolClarityGate)} requires " +
                    "FigureClarityState, FigureToolLoadoutController and " +
                    "InkPainterRoleAuthority. Run M25.1 Setup again.",
                    this);
                enabled = false;
                return;
            }

            ApplyGate();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (clarityState != null)
            {
                clarityState.LevelChanged -= HandleClarityLevelChanged;
                clarityState.LevelChanged += HandleClarityLevelChanged;
            }

            ApplyGate();
        }

        private void OnDisable()
        {
            if (clarityState != null)
            {
                clarityState.LevelChanged -= HandleClarityLevelChanged;
            }
        }

        private void Update()
        {
            ApplyGate();
        }

        public void Configure(
            FigureClarityState targetClarityState,
            FigureToolLoadoutController targetToolLoadout,
            InkPainterRoleAuthority targetRoleAuthority)
        {
            if (clarityState != null)
            {
                clarityState.LevelChanged -= HandleClarityLevelChanged;
            }

            clarityState = targetClarityState;
            toolLoadout = targetToolLoadout;
            roleAuthority = targetRoleAuthority;

            if (isActiveAndEnabled && clarityState != null)
            {
                clarityState.LevelChanged += HandleClarityLevelChanged;
            }

            ApplyGate();
        }

        private void HandleClarityLevelChanged(
            FigureClarityLevel previous,
            FigureClarityLevel current)
        {
            ApplyGate();
        }

        private void ApplyGate()
        {
            if (clarityState == null ||
                toolLoadout == null ||
                roleAuthority == null)
            {
                return;
            }

            bool figureRoleActive = !roleAuthority.IsInkPainter;
            bool primaryToolAllowed =
                clarityState.CanUsePrimaryTool;
            bool shouldAllowLoadout =
                figureRoleActive && primaryToolAllowed;

            if (!shouldAllowLoadout)
            {
                if (toolLoadout.enabled)
                {
                    loadoutSuppressedByGate = true;
                    toolLoadout.enabled = false;
                }

                lastState = !figureRoleActive
                    ? "Blocked: Ink Painter role"
                    : $"Blocked: {clarityState.CurrentLevel} form";
                return;
            }

            if (loadoutSuppressedByGate && !toolLoadout.enabled)
            {
                toolLoadout.enabled = true;
            }

            loadoutSuppressedByGate = false;
            lastState =
                $"Allowed: Figure {clarityState.CurrentLevel}";
        }

        private void ResolveReferences()
        {
            if (clarityState == null)
            {
                clarityState =
                    GetComponentInParent<FigureClarityState>();
            }

            if (toolLoadout == null)
            {
                toolLoadout =
                    GetComponentInChildren<
                        FigureToolLoadoutController>(true);
            }

            if (roleAuthority == null)
            {
                roleAuthority =
                    InkPainterRoleAuthority.ActiveInstance;
            }

            if (roleAuthority == null)
            {
                roleAuthority =
                    Object.FindFirstObjectByType<
                        InkPainterRoleAuthority>(
                        FindObjectsInactive.Include);
            }
        }
    }
}
