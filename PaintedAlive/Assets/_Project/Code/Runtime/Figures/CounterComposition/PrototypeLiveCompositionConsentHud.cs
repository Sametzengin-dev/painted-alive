using PaintedAlive.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public sealed class PrototypeLiveCompositionConsentHud :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeLiveCompositionConsentHarness harness;

        [SerializeField]
        private CanvasGroup group;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Text controlsText;

        [Header("Focus")]
        [SerializeField, Min(0f)]
        private float collapseHoldSeconds = 3f;

        private float visibleUntilUnscaled;

        public void Configure(
            PrototypeLiveCompositionConsentHarness configuredHarness,
            CanvasGroup configuredGroup,
            Text configuredTitle,
            Text configuredState,
            Text configuredControls)
        {
            harness = configuredHarness;
            group = configuredGroup;
            titleText = configuredTitle;
            stateText = configuredState;
            controlsText = configuredControls;
        }

        private void Update()
        {
            if (harness == null)
            {
                PrototypeRuntimeHudVisibilityUtility.ApplyCanvasGroup(
                    group,
                    false);
                return;
            }

            bool relevantNow =
                harness.State != PrototypeLiveCompositionConsentState.Idle ||
                harness.InvitationPending ||
                harness.PartnerConsentGranted ||
                harness.WithdrawalRequested;

            bool visible =
                PrototypeRuntimeHudVisibilityUtility.Hold(
                    relevantNow,
                    ref visibleUntilUnscaled,
                    collapseHoldSeconds);

            PrototypeRuntimeHudVisibilityUtility.ApplyCanvasGroup(
                group,
                visible);

            if (!visible)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    "CANLI KOMPOZİSYON • PARTNER ONAYI";
            }

            if (stateText != null)
            {
                string timer =
                    harness.InvitationPending
                        ? $"DAVET {harness.InvitationRemainingSeconds:F1}s"
                        : harness.PartnerConsentGranted
                            ? $"ONAY {harness.AcceptedCommitRemainingSeconds:F1}s"
                            : harness.StateLabel;

                stateText.text =
                    $"DURUM {timer}\n" +
                    $"DAVET {harness.InvitationCount}   " +
                    $"ONAY {harness.PartnerAcceptCount}   " +
                    $"ÇEKİLME {harness.PartnerWithdrawCount}\n" +
                    harness.LastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    "PLAY MODE MENÜ: PARTNER ACCEPT / WITHDRAW\n" +
                    "DEV HARNESS • GAMEPLAY INPUT DEĞİL";
            }
        }
    }
}
