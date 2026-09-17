using System.Collections;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Painters.World;
using PaintedAlive.UI.Telegraph;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    public enum LivingGalleryPainterActionKind
    {
        FrameGate,
        DryingPaper,
        MovingCanvas
    }

    /// <summary>
    /// Player-facing Painter adapter for authored Living Gallery mechanics.
    /// It does not invent Pigment route effects, trampoline commands or hard
    /// canvas collision. Only authored/implemented stateful mechanics are exposed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LivingGalleryPainterWorldInteraction : MonoBehaviour,
        ILivingGalleryResettable,
        INetworkReplicatedPainterWorldAction
    {
        [SerializeField] private string systemReference;
        [SerializeField] private string displayName;
        [SerializeField] private LivingGalleryPainterActionKind actionKind;
        [SerializeField] private RotatingFrameGate frameGate;
        [SerializeField] private DryingPaperSurface dryingPaper;
        [SerializeField] private MovingCanvasObstacle movingCanvas;
        [SerializeField] private Transform telegraphGeometry;
        [SerializeField, Min(0f)] private float telegraphSeconds = 1.5f;
        [SerializeField] private bool requirePainterRole = true;

        [Header("Runtime Feedback")]
        [SerializeField] private bool drawFallbackTelegraph = true;
        [SerializeField, Min(0.01f)] private float fallbackLineWidth = 0.065f;

        [Header("Runtime - Read Only")]
        [SerializeField] private bool activationInProgress;
        [SerializeField] private string lastResult = "Not used";
        [SerializeField] private string lastTargetState = "Unknown";
        [SerializeField] private int acceptedRequestCount;
        [SerializeField] private int blockedRequestCount;
        [SerializeField] private int committedRequestCount;
        [SerializeField] private int cancelledRequestCount;

        private LineRenderer fallbackLine;
        private Material fallbackLineMaterial;

        public string SystemReference => systemReference;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? systemReference
            : displayName;
        public float TelegraphSeconds => telegraphSeconds;
        public bool ActivationInProgress => activationInProgress;
        public string LastResult => lastResult;
        public Transform ActionTransform => transform;
        public LivingGalleryPainterActionKind ActionKind => actionKind;
        public string LastTargetState => lastTargetState;

        public void ConfigureFrameGate(
            string id,
            string label,
            RotatingFrameGate gate,
            Transform telegraph,
            float warningSeconds = 1.5f)
        {
            systemReference = id ?? string.Empty;
            displayName = label ?? id ?? string.Empty;
            actionKind = LivingGalleryPainterActionKind.FrameGate;
            frameGate = gate;
            dryingPaper = null;
            movingCanvas = null;
            telegraphGeometry = telegraph;
            telegraphSeconds = Mathf.Max(0f, warningSeconds);
        }

        public void ConfigureDryingPaper(
            string id,
            string label,
            DryingPaperSurface paper,
            Transform telegraph,
            float warningSeconds = 1.5f)
        {
            systemReference = id ?? string.Empty;
            displayName = label ?? id ?? string.Empty;
            actionKind = LivingGalleryPainterActionKind.DryingPaper;
            frameGate = null;
            dryingPaper = paper;
            movingCanvas = null;
            telegraphGeometry = telegraph;
            telegraphSeconds = Mathf.Max(0f, warningSeconds);
        }

        public void ConfigureMovingCanvas(
            string id,
            string label,
            MovingCanvasObstacle canvas,
            Transform telegraph,
            float warningSeconds = 1.0f)
        {
            systemReference = id ?? string.Empty;
            displayName = label ?? id ?? string.Empty;
            actionKind = LivingGalleryPainterActionKind.MovingCanvas;
            frameGate = null;
            dryingPaper = null;
            movingCanvas = canvas;
            telegraphGeometry = telegraph;
            telegraphSeconds = Mathf.Max(0f, warningSeconds);
        }

        [ContextMenu("Debug Request Activate")]
        private void DebugRequestActivate()
        {
            RequestActivate();
        }

        [ContextMenu("Debug Force Activate (Bypass Role, Keep Safety)")]
        private void DebugForceActivate()
        {
            RequestActivateInternal(ignoreRole: true);
        }

        public bool RequestActivate()
        {
            return RequestActivateInternal(ignoreRole: false);
        }

        public bool RequestActivateFromNetworkAuthority()
        {
            return RequestActivateInternal(ignoreRole: true);
        }

        private bool RequestActivateInternal(bool ignoreRole)
        {
            if (!Application.isPlaying)
                return Block("Play Mode gerekli");

            if (!isActiveAndEnabled)
                return Block("Interaction component aktif değil");

            if (activationInProgress)
                return Block("Telegraph zaten aktif");

            if (Time.timeScale <= 0.0001f)
                return Block("World paused (Time.timeScale=0)");

            if (!ignoreRole &&
                requirePainterRole &&
                !PrototypeRoleAuthorityResolver.IsPainter(out string role))
            {
                return Block("Local role Painter değil: " + role);
            }

            if (!CanTargetActivate(out string reason))
                return Block(reason);

            acceptedRequestCount++;
            lastTargetState = ReadTargetState();
            lastResult =
                $"Accepted • {DisplayName} • telegraph {telegraphSeconds:0.0}s";

            Debug.Log(
                "[M55.9.9 Living Gallery Interaction]\n" +
                $"System={systemReference}\n" +
                $"Kind={actionKind}\n" +
                "Request=ACCEPTED\n" +
                $"StateBefore={lastTargetState}\n" +
                $"Telegraph={telegraphSeconds:0.00}s\n" +
                $"IgnoreRole={ignoreRole}",
                this);

            StartCoroutine(TelegraphAndCommit(lastTargetState));
            return true;
        }

        private bool Block(string reason)
        {
            blockedRequestCount++;
            lastResult =
                $"Blocked • {DisplayName} • {reason}";

            Debug.LogWarning(
                "[M55.9.9 Living Gallery Interaction]\n" +
                $"System={systemReference}\n" +
                $"Kind={actionKind}\n" +
                "Request=BLOCKED\n" +
                $"Reason={reason}",
                this);

            return false;
        }

        private IEnumerator TelegraphAndCommit(string stateBefore)
        {
            activationInProgress = true;
            float startedAt = Time.unscaledTime;
            int sourceId = GetInstanceID();
            Vector3[] points = BuildTelegraphPoints();

            ShowFallbackTelegraph(points);

            while (Time.unscaledTime - startedAt < telegraphSeconds)
            {
                float elapsed = Time.unscaledTime - startedAt;
                float progress = telegraphSeconds <= 0.001f
                    ? 1f
                    : Mathf.Clamp01(elapsed / telegraphSeconds);

                PrototypePainterTelegraphHub.Publish(
                    new PrototypePainterTelegraphSignal(
                        sourceId,
                        points,
                        "LivingGallery/" + systemReference,
                        PrototypePainterTelegraphPhase.Armed,
                        progress,
                        Mathf.Max(0f, telegraphSeconds - elapsed),
                        true,
                        0f,
                        startedAt,
                        Time.unscaledTime));

                yield return null;
            }

            if (!CanTargetActivate(out string reason))
            {
                CancelCommit(
                    sourceId,
                    points,
                    reason,
                    "Safety/state changed during telegraph");
                yield break;
            }

            bool committed = TryCommitTarget();
            HideFallbackTelegraph();

            if (!committed)
            {
                CancelCommit(
                    sourceId,
                    points,
                    string.IsNullOrWhiteSpace(reason)
                        ? "Target rejected activation"
                        : reason,
                    "Target rejected activation");
                yield break;
            }

            committedRequestCount++;
            yield return null;

            lastTargetState = ReadTargetState();
            lastResult =
                $"Committed • {DisplayName} • " +
                $"{stateBefore} → {lastTargetState}";

            PrototypePainterTelegraphHub.Resolve(
                new PrototypePainterTelegraphOutcome(
                    sourceId,
                    points,
                    "LivingGallery/" + systemReference,
                    PrototypePainterTelegraphResolution.Committed,
                    lastResult,
                    Time.unscaledTime));

            Debug.Log(
                "[M55.9.9 Living Gallery Interaction]\n" +
                $"System={systemReference}\n" +
                "Commit=TRUE\n" +
                $"StateBefore={stateBefore}\n" +
                $"StateAfterStart={lastTargetState}",
                this);

            activationInProgress = false;
            StartCoroutine(MonitorCommittedTarget());
        }

        private void CancelCommit(
            int sourceId,
            Vector3[] points,
            string reason,
            string logReason)
        {
            cancelledRequestCount++;
            lastResult =
                $"Cancelled • {DisplayName} • {reason}";

            HideFallbackTelegraph();

            PrototypePainterTelegraphHub.Resolve(
                new PrototypePainterTelegraphOutcome(
                    sourceId,
                    points,
                    "LivingGallery/" + systemReference,
                    PrototypePainterTelegraphResolution.Cancelled,
                    lastResult,
                    Time.unscaledTime));

            Debug.LogWarning(
                "[M55.9.9 Living Gallery Interaction]\n" +
                $"System={systemReference}\n" +
                "Commit=CANCELLED\n" +
                $"Reason={logReason}: {reason}",
                this);

            activationInProgress = false;
        }

        private IEnumerator MonitorCommittedTarget()
        {
            float deadline = Time.unscaledTime + 4f;

            while (Time.unscaledTime < deadline && IsTargetTransitioning())
                yield return null;

            lastTargetState = ReadTargetState();

            if (IsTargetTransitioning())
            {
                lastResult =
                    $"Warning • {DisplayName} hâlâ {lastTargetState}";

                Debug.LogWarning(
                    "[M55.9.9 Living Gallery Mechanic Watch]\n" +
                    lastResult,
                    this);
            }
            else
            {
                lastResult =
                    $"{DisplayName} state → {lastTargetState}";

                Debug.Log(
                    "[M55.9.9 Living Gallery Mechanic Watch]\n" +
                    lastResult,
                    this);
            }
        }

        private bool CanTargetActivate(out string reason)
        {
            switch (actionKind)
            {
                case LivingGalleryPainterActionKind.FrameGate:
                    if (frameGate == null)
                    {
                        reason = "Frame Gate reference NULL";
                        return false;
                    }
                    return frameGate.CanPainterToggle(out reason);

                case LivingGalleryPainterActionKind.DryingPaper:
                    if (dryingPaper == null)
                    {
                        reason = "Drying Paper reference NULL";
                        return false;
                    }
                    return dryingPaper.CanPainterToggle(out reason);

                case LivingGalleryPainterActionKind.MovingCanvas:
                    if (movingCanvas == null)
                    {
                        reason = "Moving Canvas reference NULL";
                        return false;
                    }
                    return movingCanvas.CanPainterCycle(out reason);

                default:
                    reason = "Unsupported Living Gallery action kind";
                    return false;
            }
        }

        private bool TryCommitTarget()
        {
            switch (actionKind)
            {
                case LivingGalleryPainterActionKind.FrameGate:
                    return frameGate != null &&
                        frameGate.TryPainterToggleAfterExternalTelegraph();

                case LivingGalleryPainterActionKind.DryingPaper:
                    return dryingPaper != null &&
                        dryingPaper.TryPainterToggleAfterExternalTelegraph();

                case LivingGalleryPainterActionKind.MovingCanvas:
                    return movingCanvas != null &&
                        movingCanvas.TryPainterCycleAfterExternalTelegraph();

                default:
                    return false;
            }
        }

        private bool IsTargetTransitioning()
        {
            switch (actionKind)
            {
                case LivingGalleryPainterActionKind.FrameGate:
                    return frameGate != null && frameGate.IsTransitioning;

                case LivingGalleryPainterActionKind.DryingPaper:
                    return dryingPaper != null &&
                        dryingPaper.State == DryingPaperState.Transitioning;

                case LivingGalleryPainterActionKind.MovingCanvas:
                    return movingCanvas != null && movingCanvas.IsMoving;

                default:
                    return false;
            }
        }

        private string ReadTargetState()
        {
            switch (actionKind)
            {
                case LivingGalleryPainterActionKind.FrameGate:
                    return frameGate != null
                        ? frameGate.StateLabel
                        : "MISSING";

                case LivingGalleryPainterActionKind.DryingPaper:
                    return dryingPaper != null
                        ? dryingPaper.StateLabel
                        : "MISSING";

                case LivingGalleryPainterActionKind.MovingCanvas:
                    return movingCanvas != null
                        ? movingCanvas.StateLabel
                        : "MISSING";

                default:
                    return "UNKNOWN";
            }
        }

        private Vector3[] BuildTelegraphPoints()
        {
            Vector3 center = telegraphGeometry != null
                ? telegraphGeometry.position
                : transform.position;

            Transform basis = telegraphGeometry != null
                ? telegraphGeometry
                : transform;

            float radius = actionKind == LivingGalleryPainterActionKind.FrameGate
                ? 1.4f
                : actionKind == LivingGalleryPainterActionKind.DryingPaper
                    ? 0.9f
                    : 0.8f;

            Vector3 right = basis.right * radius;
            Vector3 forward = basis.forward * radius;
            Vector3 lift = Vector3.up * 0.08f;

            return new[]
            {
                center - right + lift,
                center + forward + lift,
                center + right + lift,
                center - forward + lift,
                center - right + lift
            };
        }

        private void ShowFallbackTelegraph(Vector3[] points)
        {
            if (!drawFallbackTelegraph || points == null || points.Length < 2)
                return;

            EnsureFallbackLine();
            if (fallbackLine == null)
                return;

            fallbackLine.positionCount = points.Length;
            fallbackLine.SetPositions(points);
            fallbackLine.enabled = true;
        }

        private void HideFallbackTelegraph()
        {
            if (fallbackLine != null)
                fallbackLine.enabled = false;
        }

        private void EnsureFallbackLine()
        {
            if (fallbackLine != null)
                return;

            GameObject child =
                new GameObject("__LivingGalleryTelegraphFallback");
            child.transform.SetParent(transform, false);

            fallbackLine = child.AddComponent<LineRenderer>();
            fallbackLine.useWorldSpace = true;
            fallbackLine.loop = false;
            fallbackLine.widthMultiplier = fallbackLineWidth;
            fallbackLine.numCapVertices = 3;
            fallbackLine.numCornerVertices = 3;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                fallbackLineMaterial = new Material(shader);
                fallbackLineMaterial.name =
                    "Runtime_LivingGalleryTelegraph_Unlit";
                fallbackLine.material = fallbackLineMaterial;
            }

            Color color = new Color(0.1f, 0.85f, 1f, 0.98f);
            fallbackLine.startColor = color;
            fallbackLine.endColor = color;
            fallbackLine.enabled = false;
        }

        public void ResetLivingGalleryState()
        {
            StopAllCoroutines();
            activationInProgress = false;
            lastResult = "Reset";
            lastTargetState = ReadTargetState();
            HideFallbackTelegraph();
        }

        private void OnDestroy()
        {
            if (fallbackLineMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(fallbackLineMaterial);
            else
                DestroyImmediate(fallbackLineMaterial);
        }
    }
}
