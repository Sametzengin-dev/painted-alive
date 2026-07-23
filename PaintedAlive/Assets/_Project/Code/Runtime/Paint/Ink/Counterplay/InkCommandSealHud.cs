using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Paint.Ink.Counterplay
{
    [DisallowMultipleComponent]
    public sealed class InkCommandSealHud : MonoBehaviour
    {
        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkCommandSeal commandSeal;

        [SerializeField]
        private GameObject panelRoot;

        [SerializeField]
        private Text statusText;

        private float nextRefreshTime;

        private void Update()
        {
            bool visible =
                roleAuthority != null &&
                !roleAuthority.IsInkPainter &&
                commandSeal != null &&
                commandSeal.IsSealActive;

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

            nextRefreshTime = Time.unscaledTime + 0.1f;
            Refresh();
        }

        public void Configure(
            InkPainterRoleAuthority authority,
            InkCommandSeal seal,
            GameObject targetPanel,
            Text targetText)
        {
            roleAuthority = authority;
            commandSeal = seal;
            panelRoot = targetPanel;
            statusText = targetText;
            Refresh();
        }

        private void Refresh()
        {
            if (statusText == null || commandSeal == null)
            {
                return;
            }

            statusText.text =
                "MÜREKKEP EMİR MÜHRÜ\n" +
                $"SÜNGER + E İLE EM  •  " +
                $"{commandSeal.RemainingInk:0}/" +
                $"{commandSeal.MaximumInk:0}";
        }
    }
}
