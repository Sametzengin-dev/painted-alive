using System;
using System.Reflection;
using UnityEngine;

namespace PaintedAlive.MatchFlow
{
    [DefaultExecutionOrder(400)]
    [DisallowMultipleComponent]
    public sealed class PrototypeCoreMatchController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private Transform figureRoot;

        [SerializeField]
        private MonoBehaviour clarityState;

        [SerializeField]
        private Transform startPoint;

        [SerializeField]
        private Transform exitPoint;

        [SerializeField]
        private PrototypeCoreMatchExitTrigger exitTrigger;

        [SerializeField]
        private PrototypeCoreMatchWorldResetCoordinator worldResetCoordinator;

        [Header("Local Foundation Timing")]
        [SerializeField, Min(0.5f)]
        private float preparationSeconds = 3.0f;

        [SerializeField, Min(10f)]
        private float roundSeconds = 300.0f;

        [Header("GDD Score Contract")]
        [SerializeField, Min(1)]
        private int maximumDistanceScore = 1000;

        [SerializeField, Min(0)]
        private int exitBonus = 250;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeCoreMatchPhase phase =
            PrototypeCoreMatchPhase.Boot;

        [SerializeField]
        private PrototypeCoreMatchResult result =
            PrototypeCoreMatchResult.None;

        [SerializeField]
        private int matchRunId;

        [SerializeField]
        private int preparationStartCount;

        [SerializeField]
        private int activeStartCount;

        [SerializeField]
        private int matchCompleteCount;

        [SerializeField]
        private int resetCount;

        [SerializeField]
        private int figureExitCount;

        [SerializeField]
        private int timeExpiredCount;

        [SerializeField]
        private float preparationRemainingSeconds;

        [SerializeField]
        private float roundRemainingSeconds;

        [SerializeField]
        private float elapsedRoundSeconds;

        [SerializeField]
        private float currentProgressNormalized;

        [SerializeField]
        private float bestProgressNormalized;

        [SerializeField]
        private int distanceScore;

        [SerializeField]
        private int currentScore;

        [SerializeField]
        private bool escaped;

        [SerializeField]
        private string clarityLevel = "Unknown";

        [SerializeField]
        private float normalizedClarity = -1f;

        [SerializeField]
        private string lastAction =
            "Core Match Foundation boot.";

        private PropertyInfo clarityLevelProperty;
        private PropertyInfo normalizedClarityProperty;
        private MethodInfo clarityResetMethod;

        public PrototypeCoreMatchPhase Phase => phase;
        public PrototypeCoreMatchResult Result => result;
        public int MatchRunId => matchRunId;
        public int PreparationStartCount => preparationStartCount;
        public int ActiveStartCount => activeStartCount;
        public int MatchCompleteCount => matchCompleteCount;
        public int ResetCount => resetCount;
        public int FigureExitCount => figureExitCount;
        public int TimeExpiredCount => timeExpiredCount;
        public float PreparationRemainingSeconds =>
            preparationRemainingSeconds;
        public float RoundRemainingSeconds =>
            roundRemainingSeconds;
        public float ElapsedRoundSeconds =>
            elapsedRoundSeconds;
        public float CurrentProgressNormalized =>
            currentProgressNormalized;
        public float BestProgressNormalized =>
            bestProgressNormalized;
        public int DistanceScore => distanceScore;
        public int CurrentScore => currentScore;
        public bool Escaped => escaped;
        public string ClarityLevel => clarityLevel;
        public float NormalizedClarity => normalizedClarity;
        public string LastAction => lastAction;
        public PrototypeCoreMatchWorldResetCoordinator WorldResetCoordinator =>
            worldResetCoordinator;

        public bool FeatureFreezeActive => true;
        public bool NewMajorGameplayMechanicsAllowed => false;
        public bool UsesGddDistanceScoreContract => true;
        public bool UsesGddExitBonusContract => true;
        public bool PainterKillScoreEnabled => false;
        public bool StainEndsMatch => false;
        public bool LocalStraightLineProgressProxy => true;
        public bool FinalRouteGraphScoringImplemented => false;
        public bool FullWorldStateResetImplemented => false;
        public bool MatchCompletionFreezesPlayer => false;
        public bool AddsNewGameplayInputAction => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;
        public bool MatchFlowReadyForNetworkPort => true;
        public bool IntegratedTransientWorldResetEnabled =>
            worldResetCoordinator != null;
        public bool FullLocalRoundResetContractEnabled =>
            worldResetCoordinator != null;

