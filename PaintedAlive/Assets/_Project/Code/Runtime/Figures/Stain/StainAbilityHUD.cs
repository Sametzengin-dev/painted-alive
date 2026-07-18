using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Figures
{
    public sealed class StainAbilityHUD : MonoBehaviour
    {
        [SerializeField] private FigureClarityState clarityState;
        [SerializeField] private StainMarkController markController;
        [SerializeField] private StainWallCrawlController wallCrawlController;

        [Header("Direction Mark UI")]
        [SerializeField] private Text markAbilityText;
        [SerializeField] private Slider markCooldownSlider;

        [Header("Wall Crawl UI")]
        [SerializeField] private Text crawlAbilityText;
        [SerializeField] private Slider crawlAbilitySlider;

        [Header("Visibility")]
        [SerializeField] private CanvasGroup canvasGroup;

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
                markController == null ||
                wallCrawlController == null)
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

            RefreshMarkUI();
            RefreshCrawlUI();
        }

        private void RefreshMarkUI()
        {
            if (markAbilityText != null)
            {
                markAbilityText.text =
                    markController.CanPlaceMark
                        ? "G  •  YÖN İZİ"
                        : "YÖN İZİ HAZIRLANIYOR";
            }

            if (markCooldownSlider != null)
            {
                markCooldownSlider.value =
                    1f -
                    markController.CooldownNormalized;
            }
        }

        private void RefreshCrawlUI()
        {
            if (crawlAbilityText != null)
            {
                if (wallCrawlController.IsCrawling)
                {
                    crawlAbilityText.text =
                        "SHIFT BIRAK  •  DUVARI BIRAK";
                }
                else if (
                    wallCrawlController.CrawlCooldownRemaining > 0f)
                {
                    crawlAbilityText.text =
                        "TUTUNMA HAZIRLANIYOR";
                }
                else
                {
                    crawlAbilityText.text =
                        "SHIFT BASILI TUT  •  DUVARA YAPIŞ";
                }
            }

            if (crawlAbilitySlider != null)
            {
                crawlAbilitySlider.value =
                    wallCrawlController.AbilityNormalized;
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