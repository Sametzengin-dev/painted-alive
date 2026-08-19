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
                if (group != null)
                {
                    group.alpha = 0f;
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
