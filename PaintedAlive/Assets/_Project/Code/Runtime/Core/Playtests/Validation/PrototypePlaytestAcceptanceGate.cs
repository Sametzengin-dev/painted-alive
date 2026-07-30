using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PaintedAlive.Core.Playtests;
using PaintedAlive.Core.Prototypes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Core.Playtests.Validation
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlaytestAcceptanceGate : MonoBehaviour
    {
        private const string ReportFolderName = "PlaytestTelemetry/M41_Acceptance";
        private const string AggregateFileName = "M41_Aggregate.json";

        private static readonly PrototypeAcceptanceQuestion[] Questions =
        {
            PrototypeAcceptanceQuestion.AttackReadBeforeImpact,
            PrototypeAcceptanceQuestion.CounterplayUnderstood,
            PrototypeAcceptanceQuestion.FailureCauseUnderstood,
            PrototypeAcceptanceQuestion.ControlsFeltReliable,
            PrototypeAcceptanceQuestion.WouldPlayAnotherRun
        };

        [Header("Existing Authoritative Systems")]
        [SerializeField] private PrototypeMatchController matchController;
        [SerializeField] private PrototypeOneVsOnePlaytestSession m40Session;
        [SerializeField] private PrototypePlaytestTelemetry legacyTelemetry;

        [Header("Configuration")]
        [SerializeField] private PrototypePlaytestAcceptanceConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField] private bool collectingReports;
        [SerializeField] private bool reviewActive;
        [SerializeField] private bool reviewCompleted;
        [SerializeField] private int currentQuestionIndex;
        [SerializeField] private string currentQuestionText;
        [SerializeField] private string statusMessage;
        [SerializeField] private string currentRunReportPath;
        [SerializeField] private string aggregateReportPath;
        [SerializeField] private bool currentRunPassed;
        [SerializeField] private bool networkSpikeCandidateReady;
        [SerializeField] private int aggregatePassingRuns;
        [SerializeField] private int aggregateEvaluatedRuns;
        [SerializeField] private int aggregateRequiredRuns;

        private DateTime runStartedUtc;
        private string m40PathAtRunStart;
        private Coroutine collectionRoutine;
        private PrototypeAcceptanceRunReport currentReport;
        private PrototypeAcceptanceAggregateReport aggregateReport;
        private PrototypeAcceptanceSnapshot currentSnapshot;

        public event Action<PrototypeAcceptanceSnapshot> SnapshotChanged;

        public PrototypePlaytestAcceptanceConfig Config => config;
        public PrototypeAcceptanceSnapshot CurrentSnapshot => currentSnapshot;
        public bool ReviewActive => reviewActive;
        public bool ReviewCompleted => reviewCompleted;
        public string CurrentRunReportPath => currentRunReportPath;
        public string AggregateReportPath => aggregateReportPath;
        public bool CurrentRunPassed => currentRunPassed;
        public bool NetworkSpikeCandidateReady => networkSpikeCandidateReady;

        public void Configure(
            PrototypeMatchController authoritativeMatch,
            PrototypeOneVsOnePlaytestSession oneVsOneSession,
            PrototypePlaytestTelemetry telemetry,
            PrototypePlaytestAcceptanceConfig acceptanceConfig)
        {
            matchController = authoritativeMatch;
            m40Session = oneVsOneSession;
            legacyTelemetry = telemetry;
            config = acceptanceConfig;
        }

        private string ReportDirectory =>
            Path.Combine(Application.persistentDataPath, ReportFolderName);

        private void Awake()
        {
            ResolveDependencies();
            LoadAggregateIfPresent();
            PublishSnapshot();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (matchController != null)
            {
                matchController.StateChanged += HandleMatchStateChanged;
            }
        }

        private void OnDisable()
        {
            if (matchController != null)
            {
                matchController.StateChanged -= HandleMatchStateChanged;
            }

            if (collectionRoutine != null)
            {
                StopCoroutine(collectionRoutine);
                collectionRoutine = null;
            }
        }

        private void Start()
        {
            if (matchController == null)
            {
                Debug.LogError("[M41] PrototypeMatchController bulunamadı.", this);
                enabled = false;
                return;
            }

            HandleMatchStateChanged(matchController.State);
        }

        private void Update()
        {
            if (!reviewActive || currentQuestionIndex >= Questions.Length)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.yKey.wasPressedThisFrame)
            {
                RecordAnswer(true);
            }
            else if (keyboard.nKey.wasPressedThisFrame)
            {
                RecordAnswer(false);
            }
        }

        private void ResolveDependencies()
        {
            matchController ??= GetComponent<PrototypeMatchController>();
            m40Session ??= GetComponent<PrototypeOneVsOnePlaytestSession>();
            legacyTelemetry ??= GetComponent<PrototypePlaytestTelemetry>();
        }

        private void HandleMatchStateChanged(PrototypeMatchState state)
        {
            switch (state)
            {
                case PrototypeMatchState.Countdown:
                    HandleNewRunCountdown();
                    break;

                case PrototypeMatchState.Running:
                    runStartedUtc = DateTime.UtcNow;
                    m40PathAtRunStart = m40Session != null
                        ? m40Session.LastReportPath
                        : string.Empty;
                    statusMessage = "M41 mevcut M40 ve telemetry raporlarını bekliyor.";
                    PublishSnapshot();
                    break;

                case PrototypeMatchState.FigureEscaped:
                case PrototypeMatchState.TimeExpired:
                    BeginReportCollection();
                    break;
            }
        }

        private void HandleNewRunCountdown()
        {
            if (reviewActive && !reviewCompleted)
            {
                FinalizeIncompleteReview(
                    "Oyuncu okunabilirlik sorularını tamamlamadan yeni koşu başlattı.");
            }

            if (collectionRoutine != null)
            {
                StopCoroutine(collectionRoutine);
                collectionRoutine = null;
            }

            collectingReports = false;
            reviewActive = false;
            reviewCompleted = false;
            currentQuestionIndex = 0;
            currentQuestionText = string.Empty;
            currentRunReportPath = string.Empty;
            currentRunPassed = false;
            currentReport = null;
            runStartedUtc = DateTime.UtcNow;
            m40PathAtRunStart = m40Session != null
                ? m40Session.LastReportPath
                : string.Empty;
            statusMessage = "M41 yeni koşu için sıfırlandı.";
            PublishSnapshot();
        }

        private void BeginReportCollection()
        {
            if (collectingReports || reviewActive || reviewCompleted)
            {
                return;
            }

            if (collectionRoutine != null)
            {
                StopCoroutine(collectionRoutine);
            }

            collectionRoutine = StartCoroutine(CollectReportsAndBeginReview());
        }

        private IEnumerator CollectReportsAndBeginReview()
        {
            collectingReports = true;
            statusMessage = "M40 ve ayrıntılı telemetry raporları birleştiriliyor...";
            PublishSnapshot();

            float delay = config != null ? config.ReportCollectionDelay : 0.25f;
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            float timeout = config != null ? config.ReportWaitTimeout : 3f;
            float deadline = Time.realtimeSinceStartup + timeout;
            string m40Path = string.Empty;
            bool foundM40Report = false;

            while (Time.realtimeSinceStartup < deadline)
            {
                m40Path = m40Session != null ? m40Session.LastReportPath : string.Empty;
                bool isNewReport = !string.IsNullOrWhiteSpace(m40Path) &&
                    !string.Equals(m40Path, m40PathAtRunStart, StringComparison.Ordinal) &&
                    File.Exists(m40Path) &&
                    File.GetLastWriteTimeUtc(m40Path) >= runStartedUtc.AddSeconds(-2d);
                if (isNewReport)
                {
                    foundM40Report = true;
                    break;
                }

                yield return null;
            }

            if (!foundM40Report)
            {
                m40Path = string.Empty;
            }

            PrototypeOneVsOneRunReport m40Report = LoadM40Report(m40Path);

            string legacyPath = string.Empty;
            float legacyDeadline = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < legacyDeadline)
            {
                legacyPath = FindLatestLegacyTelemetryPath();
                if (!string.IsNullOrWhiteSpace(legacyPath) && File.Exists(legacyPath))
                {
                    break;
                }

                yield return null;
            }

            PrototypePlaytestReport legacyReport = LoadLegacyReport(legacyPath);
            BuildCurrentReport(m40Path, m40Report, legacyPath, legacyReport);

            collectingReports = false;
            reviewActive = true;
            reviewCompleted = false;
            currentQuestionIndex = 0;
            currentQuestionText = GetQuestionText(Questions[currentQuestionIndex]);
            statusMessage =
                "Maç sonrası okunabilirlik incelemesi başladı. Y=Evet, N=Hayır. " +
                "Sorular bitmeden Enter ile yeni koşu başlatma.";
            collectionRoutine = null;
            PublishSnapshot();
        }

        private void BuildCurrentReport(
            string m40Path,
            PrototypeOneVsOneRunReport m40Report,
            string legacyPath,
            PrototypePlaytestReport legacyReport)
        {
            currentReport = new PrototypeAcceptanceRunReport
            {
                reviewId = Guid.NewGuid().ToString("N"),
                utcStartedAt = runStartedUtc.ToString("O"),
                finalMatchState = matchController != null
                    ? matchController.State.ToString()
                    : "Unknown",
                m40ReportFound = m40Report != null,
                m40ReportPath = m40Path ?? string.Empty,
                legacyTelemetryFound = legacyReport != null,
                legacyTelemetryPath = legacyPath ?? string.Empty
            };

            if (m40Report != null)
            {
                currentReport.sourceM40RunId = m40Report.runId;
                currentReport.sourceM40RunNumber = m40Report.runNumber;
                currentReport.m40Accepted = m40Report.accepted;
                currentReport.distinctOutcomeCount = m40Report.passedOutcomeCount;
                currentReport.requiredOutcomeCount = m40Report.requiredOutcomeCount;
                currentReport.normalFigureExit = m40Report.normalFigureExit;
                currentReport.stainArrivalDuringRun = m40Report.stainArrivalDuringRun;
                currentReport.finalJourneyScore = m40Report.finalJourneyScore;
                currentReport.actualRunningDuration = m40Report.actualRunningDuration;
                currentReport.remainingTime = m40Report.remainingTime;
                currentReport.strokeCount = m40Report.strokeCount;
                currentReport.totalCutCount = m40Report.cutCount;
            }

            if (legacyReport != null)
            {
                currentReport.actualRunningDuration = legacyReport.actualRunningDuration;
                currentReport.strokeCount = legacyReport.strokeCount;
                currentReport.totalStrokeLength = legacyReport.totalStrokeLength;
                currentReport.pigmentSpent = legacyReport.pigmentSpent;
                currentReport.totalCutCount = legacyReport.totalCutCount;
                currentReport.wetCutCount = legacyReport.wetCutCount;
                currentReport.dryingCutCount = legacyReport.dryingCutCount;
                currentReport.dryCutCount = legacyReport.dryCutCount;
                currentReport.roleSwitchCount = legacyReport.roleSwitchCount;
                currentReport.figureRoleTime = legacyReport.figureRoleTime;
                currentReport.painterRoleTime = legacyReport.painterRoleTime;
                currentReport.furthestProgressNormalized =
                    legacyReport.furthestProgressNormalized;
                currentReport.blockedInputTime = legacyReport.blockedInputTime;
                currentReport.longestBlockedInputSequence =
                    legacyReport.longestBlockedInputSequence;
            }

            currentReport.blockedInputRatio = currentReport.actualRunningDuration > 0.001f
                ? currentReport.blockedInputTime / currentReport.actualRunningDuration
                : 0f;

            currentReport.answers.Clear();
            for (int i = 0; i < Questions.Length; i++)
            {
                currentReport.answers.Add(new PrototypeAcceptanceAnswer
                {
                    question = Questions[i],
                    answered = false,
                    value = false
                });
            }
        }

        private void RecordAnswer(bool value)
        {
            if (currentReport == null || currentQuestionIndex >= Questions.Length)
            {
                return;
            }

            PrototypeAcceptanceAnswer answer = currentReport.answers[currentQuestionIndex];
            answer.answered = true;
            answer.value = value;

            currentQuestionIndex++;
            if (currentQuestionIndex >= Questions.Length)
            {
                FinalizeCompletedReview();
                return;
            }

            currentQuestionText = GetQuestionText(Questions[currentQuestionIndex]);
            statusMessage = $"Yanıt kaydedildi. Soru {currentQuestionIndex + 1}/{Questions.Length}.";
            PublishSnapshot();
        }

        private void FinalizeCompletedReview()
        {
            reviewActive = false;
            reviewCompleted = true;
            currentQuestionText = string.Empty;
            currentReport.reviewCompleted = true;
            currentReport.incompleteReason = string.Empty;
            currentReport.utcFinishedAt = DateTime.UtcNow.ToString("O");

            EvaluateCurrentReport();
            SaveCurrentReport();
            RebuildAndSaveAggregate();

            currentRunPassed = currentReport.runPassed;
            statusMessage = currentRunPassed
                ? "M41 koşusu kabul edildi. Enter ile yeni koşuya geçebilirsin."
                : "M41 koşusu kabul edilmedi. Raporu incele; Enter ile yeni koşuya geçebilirsin.";
            PublishSnapshot();
        }

        private void FinalizeIncompleteReview(string reason)
        {
            if (currentReport == null)
            {
                return;
            }

            reviewActive = false;
            reviewCompleted = false;
            currentReport.reviewCompleted = false;
            currentReport.incompleteReason = reason;
            currentReport.utcFinishedAt = DateTime.UtcNow.ToString("O");
            currentReport.runPassed = false;
            SaveCurrentReport();
            RebuildAndSaveAggregate();
        }

        private void EvaluateCurrentReport()
        {
            bool attackRead = GetAnswer(PrototypeAcceptanceQuestion.AttackReadBeforeImpact);
            bool counterUnderstood = GetAnswer(PrototypeAcceptanceQuestion.CounterplayUnderstood);
            bool failureUnderstood = GetAnswer(PrototypeAcceptanceQuestion.FailureCauseUnderstood);
            bool controlsReliable = GetAnswer(PrototypeAcceptanceQuestion.ControlsFeltReliable);
            bool replayDesired = GetAnswer(PrototypeAcceptanceQuestion.WouldPlayAnotherRun);

            int readabilityYes = 0;
            readabilityYes += attackRead ? 1 : 0;
            readabilityYes += counterUnderstood ? 1 : 0;
            readabilityYes += failureUnderstood ? 1 : 0;
            currentReport.readabilityRatio = readabilityYes / 3f;
            currentReport.replayDesired = replayDesired;

            float maxBlockedRatio = config != null
                ? config.MaximumBlockedInputRatio
                : 0.20f;
            float maxBlockedSequence = config != null
                ? config.MaximumLongestBlockedSequence
                : 6f;

            currentReport.automatedEvidencePassed =
                currentReport.m40ReportFound &&
                currentReport.legacyTelemetryFound &&
                currentReport.m40Accepted &&
                currentReport.distinctOutcomeCount >=
                Mathf.Max(3, currentReport.requiredOutcomeCount);

            currentReport.controlReliabilityPassed =
                controlsReliable &&
                currentReport.blockedInputRatio <= maxBlockedRatio &&
                currentReport.longestBlockedInputSequence <= maxBlockedSequence;

            currentReport.readabilityPassed =
                attackRead &&
                counterUnderstood &&
                failureUnderstood;

            currentReport.runPassed =
                currentReport.automatedEvidencePassed &&
                currentReport.controlReliabilityPassed &&
                currentReport.readabilityPassed &&
                replayDesired;
        }

        private bool GetAnswer(PrototypeAcceptanceQuestion question)
        {
            if (currentReport == null || currentReport.answers == null)
            {
                return false;
            }

            for (int i = 0; i < currentReport.answers.Count; i++)
            {
                PrototypeAcceptanceAnswer answer = currentReport.answers[i];
                if (answer.question == question)
                {
                    return answer.answered && answer.value;
                }
            }

            return false;
        }

        private void SaveCurrentReport()
        {
            Directory.CreateDirectory(ReportDirectory);

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string id = !string.IsNullOrWhiteSpace(currentReport.reviewId)
                ? currentReport.reviewId.Substring(0, 8)
                : Guid.NewGuid().ToString("N").Substring(0, 8);
            string fileName = $"M41_Run_{timestamp}_{id}.json";
            currentRunReportPath = Path.Combine(ReportDirectory, fileName);

            File.WriteAllText(
                currentRunReportPath,
                JsonUtility.ToJson(currentReport, true));

            Debug.Log(
                $"[M41] Acceptance run report saved:\n{currentRunReportPath}",
                this);
        }

        private void RebuildAndSaveAggregate()
        {
            Directory.CreateDirectory(ReportDirectory);

            string[] runFiles = Directory.GetFiles(ReportDirectory, "M41_Run_*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToArray();

            var completed = new List<(PrototypeAcceptanceRunReport report, string path)>();
            for (int i = 0; i < runFiles.Length; i++)
            {
                try
                {
                    PrototypeAcceptanceRunReport report =
                        JsonUtility.FromJson<PrototypeAcceptanceRunReport>(
                            File.ReadAllText(runFiles[i]));
                    if (report != null && report.reviewCompleted)
                    {
                        completed.Add((report, runFiles[i]));
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"[M41] Run raporu okunamadı: {runFiles[i]}\n{exception.Message}",
                        this);
                }
            }

            int window = config != null ? config.EvaluationWindow : 3;
            int requiredPassing = config != null ? config.RequiredPassingRuns : 3;
            int take = Mathf.Min(window, completed.Count);

            aggregateReport = new PrototypeAcceptanceAggregateReport
            {
                utcUpdatedAt = DateTime.UtcNow.ToString("O"),
                availableCompletedRuns = completed.Count,
                evaluationWindow = window,
                evaluatedRuns = take,
                requiredPassingRuns = requiredPassing
            };

            float readabilitySum = 0f;
            float blockedRatioSum = 0f;

            for (int i = 0; i < take; i++)
            {
                PrototypeAcceptanceRunReport report = completed[i].report;
                string path = completed[i].path;

                aggregateReport.passingRuns += report.runPassed ? 1 : 0;
                aggregateReport.replayYesRuns += report.replayDesired ? 1 : 0;
                readabilitySum += report.readabilityRatio;
                blockedRatioSum += report.blockedInputRatio;

                aggregateReport.evaluatedRunSummaries.Add(
                    new PrototypeAcceptanceRunSummary
                    {
                        reviewId = report.reviewId,
                        utcFinishedAt = report.utcFinishedAt,
                        reviewCompleted = report.reviewCompleted,
                        runPassed = report.runPassed,
                        m40Accepted = report.m40Accepted,
                        readabilityRatio = report.readabilityRatio,
                        replayDesired = report.replayDesired,
                        blockedInputRatio = report.blockedInputRatio,
                        longestBlockedInputSequence = report.longestBlockedInputSequence,
                        reportPath = path
                    });
            }

            aggregateReport.replayYesRatio = take > 0
                ? aggregateReport.replayYesRuns / (float)take
                : 0f;
            aggregateReport.averageReadabilityRatio = take > 0
                ? readabilitySum / take
                : 0f;
            aggregateReport.averageBlockedInputRatio = take > 0
                ? blockedRatioSum / take
                : 0f;

            aggregateReport.enoughRuns = take >= window;
            aggregateReport.repeatedRunGatePassed =
                aggregateReport.enoughRuns &&
                aggregateReport.passingRuns >= requiredPassing;
            aggregateReport.replayGatePassed =
                aggregateReport.enoughRuns &&
                aggregateReport.replayYesRatio >=
                (config != null ? config.MinimumReplayYesRatio : 0.67f);
            aggregateReport.readabilityGatePassed =
                aggregateReport.enoughRuns &&
                aggregateReport.averageReadabilityRatio >=
                (config != null ? config.MinimumAverageReadabilityRatio : 0.80f);
            aggregateReport.networkSpikeCandidateReady =
                aggregateReport.repeatedRunGatePassed &&
                aggregateReport.replayGatePassed &&
                aggregateReport.readabilityGatePassed;

            aggregateReportPath = Path.Combine(ReportDirectory, AggregateFileName);
            File.WriteAllText(
                aggregateReportPath,
                JsonUtility.ToJson(aggregateReport, true));

            ApplyAggregateRuntimeState();

            Debug.Log(
                "[M41] Aggregate acceptance report updated. " +
                $"Passing={aggregateReport.passingRuns}/{aggregateReport.evaluatedRuns} | " +
                $"ReadyForNetworkSpikeCandidate={aggregateReport.networkSpikeCandidateReady}\n" +
                aggregateReportPath,
                this);
        }

        private void LoadAggregateIfPresent()
        {
            aggregateReportPath = Path.Combine(ReportDirectory, AggregateFileName);
            if (!File.Exists(aggregateReportPath))
            {
                aggregateReport = new PrototypeAcceptanceAggregateReport();
                ApplyAggregateRuntimeState();
                return;
            }

            try
            {
                aggregateReport =
                    JsonUtility.FromJson<PrototypeAcceptanceAggregateReport>(
                        File.ReadAllText(aggregateReportPath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[M41] Aggregate rapor okunamadı: {exception.Message}",
                    this);
                aggregateReport = new PrototypeAcceptanceAggregateReport();
            }

            ApplyAggregateRuntimeState();
        }

        private void ApplyAggregateRuntimeState()
        {
            networkSpikeCandidateReady =
                aggregateReport != null && aggregateReport.networkSpikeCandidateReady;
            aggregatePassingRuns = aggregateReport != null
                ? aggregateReport.passingRuns
                : 0;
            aggregateEvaluatedRuns = aggregateReport != null
                ? aggregateReport.evaluatedRuns
                : 0;
            aggregateRequiredRuns = config != null ? config.RequiredPassingRuns : 3;
        }

        private PrototypeOneVsOneRunReport LoadM40Report(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<PrototypeOneVsOneRunReport>(
                    File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[M41] M40 raporu okunamadı: {exception.Message}",
                    this);
                return null;
            }
        }

        private PrototypePlaytestReport LoadLegacyReport(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<PrototypePlaytestReport>(
                    File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[M41] Ayrıntılı telemetry raporu okunamadı: {exception.Message}",
                    this);
                return null;
            }
        }

        private string FindLatestLegacyTelemetryPath()
        {
            string directory = Path.Combine(
                Application.persistentDataPath,
                "PlaytestTelemetry");
            if (!Directory.Exists(directory))
            {
                return string.Empty;
            }

            string[] files = Directory.GetFiles(directory, "painted_alive_*.json");
            if (files.Length == 0)
            {
                return string.Empty;
            }

            DateTime earliestAllowed = runStartedUtc.AddSeconds(-2d);
            return files
                .Where(path => File.GetLastWriteTimeUtc(path) >= earliestAllowed)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault() ?? string.Empty;
        }

        private static string GetQuestionText(PrototypeAcceptanceQuestion question)
        {
            return question switch
            {
                PrototypeAcceptanceQuestion.AttackReadBeforeImpact =>
                    "Ressam saldırısını etkisi başlamadan önce okuyabildin mi?",
                PrototypeAcceptanceQuestion.CounterplayUnderstood =>
                    "Kullanabileceğin karşı hamleyi anlayabildin mi?",
                PrototypeAcceptanceQuestion.FailureCauseUnderstood =>
                    "Engellendiğinde veya başarısız olduğunda nedenini anlayabildin mi?",
                PrototypeAcceptanceQuestion.ControlsFeltReliable =>
                    "Figür ve Ressam kontrolleri güvenilir ve kasıtlı hissettirdi mi?",
                PrototypeAcceptanceQuestion.WouldPlayAnotherRun =>
                    "Bu prototipi hemen bir tur daha oynamak ister misin?",
                _ => "Yanıtlanmamış test sorusu."
            };
        }

        private void PublishSnapshot()
        {
            PrototypeMatchState state = matchController != null
                ? matchController.State
                : PrototypeMatchState.Waiting;

            currentSnapshot = new PrototypeAcceptanceSnapshot(
                state,
                collectingReports,
                reviewActive,
                reviewCompleted,
                currentQuestionIndex,
                Questions.Length,
                currentQuestionText,
                statusMessage,
                currentRunPassed,
                currentRunReportPath,
                networkSpikeCandidateReady,
                aggregatePassingRuns,
                aggregateEvaluatedRuns,
                aggregateRequiredRuns);

            SnapshotChanged?.Invoke(currentSnapshot);
        }

        [ContextMenu("Open M41 Acceptance Folder")]
        public void OpenAcceptanceFolder()
        {
            Directory.CreateDirectory(ReportDirectory);
            string normalizedPath = ReportDirectory.Replace("\\", "/");
            Application.OpenURL($"file:///{normalizedPath}");
        }
    }
}
