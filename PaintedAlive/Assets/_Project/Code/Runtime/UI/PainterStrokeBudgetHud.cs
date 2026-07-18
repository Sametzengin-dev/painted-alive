using PaintedAlive.Paint;
using PaintedAlive.Painters;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.UI
{
    public sealed class PainterStrokeBudgetHud : MonoBehaviour
    {
        [SerializeField]
        private PainterStrokeBudget budget;

        [SerializeField]
        private PainterBrushController brushController;

        [SerializeField]
        private PainterStrokeModeSelector modeSelector;

        [Header("UI")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private Slider pressureSlider;

        [SerializeField]
        private Text activeStrokeText;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private Text estimatedCostText;

        private void OnEnable()
        {
            SetVisible(true);
            Refresh();
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (budget == null ||
                brushController == null ||
                modeSelector == null)
            {
                return;
            }

            if (pressureSlider != null)
            {
                pressureSlider.value =
                    budget.NormalizedPressure;
            }

            if (activeStrokeText != null)
            {
                activeStrokeText.text =
                    $"AKTİF BOYA  " +
                    $"{budget.ActiveStrokeCount}/" +
                    $"{budget.MaximumActiveStrokes}";
            }

            if (brushController.IsPreviewing)
            {
                RefreshPreviewStatus();
            }
            else
            {
                RefreshIdleStatus();
            }
        }

        private void RefreshPreviewStatus()
{
    if (estimatedCostText != null)
    {
        int estimatedCost =
            Mathf.CeilToInt(
                brushController.EstimatedPigmentCost);

        estimatedCostText.text =
            $"TAHMİNİ MALİYET  {estimatedCost}";
    }

    if (statusText == null)
    {
        return;
    }

    if (!brushController.PreviewCanAfford)
    {
        statusText.text =
            "PİGMENT YETERSİZ — BIRAKINCA İPTAL";
    }
    else if (!brushController.IsTelegraphComplete)
    {
        int percent =
            Mathf.RoundToInt(
                brushController.TelegraphNormalized *
                100f);

        statusText.text =
            $"TELEGRAPH  %{percent}";
    }
    else
    {
        statusText.text =
            "HAZIR — UYGULAMAK İÇİN MOUSE'U BIRAK";
    }
}

        private void RefreshIdleStatus()
        {
            if (estimatedCostText != null)
            {
                estimatedCostText.text =
                    string.Empty;
            }

            if (statusText == null)
            {
                return;
            }

            OilStrokeShape shape =
                modeSelector.CurrentShape;

            if (budget.CanBeginStroke(
                    shape,
                    out PainterStrokeBlockReason reason))
            {
                statusText.text = "HAZIR";
                return;
            }

            statusText.text = reason switch
            {
                PainterStrokeBlockReason.Cooldown =>
                    "FIRÇA TOPARLANIYOR",

                PainterStrokeBlockReason.ActiveStrokeLimit =>
                    "AKTİF BOYA SINIRINA ULAŞILDI",

                PainterStrokeBlockReason.PressureLimit =>
                    "KOMPOZİSYON BASKISI DOLU",

                PainterStrokeBlockReason.StrokeInProgress =>
                    "STROKE DEVAM EDİYOR",

                _ => "KULLANILAMIYOR"
            };
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha =
                visible ? 1f : 0f;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}