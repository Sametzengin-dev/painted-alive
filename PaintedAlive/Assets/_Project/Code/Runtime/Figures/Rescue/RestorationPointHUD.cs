using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Figures
{
    public sealed class RestorationPointHUD : MonoBehaviour
    {
        [SerializeField]
        private FigureRestorationInteractor interactor;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text promptText;
        [SerializeField] private Slider progressSlider;

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
            if (interactor == null)
            {
                SetVisible(false);
                return;
            }

            bool hasPoint =
                interactor.CurrentPoint != null;

            SetVisible(hasPoint);

            if (!hasPoint)
                return;

            if (promptText != null)
            {
                if (interactor.CurrentPointAlreadyUsed)
                {
                    promptText.text =
                        "RESTORASYON NOKTASI KULLANILDI";
                }
                else if (interactor.RestorationNotNeeded)
                {
                    promptText.text =
                        "RESTORASYON GEREKMİYOR";
                }
                else
                {
                    promptText.text =
                        "R BASILI TUT  •  RESTORE";
                }
            }

            if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(
                    interactor.CanUseCurrentPoint);

                progressSlider.value =
                    interactor.NormalizedProgress;
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
