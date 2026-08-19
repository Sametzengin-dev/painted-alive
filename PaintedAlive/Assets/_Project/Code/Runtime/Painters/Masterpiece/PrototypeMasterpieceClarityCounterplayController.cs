using System;
using PaintedAlive.Core.RoleAuthority;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(24950)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceClarityCounterplayController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeMasterpieceHitValidationController hitValidation;

        [SerializeField]
        private PrototypeMasterpieceAutonomousEncounterController encounter;

        [SerializeField]
        private PrototypeFigureClarityImpactBridge clarityBridge;

        [Header("Clarity Contract")]
        [SerializeField, Min(0.1f)]
        private float clarityExposurePerConfirmedHit = 10f;

        [SerializeField, Min(0.1f)]
        private float feedbackVisibleSeconds = 2.2f;

        [SerializeField, Min(0f)]
        private float feedbackFadeSeconds = 0.35f;

        [Header("HUD")]
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text resultText;
        [SerializeField] private Text counterText;

        [Header("Runtime Read Only")]
        [SerializeField] private bool roleSourceResolved;
        [SerializeField] private bool figureRoleActive;
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private int lastProcessedSequenceId;
        [SerializeField] private int processedResultCount;
        [SerializeField] private int clarityHitCount;
        [SerializeField] private int clarityWriteCount;
        [SerializeField] private int clarityWriteFailureCount;
        [SerializeField] private int zeroDeltaHitCount;
        [SerializeField] private int successfulDodgeCount;
        [SerializeField] private int outOfReachMissCount;
        [SerializeField] private int occludedMissCount;
        [SerializeField] private int targetUnavailableCount;
        [SerializeField] private int abortedResultCount;
        [SerializeField] private float totalClarityLost;
        [SerializeField] private float lastClarityBefore;
        [SerializeField] private float lastClarityAfter;
        [SerializeField] private float lastClarityDelta;
        [SerializeField] private string lastLevelBefore = "Unknown";
        [SerializeField] private string lastLevelAfter = "Unknown";
        [SerializeField]
        private PrototypeMasterpieceHitValidationResult lastProcessedResult =
            PrototypeMasterpieceHitValidationResult.None;

        [SerializeField] private string lastMaterial =
            "Baş Yapıt Mürekkep Darbesi";

        [SerializeField] private string lastAvailableCounter =
            "Telegraph alanından çık.";

        [SerializeField] private string lastAction =
            "M49.1 sonucu bekleniyor.";

        private float feedbackVisibleUntil;

        public bool RoleSourceResolved =>
            roleSourceResolved;
        public bool FigureRoleActive =>
            figureRoleActive;
        public string ResolvedRole =>
            resolvedRole;
        public int LastProcessedSequenceId =>
            lastProcessedSequenceId;
        public int ProcessedResultCount =>
            processedResultCount;
        public int ClarityHitCount =>
            clarityHitCount;
        public int ClarityWriteCount =>
            clarityWriteCount;
        public int ClarityWriteFailureCount =>
            clarityWriteFailureCount;
        public int ZeroDeltaHitCount =>
            zeroDeltaHitCount;
        public int SuccessfulDodgeCount =>
            successfulDodgeCount;
        public int OutOfReachMissCount =>
            outOfReachMissCount;
        public int OccludedMissCount =>
            occludedMissCount;
        public int TargetUnavailableCount =>
            targetUnavailableCount;
        public int AbortedResultCount =>
            abortedResultCount;
        public float TotalClarityLost =>
            totalClarityLost;
        public float LastClarityBefore =>
            lastClarityBefore;
        public float LastClarityAfter =>
            lastClarityAfter;
        public float LastClarityDelta =>
            lastClarityDelta;
        public string LastLevelBefore =>
            lastLevelBefore;
        public string LastLevelAfter =>
            lastLevelAfter;
        public PrototypeMasterpieceHitValidationResult
            LastProcessedResult =>
                lastProcessedResult;
        public string LastMaterial =>
            lastMaterial;
        public string LastAvailableCounter =>
            lastAvailableCounter;
        public string LastAction =>
            lastAction;

        public bool ClarityWritesEnabled => true;
        public bool UsesAuthoritativeFigureClarityState => true;
        public bool RegionalExposureEnabled => true;
        public string ExposureRegion => "Torso";
        public bool DodgeCounterplayEnabled => true;
        public bool NewParryInputAdded => false;
        public bool HealthSystemEnabled => false;
        public bool KnockbackEnabled => false;
        public bool ControlLockEnabled => false;
        public bool StunEnabled => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeMasterpieceHitValidationController
                configuredHitValidation,
            PrototypeMasterpieceAutonomousEncounterController
                configuredEncounter,
            PrototypeFigureClarityImpactBridge
                configuredClarityBridge,
            CanvasGroup configuredFeedbackGroup,
            Text configuredTitleText,
            Text configuredResultText,
            Text configuredCounterText)
        {
            hitValidation =
                configuredHitValidation;

            encounter =
                configuredEncounter;

            clarityBridge =
                configuredClarityBridge;

            feedbackGroup =
                configuredFeedbackGroup;

            titleText =
                configuredTitleText;

            resultText =
                configuredResultText;

            counterText =
                configuredCounterText;

            HideFeedback();
        }

        private void Awake()
        {
            HideFeedback();
        }

        private void Update()
        {
            RefreshRoleAuthority();
            TryProcessResolvedSequence();
            UpdateFeedbackVisibility();

            clarityBridge?.RefreshSnapshot();
        }

        public bool ApplyTestClarityExposure()
        {
            RefreshRoleAuthority();

            if (!figureRoleActive)
            {
                lastAction =
                    "Test Netlik maruziyeti yalnız Figure rolünde çalışır.";

                ShowFeedback(
                    "TEST REDDEDİLDİ",
                    lastAction,
                    "F1 ile Figure rolüne geç.");

                return false;
            }

            return ApplyClarityHit(
                sequenceId: -1,
                source:
                    "Editor test clarity exposure");
        }

        public bool ResetFigureClarityForPrototype()
        {
            if (clarityBridge == null)
            {
                lastAction =
                    "Clarity bridge bulunamadı.";

                return false;
            }

            bool reset =
                clarityBridge.TryResetToFull(
                    out string reason);

            lastAction = reason;

            if (reset)
            {
                lastClarityBefore =
                    clarityBridge.LastClarity;

                lastClarityAfter =
                    clarityBridge.LastClarity;

                lastClarityDelta = 0f;
                lastLevelBefore =
                    clarityBridge.LastLevel;

                lastLevelAfter =
                    clarityBridge.LastLevel;

                ShowFeedback(
                    "NETLİK SIFIRLANDI",
                    $"{clarityBridge.LastClarity:F0} / " +
                    $"{clarityBridge.LastMaximumClarity:F0}",
                    "Yalnız prototip geliştirme komutu.");
            }

            return reset;
        }

        private void RefreshRoleAuthority()
        {
            roleSourceResolved =
                PrototypeRoleAuthorityResolver.TryResolve(
                    out resolvedRole,
                    out bool isPainter);

            figureRoleActive =
                roleSourceResolved &&
                !isPainter;
        }

        private void TryProcessResolvedSequence()
        {
            if (
                hitValidation == null ||
                !hitValidation.StrikeValidated ||
                hitValidation.SequenceId <= 0 ||
                hitValidation.SequenceId ==
                    lastProcessedSequenceId ||
                hitValidation.LastResult ==
                    PrototypeMasterpieceHitValidationResult.None ||
                hitValidation.LastResult ==
                    PrototypeMasterpieceHitValidationResult.Pending
            )
            {
                return;
            }

            lastProcessedSequenceId =
                hitValidation.SequenceId;

            lastProcessedResult =
                hitValidation.LastResult;

            processedResultCount++;

            switch (lastProcessedResult)
            {
                case PrototypeMasterpieceHitValidationResult.HitConfirmed:
                    ApplyClarityHit(
                        lastProcessedSequenceId,
                        "M49.1 HitConfirmed");
                    break;

                case PrototypeMasterpieceHitValidationResult.MissDodged:
                    successfulDodgeCount++;
                    lastAction =
                        "Telegraph dışına zamanında çıkıldı; Netlik kaybı yok.";

                    ShowFeedback(
                        "KARŞI HAMLE BAŞARILI",
                        "NETLİK KAYBI 0",
                        "Kaçınma: telegraph alanından çıkış.");
                    break;

                case PrototypeMasterpieceHitValidationResult.MissOutOfReach:
                    outOfReachMissCount++;
                    lastAction =
                        "Baş Yapıt saldırısı menzil dışında kaldı.";

                    ShowFeedback(
                        "SALDIRI MENZİL DIŞI",
                        "NETLİK KAYBI 0",
                        "Mesafeyi koruma karşı hamlesi.");
                    break;

                case PrototypeMasterpieceHitValidationResult.MissOccluded:
                    occludedMissCount++;
                    lastAction =
                        "Dünya engeli saldırı hattını kesti.";

                    ShowFeedback(
                        "SİPER BAŞARILI",
                        "NETLİK KAYBI 0",
                        "Dünya geometrisini saldırı hattına sok.");
                    break;

                case PrototypeMasterpieceHitValidationResult.TargetUnavailable:
                    targetUnavailableCount++;
                    lastAction =
                        "Hedef query hacmi doğrulanamadı.";

                    ShowFeedback(
                        "HEDEF DOĞRULANAMADI",
                        "NETLİK YAZIMI YOK",
                        "Query sözleşmesi kontrol edilmeli.");
                    break;

                default:
                    abortedResultCount++;
                    lastAction =
                        $"Saldırı sonucu {lastProcessedResult}; Netlik yazımı yok.";

                    ShowFeedback(
                        "SALDIRI İPTAL",
                        TranslateAbort(
                            lastProcessedResult),
                        "Netlik kaybı uygulanmadı.");
                    break;
            }
        }

        private bool ApplyClarityHit(
            int sequenceId,
            string source)
        {
            clarityHitCount++;

            if (clarityBridge == null)
            {
                clarityWriteFailureCount++;
                lastAction =
                    "Clarity bridge bulunamadı.";

                ShowFeedback(
                    "HIT • NETLİK YAZILAMADI",
                    lastAction,
                    "FigureClarityState bağlantısını kontrol et.");

                return false;
            }

            bool applied =
                clarityBridge.TryApplyTorsoExposure(
                    clarityExposurePerConfirmedHit,
                    out float before,
                    out float after,
                    out string levelBefore,
                    out string levelAfter,
                    out string reason);

            lastClarityBefore = before;
            lastClarityAfter = after;
            lastClarityDelta =
                Mathf.Max(
                    0f,
                    before -
                    after);

            lastLevelBefore = levelBefore;
            lastLevelAfter = levelAfter;

            if (!applied)
            {
                clarityWriteFailureCount++;
                lastAction =
                    $"Netlik yazımı başarısız: {reason}";

                ShowFeedback(
                    "HIT • NETLİK YAZILAMADI",
                    reason,
                    "FigureClarityState sözleşmesini kontrol et.");

                return false;
            }

            clarityWriteCount++;
            totalClarityLost +=
                lastClarityDelta;

            if (lastClarityDelta <= 0.001f)
            {
                zeroDeltaHitCount++;
            }

            lastAction =
                $"{source}; {lastClarityDelta:F0} Netlik kaybı, Torso bölgesi.";

            string levelTransition =
                string.Equals(
                    levelBefore,
                    levelAfter,
                    StringComparison.Ordinal)
                    ? levelAfter
                    : $"{levelBefore} → {levelAfter}";

            ShowFeedback(
                "BAŞ YAPIT İSABETİ",
                $"-{lastClarityDelta:F0} NETLİK • " +
                $"GÖVDE • {after:F0}/{clarityBridge.LastMaximumClarity:F0}",
                "Karşı hamle: telegraph alanından çık. " +
                $"Durum: {levelTransition}");

            Debug.Log(
                "[M49.2 Figure Clarity Impact]\n" +
                $"SequenceId={sequenceId}\n" +
                $"Material={lastMaterial}\n" +
                $"Region=Torso\n" +
                $"ConfiguredExposure={clarityExposurePerConfirmedHit:F2}\n" +
                $"ClarityBefore={before:F2}\n" +
                $"ClarityAfter={after:F2}\n" +
                $"ClarityDelta={lastClarityDelta:F2}\n" +
                $"LevelBefore={levelBefore}\n" +
                $"LevelAfter={levelAfter}\n" +
                "AvailableCounter=Exit telegraph area\n" +
                "HealthSystemEnabled=False\n" +
                "KnockbackEnabled=False\n" +
                "ControlLockEnabled=False\n" +
                "NetworkAuthorityEnabled=False",
                clarityBridge.ClarityState);

            return true;
        }

        private void ShowFeedback(
            string title,
            string result,
            string counter)
        {
            feedbackVisibleUntil =
                Time.unscaledTime +
                Mathf.Max(
                    0.2f,
                    feedbackVisibleSeconds);

            if (titleText != null)
            {
                titleText.text =
                    title;
            }

            if (resultText != null)
            {
                resultText.text =
                    result;
            }

            if (counterText != null)
            {
                counterText.text =
                    counter;
            }

            UpdateFeedbackVisibility();
        }

        private void UpdateFeedbackVisibility()
        {
            if (feedbackGroup == null)
            {
                return;
            }

            bool shouldDisplay =
                figureRoleActive &&
                Time.unscaledTime <
                    feedbackVisibleUntil;

            if (!shouldDisplay)
            {
                feedbackGroup.alpha = 0f;
                feedbackGroup.interactable = false;
                feedbackGroup.blocksRaycasts = false;
                return;
            }

            float remaining =
                feedbackVisibleUntil -
                Time.unscaledTime;

            float alpha = 1f;

            if (
                feedbackFadeSeconds > 0f &&
                remaining <
                    feedbackFadeSeconds
            )
            {
                alpha =
                    Mathf.Clamp01(
                        remaining /
                        feedbackFadeSeconds);
            }

            feedbackGroup.alpha = alpha;
            feedbackGroup.interactable = false;
            feedbackGroup.blocksRaycasts = false;
        }

        private void HideFeedback()
        {
            feedbackVisibleUntil = 0f;

            if (feedbackGroup != null)
            {
                feedbackGroup.alpha = 0f;
                feedbackGroup.interactable = false;
                feedbackGroup.blocksRaycasts = false;
            }
        }

        private static string TranslateAbort(
            PrototypeMasterpieceHitValidationResult result)
        {
            switch (result)
            {
                case PrototypeMasterpieceHitValidationResult.AbortedPossession:
                    return "Sahiplenme AI saldırısını durdurdu.";

                case PrototypeMasterpieceHitValidationResult.AbortedCapability:
                    return "Saldırı organı veya çekirdek capability kapandı.";

                case PrototypeMasterpieceHitValidationResult.AbortedRecall:
                    return "Baş Yapıt dünyadan geri çağrıldı.";

                default:
                    return "Encounter state saldırıyı iptal etti.";
            }
        }

        private void OnDisable()
        {
            HideFeedback();
        }
    }
}
