using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PaintedAlive.Core.Encounters;
using PaintedAlive.Core.Prototypes;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Core.Playtests
{
    [DisallowMultipleComponent]
    public sealed class PrototypeOneVsOnePlaytestSession : MonoBehaviour
    {
        private const string ReportFolderName = "PlaytestTelemetry/M40_OneVsOne";

        [Header("Existing Authoritative Systems")]
        [SerializeField] private PrototypeMatchController matchController;
        [SerializeField] private PrototypeRoleSwitcher roleSwitcher;
        [SerializeField] private FigureProgressTracker progressTracker;
        [SerializeField] private PrototypeJourneyScoreTracker scoreTracker;
        [SerializeField] private PrototypeMatchExpeditionBridge expeditionBridge;
        [SerializeField] private PrototypeEncounterRhythmDirector encounterDirector;
        [SerializeField] private PrototypeEncounterRhythmLedger encounterLedger;
        [SerializeField] private PrototypePlaytestTelemetry legacyTelemetry;

        [Header("Paint Evidence")]
        [SerializeField] private OilStrokeSystem strokeSystem;

        [Header("Configuration")]
        [SerializeField] private PrototypeOneVsOnePlaytestConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField] private int runNumber;
        [SerializeField] private int currentOutcomeIndex;
        [SerializeField] private int passedOutcomeCount;
        [SerializeField] private bool accepted;
        [SerializeField] private float runningStartedAtUnscaled;
        [SerializeField] private float runningElapsed;
        [SerializeField] private float nextEvidenceScanAt;
        [SerializeField] private int baselineStrokeCount;
        [SerializeField] private int baselineCutCount;
        [SerializeField] private int observedStrokeCount;
        [SerializeField] private int observedCutCount;
        [SerializeField] private int observedRampCount;
        [SerializeField] private bool fixativeEvidenceSeen;
        [SerializeField] private bool fractureEvidenceSeen;
        [SerializeField] private float rampContactStartProgress = -1f;
        [SerializeField] private string statusMessage;
        [SerializeField] private string lastReportPath;

        [SerializeField]
        private List<PrototypeOneVsOneOutcomeRecord> outcomes = new();

        [SerializeField]
        private List<string> visitedPhases = new();

        private readonly HashSet<string> visitedPhaseSet = new();
        private PrototypeOneVsOnePlaytestSnapshot currentSnapshot;
        private string runId;
        private DateTime utcStartedAt;
        private int encounterTransitionCount;
        private bool observedMatchStateEvent;

        public event Action<PrototypeOneVsOnePlaytestSnapshot> SnapshotChanged;

        public PrototypeMatchController MatchController => matchController;
        public PrototypeRoleSwitcher RoleSwitcher => roleSwitcher;
        public PrototypeOneVsOnePlaytestConfig Config => config;
        public IReadOnlyList<PrototypeOneVsOneOutcomeRecord> Outcomes => outcomes;
        public int CurrentOutcomeIndex => currentOutcomeIndex;
        public int PassedOutcomeCount => passedOutcomeCount;
        public bool Accepted => accepted;
        public float RunningElapsed => runningElapsed;
        public string StatusMessage => statusMessage;
        public string LastReportPath => lastReportPath;
        public PrototypeOneVsOnePlaytestSnapshot CurrentSnapshot => currentSnapshot;

        public void Configure(
            PrototypeMatchController authoritativeMatch,
            PrototypeRoleSwitcher authoritativeRoleSwitcher,
            FigureProgressTracker authoritativeProgress,
            PrototypeJourneyScoreTracker journeyScore,
            PrototypeMatchExpeditionBridge expeditionResultBridge,
            PrototypeEncounterRhythmDirector rhythmDirector,
            PrototypeEncounterRhythmLedger rhythmLedger,
            PrototypePlaytestTelemetry telemetry,
            OilStrokeSystem oilStrokeSystem,
            PrototypeOneVsOnePlaytestConfig playtestConfig)
        {
            matchController = authoritativeMatch;
            roleSwitcher = authoritativeRoleSwitcher;
            progressTracker = authoritativeProgress;
            scoreTracker = journeyScore;
            expeditionBridge = expeditionResultBridge;
            encounterDirector = rhythmDirector;
            encounterLedger = rhythmLedger;
            legacyTelemetry = telemetry;
            strokeSystem = oilStrokeSystem;
            config = playtestConfig;
        }

        private void Awake()
        {
            ResolveDependencies();
            EnsureOutcomeRecords();
            PublishSnapshot();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (matchController != null)
            {
                matchController.StateChanged += HandleMatchStateChanged;
            }

            PrototypeEncounterRhythmEventHub.Transitioned += HandleEncounterTransition;
        }

        private void OnDisable()
        {
            if (matchController != null)
            {
                matchController.StateChanged -= HandleMatchStateChanged;
            }

            PrototypeEncounterRhythmEventHub.Transitioned -= HandleEncounterTransition;
        }

        private void Start()
        {
            ValidateDependencies();

            if (matchController != null && !observedMatchStateEvent)
            {
                HandleMatchStateChanged(matchController.State);
            }
        }

        private void Update()
        {
            if (matchController == null ||
                matchController.State != PrototypeMatchState.Running)
            {
                return;
            }

            runningElapsed = Mathf.Max(
                0f,
                Time.unscaledTime - runningStartedAtUnscaled);

            UpdateCurrentOutcomeFromEncounter();
            HandleManualEvidenceInput();

            if (Time.unscaledTime >= nextEvidenceScanAt)
            {
                nextEvidenceScanAt = Time.unscaledTime +
                    (config != null ? config.EvidenceScanInterval : 0.15f);
                ScanAutomaticEvidence();
            }

            PublishSnapshot();
        }

        private void ResolveDependencies()
        {
            matchController ??= GetComponent<PrototypeMatchController>();
            roleSwitcher ??= FindFirstObjectByType<PrototypeRoleSwitcher>();
            progressTracker ??= FindFirstObjectByType<FigureProgressTracker>();
            scoreTracker ??= FindFirstObjectByType<PrototypeJourneyScoreTracker>();
            expeditionBridge ??= GetComponent<PrototypeMatchExpeditionBridge>();
            encounterDirector ??= GetComponent<PrototypeEncounterRhythmDirector>();
            encounterLedger ??= GetComponent<PrototypeEncounterRhythmLedger>();
            legacyTelemetry ??= GetComponent<PrototypePlaytestTelemetry>();
            strokeSystem ??= FindFirstObjectByType<OilStrokeSystem>();
        }

        private void HandleMatchStateChanged(PrototypeMatchState state)
        {
            observedMatchStateEvent = true;

            switch (state)
            {
                case PrototypeMatchState.Countdown:
                    ResetForNewRun();
                    break;

                case PrototypeMatchState.Running:
                    BeginRunningRun();
                    break;

                case PrototypeMatchState.FigureEscaped:
                case PrototypeMatchState.TimeExpired:
                    FinalizeRun(state);
                    break;
            }

            PublishSnapshot();
        }

        private void ResetForNewRun()
        {
            runNumber++;
            runId = Guid.NewGuid().ToString("N");
            utcStartedAt = DateTime.UtcNow;
            currentOutcomeIndex = 0;
            passedOutcomeCount = 0;
            accepted = false;
            runningStartedAtUnscaled = 0f;
            runningElapsed = 0f;
            nextEvidenceScanAt = 0f;
            baselineStrokeCount = CountStrokes();
            baselineCutCount = CountCuts();
            observedStrokeCount = baselineStrokeCount;
            observedCutCount = baselineCutCount;
            observedRampCount = CountRamps();
            fixativeEvidenceSeen = false;
            fractureEvidenceSeen = false;
            rampContactStartProgress = -1f;
            encounterTransitionCount = 0;
            statusMessage = "1v1 test protokolü geri sayımdan sonra başlayacak.";
            lastReportPath = string.Empty;
            visitedPhases.Clear();
            visitedPhaseSet.Clear();
            EnsureOutcomeRecords(reset: true);
        }

        private void BeginRunningRun()
        {
            if (string.IsNullOrEmpty(runId))
            {
                ResetForNewRun();
            }

            runningStartedAtUnscaled = Time.unscaledTime;
            runningElapsed = 0f;
            baselineStrokeCount = CountStrokes();
            baselineCutCount = CountCuts();
            nextEvidenceScanAt = Time.unscaledTime;
            statusMessage = "Düğüm 1: boya yükselmeden geç veya saldırıyı araç kullanmadan boşa çıkar.";
        }

        private void HandleEncounterTransition(PrototypeEncounterRhythmSnapshot snapshot)
        {
            if (matchController == null ||
                matchController.State != PrototypeMatchState.Running ||
                snapshot.Phase == PrototypeEncounterPhase.Inactive)
            {
                return;
            }

            encounterTransitionCount++;
            string phaseName = snapshot.Phase.ToString();
            if (visitedPhaseSet.Add(phaseName))
            {
                visitedPhases.Add(phaseName);
            }

            UpdateCurrentOutcomeFromEncounter();
        }

        private void UpdateCurrentOutcomeFromEncounter()
        {
            if (encounterDirector == null)
            {
                currentOutcomeIndex = FindFirstPendingOutcomeIndex();
                return;
            }

            int targetIndex = encounterDirector.CurrentEncounterIndex switch
            {
                <= 1 => 0,
                2 => 1,
                _ => 2
            };

            if (outcomes[targetIndex].status == PrototypeOneVsOneOutcomeStatus.Pending)
            {
                currentOutcomeIndex = targetIndex;
            }
            else
            {
                currentOutcomeIndex = FindFirstPendingOutcomeIndex();
            }
        }

        private void HandleManualEvidenceInput()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                if (roleSwitcher != null &&
                    roleSwitcher.CurrentRole != PrototypeRole.Figure)
                {
                    statusMessage = "F11 sonucu yalnız Figür rolündeyken kaydedilir.";
                    return;
                }

                MarkOutcome(
                    currentOutcomeIndex,
                    PrototypeOneVsOneOutcomeStatus.Passed,
                    "Oyuncu tarafından F11 ile gözlemlenmiş başarılı doğal sonuç olarak doğrulandı.");
            }

            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                MarkOutcome(
                    currentOutcomeIndex,
                    PrototypeOneVsOneOutcomeStatus.Failed,
                    "Oyuncu tarafından F12 ile başarısız/okunamaz sonuç olarak işaretlendi.");
            }
        }

        private void ScanAutomaticEvidence()
        {
            observedStrokeCount = CountStrokes();
            observedCutCount = CountCuts();
            observedRampCount = CountRamps();

            DetectEarlyPassEvidence();
            DetectPaletteKnifeEvidence();
            DetectFixativeFractureAndRampEvidence();
        }

        private void DetectEarlyPassEvidence()
        {
            if (!IsPending(0) || progressTracker == null)
            {
                return;
            }

            float threshold = config != null
                ? config.EarlyPassProgressThreshold
                : 0.28f;

            bool painterCreatedObstacle = observedStrokeCount > baselineStrokeCount;
            bool noToolCutUsed = observedCutCount <= baselineCutCount;
            bool crossedFirstEncounter = progressTracker.NormalizedProgress >= threshold;

            if (painterCreatedObstacle && noToolCutUsed && crossedFirstEncounter)
            {
                AutoMarkOutcome(
                    0,
                    "İlk encounter geçildi; Painter stroke oluşturdu ve Palet Bıçağı kesimi kullanılmadı.");
            }
        }

        private void DetectPaletteKnifeEvidence()
        {
            if (!IsPending(1) || observedCutCount <= baselineCutCount)
            {
                return;
            }

            AutoMarkOutcome(
                1,
                $"OilStrokeRuntime.CutCount arttı ({baselineCutCount} → {observedCutCount}).");
        }

        private void DetectFixativeFractureAndRampEvidence()
        {
            if (!IsPending(2) || strokeSystem == null)
            {
                return;
            }

            Transform figureTransform = scoreTracker != null && scoreTracker.Figure != null
                ? scoreTracker.Figure.transform
                : null;

            bool rampContact = false;

            foreach (OilStrokeRuntime stroke in strokeSystem.Strokes)
            {
                if (stroke == null)
                {
                    continue;
                }

                InspectStrokeComponents(stroke);

                if (!string.Equals(stroke.Shape.ToString(), "Ramp", StringComparison.Ordinal))
                {
                    continue;
                }

                Renderer renderer = stroke.GetComponent<Renderer>();
                if (renderer == null || figureTransform == null)
                {
                    continue;
                }

                Bounds expanded = renderer.bounds;
                expanded.Expand(new Vector3(1.2f, 1.6f, 1.2f));
                if (!expanded.Contains(figureTransform.position))
                {
                    continue;
                }

                rampContact = true;
                float progress = progressTracker != null
                    ? progressTracker.NormalizedProgress
                    : 0f;

                if (rampContactStartProgress < 0f)
                {
                    rampContactStartProgress = progress;
                }

                float requiredDelta = config != null
                    ? config.RampProgressDelta
                    : 0.025f;

                if (progress >= rampContactStartProgress + requiredDelta)
                {
                    AutoMarkOutcome(
                        2,
                        "Figür Ramp stroke bounds içinde ilerleme kazandı; rampa traversal kanıtı oluştu.");
                    return;
                }
            }

            if (!rampContact)
            {
                rampContactStartProgress = -1f;
            }

            if (fixativeEvidenceSeen && fractureEvidenceSeen)
            {
                AutoMarkOutcome(
                    2,
                    "Aynı koşuda Sabitleyici dozu ve fiziksel stroke kırılması gözlendi.");
            }
        }

        private void InspectStrokeComponents(OilStrokeRuntime stroke)
        {
            MonoBehaviour[] components = stroke.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null)
                {
                    continue;
                }

                Type type = component.GetType();
                string typeName = type.Name;

                if (typeName.IndexOf("Fixative", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    HasPositiveFixativeEvidence(component, type))
                {
                    fixativeEvidenceSeen = true;
                }

                if (typeName.IndexOf("StructuralIntegrity", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    ReadBooleanMember(component, type, "Fractured"))
                {
                    fractureEvidenceSeen = true;
                }
            }
        }

        private static bool HasPositiveFixativeEvidence(object instance, Type type)
        {
            string[] numericMembers =
            {
                "Saturation",
                "CurrentSaturation",
                "Dose",
                "AppliedDose",
                "FixativeAmount"
            };

            for (int i = 0; i < numericMembers.Length; i++)
            {
                if (TryReadNumericMember(instance, type, numericMembers[i], out float value) &&
                    value > 0.001f)
                {
                    return true;
                }
            }

            string[] booleanMembers =
            {
                "IsFixed",
                "Applied",
                "HasFixative",
                "IsSaturated"
            };

            for (int i = 0; i < booleanMembers.Length; i++)
            {
                if (ReadBooleanMember(instance, type, booleanMembers[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadNumericMember(
            object instance,
            Type type,
            string memberName,
            out float value)
        {
            const BindingFlags Flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            PropertyInfo property = type.GetProperty(memberName, Flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                object propertyValue = property.GetValue(instance);
                if (TryConvertToFloat(propertyValue, out value))
                {
                    return true;
                }
            }

            FieldInfo field = type.GetField(memberName, Flags);
            if (field != null && TryConvertToFloat(field.GetValue(instance), out value))
            {
                return true;
            }

            value = 0f;
            return false;
        }

        private static bool ReadBooleanMember(object instance, Type type, string memberName)
        {
            const BindingFlags Flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            PropertyInfo property = type.GetProperty(memberName, Flags);
            if (property != null &&
                property.PropertyType == typeof(bool) &&
                property.GetIndexParameters().Length == 0)
            {
                return (bool)property.GetValue(instance);
            }

            FieldInfo field = type.GetField(memberName, Flags);
            return field != null &&
                field.FieldType == typeof(bool) &&
                (bool)field.GetValue(instance);
        }

        private static bool TryConvertToFloat(object value, out float result)
        {
            switch (value)
            {
                case float single:
                    result = single;
                    return true;
                case double doubleValue:
                    result = (float)doubleValue;
                    return true;
                case int integer:
                    result = integer;
                    return true;
                default:
                    result = 0f;
                    return false;
            }
        }

        private void AutoMarkOutcome(int index, string evidence)
        {
            if (config != null && !config.AutoConfirmDetectedEvidence)
            {
                statusMessage = $"Otomatik kanıt bulundu: {evidence} F11 ile doğrula.";
                currentOutcomeIndex = index;
                return;
            }

            MarkOutcome(index, PrototypeOneVsOneOutcomeStatus.Passed, evidence);
        }

        private void MarkOutcome(
            int index,
            PrototypeOneVsOneOutcomeStatus status,
            string evidence)
        {
            if (index < 0 || index >= outcomes.Count)
            {
                return;
            }

            PrototypeOneVsOneOutcomeRecord record = outcomes[index];
            if (record.status == PrototypeOneVsOneOutcomeStatus.Passed &&
                status != PrototypeOneVsOneOutcomeStatus.Passed)
            {
                return;
            }

            record.status = status;
            record.evidence = evidence;
            record.matchTime = runningElapsed;
            record.encounterIndex = encounterDirector != null
                ? encounterDirector.CurrentEncounterIndex
                : index + 1;

            RecalculateAcceptance();
            currentOutcomeIndex = FindFirstPendingOutcomeIndex();
            statusMessage = status == PrototypeOneVsOneOutcomeStatus.Passed
                ? $"Kanıt kaydedildi: {GetOutcomeTitle(record.outcome)}"
                : $"Yeniden test gerekli: {GetOutcomeTitle(record.outcome)}";

            if (config == null || config.LogOutcomeChanges)
            {
                Debug.Log(
                    $"[M40] Outcome={record.outcome} | Status={record.status} | " +
                    $"Time={record.matchTime:0.00} | Evidence={record.evidence}",
                    this);
            }

            PublishSnapshot();
        }

        private void RecalculateAcceptance()
        {
            passedOutcomeCount = 0;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i].status == PrototypeOneVsOneOutcomeStatus.Passed)
                {
                    passedOutcomeCount++;
                }
            }

            int required = config != null ? config.RequiredDistinctOutcomes : 3;
            accepted = passedOutcomeCount >= required;
        }

        private void FinalizeRun(PrototypeMatchState finalState)
        {
            if (string.IsNullOrEmpty(runId))
            {
                return;
            }

            runningElapsed = runningStartedAtUnscaled > 0f
                ? Mathf.Max(0f, Time.unscaledTime - runningStartedAtUnscaled)
                : 0f;

            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i].status == PrototypeOneVsOneOutcomeStatus.Pending)
                {
                    outcomes[i].status = PrototypeOneVsOneOutcomeStatus.Failed;
                    outcomes[i].evidence = "Koşu sona erdiğinde bu doğal sonuç doğrulanmamıştı.";
                    outcomes[i].matchTime = runningElapsed;
                }
            }

            RecalculateAcceptance();
            statusMessage = accepted
                ? "M40 KABUL: Üç farklı doğal karşı sonuç aynı koşuda doğrulandı."
                : "M40 TEKRAR TEST: Üç farklı doğal sonuç henüz doğrulanmadı.";

            if (config == null || config.WriteJsonReport)
            {
                SaveReport(finalState);
            }

            PublishSnapshot();
        }

        private void SaveReport(PrototypeMatchState finalState)
        {
            PrototypeOneVsOneRunReport report = new PrototypeOneVsOneRunReport
            {
                runId = runId,
                runNumber = runNumber,
                utcStartedAt = utcStartedAt.ToString("O"),
                utcFinishedAt = DateTime.UtcNow.ToString("O"),
                finalMatchState = finalState.ToString(),
                accepted = accepted,
                requiredOutcomeCount = config != null ? config.RequiredDistinctOutcomes : 3,
                passedOutcomeCount = passedOutcomeCount,
                configuredDuration = config != null ? config.ExpectedMatchDuration : 300f,
                actualRunningDuration = runningElapsed,
                remainingTime = matchController != null ? matchController.TimeRemaining : 0f,
                finalJourneyScore = scoreTracker != null ? scoreTracker.TotalScore : 0,
                normalFigureExit = scoreTracker != null && scoreTracker.NormalExitCompleted,
                stainArrivalDuringRun = expeditionBridge != null && expeditionBridge.StainArrivalDuringRun,
                legacyTelemetryPresent = legacyTelemetry != null,
                strokeCount = observedStrokeCount,
                cutCount = observedCutCount,
                rampCount = observedRampCount,
                encounterTransitionCount = encounterTransitionCount,
                visitedPhases = new List<string>(visitedPhases),
                outcomes = CloneOutcomeRecords()
            };

            string directory = Path.Combine(
                Application.persistentDataPath,
                ReportFolderName);
            Directory.CreateDirectory(directory);

            string fileName =
                $"painted_alive_m40_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{runId.Substring(0, 8)}.json";
            lastReportPath = Path.Combine(directory, fileName);
            File.WriteAllText(lastReportPath, JsonUtility.ToJson(report, true));

            Debug.Log(
                $"[M40] 1v1 playtest report saved:\n{lastReportPath}\n" +
                $"Accepted={accepted} | Outcomes={passedOutcomeCount}/{report.requiredOutcomeCount}",
                this);
        }

        private List<PrototypeOneVsOneOutcomeRecord> CloneOutcomeRecords()
        {
            List<PrototypeOneVsOneOutcomeRecord> copy = new(outcomes.Count);
            for (int i = 0; i < outcomes.Count; i++)
            {
                PrototypeOneVsOneOutcomeRecord source = outcomes[i];
                copy.Add(new PrototypeOneVsOneOutcomeRecord
                {
                    outcome = source.outcome,
                    status = source.status,
                    evidence = source.evidence,
                    matchTime = source.matchTime,
                    encounterIndex = source.encounterIndex
                });
            }

            return copy;
        }

        private int CountStrokes()
        {
            return strokeSystem != null ? strokeSystem.Strokes.Count : 0;
        }

        private int CountCuts()
        {
            if (strokeSystem == null)
            {
                return 0;
            }

            int count = 0;
            foreach (OilStrokeRuntime stroke in strokeSystem.Strokes)
            {
                if (stroke != null)
                {
                    count += stroke.CutCount;
                }
            }

            return count;
        }

        private int CountRamps()
        {
            if (strokeSystem == null)
            {
                return 0;
            }

            int count = 0;
            foreach (OilStrokeRuntime stroke in strokeSystem.Strokes)
            {
                if (stroke != null &&
                    string.Equals(stroke.Shape.ToString(), "Ramp", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsPending(int index)
        {
            return index >= 0 &&
                index < outcomes.Count &&
                outcomes[index].status == PrototypeOneVsOneOutcomeStatus.Pending;
        }

        private int FindFirstPendingOutcomeIndex()
        {
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i].status == PrototypeOneVsOneOutcomeStatus.Pending)
                {
                    return i;
                }
            }

            return Mathf.Clamp(currentOutcomeIndex, 0, outcomes.Count - 1);
        }

        private void EnsureOutcomeRecords(bool reset = false)
        {
            if (!reset && outcomes.Count == 3)
            {
                return;
            }

            outcomes.Clear();
            outcomes.Add(CreateOutcome(PrototypeOneVsOneOutcomeKind.EarlyPassOrAvoidance));
            outcomes.Add(CreateOutcome(PrototypeOneVsOneOutcomeKind.PaletteKnifeCut));
            outcomes.Add(CreateOutcome(PrototypeOneVsOneOutcomeKind.FixativeBreakOrRampUse));
        }

        private static PrototypeOneVsOneOutcomeRecord CreateOutcome(
            PrototypeOneVsOneOutcomeKind outcome)
        {
            return new PrototypeOneVsOneOutcomeRecord
            {
                outcome = outcome,
                status = PrototypeOneVsOneOutcomeStatus.Pending,
                evidence = string.Empty,
                matchTime = 0f,
                encounterIndex = 0
            };
        }

        public static string GetOutcomeTitle(PrototypeOneVsOneOutcomeKind outcome)
        {
            return outcome switch
            {
                PrototypeOneVsOneOutcomeKind.EarlyPassOrAvoidance =>
                    "Boya yükselmeden geç / araçsız kaçın",
                PrototypeOneVsOneOutcomeKind.PaletteKnifeCut =>
                    "Palet Bıçağıyla geçit aç",
                PrototypeOneVsOneOutcomeKind.FixativeBreakOrRampUse =>
                    "Sabitleyip kır veya rampayı kullan",
                _ => outcome.ToString()
            };
        }

        public string GetCurrentPrompt()
        {
            if (outcomes.Count == 0)
            {
                return string.Empty;
            }

            PrototypeOneVsOneOutcomeKind kind =
                outcomes[Mathf.Clamp(currentOutcomeIndex, 0, outcomes.Count - 1)].outcome;

            return kind switch
            {
                PrototypeOneVsOneOutcomeKind.EarlyPassOrAvoidance =>
                    "Painter bir engel kursun. Figür duvar yükselmeden geçsin veya hamleyi araç kullanmadan boşa çıkarsın.",
                PrototypeOneVsOneOutcomeKind.PaletteKnifeCut =>
                    "Painter geçidi kapatsın. Figür Palet Bıçağıyla gerçek bir boşluk açıp rotaya devam etsin.",
                PrototypeOneVsOneOutcomeKind.FixativeBreakOrRampUse =>
                    "Figür Sabitleyiciyle boyayı erken kurutup kırmalı veya Painter rampasını kendi kestirmesine çevirmeli.",
                _ => string.Empty
            };
        }

        private void PublishSnapshot()
        {
            PrototypeMatchState state = matchController != null
                ? matchController.State
                : PrototypeMatchState.Waiting;
            int required = config != null ? config.RequiredDistinctOutcomes : 3;

            currentSnapshot = new PrototypeOneVsOnePlaytestSnapshot(
                state,
                runNumber,
                currentOutcomeIndex,
                passedOutcomeCount,
                required,
                accepted,
                runningElapsed,
                statusMessage,
                lastReportPath);

            SnapshotChanged?.Invoke(currentSnapshot);
        }

        [ContextMenu("Open M40 Report Folder")]
        public void OpenReportFolder()
        {
            string directory = Path.Combine(
                Application.persistentDataPath,
                ReportFolderName);
            Directory.CreateDirectory(directory);
            Application.OpenURL($"file:///{directory.Replace("\\", "/")}");
        }

        private void ValidateDependencies()
        {
            if (matchController == null)
            {
                Debug.LogError("[M40] PrototypeMatchController eksik.", this);
            }

            if (encounterDirector == null)
            {
                Debug.LogError("[M40] M39 PrototypeEncounterRhythmDirector eksik.", this);
            }

            if (scoreTracker == null)
            {
                Debug.LogError("[M40] M37 PrototypeJourneyScoreTracker eksik.", this);
            }

            if (strokeSystem == null)
            {
                Debug.LogError("[M40] OilStrokeSystem eksik.", this);
            }

            if (legacyTelemetry == null)
            {
                Debug.LogWarning(
                    "[M40] Eski PrototypePlaytestTelemetry bulunamadı. " +
                    "M40 kendi kabul raporunu kaydeder ancak ayrıntılı eski telemetry JSON'u oluşmaz.",
                    this);
            }
        }
    }
}
