using PaintedAlive.Painters;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.UI
{
    [DisallowMultipleComponent]
    public sealed class PainterPigmentHud : MonoBehaviour
    {
        [SerializeField] private PainterPigmentReservoir reservoir;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider pigmentSlider;
        [SerializeField] private Image fillImage;

        [Header("Colors")]
        [SerializeField] private Color fullColor =
            new(0.75f, 0.12f, 0.16f, 1f);

        [SerializeField] private Color lowColor =
            new(0.35f, 0.04f, 0.05f, 1f);

        private void Awake()
        {
            if (pigmentSlider != null)
            {
                pigmentSlider.minValue = 0f;
                pigmentSlider.maxValue = 1f;
                pigmentSlider.interactable = false;
            }

            SetVisible(false);
        }

        private void OnEnable()
        {
            if (reservoir != null)
            {
                reservoir.PigmentChanged += HandlePigmentChanged;
            }

            SetVisible(true);
            Refresh();
        }

        private void OnDisable()
        {
            if (reservoir != null)
            {
                reservoir.PigmentChanged -= HandlePigmentChanged;
            }

            SetVisible(false);
        }

        private void HandlePigmentChanged(
            float current,
            float capacity)
        {
            float normalized = capacity > 0f
                ? current / capacity
                : 0f;

            UpdateVisual(normalized);
        }

        private void Refresh()
        {
            UpdateVisual(
                reservoir != null
                    ? reservoir.Normalized
                    : 0f);
        }

        private void UpdateVisual(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);

            if (pigmentSlider != null)
            {
                pigmentSlider.value = normalized;
            }

            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(
                    lowColor,
                    fullColor,
                    normalized);
            }
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
