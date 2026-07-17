using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Figures
{
    public sealed class SpongeRescueHUD : MonoBehaviour
    {
        [SerializeField] private SpongeRescueController rescueController;
        [SerializeField] private FigureCleanPigmentInventory inventory;

        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text pigmentText;
        [SerializeField] private Text rescuePromptText;
        [SerializeField] private Slider rescueProgressSlider;

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
            if (rescueController == null || inventory == null)
                return;

            if (pigmentText != null)
            {
                pigmentText.text =
                    $"TEMİZ PİGMENT  ×{inventory.CurrentPigment}";
            }

            bool hasTarget =
                rescueController.CurrentTarget != null;

            if (rescuePromptText != null)
            {
                if (!hasTarget)
                {
                    rescuePromptText.text = "Q  SÜNGER";
                }
                else if (!inventory.HasPigment)
                {
                    rescuePromptText.text =
                        "TEMİZ PİGMENT YOK";
                }
                else
                {
                    rescuePromptText.text =
                        "Q BASILI TUT  •  RESTORE";
                }
            }

            if (rescueProgressSlider != null)
            {
                rescueProgressSlider.gameObject.SetActive(hasTarget);
                rescueProgressSlider.value =
                    rescueController.NormalizedProgress;
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