        public void Configure(
            Transform configuredFigureRoot,
            MonoBehaviour configuredClarityState,
            Transform configuredStartPoint,
            Transform configuredExitPoint,
            PrototypeCoreMatchExitTrigger configuredExitTrigger)
        {
            figureRoot = configuredFigureRoot;
            clarityState = configuredClarityState;
            startPoint = configuredStartPoint;
            exitPoint = configuredExitPoint;
            exitTrigger = configuredExitTrigger;

            ResolveClarityContract();

            if (exitTrigger != null)
            {
                exitTrigger.Configure(
                    this,
                    figureRoot);
            }
        }

        public void ConfigureWorldResetCoordinator(
            PrototypeCoreMatchWorldResetCoordinator configuredCoordinator)
        {
            worldResetCoordinator =
                configuredCoordinator;

            if (worldResetCoordinator != null)
            {
                worldResetCoordinator.Configure(
                    this);
            }
        }

        private void Awake()
        {
            ResolveClarityContract();
        }

        private void Start()
        {
            if (
                phase ==
                PrototypeCoreMatchPhase.Boot
            )
            {
                BeginPreparation(
                    "Automatic local foundation start");
            }
        }

        private void Update()
        {
            RefreshClarity();
            RefreshProgressAndScore();

            switch (phase)
            {
                case PrototypeCoreMatchPhase.Preparation:
                    TickPreparation();
                    break;

                case PrototypeCoreMatchPhase.Active:
                    TickActiveMatch();
                    break;
            }
        }

        public void BeginPreparation(
            string source)
        {
            if (
                figureRoot == null ||
                startPoint == null ||
                exitPoint == null
            )
            {
                lastAction =
                    "Preparation rejected: match anchors missing.";

                return;
            }

            result =
                PrototypeCoreMatchResult.None;

            escaped = false;
            elapsedRoundSeconds = 0f;
            currentProgressNormalized = 0f;
            bestProgressNormalized = 0f;
            distanceScore = 0;
            currentScore = 0;

            preparationRemainingSeconds =
                preparationSeconds;

            roundRemainingSeconds =
                roundSeconds;

            phase =
                PrototypeCoreMatchPhase.Preparation;

            preparationStartCount++;

            lastAction =
                $"Preparation başladı: {source}.";
        }

        public void ForceStartActiveMatch()
        {
            if (
                phase ==
                PrototypeCoreMatchPhase.Active
            )
            {
                return;
            }

            BeginActiveMatch(
                "Development force start");
        }

        public void NotifyFigureReachedExit()
        {
            if (
                phase !=
                PrototypeCoreMatchPhase.Active
            )
            {
                return;
            }

            figureExitCount++;
            escaped = true;

            bestProgressNormalized = 1f;
            currentProgressNormalized = 1f;

            RefreshProgressAndScore();

            CompleteMatch(
                PrototypeCoreMatchResult.FigureEscaped,
                "Figure reached frame exit");
        }

        public void ForceTimeExpiry()
        {
            if (
                phase !=
                PrototypeCoreMatchPhase.Active
            )
            {
                BeginActiveMatch(
                    "Development force before time expiry");
            }

            CompleteMatch(
                PrototypeCoreMatchResult.TimeExpired,
                "Development force time expiry");
        }

        public void ResetMatchAndBeginPreparation()
        {
            if (worldResetCoordinator != null)
            {
                worldResetCoordinator.ResetRoundWorldState(
                    "Core Match reset");
            }
            else
            {
                ReleaseTransientCompositionSessions();
            }

            ResetClarityToFull();
            TeleportFigureToStart();

            resetCount++;
            matchRunId++;

            BeginPreparation(
                worldResetCoordinator != null
                    ? "Integrated world reset"
                    : "Development reset");
        }

