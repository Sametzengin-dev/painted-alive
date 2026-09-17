using PaintedAlive.UI.UnifiedHUD;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.MatchFlow
{
    [DefaultExecutionOrder(420)]
    [DisallowMultipleComponent]
    public sealed class PrototypeCoreMatchHud :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeCoreMatchController controller;

        [SerializeField]
        private CanvasGroup group;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Text scoreText;

        [SerializeField]
        private Text contractText;

        [Header("Focus")]
        [SerializeField]
        private bool hideWhenUnifiedHudPresent = true;

        private PrototypeUnifiedHudView cachedUnifiedHud;

        public void Configure(
            PrototypeCoreMatchController configuredController,
            CanvasGroup configuredGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredScoreText,
            Text configuredContractText)
        {
            controller =
                configuredController;

            group =
                configuredGroup;

            titleText =
                configuredTitleText;

            stateText =
                configuredStateText;

            scoreText =
                configuredScoreText;

            contractText =
                configuredContractText;
        }

        private void Update()
        {
            if (controller == null || ShouldHideForUnifiedHud())
            {
                if (group != null)
                {
                    group.alpha = 0f;
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }

                return;
            }

            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            if (titleText != null)
            {
                titleText.text =
                    "CORE MATCH • FEATURE FREEZE";
            }

            if (stateText != null)
            {
                string timer =
                    controller.Phase ==
                        PrototypeCoreMatchPhase.Preparation
                        ? $"HAZIRLIK {controller.PreparationRemainingSeconds:F1}s"
                        : controller.Phase ==
                            PrototypeCoreMatchPhase.Active
                            ? $"MAÇ {controller.RoundRemainingSeconds:F1}s"
                            : controller.Phase ==
                                PrototypeCoreMatchPhase.Complete
                                ? $"SONUÇ {TranslateResult(controller.Result)}"
                                : "BOOT";

                stateText.text =
                    $"{timer}   RUN {controller.MatchRunId}\n" +
                    $"İLERLEME %{controller.BestProgressNormalized * 100f:F0}   " +
                    $"NETLİK {controller.ClarityLevel}";
            }

            if (scoreText != null)
            {
                scoreText.text =
                    $"MESAFE {controller.DistanceScore}/1000   " +
                    $"ÇIKIŞ {(controller.Escaped ? "+250" : "—")}   " +
                    $"TOPLAM {controller.CurrentScore}";
            }

            if (contractText != null)
            {
                contractText.text =
                    "LEKE MAÇI BİTİRMEZ • RESSAM KILL PUANI YOK • YENİ INPUT YOK";
            }
        }

        private bool ShouldHideForUnifiedHud()
        {
            if (!hideWhenUnifiedHudPresent)
            {
                return false;
            }

            if (cachedUnifiedHud == null)
            {
                cachedUnifiedHud = FindFirstObjectByType<PrototypeUnifiedHudView>(
                    FindObjectsInactive.Exclude);
            }

            return cachedUnifiedHud != null &&
                   cachedUnifiedHud.isActiveAndEnabled;
        }

        private static string TranslateResult(
            PrototypeCoreMatchResult result)
        {
            switch (result)
            {
                case PrototypeCoreMatchResult.FigureEscaped:
                    return "FIGURE ESCAPE";

                case PrototypeCoreMatchResult.TimeExpired:
                    return "TIME EXPIRED";

                default:
                    return "—";
            }
        }
    }
}
