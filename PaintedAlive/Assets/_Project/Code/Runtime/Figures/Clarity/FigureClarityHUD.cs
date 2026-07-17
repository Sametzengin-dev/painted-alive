using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Figures
{
    public sealed class FigureClarityHUD : MonoBehaviour
    {
        [SerializeField] private FigureClarityState clarityState;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider claritySlider;
        [SerializeField] private Text stateText;
        [SerializeField] private Image portraitImage;

        private void OnEnable()
        {
            SetVisible(true);

            if (clarityState == null)
                return;

            clarityState.ClarityChanged += HandleClarityChanged;
            clarityState.LevelChanged += HandleLevelChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (clarityState != null)
            {
                clarityState.ClarityChanged -= HandleClarityChanged;
                clarityState.LevelChanged -= HandleLevelChanged;
            }

            SetVisible(false);
        }

        private void HandleClarityChanged(float previous, float current)
        {
            Refresh();
        }

        private void HandleLevelChanged(
            FigureClarityLevel previous,
            FigureClarityLevel current)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (clarityState == null)
                return;

            if (claritySlider != null)
                claritySlider.value = clarityState.NormalizedClarity;

            Color stateColor = clarityState.CurrentLevel switch
            {
                FigureClarityLevel.Clean =>
                    new Color(0.85f, 0.92f, 0.88f),

                FigureClarityLevel.Stained =>
                    new Color(0.92f, 0.68f, 0.34f),

                FigureClarityLevel.Distorted =>
                    new Color(0.80f, 0.30f, 0.25f),

                FigureClarityLevel.Dissolving =>
                    new Color(0.55f, 0.12f, 0.20f),

                FigureClarityLevel.Stain =>
                    new Color(0.20f, 0.02f, 0.05f),

                _ => Color.white
            };

            if (portraitImage != null)
                portraitImage.color = stateColor;

            if (stateText != null)
            {
                stateText.text = clarityState.CurrentLevel switch
                {
                    FigureClarityLevel.Clean => "NET",
                    FigureClarityLevel.Stained => "LEKELİ",
                    FigureClarityLevel.Distorted => "BOZULMUŞ",
                    FigureClarityLevel.Dissolving => "ÇÖZÜLÜYOR",
                    FigureClarityLevel.Stain => "LEKE — DESTEK FORMU",
                    _ => string.Empty
                };

                stateText.color = stateColor;
            }
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