        private void TickPreparation()
        {
            preparationRemainingSeconds =
                Mathf.Max(
                    0f,
                    preparationRemainingSeconds -
                    Mathf.Max(
                        0f,
                        Time.deltaTime));

            if (
                preparationRemainingSeconds >
                0f
            )
            {
                return;
            }

            BeginActiveMatch(
                "Preparation complete");
        }

        private void BeginActiveMatch(
            string source)
        {
            if (
                phase ==
                PrototypeCoreMatchPhase.Complete
            )
            {
                result =
                    PrototypeCoreMatchResult.None;
            }

            phase =
                PrototypeCoreMatchPhase.Active;

            result =
                PrototypeCoreMatchResult.None;

            escaped = false;

            preparationRemainingSeconds = 0f;
            roundRemainingSeconds =
                roundSeconds;
            elapsedRoundSeconds = 0f;

            currentProgressNormalized =
                EvaluateProgressNormalized();

            bestProgressNormalized =
                Mathf.Max(
                    bestProgressNormalized,
                    currentProgressNormalized);

            activeStartCount++;

            lastAction =
                $"Active match başladı: {source}.";

            Debug.Log(
                "[M54.0 Core Match Active]\n" +
                $"MatchRunId={matchRunId}\n" +
                $"RoundSeconds={roundSeconds:F1}\n" +
                $"DistanceScoreMax={maximumDistanceScore}\n" +
                $"ExitBonus={exitBonus}\n" +
                "PainterKillScoreEnabled=False\n" +
                "StainEndsMatch=False\n" +
                "AddsNewGameplayInputAction=False\n" +
                "NetworkAuthorityEnabled=False",
                this);
        }

        private void TickActiveMatch()
        {
            float delta =
                Mathf.Max(
                    0f,
                    Time.deltaTime);

            elapsedRoundSeconds +=
                delta;

            roundRemainingSeconds =
                Mathf.Max(
                    0f,
                    roundRemainingSeconds -
                    delta);

            if (
                roundRemainingSeconds >
                0f
            )
            {
                return;
            }

            timeExpiredCount++;

            CompleteMatch(
                PrototypeCoreMatchResult.TimeExpired,
                "Round timer expired");
        }

        private void CompleteMatch(
            PrototypeCoreMatchResult completedResult,
            string source)
        {
            if (
                phase ==
                PrototypeCoreMatchPhase.Complete
            )
            {
                return;
            }

            phase =
                PrototypeCoreMatchPhase.Complete;

            result =
                completedResult;

            if (
                completedResult ==
                PrototypeCoreMatchResult.FigureEscaped
            )
            {
                escaped = true;
                bestProgressNormalized = 1f;
                currentProgressNormalized = 1f;
            }

            RefreshProgressAndScore();

            matchCompleteCount++;

            lastAction =
                $"Match complete: {completedResult} • {source}.";

            Debug.Log(
                "[M54.0 Core Match Complete]\n" +
                $"Result={completedResult}\n" +
                $"DistanceScore={distanceScore}\n" +
                $"ExitBonusApplied={(escaped ? exitBonus : 0)}\n" +
                $"TotalScore={currentScore}\n" +
                $"BestProgress={bestProgressNormalized:F3}\n" +
                $"ElapsedSeconds={elapsedRoundSeconds:F2}\n" +
                $"ClarityLevel={clarityLevel}\n" +
                "StainEndsMatch=False\n" +
                "PainterKillScoreEnabled=False",
                this);
        }

