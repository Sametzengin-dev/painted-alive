using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(325)]
    [DisallowMultipleComponent]
    public sealed class PrototypeLiveCompositionPartnerPoseHud :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeLiveCompositionPartnerPoseBinder binder;

        [SerializeField]
        private CanvasGroup group;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Text controlsText;

        public void Configure(
            PrototypeLiveCompositionPartnerPoseBinder configuredBinder,
            CanvasGroup configuredGroup,
            Text configuredTitle,
            Text configuredState,
            Text configuredControls)
        {
            binder = configuredBinder;
            group = configuredGroup;
            titleText = configuredTitle;
            stateText = configuredState;
            controlsText = configuredControls;
        }

        private void Update()
        {
            if (binder == null)
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
                    "CANLI KOMPOZİSYON • PARTNER POZ BAĞI";
            }

            if (stateText != null)
            {
                stateText.text =
                    $"BAĞ {(binder.PartnerPoseBound ? "AKTİF" : "BEKLİYOR")}   " +
                    $"BIND {binder.BindCount}   RELEASE {binder.ReleaseCount}\n" +
                    $"COMPOSITION {(binder.CompositionObserved ? "AKTİF" : "YOK")}   " +
                    $"CONSENT {(binder.PartnerConsentSessionObserved ? "AKTİF" : "YOK")}\n" +
                    binder.LastState;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    "ORTAK KÖPRÜ BAŞLAYINCA PARTNER POZU OTOMATİK BAĞLANIR\n" +
                    "FIGURE E / PARTNER WITHDRAW → POZ HOME'A GERİ DÖNER";
            }
        }
    }
}
