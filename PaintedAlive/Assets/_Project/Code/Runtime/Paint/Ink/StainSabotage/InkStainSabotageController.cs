using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.Counterplay;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Ink.StainSabotage
{
    [DefaultExecutionOrder(13000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class InkStainSabotageController : MonoBehaviour
    {
        private const int MaximumTargetHits = 24;

        private readonly RaycastHit[] targetHits =
            new RaycastHit[MaximumTargetHits];

        [Header("References")]
        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private Camera figureCamera;

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkStainSabotageConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private InkCreatureRuntime currentTarget;

        [SerializeField, Range(0f, 1f)]
        private float holdProgress;

        [SerializeField]
        private int successfulSabotages;

        [SerializeField]
        private string lastResult =
            "Become a Stain to sabotage small Ink creatures";

        private float nextUseTime;

        public InkCreatureRuntime CurrentTarget => currentTarget;
        public float HoldProgress => holdProgress;
        public int SuccessfulSabotages => successfulSabotages;
        public string LastResult => lastResult;
        public bool IsStainRoleActive =>
            roleAuthority != null &&
            !roleAuthority.IsInkPainter &&
            clarityState != null &&
            clarityState.CurrentLevel == FigureClarityLevel.Stain;

        private void Awake()
        {
            if (clarityState == null)
            {
                clarityState = GetComponent<FigureClarityState>();
            }

            if (figureMotor == null)
            {
                figureMotor = GetComponent<FigureMotor>();
            }

            if (figureCamera == null)
            {
                figureCamera = GetComponentInChildren<Camera>(true);
            }

            if (clarityState == null ||
                figureMotor == null ||
                figureCamera == null ||
                roleAuthority == null ||
                config == null)
            {
                Debug.LogError(
                    "InkStainSabotageController references are incomplete. " +
                    "Run M25 Setup again.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!IsStainRoleActive || IsEditingText())
            {
                ResetInteraction(
                    "Stain sabotage available only in Figure Stain form");
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || !keyboard.eKey.isPressed)
            {
                currentTarget = FindBestTarget();
                holdProgress = 0f;
                lastResult = currentTarget != null
                    ? $"Hold E: scramble {GetTargetLabel(currentTarget)}"
                    : "Aim at a small Ink creature and hold E";
                return;
            }

            if (Time.unscaledTime < nextUseTime)
            {
                holdProgress = 0f;
                lastResult = "Stain signal recovering";
                return;
            }

            InkCreatureRuntime candidate = FindBestTarget();

            if (candidate == null)
            {
                ResetInteraction(
                    "No vulnerable small Ink creature in range");
                return;
            }

            if (candidate != currentTarget)
            {
                currentTarget = candidate;
                holdProgress = 0f;
            }

            holdProgress = Mathf.Clamp01(
                holdProgress +
                Time.unscaledDeltaTime /
                Mathf.Max(0.1f, config.HoldDuration));
            lastResult =
                $"Scrambling {GetTargetLabel(currentTarget)} " +
                $"{holdProgress * 100f:0}%";

            if (holdProgress >= 1f)
            {
                TryCompleteSabotage();
            }
        }

        public void Configure(
            FigureClarityState targetClarityState,
            FigureMotor targetFigureMotor,
            Camera targetCamera,
            InkPainterRoleAuthority authority,
            InkStainSabotageConfig sabotageConfig)
        {
            clarityState = targetClarityState;
            figureMotor = targetFigureMotor;
            figureCamera = targetCamera;
            roleAuthority = authority;
            config = sabotageConfig;
        }

        private void TryCompleteSabotage()
        {
            InkCreatureRuntime target = currentTarget;
            holdProgress = 0f;

            if (!IsValidTarget(target))
            {
                lastResult = "Target became immune before sabotage completed";
                currentTarget = null;
                return;
            }

            InkStainSabotageStatus status =
                target.GetComponent<InkStainSabotageStatus>();

            if (status == null)
            {
                status =
                    target.gameObject.AddComponent<
                        InkStainSabotageStatus>();
            }

            status.Configure(config);

            if (!status.Apply(transform))
            {
                lastResult = "Creature resisted the sabotage";
                currentTarget = null;
                return;
            }

            successfulSabotages++;
            nextUseTime =
                Time.unscaledTime + config.ReuseCooldown;
            lastResult =
                $"{GetTargetLabel(target)} signal scrambled";
            currentTarget = null;
        }

        private InkCreatureRuntime FindBestTarget()
        {
            if (figureCamera == null || config == null)
            {
                return null;
            }

            Transform cameraTransform = figureCamera.transform;
            int hitCount = Physics.SphereCastNonAlloc(
                cameraTransform.position,
                config.AimAssistRadius,
                cameraTransform.forward,
                targetHits,
                config.InteractionRange,
                config.TargetMask,
                QueryTriggerInteraction.Collide);
            InkCreatureRuntime best = null;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = targetHits[i];
                Collider hitCollider = hit.collider;
                InkCreatureRuntime candidate =
                    hitCollider != null
                        ? hitCollider.GetComponentInParent<
                            InkCreatureRuntime>()
                        : null;

                if (!IsValidTarget(candidate) ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                best = candidate;
                nearestDistance = hit.distance;
            }

            return best;
        }

        private bool IsValidTarget(InkCreatureRuntime candidate)
        {
            if (candidate == null ||
                !candidate.gameObject.activeInHierarchy ||
                !candidate.IsInitialized ||
                candidate.IsFixed ||
                candidate.IsPinned)
            {
                return false;
            }

            int complexity =
                InkGlyphComplexityUtility.GetCreatureCost(
                    candidate,
                    config != null
                        ? config.MaximumComplexity + 1
                        : 4);

            if (config == null ||
                complexity > config.MaximumComplexity)
            {
                return false;
            }

            InkStainSabotageStatus sabotage =
                candidate.GetComponent<InkStainSabotageStatus>();

            if (sabotage != null && sabotage.IsSabotaged)
            {
                return false;
            }

            InkCommandDisruptionStatus disruption =
                candidate.GetComponent<InkCommandDisruptionStatus>();

            return disruption == null || !disruption.IsDisrupted;
        }

        private static string GetTargetLabel(
            InkCreatureRuntime target)
        {
            if (target == null)
            {
                return "Ink creature";
            }

            if (target.HasGlyph(InkGlyphType.BrokenLine))
            {
                return "Kesik Avcı";
            }

            if (target.HasGlyph(InkGlyphType.Shell))
            {
                return "Kabuklu";
            }

            return "Lekebacak";
        }

        private void ResetInteraction(string reason)
        {
            currentTarget = null;
            holdProgress = 0f;
            lastResult = reason;
        }

        private static bool IsEditingText()
        {
            GameObject selected =
                EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject
                    : null;

            return selected != null &&
                (selected.GetComponent("TMP_InputField") != null ||
                 selected.GetComponent("InputField") != null);
        }
    }
}
