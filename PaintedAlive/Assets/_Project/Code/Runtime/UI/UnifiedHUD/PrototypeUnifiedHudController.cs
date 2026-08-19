using UnityEngine;

namespace PaintedAlive.UI.UnifiedHUD
{
    [DisallowMultipleComponent]
    public sealed class PrototypeUnifiedHudController : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private PrototypeUnifiedHudView view;

        [Header("Existing Runtime Sources")]
        [SerializeField] private Component matchController;
        [SerializeField] private Component roleSwitcher;
        [SerializeField] private Component figureMotor;
        [SerializeField] private Component clarityState;
        [SerializeField] private Component pigmentReservoir;
        [SerializeField] private Component progressTracker;
        [SerializeField] private Component journeyScore;
        [SerializeField] private Component encounterDirector;
        [SerializeField] private Component figureToolSource;
        [SerializeField] private Component painterSource;

        [Header("Sampling")]
        [SerializeField, Min(0.02f)] private float refreshInterval = 0.10f;

        [Header("Runtime Read Only")]
        [SerializeField] private string resolvedRole = "Figure";
        [SerializeField] private string resolvedMatchState = "Waiting";
        [SerializeField] private bool dataSourcesResolved;
        [SerializeField] private bool authoritativeClarityBound;
        [SerializeField] private string claritySourcePath = "MISSING";

        private float nextRefreshAt;

        public bool DataSourcesResolved => dataSourcesResolved;
        public string ResolvedRole => resolvedRole;
        public string ResolvedMatchState => resolvedMatchState;
        public bool AuthoritativeClarityBound => authoritativeClarityBound;
        public string ClaritySourcePath => claritySourcePath;

        public void Configure(
            PrototypeUnifiedHudView configuredView,
            Component configuredMatchController,
            Component configuredRoleSwitcher,
            Component configuredClarityState,
            Component configuredPigmentReservoir,
            Component configuredProgressTracker,
            Component configuredJourneyScore,
            Component configuredEncounterDirector,
            Component configuredFigureToolSource,
            Component configuredPainterSource)
        {
            view = configuredView;
            matchController = configuredMatchController;
            roleSwitcher = configuredRoleSwitcher;
            clarityState = configuredClarityState;
            pigmentReservoir = configuredPigmentReservoir;
            progressTracker = configuredProgressTracker;
            journeyScore = configuredJourneyScore;
            encounterDirector = configuredEncounterDirector;
            figureToolSource = configuredFigureToolSource;
            painterSource = configuredPainterSource;

            ResolveAuthoritativeFigureBindings();

            dataSourcesResolved =
                matchController != null &&
                roleSwitcher != null &&
                clarityState != null &&
                pigmentReservoir != null &&
                progressTracker != null;
        }

        private void Awake()
        {
            ResolveMissingSources();
        }

        private void OnEnable()
        {
            nextRefreshAt = 0f;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = Time.unscaledTime + refreshInterval;

            if (!dataSourcesResolved)
            {
                ResolveMissingSources();
            }

            if (view != null)
            {
                view.Apply(BuildSnapshot());
            }
        }

        [ContextMenu("Resolve Missing HUD Sources")]
        public void ResolveMissingSources()
        {
            matchController ??= PrototypeUnifiedHudReflection.FindSceneComponentByTypeName(
                "PaintedAlive.Core.Prototypes.PrototypeMatchController",
                "PrototypeMatchController");

            roleSwitcher ??= PrototypeUnifiedHudReflection.FindSceneComponentByTypeName(
                "PaintedAlive.Core.Prototypes.PrototypeRoleSwitcher",
                "PrototypeRoleSwitcher");

            pigmentReservoir ??= PrototypeUnifiedHudReflection.FindSceneComponentByTypeName(
                "PaintedAlive.Painters.PainterPigmentReservoir",
                "PainterPigmentReservoir");

            progressTracker ??= PrototypeUnifiedHudReflection.FindSceneComponentByTypeName(
                "PaintedAlive.Core.Prototypes.FigureProgressTracker",
                "FigureProgressTracker");

            journeyScore ??= PrototypeUnifiedHudReflection.FindSceneComponentContaining(
                "JourneyProgressScore",
                "JourneyScore",
                "ProgressScore");

            encounterDirector ??= PrototypeUnifiedHudReflection.FindSceneComponentByTypeName(
                "PaintedAlive.Core.Prototypes.PrototypeEncounterRhythmDirector",
                "PrototypeEncounterRhythmDirector");

            figureToolSource ??= PrototypeUnifiedHudReflection.FindSceneComponentContaining(
                "FigureTool",
                "PaletteKnife",
                "Fixative");

            painterSource ??= PrototypeUnifiedHudReflection.FindSceneComponentByTypeName(
                "PaintedAlive.Painters.PainterBrushController",
                "PainterBrushController");

            ResolveAuthoritativeFigureBindings();

            dataSourcesResolved =
                matchController != null &&
                roleSwitcher != null &&
                clarityState != null &&
                pigmentReservoir != null &&
                progressTracker != null;
        }

