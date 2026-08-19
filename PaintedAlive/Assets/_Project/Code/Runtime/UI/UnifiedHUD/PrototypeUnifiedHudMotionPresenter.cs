using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.UI.UnifiedHUD
{
    /// <summary>
    /// Cosmetic-only presentation layer for the M43 unified HUD.
    ///
    /// This implementation intentionally has no compile-time dependency on
    /// PrimeTween or another tween package. PrimeTween may remain installed,
    /// but the project still compiles when its assembly is unavailable.
    ///
    /// The component only modifies UI RectTransform and Text presentation.
    /// It never writes gameplay state, match flow, role, clarity, pigment,
    /// score, progress, encounter state, camera state, or network state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeUnifiedHudMotionPresenter : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private PrototypeUnifiedHudController snapshotSource;

        [Header("Animated Panels")]
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform encounterStrip;
        [SerializeField] private RectTransform progressPanel;
        [SerializeField] private RectTransform figurePanel;
        [SerializeField] private RectTransform painterPanel;
        [SerializeField] private RectTransform contextPanel;

        [Header("Animated Labels")]
        [SerializeField] private Text roleText;
        [SerializeField] private Text matchStateText;

        [Header("Accessibility")]
        [SerializeField] private bool motionEnabled = true;
        [SerializeField, Range(0f, 1f)] private float motionIntensity = 0.75f;

        [Header("Thresholds")]
        [SerializeField, Range(0f, 1f)] private float clarityCriticalThreshold = 0.25f;
        [SerializeField, Range(0f, 1f)] private float pigmentCriticalThreshold = 0.15f;
        [SerializeField, Min(0.001f)] private float progressPulseMinimumGain = 0.02f;
        [SerializeField, Min(0.25f)] private float repeatedCriticalPulseInterval = 1.20f;

        [Header("Sampling")]
        [SerializeField, Min(0.02f)] private float sampleInterval = 0.10f;

        [Header("Runtime Read Only")]
        [SerializeField] private bool configured;
        [SerializeField] private string lastObservedRole = string.Empty;
        [SerializeField] private string lastObservedMatchState = string.Empty;
        [SerializeField] private bool clarityCritical;
        [SerializeField] private bool pigmentCritical;
        [SerializeField] private int motionEventCount;

        private Vector2 topBarRestPosition;
        private Vector2 encounterRestPosition;
        private Vector2 progressRestPosition;
        private Vector2 contextRestPosition;

        private Vector3 topBarRestScale = Vector3.one;
        private Vector3 encounterRestScale = Vector3.one;
        private Vector3 progressRestScale = Vector3.one;
        private Vector3 figureRestScale = Vector3.one;
        private Vector3 painterRestScale = Vector3.one;
        private Vector3 contextRestScale = Vector3.one;
        private Vector3 roleTextRestScale = Vector3.one;
        private Vector3 matchStateTextRestScale = Vector3.one;

        private Color roleTextRestColor = Color.white;
        private Color matchStateTextRestColor = Color.white;

        private float previousProgress;
        private string previousContext = string.Empty;
        private float nextSampleAt;
        private float nextClarityPulseAt;
        private float nextPigmentPulseAt;
        private bool hasInitialSnapshot;

        private Coroutine topBarMotion;
        private Coroutine encounterMotion;
        private Coroutine progressMotion;
        private Coroutine figureMotion;
        private Coroutine painterMotion;
        private Coroutine contextMotion;
        private Coroutine roleTextMotion;
        private Coroutine matchStateTextMotion;

        public bool IsConfigured => configured;
        public bool MotionEnabled => motionEnabled;
        public string LastObservedRole => lastObservedRole;
        public string LastObservedMatchState => lastObservedMatchState;
        public bool ClarityCritical => clarityCritical;
        public bool PigmentCritical => pigmentCritical;
        public int MotionEventCount => motionEventCount;
        public string MotionBackend => "UnityCoroutine";

        public void Configure(
            PrototypeUnifiedHudController configuredSnapshotSource,
            RectTransform configuredTopBar,
            RectTransform configuredEncounterStrip,
            RectTransform configuredProgressPanel,
            RectTransform configuredFigurePanel,
            RectTransform configuredPainterPanel,
            RectTransform configuredContextPanel,
            Text configuredRoleText,
            Text configuredMatchStateText)
        {
            snapshotSource = configuredSnapshotSource;
            topBar = configuredTopBar;
            encounterStrip = configuredEncounterStrip;
            progressPanel = configuredProgressPanel;
            figurePanel = configuredFigurePanel;
            painterPanel = configuredPainterPanel;
            contextPanel = configuredContextPanel;
            roleText = configuredRoleText;
            matchStateText = configuredMatchStateText;

            CacheRestPose();
        }

        private void Awake()
        {
            CacheRestPose();
        }

        private void OnEnable()
        {
            nextSampleAt = 0f;
            nextClarityPulseAt = 0f;
            nextPigmentPulseAt = 0f;
            hasInitialSnapshot = false;

            if (configured)
            {
                TriggerEntrance();
            }
        }

        private void OnDisable()
        {
            StopAllMotion();
            RestoreRestPose();
        }

        private void OnDestroy()
        {
            StopAllMotion();
        }

        private void Update()
        {
            if (!configured)
            {
                CacheRestPose();
                if (!configured)
                {
                    return;
                }
            }

            if (!motionEnabled)
            {
                StopAllMotion();
                RestoreRestPose();
            }

            if (Time.unscaledTime < nextSampleAt)
            {
                return;
            }

            nextSampleAt = Time.unscaledTime + sampleInterval;
            PrototypeUnifiedHudSnapshot snapshot = snapshotSource.BuildSnapshot();

            if (!hasInitialSnapshot)
            {
                hasInitialSnapshot = true;
                lastObservedRole = snapshot.Role ?? string.Empty;
                lastObservedMatchState = snapshot.MatchState ?? string.Empty;
                previousProgress = snapshot.NormalizedProgress;
                previousContext = snapshot.ContextStatus ?? string.Empty;
                clarityCritical = snapshot.NormalizedClarity <= clarityCriticalThreshold;
                pigmentCritical = snapshot.NormalizedPigment <= pigmentCriticalThreshold;
                return;
            }

            ObserveRole(snapshot);
            ObserveMatchState(snapshot);
            ObserveProgress(snapshot);
            ObserveContext(snapshot);
            ObserveCriticalStates(snapshot);
        }

        [ContextMenu("Set Motion Enabled")]
        public void EnableMotion()
        {
            motionEnabled = true;
        }

        [ContextMenu("Set Motion Disabled")]
        public void DisableMotion()
        {
            motionEnabled = false;
            StopAllMotion();
            RestoreRestPose();
        }

        private void ObserveRole(PrototypeUnifiedHudSnapshot snapshot)
        {
            string role = snapshot.Role ?? string.Empty;
            if (role == lastObservedRole)
            {
                return;
            }

            lastObservedRole = role;
            TriggerRoleChanged(snapshot.IsPainter);
        }

        private void ObserveMatchState(PrototypeUnifiedHudSnapshot snapshot)
        {
            string matchState = snapshot.MatchState ?? string.Empty;
            if (matchState == lastObservedMatchState)
            {
                return;
            }

            lastObservedMatchState = matchState;
            TriggerMatchStateChanged();
        }

        private void ObserveProgress(PrototypeUnifiedHudSnapshot snapshot)
        {
            float progress = Mathf.Clamp01(snapshot.NormalizedProgress);
            float gain = progress - previousProgress;

            if (gain >= progressPulseMinimumGain)
            {
                TriggerProgressGain();
            }

            previousProgress = Mathf.Max(previousProgress, progress);
        }

        private void ObserveContext(PrototypeUnifiedHudSnapshot snapshot)
        {
            string context = snapshot.ContextStatus ?? string.Empty;
            bool appeared =
                !string.IsNullOrWhiteSpace(context) &&
                !string.Equals(context, previousContext);

            previousContext = context;

            if (appeared)
            {
                TriggerContextMessage();
            }
        }

        private void ObserveCriticalStates(PrototypeUnifiedHudSnapshot snapshot)
        {
            clarityCritical =
                !snapshot.IsPainter &&
                snapshot.NormalizedClarity <= clarityCriticalThreshold;

            pigmentCritical =
                snapshot.IsPainter &&
                snapshot.NormalizedPigment <= pigmentCriticalThreshold;

            if (clarityCritical &&
                Time.unscaledTime >= nextClarityPulseAt)
            {
                nextClarityPulseAt =
                    Time.unscaledTime + repeatedCriticalPulseInterval;
                TriggerClarityCritical();
            }

            if (pigmentCritical &&
                Time.unscaledTime >= nextPigmentPulseAt)
            {
                nextPigmentPulseAt =
                    Time.unscaledTime + repeatedCriticalPulseInterval;
                TriggerPigmentCritical();
            }
        }

        private void TriggerEntrance()
        {
            if (!CanAnimate())
            {
                return;
            }

            float intensity = EffectiveIntensity;

            StartTracked(
                ref topBarMotion,
                AnimatePositionAndScale(
                    topBar,
                    topBarRestPosition + Vector2.up * Mathf.Lerp(8f, 28f, intensity),
                    topBarRestPosition,
                    topBarRestScale * Mathf.Lerp(0.995f, 0.965f, intensity),
                    topBarRestScale,
                    0.28f));

            StartTracked(
                ref encounterMotion,
                AnimatePositionAndScale(
                    encounterStrip,
                    encounterRestPosition + Vector2.up * Mathf.Lerp(6f, 18f, intensity),
                    encounterRestPosition,
                    encounterRestScale * Mathf.Lerp(0.995f, 0.975f, intensity),
                    encounterRestScale,
                    0.30f));

            StartTracked(
                ref progressMotion,
                AnimatePositionAndScale(
                    progressPanel,
                    progressRestPosition + Vector2.down * Mathf.Lerp(8f, 24f, intensity),
                    progressRestPosition,
                    progressRestScale * Mathf.Lerp(0.995f, 0.970f, intensity),
                    progressRestScale,
                    0.32f));

            motionEventCount++;
        }

        private void TriggerRoleChanged(bool isPainter)
        {
            if (!CanAnimate())
            {
                return;
            }

            StartTracked(
                ref topBarMotion,
                AnimateScalePulse(
                    topBar,
                    topBarRestScale,
                    Mathf.Lerp(1.01f, 1.035f, EffectiveIntensity),
                    0.09f,
                    0.18f));

            Color highlight = isPainter
                ? new Color(0.95f, 0.28f, 0.18f, 1f)
                : new Color(0.15f, 0.80f, 0.85f, 1f);

            StartTracked(
                ref roleTextMotion,
                AnimateTextPulse(
                    roleText,
                    roleTextRestScale,
                    roleTextRestColor,
                    highlight,
                    Mathf.Lerp(1.02f, 1.09f, EffectiveIntensity),
                    0.10f,
                    0.22f));

            motionEventCount++;
        }

        private void TriggerMatchStateChanged()
        {
            if (!CanAnimate())
            {
                return;
            }

            StartTracked(
                ref matchStateTextMotion,
                AnimateTextPulse(
                    matchStateText,
                    matchStateTextRestScale,
                    matchStateTextRestColor,
                    new Color(1f, 0.67f, 0.20f, 1f),
                    Mathf.Lerp(1.02f, 1.10f, EffectiveIntensity),
                    0.10f,
                    0.24f));

            StartTracked(
                ref encounterMotion,
                AnimateScalePulse(
                    encounterStrip,
                    encounterRestScale,
                    Mathf.Lerp(1.008f, 1.030f, EffectiveIntensity),
                    0.10f,
                    0.20f));

            motionEventCount++;
        }

        private void TriggerProgressGain()
        {
            if (!CanAnimate())
            {
                return;
            }

            StartTracked(
                ref progressMotion,
                AnimateScalePulse(
                    progressPanel,
                    progressRestScale,
                    Mathf.Lerp(1.008f, 1.030f, EffectiveIntensity),
                    0.08f,
                    0.18f));

            motionEventCount++;
        }

        private void TriggerContextMessage()
        {
            if (!CanAnimate())
            {
                return;
            }

            float intensity = EffectiveIntensity;

            StartTracked(
                ref contextMotion,
                AnimatePositionAndScale(
                    contextPanel,
                    contextRestPosition + Vector2.down * Mathf.Lerp(6f, 18f, intensity),
                    contextRestPosition,
                    contextRestScale * Mathf.Lerp(0.990f, 0.940f, intensity),
                    contextRestScale,
                    0.22f));

            motionEventCount++;
        }

        private void TriggerClarityCritical()
        {
            if (!CanAnimate())
            {
                return;
            }

            StartTracked(
                ref figureMotion,
                AnimateScalePulse(
                    figurePanel,
                    figureRestScale,
                    Mathf.Lerp(1.012f, 1.050f, EffectiveIntensity),
                    0.10f,
                    0.22f));

            motionEventCount++;
        }

        private void TriggerPigmentCritical()
        {
            if (!CanAnimate())
            {
                return;
            }

            StartTracked(
                ref painterMotion,
                AnimateScalePulse(
                    painterPanel,
                    painterRestScale,
                    Mathf.Lerp(1.012f, 1.050f, EffectiveIntensity),
                    0.10f,
                    0.22f));

            motionEventCount++;
        }

        private IEnumerator AnimatePositionAndScale(
            RectTransform target,
            Vector2 startPosition,
            Vector2 endPosition,
            Vector3 startScale,
            Vector3 endScale,
            float duration)
        {
            if (target == null)
            {
                yield break;
            }

            target.anchoredPosition = startPosition;
            target.localScale = startScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f
                    ? Mathf.Clamp01(elapsed / duration)
                    : 1f;
                float eased = EaseOutCubic(t);

                if (target == null)
                {
                    yield break;
                }

                target.anchoredPosition =
                    Vector2.LerpUnclamped(startPosition, endPosition, eased);
                target.localScale =
                    Vector3.LerpUnclamped(startScale, endScale, eased);

                yield return null;
            }

            if (target != null)
            {
                target.anchoredPosition = endPosition;
                target.localScale = endScale;
            }
        }

        private IEnumerator AnimateScalePulse(
            RectTransform target,
            Vector3 restScale,
            float peakMultiplier,
            float upDuration,
            float downDuration)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 startScale = target.localScale;
            Vector3 peakScale = restScale * peakMultiplier;

            yield return AnimateScalePhase(
                target,
                startScale,
                peakScale,
                upDuration,
                EaseOutCubic);

            yield return AnimateScalePhase(
                target,
                peakScale,
                restScale,
                downDuration,
                SmoothStep);

            if (target != null)
            {
                target.localScale = restScale;
            }
        }

        private IEnumerator AnimateTextPulse(
            Text target,
            Vector3 restScale,
            Color restColor,
            Color highlightColor,
            float peakMultiplier,
            float upDuration,
            float downDuration)
        {
            if (target == null)
            {
                yield break;
            }

            RectTransform rect = target.rectTransform;
            Vector3 startScale = rect.localScale;
            Color startColor = target.color;
            Vector3 peakScale = restScale * peakMultiplier;

            float elapsed = 0f;
            while (elapsed < upDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = upDuration > 0f
                    ? Mathf.Clamp01(elapsed / upDuration)
                    : 1f;
                float eased = EaseOutCubic(t);

                if (target == null || rect == null)
                {
                    yield break;
                }

                rect.localScale =
                    Vector3.LerpUnclamped(startScale, peakScale, eased);
                target.color =
                    Color.LerpUnclamped(startColor, highlightColor, eased);

                yield return null;
            }

            elapsed = 0f;
            while (elapsed < downDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = downDuration > 0f
                    ? Mathf.Clamp01(elapsed / downDuration)
                    : 1f;
                float eased = SmoothStep(t);

                if (target == null || rect == null)
                {
                    yield break;
                }

                rect.localScale =
                    Vector3.LerpUnclamped(peakScale, restScale, eased);
                target.color =
                    Color.LerpUnclamped(highlightColor, restColor, eased);

                yield return null;
            }

            if (target != null && rect != null)
            {
                rect.localScale = restScale;
                target.color = restColor;
            }
        }

        private IEnumerator AnimateScalePhase(
            RectTransform target,
            Vector3 from,
            Vector3 to,
            float duration,
            System.Func<float, float> easing)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f
                    ? Mathf.Clamp01(elapsed / duration)
                    : 1f;

                if (target == null)
                {
                    yield break;
                }

                target.localScale =
                    Vector3.LerpUnclamped(from, to, easing(t));

                yield return null;
            }

            if (target != null)
            {
                target.localScale = to;
            }
        }

        private void StartTracked(
            ref Coroutine running,
            IEnumerator routine)
        {
            if (running != null)
            {
                StopCoroutine(running);
            }

            running = StartCoroutine(routine);
        }

        private bool CanAnimate()
        {
            return motionEnabled &&
                configured &&
                EffectiveIntensity > 0.001f &&
                isActiveAndEnabled;
        }

        private float EffectiveIntensity =>
            Mathf.Clamp01(motionIntensity);

        private void CacheRestPose()
        {
            if (topBar != null)
            {
                topBarRestPosition = topBar.anchoredPosition;
                topBarRestScale = topBar.localScale;
            }

            if (encounterStrip != null)
            {
                encounterRestPosition = encounterStrip.anchoredPosition;
                encounterRestScale = encounterStrip.localScale;
            }

            if (progressPanel != null)
            {
                progressRestPosition = progressPanel.anchoredPosition;
                progressRestScale = progressPanel.localScale;
            }

            if (figurePanel != null)
            {
                figureRestScale = figurePanel.localScale;
            }

            if (painterPanel != null)
            {
                painterRestScale = painterPanel.localScale;
            }

            if (contextPanel != null)
            {
                contextRestPosition = contextPanel.anchoredPosition;
                contextRestScale = contextPanel.localScale;
            }

            if (roleText != null)
            {
                roleTextRestScale = roleText.rectTransform.localScale;
                roleTextRestColor = roleText.color;
            }

            if (matchStateText != null)
            {
                matchStateTextRestScale =
                    matchStateText.rectTransform.localScale;
                matchStateTextRestColor = matchStateText.color;
            }

            configured =
                snapshotSource != null &&
                topBar != null &&
                encounterStrip != null &&
                progressPanel != null &&
                figurePanel != null &&
                painterPanel != null &&
                contextPanel != null &&
                roleText != null &&
                matchStateText != null;
        }

        private void RestoreRestPose()
        {
            RestorePosition(topBar, topBarRestPosition);
            RestorePosition(encounterStrip, encounterRestPosition);
            RestorePosition(progressPanel, progressRestPosition);
            RestorePosition(contextPanel, contextRestPosition);

            RestoreScale(topBar, topBarRestScale);
            RestoreScale(encounterStrip, encounterRestScale);
            RestoreScale(progressPanel, progressRestScale);
            RestoreScale(figurePanel, figureRestScale);
            RestoreScale(painterPanel, painterRestScale);
            RestoreScale(contextPanel, contextRestScale);

            if (roleText != null)
            {
                roleText.rectTransform.localScale = roleTextRestScale;
                roleText.color = roleTextRestColor;
            }

            if (matchStateText != null)
            {
                matchStateText.rectTransform.localScale =
                    matchStateTextRestScale;
                matchStateText.color = matchStateTextRestColor;
            }
        }

        private void StopAllMotion()
        {
            StopTracked(ref topBarMotion);
            StopTracked(ref encounterMotion);
            StopTracked(ref progressMotion);
            StopTracked(ref figureMotion);
            StopTracked(ref painterMotion);
            StopTracked(ref contextMotion);
            StopTracked(ref roleTextMotion);
            StopTracked(ref matchStateTextMotion);
        }

        private void StopTracked(ref Coroutine running)
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }
        }

        private static float EaseOutCubic(float value)
        {
            float oneMinus = 1f - value;
            return 1f - oneMinus * oneMinus * oneMinus;
        }

        private static float SmoothStep(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static void RestorePosition(
            RectTransform target,
            Vector2 value)
        {
            if (target != null)
            {
                target.anchoredPosition = value;
            }
        }

        private static void RestoreScale(
            RectTransform target,
            Vector3 value)
        {
            if (target != null)
            {
                target.localScale = value;
            }
        }
    }
}
