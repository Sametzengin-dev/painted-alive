using PaintedAlive.Painters;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.UI
{
    public sealed class PainterStrokePressureHud : MonoBehaviour
    {
        [SerializeField]
        private PainterBrushController brushController;

        [SerializeField]
        private PainterStrokePressureTracker pressureTracker;

        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider pressureSlider;
        [SerializeField] private Text styleText;
        [SerializeField] private Text speedText;

        [Header("Colors")]
        [SerializeField] private Color fastColor =
            new(0.90f, 0.58f, 0.20f, 1f);

        [SerializeField] private Color balancedColor =
            new(0.90f, 0.82f, 0.63f, 1f);

        [SerializeField] private Color heavyColor =
            new(0.65f, 0.10f, 0.16f, 1f);

        private void Awake()
        {
            if (pressureSlider != null)
            {
                pressureSlider.minValue = 0f;
                pressureSlider.maxValue = 1f;
                pressureSlider.interactable = false;
            }
        }

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
            if (brushController == null ||
                pressureTracker == null)
            {
                return;
            }

            if (!brushController.IsPreviewing)
            {
                if (pressureSlider != null)
                    pressureSlider.value = 0.5f;

                if (styleText != null)
                {
                    styleText.text =
                        "ÇİZİM BASINCI —";
                    styleText.color = balancedColor;
                }

                if (speedText != null)
                    speedText.text = string.Empty;

                return;
            }

            float pressure =
                pressureTracker.PressureNormalized;

            if (pressureSlider != null)
                pressureSlider.value = pressure;

            string style;
            Color color;

            if (pressure < 0.33f)
            {
                style = "HIZLI • İNCE / KIRILGAN";
                color = fastColor;
            }
            else if (pressure < 0.66f)
            {
                style = "DENGELİ • STANDART";
                color = balancedColor;
            }
            else
            {
                style = "AĞIR • KALIN / DAYANIKLI";
                color = heavyColor;
            }

            if (styleText != null)
            {
                styleText.text = style;
                styleText.color = color;
            }

            if (speedText != null)
            {
                float speed =
                    pressureTracker.AverageDrawSpeed;

                float pigmentMultiplier =
                    pressureTracker
                        .CurrentProfile
                        .PigmentMultiplier;

                speedText.text =
                    $"HIZ {speed:0.0} m/s  •  " +
                    $"PİGMENT ×{pigmentMultiplier:0.00}";
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