        private void ResolveAuthoritativeFigureBindings()
        {
            figureMotor ??=
                PrototypeUnifiedHudReflection.FindSceneComponentByTypeName(
                    "PaintedAlive.Figures.FigureMotor",
                    "FigureMotor");

            Component authoritativeClarity =
                PrototypeUnifiedHudReflection.FindRelatedComponentByTypeName(
                    figureMotor,
                    "PaintedAlive.Figures.FigureClarityState",
                    "FigureClarityState");

            if (authoritativeClarity != null)
            {
                clarityState = authoritativeClarity;
            }
            else
            {
                clarityState ??=
                    PrototypeUnifiedHudReflection.FindSceneComponentByTypeName(
                        "PaintedAlive.Figures.FigureClarityState",
                        "FigureClarityState");
            }

            Component authoritativeTool =
                PrototypeUnifiedHudReflection.FindRelatedComponentContaining(
                    figureMotor,
                    "FigureToolLoadout",
                    "FigureTool",
                    "PaletteKnife",
                    "Fixative");

            if (authoritativeTool != null)
            {
                figureToolSource = authoritativeTool;
            }

            claritySourcePath =
                PrototypeUnifiedHudReflection.GetHierarchyPath(clarityState);

            authoritativeClarityBound =
                figureMotor != null &&
                clarityState != null &&
                string.Equals(
                    PrototypeUnifiedHudReflection.GetHierarchyPath(figureMotor),
                    PrototypeUnifiedHudReflection.GetHierarchyPath(clarityState),
                    System.StringComparison.Ordinal);
        }

        public PrototypeUnifiedHudSnapshot BuildSnapshot()
        {
            string role = PrototypeUnifiedHudReflection.ReadString(
                roleSwitcher,
                "Figure",
                "CurrentRole",
                "Role",
                "currentRole",
                "activeRole");

            string matchState = PrototypeUnifiedHudReflection.ReadString(
                matchController,
                "Waiting",
                "State",
                "CurrentState",
                "state",
                "currentState");

            float remainingSeconds = PrototypeUnifiedHudReflection.ReadFloat(
                matchController,
                0f,
                "RemainingTime",
                "TimeRemaining",
                "RemainingSeconds",
                "MatchTimeRemaining",
                "remainingTime",
                "timeRemaining",
                "runningRemaining");

            float clarity = PrototypeUnifiedHudReflection.ReadFloat(
                clarityState,
                100f,
                "CurrentClarity",
                "currentClarity");

            float maximumClarity = PrototypeUnifiedHudReflection.ReadFloat(
                clarityState,
                100f,
                "MaximumClarity",
                "MaxClarity",
                "maximumClarity");

            float normalizedClarity = PrototypeUnifiedHudReflection.ReadFloat(
                clarityState,
                maximumClarity > 0f ? clarity / maximumClarity : 0f,
                "NormalizedClarity",
                "ClarityNormalized",
                "normalizedClarity");

            string clarityLevel = PrototypeUnifiedHudReflection.ReadString(
                clarityState,
                "Clean",
                "CurrentLevel",
                "ClarityLevel",
                "currentLevel");

            float pigment = PrototypeUnifiedHudReflection.ReadFloat(
                pigmentReservoir,
                0f,
                "Current",
                "CurrentPigment",
                "Pigment",
                "currentPigment",
                "current");

            float maximumPigment = PrototypeUnifiedHudReflection.ReadFloat(
                pigmentReservoir,
                100f,
                "Capacity",
                "Maximum",
                "MaximumPigment",
                "MaxPigment",
                "capacity",
                "maximumPigment");

            float normalizedPigment = PrototypeUnifiedHudReflection.ReadFloat(
                pigmentReservoir,
                maximumPigment > 0f ? pigment / maximumPigment : 0f,
                "Normalized",
                "NormalizedPigment",
                "PigmentNormalized");

            float normalizedProgress = PrototypeUnifiedHudReflection.ReadFloat(
                progressTracker,
                0f,
                "NormalizedProgress",
                "normalizedProgress");

            float furthestDistance = PrototypeUnifiedHudReflection.ReadFloat(
                progressTracker,
                0f,
                "FurthestDistance",
                "furthestDistance");

            int score = PrototypeUnifiedHudReflection.ReadInt(
                journeyScore,
                Mathf.RoundToInt(normalizedProgress * 1000f),
                "TotalScore",
                "Score",
                "CurrentScore",
                "totalScore",
                "score");

            string encounterPhase = PrototypeUnifiedHudReflection.ReadString(
                encounterDirector,
                string.Empty,
                "CurrentPhase",
                "Phase",
                "currentPhase",
                "phase");

            int encounterIndex = PrototypeUnifiedHudReflection.ReadInt(
                encounterDirector,
                0,
                "CurrentEncounter",
                "EncounterIndex",
                "currentEncounter",
                "encounterIndex");

            bool canSprint = PrototypeUnifiedHudReflection.ReadBool(
                clarityState,
                true,
                "CanSprint",
                "canSprint");

            bool canUsePrimaryTool = PrototypeUnifiedHudReflection.ReadBool(
                clarityState,
                true,
                "CanUsePrimaryTool",
                "canUsePrimaryTool");

            string figureTool = PrototypeUnifiedHudReflection.ReadString(
                figureToolSource,
                string.Empty,
                "CurrentTool",
                "ActiveTool",
                "SelectedTool",
                "currentTool",
                "activeTool");

            if (!canUsePrimaryTool)
            {
                figureTool =
                    $"KAPALI • {clarityLevel.ToUpperInvariant()}";
            }

            string painterMaterial = PrototypeUnifiedHudReflection.ReadString(
                painterSource,
                "Yağlı Boya",
                "CurrentMaterial",
                "ActiveMaterial",
                "ActivePreviewShape",
                "activeShape");

            string context = BuildContextStatus(
                role,
                matchState,
                clarityLevel,
                normalizedClarity,
                normalizedPigment,
                canSprint,
                canUsePrimaryTool);

            resolvedRole = role;
            resolvedMatchState = matchState;

            return new PrototypeUnifiedHudSnapshot
            {
                Role = role,
                MatchState = matchState,
                RemainingSeconds = remainingSeconds,

                Clarity = clarity,
                MaximumClarity = maximumClarity,
                NormalizedClarity = normalizedClarity,
                ClarityLevel = clarityLevel,

                Pigment = pigment,
                MaximumPigment = maximumPigment,
                NormalizedPigment = normalizedPigment,

                NormalizedProgress = normalizedProgress,
                FurthestDistance = furthestDistance,
                JourneyScore = score,

                EncounterPhase = encounterPhase,
                EncounterIndex = encounterIndex,

                FigureTool = figureTool,
                PainterMaterial = painterMaterial,
                ContextStatus = context
            };
        }

