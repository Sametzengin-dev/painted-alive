using PaintedAlive.Paint;
using PaintedAlive.Painters;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.UI
{
    public sealed class PainterPaintMoundHud : MonoBehaviour
    {
        [SerializeField]
        private PainterPaintMoundController controller;

        [SerializeField]
        private PaintMoundSystem moundSystem;

        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text statusText;
        [SerializeField] private Text detailText;
        [SerializeField] private Slider chargeSlider;

        private void Awake()
        {
            if (chargeSlider != null)
            {
                chargeSlider.minValue = 0f;
                chargeSlider.maxValue = 1f;
                chargeSlider.interactable = false;
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
            if (controller == null || moundSystem == null)
                return;

            if (controller.IsCharging)
            {
                if (chargeSlider != null)
                    chargeSlider.value = controller.ChargeNormalized;

                if (statusText != null)
                {
                    statusText.text = controller.IsReady
                        ? "BOYA TEPESİ HAZIR — 3'Ü BIRAK"
                        : "BOYA TEPESİ BÜYÜYOR";
                }

                if (detailText != null)
                {
                    detailText.text =
                        $"PİGMENT {controller.EstimatedPigmentCost:0}  •  " +
                        $"BOYUT {controller.PreviewRadius:0.0} m";
                }

                return;
            }

            if (chargeSlider != null)
                chargeSlider.value = 0f;

            if (statusText != null)
            {
                if (!moundSystem.CanPlace(out PaintMoundBlockReason reason))
                {
                    statusText.text = reason switch
                    {
                        PaintMoundBlockReason.Cooldown =>
                            $"BOYA TEPESİ  {moundSystem.CooldownRemaining:0.0}s",

                        PaintMoundBlockReason.ActiveLimit =>
                            "BOYA TEPESİ — AKTİF SINIR",

                        PaintMoundBlockReason.TotalLimit =>
                            "BOYA TEPESİ — TOPLAM SINIR",

                        _ => "BOYA TEPESİ KULLANILAMIYOR"
                    };
                }
                else
                {
                    statusText.text = "3 BASILI TUT — BOYA TEPESİ";
                }
            }

            if (detailText != null)
            {
                detailText.text =
                    $"AKTİF {moundSystem.ActiveMoundCount}  •  " +
                    $"TOPLAM {moundSystem.TotalMoundCount}";
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
