using System;
using System.Collections;
using System.Text;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.UI.Telegraph;
using PaintedAlive.Painters.World;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    /// <summary>
    /// Player-facing Palimpsest Painter interaction adapter.
    /// M55.9.7 keeps the existing telegraph/authority contract but makes every
    /// accepted, blocked and committed request visible and diagnosable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PalimpsestPainterWorldInteraction : MonoBehaviour,
        IPalimpsestResettable,
        INetworkReplicatedPainterWorldAction
    {
        [SerializeField] private string systemReference;
        [SerializeField] private MonoBehaviour[] targets;
        [SerializeField] private Transform telegraphGeometry;
        [SerializeField, Min(0f)] private float telegraphSeconds = 1.25f;
        [SerializeField] private bool requirePainterRole = true;

        [Header("Runtime Feedback")]
        [SerializeField] private bool drawFallbackTelegraph = true;
        [SerializeField, Min(0.01f)] private float fallbackLineWidth = 0.065f;
        [SerializeField, Min(0.25f)] private float feedbackHoldSeconds = 3.25f;

        [Header("Runtime - Read Only")]
        [SerializeField] private bool activationInProgress;
        [SerializeField] private string lastResult = "Not used";
        [SerializeField] private string lastResolvedTarget = "None";
        [SerializeField] private string lastTargetState = "Unknown";
        [SerializeField] private int acceptedRequestCount;
        [SerializeField] private int blockedRequestCount;
        [SerializeField] private int committedRequestCount;
        [SerializeField] private int cancelledRequestCount;

        private LineRenderer fallbackLine;
        private Material fallbackLineMaterial;

        private static int feedbackOwnerId;
        private static string globalFeedback = string.Empty;
        private static bool globalFeedbackSuccess;
        private static float globalFeedbackUntil;

        public string SystemReference => systemReference;
        public string DisplayName => FormatDisplayName(systemReference);
        public Transform ActionTransform => transform;
        public float TelegraphSeconds => telegraphSeconds;
        public bool ActivationInProgress => activationInProgress;
        public string LastResult => lastResult;
        public string LastResolvedTarget => lastResolvedTarget;
        public string LastTargetState => lastTargetState;
        public int ConfiguredTargetCount => targets != null ? targets.Length : 0;
        public Transform TelegraphGeometry => telegraphGeometry;

        public void Configure(
            string systemId,
            MonoBehaviour[] configuredTargets,
            Transform geometry,
            float authoredTelegraphSeconds,
            bool painterRoleRequired = true)
        {
            systemReference = systemId ?? string.Empty;
            targets = configuredTargets ?? Array.Empty<MonoBehaviour>();
            telegraphGeometry = geometry;
            telegraphSeconds = Mathf.Max(0f, authoredTelegraphSeconds);
            requirePainterRole = painterRoleRequired;
        }

        [ContextMenu("Debug Request Activate")]
        public void DebugRequestActivate()
        {
            RequestActivate();
        }

        [ContextMenu("Debug Force Activate (Bypass Role, Keep Safety)")]
        public void DebugForceActivate()
        {
            RequestActivateInternal(ignoreRole: true);
        }

        [ContextMenu("Debug Print Binding State")]
        public void DebugPrintBindingState()
        {
            var builder = new StringBuilder();
            builder.AppendLine("[M55.9.7 Palimpsest Interaction Binding]");
            builder.AppendLine("System=" + systemReference);
            builder.AppendLine("GameObject=" + name);
            builder.AppendLine("Active=" + isActiveAndEnabled);
            builder.AppendLine("TelegraphSeconds=" + telegraphSeconds.ToString("0.00"));
            builder.AppendLine("Targets=" + ConfiguredTargetCount);

            if (targets != null)
            {
                for (int index = 0; index < targets.Length; index++)
                {
                    MonoBehaviour behaviour = targets[index];
                    if (behaviour == null)
                    {
                        builder.AppendLine($"Target[{index}]=NULL");
                        continue;
                    }

                    if (behaviour is IPalimpsestPainterActivatable candidate)
                    {
                        bool can = candidate.CanPainterActivate(out string reason);
                        builder.AppendLine(
                            $"Target[{index}]={behaviour.GetType().Name} " +
                            $"System={candidate.PalimpsestSystemId} CanActivate={can} " +
                            $"Reason={(string.IsNullOrWhiteSpace(reason) ? "OK" : reason)} " +
                            $"State={ReadPersistentState(candidate)}");
                    }
                    else
                    {
                        builder.AppendLine(
                            $"Target[{index}]={behaviour.GetType().Name} " +
                            "(does not implement IPalimpsestPainterActivatable)");
                    }
                }
            }

            string role = "Unknown";
            bool painter = PrototypeRoleAuthorityResolver.IsPainter(out role);
            builder.AppendLine($"ResolvedRole={role} IsPainter={painter}");
            builder.AppendLine($"TimeScale={Time.timeScale:0.###}");
            Debug.Log(builder.ToString(), this);

            SetFeedback(
                "Binding state Unity Console'a yazıldı • " + systemReference,
                true);
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
            {
                return Block(
                    "Play Mode gerekli. Debug activation Edit Mode'da çalıştırılmaz.");
            }

            if (!isActiveAndEnabled)
            {
                return Block("Interaction component aktif değil.");
            }

            if (activationInProgress)
            {
                return Block("Telegraph zaten aktif.");
            }

            if (Time.timeScale <= 0.0001f)
            {
                return Block(
                    "World paused (Time.timeScale=0). Mekanik hareketi ilerleyemez.");
            }

            if (!ignoreRole &&
                requirePainterRole &&
                !PrototypeRoleAuthorityResolver.IsPainter(out string role))
            {
                return Block("Local role Painter değil: " + role);
            }

            IPalimpsestPainterActivatable target =
                ResolveTarget(out string reason);

            if (target == null)
            {
                return Block(reason);
            }

            acceptedRequestCount++;
            lastResolvedTarget = target.GetType().Name;
            lastTargetState = ReadPersistentState(target);
            lastResult =
                $"Accepted • {systemReference} • telegraph {telegraphSeconds:0.0}s";

            SetFeedback(lastResult, true);
            Debug.Log(
                "[M55.9.7 Palimpsest Interaction]\n" +
                $"System={systemReference}\n" +
                $"Request=ACCEPTED\n" +
                $"Target={lastResolvedTarget}\n" +
                $"StateBefore={lastTargetState}\n" +
                $"Telegraph={telegraphSeconds:0.00}s\n" +
                $"IgnoreRole={ignoreRole}\n" +
                $"TimeScale={Time.timeScale:0.###}",
                this);

            StartCoroutine(TelegraphAndCommit(target, lastTargetState));
            return true;
        }

        private bool Block(string reason)
        {
            blockedRequestCount++;
            lastResult = "Blocked • " + systemReference + " • " + reason;
            SetFeedback(lastResult, false);

            Debug.LogWarning(
                "[M55.9.7 Palimpsest Interaction]\n" +
                $"System={systemReference}\n" +
                "Request=BLOCKED\n" +
                $"Reason={reason}\n" +
                $"ResolvedRole={PrototypeRoleAuthorityResolver.LastResolvedRole}\n" +
                $"TimeScale={Time.timeScale:0.###}",
                this);

            return false;
        }

        private IEnumerator TelegraphAndCommit(
            IPalimpsestPainterActivatable target,
            string stateBefore)
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
                        "Palimpsest/" + systemReference,
                        PrototypePainterTelegraphPhase.Armed,
                        progress,
                        Mathf.Max(0f, telegraphSeconds - elapsed),
                        true,
                        0f,
                        startedAt,
                        Time.unscaledTime));

                yield return null;
            }

            bool canCommit =
                target.CanPainterActivate(out string reason);

            bool committed =
                canCommit &&
                target.TryPainterActivate();

            HideFallbackTelegraph();

            if (!committed)
            {
                cancelledRequestCount++;
                lastResult =
                    "Cancelled • " + systemReference + " • " +
                    (string.IsNullOrWhiteSpace(reason)
                        ? "Target rejected activation"
                        : reason);

                PrototypePainterTelegraphHub.Resolve(
                    new PrototypePainterTelegraphOutcome(
                        sourceId,
                        points,
                        "Palimpsest/" + systemReference,
                        PrototypePainterTelegraphResolution.Cancelled,
                        lastResult,
                        Time.unscaledTime));

                SetFeedback(lastResult, false);
                Debug.LogWarning(
                    "[M55.9.7 Palimpsest Interaction]\n" +
                    $"System={systemReference}\n" +
                    "Commit=CANCELLED\n" +
                    $"Reason={lastResult}\n" +
                    $"State={ReadPersistentState(target)}",
                    this);

                activationInProgress = false;
                yield break;
            }

            committedRequestCount++;

            // Let the target's own coroutine enter TRANSITIONING/MOVING/ALIGNING.
            yield return null;

            string transitionState = ReadPersistentState(target);
            lastTargetState = transitionState;
            lastResult =
                $"Committed • {systemReference} • " +
                $"{stateBefore} → {transitionState}";

            PrototypePainterTelegraphHub.Resolve(
                new PrototypePainterTelegraphOutcome(
                    sourceId,
                    points,
                    "Palimpsest/" + systemReference,
                    PrototypePainterTelegraphResolution.Committed,
                    lastResult,
                    Time.unscaledTime));

            SetFeedback(lastResult, true);
            Debug.Log(
                "[M55.9.7 Palimpsest Interaction]\n" +
                $"System={systemReference}\n" +
                "Commit=TRUE\n" +
                $"Target={target.GetType().Name}\n" +
                $"StateBefore={stateBefore}\n" +
                $"StateAfterStart={transitionState}",
                this);

            activationInProgress = false;
            StartCoroutine(MonitorCommittedTarget(target, transitionState));
        }

        private IEnumerator MonitorCommittedTarget(
            IPalimpsestPainterActivatable target,
            string transitionState)
        {
            float deadline = Time.unscaledTime + 3.25f;
            string observed = transitionState;

            while (Time.unscaledTime < deadline)
            {
                observed = ReadPersistentState(target);

                if (!IsTransitionState(observed))
                    break;

                yield return null;
            }

            lastTargetState = observed;

            if (IsTransitionState(observed))
            {
                string warning =
                    $"{systemReference} kabul edildi ama 3.25s sonra hâlâ " +
                    $"{observed}. Figure clearance veya TimeScale kontrol et.";

                SetFeedback(warning, false);
                Debug.LogWarning(
                    "[M55.9.7 Palimpsest Mechanic Watch]\n" + warning,
                    this);
            }
            else
            {
                string success =
                    $"{systemReference} state → {observed}";

                SetFeedback(success, true);
                Debug.Log(
                    "[M55.9.7 Palimpsest Mechanic Watch]\n" + success,
                    this);
            }
        }

        private IPalimpsestPainterActivatable ResolveTarget(out string reason)
        {
            reason = "Configured target yok veya tüm target'lar activation'ı reddetti.";

            if (targets == null || targets.Length == 0)
            {
                reason = "Configured target array boş.";
                return null;
            }

            foreach (MonoBehaviour behaviour in targets)
            {
                if (behaviour == null)
                {
                    reason = "Configured target reference NULL.";
                    continue;
                }

                if (!(behaviour is IPalimpsestPainterActivatable candidate))
                {
                    reason =
                        behaviour.GetType().Name +
                        " IPalimpsestPainterActivatable değil.";
                    continue;
                }

                if (candidate.CanPainterActivate(out string candidateReason))
                {
                    lastResolvedTarget = behaviour.GetType().Name;
                    lastTargetState = ReadPersistentState(candidate);
                    return candidate;
                }

                reason =
                    $"{behaviour.GetType().Name}: " +
                    (string.IsNullOrWhiteSpace(candidateReason)
                        ? "CanPainterActivate=false"
                        : candidateReason);
            }

            return null;
        }

        private Vector3[] BuildTelegraphPoints()
        {
            BoxCollider box = telegraphGeometry != null
                ? telegraphGeometry.GetComponent<BoxCollider>()
                : null;

            if (box != null)
            {
                Bounds bounds = box.bounds;
                float y = bounds.max.y + 0.08f;
                return new[]
                {
                    new Vector3(bounds.min.x, y, bounds.min.z),
                    new Vector3(bounds.max.x, y, bounds.min.z),
                    new Vector3(bounds.max.x, y, bounds.max.z),
                    new Vector3(bounds.min.x, y, bounds.max.z),
                    new Vector3(bounds.min.x, y, bounds.min.z)
                };
            }

            Vector3 center =
                telegraphGeometry != null
                    ? telegraphGeometry.position
                    : transform.position;

            Vector3 right =
                (telegraphGeometry != null
                    ? telegraphGeometry.right
                    : transform.right) * 1.0f;

            Vector3 forward =
                (telegraphGeometry != null
                    ? telegraphGeometry.forward
                    : transform.forward) * 1.0f;

            return new[]
            {
                center - right,
                center + forward,
                center + right,
                center - forward,
                center - right
            };
        }

        private void ShowFallbackTelegraph(Vector3[] points)
        {
            if (!drawFallbackTelegraph ||
                points == null ||
                points.Length < 2)
            {
                return;
            }

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
                new GameObject("__PalimpsestTelegraphFallback");

            child.transform.SetParent(transform, false);

            fallbackLine = child.AddComponent<LineRenderer>();
            fallbackLine.useWorldSpace = true;
            fallbackLine.loop = false;
            fallbackLine.widthMultiplier = fallbackLineWidth;
            fallbackLine.numCapVertices = 3;
            fallbackLine.numCornerVertices = 3;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                fallbackLineMaterial = new Material(shader);
                fallbackLineMaterial.name =
                    "Runtime_PalimpsestTelegraph_Unlit";
                fallbackLine.material = fallbackLineMaterial;
            }

            Color color = new Color(1f, 0.55f, 0.08f, 0.98f);
            fallbackLine.startColor = color;
            fallbackLine.endColor = color;
            fallbackLine.enabled = false;
        }

        private static string ReadPersistentState(
            IPalimpsestPainterActivatable target)
        {
            if (target is IPalimpsestPersistentState persistent)
            {
                return
                    $"{persistent.PersistentStateName} " +
                    $"({persistent.PersistentProgress01:0.00})";
            }

            return "No persistent-state contract";
        }

        private static string FormatDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Palimpsest Mekaniği";

            return value
                .Replace("FOLD_A", "Fold A")
                .Replace("GREAT_FOLD", "Great Fold")
                .Replace("UNDERPAINT_A", "Underpaint A")
                .Replace("UNDERPAINT_B", "Underpaint B")
                .Replace("UNDERPAINT", "Underpaint")
                .Replace("SMEAR_A", "Smear A")
                .Replace("SMEAR_B", "Smear B")
                .Replace("PERSPECTIVE", "Perspective")
                .Replace("_", " ");
        }

        private static bool IsTransitionState(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return
                value.IndexOf("TRANSITION", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("MOVING", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("ALIGNING", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetFeedback(string message, bool success)
        {
            feedbackOwnerId = GetInstanceID();
            globalFeedback = message ?? string.Empty;
            globalFeedbackSuccess = success;
            globalFeedbackUntil =
                Time.unscaledTime + feedbackHoldSeconds;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying ||
                feedbackOwnerId != GetInstanceID() ||
                Time.unscaledTime > globalFeedbackUntil ||
                string.IsNullOrWhiteSpace(globalFeedback))
            {
                return;
            }

            float scale = Mathf.Clamp(
                Screen.height / 900f,
                0.8f,
                1.35f);

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal =
                {
                    textColor = globalFeedbackSuccess
                        ? new Color(0.78f, 1f, 0.78f, 1f)
                        : new Color(1f, 0.72f, 0.64f, 1f)
                }
            };

            GUI.Box(
                new Rect(
                    Screen.width * 0.5f - 260f * scale,
                    Screen.height - 152f * scale,
                    520f * scale,
                    54f * scale),
                globalFeedback,
                style);
        }

        public void ResetPalimpsestState()
        {
            StopAllCoroutines();
            activationInProgress = false;
            lastResult = "Reset";
            lastTargetState = "Reset";
            HideFallbackTelegraph();
        }

        private void OnDestroy()
        {
            if (fallbackLineMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(fallbackLineMaterial);
                else
                    DestroyImmediate(fallbackLineMaterial);
            }
        }
    }
}
