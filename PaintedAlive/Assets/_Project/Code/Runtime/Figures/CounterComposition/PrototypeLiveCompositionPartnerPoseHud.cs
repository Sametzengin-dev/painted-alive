using PaintedAlive.UI;
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

        [Header("Focus")]
        [SerializeField, Min(0f)]
        private float collapseHoldSeconds = 3f;

        private float visibleUntilUnscaled;

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
                PrototypeRuntimeHudVisibilityUtility.ApplyCanvasGroup(
                    group,
                    false);
                return;
            }

            bool relevantNow =
                binder.PartnerPoseBound ||
                binder.CompositionObserved ||
                binder.PartnerConsentSessionObserved;

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
