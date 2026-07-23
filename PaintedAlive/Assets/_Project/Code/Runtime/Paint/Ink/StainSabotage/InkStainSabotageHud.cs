using PaintedAlive.Paint.Ink.GlyphLoadouts;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Paint.Ink.StainSabotage
{
    [DisallowMultipleComponent]
    public sealed class InkStainSabotageHud : MonoBehaviour
    {
        [SerializeField]
        private InkStainSabotageController controller;

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

            nextRefreshTime = Time.unscaledTime + 0.08f;
            Refresh();
        }

        public void Configure(
            InkStainSabotageController sabotageController,
            GameObject targetPanel,
            Text targetText,
            Image targetProgressFill)
        {
            controller = sabotageController;
            panelRoot = targetPanel;
            statusText = targetText;
            progressFill = targetProgressFill;
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
                InkCreatureRuntime target = controller.CurrentTarget;
                string targetName = target != null
                    ? ResolveTargetName(target)
                    : "KÜÇÜK MÜREKKEP";
                statusText.text =
                    $"LEKE SABOTAJI  •  {targetName}\n" +
                    $"E BASILI TUT  •  {controller.LastResult}";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = controller.HoldProgress;
            }
        }

        private static string ResolveTargetName(
            InkCreatureRuntime target)
        {
            if (target.HasGlyph(InkGlyphType.BrokenLine))
            {
                return "KESİK AVCI";
            }

            if (target.HasGlyph(InkGlyphType.Shell))
            {
                return "KABUKLU";
            }

            int complexity =
                InkGlyphComplexityUtility.GetCreatureCost(target);
            return $"LEKEBACAK • K{complexity}";
        }
    }
}
