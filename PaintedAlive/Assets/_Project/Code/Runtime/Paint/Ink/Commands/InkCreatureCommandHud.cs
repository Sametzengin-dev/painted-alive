using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Paint.Ink.Commands
{
    [DisallowMultipleComponent]
    public sealed class InkCreatureCommandHud : MonoBehaviour
    {
        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkCreatureCommandDirector director;

        [SerializeField]
        private GameObject panelRoot;

        [SerializeField]
        private Text commandText;

        private float nextRefreshTime;

        private void Update()
        {
            bool visible =
                roleAuthority != null &&
                roleAuthority.IsInkPainter;

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

            nextRefreshTime = Time.unscaledTime + 0.12f;
            Refresh();
        }

        public void Configure(
            InkPainterRoleAuthority authority,
            InkCreatureCommandDirector commandDirector,
            GameObject targetPanel,
            Text targetText)
        {
            roleAuthority = authority;
            director = commandDirector;
            panelRoot = targetPanel;
            commandText = targetText;
            Refresh();
        }

        private void Refresh()
        {
            if (commandText == null || director == null)
            {
                return;
            }

            commandText.text =
                $"F5  SÜRÜ EMRİ  •  AKTİF {director.ActiveCommandCount}\n" +
                $"SHIFT+F5  SERBEST  •  {director.LastResult}";
        }
    }
}
