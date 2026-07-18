using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Figures
{
    public sealed class StainAbilityHUD : MonoBehaviour
    {
        [SerializeField] private FigureClarityState clarityState;
        [SerializeField] private StainMarkController markController;

        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text abilityText;
        [SerializeField] private Slider cooldownSlider;

        private void OnEnable()
        {
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
            if (clarityState == null ||
                markController == null)
            {
                SetVisible(false);
                return;
            }

            bool isStain =
                clarityState.CurrentLevel ==
                FigureClarityLevel.Stain;

            SetVisible(isStain);

            if (!isStain)
                return;

            bool ready = markController.CanPlaceMark;

            if (abilityText != null)
            {
                abilityText.text = ready
                    ? "G  •  YÖN İZİ"
                    : "YÖN İZİ HAZIRLANIYOR";
            }

            if (cooldownSlider != null)
            {
                cooldownSlider.value =
                    1f - markController.CooldownNormalized;
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