        private void RefreshProgressAndScore()
        {
            if (
                figureRoot == null ||
                startPoint == null ||
                exitPoint == null
            )
            {
                currentProgressNormalized = 0f;
                return;
            }

            if (
                phase !=
                    PrototypeCoreMatchPhase.Complete ||
                !escaped
            )
            {
                currentProgressNormalized =
                    EvaluateProgressNormalized();

                bestProgressNormalized =
                    Mathf.Max(
                        bestProgressNormalized,
                        currentProgressNormalized);
            }

            distanceScore =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        bestProgressNormalized *
                        maximumDistanceScore),
                    0,
                    maximumDistanceScore);

            currentScore =
                distanceScore +
                (
                    escaped
                        ? exitBonus
                        : 0
                );
        }

        private float EvaluateProgressNormalized()
        {
            Vector3 start =
                startPoint.position;

            Vector3 end =
                exitPoint.position;

            Vector3 route =
                Vector3.ProjectOnPlane(
                    end -
                    start,
                    Vector3.up);

            float routeLength =
                route.magnitude;

            if (
                routeLength <
                0.05f
            )
            {
                return 0f;
            }

            Vector3 direction =
                route /
                routeLength;

            Vector3 relative =
                Vector3.ProjectOnPlane(
                    figureRoot.position -
                    start,
                    Vector3.up);

            float projected =
                Vector3.Dot(
                    relative,
                    direction);

            return
                Mathf.Clamp01(
                    projected /
                    routeLength);
        }

        private void ResolveClarityContract()
        {
            clarityLevelProperty = null;
            normalizedClarityProperty = null;
            clarityResetMethod = null;

            if (clarityState == null)
            {
                return;
            }

            Type type =
                clarityState.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            clarityLevelProperty =
                type.GetProperty(
                    "CurrentLevel",
                    flags);

            normalizedClarityProperty =
                type.GetProperty(
                    "NormalizedClarity",
                    flags);

            clarityResetMethod =
                type.GetMethod(
                    "ResetToFull",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null);
        }

        private void RefreshClarity()
        {
            if (clarityState == null)
            {
                clarityLevel = "Unknown";
                normalizedClarity = -1f;
                return;
            }

            if (
                clarityLevelProperty == null &&
                normalizedClarityProperty == null
            )
            {
                ResolveClarityContract();
            }

            try
            {
                if (clarityLevelProperty != null)
                {
                    object value =
                        clarityLevelProperty.GetValue(
                            clarityState);

                    clarityLevel =
                        value != null
                            ? value.ToString()
                            : "Unknown";
                }

                if (normalizedClarityProperty != null)
                {
                    object value =
                        normalizedClarityProperty.GetValue(
                            clarityState);

                    if (value is float floatValue)
                    {
                        normalizedClarity =
                            floatValue;
                    }
                }
            }
            catch (Exception)
            {
                clarityLevel = "Unknown";
                normalizedClarity = -1f;
            }
        }

        private void ResetClarityToFull()
        {
            if (clarityState == null)
            {
                return;
            }

            if (clarityResetMethod == null)
            {
                ResolveClarityContract();
            }

            if (clarityResetMethod == null)
            {
                return;
            }

            try
            {
                clarityResetMethod.Invoke(
                    clarityState,
                    null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[M54.0] Clarity reset skipped: {exception.Message}",
                    this);
            }
        }

        private void TeleportFigureToStart()
        {
            if (
                figureRoot == null ||
                startPoint == null
            )
            {
                return;
            }

            CharacterController characterController =
                figureRoot.GetComponent<
                    CharacterController>();

            bool controllerWasEnabled =
                characterController != null &&
                characterController.enabled;

            if (controllerWasEnabled)
            {
                characterController.enabled = false;
            }

            figureRoot.position =
                startPoint.position;

            figureRoot.rotation =
                startPoint.rotation;

            if (controllerWasEnabled)
            {
                characterController.enabled = true;
            }

            Physics.SyncTransforms();
        }

        private void ReleaseTransientCompositionSessions()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (
                    behaviour == null ||
                    behaviour.GetType().Name !=
                        "PrototypeLiveCompositionController"
                )
                {
                    continue;
                }

                MethodInfo release =
                    behaviour
                        .GetType()
                        .GetMethod(
                            "ForceReleaseComposition",
                            flags,
                            null,
                            Type.EmptyTypes,
                            null);

                if (release == null)
                {
                    continue;
                }

                try
                {
                    release.Invoke(
                        behaviour,
                        null);
                }
                catch (Exception)
                {
                    // Best-effort local reset only.
                }
            }
        }
    }
}
