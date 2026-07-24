using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Paint.Ink.StainHijack
{
    [DisallowMultipleComponent]
    public sealed class InkStainCreatureHijackHud : MonoBehaviour
    {
        [SerializeField]
        private InkStainCreatureHijackController controller;

        [SerializeField]
        private GameObject panelRoot;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private Image progressFill;

        private float nextRefreshTime;

        private void Update()
        {
            bool visible =
                controller != null &&
                controller.IsStainRoleActive;

            if (panelRoot != null &&
                panelRoot.activeSelf != visible)
            {
                panelRoot.SetActive(visible);
            }

            if (!visible ||
                Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + 0.06f;
            Refresh();
        }

        public void Configure(
            InkStainCreatureHijackController hijackController,
            GameObject targetPanel,
            Text targetText,
            Image targetProgress)
        {
            controller = hijackController;
            panelRoot = targetPanel;
            statusText = targetText;
            progressFill = targetProgress;
            Refresh();
        }

        private void Refresh()
        {
            if (controller == null)
            {
                return;
            }

            if (statusText != null)
            {
                string title = controller.IsHijacking
                    ? "LEKE SIZMASI • KONTROL AKTİF"
                    : controller.CurrentTarget != null
                        ? "LEKE SIZMASI • HEDEF HAZIR"
                        : "LEKE SIZMASI";
                statusText.text =
                    $"{title}\n{controller.LastResult}";
            }

            if (progressFill != null)
            {
                float fill = controller.IsHijacking
                    ? Mathf.Clamp01(
                        controller.RemainingSeconds /
                        controller.MaximumHijackDuration)
                    : controller.EntryProgress;
                progressFill.fillAmount = fill;
                progressFill.color = controller.IsHijacking
                    ? new Color(0.96f, 0.2f, 0.7f, 0.98f)
                    : new Color(0.12f, 0.95f, 0.75f, 0.98f);
            }
        }
    }
}
