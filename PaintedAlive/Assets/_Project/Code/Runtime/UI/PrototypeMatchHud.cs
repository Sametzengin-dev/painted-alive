using PaintedAlive.Core.Prototypes;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.UI
{
    public sealed class PrototypeMatchHud : MonoBehaviour
    {
        [SerializeField] private PrototypeMatchController matchController;
        [SerializeField] private FigureProgressTracker progressTracker;

        [Header("Text")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text progressText;

        [Header("Progress")]
        [SerializeField] private Slider progressSlider;

        private void Awake()
        {
            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.interactable = false;
            }
        }

        private void OnEnable()
        {
            if (matchController != null)
            {
                matchController.StateChanged += HandleStateChanged;
                matchController.CountdownChanged += HandleCountdownChanged;
                matchController.TimeChanged += HandleTimeChanged;
            }

            if (progressTracker != null)
            {
                progressTracker.ProgressChanged += HandleProgressChanged;
            }
        }

        private void OnDisable()
        {
            if (matchController != null)
            {
                matchController.StateChanged -= HandleStateChanged;
                matchController.CountdownChanged -= HandleCountdownChanged;
                matchController.TimeChanged -= HandleTimeChanged;
            }

            if (progressTracker != null)
            {
                progressTracker.ProgressChanged -= HandleProgressChanged;
            }
        }

        private void HandleStateChanged(
            PrototypeMatchState state)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = state switch
            {
                PrototypeMatchState.Countdown => "HAZIRLAN",
                PrototypeMatchState.Running => "ÇIKIŞA ULAŞ",
                PrototypeMatchState.FigureEscaped =>
                    "FİGÜR KAÇTI\nYeniden başlatmak için ENTER",
                PrototypeMatchState.TimeExpired =>
                    "SÜRE DOLDU\nYeniden başlatmak için ENTER",
                _ => string.Empty
            };
        }

        private void HandleCountdownChanged(float remaining)
        {
            if (statusText != null)
            {
                statusText.text = remaining > 0.05f
                    ? Mathf.CeilToInt(remaining).ToString()
                    : "BAŞLA";
            }
        }

        private void HandleTimeChanged(float remaining)
        {
            if (timerText != null)
            {
                timerText.text = FormatTime(remaining);
            }
        }

        private void HandleProgressChanged(
            float normalized,
            float distance,
            float remaining)
        {
            if (progressSlider != null)
            {
                progressSlider.value = normalized;
            }

            if (progressText != null)
            {
                progressText.text =
                    $"İLERLEME %{normalized * 100f:0}  •  " +
                    $"KALAN {remaining:0.0} m";
            }
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds =
                Mathf.Max(0, Mathf.CeilToInt(seconds));

            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;

            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
