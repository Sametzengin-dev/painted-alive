using System.Collections;
using PaintedAlive.UI.UnifiedHUD;
using UnityEngine;

namespace PaintedAlive.UI.Telegraph
{
    [DisallowMultipleComponent]
    public sealed class PrototypePainterTelegraphDebugDriver :
        MonoBehaviour
    {
        [SerializeField] private Transform worldAnchor;
        [SerializeField] private PrototypeUnifiedHudController unifiedHudController;

        [Header("Runtime Read Only")]
        [SerializeField] private bool selfTestRunning;
        [SerializeField] private int completedSelfTestCount;

        private Coroutine runningRoutine;

        public bool SelfTestRunning => selfTestRunning;
        public int CompletedSelfTestCount => completedSelfTestCount;

        public void Configure(
            Transform configuredWorldAnchor,
            PrototypeUnifiedHudController configuredHudController)
        {
            worldAnchor = configuredWorldAnchor;
            unifiedHudController = configuredHudController;
        }

        public void RunSelfTest()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
            }

            runningRoutine =
                StartCoroutine(SelfTestRoutine());
        }

        private IEnumerator SelfTestRoutine()
        {
            selfTestRunning = true;

            int sourceId =
                -Mathf.Abs(GetInstanceID()) - 4400;

            Vector3 origin =
                worldAnchor != null
                    ? worldAnchor.position
                    : transform.position;

            Vector3 forward =
                worldAnchor != null
                    ? worldAnchor.forward
                    : Vector3.forward;

            Vector3 right =
                worldAnchor != null
                    ? worldAnchor.right
                    : Vector3.right;

            Vector3[] wallPoints =
            {
                origin + forward * 2.2f - right * 1.6f,
                origin + forward * 2.4f - right * 0.8f,
                origin + forward * 2.5f,
                origin + forward * 2.4f + right * 0.8f,
                origin + forward * 2.2f + right * 1.6f
            };

            float startedAt = Time.time;
            const float duration = 1.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress =
                    Mathf.Clamp01(elapsed / duration);

                PrototypePainterTelegraphPhase phase =
                    progress >= 0.999f
                        ? PrototypePainterTelegraphPhase.Armed
                        : PrototypePainterTelegraphPhase.Drafting;

                PrototypePainterTelegraphHub.Publish(
                    new PrototypePainterTelegraphSignal(
                        sourceId,
                        wallPoints,
                        "Wall",
                        phase,
                        progress,
                        Mathf.Max(0f, duration - elapsed),
                        true,
                        12f,
                        startedAt,
                        Time.time));

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.35f);

            PrototypePainterTelegraphHub.Resolve(
                new PrototypePainterTelegraphOutcome(
                    sourceId,
                    wallPoints,
                    "Wall",
                    PrototypePainterTelegraphResolution.Committed,
                    "SelfTestCommit",
                    Time.time));

            yield return new WaitForSecondsRealtime(1.65f);

            Vector3[] rampPoints =
            {
                origin + forward * 1.8f - right * 1.2f,
                origin + forward * 2.1f - right * 0.4f +
                    Vector3.up * 0.15f,
                origin + forward * 2.4f + right * 0.4f +
                    Vector3.up * 0.35f,
                origin + forward * 2.8f + right * 1.2f +
                    Vector3.up * 0.55f
            };

            PrototypePainterTelegraphHub.Publish(
                new PrototypePainterTelegraphSignal(
                    sourceId - 1,
                    rampPoints,
                    "Ramp",
                    PrototypePainterTelegraphPhase.Blocked,
                    0.62f,
                    0.6f,
                    false,
                    30f,
                    Time.time,
                    Time.time));

            yield return new WaitForSecondsRealtime(0.8f);

            PrototypePainterTelegraphHub.Resolve(
                new PrototypePainterTelegraphOutcome(
                    sourceId - 1,
                    rampPoints,
                    "Ramp",
                    PrototypePainterTelegraphResolution.Cancelled,
                    "PigmentOrBudgetBlocked",
                    Time.time));

            yield return new WaitForSecondsRealtime(1.45f);

            completedSelfTestCount++;
            selfTestRunning = false;
            runningRoutine = null;
        }

        private void OnDisable()
        {
            if (runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
                runningRoutine = null;
            }

            selfTestRunning = false;
        }
    }
}