        private static string BuildContextStatus(
            string role,
            string matchState,
            string clarityLevel,
            float normalizedClarity,
            float normalizedPigment,
            bool canSprint,
            bool canUsePrimaryTool)
        {
            if (matchState == "Countdown")
            {
                return "ROLLERİNİ KONTROL ET • KOŞU BAŞLAMAK ÜZERE";
            }

            if (matchState == "FigureEscaped")
            {
                return "ÇIKIŞ TAMAMLANDI • ENTER İLE YENİ KOŞU";
            }

            if (matchState == "TimeExpired")
            {
                return "SÜRE DOLDU • ENTER İLE YENİ KOŞU";
            }

            bool isPainter =
                string.Equals(role, "Painter", System.StringComparison.OrdinalIgnoreCase);

            if (isPainter && normalizedPigment <= 0.15f)
            {
                return "PİGMENT KRİTİK • DAHA KISA VE ANLAMLI HAMLELER YAP";
            }

            bool isStain =
                clarityLevel != null &&
                clarityLevel.IndexOf("Stain", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (isStain)
            {
                return "LEKE DESTEĞİ AKTİF • KOŞU VE ANA FIGURE ARAÇLARI KAPALI";
            }

            if (!isPainter && !canSprint && !canUsePrimaryTool)
            {
                return "NETLİK BOZULDU • KOŞU VE ARAÇLAR KAPALI • RESTORE ARA";
            }

            if (!isPainter && !canSprint)
            {
                return "NETLİK BOZULDU • KOŞU KAPALI • RESTORE VEYA GÜVENLİ ROTA ARA";
            }

            if (!isPainter && !canUsePrimaryTool)
            {
                return "NETLİK BOZULDU • ANA ARAÇLAR KAPALI • RESTORE ARA";
            }

            if (!isPainter && normalizedClarity <= 0.25f)
            {
                return "NETLİK KRİTİK • RESTORE VEYA GÜVENLİ ROTA ARA";
            }

            return string.Empty;
        }
    }
}
